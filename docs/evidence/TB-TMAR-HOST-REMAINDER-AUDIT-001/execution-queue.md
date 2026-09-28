# TB-TMAR-HOST-REMAINDER-AUDIT-001 — Execution Queue

Ranked by **architectural dependency/order** (not subjective quality): smallest high-confidence
areas first, then their prerequisites, then the heavy global boundaries whose repair depends on the
module seams produced earlier.

`FIRST RECOMMENDED TASK = #1`. This task was **not** started.

---

## #1 — `MEDIA` (recommended first)

* **Scope** — `src/backend/Host/Tooba.Host/Media/MediaEndpoints.cs` (1 file)
* **Classification** — `READY_TO_MIGRATE`
* **Blockers** — none blocking. `Media.Application` import; `/v1/admin/media` upload +
  public binary delivery routes; admin auth currently delegated through `Host/Admin` `AdminPanelAccess`
  (already certified).
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-MEDIA-EVACUATE-001 — Evacuate Host Media HTTP boundary into Tooba.Media.Endpoints`
* **Expected size** — **TINY**
* **Expected execution time** — **<=10 min**
* **Why first** — single self-contained file, one module destination, no persistence, no
  message parsing, no dev residue, and the Admin auth seam it needs is already certified.

---

## #2 — `OPERATORPROFILE`

* **Scope** — `OperatorProfile/OperatorProfileEndpoints.cs` (1 file)
* **Classification** — `READY_TO_MIGRATE`
* **Blockers** — `title = ex.Message` at lines 56, 90 (display-only, but should be normalized to
  the canonical `ApiResponseFactory` pattern during evacuation).
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-OPERATORPROFILE-EVACUATE-001 — Evacuate Operator Profile HTTP boundary`
* **Expected size** — **TINY**
* **Expected execution time** — **<=10 min**

---

## #3 — `PRODUCTQNA` (+ BulkInquiry split)

* **Scope** — `ProductQnA/ProductQnAEndpoints.cs` (1 file)
* **Classification** — `READY_TO_MIGRATE`
* **Blockers** — two modules in one Host file (`ProductQnA.Application` +
  `BulkInquiry.Application`); requires a clean two-destination split.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-PRODUCTQNA-EVACUATE-001 — Split ProductQnA/BulkInquiry HTTP boundary into owning modules`
* **Expected size** — **SMALL**
* **Expected execution time** — **10–15 min**

---

## #4 — `PREFERENCES`

* **Scope** — `Preferences/UiPreferenceEndpoints.cs`, `Preferences/UserPreferenceEndpoints.cs`
* **Classification** — `READY_TO_MIGRATE` (+ minor `NEEDS_PRECERT_REPAIR` hygiene)
* **Blockers** — `UserPreference.Application` + `UserPreference.Domain` imports;
  `title = ex.Message` in 4 error paths.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-PREFERENCES-EVACUATE-001 — Evacuate UI/User preference HTTP boundaries`
* **Expected size** — **SMALL**
* **Expected execution time** — **10–15 min**

---

## #5 — `WISHLIST`

* **Scope** — `Wishlist/WishlistEndpoints.cs`, `Wishlist/WishlistComposer.cs`,
  `Wishlist/WishlistDevelopmentSeed.cs` (3 files)
* **Classification** — `READY_TO_MIGRATE` + `NEEDS_PRECERT_REPAIR`
* **Blockers** — `WishlistDevelopmentSeed` performs `SaveChangesAsync` on
  `Wishlist.Infrastructure.Persistence` and reads `Catalog.Infrastructure.Persistence` +
  `Catalog.Domain`; seed must move to module Infrastructure/Development while endpoints move to
  `Wishlist.Endpoints`.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-WISHLIST-EVACUATE-001 — Evacuate Wishlist HTTP + relocate demo seed`
* **Expected size** — **SMALL**
* **Expected execution time** — **10–15 min**

---

## #6 — `STORY` (prerequisite for Story grid)

* **Scope** — `Story/StoryEndpoints.cs`, `Story/StoryPanelComposer.cs` (2 files)
* **Classification** — `NEEDS_PRECERT_REPAIR`
* **Blockers** — message-text classification at `StoryEndpoints.cs:52-53`;
  `Story.Infrastructure.Persistence` hub in `StoryPanelComposer`; the Host Story grid engine
  (`Grid/AdminStoryGridQueryEngine.cs`) depends on the same persistence.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-STORY-PRECERT-REPAIR-001 — Replace Story message-text classification with stable codes`
