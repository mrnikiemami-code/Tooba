# Architect review — TB239 — 2026-10-28

Reviewed main8f5126bfe0fb716d8d9c52185f19861ec57a6f65 (docs-only), actual Host Development bootstrap files,
Program startup gate, HostAdminPanelAccess, Identity authentication Contracts and actual seller-dev bootstrap.
Root did not execute the Windows requests. Verdict: ACCEPT REAL MARKETPLACE BOOT/EXPECTED DENIAL ONLY;
remaining owner checkout/user outcome NOT delivered. This is a concrete development commissioning gap, not a
reason to weaken authorization or another unexplained shipping algorithm defect.

## Actual source facts
Program calls MarketplaceDevelopmentBootstrap only under IsDevelopment and registry.Edition=Marketplace.
It assigns Marketplace CommerceContext(tenant:null), explicitly migrates a subset of module contexts, and calls
seller/admin dev bootstraps. Official MigrationRunner filling remaining modules is the proper route; no need
to rewrite old migrations or duplicate all migrations into Host.
MarketplaceAdminDevBootstrap writes tenant Member tuple for fixed actor cccccccc-cccc-4ccc-8ccc-000000000001
but never creates/resolves a real Identity principal. HostAdminPanelAccess requires a real current session/actor
authorized against existing Development Marketplace platform tenant. Normal seller403 is CORRECT and retained.
Identity.Contracts.IIdentityAuthenticationService already provides RegisterAsync, FindUserIdByIdentifierAsync
and normal authentication; no foreign EF access/new Identity primitive needed for commissioning.
SellerDevContextBootstrap.EnsureAsync creates/resolves the two actual Identity users, then returns without
membership/snapshot when real Party organizations do not exist. After official data seed exists it can reconcile
real actor->real seller memberships through Party.Contracts + existing tuple writer.
MarketplaceSellerDevBootstrap separately publishes a fixed legacy pair (same default actor for A/B); do not use
that synthetic pair as proof of two real seller principals. Use actual Identity user ids and Party relationships.

## Scoped worker evidence
Task-owned actual Marketplace5190 /tooba_marketplace_tb239 /tenantNULL, home500 missingCatalog then normalrunner
home200 empty data; normal sellerlogin200 and protected seed403. Retain this target, do not repeat bootstrap/
driver work. No source change; Code-SHA==Base is correct. No admin-authoring/mixed-owner/coupon/Order proof.
238 invalid-read/tolerance and22001 column attribution corrections are retained. Unknown correlation failure
remains unclassified, not cured by UI ref. No paid/PSP/multi-owner/Visual ACCEPT/fullrelease claim.
42min vs35 and >1 correction NONCOMPLIANT, not a timing certificate. All3 recovery current worker TOPs present.

## Explicit architectural decision / next task authorization
Authorize ONE opt-in, Development+Marketplace-only binding of a dedicated normally registered DEV administrator
to the ALREADY EXISTING Marketplace admin relation. This is approved commissioning, not forbidden invented role
or permission bypass. Use actual Identity.Contracts lookup and existing IAuthorizationTupleWriter, no user-supplied
HTTP owner/role, SQL grant, fake header/JWT, wildcard/admin-everyone, new endpoint/permission/production policy.
Register that dedicated account via normal existing public registration first; supply IDENTIFIER-only opt-in
configuration privately/local to the task runtime. No hardcoded admin password, no automatic promotion of current
seller/first user. Require explicit identifier lookup success, exact Marketplace/context target; no fallback to
an unrelated fixed id. Repeated provisioning idempotent, not credential reset. Production/SingleStore/missing/
disabled configuration cannot activate this additional binding. Preserve legacy behavior where outside this opt-in.
Then normal admin login and protected official seed/authoring APIs MUST work, while existing seller stays403.
Reconcile actual SellerA/B after official Party seed through existing capability, then continue real Marketplace
buyer flow. No new security/Identity/framework or shipping policy survey. Original userreview runtimes untouched.
