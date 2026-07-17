# Manual production deployment

Production deployment is intentionally manual. The workflow is designed to validate and package the application now, while failing safely until a real hosting target is configured.

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
5. Add production secrets as environment or repository secrets.

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

Required only for deploy mode after a real target is implemented:

- `PRODUCTION_DEPLOYMENT_TARGET_CONFIGURED=true`
- Provider-specific deployment credentials chosen by the operator

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

## Deployment target status

No fixed production hosting provider is configured yet. The deploy step currently calls:

```bash
scripts/deploy/production-deploy-placeholder.sh
```

In `validate-only` mode, the workflow builds and packages everything but does not deploy. In `deploy` mode, the placeholder fails clearly until it is replaced or adapted for the chosen hosting provider.

This repository does not currently provision AWS, Azure, Kubernetes, Terraform, DNS, database servers, buckets, or CDN infrastructure.

## Migration handling

The workflow generates an idempotent SQL migration script and uploads it as an artifact. It does not automatically mutate the production database.

Recommended order for an operator-controlled deployment:

1. Review the generated migration SQL artifact.
2. Back up the production database.
3. Apply the migration using the approved production database process.
4. Deploy the backend revision that matches the migration artifact.
5. Deploy the frontend bundle.
6. Run post-deployment verification.

Automatic database downgrade is not implemented. Rollback after a migration may require a forward-fix migration or database restore depending on the change.

## Post-deployment verification

When deploy mode is implemented, provide:

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
2. Re-run the manual workflow from that revision or redeploy the previously stored artifact according to the hosting provider's process.
3. Verify backend health/readiness and the public rental-home listing.

Database rollback is not automatic. If a migration has been applied, review whether the previous application revision is compatible with the current database schema. Prefer forward fixes unless a tested restore/downgrade plan exists.

File/storage compatibility notes:

- Public rental-home media URLs should remain stable.
- Private deposit receipts must remain private and accessible only through the authorized API endpoint.
- S3 bucket prefixes and permissions must be kept compatible with the deployed revision.
