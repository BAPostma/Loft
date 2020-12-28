sam publish --template package.yaml "$@"

APP_ARN=$(aws serverlessrepo list-applications --output=json | jq -r ".Applications[] | .ApplicationId?" | grep Loft)

aws serverlessrepo put-application-policy \
    --application-id $APP_ARN \
    --statements Principals=*,Actions=Deploy