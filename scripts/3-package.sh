# Call with at least --s3-bucket <artifact bucket name> and optionally --s3-prefix <loft>
sam package --s3-bucket aws-lambda-sam-artifacts --s3-prefix loft --output-template-file package.yaml "$@"