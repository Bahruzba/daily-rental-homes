#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
SCRIPT="$REPO_ROOT/scripts/deploy/production-vps-deploy.sh"

TMP_DIR="$(mktemp -d)"
cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

mkdir -p "$TMP_DIR/artifacts/backend" "$TMP_DIR/artifacts/frontend" "$TMP_DIR/artifacts/migrations"
printf 'fake backend\n' > "$TMP_DIR/artifacts/backend/DailyRentalHomes.Api.dll"
printf 'fake frontend\n' > "$TMP_DIR/artifacts/frontend/index.html"
printf '__EFMigrationsHistory\n' > "$TMP_DIR/artifacts/migrations/migrations-idempotent.sql"

run_deploy() {
  (
    cd "$REPO_ROOT"
    env \
      DEPLOYMENT_ARTIFACTS_DIR="$TMP_DIR/artifacts" \
      DEPLOYMENT_COMMIT_SHA="test-sha" \
      VPS_SSH_HOST="${VPS_SSH_HOST:-127.0.0.1}" \
      VPS_SSH_PORT="${VPS_SSH_PORT:-22}" \
      VPS_SSH_USER="${VPS_SSH_USER:-deploy}" \
      VPS_SSH_PRIVATE_KEY="${VPS_SSH_PRIVATE_KEY:-test-key}" \
      VPS_SSH_KNOWN_HOSTS="${VPS_SSH_KNOWN_HOSTS:-127.0.0.1 ssh-ed25519 test}" \
      VPS_DEPLOYMENT_DIRECTORY="${VPS_DEPLOYMENT_DIRECTORY:-/opt/daily-rental-homes}" \
      PRODUCTION_DATABASE_CONNECTION_STRING="${PRODUCTION_DATABASE_CONNECTION_STRING:-Server=db;Database=DailyRentalHomes;}" \
      PRODUCTION_TOKEN_KEY="${PRODUCTION_TOKEN_KEY:-test-token-key-with-more-than-32-bytes}" \
      VPS_DEPLOYMENT_DRY_RUN="${VPS_DEPLOYMENT_DRY_RUN:-true}" \
      "$SCRIPT"
  )
}

assert_contains() {
  local haystack="$1"
  local needle="$2"
  if [[ "$haystack" != *"$needle"* ]]; then
    printf 'Expected output to contain: %s\nActual output:\n%s\n' "$needle" "$haystack" >&2
    exit 1
  fi
}

set +e
missing_output="$(
  cd "$REPO_ROOT" &&
    env \
      DEPLOYMENT_ARTIFACTS_DIR="$TMP_DIR/artifacts" \
      DEPLOYMENT_COMMIT_SHA="test-sha" \
      VPS_SSH_USER="deploy" \
      VPS_SSH_PRIVATE_KEY="test-key" \
      VPS_SSH_KNOWN_HOSTS="127.0.0.1 ssh-ed25519 test" \
      VPS_DEPLOYMENT_DIRECTORY="/opt/daily-rental-homes" \
      PRODUCTION_DATABASE_CONNECTION_STRING="Server=db;Database=DailyRentalHomes;" \
      PRODUCTION_TOKEN_KEY="test-token-key-with-more-than-32-bytes" \
      "$SCRIPT" 2>&1
)"
missing_status=$?
set -e

if [[ "$missing_status" -eq 0 ]]; then
  printf 'Expected missing SSH host validation to fail.\n' >&2
  exit 1
fi
assert_contains "$missing_output" "VPS_SSH_HOST"

dry_run_output="$(run_deploy 2>&1)"
assert_contains "$dry_run_output" "VPS_DEPLOYMENT_DRY_RUN=true"
assert_contains "$dry_run_output" "Target directory: /opt/daily-rental-homes"

secret_output="$(
  PRODUCTION_TOKEN_KEY="SUPER_SECRET_MARKER_WITH_MORE_THAN_32_BYTES" run_deploy 2>&1
)"
if [[ "$secret_output" == *"SUPER_SECRET_MARKER"* ]]; then
  printf 'Secret marker was printed in deployment output.\n' >&2
  exit 1
fi

if grep -R "docker compose down -v" "$SCRIPT" "$REPO_ROOT/scripts/deploy/docker-compose.production.yml"; then
  printf 'Deployment must not delete persistent Docker volumes.\n' >&2
  exit 1
fi

if ! grep -q "scp" "$SCRIPT" || ! grep -q "ssh" "$SCRIPT"; then
  printf 'Deployment script must use SSH/SCP for the VPS target.\n' >&2
  exit 1
fi

printf 'production-vps-deploy script validation passed.\n'
