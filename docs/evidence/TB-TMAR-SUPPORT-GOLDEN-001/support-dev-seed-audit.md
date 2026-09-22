# Support Dev Seed Audit

## SupportDevelopmentSeedHost
- Path: `Host/Tooba.Host/Support/SupportDevelopmentSeedHost.cs`
- Role: Development-only composition bootstrap
- Touches: SupportDbContext migrate, AccessControl bootstrap tuples, `SupportDevelopmentSeed.ApplyAsync`
- Not production HTTP / not runtime use-case

## Architecture guard
- Explicitly allowlisted for Host SupportDbContext references
- ProductWorkspaceDevelopmentBootstrap also allowlisted for migrate-only

## Demo preview seam
- Application: `ISupportDemoPreviewPort` + `SupportDemoSnapshotDto`
- Infrastructure: `SupportDemoSnapshotStore` + `SupportDemoPreviewAdapter`
- Endpoints: MediatR `GetSupportDemoPreviewQuery` (no Infrastructure reference)
- Behavior: non-Development → 404; not-ready → 503 + `support.demo.not_ready`

## Verdict
**Support-Dev-Seed-Audit: ALLOWED_DEV_BOOTSTRAP_ONLY**
