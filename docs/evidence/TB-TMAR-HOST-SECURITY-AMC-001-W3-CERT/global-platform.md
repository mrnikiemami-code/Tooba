# global-platform — TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT

## AuthSecurityHostOptions (+ validator)

- Host-owned configuration only (`AuthSecurity` section)
- Fail-fast `IValidateOptions` registered
- No module business policy / secrets hard-coded / foreign App|Infra|Domain
- No user-facing runtime prose
- Classification: KEEP_AS_GLOBAL_HOST_SECURITY_PLATFORM → HOST_SECURITY_GLOBAL_PLATFORM_CERTIFIED

## SecurityHeadersMiddleware

- Host HTTP platform middleware only
- Bounded security headers; Production HSTS deliberate
- No module business ownership / sensitive logging / custom ActivitySource|Meter|correlation
- Classification: KEEP_AS_GLOBAL_HOST_SECURITY_PLATFORM → HOST_SECURITY_GLOBAL_PLATFORM_CERTIFIED
