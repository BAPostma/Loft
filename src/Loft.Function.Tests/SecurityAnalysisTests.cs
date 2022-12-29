using Amazon.Lambda.SimpleEmailEvents.Actions;
using Loft.Function.Models.DynamoDB;
using Xunit;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Tests;

public class SecurityAnalysisTests
{
    [Fact]
    public void ConstructEmptySecurityAnalysisModelWithoutSESData()
    {
        SimpleEmailService<S3ReceiptAction> rawMessage = new() {
            Receipt = new()
        };
        
        var model = SecurityAnalysis.CreateFrom(rawMessage.Receipt);

        Assert.NotNull(model);
    }
}