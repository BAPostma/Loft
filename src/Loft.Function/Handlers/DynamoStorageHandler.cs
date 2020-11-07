using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Loft.Function.Models;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.DataModel;
using Loft.Function.Models.DynamoDB;

namespace Loft.Function.Handlers
{
    public class DynamoStorageHandler : HandlerBase
    {
        private readonly IAmazonDynamoDB _client;
        private readonly Table _table;
        private readonly IDynamoDBContext _context;

        public string TableName { get; private set; }

        public DynamoStorageHandler()
        {
            TableName = Configuration.DynamoDbTableName;

            _client = new AmazonDynamoDBClient();
            _table = Table.LoadTable(_client, TableName);
            _context = new DynamoDBContext(_client);
        }

        public override async Task Handle(SimpleEmailService<S3ReceiptAction> message, ILambdaContext context)
        {
            var document = new EmailMessage(message);
            await _context.SaveAsync(document, new DynamoDBOperationConfig { OverrideTableName = TableName });

            Log($"Message stored in DynamoDB under key {document.Id}");

            await Next(message, context);
        }
    }
}