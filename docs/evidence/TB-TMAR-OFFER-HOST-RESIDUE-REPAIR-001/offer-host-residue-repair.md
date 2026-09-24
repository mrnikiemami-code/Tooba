# TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001 — Offer business policy left Host

Task: `TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001`
Parent: `TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001` (ARCHITECT-ACCEPTED at `7a0b36061db85fc4ba57e117bcf50d9ee390fa73`)
Channel: `tooba-main` · Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`

## Objective

Remove Offer-owned business selection policy and hidden Offer type aliases from Host while preserving storefront behavior exactly. No structure certification, no validators, no `Contracts/Errors` namespace repair, no Payment/Checkout W6/SharedDB/frontend scope.

## Boundary decision

```text
Host Storefront composition -> Tooba.Offer.Contracts port -> Offer.Application implementation
```

Host never calls the Offer Application implementation directly.

## Before / after residue

| Item | Before | After |
| --- | --- | --- |
| `Host/Tooba.Host/Storefront/StorefrontPrimaryOfferResolver.cs` | Host-owned selection policy | **deleted** |
| `Host/Tooba.Host/OfferGlobalUsings.cs` | global aliases `OfferStatus`, `SalesChannel` | **deleted** |
| `Host/Tooba.Host/.tmp-t014-test-out/` | 9 stale `Tooba.Offer.*` build artifacts | **deleted** |
| `Host/Tooba.Host/Seller/HostOfferSellerAuthorizer.cs` | thin security adapter | **kept, unchanged (thin)** |

## Offer-owned selection boundary

- `src/backend/Modules/Offer/Tooba.Offer.Contracts/Dtos/OfferSelectionCandidate.cs` — `Tooba.Offer.Contracts.Dtos`, five selection fields only: `OfferId`, `CatalogVariantId`, `SellerPartyId`, `AmountExclusiveOfTax`, `AvailableUnits`. No Pricing/Inventory/Application/Infrastructure types.
- `src/backend/Modules/Offer/Tooba.Offer.Contracts/Ports/IPrimaryOfferSelectionPolicy.cs` — `Tooba.Offer.Contracts.Ports`, `Resolve(IReadOnlyList<OfferSelectionCandidate>)` and `ResolveVariantId(Guid?, IReadOnlyCollection<Guid>, IReadOnlyList<OfferSelectionCandidate>)`.
- `src/backend/Modules/Offer/Tooba.Offer.Application/Policies/PrimaryOfferSelectionPolicy.cs` — `Tooba.Offer.Application.Policies`, implements the port with the exact former semantics:
  - empty ⇒ `null`;
  - `OrderByDescending(AvailableUnits > 0)`;
  - then `ThenBy(AmountExclusiveOfTax)`;
  - then `ThenBy(OfferId)`;
  - `ResolveVariantId` keeps the requested variant only when it belongs to the product and has a candidate, otherwise falls back to `Resolve(...)?.CatalogVariantId`.
  - No DbContext, no Host dependency, no lookup, no time/id/randomness.
- Registration: `Tooba.Offer.Infrastructure/DependencyInjection/OfferModule.cs` registers `AddSingleton<IPrimaryOfferSelectionPolicy, PrimaryOfferSelectionPolicy>()` because the implementation is pure and stateless.

## The 3 Storefront caller changes

`src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs`:

1. constructor takes `IPrimaryOfferSelectionPolicy` (Contracts port) and stores it as `_primaryOffers`; the file's only Offer imports are `Tooba.Offer.Contracts.Dtos` and `Tooba.Offer.Contracts.Ports`, and it contains no `Tooba.Offer.Application` reference;
2. `ComposeProductAsync` resolves the variant via `_primaryOffers.ResolveVariantId(..., candidates.Select(ToSelectionCandidate).ToList())` and the primary via `_primaryOffers.Resolve(...)`, then maps the selected `OfferId` back to the existing Host presentation record (`primary = resolvableCandidates.First(c => c.OfferId == selected.OfferId)`) so all downstream presentation, promotion and card fields keep reading the Host record unchanged;
3. `BuildVariantsAsync` resolves the per-variant primary the same way.

A single explicit selection-only mapping was added:

```csharp
private static OfferSelectionCandidate ToSelectionCandidate(StorefrontOfferCandidate candidate)
    => new(candidate.OfferId, candidate.CatalogVariantId, candidate.SellerPartyId,
        candidate.AmountExclusiveOfTax, candidate.AvailableUnits);