* **Expected size** — **SMALL**
* **Expected execution time** — **10–15 min**
* **Blocks** — Story evacuation and `#7`.

---

## #7 — `REVIEWS` (prerequisite for Reviews grid)

* **Scope** — `Reviews/ReviewEndpoints.cs`, `Reviews/ReviewPanelComposer.cs` (2 files)
* **Classification** — `NEEDS_PRECERT_REPAIR`
* **Blockers** — duplicate detection via `ex.Message.Contains("قبلاً")` (line 58) must become a
  stable machine code; `ReviewPanelComposer` holds cross-module `DbContext` joins
  (`Catalog.Infrastructure.Persistence` + `Reviews.Infrastructure.Persistence`).
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-REVIEWS-PRECERT-REPAIR-001 — Replace Reviews message-text classification with stable codes`
* **Expected size** — **SMALL**
* **Expected execution time** — **10–15 min**

---

## #8 — `PAGECOMPOSITION`

* **Scope** — `PageComposition/PageCompositionEndpoints.cs`,
  `PageComposition/PageCompositionPanelComposer.cs` (2 files)
* **Classification** — `NEEDS_PRECERT_REPAIR`
* **Blockers** — three-way Persian message-text classification at lines 211-213 driving
  404/400/403; `PageComposition.Domain` import in the panel composer.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-PAGECOMPOSITION-PRECERT-REPAIR-001 — Replace PageComposition message-text classification`
* **Expected size** — **MEDIUM**
* **Expected execution time** — **15–25 min**

---

## #9 — `LOCALIZATION`

* **Scope** — `Localization/LocaleAdminEndpoints.cs`, `Localization/ContentLanguageReferenceGuard.cs`
* **Classification** — `NEEDS_PRECERT_REPAIR`
* **Blockers** — `errorCode = ex.Message` at line 171; `ContentLanguageReferenceGuard` holds a
  `DbContext` reference guard that belongs closer to Content/Localization Infrastructure.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-LOCALIZATION-PRECERT-REPAIR-001 — Replace locale message-as-code and relocate reference guard`
* **Expected size** — **SMALL**
* **Expected execution time** — **10–15 min**

---

## #10 — `GRID` admin query engines

* **Scope** — `Grid/AdminReviewGridQueryEngine.cs`, `Grid/AdminStoryGridQueryEngine.cs`,
  `Grid/AdminSellersGridQueryEngine.cs`, `Grid/AdminListGridPolicies.cs` (4 of 8 Grid files)
* **Classification** — `NEEDS_PRECERT_REPAIR` (migrate with their owning modules)
* **Blockers** — direct `DbContext`/`IQueryable` grid reads with `Catalog.Domain` /
  `Catalog.Infrastructure` / `Catalog.Application` imports.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-GRID-ADMIN-ENGINES-AUDIT-002 — Decide engine ownership and destination modules`
* **Expected size** — **MEDIUM**
* **Expected execution time** — **15–25 min**
* **Depends on** — `#6`, `#7` (stable codes must exist before grid evacuation).

---

## #11 — `CATALOGADAPTERS`

* **Scope** — `CatalogAdapters/MerchandisingStoreLandingReferenceGate.cs`,
  `StoreLandingMerchandisingAdapter.cs`, `StoreLandingShellAdapter.cs` (3 files)
