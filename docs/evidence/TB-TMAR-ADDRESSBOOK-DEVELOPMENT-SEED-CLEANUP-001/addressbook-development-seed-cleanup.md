# AddressBook Development Seed Cleanup (Host Evacuation)

Task: `TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001`
Parent: `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002` at `37f4d7507eb7e884f8f8394be9f985e6cdb75bf5`
Recovery SoT checkpoint: `bf57df9053b32e67efa27a13ea2fcdd16ddfbb57`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — single-objective seed evacuation.
Recovery honesty: after this slice Host AddressBook residue is **ZERO**, but AddressBook is still **NOT** `STRUCTURE_CERTIFIED`; root/capability cleanup may remain and ARCH-COMPLETE-002 certification is still pending. **NOT** `COMPLETE_REFERENCE_PATTERN`.

## 1. Old Host path → new Infrastructure path

| | Path | Namespace |
| --- | --- | --- |
| Before | `src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs` | `Tooba.Host.AddressBook` |
| After | `src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Development/AddressBookDevelopmentSeed.cs` | `Tooba.AddressBook.Infrastructure.Development` |

The Host file was **deleted**. `src/backend/Host/Tooba.Host/AddressBook/` is now **absent** (folder residue = **ZERO**).

## 2. Guest actor authority

The old dependency `Tooba.Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId` was replaced with the canonical contract authority `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`. Both resolve to the same value (`aaaaaaaa-aaaa-4aaa-8aaa-000000000009`), so seeded ownership is unchanged. A `grep` for `Order.Application` / `Tooba.Host` across the whole `Tooba.AddressBook.Infrastructure` project returns **no matches**; the moved seed has no `Order.Application` reference.

## 3. Infrastructure project reference

`Tooba.AddressBook.Infrastructure.csproj` gained exactly one reference: `..\..\Order\Tooba.Order.Contracts\Tooba.Order.Contracts.csproj`. Nothing broader was added and no Host reference exists. The seed also uses `Microsoft.Extensions.DependencyInjection` for `GetRequiredService` (explicit using added so `ImplicitUsings` differences cannot break the build).

## 4. Bootstrap call sites

Only `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceDevelopmentBootstrap.cs` was touched. The `using Tooba.Host.AddressBook;` directive was repointed to `using Tooba.AddressBook.Infrastructure.Development;` (one using, file otherwise clean). Both pre-existing call sites are preserved with identical call count, argument shape and ordering relative to Reviews/Wishlist/CustomerProfile seeds:

| # | Line | Call |
| --- | --- | --- |
| 1 | 161 | `await AddressBookDevelopmentSeed.ApplyAsync(provider);` (development bootstrap path) |
| 2 | 287 | `await AddressBookDevelopmentSeed.ApplyAsync(provider, cancellation);` (reset/reseed path) |

Still exactly **2** calls; the `provider`/`cancellation` argument and cancellation behaviour are unchanged.

## 5. Seed semantics / idempotency preserved

- `DefaultAddressId` = `aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1` — unchanged.
- `AlternateAddressId` = `aaaaaaaa-aaaa-4aaa-8aaa-0000000000a2` — unchanged.
- Deterministic `createdAt` = `2026-08-25 14:00:00 +00:00`, with the alternate at `createdAt.AddMinutes(5)` — unchanged.
- Same two `CustomerAddress.Create` calls with identical field values, labels, `isDefault` flags and explicit address ids.
- Same idempotency `AnyAsync(x => x.AddressId == …)` guards per row.
- Same single `SaveChangesAsync(cancellationToken)` at the end; still not invoked in Production.

## 6. Host AddressBook residue

**ZERO.** Both Host AddressBook files are gone: `AddressBookEndpoints.cs` in the previous slice and `AddressBookDevelopmentSeed.cs` here. The folder `src/backend/Host/Tooba.Host/AddressBook/` no longer exists.

## 7. Focused baseline / guard update

- `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-host-write-files.json` — removed the `AddressBook/AddressBookDevelopmentSeed.cs` entry (baseline shrink, reflecting the host-write site that no longer exists). No unrelated baselines touched.
- `src/backend/Host/Tooba.Host.Tests/AddressBookFoundationTests.cs` — the seed using was repointed to `Tooba.AddressBook.Infrastructure.Development`, and the `Development_seed_is_idempotent` expected actor now reads `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` so the assertion no longer depends on the `Order.Application` projection.

## 8. Focused build and test results

| Command | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Infrastructure.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | **Build succeeded**, 0 errors, 11 warnings (all pre-existing) |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests` | **Passed! Failed: 0, Passed: 8, Skipped: 0, Total: 8** |

## 9. Explicitly untouched

AddressBook CQRS requests/handlers/validators, `AddressBook.Endpoints`, `AddressBookEndpointModule`, the AddressBook actor seam, AddressBook root structure, `AddressBookContracts.cs`, `AddressBookDirectory.cs`, `AddressBookModule.cs`, `CustomerAddress.cs`, certification manifests, frontend, and checkout logic. No HTTP endpoint was modified — Host AddressBook HTTP ownership was already ZERO.

## 10. Production code scope

1. `src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Development/AddressBookDevelopmentSeed.cs` — **new** (moved seed, `Order.Contracts` guest actor).
2. `src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj` — added the `Tooba.Order.Contracts` reference.
3. `src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs` — **deleted**.
4. `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceDevelopmentBootstrap.cs` — repointed one using; two call sites unchanged.

## 11. Test-code scope

5. `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-host-write-files.json` — removed the moved Host-write entry.
6. `src/backend/Host/Tooba.Host.Tests/AddressBookFoundationTests.cs` — seed using repointed; idempotency test actor switched to the contract constant.

Test-only, evidence not navigation; no other test scope broadened.

## 12. Residual defects

None introduced. Still deferred: AddressBook root/capability structure cleanup, the Host `Program.cs` composition edges to AddressBook.Application/Infrastructure, and final ARCH-COMPLETE-002 certification. The durable guards still carry pre-slice SoT expectations (`TmarDurableGuardTests` asserts `nextTask`/`currentHostEvacuation.currentTask` as the inventory task); that SoT advance is an Architect-owned commit and was not rewritten here.

## 13. Exact next recommended task

**A bounded AddressBook structure/pre-cert audit** — e.g. `TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001` — to classify the current AddressBook capability/root folder structure against `APPLICATION_CAPABILITY_FOLDERS` / `ENDPOINTS_CAPABILITY_FOLDERS` / `INFRASTRUCTURE_CAPABILITY_FOLDERS` and the path↔namespace exactness rule, enumerate endpoint-reachable CQRS requests with validator coverage, and confirm `Host → AddressBook` composition edges, producing a pre-certification gap list. No root refactor, no certification, no endpoint/CQRS change in that audit.
