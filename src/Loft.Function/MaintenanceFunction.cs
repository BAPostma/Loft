using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Loft.Function.Maintenance;

namespace Loft.Function
{
    public static class MaintenanceFunction {
        public static async Task Handler(SQSEvent sqsEvent, ILambdaContext context)
        {
            if (sqsEvent == null)
            {
                LambdaLogger.Log("Function invoked but no event passed. Exiting...");
                return;
            }

            foreach (var record in sqsEvent.Records)
            {
                if(string.IsNullOrWhiteSpace(record.Body)) continue;

                switch (record.Body)
                {
                    case "receipient-case-normalisation":
                        await ReceipientCaseNormalisation.NormaliseReceipientCase();
                        break;

                    default:
                        LambdaLogger.Log($"Maintenance command {record.Body} not understood, cancelling execution");
                        break;
                }
            }
        }
    }
}