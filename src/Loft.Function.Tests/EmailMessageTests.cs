using System.Collections.Generic;
using Amazon.Lambda.SimpleEmailEvents;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using Loft.Function.Models.DynamoDB;
using Xunit;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Tests;

public class EmailMessageTests
{
    public EmailMessageTests()
    {
        SimpleEmailService<S3ReceiptAction> rawMessage = new()
        {
            Mail = new()
            {
                // Source = "unit@test.com",
                // Destination = new List<string> { "Unit test <unit@test.com>" },
                CommonHeaders = new SimpleEmailCommonHeaders {
                    From = new List<string> { "Unit test <unit@test.com>" },
                    To = null // new List<string> { "Test runner <runner@tests.com>" }
                },
                // Headers = new List<SimpleEmailHeader> {
                //     new SimpleEmailHeader {
                //         Name = "To",
                //         Value = null
                //     }
                // }
            },
            // Receipt = new()
            // {
            //     Action = new() {
            //         Type = "S3",
            //         TopicArn = "arn:aws:sns:eu-west-1:012345678912:unit-test",
            //         BucketName = "loft-unit-test-nonexistent-bucket",
            //         ObjectKeyPrefix = "inbox/",
            //         ObjectKey = "inbox/5e57ffl0agm01vj876t7vs3f7vf62brk9edgmd81"
            //     }
            // }
        };
    }

    [Fact]
    public void ConstructEmptyMessageModelForBrokenInput()
    {
        SimpleEmailService<S3ReceiptAction> rawMessage = new()
        {
            Mail = new(),
            Receipt = new()
        };

        var modelConstructionException = Record.Exception(() => new EmailMessage(null, rawMessage));

        Assert.Null(modelConstructionException);
    }

    [Fact]
    // Bug fix #2 (https://github.com/BAPostma/Loft/issues/2)
    public void ConstructMessageModelWithoutToHeader()
    {
        SimpleEmailService<S3ReceiptAction> rawMessage = new()
        {
            Mail = new()
            {
                Destination = null, // this is okay
                CommonHeaders = new SimpleEmailCommonHeaders {
                    From = new List<string> { "Unit test <unit@test.com>" },
                    To = null // this is okay
                }
            }
        };

        EmailMessage model = new(null, rawMessage);

        Assert.Null(model.Destination);
        Assert.Null(model.DestinationName);
    }

    [Fact]
    // Bug fix #4 (https://github.com/BAPostma/Loft/issues/4)
    public void SelectLoftAliasIfMultipleRecipientsAndNotFirstRecipient()
    {
        SimpleEmailService<S3ReceiptAction> rawMessage = new()
        {
            Mail = new()
            {
                Destination = new List<string> {
                    "someone-else@domain.com",
                    "me@Loft.COM" // we're second
                },
                CommonHeaders = new SimpleEmailCommonHeaders {
                    To = new List<string> {
                        "\"Someone Else\" <someone-else@domain.com>",
                        "\"Myself\" <ME@LOFT.COM>" // we're second
                    }
                }
            }
        };

        EmailMessage model = new("loft.com", rawMessage);

        Assert.Equal("me@Loft.COM", model.Destination);
        Assert.Equal("\"Myself\" <ME@LOFT.COM>", model.DestinationName);
    }
}