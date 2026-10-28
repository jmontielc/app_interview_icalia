#!/bin/bash
# Local-only: publishes the PdfProcessor Lambda to LocalStack and wires S3 ObjectCreated events to it.
# Run after `docker compose up -d localstack`. Needs dotnet, zip, and awslocal (pip install awscli-local).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BUCKET="app-interview-documents-local"
FUNCTION="pdf-processor"
ACCOUNT="000000000000"
REGION="us-east-1"
WORK="$(mktemp -d)"

dotnet publish "$ROOT/src/Lambda/PdfProcessor/PdfProcessor.csproj" -c Release -r linux-x64 --self-contained false -o "$WORK/publish"
(cd "$WORK/publish" && zip -qr "$WORK/function.zip" .)

awslocal s3 mb "s3://$BUCKET" || true

awslocal lambda create-function \
  --function-name "$FUNCTION" \
  --runtime dotnet8 \
  --handler "PdfProcessor::PdfProcessor.Function::HandleAsync" \
  --role "arn:aws:iam::$ACCOUNT:role/lambda-local" \
  --zip-file "fileb://$WORK/function.zip"

awslocal lambda add-permission \
  --function-name "$FUNCTION" \
  --statement-id s3-invoke \
  --action lambda:InvokeFunction \
  --principal s3.amazonaws.com \
  --source-arn "arn:aws:s3:::$BUCKET"

awslocal s3api put-bucket-notification-configuration \
  --bucket "$BUCKET" \
  --notification-configuration "{\"LambdaFunctionConfigurations\":[{\"LambdaFunctionArn\":\"arn:aws:lambda:$REGION:$ACCOUNT:function:$FUNCTION\",\"Events\":[\"s3:ObjectCreated:*\"]}]}"

echo "PdfProcessor wired to s3://$BUCKET"