* **Classification** — `READY_TO_MIGRATE`
* **Blockers** — `Catalog.Application`, `Promotion.Application`, `Promotion.Domain.Merchandising`,
  `Content.Domain.Rules` imports.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-CATALOGADAPTERS-EVACUATE-001 — Evacuate store-landing adapters into owning modules`
* **Expected size** — **MEDIUM**
* **Expected execution time** — **15–25 min**

---

## #12 — `SELLER` (highest-value structural repair)

* **Scope** — `Seller/**` (15 files)
* **Classification** — `GLOBAL_HOST_BOUNDARY_REVIEW` + `NEEDS_PRECERT_REPAIR`
* **Blockers** — 7 authorizers using `HttpContext.RequestServices.GetRequiredService(...)`;
  `HostSellerOrderViewAccessReader` + `HostSupportSellerAuthorizer` with
  `AccessControl.Application/Domain`; `SellerPanelEndpoints`/`SellerSettingsEndpoints`/
  `SellerPanelComposer` with `Catalog.Application/Domain/Infrastructure.Persistence`,
  `Party.Application`, `Order.Application.Seller.Queries.*`;
  `SellerDevActorBootstrap` with `Identity.Infrastructure` + `Party.Infrastructure.Persistence`.
  This is the exact CANON-002/003/006/009 defect family already repaired for Admin and is the
  natural next canon wave.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-SELLER-CANON-001 — Seller panel access seams: remove service locator + AccessControl coupling`
* **Expected size** — **MEDIUM**
* **Expected execution time** — **15–25 min**

---

## #13 — `STOREFRONT`

* **Scope** — `Storefront/**` (12 files)
* **Classification** — `NEEDS_PRECERT_REPAIR` + `GLOBAL_HOST_BOUNDARY_REVIEW`
* **Blockers** — 18 `*.Application`, 11 `*.Infrastructure`, 11 `*.Domain` imports; `DbContext`
  reads and `SaveChangesAsync` on catalog persistence in the composer/demo bootstrap/query files;
  `HostPaymentStorefrontAuthorizer` service locator. Largest single-area debt; needs decomposition.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-STOREFRONT-AUDIT-002 — Decompose Storefront composer/bootstrap/query ownership`
* **Expected size** — **MEDIUM**
* **Expected execution time** — **15–25 min**

---

## #14 — `ROOT GLOBAL BOUNDARIES`

* **Scope** — `CheckoutReservationHoldPolicy.cs`, `CommerceHoldPolicy.cs`,
  `UnpaidOrderExpiryHostedService.cs`, `UnpaidOrderExpiryHostOptions.cs`,
  `GlobalUsings.SettlementApp.cs`, `GlobalUsings.SettlementDomain.cs`, `Program.cs`
* **Classification** — `GLOBAL_HOST_BOUNDARY_REVIEW`
* **Blockers** — 33 `*.Application`, 13 `*.Infrastructure`, 5 `*.Domain` imports; Order/Payment
  checkout + reservation policy and settlement global usings live in Host.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-CHECKOUT-BOUNDARY-AUDIT-001 — Decide checkout/reservation hold-policy destination`
* **Expected size** — **MEDIUM**
* **Expected execution time** — **15–25 min**

---

## #15 — `DEVELOPMENT` residue

* **Scope** — `Development/**` (12), `Composition/ContentDevelopmentSeedHost.cs`,
  `Settings/SettingsFoundationDevelopmentSeed.cs`, `Support/SupportDevelopmentSeedHost.cs`,
  `Wallet/WalletDevelopmentSeedHost.cs`, `Seller/SellerDevActorBootstrap.cs`
* **Classification** — `DEVELOPMENT_ONLY_REVIEW` (+ 3 `READY_TO_MIGRATE` dev seeds)
* **Blockers** — 52 `*.Infrastructure` + 18 `*.Application` + 7 `*.Domain` imports;
  `ProductWorkspaceDevelopmentBootstrap` writes via `SaveChangesAsync` on catalog+party DBs;
  `CatalogAttributeSchemaSellableEnricher` reaches `Party.Infrastructure.Persistence`.
* **Recommended next task ID/title** —
  `TB-TMAR-HOST-DEVELOPMENT-SEED-RELOCATION-001 — Relocate dev seed hosts into module Infrastructure`
* **Expected size** — **MEDIUM**
* **Expected execution time** — **15–25 min**

---

## Dependency order (summary)

```text
#1 Media  ->  #2 OperatorProfile  ->  #3 ProductQnA  ->  #4 Preferences  ->  #5 Wishlist
#6 Story  ->  #7 Reviews  ->  #10 Grid engines
#8 PageComposition   #9 Localization
#11 CatalogAdapters
#12 Seller (canon wave)
#13 Storefront
#14 Root global boundaries
#15 Development residue
```

## Recommended immediate next task

**`TB-TMAR-HOST-MEDIA-EVACUATE-001 — Evacuate Host Media HTTP boundary into Tooba.Media.Endpoints`**

* scope — `src/backend/Host/Tooba.Host/Media/MediaEndpoints.cs`
* size — **TINY**; time bucket — **<=10 min**
* rationale — smallest, self-contained, single module destination, no persistence, no message
  parsing, no dev residue, and its admin authorization seam is already certified.
