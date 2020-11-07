using System;
using Amazon.Lambda.Core;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Amazon.S3;
using Humanizer;
using Loft.Function.MailKitHelpers;
using Loft.Function.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;
using Task = System.Threading.Tasks.Task;

namespace Loft.Function.Handlers
{
    public class IMAPSynchronisationHandler : HandlerBase
    {
        private readonly AmazonS3Client _s3client;
        private readonly ImapClient _imapClient;

        public IMAPSynchronisationHandler()
        {
            _s3client = new AmazonS3Client();
            
            var imapLambdaLogger = new LambdaProtocolLogger(Log);
            _imapClient = new ImapClient(imapLambdaLogger);
        }

        public override async Task Handle(SimpleEmailService<S3ReceiptAction> message, ILambdaContext context)
        {
            try
            {
                Log("Retrieving MIME message from S3 bucket");
                var rawMessageRequest = await _s3client.GetObjectAsync(message.Receipt.Action.BucketName, message.Receipt.Action.ObjectKey);
                Log($"MIME message retrieved: {rawMessageRequest.ContentLength.Bytes().Humanize()}");

                using var rawMessageStream = rawMessageRequest.ResponseStream;
                var mimeMessage = await MimeMessage.LoadAsync(rawMessageStream);
                Log("Message loaded & parsed");

                Log($"Uploading to {Configuration.IMAPServer}, debug logging {(Configuration.IMAPDebugLogging ? "on" : "off")}");
                await _imapClient.ConnectAsync(Configuration.IMAPServer, options: SecureSocketOptions.SslOnConnect);
                await _imapClient.AuthenticateAsync(Configuration.IMAPUsername, Configuration.IMAPPassword);
                var folder = await _imapClient.GetFolderAsync(Configuration.IMAPDestinationFolder);
                var open = await folder.OpenAsync(FolderAccess.ReadWrite);
                var upload = await folder.AppendAsync(mimeMessage, Configuration.IMAPMarkAsRead ? MessageFlags.Seen : MessageFlags.Recent, progress: new LambdaProgressLogger(Log));
                Log($"Stored message with ID {upload.Value.Id}");
            }
            catch(Exception ex)
            {
                Log(ex.ToString());
                throw; // ensure Lambda fails
            }
            finally
            {
                await _imapClient.DisconnectAsync(true);
                await Next(message, context);
            }
        }
    }
}