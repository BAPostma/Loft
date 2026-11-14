using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Humanizer;
using Loft.Function.Maintenance;

namespace Loft.Function
{
    public static class ControlFunction {
        public static async Task CommandHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            if (sqsEvent == null)
            {
                LambdaLogger.Log("Function invoked but no event passed. Exiting...");
                return;
            }

            foreach (var record in sqsEvent.Records)
            {
                if(string.IsNullOrWhiteSpace(record.Body)) continue;

                var command = ControlCommand.ParseFrom(record.Body);
                LambdaLogger.Log($"Control function called with command {command.Name} (parameters: {command.Parameters.Humanize()})");

                switch (command.Name)
                {
                    case "receipient-case-normalisation":
                        await ReceipientCaseNormalisation.NormaliseReceipientCase();
                        break;

                    case "reprocess-inbox":
                        await InboxReprocessor.Reprocess(command["bucket"]);
                        break;

                    default:
                        LambdaLogger.Log($"Control command {command.Name} not understood, cancelling execution");
                        break;
                }
            }

            LambdaLogger.Log("All commands finished processing");
        }
    }
}