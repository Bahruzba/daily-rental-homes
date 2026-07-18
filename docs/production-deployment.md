# Manual production deployment

Production deployment is intentionally manual. The workflow validates, packages, and can deploy the application to a Linux VPS over SSH using Docker Compose when the production environment is explicitly configured.

Workflow:

- GitHub Actions: `Manual Production Deployment`
- Trigger: `workflow_dispatch` only
- Environment: `production`
- No automatic deployment on pull requests, pushes, or tags

## Before running

1. Merge approved code to `main`.
2. Confirm the CI quality gates pass:
   - `Backend`
   - `Frontend`
   - `Migrations`
3. In GitHub repository settings, create an environment named `production`.
4. Configure required reviewers for the `production` environment.
5. Add production secrets and variables as environment or repository configuration.
6. Prepare the Linux VPS with Docker Engine, the Docker Compose plugin, and a restricted deploy user.

## Required secrets

Always required:

- `PRODUCTION_DATABASE_CONNECTION_STRING`
- `PRODUCTION_TOKEN_KEY`

Required when `file_storage_provider=S3`:

- `FILE_STORAGE_S3_BUCKET_NAME`
- `FILE_STORAGE_S3_REGION` or `FILE_STORAGE_S3_SERVICE_URL`
- `FILE_STORAGE_S3_ACCESS_KEY` and `FILE_STORAGE_S3_SECRET_KEY` only when explicit S3 credentials are used

Required when `notification_provider=MetaWhatsApp`:

- `NOTIFICATION_META_PHONE_NUMBER_ID`
- `NOTIFICATION_META_ACCESS_TOKEN`
- `NOTIFICATION_META_API_VERSION`
- `NOTIFICATION_META_WEBHOOK_VERIFY_TOKEN`
- `NOTIFICATION_META_APP_SECRET`

Required for deploy mode:

- `PRODUCTION_DEPLOYMENT_TARGET_CONFIGURED=true`
- `VPS_SSH_HOST`
- `VPS_SSH_USER`
- `VPS_SSH_PRIVATE_KEY`
- `VPS_SSH_KNOWN_HOSTS`

Required deploy variables:

- `VPS_DEPLOYMENT_DIRECTORY`, for example `/opt/daily-rental-homes`

Optional deploy variables:

- `VPS_SSH_PORT`, default `22`
- `API_HTTP_PORT`, default `5099`
- `FRONTEND_HTTP_PORT`, default `80`
- `TOKEN_ISSUER`, default `DailyRentalHomes`
- `TOKEN_AUDIENCE`, default `DailyRentalHomesClients`
- `TOKEN_MINUTES`, default `1440`
- `NOTIFICATIONS_WORKER_ENABLED`, default `false`
- `FILE_STORAGE_LOCAL_ROOT_PATH`, default `uploads`
- `FILE_STORAGE_LOCAL_PRIVATE_ROOT_PATH`, default `private-uploads`
- `FILE_STORAGE_LOCAL_PUBLIC_BASE_PATH`, default `/uploads`
- `FILE_STORAGE_S3_PUBLIC_BASE_URL`
- `FILE_STORAGE_S3_FORCE_PATH_STYLE`, default `false`

Never store production secrets in tracked files or workflow YAML.

## What the workflow does

The workflow:

1. Validates required production secret presence without printing values.
2. Rejects obvious placeholder JWT signing keys.
3. Restores, builds, and tests the backend in Release mode.
4. Publishes the backend API artifact.
5. Builds the frontend production bundle.
6. Generates an idempotent EF Core migration SQL script.
7. Builds the production backend Docker image to validate the Dockerfile.
8. Uploads backend, frontend, and migration artifacts.
9. Enters the protected `production` environment for the deployment gate.
10. In deploy mode only, executes the idempotent migration SQL against the production database before rolling out the application.

## Linux VPS deployment target

The deploy step calls:

```bash
scripts/deploy/production-vps-deploy.sh
```

