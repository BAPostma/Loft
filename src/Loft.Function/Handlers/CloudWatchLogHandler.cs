using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Humanizer;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Handlers
{
    public class CloudWatchLogHandler : HandlerBase
    {
        public override async Task Handle(SimpleEmailService<S3ReceiptAction> message, ILambdaContext context)
        {
            LogPrefix = $"[{message.Mail.MessageId}] ";
            
            Log($"Message received from {message.Mail.Source}");
            Log($"Message destined for {message.Receipt.Recipients.Humanize(",")}");
            Log($"Message saved in bucket {message.Receipt.Action.BucketName} as {message.Receipt.Action.ObjectKey}");
            
            await Next(message, context);
        }
    }
}