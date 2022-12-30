using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Newtonsoft.Json;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Models.DynamoDB
{
    [DynamoDBTable("loft", true)]
    public class EmailMessage
    {
        [DynamoDBHashKey]
        public string Id { get; set; }
        [DynamoDBRangeKey]
        public DateTime? Timestamp { get; set; }

        [DynamoDBGlobalSecondaryIndexHashKey]
        public string Source { get; set; }
        public string SourceName { get; set; }
        [DynamoDBGlobalSecondaryIndexHashKey]
        public string Destination { get; set; }
        public string DestinationName { get; set; }
        public string Subject { get; set; }
        public string Sent { get; set; }
        public string ReturnPath { get; set; }
        public string MessageId { get; set; }
        public IList<SimpleEmailHeader> Headers { get; set; }
        public SecurityAnalysis SecurityAnalysis { get; set; }
        public S3ReceiptAction Metadata { get; set; }

        [DynamoDBProperty("_raw")]
        public string Raw { get; set; }

        public EmailMessage() { }

        public EmailMessage(string domain, SimpleEmailService<S3ReceiptAction> message)
        {
            if(message == null || message.Mail == null)
                throw new ArgumentNullException(nameof(message), "Mail data is missing from SES event");

            Id = message.Mail.MessageId;
            Timestamp = message.Mail.Timestamp == DateTime.MinValue ? (DateTime?)null : message.Mail.Timestamp;
            Source = message.Mail.Source;
            SourceName = message.Mail.CommonHeaders?.From?.FirstOrDefault();
            Destination = message.Mail.Destination?.FirstOrDefault(d => d.Contains(domain, StringComparison.InvariantCultureIgnoreCase));
            DestinationName = message.Mail.CommonHeaders?.To?.FirstOrDefault(d => d.Contains(domain, StringComparison.InvariantCultureIgnoreCase));
            Subject = message.Mail.CommonHeaders?.Subject;
            Sent = message.Mail.CommonHeaders?.Date;
            ReturnPath = message.Mail.CommonHeaders?.ReturnPath;
            MessageId = message.Mail.CommonHeaders?.MessageId;
            
            Headers = message.Mail.Headers;
            
            SecurityAnalysis = SecurityAnalysis.CreateFrom(message.Receipt);
            
            Metadata = message.Receipt?.Action;

            Raw = JsonConvert.SerializeObject(message);
        }
    }
}