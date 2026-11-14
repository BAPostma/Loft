using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Amazon.S3;
using Amazon.S3.Model;
using Loft.Function.Handlers;
using Loft.Function.Models;
using MimeKit;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Maintenance
{
    /// <summary>
    /// Re-ingests raw messages left in the S3 inbox/ prefix (e.g. after failed or lost
    /// notifications) by synthesising SES events for them and running the regular
    /// handler chain, so they are archived, stored in DynamoDB and synced to IMAP
    /// exactly like originally received mail.
    /// </summary>
    public static class InboxReprocessor
    {
        private const string InboxPrefix = "inbox/";
        // SES writes this placeholder object when the receipt rule is created; it is not an email
        private const string SetupNotificationKey = InboxPrefix + "AMAZON_SES_SETUP_NOTIFICATION";
        private static readonly AmazonS3Client _s3client = new();

        public static async Task Reprocess(string bucketName)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                // Not passed in the control command; fall back to the naming convention
                LambdaLogger.Log("No bucket parameter given, deriving bucket name from the Loft domain");
                bucketName = $"loft-{Configuration.LoftDomain}";
            }

            var keys = await ListInboxMessages(bucketName);
            LambdaLogger.Log($"Found {keys.Count} message(s) in {bucketName}/{InboxPrefix}");

            var handlerChain = BuildHandlerChain();

            var succeeded = 0;
            var skipped = 0;
            var failed = 0;
            foreach (var key in keys)
            {
                try
                {
                    var message = await BuildSyntheticEvent(bucketName, key);
                    if (message == null)
                    {
                        skipped++;
                        continue;
                    }

                    await handlerChain.Handle(message, null);
                    succeeded++;
                }
                catch (Exception ex)
                {
                    failed++;
                    LambdaLogger.Log($"Failed to reprocess {key}: {ex}");
                }
            }

            LambdaLogger.Log($"Done: {succeeded} reprocessed, {skipped} skipped, {failed} failed");
        }

        private static async Task<List<string>> ListInboxMessages(string bucketName)
        {
            var messages = new List<string>();
            var request = new ListObjectsV2Request { BucketName = bucketName, Prefix = InboxPrefix };
            do
            {
                var response = await _s3client.ListObjectsV2Async(request);
                // Filter out subfolders (which end with '/') and the SES setup notification
                messages.AddRange(response.S3Objects.Where(obj => !obj.Key.EndsWith('/') && obj.Key != SetupNotificationKey).Select(o => o.Key));

                request.ContinuationToken = response.NextContinuationToken;
            } 
            while (request.ContinuationToken != null);

            return messages;
        }

        private static IHandler BuildHandlerChain()
        {
            var cloudWatchLogHandler = new CloudWatchLogHandler();
            var s3ArchiveHandler = new S3ArchiveHandler();
            var dynamoStorageHandler = new DynamoStorageHandler();
            var imapSynchronisationHandler = new IMAPSynchronisationHandler();

            cloudWatchLogHandler
                .SetNext(s3ArchiveHandler)
                .SetNext(dynamoStorageHandler)
                .SetNext(imapSynchronisationHandler);

            return cloudWatchLogHandler;
        }

        /// <summary>
        /// Builds a SES event equivalent to what SES would have published for this object,
        /// using the stored raw message and S3 metadata. Returns null when the message
        /// cannot be backfilled (logged why by the caller).
        /// </summary>
        private static async Task<SimpleEmailService<S3ReceiptAction>> BuildSyntheticEvent(string bucketName, string messageObjectKey)
        {
            var messageId = messageObjectKey.Substring(InboxPrefix.Length);

            var getObject = await _s3client.GetObjectAsync(bucketName, messageObjectKey);
            MimeMessage mime;
            using (getObject.ResponseStream)
            {
                mime = await MimeMessage.LoadAsync(getObject.ResponseStream);
            }

            var destination = mime.To.Mailboxes
                .Select(m => m.Address)
                .Where(a => a.EndsWith($"@{Configuration.LoftDomain}", StringComparison.InvariantCultureIgnoreCase))
                .ToList();
            if (destination.Count == 0)
            {
                // BCC-only or unparsable: without an envelope recipient the chain cannot
                // route the message (see S3ArchiveHandler.GetDestinationMailbox).
                LambdaLogger.Log($"Skipping {messageObjectKey}: no recipient on the Loft domain found");
                return null;
            }

            var timestamp = mime.Date == DateTimeOffset.MinValue ? DateTime.UtcNow : mime.Date.UtcDateTime;
            var headers = mime.Headers.Select(h => new SimpleEmailHeader { Name = h.Field, Value = h.Value }).ToList();
            var source = ((mime.Sender ?? mime.From.Mailboxes.FirstOrDefault())?.Address)
                        ?? mime.ReplyTo.Mailboxes.FirstOrDefault()?.Address;

            var message = new SimpleEmailService<S3ReceiptAction>
            {
                Mail = new SimpleEmailMessage
                {
                    MessageId = messageId,
                    Timestamp = timestamp,
                    Destination = destination,
                    Headers = headers,
                    HeadersTruncated = false,
                    Source = source,
                    CommonHeaders = new SimpleEmailCommonHeaders
                    {
                        ReturnPath = ((mime.ReplyTo.Mailboxes.FirstOrDefault()?.Address) ?? source),
                        From = [mime.From.ToString()],
                        To = [.. mime.To.ToString().Split([", "], StringSplitOptions.RemoveEmptyEntries)],
                        Subject = mime.Subject,
                        Date = mime.Date.ToString("R"),
                        MessageId = mime.MessageId
                    }
                },
                Receipt = new SimpleEmailReceipt<S3ReceiptAction>
                {
                    Timestamp = timestamp,
                    Recipients = destination,
                    Action = new S3ReceiptAction
                    {
                        Type = "S3",
                        BucketName = bucketName,
                        ObjectKeyPrefix = InboxPrefix,
                        ObjectKey = messageObjectKey
                    }
                }
            };

            return message;
        }
    }
}
