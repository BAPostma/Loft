using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Loft.Function.Models;

namespace Loft.Function.Maintenance
{
    public static class ReceipientCaseNormalisation
    {
        private static readonly IAmazonDynamoDB _dynamoClient;
        private static readonly Table _dynamoTable;
        private static readonly AmazonS3Client _s3client;

        public static string TableName { get; } = Configuration.DynamoDbTableName;
        
        static ReceipientCaseNormalisation()
        {
            // Using Document Interface because deserialisation fails (https://github.com/aws/aws-sdk-net/issues/1930)
            _dynamoClient = new AmazonDynamoDBClient();
            _dynamoTable = Table.LoadTable(_dynamoClient, TableName);
            _s3client = new AmazonS3Client();
        }

        /// <summary>Normalises casing on destination email addresses across the table and bucket</summary>
        /// <example>
        /// Converts EMAIL@alias.com to email@alias.com and consolidates messages under the latter.
        /// </example>
        public static async Task NormaliseReceipientCase()
        {
            var result = _dynamoTable.Scan(new ScanOperationConfig());
            var records = await result.GetRemainingAsync();
            foreach (Document email in records)
            {
                var id = email["id"].AsString();
                var destination = email["destination"].AsString();
                if(destination == destination.ToLowerInvariant()) continue;

                LambdaLogger.Log($"Normalising message {id} to {destination}");

                var orig = destination; // UPPERCASE@domain.com
                var norm = orig.ToLowerInvariant(); // uppercase@domain.com

                var metadata = email["metadata"].AsDocument();
                var origLoc = (metadata["ObjectKeyPrefix"].AsString(), metadata["ObjectKey"].AsString());
                var normLoc = (origLoc.Item1.ToLowerInvariant(), origLoc.Item2.ToLowerInvariant());

                var (copyResult, _) = await RenameItemInS3Bucket(metadata["BucketName"].AsString(), origLoc, normLoc);
                if(!copyResult) continue;

                await RenameItemInDynamo(email, norm, normLoc);
                LambdaLogger.Log($"Item {id} to {destination} updated");
            }
        }

        private static async Task<(bool copy, bool delete)> RenameItemInS3Bucket(string bucketName, (string, string) orig, (string, string) norm)
        {
            var copyRequest = new CopyObjectRequest {
                SourceBucket = bucketName,
                SourceKey = orig.Item2,
                DestinationBucket = bucketName,
                DestinationKey = norm.Item2
            };

            // COPY
            var copyResult = await _s3client.CopyObjectAsync(copyRequest);
            if(copyResult.HttpStatusCode != HttpStatusCode.OK)
            {
                LambdaLogger.Log($"Failed to copy {copyRequest.SourceKey} to {copyRequest.DestinationKey} in bucket {bucketName}");
            }
            else
            {
                LambdaLogger.Log($"Copied {copyRequest.SourceKey} to {copyRequest.DestinationKey} in bucket {bucketName}");
            }

            // DELETE
            var deleteResult = await _s3client.DeleteObjectAsync(bucketName, orig.Item2);
            if(deleteResult.HttpStatusCode !=  HttpStatusCode.NoContent)
            {
                LambdaLogger.Log($"Failed to delete {copyRequest.SourceKey} in bucket {bucketName} after copying it to {copyRequest.DestinationKey}");
            }
            else
            {
                LambdaLogger.Log($"Deleted {copyRequest.SourceKey} from bucket {bucketName}");
            }

            return (copyResult.HttpStatusCode == HttpStatusCode.OK, deleteResult.HttpStatusCode == HttpStatusCode.OK);
        }

        private static async Task RenameItemInDynamo(Document record, string norm, (string, string) normLoc)
        {
            record["destination"] = norm;
            record["metadata"].AsDocument()["ObjectKeyPrefix"] = normLoc.Item1;
            record["metadata"].AsDocument()["ObjectKey"] = normLoc.Item2;
            
            await _dynamoTable.UpdateItemAsync(record);
        }
    }
}