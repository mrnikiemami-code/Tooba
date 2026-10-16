TB-SHIPPING-ONE-LEVEL-TABS-116 — FOCUSED TESTS / BUILD / GUARD RECORD
====================================================================

New focused test class
----------------------
src/backend/Host/Tooba.Host.Tests/CatalogLegacyBootstrapsCommerceContextTests.cs (5 facts + 1 theory=2 cases -> 7 test cases)

1. Gateway_assigns_trusted_context_inside_the_same_scope_before_the_seed_resolves_its_db_dependency
   A real `ServiceCollection` + `BuildServiceProvider(ValidateScopes = true, ValidateOnBuild = true)`
   mirrors the Program.cs Development registrations (HttpCommerceContextAccessor scoped;
   ICurrentCommerceContext / ICurrentEdition / ICurrentTenant / ICommerceContextAssigner all resolving
   to it) plus a singleton `ControlPlaneRegistry` with a SingleStore edition and an Active `store-alpha`.
   Inside the seed delegate it resolves the SCOPED `ICurrentCommerceContext` and calls the REAL
   production `ToobaNpgsql.ResolveForContext` — the exact function that threw in TB-115 — then asserts:
   the seed ran; `Current` is non-null; edition SingleStore with the registry deployment id; tenant is
   Active with `DevelopmentTenantCommerceContext.DevelopmentTenantId`; the tenant's own connection
   reference; `DatabaseConnectionReference` equals the tenant's reference (no marketplace fallback); and
   the trace label is the legacy-catalog label.
2. Gateway_is_fail_closed_when_the_development_tenant_is_not_active (Disabled, Suspended)
   The seed must NOT run and NO CommerceContext may be observable in the scope.
3. Gateway_is_fail_closed_when_the_development_tenant_is_absent
   No tenant at all -> the seed must NOT run.
4. Unassigned_scope_still_reproduces_the_tb115_platform_edition_unconfigured_failure
   Negative control using the real production resolve path: the SAME resolve against an unassigned scope
   still throws `PlatformHttpException` with StatusCode 503 and ErrorCode `platform.edition.unconfigured`
   — i.e. test 1 is not vacuous and the assignment is what changed the outcome.
5. Gateway_uses_a_fresh_scope_per_invocation_and_disposes_it_on_success_and_throw
   Two successive invocations get different scoped probe instances (fresh scope each time, no leakage);
   every scope is disposed after the call; and when the seed throws, the exception propagates while the
   scope still disposes.
6. Program_uses_the_canonical_gateway_for_the_legacy_catalog_bootstraps
   Program.cs must contain `DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync` and must NOT
   build a CommerceContext by hand (`new CommerceContext(`); the gateway itself must contain
   `CreateAsyncScope`, `ICommerceContextAssigner` and the literal `store-alpha` tenant id — which is what
   makes the assignment and the seed share one scoped lifetime in the real Host.

Updated test
------------
src/backend/Host/Tooba.Host.Tests/CatalogLegacyBootstrapsDiScopeTests.cs —
`Program_hands_the_legacy_catalog_bootstraps_a_scoped_provider` now asserts the seed seam is reachable
only as the delegate passed to the gateway (and never receives `app.Services`), instead of asserting the
scope was created inline in Program.cs. All other TB-114 assertions remain.

Run (focused only)
------------------
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "FullyQualifiedName~CatalogLegacyBootstrapsCommerceContextTests|FullyQualifiedName~CatalogLegacyBootstrapsDiScopeTests"
-> Passed!  Failed: 0, Passed: 12, Skipped: 0, Total: 12

Relevant existing Host Development / commerce-context guards (no broad suite run)
--------------------------------------------------------------------------------
dotnet test ... --filter "HostDevelopmentAmcGuardTests|HostDevelopmentMigrationSeamGuardTests|HostDevelopmentEnricherClosureGuardTests|HostAdminAmcW34TemplateSeedsGuardTests|HostAdminAmcLandingPageSeedGuardTests|HostAdminAmcStoreMenuGuardTests|ContentDevelopmentSeedHostSourceTests|HostSellerAmcR5GuardTests|HostStorefrontAmcR4GuardTests|HostGridAmcR5GuardTests|CatalogLegacyBootstraps*"
-> Failed: 4, Passed: 55, Total: 59

All 4 failures are PRE-EXISTING and unrelated to TB-116 (no `src/` path they read is touched by this
task):
  * HostAdminAmcLandingPageSeedGuardTests.Host_Admin_count_15_StoreAppearance_evacuated
  * HostAdminAmcStoreMenuGuardTests.Host_Admin_count_15_StoreAppearance_evacuated
  * HostAdminAmcW34TemplateSeedsGuardTests.Host_Admin_count_15_StoreAppearance_and_HoldPolicy_evacuated
      -> all three assert 15 `*.cs` files under `src/backend/Host/Tooba.Host/Admin` while 17 exist;
         `git ls-tree` reports 17 at the expected base SHA and at HEAD, and TB-114/TB-116 touched
         nothing in that folder.
  * HostSellerAmcR5GuardTests.Party_development_seam_exposes_no_party_persistence_or_domain_types
      -> `FileNotFoundException` for `Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs`;
         the file actually lives at `Party/Tooba.Party.Contracts/Ports/IPartyDevelopmentSeedGateway.cs`
         (a stale guard path). Verified absent at the base SHA 2ea499e98 too:
         `git cat-file -e 2ea499e98:src/backend/Modules/Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs`
         -> "does not exist in 2ea499e98". No Party path is touched by this task
         (`git diff --name-only 2ea499e98..HEAD -- src/backend/Modules/Party` is empty).

Every other guard passes, including all Development-folder allowlist guards
(HostGridAmcR5 / HostSellerAmcR5 / HostStorefrontAmcR4 / HostDevelopmentAmc) and
HostDevelopmentEnricherClosureGuardTests.

Build
-----
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug -> 0 Error(s) (1 pre-existing
warning CS0105 in AdminDevActorBootstrap.cs, untouched).

NOT run on purpose: any isolated/live Host start, scratch database, demo reset/seed, the full legacy
seed chain, broad Host suite, frontend build.
