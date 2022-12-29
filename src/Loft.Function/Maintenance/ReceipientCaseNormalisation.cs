using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Loft.Function.Models;
using Loft.Function.Models.DynamoDB;

namespace Loft.Function.Maintenance
{
    public static class ReceipientCaseNormalisation
    {
        private static readonly IAmazonDynamoDB _dynamoClient;
        private static readonly IDynamoDBContext _context;
        private static readonly AmazonS3Client _s3client;

        public static string TableName { get; } = Configuration.DynamoDbTableName;
        
        static ReceipientCaseNormalisation()
        {
            _dynamoClient = new AmazonDynamoDBClient();
            _context = new DynamoDBContext(_dynamoClient);
            _s3client = new AmazonS3Client();
        }

        /// <summary>Normalises casing on destination email addresses across the table and bucket</summary>
        /// <example>
        /// Converts EMAIL@alias.com to email@alias.com and consolidates messages under the latter.
        /// </example>
        public static async Task NormaliseReceipientCase()
        {
            var result = _context.ScanAsync<EmailMessage>(Enumerable.Empty<ScanCondition>(), new DynamoDBOperationConfig { OverrideTableName = TableName });
            var records = await result.GetRemainingAsync();
            foreach (var email in records)
            {
                if(email.Destination == email.Destination.ToLowerInvariant()) continue;

                LambdaLogger.Log($"Normalising message {email.Id} to {email.Destination}");

                var orig = email.Destination; // UPPERCASE@domain.com
                var norm = orig.ToLowerInvariant(); // uppercase@domain.com

                var origLoc = (email.Metadata.ObjectKeyPrefix, email.Metadata.ObjectKey);
                var normLoc = (origLoc.ObjectKeyPrefix.ToLowerInvariant(), origLoc.ObjectKey.ToLowerInvariant());

                var moveResult = await RenameItemInS3Bucket(email.Metadata.BucketName, origLoc, normLoc);
                if(!moveResult) continue;

                await RenameItemInDynamo(email, norm);
                LambdaLogger.Log($"Item {email.Id} to {email.Destination} updated");
            }
        }

        private static async Task<bool> RenameItemInS3Bucket(string bucketName, (string, string) orig, (string, string) norm)
        {
            var copyRequest = new CopyObjectRequest {
                SourceBucket = bucketName,
                SourceKey = orig.Item2,
                DestinationBucket = bucketName,
                DestinationKey = norm.Item2
            };

            // COPY
            AmazonWebServiceResponse result = await _s3client.CopyObjectAsync(copyRequest);
            if(result.HttpStatusCode != HttpStatusCode.OK)
            {
                LambdaLogger.Log($"Failed to move {copyRequest.SourceKey} to {copyRequest.DestinationKey} in bucket {bucketName}");
                return false;
            }
            LambdaLogger.Log($"Copied {copyRequest.SourceKey} to {copyRequest.DestinationKey} in bucket {bucketName}");

            // DELETE
            result = await _s3client.DeleteObjectAsync(bucketName, orig.Item2);
            if(result.HttpStatusCode !=  HttpStatusCode.OK)
            {
                LambdaLogger.Log($"Failed to delete {copyRequest.SourceKey} in bucket {bucketName} after copying it to {copyRequest.DestinationKey}");
                return false;
            }
            LambdaLogger.Log($"Deleted {copyRequest.SourceKey} from bucket {bucketName}");

            return true;
        }

        private static async Task RenameItemInDynamo(EmailMessage record, string norm)
        {
            record.Destination = record.Destination?.ToLowerInvariant();
            record.Metadata.ObjectKey = record.Metadata.ObjectKey.ToLowerInvariant();
            record.Metadata.ObjectKeyPrefix = record.Metadata.ObjectKeyPrefix.ToLowerInvariant();
            
            await _context.SaveAsync(record);
        }
    }
}