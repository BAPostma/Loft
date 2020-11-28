using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Amazon.S3;
using Amazon.S3.Model;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Handlers
{
    public class S3ArchiveHandler : HandlerBase
    {
        private const string ToHeaderName = "To";
        private const string EmailMessageExtension = "eml";
        private const string EmailMessageMimeType = "message/rfc822";
        private readonly AmazonS3Client _s3client = new AmazonS3Client();

        public override async Task Handle(SimpleEmailService<S3ReceiptAction> message, ILambdaContext context)
        {
            var bucket = message.Receipt.Action.BucketName;
            var mailbox = GetDestinationMailbox(message.Mail) ?? message.Receipt.Recipients.FirstOrDefault(); // Todo: tidy up

            var objectName = $"{message.Mail.MessageId}.{EmailMessageExtension}"; // add extension
            var originalKey = message.Receipt.Action.ObjectKey;
            var destinationKeyPrefix = string.IsNullOrEmpty(mailbox)         // if BCCd,
                                    ? message.Receipt.Action.ObjectKeyPrefix // leave as-is
                                    : $"mailbox/{mailbox}/";                 // otherwise, move to mailbox
            var destinationKey = $"{destinationKeyPrefix}{objectName}";      // append object name

            // Move the file in S3 to its destination
            await MoveMessage(message.Mail, bucket, originalKey, destinationKey);
            
            // Update S3 location information for the next handler
            message.Receipt.Action.ObjectKeyPrefix = destinationKeyPrefix;
            message.Receipt.Action.ObjectKey = destinationKey;

            await Next(message, context);
        }

        private async Task MoveMessage(SimpleEmailMessage mail, string bucket, string sourceKey, string destinationKey)
        {
            if(await CheckDestinationExists(bucket, destinationKey))
            {
                Log($"Destination already exists, message was moved previously or possible duplicate. Proceeding...");
                return;
            }

            var copyConfig = new CopyObjectRequest {
                SourceBucket = bucket,
                DestinationBucket = bucket,
                SourceKey = sourceKey,
                DestinationKey = destinationKey
            };

            SetMetadata(copyConfig, mail);
            
            var copy = await _s3client.CopyObjectAsync(copyConfig);
            var delete = await _s3client.DeleteObjectAsync(bucket, sourceKey);
            Log($"Moved message from {sourceKey} to {destinationKey}");
        }

        private async Task<bool> CheckDestinationExists(string bucketName, string destinationKey)
        {
            try
            {
                var message = await _s3client.GetObjectMetadataAsync(bucketName, destinationKey);
                return message.HttpStatusCode == HttpStatusCode.OK;
            }
            catch(AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            catch
            {
                throw;
            }
        }

        private void SetMetadata(CopyObjectRequest request, SimpleEmailMessage mail)
        {
            var subjectCleaned = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(mail.CommonHeaders?.Subject ?? string.Empty));
            request.ContentType = EmailMessageMimeType;

            request.MetadataDirective = S3MetadataDirective.REPLACE;
            request.Metadata.Add("MessageId", mail.MessageId);
            request.Metadata.Add("From", mail.Source);
            request.Metadata.Add("Subject", subjectCleaned);
            request.Metadata.Add("Received", mail.Timestamp.ToUniversalTime().ToString("s"));
        }

        private string GetDestinationMailbox(SimpleEmailMessage mail)
        {
            var destination = mail.Destination.FirstOrDefault();
            if (destination != null) return destination;
            
            var toHeader = mail.Headers.FirstOrDefault(h => h.Name == ToHeaderName);
            if (toHeader != null)
            {
                destination = toHeader.Value;
            }

            return destination;
        }
    }
}