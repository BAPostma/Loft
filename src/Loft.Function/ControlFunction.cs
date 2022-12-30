using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
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
                
                LambdaLogger.Log($"Control function called with command {record.Body}");

                switch (record.Body)
                {
                    case "receipient-case-normalisation":
                        await ReceipientCaseNormalisation.NormaliseReceipientCase();
                        break;

                    default:
                        LambdaLogger.Log($"Control command {record.Body} not understood, cancelling execution");
                        break;
                }
            }

            LambdaLogger.Log("All commands finished processing");
        }
    }
}