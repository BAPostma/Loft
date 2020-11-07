using Amazon.Lambda.SimpleEmailEvents.Actions;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Models.DynamoDB
{
    public class SecurityAnalysis
    {
        public string DKIM { get; set; }
        public string SPF { get; set; }
        public string Spam { get; set; }
        public string Virus { get; set; }

        public SecurityAnalysis() { }

        public static SecurityAnalysis CreateFrom(SimpleEmailReceipt<S3ReceiptAction> receipt) => new SecurityAnalysis
        {
            DKIM = receipt.DKIMVerdict.Status,
            SPF = receipt.SPFVerdict.Status,
            Spam = receipt.SpamVerdict.Status,
            Virus = receipt.VirusVerdict.Status
        };
    }
}