```

`StorefrontOfferCandidate` remains the Host presentation shape; `SellerDisplayName`, `SellerSku`, `Currency`, `Market` and `TaxCategoryLabel` were **not** moved into Offer.

## Host global alias removal and the 9 consumers

`OfferGlobalUsings.cs` deleted. Each of the 9 audited consumers already had an explicit `using Tooba.Offer.Contracts.Dtos;` and compiles unchanged:

`AccessControl/AccessControlDevelopmentSeed.cs`, `Admin/AdminPanelComposer.cs`, `Admin/CatalogAttributeSchemaDevelopmentBootstrap.cs`, `Admin/MerchandisingCampaignAdminEndpoints.cs`, `Admin/ProductWorkspaceComposer.cs`, `Admin/ProductWorkspaceDevelopmentBootstrap.cs`, `Configuration/ToobaPlatformOptions.cs`, `Storefront/StorefrontDemoCatalogBootstrap.cs`; `Outbox/OutboxWorkerSeams.cs` merely mentions the names in an XML comment and needed no change. No new alias workaround was introduced, and `HostFolderStructureTests` / `HostCartResidualGuardTests` allowlists were updated to drop the removed file so the root allowlist cannot resurrect it.

## Thin adapter justification

`HostOfferSellerAuthorizer.cs` still implements the Offer-owned port `Tooba.Offer.Endpoints.Seller.IOfferSellerAuthorizer` and delegates once to `SellerPanelAccess.RequireAuthorizedAsync`. It contains no ranking, no pricing, no inventory decision, no persistence, no response composition and no Offer business service call, so it remains an allowed thin Host security adapter and needed no change.

## Behavior parity

- The ordering rule is byte-for-byte the same predicate chain; the tie-break order (`AvailableUnits > 0`, then amount, then `OfferId`) is unchanged.
- `Resolve` returns the same candidate for the same input list, and the stored selection is always the Host record that produced it, so promotion evaluation, alternate-offer projection, variant `Purchasable` flags, card amount/currency/availability and JSON contracts are unchanged.
- Empty candidates still resolve to `null`, so `ComposeProductAsync` still returns `null` exactly as before.

## Recovery / SoT

- Offer audit = ACCEPTED
- Host residue = `BUSINESS_RESIDUE_REMOVED_THIN_SECURITY_ADAPTER_ONLY`
- selection owner = `OFFER_CONTRACT_PORT_APPLICATION_POLICY`
- global alias = `REMOVED`
- temp residue = `REMOVED`
- structure certification = `PENDING_TB_TMAR_OFFER_ARCH_COMPLETE_002_STRUCTURE_001`
- Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`
- `frontendFrozen = true`
- `nextTask = TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001`

## Validation

- `PrimaryOfferSelectionPolicyTests` (8 cases) + `OfferArchitectureGuardTests` + `OfferPhysicalStructureGuardTests` — 43 passed, 0 failed.
- Host `StorefrontCompositionTests`, `HostFolderStructureTests`, `TmarDurableGuardTests`, `TmarCompleteReferenceStructureGateTests` — 25 passed, 0 failed.
- `dotnet build src/backend/Tooba.slnx` — see result.

## Architecture guard

`OfferArchitectureGuardTests.Host_owns_no_offer_selection_policy_or_hidden_offer_alias` now asserts: `StorefrontPrimaryOfferResolver.cs` absent, `OfferGlobalUsings.cs` absent, `.tmp-t014-test-out` absent, no `global using OfferStatus`/`SalesChannel`/`Tooba.Offer`, no surviving `OrderByDescending(candidate => candidate.AvailableUnits > 0)` rule in Host, `StorefrontComposer` consumes `IPrimaryOfferSelectionPolicy` with no `Tooba.Offer.Application` reference, Contracts owns the port, Application owns the policy, `HostOfferSellerAuthorizer` stays business-free, and no Offer production project references `Tooba.Host`.
