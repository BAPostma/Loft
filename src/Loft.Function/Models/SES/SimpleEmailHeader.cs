using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;

namespace Loft.Function.Models.SES
{
    // Attempt to fix https://github.com/aws/aws-sdk-net/issues/1930
    public class SimpleEmailHeader : SimpleEmailEvent<IReceiptAction>.SimpleEmailHeader
    {
        public SimpleEmailHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}