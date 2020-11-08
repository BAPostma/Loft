# Call with at least these parameters: <your stack name> <your domain name>
aws s3 rm s3://loft-$2 --recursive
aws cloudformation delete-stack --stack-name $1