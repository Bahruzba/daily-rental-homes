#!/usr/bin/env bash
set -euo pipefail

log() {
  printf '%s\n' "$*"
}

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

require_value() {
  local name="$1"
  local value="${!name:-}"
  if [[ -z "$value" ]]; then
    fail "$name is required for VPS production deployment."
  fi
}

quote_remote() {
  printf '%q' "$1"
}

validate_deployment_directory() {
  local directory="$1"
  if [[ "$directory" != /* ]]; then
    fail "VPS_DEPLOYMENT_DIRECTORY must be an absolute Linux path."
  fi

  case "$directory" in
    "/"|"/root"|"/home"|"/var"|"/opt"|"/usr"|"/srv")
      fail "VPS_DEPLOYMENT_DIRECTORY must point to an application-specific directory, not $directory."
      ;;
  esac
}

require_value DEPLOYMENT_ARTIFACTS_DIR
require_value DEPLOYMENT_COMMIT_SHA
require_value VPS_SSH_HOST
require_value VPS_SSH_USER
require_value VPS_SSH_PRIVATE_KEY
require_value VPS_SSH_KNOWN_HOSTS
require_value VPS_DEPLOYMENT_DIRECTORY
require_value PRODUCTION_DATABASE_CONNECTION_STRING
require_value PRODUCTION_TOKEN_KEY

VPS_SSH_PORT="${VPS_SSH_PORT:-22}"
FILE_STORAGE_PROVIDER="${FILE_STORAGE_PROVIDER:-Local}"
NOTIFICATION_PROVIDER="${NOTIFICATION_PROVIDER:-Fake}"
API_HTTP_PORT="${API_HTTP_PORT:-5099}"
FRONTEND_HTTP_PORT="${FRONTEND_HTTP_PORT:-80}"

if ! [[ "$VPS_SSH_PORT" =~ ^[0-9]+$ ]]; then
  fail "VPS_SSH_PORT must be numeric."
fi

validate_deployment_directory "$VPS_DEPLOYMENT_DIRECTORY"

if [[ ! -d "$DEPLOYMENT_ARTIFACTS_DIR/backend" ]]; then
  fail "Backend deployment artifact directory was not found."
fi

if [[ ! -d "$DEPLOYMENT_ARTIFACTS_DIR/frontend" ]]; then
  fail "Frontend deployment artifact directory was not found."
fi

if [[ ! -f "$DEPLOYMENT_ARTIFACTS_DIR/migrations/migrations-idempotent.sql" ]]; then
  fail "Migration SQL artifact was not found."
fi

TMP_DIR="$(mktemp -d)"
cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

PACKAGE_DIR="$TMP_DIR/package"
mkdir -p "$PACKAGE_DIR/backend" "$PACKAGE_DIR/frontend" "$PACKAGE_DIR/migrations"

cp -R "$DEPLOYMENT_ARTIFACTS_DIR/backend/." "$PACKAGE_DIR/backend/"
cp -R "$DEPLOYMENT_ARTIFACTS_DIR/frontend/." "$PACKAGE_DIR/frontend/"
cp "$DEPLOYMENT_ARTIFACTS_DIR/migrations/migrations-idempotent.sql" "$PACKAGE_DIR/migrations/migrations-idempotent.sql"
cp scripts/deploy/docker-compose.production.yml "$PACKAGE_DIR/docker-compose.production.yml"
cp scripts/deploy/Dockerfile.api-runtime "$PACKAGE_DIR/Dockerfile.api-runtime"
cp scripts/deploy/Dockerfile.frontend-runtime "$PACKAGE_DIR/Dockerfile.frontend-runtime"
cp scripts/deploy/nginx.frontend.conf "$PACKAGE_DIR/nginx.frontend.conf"

{
  printf 'DEPLOYMENT_COMMIT_SHA=%s\n' "$DEPLOYMENT_COMMIT_SHA"
  printf 'API_HTTP_PORT=%s\n' "$API_HTTP_PORT"
  printf 'FRONTEND_HTTP_PORT=%s\n' "$FRONTEND_HTTP_PORT"
} > "$PACKAGE_DIR/.env.compose"
chmod 600 "$PACKAGE_DIR/.env.compose"

{
  printf 'ASPNETCORE_ENVIRONMENT=Production\n'
  printf 'ASPNETCORE_URLS=http://+:8080\n'
  printf 'ConnectionStrings__DefaultConnection=%s\n' "$PRODUCTION_DATABASE_CONNECTION_STRING"
  printf 'Token__Issuer=%s\n' "${TOKEN_ISSUER:-DailyRentalHomes}"
  printf 'Token__Audience=%s\n' "${TOKEN_AUDIENCE:-DailyRentalHomesClients}"
  printf 'Token__Key=%s\n' "$PRODUCTION_TOKEN_KEY"
  printf 'Token__Minutes=%s\n' "${TOKEN_MINUTES:-1440}"
  printf 'Notifications__WorkerEnabled=%s\n' "${NOTIFICATIONS_WORKER_ENABLED:-false}"
  printf 'NotificationDelivery__Provider=%s\n' "$NOTIFICATION_PROVIDER"
  printf 'FileStorage__Provider=%s\n' "$FILE_STORAGE_PROVIDER"
  printf 'FileStorage__Local__RootPath=%s\n' "${FILE_STORAGE_LOCAL_ROOT_PATH:-uploads}"
  printf 'FileStorage__Local__PrivateRootPath=%s\n' "${FILE_STORAGE_LOCAL_PRIVATE_ROOT_PATH:-private-uploads}"
  printf 'FileStorage__Local__PublicBasePath=%s\n' "${FILE_STORAGE_LOCAL_PUBLIC_BASE_PATH:-/uploads}"
  printf 'FileStorage__S3__BucketName=%s\n' "${FILE_STORAGE_S3_BUCKET_NAME:-}"
  printf 'FileStorage__S3__Region=%s\n' "${FILE_STORAGE_S3_REGION:-}"
  printf 'FileStorage__S3__ServiceUrl=%s\n' "${FILE_STORAGE_S3_SERVICE_URL:-}"
  printf 'FileStorage__S3__AccessKey=%s\n' "${FILE_STORAGE_S3_ACCESS_KEY:-}"
  printf 'FileStorage__S3__SecretKey=%s\n' "${FILE_STORAGE_S3_SECRET_KEY:-}"
  printf 'FileStorage__S3__PublicBaseUrl=%s\n' "${FILE_STORAGE_S3_PUBLIC_BASE_URL:-}"
  printf 'FileStorage__S3__ForcePathStyle=%s\n' "${FILE_STORAGE_S3_FORCE_PATH_STYLE:-false}"
  printf 'NotificationDelivery__MetaWhatsApp__PhoneNumberId=%s\n' "${NOTIFICATION_META_PHONE_NUMBER_ID:-}"
  printf 'NotificationDelivery__MetaWhatsApp__AccessToken=%s\n' "${NOTIFICATION_META_ACCESS_TOKEN:-}"
  printf 'NotificationDelivery__MetaWhatsApp__ApiVersion=%s\n' "${NOTIFICATION_META_API_VERSION:-}"
  printf 'NotificationDelivery__MetaWhatsApp__WebhookVerifyToken=%s\n' "${NOTIFICATION_META_WEBHOOK_VERIFY_TOKEN:-}"
  printf 'NotificationDelivery__MetaWhatsApp__AppSecret=%s\n' "${NOTIFICATION_META_APP_SECRET:-}"
} > "$PACKAGE_DIR/app.env.production"
chmod 600 "$PACKAGE_DIR/app.env.production"

PACKAGE_ARCHIVE="$TMP_DIR/daily-rental-homes-${DEPLOYMENT_COMMIT_SHA}.tar.gz"
tar -C "$PACKAGE_DIR" -czf "$PACKAGE_ARCHIVE" .

log "Prepared production deployment package for commit $DEPLOYMENT_COMMIT_SHA."
log "Target VPS: ${VPS_SSH_USER}@${VPS_SSH_HOST}:${VPS_SSH_PORT}"
log "Target directory: $VPS_DEPLOYMENT_DIRECTORY"
log "Secret values were not printed."

if [[ "${VPS_DEPLOYMENT_DRY_RUN:-false}" == "true" ]]; then
  log "VPS_DEPLOYMENT_DRY_RUN=true; package validation completed without opening an SSH connection."
  exit 0
fi

KEY_FILE="$TMP_DIR/vps_deploy_key"
KNOWN_HOSTS_FILE="$TMP_DIR/known_hosts"
printf '%s\n' "$VPS_SSH_PRIVATE_KEY" > "$KEY_FILE"
printf '%s\n' "$VPS_SSH_KNOWN_HOSTS" > "$KNOWN_HOSTS_FILE"
chmod 600 "$KEY_FILE" "$KNOWN_HOSTS_FILE"

SSH_OPTS=(
  -i "$KEY_FILE"
  -p "$VPS_SSH_PORT"
  -o "BatchMode=yes"
  -o "StrictHostKeyChecking=yes"
  -o "UserKnownHostsFile=$KNOWN_HOSTS_FILE"
)

SCP_OPTS=(
  -i "$KEY_FILE"
  -P "$VPS_SSH_PORT"
  -o "BatchMode=yes"
  -o "StrictHostKeyChecking=yes"
  -o "UserKnownHostsFile=$KNOWN_HOSTS_FILE"
)

REMOTE_PACKAGE="/tmp/daily-rental-homes-${DEPLOYMENT_COMMIT_SHA}.tar.gz"
REMOTE_DEPLOY_DIR="$(quote_remote "$VPS_DEPLOYMENT_DIRECTORY")"
REMOTE_SHA="$(quote_remote "$DEPLOYMENT_COMMIT_SHA")"
REMOTE_PACKAGE_QUOTED="$(quote_remote "$REMOTE_PACKAGE")"

scp "${SCP_OPTS[@]}" "$PACKAGE_ARCHIVE" "${VPS_SSH_USER}@${VPS_SSH_HOST}:$REMOTE_PACKAGE"

ssh "${SSH_OPTS[@]}" "${VPS_SSH_USER}@${VPS_SSH_HOST}" "DEPLOY_DIR=$REMOTE_DEPLOY_DIR DEPLOY_SHA=$REMOTE_SHA DEPLOY_PACKAGE=$REMOTE_PACKAGE_QUOTED bash -s" <<'REMOTE_SCRIPT'
set -euo pipefail

release_dir="$DEPLOY_DIR/releases/$DEPLOY_SHA"
mkdir -p "$release_dir" "$DEPLOY_DIR/shared"
tar -xzf "$DEPLOY_PACKAGE" -C "$release_dir"
rm -f "$DEPLOY_PACKAGE"

ln -sfn "$release_dir" "$DEPLOY_DIR/current"
cd "$DEPLOY_DIR/current"

docker compose -f docker-compose.production.yml --env-file .env.compose up -d --build --remove-orphans

printf '%s\n' "$DEPLOY_SHA" > "$DEPLOY_DIR/.deployed-revision"
REMOTE_SCRIPT

log "VPS deployment command completed. Existing Docker volumes were preserved."
