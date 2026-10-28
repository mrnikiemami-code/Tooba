# RESULT — TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1
Parent-Task: TB-SHIPPING-MARKETPLACE-FIRST-LIVE-ACCEPTANCE-239
Channel: tooba-main
Worker-Id: tooba-worker-01
Bridge-Task-Row: 210643f4-ada5-4997-ae78-101494d2aa4c
Status: PARTIAL
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: fe963e325086e1da74093ee671f9f65eefe5085d
Base-SHA: fe963e325086e1da74093ee671f9f65eefe5085d

## 1. THE DECIDED GAP IS CLOSED — a real registered Development administrator now reaches the
existing Marketplace platform admin relation, and the seller still does not

All of the following is real normal HTTP against the SAME task Marketplace target
`http://127.0.0.1:5190` / `tooba_marketplace_tb239` / `Edition=Marketplace` / `Tenant=NULL` that
239 booted (no reset, no new database, no relabel).

| step | order | real receipt |
|---|---|---|
| dedicated DEV admin registered through the normal public API | 1 | `POST /v1/auth/register` **200** `{"userId":"01a1818b-5257-7000-bada-84233a2b21fb"}` (random private password, `@example.invalid`, file only under `D:\temp\tb239r1\admin.txt`, never in git/result/screens) |
| BEFORE the opt-in binding the same real account is powerless | 2 | `POST /v1/auth/login` **200**, then `POST /v1/admin/catalog/demo/reset-and-seed` **403** `errorCode=admin.authorization.denied`, `traceId=dc4bdc78298b4553a479a5481ed71eb5` |
| host restarted (source changed) with private opt-in env | 3 | new binary `D:\temp\tb239r1-build\out\Tooba.Host.dll` SHA256 prefix `B31E1048BA6AB101`, Development, `PID 27428`, `/health` **200** |
| AFTER the binding the same account authenticates AND the protected official seed runs | 4 | login **200**; `POST /v1/admin/catalog/demo/reset-and-seed` executes for **890 620 ms** and `Request finished … - 200` (566 products, real catalogue data) |
| an existing real seller keeps the exact 403 | 5 | `seller-actor-a@tooba.local` `POST /v1/auth/login` **200**, then the SAME admin route **403** `admin.authorization.denied`, `traceId=2a7401a01661dccb7f163a3b29e503ae` |
| a THIRD normally registered account is still denied | 6 | `tb239r1-bystander@example.invalid` **200** register/login, then `GET /v1/admin/catalog/demo/status` **403** `admin.authorization.denied` |
| idempotent re-provisioning after a real process restart | 7 | `PID 27428 → 22832`; the authorization store is `InMemory` and is EMPTY after restart, so the 200 below can only come from this seam writing again: admin `GET /v1/admin/catalog/demo/status` **200** `{"rootsDemo":15,"categoriesDemo":116,"brandsDemo":22,"tagsDemo":36,"attributesDemo":41,"productsTotal":566,"productsDraft":566,"allowResetAndSeed":true}`; seller `GET` on the same route **403** |

Tuple actually written (one, no wildcard, no invented role):

```
subject  = user:01a1818b-5257-7000-bada-84233a2b21fb   (the real generated Identity user id)
resource = tenant:marketplace-platform                 (HostAdminPanelAccess.MarketplacePlatformTenantId)
relation = member                                      (AuthorizationRelations.Member, operation Touch)
```

`DefaultAdminActor` legacy tuple is untouched, no credential was created/reset by Host, and no
seller/request actor/first-user was promoted.

## 2. Source seam (Host STARTUP COMPOSITION only)

| path | change |
|---|---|
| `src/backend/Host/Tooba.Host/Configuration/MarketplaceAdminDevOptions.cs` | NEW. `Tooba:Development:MarketplaceAdmin:{Enabled,Identifier}`; `Enabled` default `false`, no password field at all |
| `src/backend/Host/Tooba.Host/Development/MarketplaceAdminDevBootstrap.cs` | extended with `ProvisionConfiguredAsync(provider, IHostEnvironment, ct)`. Refuses unless real `IsDevelopment()` **and** `ControlPlaneRegistry.Edition == Marketplace` **and** a marketplace connection reference exists **and** explicit opt-in **and** a non-blank identifier resolves via `IIdentityAuthenticationService.FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, …)`; writes through the existing `IAuthorizationTupleWriter`. Unknown/blank identifier returns without fallback |
| `src/backend/Host/Tooba.Host/Development/MarketplaceDevelopmentBootstrap.cs` | `ApplyAsync` takes `IHostEnvironment` and calls the new seam AFTER the unchanged legacy write |
| `src/backend/Host/Tooba.Host/Program.cs` | binds the options section; passes `app.Environment` to the existing Development+Marketplace branch |
| `src/backend/Host/Tooba.Host.Tests/Development/MarketplaceAdminDevBootstrapTests.cs` | NEW focused facts |

No Identity/AccessControl EF, `DbContext`/`SaveChanges`/`MigrateAsync` in the bootstrap, no raw SQL,
no new permission/role catalogue, no new endpoint, no service-locator workaround, no Production or
SingleStore behaviour change, no shipping/order/pricing/tax/calendar edit, no migration/schema/
baseline/history change.

