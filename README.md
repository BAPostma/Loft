# Loft
Receive email via Amazon Simple Email Service (SES) and synchronise it to a designated IMAP mailbox. Adequate logging, persistence in S3 and Dynamo DB is part of the handler's process.

## Purpose
Many email providers like Gmail allow for the addition of a '+' in an email address to create specialised address aliases (e.g. johndoe+contoso@example.com). This is a useful approach to limit exposure of your email address as well as to track a possible data leak, should that alias suddenly receive spam or appear on blacklists.

If you're not using a provider that supports this and you prefer an entire catch-all domain to support your anonymity, Loft will solve this problem using AWS services. With a few emails per day, the solution is practically $0 on your AWS bill, too!

### Name
This project is named after the construction that houses homing pigeons: a Loft. Each pigeonhole is essentially a separate mailbox.

# Architecture
![Architecture](./doc/architecture.png)
**The following AWS services are used:** CloudFormation, SES, SNS, SQS, S3, CloudWatch, Dynamo DB, Lambda.

## Prerequisites
- Domain name with control over MX records in DNS
- Configured Amazon Simple Email Service to receive email on the domain
- Mailbox that supports IMAP for synchronising email
  - IMAP credentials for the destination mail server

# Setup
The simplest means of installation is to fork this repository and run the GitHub action to build & install.  
You need to configure secrets to match the [Cloudformation parameters](./template.yaml) and [AWS Credentials](https://github.com/aws-actions/configure-aws-credentials#usage).  

Required:
- `LoftDomain` (e.g. `example.com`)
- `LambdaRoleARN`
- `IMAPServer` (requires SSL/TLS)
- `IMAPUsername`
- `IMAPPassword`

Optional:
- `IMAPDestinationFolder`
- `IMAPMarkAsRead`
- `IMAPDebugLogging`

## Deployment
This script uses AWS CloudFormation to deploy the Lambda functions and an IAM role. If the AWS CloudFormation stack that contains the resources already exists, the script updates it with any changes to the template or function code.

Run the [GitHub action](./.github/workflows/main.yml) to trigger the deployment into your AWS account.

_Note: this app is not deployed to the Serverless Application Repository because SES is not a supported resource._

## Activation
Ensure you set the newly created Amazon SES rule set as **active** under _Email receiving_ > _Rule Sets_.

## Cleanup
**Warning! This permanently destroys all email in the S3 bucket and Dynamo DB.**  
To delete the application, run the cleanup script: `loft$ scripts/6-remove.sh`.

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