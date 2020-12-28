# Loft 🕊
Receive email via Amazon Simple Email Service (SES) and synchronise it to a designated IMAP mailbox. Adequate logging, persistence in S3 and Dynamo DB is part of the handler's process.

[![CI](https://github.com/BAPostma/Loft/workflows/CI/badge.svg?branch=master)](https://github.com/BAPostma/Loft/actions?query=workflow%3ACI)
[![AWS SAR](https://img.shields.io/badge/Serverless%20Application%20Repository-Deploy%20Loft%20%E2%96%B6-orange?style=flat&logo=amazon-aws)](https://serverlessrepo.aws.amazon.com/applications/eu-west-1/386916026209/Loft)

## Purpose
For anyone who wants to keep their email address anonymous, Loft is a catch-all email receiver based on AWS. Loft moves email sent to any '[alias]@[mydomain].com'  to an IMAP mailbox such as Gmail. Unlike email providers who allow for the addition of a '+' in email addresses to create aliases (e.g. johndoe+contoso@example.com), Loft works with any registered alias, for any domain name you register. Thereby solving the problem that email addresses often contain (parts of) a real name. What's more is that with a few dozen emails per day, the solution is practically $0 on your AWS bill too!

### Name
This project is named after the construction that houses homing pigeons: a Loft. Each pigeonhole is essentially a separate mailbox.

# Architecture
![Architecture](https://raw.githubusercontent.com/BAPostma/Loft/master/doc/architecture.svg)
**The following AWS services are used:** CloudFormation, SES, SNS, SQS, S3, CloudWatch, Dynamo DB, Lambda, SAR and IAM.

## Prerequisites
- Domain name with control over MX records in DNS
- Configured Amazon Simple Email Service to receive email on the domain ([guide](https://docs.aws.amazon.com/ses/latest/DeveloperGuide/receiving-email-setting-up.html))
- Mailbox that supports IMAP for synchronising email (e.g. [Gmail](https://support.google.com/mail/answer/7126229?hl=en-GB))
  - IMAP credentials for the destination mail server (e.g. [Gmail app password](https://support.google.com/mail/answer/185833?hl=en-GB) is recommended to avoid using your main password)

## Deployment & activation
Loft is available in the [AWS Serverless Application repository](https://serverlessrepo.aws.amazon.com/applications/eu-west-1/386916026209/Loft) from where you can deploy directly into your AWS account using AWS CloudFormation.

1. [Click here to deploy directly](https://console.aws.amazon.com/lambda/home?#/create/app?applicationId=arn:aws:serverlessrepo:eu-west-1:386916026209:applications/Loft)
1. Set the new SES _Rule Set_ as **active** under [_Email receiving_ > _Rule Sets_](https://console.aws.amazon.com/ses/home#receipt-rules:)

Note: when deploying **manually**, the default AWS region is Ireland (`eu-west-1`). You can change this in [`samconfig.toml`](./samconfig.toml).

## Cleanup
1. In the [S3 console](https://s3.console.aws.amazon.com/s3/home), empty the Loft email bucket  
**Warning! This permanently destroys all email in the S3 bucket and Dynamo DB.**
1. Delete the Loft CloudFormation stack from the [control panel](https://console.aws.amazon.com/cloudformation/home)

Alternatively, you can run the cleanup script: `loft$ scripts/6-remove.sh`.

# Contributing
We'd love contributions that help to build a web UI on top of Dynamo DB, providing metrics on incoming mail and used aliases for the domain.

The project source includes function code and supporting resources:
- `src/` - C# .NET Core Lambda function with the solution file in the repository root under `Loft.sln`.
- `doc/` - Architecture diagram from [Draw.io](https://www.draw.io).
- `scripts/` - Shell scripts that use the AWS SAM CLI to deploy and manage the application.

## Prerequisites
To develop on any platform, you'll need the following toolchain:
- [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) (or higher)
- [AWS extensions for .NET CLI](https://github.com/aws/aws-extensions-for-dotnet-cli)
- [AWS CLI v1](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-install.html).
- [AWS Serverless Application Model (SAM) CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html)