## 3. Focused provisioning facts (5 new + 5 affected guard, 10/10)

`dotnet test … --filter "MarketplaceAdminDevBootstrapTests|HostDevelopmentAmcGuardTests"` → **10/10 passed**;
`Tooba.Host.Tests` builds **0 errors**.

- enabled + Development + Marketplace + resolvable identifier ⇒ exactly ONE write with the resolved
  real user id, `tenant` / `marketplace-platform`, `member`;
- repeat call ⇒ the same single subject twice, never a second/duplicate principal concept;
- unknown / blank / whitespace identifier ⇒ **zero** writes and the directory is never even asked for
  a blank value by the writer path;
- disabled, `Production`, `Staging` and `SingleStore` ⇒ **zero** writes and the resolver is never called;
- checked-in `appsettings*.json` contains no `MarketplaceAdmin` key (seam can never be on by default).

The main gate remains the real HTTP control in §1 (worker facts are not a substitute and are not
presented as production security proof).

## 4. Where the owner flow actually stops — CONCRETE, NOT a permission problem

The authorized tick was made and the official seed ran. After it, the task DB contains:

```
catalog.products = 566   (all Draft)
party.parties    = 0
offer.offers     = 0
```

Consequences measured on the same live target:

- `GET /v1/storefront/home` **200** (19 566 bytes — the storefront renders, but with no sellable offers);
- seller `GET /v1/seller/offers` **403** `seller.authorization.denied` — no seller business scope exists;
- therefore no `Platform` / `SellerA` / `SellerB` publication, no real offer responsibility/type, no stock,
  no shipping draft/rates/week/publication, no cart line, no projection, no selection echoes, no
  payment preview and no Order could be exercised in this slice.

Root cause is source-level and exact: the Marketplace Development path never invokes the Party/offer
seeds that the SingleStore path uses — `SettingsFoundationDevelopmentSeedHost`
(`PartyOrganizationProfileDevelopmentSeed`) and `IWorkspaceDemoSeed`/`WorkspaceDemoMarketplaceSeed`
are called only from `DevelopmentSchemaMigrator`, which `MarketplaceDevelopmentBootstrap` does not run,
and **no HTTP route creates seller organizations** (`Party` exposes only `GET /v1/admin/sellers` and
`POST /v1/admin/sellers/query`). `SellerDevContextBootstrap.EnsureAsync` therefore reaches its
documented early return with `sellerA is null || sellerB is null`.

This is data/bootstrap commissioning, not authorization. Nothing was invented to get past it: no direct
business SQL seed, no fake Party id, no seller promotion, no fake `X-Tooba-Seller-Party-Id`.

## 5. Runtime / preservation ledger

| item | value |
|---|---|
| target | `http://127.0.0.1:5190`, `Edition=Marketplace`, `DeploymentId=tb239-marketplace`, `Tenant=NULL` |
| DB | `tooba_marketplace_tb239` on `127.0.0.1:5432` (task-owned, already migrated in 239, NOT reset) |
| binary | `D:\temp\tb239r1-build\out` (ONE publish from this task's source), SHA256 prefix `B31E1048BA6AB101` |
| process | restarts only of the verified task 5190 listener (`40544 → 27428 → 22832`); final PID 22832 left running |
| local task env | gates `Enabled/SelectionEnabled/CheckoutEnabled=true`, `AvailableDateCount=7`, `ShippingOneLevelCatalog.Enabled=true`, plus `Tooba__Development__MarketplaceAdmin__{Enabled,Identifier}` — never checked in |
| user work | original `3000`/`5088`, handed-off `3100`/`5188` and `D:\temp\tb238-handoff-chrome` were never enumerated, read, reloaded, automated, closed or source-edited |
| 238 defect | unknown `22001` correlation failure retained as unclassified; no schema fix invented, no Outbox work |
| other | TB185 paused/default-OFF/PendingAllocation unchanged; checked-in shipping gates remain OFF; 227/225-R1 losses retained |

No payment/PSP/real money/production activation. No new browser automation was needed because the
blocker is measured on real APIs.

## 6. Timing / compliance

Task claimed `07:58:54Z`; this result ≈`08:45Z` — well past HARD35, recorded NONCOMPLIANT (the official
seed alone is a 890 s synchronous request). One in-task correction was needed (the `Assert.Equal` on the
non-`IEquatable` `AuthorizationSubject` class and the repo-root marker), beyond the single-round budget —
recorded, not hidden.

## 7. Remaining step (for the next bounded task)

Wire an existing, module-owned Development seed for Marketplace so that real Party organizations +
real offers + real stock exist (reuse `PartyOrganizationProfileDevelopmentSeed` /
`WorkspaceDemoMarketplaceSeed` / `SellerDevContextBootstrap.EnsureAsync` through their existing
Contracts inside the Marketplace Development branch — or an authorized module-owned seed endpoint),
then re-run the mixed-owner shipping flow: Platform + SellerA + SellerB publications on one binding,
per-group method/date/money evidence, coupon on one group only, `paymentpreview`, real StartPayment,
Order evidence, plus the existing Marketplace UI smoke and 390/360 payment captures.
