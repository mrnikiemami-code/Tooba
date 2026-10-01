# keep-as-global-justification — TB-TMAR-HOST-SECURITY-AMC-001

## KEEP_AS_GLOBAL_HOST_SECURITY_PLATFORM (2 files)

### AuthSecurityHostOptions.cs

- Host-only configuration binding for auth/security HTTP surface (body size, CORS, rate-limit related options consumed by Program).
- Not a module policy; not persistable business rule.
- Correct ownership: Host composition root options.

### SecurityHeadersMiddleware.cs

- Cross-cutting HTTP response headers for the Host pipeline.
- Must remain in Host; modules must not own ASP.NET middleware registration ownership for the whole app.

## KEEP_AS_THIN_HOST_SECURITY_ADAPTER (17 files)

All Checkout / Payment / Seller adapters:

- Implement module **Contracts / Endpoints** ports only.
- Compose session + BuildingBlocks authorization decisions.
- Contain **no** Domain/Application/Infrastructure/DbContext.
- Exist so modules stay free of Host session/header plumbing.

## Not HOST_ZERO

Removing the folder would force modules to take Host HTTP/session concerns or duplicate adapters elsewhere. Intentional thin Host Security platform is the correct end-state shape after hygiene + CERT — not empty Host/Security.