In `validate-only` mode, the workflow builds and packages everything but does not connect to the VPS and does not modify the production database. In `deploy` mode, the deployment gate first executes the generated migration SQL successfully, then the VPS script:

1. Packages the existing backend, frontend, and migration artifacts.
2. Adds the production Docker Compose files.
3. Creates temporary runtime config files from GitHub secrets and variables without printing secret values.
4. Uploads the package to the VPS with `scp`.
5. Extracts it under `$VPS_DEPLOYMENT_DIRECTORY/releases/<commit-sha>`.
6. Updates `$VPS_DEPLOYMENT_DIRECTORY/current` to the new release.
7. Runs:

   ```bash
   docker compose -f docker-compose.production.yml --env-file .env.compose up -d --build --remove-orphans
   ```

8. Records the deployed revision in `$VPS_DEPLOYMENT_DIRECTORY/.deployed-revision`.

The normal deployment command does not run `docker compose down -v`; Docker volumes for public/private local uploads are preserved across deployments.

This repository still does not provision AWS, Azure, Kubernetes, Terraform, DNS, database servers, buckets, or CDN infrastructure.

### VPS prerequisites

On the server:

1. Create a deploy user, for example `deploy`.
2. Install Docker Engine and the Docker Compose plugin.
3. Add the deploy user to the `docker` group, or configure equivalent least-privilege Docker access.
4. Create the deployment directory:

   ```bash
   sudo mkdir -p /opt/daily-rental-homes
   sudo chown deploy:deploy /opt/daily-rental-homes
   ```

5. Add the GitHub Actions public SSH key to the deploy user's `~/.ssh/authorized_keys`.
6. Capture the server host key with a trusted channel and store it in `VPS_SSH_KNOWN_HOSTS`. Do not disable host verification silently.

## Migration handling

The workflow generates an idempotent SQL migration script and uploads it as an artifact. In `deploy` mode, before the VPS rollout starts, the workflow verifies that the generated SQL artifact exists, is non-empty, and executes it against `PRODUCTION_DATABASE_CONNECTION_STRING`.

If migration execution fails, the workflow fails immediately and the VPS deployment step is not run. Secret values and the database connection string are not printed.

Recommended order for an operator-controlled deployment:

1. Review the generated migration SQL artifact.
2. Back up the production database, especially before risky schema changes or large data migrations.
3. Run the manual workflow in `deploy` mode.
4. Confirm the migration step succeeds.
5. Confirm the VPS Docker Compose rollout succeeds.
6. Run post-deployment verification.

Automatic database downgrade is not implemented. Rollback after a migration may require a forward-fix migration or database restore depending on the change.

## Post-deployment verification

When running deploy mode, provide:

- `backend_base_url`
- optionally `frontend_base_url`

Backend verification checks:

- `/health`
- `/health/ready`
- `/api/rental-homes`

Frontend verification checks the configured frontend base URL if provided. The workflow does not claim verification passed when URLs are missing.

## Rollback

To roll back application code:

1. Identify the previous known-good commit SHA from GitHub Actions deployment history.
2. Prefer re-running the manual workflow from that revision so artifacts match the source revision.
3. If the previous release directory still exists on the VPS, a manual operator rollback can repoint `current` and restart Compose:

   ```bash
   cd /opt/daily-rental-homes
   ln -sfn /opt/daily-rental-homes/releases/<previous-sha> current
   cd current
   docker compose -f docker-compose.production.yml --env-file .env.compose up -d --build --remove-orphans
   printf '%s\n' '<previous-sha>' > /opt/daily-rental-homes/.deployed-revision
   ```

4. Verify backend health/readiness and the public rental-home listing.

Database rollback is not automatic. If a migration has been applied, review whether the previous application revision is compatible with the current database schema. Prefer forward fixes unless a tested restore/downgrade plan exists.

File/storage compatibility notes:

- Public rental-home media URLs should remain stable.
- Private deposit receipts must remain private and accessible only through the authorized API endpoint.
- S3 bucket prefixes and permissions must be kept compatible with the deployed revision.
