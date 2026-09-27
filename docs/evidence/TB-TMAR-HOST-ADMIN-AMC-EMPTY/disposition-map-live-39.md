# Host/Admin disposition — reconciled after Analyze + in-session evacuate

Source Analyze: [Analyze remaining Host Admin](ca64dd8d-ff08-4549-8238-bb94af98bfd6) (snapshot at 52 files).
**Current inventory: 39 `*.cs`** under `Host/Tooba.Host/Admin` (2026-09-27).

Analyze counts at claim time (52): MOVE 26 · KEEP 16 · BLOCK 6 · RETAIN_PARTIAL 4.  
This file is the **live** disposition after W20 + CatalogDemo + CheckoutAbuse + StoreLanding cutovers.

## Live counts (39)

| Disposition | Count | Notes |
|---|---:|---|
| KEEP_AS_HOST_PLATFORM | 16 | Aligns with Analyze KEEP set (authorizers, AdminPanel*, grid, DevActor, CatalogActorHttpBinding) |
| RETAIN_PARTIAL | 4 | ProductWorkspace* — W21–W24 (no Catalog aggregate dump) |
| BLOCK_DEFERRED | 3 | StoreAppearance* (2) + HoldPolicy (1 multi-owner) |
| MOVE next | 16 | StoreMenu*, Reservation*, CheckoutIdentity, Merchandising*, seeds, LandingPageDevelopmentSeed, MerchandisingStoreLandingReferenceGate |

## Already evacuated (vs Analyze MOVE/BLOCK table)

| Analyze row | Live state |
|---|---|
| CatalogDemo/* (10) | **DONE** → Catalog.Infrastructure/Endpoints Development |
| CheckoutAbuseSettingsEndpoints | **DONE** → Catalog Settings CQRS |
| StoreLandingPageEndpoints/Composer | **DONE** → Catalog (Analyze marked BLOCK; Host adapters remain for shell/merch) |
| W20 brand-options | **DONE** (pre-Analyze) |

## Remaining file map (39)

| File | Disposition | Destination / blocker |
|---|---|---|
| AdminDevActorBootstrap.cs | KEEP | Host Dev actor |
| AdminGridQueryEndpoint.cs | KEEP | Thin grid helper |
| AdminPanelAccess.cs | KEEP | Admin auth core |
| AdminPanelComposer.cs | KEEP | Cross-module dashboard (Contracts debt) |
| AdminPanelEndpoints.cs | KEEP | `/v1/admin` shell HTTP |
| AdminPanelModels.cs | KEEP | Shell DTOs |
| CatalogActorHttpBinding.cs | KEEP | Until last Host PW routes leave |
| HostAdminPanelAccess.cs | KEEP | `IAdminPanelAccess` |
| HostOrderAdminAuthorizer.cs | KEEP | Thin Order auth |
| HostOrderAdminEffectiveAccessReader.cs | KEEP | Grants adapter |
| HostPaymentAdminAuthorizer.cs | KEEP | Thin Payment auth |
| HostPromotionAdminAuthorizer.cs | KEEP | Thin Promotion auth |
| HostReturnAdminAuthorizer.cs | KEEP | Thin Returns auth |
| HostSettlementAdminAuthorizer.cs | KEEP | Thin Settlement auth |
| HostSupportAdminAuthorizer.cs | KEEP | Thin Support auth |
| HostWalletAdminAuthorizer.cs | KEEP | Thin Wallet auth |
| ProductWorkspaceEndpoints.cs | RETAIN_PARTIAL | PW Endpoints + Catalog Commands W21–W24 |
| ProductWorkspaceComposer.cs | RETAIN_PARTIAL | PW composition |
| ProductWorkspaceModels.cs | RETAIN_PARTIAL | PW models |
| ProductWorkspaceDevelopmentBootstrap.cs | RETAIN_PARTIAL | W24 disposition |
| StoreAppearanceSettingsEndpoints.cs | BLOCK | Host.Storefront `StoreAppearanceProjector` |
| StoreAppearanceSettingsComposer.cs | BLOCK | same |
| HoldPolicySettingsEndpoints.cs | BLOCK | Multi-owner split needs Architect |
| StoreMenuEndpoints.cs | MOVE | Catalog.Endpoints |
| StoreMenuComposer.cs | MOVE | Catalog.Application |
| StoreMenuDevelopmentSeed.cs | MOVE | Catalog Development |
| CheckoutIdentitySettingsEndpoints.cs | MOVE | Catalog settings (+ Gate inject → query) |
| ReservationPolicyAdminEndpoints.cs | MOVE | Order.Endpoints |
| ReservationPolicyAdminComposer.cs | MOVE | Order Application |
| ReservationPolicyAdminModels.cs | MOVE | Order models |
| MerchandisingCampaignAdminEndpoints.cs | MOVE | Promotion (MUST_SPLIT / Contracts) |
| MerchandisingCampaignDevelopmentSeed.cs | MOVE | Promotion Development |
| MerchandisingStoreLandingReferenceGate.cs | MOVE/KEEP thin | Catalog adapter via Promotion.Contracts |
| LandingPageDevelopmentSeed.cs | MOVE | Catalog Development (Landing unlocked) |
| CatalogAttributeSchemaDevelopmentBootstrap.cs | MOVE later | Catalog Dev — still Host (cross-module sellable) |
| FashionTemplateCatalogSeed.cs | MOVE later | Catalog Dev — Host cross-module |
| IndustryBatchA/B/CTemplateCatalogSeed.cs | MOVE later | Catalog Dev — Host cross-module |

## Next waves (Analyze N1–N3, adjusted)

1. **StoreMenu*** → Catalog (Analyze N2; N1 CheckoutAbuse already done)
2. **CheckoutIdentity** → Catalog; **ReservationPolicy*** → Order
3. **W21 ProductWorkspace lifecycle** (locked W17) — member-partial; files stay until W24
4. Later: Merchandising→Promotion; template seeds after Contracts ports; StoreAppearance/HoldPolicy only after Architect unlock

## Cannot empty Admin yet

Same as Analyze §4, minus StoreLanding BLOCK (evacuated with Host shell/merch adapters): KEEP 16 + StoreAppearance + HoldPolicy + ProductWorkspace* until W24 + remaining MOVE queues.
