#!/usr/bin/env bash
set -euo pipefail

if [[ "${PRODUCTION_DEPLOYMENT_TARGET_CONFIGURED:-}" != "true" ]]; then
  cat >&2 <<'MESSAGE'
Production deployment target is not configured.

Set the GitHub production environment secret PRODUCTION_DEPLOYMENT_TARGET_CONFIGURED=true
only after replacing scripts/deploy/production-deploy-placeholder.sh with the real provider-specific
deployment command or adapting this script to the chosen hosting target.

No production deployment was performed.
MESSAGE
  exit 1
fi

echo "Production deployment target flag is set, but no provider-specific deployment implementation exists yet."
echo "Artifacts directory: ${DEPLOYMENT_ARTIFACTS_DIR:-deployment-artifacts}"
echo "Commit: ${DEPLOYMENT_COMMIT_SHA:-unknown}"
echo "Run id: ${DEPLOYMENT_RUN_ID:-unknown}"
echo "Replace this placeholder with the real deployment command before using deploy mode."
exit 1
