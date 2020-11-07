using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.SQSEvents;
using Loft.Function.Handlers;
using System;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Loft.Function
{
    public class LambdaFunction
    {
        private static AmazonLambdaClient _lambdaClient;

        private readonly CloudWatchLogHandler _cloudWatchLogHandler;
        private readonly S3ArchiveHandler _s3ArchiveHandler;
        private readonly DynamoStorageHandler _dynamoStorageHandler;
        private readonly IMAPSynchronisationHandler _imapSynchronisationHandler;
        private readonly IHandler _handlerChain;

        public LambdaFunction()
        {
            _cloudWatchLogHandler = new CloudWatchLogHandler();
            _s3ArchiveHandler = new S3ArchiveHandler();
            _dynamoStorageHandler = new DynamoStorageHandler();
            _imapSynchronisationHandler = new IMAPSynchronisationHandler();
            
            _cloudWatchLogHandler
                .SetNext(_s3ArchiveHandler)
                .SetNext(_dynamoStorageHandler)
                .SetNext(_imapSynchronisationHandler);

            _handlerChain = _cloudWatchLogHandler;
        }

        static async void initialize()
        {
            _lambdaClient = new AmazonLambdaClient();
            await callLambda();
        }

        public static async Task<GetAccountSettingsResponse> callLambda()
        {
            var request = new GetAccountSettingsRequest();
            var response = await _lambdaClient.GetAccountSettingsAsync(request);
            return response;
        }

        public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
        {
            if (sqsEvent == null)
            {
                LambdaLogger.Log("Function invoked but no event passed. Exiting...");
                return;
            }

            LambdaLogger.Log($"Function invoked from {sqsEvent.GetType().Name} with {sqsEvent.Records.Count} notification(s)");
            LambdaLogger.Log(JsonConvert.SerializeObject(sqsEvent));

            foreach (var record in sqsEvent.Records)
            {
                var sanitisedJson = JToken.Parse(record.Body); // json is escaped inside message
                var s3record = sanitisedJson.ToObject<SimpleEmailService<S3ReceiptAction>>();
                await _handlerChain.Handle(s3record, context);
            }

            LambdaLogger.Log("Function invocation completed");
        }

        [Obsolete("Only use when SES triggers Lambda from a rule-set action")]
        public async Task FunctionHandlerSES(SimpleEmailService<S3ReceiptAction> sesEvent, ILambdaContext context)
        {
            if (sesEvent == null)
            {
                LambdaLogger.Log("Function invoked but no event passed. Exiting...");
                return;
            }

            LambdaLogger.Log($"Function invoked directly from {sesEvent.GetType().Name} for 1 notification");
           
            await _handlerChain.Handle(sesEvent, context);

            LambdaLogger.Log("Function invocation completed");
        }

        [Obsolete("Only use when SNS tiggers Lambda upon notification on SNS topic")]
        public async Task FunctionHandlerSNS(SNSEvent snsEvent, ILambdaContext context)
        {
            if (snsEvent == null)
            {
                LambdaLogger.Log("Function invoked but no event passed. Exiting...");
                return;
            }

            LambdaLogger.Log($"Function invoked from {snsEvent.GetType().Name} with {snsEvent.Records.Count} notification(s)");
            
            foreach (var record in snsEvent.Records)
            {
                var sanitisedJson = JToken.Parse(record.Sns.Message); // json is escaped inside message
                var s3record = sanitisedJson.ToObject<SimpleEmailService<S3ReceiptAction>>();
                await _handlerChain.Handle(s3record, context);
            }

            LambdaLogger.Log("Function invocation completed");
        }
    }
}
