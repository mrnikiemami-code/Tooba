# Support Host Authority Audit

## Removed from Host
- `Support/SupportEndpoints.cs` (HTTP + business + manual errors + AccessControl inline)

## Remaining Host Support surface
- `Support/SupportDevelopmentSeedHost.cs` — Development seed/bootstrap only
- `Program.cs`: `AddSupportEndpointPresentation()`, authorizer DI, CQRS assembly, `MapSupportEndpoints()`
- Host authorizer adapters under Customer/Seller/Admin

## Host business authority
- No `ISupportDirectory` in Host HTTP
- No Support presentation/error mapping in Host
- No SupportDbContext production authority except Dev bootstrap allowlist:
  - `SupportDevelopmentSeedHost.cs`
  - `ProductWorkspaceDevelopmentBootstrap.cs` (migrate)
  - Program / ModuleMigrationRegistry composition

## Verdict
**Support-Host-Business-Authority: NONE**
**Support-Host-DbAuthority: NONE_EXCEPT_DEV_BOOTSTRAP_ALLOWLIST**
