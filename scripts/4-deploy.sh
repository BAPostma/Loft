# Call with at least: <stack name> <domain name> and <lambda role arn>
sam deploy \
    --stack-name $1 \
    --no-confirm-changeset \
    --no-fail-on-empty-changeset \
    --parameter-overrides "\
        ParameterKey=LoftDomain,ParameterValue=$2 \
        ParameterKey=LambdaRoleARN,ParameterValue=$3 \
    " \
    --guided