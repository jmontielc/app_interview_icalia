#!/bin/bash
# Runs when LocalStack is ready. Seeds local-only placeholder secrets.
# Values are not real credentials; the WireMock stubs only check that a Bearer token is present.
set -euo pipefail

awslocal secretsmanager create-secret \
  --name "bureau/equifax/api-token" \
  --secret-string "local-equifax-placeholder-token" || true

awslocal secretsmanager create-secret \
  --name "bureau/transunion/api-token" \
  --secret-string "local-transunion-placeholder-token" || true
