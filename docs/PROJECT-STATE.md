# Tooba — Project State

Project:

```text
Tooba
```

Canonical repository:

```text
https://github.com/mrnikiemami-code/Tooba
```

Branch:

```text
main
```

Current Phase:

```text
P10 — Builder Acceptance + Product Source Finalization
```

Pipeline Mode:

```text
BRIDGE-WAKE-V1
Channel: tooba-main
```

Last Architect Accepted Task:

```text
TB-P10-T022-R20
```

Prior accepted catalog wave:

```text
TB-P07-T035
TB-P07-T036
TB-P07-T036-R1
TB-P07-T037
TB-P07-T038
TB-P07-T038-R1
TB-P07-T039
TB-P07-T041
TB-P07-T042-R1
TB-P07-T043
```


Last Architect Accepted Gate:

```text
TB-P05-GATE
```

Last Implementation Task:

```text
TB-TMAR-RECOVERY-LOCK-HARDEN-001
```

Last Architecture Audit Task:

```text
TB-P10-T022-R14
```

Current Issued Task:

```text
TB-TMAR-RECOVERY-LOCK-HARDEN-001 (Worker PASS — awaiting Architect)
```

Current Repair Task:

```text
none
```

TMAR Module Recovery (Worker PASS — awaiting Architect):

```text
TB-TMAR-WALLET-GOLDEN-001 — Wallet Endpoints Ownership + MediatR CQRS + Host Error/Idempotency Cleanup
Wallet-State: COMPLETE_REFERENCE_PATTERN
Wallet-HTTP-Ownership: MODULE_ENDPOINTS
Wallet-Endpoints-State: REAL_PROJECT_PRESENT
Wallet-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Wallet-Host-Endpoints: REMOVED
Wallet-Host-Business-Authority: NONE
Wallet-Host-DbAuthority: NONE_EXCEPT_DEV_BOOTSTRAP_ALLOWLIST
Wallet-CrossModule-Boundary: CONTRACTS_ONLY
Wallet-Result-Adoption: HTTP_USE_CASES_ADOPTED
Wallet-Error-Classification: STABLE_CODES_ONLY
Wallet-Prose-Mapping: NONE
Wallet-Unexpected-Exception-Swallow: NONE
Wallet-Idempotency-Generation: IIdGenerator
Wallet-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Wallet-Architecture-Guards: ENFORCED
Wallet-Behavior-Preservation: VERIFIED
Support-State: COMPLETE_REFERENCE_PATTERN
Notification-State: COMPLETE_REFERENCE_PATTERN
Returns-State: COMPLETE_REFERENCE_PATTERN
Fulfillment-State: COMPLETE_REFERENCE_PATTERN
Settlement-State: COMPLETE_REFERENCE_PATTERN
Cart-State: COMPLETE_REFERENCE_PATTERN
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Frontend-Production-Changes: NONE
Next-Recommended-Task: TB-TMAR-PAYMENT-GOLDEN-001
```

Prior TMAR Cart golden (awaiting Architect / USER_CART_REVIEW_CHECKPOINT):

```text
TB-TMAR-CART-GOLDEN-001-R1 — Cart Exception/Result Semantics Closure
Cart-State: COMPLETE_REFERENCE_PATTERN
Cart-HTTP-Ownership: MODULE_ENDPOINTS
Cart-Endpoints-State: REAL_PROJECT_PRESENT
Cart-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Cart-Host-Routes: REMOVED
Cart-Host-Composer: REMOVED
Cart-Host-CatalogDbAuthority: NONE
Cart-CrossModule-Boundary: CONTRACTS_ONLY
Cart-Exception-Classification: STABLE_CODES_ONLY
Cart-Prose-Mapping: NONE
Cart-Unexpected-Exception-Swallow: NONE
Cart-Result-Adoption: HTTP_USE_CASES_ADOPTED
Cart-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Cart-Architecture-Guards: ENFORCED
Cart-Behavior-Preservation: VERIFIED
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Frontend-Production-Changes: NONE
Next-Recommended-Task: USER_CART_REVIEW_CHECKPOINT
```

Prior TMAR batch (BATCH-004 tip before Cart golden):

```text
TB-TMAR-NEXT-MODULE-BATCH-004 — Fulfillment + Returns Golden Recovery Batch
Fulfillment-State: COMPLETE_REFERENCE_PATTERN
Fulfillment-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Fulfillment-CrossModule-Boundary: CONTRACTS_ONLY
Returns-State: COMPLETE_REFERENCE_PATTERN
Returns-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Returns-CrossModule-Boundary: CONTRACTS_ONLY
Behavior-Preservation: VERIFIED
Notification-State: COMPLETE_REFERENCE_PATTERN
Support-State: COMPLETE_REFERENCE_PATTERN
Wallet-State: COMPLETE_REFERENCE_PATTERN
Payment-State: COMPLETE_REFERENCE_PATTERN
Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN
Inventory-State: COMPLETE_REFERENCE_PATTERN
Promotion-State: COMPLETE_REFERENCE_PATTERN
Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes: NONE
Batch-State: COMPLETE
Module-Recovery-State: NEXT_REFERENCE_BATCH_004_COMPLETE
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-005
```

Prior (BATCH-003-R1 tip before BATCH-004):

```text
TB-TMAR-NEXT-MODULE-BATCH-003-R1 — Notification Order Boundary Closure + Guard Correction
Notification-State: COMPLETE_REFERENCE_PATTERN
Notification-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Notification-Public-Boundary: CONTRACTS_ONLY
Notification-Order-Boundary: CONTRACTS_ONLY
Support-State: COMPLETE_REFERENCE_PATTERN
Support-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Support-Notification-Boundary: CONTRACTS_ONLY
Behavior-Preservation: VERIFIED
Wallet-State: COMPLETE_REFERENCE_PATTERN
Payment-State: COMPLETE_REFERENCE_PATTERN
Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN
Inventory-State: COMPLETE_REFERENCE_PATTERN
Promotion-State: COMPLETE_REFERENCE_PATTERN
Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes: NONE
Batch-State: COMPLETE
Module-Recovery-State: NEXT_REFERENCE_BATCH_003_COMPLETE
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-004
```

Prior parent (BATCH-003 tip before R1):

```text
TB-TMAR-NEXT-MODULE-BATCH-003 — Notification + Support Golden Recovery Batch
Notification-State: COMPLETE_REFERENCE_PATTERN
Notification-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Notification-Public-Boundary: CONTRACTS_ONLY
Support-State: COMPLETE_REFERENCE_PATTERN
Support-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Support-Notification-Boundary: CONTRACTS_ONLY
Behavior-Preservation: VERIFIED
Wallet-State: COMPLETE_REFERENCE_PATTERN
Payment-State: COMPLETE_REFERENCE_PATTERN
Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN
Inventory-State: COMPLETE_REFERENCE_PATTERN
Promotion-State: COMPLETE_REFERENCE_PATTERN
Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes: NONE
Batch-State: COMPLETE
Module-Recovery-State: NEXT_REFERENCE_BATCH_003_COMPLETE
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-004
```

Prior parent (BATCH-002-R1 ACCEPTED):

```text
TB-TMAR-NEXT-MODULE-BATCH-002-R1 — Wallet Notification Boundary + Financial Behavior Preservation Closure
```

Prior parent (BATCH-002 tip before R1):

```text
TB-TMAR-NEXT-MODULE-BATCH-002 — Wallet+Payment golden recovery (REOPENED_NOTIFICATION_BOUNDARY closed by R1)
```

Implemented Under Review:

```text
TB-P10-T022-R21 — Product Source Final Integration «پیشنهاد شگفت‌انگیز» additive-only (PromotionCampaign/AMAZING, CampaignId=null); locks 428–430; Appearance YES; Builder NO; awaiting Architect review
TB-P10-T022-R20 — Amazing Campaign Admin Workspace (list/workspace/offers/pricing); locks 422–427; Architect-ACCEPTED before R21; Appearance YES; Builder NO
TB-P10-T022-R19 — Campaign-aware Cart/Checkout quote integrity (MerchandisingCampaignId + ICampaignCartPriceAuthority); locks 417–421; Architect-ACCEPTED before R20; Appearance YES; Builder NO
TB-P10-T022-R18 — Campaign promotional pricing via AuthoredPrice QualifierKind=MerchandisingCampaign + TOOBA-CAPABILITY-MAP; locks 412–416; Architect-ACCEPTED before R19; Appearance YES; Builder NO
TB-P10-T022-R17 — Product Showcase PromotionCampaign source (پیشنهاد شگفت‌انگیز) Builder→Preview→Publish→Storefront; locks 405–411; Architect-ACCEPTED before R18; Appearance YES; Builder NO
TB-P10-T022-R16 — Amazing Offers Runtime Resolver + Real Test Data (active/future selection, availability filter, Dev seed, locks 399–404); Architect-ACCEPTED before R17; Appearance YES; Builder NO
TB-P10-T022-R15 — Merchandising Promotion Foundation (Campaign + Type AMAZING + membership + translations; promo price deferred; no Builder/Storefront); Architect-ACCEPTED before R16; Appearance YES; Builder NO
TB-P10-T022-R14 — Amazing Offers / Promotion Engine Architecture Audit (docs only; zero schema/UI/storefront code); Architect-ACCEPTED before R15; Appearance YES; Builder NO
TB-P10-T022-R13-R4 — Product Showcase Motion & Proportion Polish (calm autoplay, no sweep, proportionate cinematic, upright explorer); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R3 — Product Showcase Distinct Variant V2 (visually distinct sunny/money/cinematic/cinematic-plus/explorer + autoplay); Architect-ACCEPTED before R13-R4; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R2-R2 — Product Showcase Final Closure (Admin→exact published Landing route sunny+cinematic; real Git SHA; user-work 18ca10c9 preserved); Architect-ACCEPTED before R13-R3; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R2-R1 — Product Showcase Variant Visual Closure (real Admin Appearance/Review proof; user-work 18ca10c9 preserved); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R2 — Product Showcase Variant Expansion (sunny/سانی, money/مانی, cinematic/سینمایی, cinematic-plus/سینمایی پلاس, explorer/کاشف; Embla rails; ProductCard unchanged); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R1-R1 — Hero Slider Builder Visual Closure Repair; Architect-ACCEPTED before R13-R2; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R1 — Hero Slider Builder Repair (six variants, height presets, structured CTA, visible validation); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P11-T001 — Admin Completion Kickoff (Recovery SoT reconciliation + production-grade Admin gap audit); retained for later — not executed further; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13 — Final Builder Hardening (end-to-end completeness/consistency across 10 industry Template packs); Architect-ACCEPTED after worker Result; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R12C — Industry Template Catalog Batch C (shoes, plants, beauty); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R12B — Industry Template Catalog Batch B (tile-ceramic, interior-decor, home-appliances); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R12A — Industry Template Catalog Batch A (auto-parts, building-materials, tools-hardware); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R11 — Variant Picker V2 (human design names, real component previews, live carousel/tab behavior, Review live preview); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R10 — Store Page Editor Workspace (Template Apply materialization, unified section list, drag/drop + arrow reorder, insert/delete/disable); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R9-R1 — Store Pages SSR Performance Repair (Landing latency, canonical resolver dedupe, Store+locale+page cache invalidation); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R9 — Store Pages Foundation (Home/Landing type, /landing/{slug}, Home set/restore, page SEO, sitemap, AppDataGrid); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R8-R2 — Store Preview Asset Isolation Repair (dedicated Preview-Fake content/media; zero Template Fashion asset reuse in Store fake fill); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R8-R1 — R8 Runtime Visual Closure (FE+Host evidence PNGs; article magazine-rail sample marker repair; preview-only partial-fillers fixture); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R8 — Store Preview Variant-Fidelity Repair (preview-only fake fill through real Section/Variant components); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R7 — Preview context unification (strict Sample/Store isolation, preview placeholders, dynamic locale, focal crop); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R6 — Exact Product/Category/Brand Template Catalog parity + Fashion category media fidelity; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R5 — Fashion Template Catalog Persistence (exact schema mirror + 8/15 Fashion seed + Sample purity); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R4 — Fashion Template Live Preview Pilot (real /template-preview/fashion + Admin iframe Desktop/Tablet/Mobile, isolated demo pack); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R3 — Template Selection Wireframe (device modes, seed-pack UX skeleton, distinct industry previews); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R2 — Builder Visual/Workflow Repair R2 (Landing AppDataGrid, blank Add Section, fixed wizard shell, template V2, banner/brand/workspace); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R1 — Builder Admin UX Repair (Orders-grid Resource Selector, section wizard, theme isolation, Story display-only); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022 — Builder Acceptance Candidate — Completeness Audit, Visual Consistency, Final UX/Runtime Hardening; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T021 — Variant Expansion Wave 2 + Builder visual polish + industry template fidelity; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T020 — Native StoryRail/BannerShowcase + first variant wave + template start UX; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T019 — Shared composition renderer + existing Shopeiva variants + visual Admin variant picker; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T018 — Home+Landing composition engine foundation (shared registry, responsive contracts, Landing adapter); USER_VISUAL_ACCEPTED=YES (Appearance); Builder not visually accepted
TB-P10-T017-R5 — Section-context composition (inherit-by-default, local derived surfaces, card-area guard); USER_VISUAL_ACCEPTED=YES (Appearance accepted with T018)
TB-P10-T017-R4 — Component-level theme compliance (derived Card/Elevated/Input; shared primitives; crawler); USER_VISUAL_ACCEPTED=NO
TB-P10-T017-R3 — Storefront-wide theme coverage (route inventory + shared surfaces + crawler); USER_VISUAL_ACCEPTED=NO
TB-P10-T017-R2 — PLP/Landing product-card media well height (Tailwind aspect-[4/5] emitted); USER_VISUAL_ACCEPTED=NO
TB-P10-T017-R1 — PDP first-paint 500 repair (ThemeToggle without ThemeProvider throw); USER_VISUAL_ACCEPTED=NO
TB-P10-T017 — Final Surface Theme Visual Acceptance Refresh (four-role demo pack across Storefront pages); USER_VISUAL_ACCEPTED=NO
TB-P10-T016 — Storefront Semantic Surface Architecture (PageBackground/SectionSurface/SectionAlternate/SectionAccent, Neutral+PaletteTint, Admin four-role preview); USER_VISUAL_ACCEPTED=NO
TB-P10-T015 — Storefront Background Tint Option (Neutral/PaletteTint, curated tint tokens, Admin UX, Home/Landing inherit); USER_VISUAL_ACCEPTED=NO
TB-P10-T014 — Appearance + Landing Final Demo & Visual Acceptance Pack (seeded palettes/pages/menu, inspection guide, screenshot pack); USER_VISUAL_ACCEPTED=NO
TB-P10-T013 — Storefront Menu Management (Admin UX, Menu/MenuItem, Header/Landing integration, demo seed); USER_VISUAL_ACCEPTED=NO
TB-P10-T012 — Landing Page Composer (Admin UX, preview, publish, Home selection, storefront section renderer); USER_VISUAL_ACCEPTED=NO
TB-P10-T011 — Landing Section foundation (PageSection, approved registry, controlled sources); USER_VISUAL_ACCEPTED=NO
TB-P10-T010 — Landing Page foundation (Page model, Home selection, dynamic slug, reserved routes); USER_VISUAL_ACCEPTED=NO
TB-P10-T009-R2 — Product Card Skin Admin UX + console integrity; USER_VISUAL_ACCEPTED=NO
TB-P10-T009-R1 — Product Card Skin visual proof (persistent screenshots, Admin preview, cross-surface); USER_VISUAL_ACCEPTED=NO
TB-P10-T009 — Product Card Skin System (classic/clean/elevated/glass, Admin selection, storefront-wide); USER_VISUAL_ACCEPTED=NO
TB-P10-T008-R1 — Light/Dark visual integrity repair (canonical dark brand-emphasis + product-card surfaces); USER_VISUAL_ACCEPTED=NO
TB-P10-T008 — Storefront Light/Dark ThemeMode (Admin persist, SSR first paint, UserChoice toggle); USER_VISUAL_ACCEPTED=NO
TB-P10-T007 — Storefront token completion across Home, PDP, Shipping, and remaining brand-bound surfaces; USER_VISUAL_ACCEPTED=NO
TB-P10-T006 — Store Appearance Admin (curated palette selection, preview, persistence, cache invalidation, live Storefront apply); USER_VISUAL_ACCEPTED=NO
TB-P10-T005-R1 — Appearance foundation runtime + recovery repair (live /appearance, migration, Last Architect-accepted = TB-P10-T005-R1); USER_VISUAL_ACCEPTED=NO
TB-P10-T005 — Storefront Appearance Foundation (canonical Store appearance, semantic tokens, curated palette, future page extension contracts); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1-R4 — Storefront auth/cart network churn repair (canonical session cache, one merge per login, no anonymous 401 storm); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1-R3 — Storefront account identity + logout cart persistence + exact conversion boundary; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1-R2 — Recipient visual verification gate (Payment + Customer + Admin browser proof); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1-R1 — Recipient name canonicalization repair (explicit First/Last win; legacy RecipientName fallback only); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1 — Authenticated storefront continuity repair (cart through login/shipping + canonical account header + FirstName/LastName); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24 — Final identity + limits + atomic checkout integration gate; USER_VISUAL_ACCEPTED=YES
TB-P10-T004-R23 — Max open unpaid orders + reservation churn anti-abuse + Admin Settings; USER_VISUAL_ACCEPTED=YES
TB-P10-T004-R22-R1 — First canonical Login + checkout identity policy + cart merge; USER_VISUAL_ACCEPTED=YES
TB-P10-T004-R21-R1 — Accepted pending-order UX + atomic checkout commit gate; USER_VISUAL_ACCEPTED=YES
TB-P10-T004-R20-R1 — Visual Gate Repair (long-hold countdown + Admin history localization); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R20 — Reservation UX Visual Runtime Gate; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R19 — Reservation Lifecycle Final Integration Gate; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R18 — Reservation Policy Admin UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R17 — Admin Reservation Cycle Audit UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R16 — Customer Pending-Payment UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R15 — Reservation Cycle Foundation; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R14 — Paid Checkout Re-Initiation Guard; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R13 — Committed Checkout Ownership + Active Cart Rotation; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R12 — P10 Final Lifecycle Regression + Production Readiness; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R11 — Paid Projection Financial Consistency (seller totals + StoreShipping); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R10 — Unpaid Order Expiry + Hold Policy Settings UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R9 — Cart lifetime vs Inventory hold separation; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R8 — Admin Orders + Payments Supply UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R7 — Order Supply Capability Foundation; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R6 — Historical Paid/Manual Orders Inventory Recovery; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R5 — Manual Payment Review Reservation Lifecycle; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R4 — Payment Result Ownership + Polling Repair (stop 401 after cart finalize); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R3 — Payment Completion Runtime Proof (A–J + Converted-cart payment GetAsync fix); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R2 — Storefront Payment Completion Repair (sandbox simulator + card-to-card proof/tracking + cart finalization); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R1 — Authenticated Checkout Runtime Proof (real session E2E + payment actor ownership fix); USER_VISUAL_ACCEPTED=NO
TB-P10-T004 — Storefront Checkout Final Gate (Cart→Shipping→Payment→Order E2E hardening); under repair via R1; USER_VISUAL_ACCEPTED=NO
TB-P10-T003-R1 — Payment shipping StoreShipping allocation (no first-seller shortcut); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P10-T003 — Storefront Payment (Store-enabled methods, no fake card form, authoritative payable+initiate); USER_VISUAL_ACCEPTED=NO
TB-P10-T002-R1 — Storefront Shipping multi-seller runtime gate (Scenario E real 2+ sellers); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P10-T002 — Storefront Shipping (addresses, Store-enabled methods, delivery minimum, /payment handoff); USER_VISUAL_ACCEPTED=NO
TB-P10-T001 — Storefront Cart foundation (real ATC, mini-cart, /cart recommendations); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T022-R4 — Paid-order reservation REAL runtime proof (TTL/expiry/dispatch/cancel-restore/decimal); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T022-R3 — Paid-order reservation commit clears cart TTL (ExpiresAt=null); USER_VISUAL_ACCEPTED=NO
TB-P09-T022-R2 — Customer Tracking UI Proof (owned session + consolidated package primary rendering); USER_VISUAL_ACCEPTED=NO
TB-P09-T022-R1 — Consolidated Package visual runtime repair (FE Admin 500 readiness + real browser smoke); under repair via R2; USER_VISUAL_ACCEPTED=NO
TB-P09-T022 — Consolidated Package admin/runtime completion (central package lifecycle + history); under repair via R2; USER_VISUAL_ACCEPTED=NO
TB-P09-T021-R1 — Consolidated Package customer tracking access/projection repair; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T021 — Consolidated Package foundation (multi-seller master package orchestration); under repair via R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T020-R2 — Cancel/Restore reservation rebinding + localized inventory error UX; USER_VISUAL_ACCEPTED=NO
TB-P09-T020-R1 — P09 final-gate repair: partial-dispatch composed status + multi-seller runtime; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T020 — P09 final gate: partial-dispatch remainder + multi-shipment continuation; under repair via R2; USER_VISUAL_ACCEPTED=NO
TB-P09-T019 — Admin returns & refunds queue finalization; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T018 — Admin fulfillment queue finalization; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T017 — Orders menu final gate; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T016 — Whole-order cancel until first dispatched quantity; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T015 — Quantity & invoice final gate; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T014-R1 — Invoice header LineCount vs TotalQuantity semantic repair; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T014 — Quantity & financial lock completion (lock registry, UoM admin, invoice header aggregates); under repair via R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T013-R1 — Offer/Fulfillment EF history replay-safe + Floor/Ceiling/Nearest runtime proof; USER_VISUAL_ACCEPTED=NO
TB-P09-T013 — Quantity decimal foundation (UoM, product policy, offer min/max, global rounding, numeric(18,6)); under repair via R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T012 — Reference-locked fulfillment UX (capability-driven selection/bulk/kebab/shipment); USER_VISUAL_ACCEPTED=NO
TB-P09-T011 — Orders Fulfillment UX Regression Repair (cancel precedence, grid refresh, bulk toolbar, single-selection, row kebab, return display dedup); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T010 — Fulfillment Action Scope & Sequence Repair (whole-order pack/process hide, pack-after-processing, exact pack_selected, row kebab); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T009-R1 — Cancelled-order restore financial gate (block restore after completed seller payout/settlement); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T009 — Order Corrective Actions (rollback matrix, payment/shipment/cancel restore, whole-order dedupe); Architect-accepted after R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T008 — Shipping & Delivery Operations Center — Cross-Order Work Queue; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T007 — Order Financial History + Scope-Aware Operational History; USER_VISUAL_ACCEPTED=NO
TB-P09-T006-R1 — Offer Return Policy + Shipping Provider Forms Final Runtime Proof; USER_VISUAL_ACCEPTED=NO
TB-P09-T006 — Offer return policy + dynamic shipping provider forms; USER_VISUAL_ACCEPTED=NO
TB-P09-T005-R1 — Seller-Scoped Fulfillment Final Proof runtime matrix; USER_VISUAL_ACCEPTED=NO
TB-P09-T005 — Seller-scoped line/quantity fulfillment ops, reverse rules, return deadline UI; USER_VISUAL_ACCEPTED=NO
TB-P09-T004 — Orders Operational UX Foundation (grid kebab ops, اقلام و ارسال shell, shipment modal; USER_VISUAL_ACCEPTED=NO)
TB-P09-T003-R3 — Admin Orders Repair R3 (production-safe configurable manual/card-to-card payment; USER_VISUAL_ACCEPTED=NO)
TB-P09-T003-R2 — Admin Orders Repair R2 (manual/card-to-card confirm, fresh order E2E, list return/refund visibility; USER_VISUAL_ACCEPTED=NO)
TB-P09-T003-R1 — Admin Orders Repair (ops matrix/menu portal, grid maxWidth, note delete rule, clean E2E; USER_VISUAL_ACCEPTED=NO)
TB-P09-T003 — Admin Orders Final Operational Gate (View-only nav + E2E verify + defect-only shipment fixes; USER_VISUAL_ACCEPTED=NO)
TB-P09-T002-R1 — Order Operational History Repair (human actor resolution; USER_VISUAL_ACCEPTED=NO)
TB-P09-T002 — Order Detail Operational Completeness (notes, operational history, invoice/receipt; Architect-accepted)
TB-P09-T001-R2 — Marketplace settlement event-path proof (MassTransit/outbox handlers; USER_VISUAL_ACCEPTED=NO)
TB-P09-T001-R1 — Order Operations Foundation Repair (domain cancel guard + real post-settlement return runtime; USER_VISUAL_ACCEPTED=NO)
TB-P09-T001 — Order Operations Foundation (contextual admin actions, return eligibility SoT, settlement-safe returns; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R5 — P08 repair (locale backlink, history clarity, fonts/video/category UX; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R4 — P08 repair (article locale identity, history pager, CKEditor fonts, DAM contentTypePrefix video; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R3 — P08 final visual/functional repair (DB languages, loading feedback, Full Edit density, author readiness, CKEditor font/DAM; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R2 — P08 final visual repair (Full Edit, author combobox, AppCategoryTree picker, CKEditor CMS toolbar, history wording; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R1 — Content demo mojibake/???? label cleanup (repair of T016 visual cleanliness; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016 — P08 Content Final Gate (technical/worker gate; USER_VISUAL_ACCEPTED=NO)
TB-P08-T015 — Article comments moderation, contextual help, wording cleanup, workspace polish — Architect-accepted
TB-P08-T014 — Article publication readiness, draft preview, publish lifecycle, history, Jalali schedule — Architect-accepted
TB-P08-T013 — Content taxonomy two-level categories + ContentTags — Architect-accepted
TB-P08-T012-R1 — Article TipTap → CKEditor 5 + DAM (repair of T012 editor) — Architect-accepted
TB-P08-T012 — Article workspace editor, media/SEO/category save repairs (under repair via R1; editor replaced)
TB-P08-T011 — Article language tabs, draft-first create, author picker, locale policy — Architect-accepted
TB-P08-T010-R1 — P08 Final Gate Repair (seed scope, public Content APIs, locale 308) — Architect-accepted
TB-P08-T010 — Content Visual Final Gate (USER_VISUAL_ACCEPTED=NO)
TB-P08-T009-R2 — Content Authorization Fail-Closed Repair
TB-P08-T009-R1 — Content Permission Enforcement Repair (backend content.* authorization)
TB-P08-T009 — Content Integration Gate (under repair via R2)
TB-P08-T008 — Public Content Taxonomy + Author routes + storefront home locale consistency
TB-P08-T007-R2 — Article action dialog completion (publish/unpublish canonical Dialog)
TB-P08-T007-R1 — Article destructive UX repair (canonical Dialog, no window.confirm)
TB-P08-T007 — Article CRUD UX (dedicated create route, VIEW/EDIT, safe delete/archive)
TB-P08-T006 — Content SEO + Publication + Locale Routing (public blog routes, scheduling, sitemap)
TB-P08-T005 — Article Media DAM (inline editor, gallery, featured, SEO image)
TB-P08-T004 — Article Editor Workspace (language-first create, TipTap body, tabs, locale lock)
TB-P08-T003 — Content Author management (public profiles, DAM, Article relation)
TB-P08-T002 — Article Category Taxonomy (Content-owned, language-aware AppCategoryTree)
TB-P08-T001 — under repair (parent; NOT accepted)
TB-P08-T001-R2 — Language reference integrity (Code/UrlPrefix lock after Content use)
TB-P08-T001-R1 — Persisted Language registry repair (DB-backed localization.languages)
TB-P07-T043 — Order Detail visual fidelity polish; T042-R1 data wiring preserved
```

Last Architect-Accepted Task:

```text
TB-P10-T004-R5
```

USER_VISUAL_ACCEPTED:

```text
YES
```

Worker Next State:

```text
IDLE — waits for Bridge Task (no invented next task; do NOT invent TB-P10-T023; do NOT start TB-P11-T002; TB-P11-T001 retained for later; Builder USER_VISUAL_ACCEPTED=NO)
```

Current Gate:

```text
TB-P05-GATE = ACCEPTED
```

Next Phase:

```text
P07 continuation (Architect-issued after T001 ACCEPT)
```

Gate State:

```text
TB-P01-GATE = ACCEPTED
TB-P02-GATE = ACCEPTED
TB-P03-GATE = ACCEPTED
TB-P04-GATE = ACCEPTED
TB-P05-GATE = ACCEPTED
```

Issued but not accepted:

```text
TB-P06-T001 = ACCEPTED
TB-P06-T002 = ACCEPTED
TB-P06-T003 = SUPERSEDED_WRONG_TRANSPORT
TB-P06-T003-R1 = ACCEPTED
TB-P06-T004 = ACCEPTED
TB-P06-T005 = ACCEPTED
TB-P06-T006 = AWAITING_ARCHITECT_ACCEPT
TB-P06-T007 = AWAITING_ARCHITECT_ACCEPT
TB-P06-T010-R1 = AWAITING_ARCHITECT_ACCEPT
TB-P06-T011 = ACCEPTED
TB-P06-T011-R1 = ACCEPTED
TB-P06-T011-R2 = ACCEPTED
TB-P06-T011-R3 = ACCEPTED
TB-P06-T012 = ACCEPTED
TB-P06-T013 = ACCEPTED
TB-P06-T014 = ACCEPTED
TB-P06-T015 = ACCEPTED
TB-P06-T016 = ACCEPTED
TB-P06-T016-R1 = ACCEPTED
TB-P06-T017 = ACCEPTED
TB-P06-T018 = ACCEPTED
TB-P06-T019 = SUPERSEDED_BY_ARCHITECT_RESCOPE
TB-P06-T019-R1 = ACCEPTED
TB-P06-T020 = ACCEPTED
TB-P06-T021 = ACCEPTED
TB-P06-T022 = ACCEPTED
TB-P06-T023 = CLOSED_BY_REPAIR
TB-P06-T023-R1 = ACCEPTED
TB-P06-T024 = REPAIRED_BY_TB-P06-T024-R1
TB-P06-T024-R1 = FUNCTIONALLY_COMPLETE
TB-P06-T024-R2 = ACCEPTED
TB-P06-T025 = ACCEPTED
TB-P06-T026 = REPAIRED_BY_TB-P06-T026-R1
TB-P06-T026-R1 = ACCEPTED
TB-P06-T027 = ACCEPTED
TB-P06-T028 = REPAIRED_BY_TB-P06-T028-R1
TB-P06-T028-R1 = ACCEPTED
TB-P06-T029 = ACCEPTED
COMMERCIAL_READINESS_GATE = PASSED
TB-P07-T001 = SUPERSEDED
TB-P07-T001-R1 = SUPERSEDED
TB-P07-T001-R2 = SUPERSEDED
TB-P07-T001-R3 = SUPERSEDED
TB-P07-T001-R4 = SUPERSEDED
TB-P07-T001-R5 = ACCEPTED
TB-P07-T002 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R1 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R2 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R3 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R4 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R5 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R6 = AWAITING_ARCHITECT_ACCEPT
SERVER_GRID_COMMUNITY_FEATURES = LIVE
CURRENT_FOCUS = PROFESSIONAL_DATA_GRID
CANONICAL_GRID = APP_OWNED_AG_GRID_COMMUNITY_WRAPPER
AG_GRID_ENTERPRISE = FORBIDDEN
SERVER_GRID_QUERY_CONTRACT = LIVE
FIRST_REAL_ADMIN_INTEGRATION = LIVE
BACKEND_FEATURE_EXPANSION = FROZEN
CATEGORY_ATTRIBUTE_SCHEMA = LIVE
CATEGORY_ATTRIBUTE_INHERITANCE = LIVE
PRODUCT_TYPED_ATTRIBUTES = LIVE
PRODUCT_VARIANT_AXES = LIVE
VARIANT_COMBINATION_FOUNDATION = LIVE
PRODUCT_SEO = LIVE
PRODUCT_PUBLISHING = LIVE
FULL_VARIANT_MATRIX = DEFERRED
FACETED_SEARCH_INTEGRATION = DEFERRED
SPICEDB_AUTHORIZATION = LIVE
ACCESS_CONTROL_CENTER = LIVE
ACCESS_CONTROL_DEMO_SEED = LIVE_DEV_ONLY
ACCESS_CONTROL_BROWSER_PROOF = LIVE
ACCESS_CONTROL_USER_PREVIEW = READY
SUPPORT_BACKEND = LIVE
CUSTOMER_TICKETS = LIVE
SELLER_TICKETS = LIVE
ADMIN_SUPPORT = LIVE
SUPPORT_NOTIFICATIONS = LIVE
SUPPORT_ACCESS_CONTROL = LIVE
SUPPORT_DEMO_SEED = LIVE_DEV_ONLY
SUPPORT_USER_PREVIEW = READY
WALLET_LEDGER = LIVE
GIFTCARD = LIVE
CUSTOMER_WALLET_UI = LIVE
ADMIN_WALLET_UI = LIVE
WALLET_DEMO_SEED = LIVE_DEV_ONLY
WALLET_BROWSER_PROOF = LIVE
WALLET_USER_PREVIEW = READY
WALLET_CHECKOUT = LIVE
WALLET_CHECKOUT_FULL_PAYMENT = LIVE
REFUND_TO_WALLET = LIVE
WALLET_MIXED_TENDER = DEFERRED
WALLET_CHECKOUT_BROWSER_PROOF = LIVE
REFUND_TO_WALLET_BROWSER_PROOF = LIVE
WALLET_CHECKOUT_USER_PREVIEW = READY
CUSTOMER_SETTINGS = LIVE
SELLER_SETTINGS = LIVE
ADMIN_PROFILE_SETTINGS = LIVE
REAL_PREFERENCES = PARTIAL
NOTIFICATION_PREFERENCES = DEFERRED
SECURITY_SETTINGS = DEFERRED
SETTINGS_DEMO_SEED = LIVE_DEV_ONLY
SETTINGS_USER_PREVIEW = READY
ADMIN_ROLE_MANAGEMENT = LIVE
SELLER_ROLE_MANAGEMENT = LIVE
SELLER_PERMISSION_CEILING = LIVE
USER_ROLE_ASSIGNMENT = LIVE
UI_CAPABILITY_PROJECTION = LIVE
RESOURCE_SCOPE_FOUNDATION = LIVE
REAL_DATA_SCOPE_SELECTORS = LIVE
CATEGORY_SCOPED_ORDER_POLICY = LIVE_FOUNDATION
CATEGORY_SCOPED_ORDER_ACCESS = LIVE
CATEGORY_SCOPED_ORDER_FILTERING = LIVE
PRODUCT_SALE_FLOW = LIVE

SELLER_PRODUCT_MANAGEMENT = LIVE
SELLER_OFFER_MANAGEMENT = LIVE
PRICING_PATH = LIVE
INVENTORY_PATH = LIVE
CART_CHECKOUT_PAYMENT = LIVE
SELLER_ORDER_FULFILLMENT = LIVE
CUSTOMER_ORDER_TRACKING = LIVE
ADVANCED_VARIANT_ARCHITECTURE = DEFERRED
FULL_VARIANT_MATRIX = DEFERRED
FACETED_SEARCH_INTEGRATION = DEFERRED
CATEGORY_ATTRIBUTE_SCHEMA = LIVE
CATEGORY_ATTRIBUTE_INHERITANCE = LIVE
PRODUCT_TYPED_ATTRIBUTES = LIVE
PRODUCT_VARIANT_AXES = LIVE
VARIANT_COMBINATION_FOUNDATION = LIVE
PRODUCT_SEO = LIVE
PRODUCT_PUBLISHING = LIVE
SELLABLE_PRODUCT_FLOW_LIVE = YES
SELLABLE_DEMO = YES
PRODUCTION_PAYMENT_FOUNDATION_READY = YES
REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED = YES
REAL_BANK_PAYMENT_PROVEN = NO
PRODUCTION_GO_LIVE_READY = NO
PUBLIC_LOCALE_ROUTING = PREFIXED
SUPPORTED_LOCALES = fa,en
MULTILINGUAL_SEO = LIVE
HREFLANG = REAL_VARIANTS_ONLY
UNPREFIXED_PUBLIC_URLS = REDIRECTED
PAGE_COMPOSITION = LOCALE_ROUTE_COMPATIBLE
CONTENT_BACKEND = LIVE
BLOG_CONTENT = LIVE
BLOG_PUBLIC_UI = LIVE
ARTICLE_DETAIL_UI = LIVE
ADMIN_CONTENT_UI = LIVE
STOREFRONT_UI = LIVE_AUDITED
CUSTOMER_UI = LIVE_AUDITED
SELLER_UI = LIVE_AUDITED
ADMIN_UI = LIVE_AUDITED
MULTILINGUAL_UI = AUDITED
RTL_LTR = AUDITED
MULTILINGUAL_SEO = AUDITED
VISUAL_CONTRACT = SHOPEIVA_LOCKED
NEW_UI_RULE = SOURCE_DERIVED_NATIVE_FIT
COMMERCIAL_UI_READINESS = AUDITED
FAKE_DATA_ACTION_AUDIT = AUDITED
CSS_JS_MOTION_PARITY = AUDITED
COMMERCIAL_PANEL_WAVE1_LIVE = YES
COMMERCIAL_PANEL_WAVE2_LIVE = YES
SHARED_STORY_MANAGEMENT_LIVE = YES
SELLER_PROMOTIONS = LIVE
SELLER_REVIEWS = LIVE
ADMIN_PROMOTIONS = LIVE
ADMIN_REVIEWS = LIVE
TRANSACTIONAL_NOTIFICATIONS_LIVE = YES
NOTIFICATION_BACKEND = LIVE
CUSTOMER_NOTIFICATION_UI = LIVE
SELLER_NOTIFICATION_UI = LIVE
NOTIFICATION_UNREAD = LIVE
NOTIFICATION_DEEP_LINKS = LIVE
NOTIFICATION_VISUAL_PARITY = PROVEN
TRANSACTIONAL_NOTIFICATIONS_VISUALLY_ACCEPTABLE_FOR_ARCHITECT_REVIEW = YES
REALTIME_NOTIFICATIONS = DEFERRED
FAKE_NOTIFICATIONS = FORBIDDEN
PANEL_NAVIGATION = HONEST_LIVE_ONLY

SETTLEMENT_BACKEND = LIVE
PAYOUT_BACKEND = LIVE_FOUNDATION
SETTLEMENT_SELLER_UI = LIVE
SETTLEMENT_ADMIN_UI = LIVE
SINGLE_STORE_SETTLEMENT = NOT_APPLICABLE
RETURNS_BACKEND = LIVE
REFUNDS_BACKEND = LIVE_FOUNDATION
INVENTORY_RESTOCK = LIVE
RETURNS_CUSTOMER_UI = LIVE
RETURNS_SELLER_UI = LIVE
RETURNS_ADMIN_UI = LIVE
AUTH_SESSION_STORAGE = HTTPONLY_COOKIE_SERVER_SIDE_PROPAGATION
PRODUCTION_OTP = PROVIDER_BACKED_FAIL_CLOSED
AUTHENTICATION = BEARER_SESSION_ID
AUTHORIZATION = SPICEDB_REBAC
MESSAGING_TRANSPORT = MASSTRANSIT_POSTGRESQL_SQL_TRANSPORT
HOME_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PDP_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PRODUCT_FULLY_READY = NO
```

Accepted ledger (selected):

```text
TB-P02-T001 = ACCEPTED
TB-P02-T002 = ACCEPTED
TB-P02-T003 = ACCEPTED
TB-P02-T004 = ACCEPTED
TB-P02-T005 = ACCEPTED
TB-P02-GATE = ACCEPTED
TB-P03-T001 = ACCEPTED
TB-P03-T002 = ACCEPTED
TB-P03-T003 = ACCEPTED
TB-P03-T004 = ACCEPTED
TB-P03-T005 = ACCEPTED
TB-P03-T006 = ACCEPTED
TB-P03-T007 = ACCEPTED
TB-P03-T008 = ACCEPTED
TB-P03-T009 = ACCEPTED
TB-P03-GATE = ACCEPTED
TB-P04-T001 = ACCEPTED
TB-P04-T002 = ACCEPTED
TB-P04-T003 = ACCEPTED
TB-P04-T004 = ACCEPTED
TB-P04-T005 = ACCEPTED
TB-P04-T006 = ACCEPTED
TB-P04-T007 = ACCEPTED
TB-P04-T008 = ACCEPTED
TB-P04-T009 = ACCEPTED
TB-P04-T010 = ACCEPTED
TB-P04-GATE = ACCEPTED
```

Observability / Error Handling Foundation:

```text
COMPLETE (Architect accepted TB-P01-T002)
```

Tenant / Edition / Database Resolution Foundation:

```text
COMPLETE (Architect accepted TB-P01-T003)
```

PostgreSQL Persistence Foundation:

```text
COMPLETE (Architect accepted TB-P01-T004)
```

Persian Code Documentation Standard:

```text
COMPLETE (Architect accepted TB-P01-T005)
```

Outbox / Domain Events / Background Foundation:

```text
COMPLETE (Architect accepted TB-P01-T006)
```

MassTransit PostgreSQL SQL Transport Alignment:

```text
COMPLETE (Architect accepted TB-P01-T007)
```

Cache Abstraction Foundation:

```text
COMPLETE (Architect accepted TB-P01-T008)
```

Module Composition & Boundary Enforcement:

```text
COMPLETE (Architect accepted TB-P01-T009)
```

P01 Platform Foundation Gate:

```text
COMPLETE (Architect accepted TB-P01-GATE)
```

Identity & Authentication Foundation:

```text
COMPLETE (Architect accepted TB-P02-T001)
```

SpiceDB Authorization Foundation:

```text
COMPLETE (Architect accepted TB-P02-T002)
```

Party / Organization / Membership Foundation:

```text
COMPLETE (Architect accepted TB-P02-T003)
```

Session / Token / Credential Lifecycle:

```text
COMPLETE (Architect accepted TB-P02-T004)
```

Authentication HTTP Boundary:

```text
COMPLETE (Architect accepted TB-P02-T005)
```

P02 Identity / Authorization Gate:

```text
COMPLETE (Architect accepted TB-P02-GATE)
```

Catalog Product & Variant Foundation:

```text
COMPLETE (Architect accepted TB-P03-T001)
```

Seller Offer & Listing Foundation:

```text
COMPLETE (Architect accepted TB-P03-T002)
```

Pricing Foundation:

```text
COMPLETE (Architect accepted TB-P03-T003)
```

Inventory Foundation:

```text
COMPLETE (Architect accepted TB-P03-T004)
```

Cart Foundation:

```text
COMPLETE (Architect accepted TB-P03-T005)
```

Checkout & Order Foundation:

```text
COMPLETE (Architect accepted TB-P03-T006)
```

Tax Calculation Foundation:

```text
COMPLETE (Architect accepted TB-P03-T007)
```

Payment Foundation:

```text
COMPLETE (Architect accepted TB-P03-T008)
```

Promotion & Discount Foundation:

```text
COMPLETE (Architect accepted TB-P03-T009)
```

P03 Commerce Core Gate:

```text
COMPLETE (Architect accepted TB-P03-GATE)
```

P04 Experience Foundation:

```text
COMPLETE (Architect accepted TB-P04-GATE)
```

P05 Operational Surface Integration:

```text
COMPLETE (Architect accepted TB-P05-GATE)
```

P06 Core API Integration / Operational Hardening:

```text
IN_PROGRESS (P06 = COMPLETE; TB-P06-T029 = ACCEPTED; COMMERCIAL_READINESS_GATE = PASSED; P07 = IN_PROGRESS; TB-P07-T001/R1/R2/R3/R4 = SUPERSEDED; TB-P07-T001-R5 = AWAITING_ARCHITECT_ACCEPT — Admin+Seller UI/UX polish; CURRENT_FOCUS = UI_UX_ONLY; ADMIN_UI = PRIORITY_1; SELLER_UI = PRIORITY_2; BACKEND_FEATURE_EXPANSION = FROZEN; category attribute + variant axes foundation KEEP; FULL_VARIANT_MATRIX = DEFERRED; FACETED_SEARCH_INTEGRATION = DEFERRED; VISUAL_CONTRACT = SHOPEIVA_LOCKED; not USER_VISUAL_ACCEPTED; not PRODUCT_FULLY_READY; not PRODUCTION_GO_LIVE_READY; not SELLER_PANEL_COMPLETE; not FULL_VARIANT_MATRIX_LIVE)
```

Design System Foundation:

```text
COMPLETE (Architect accepted TB-P04-T002)
```

Professional Data Grid Foundation:

```text
COMPLETE (Architect accepted TB-P04-T003)
```

Workspace Interaction Patterns:

```text
COMPLETE (Architect accepted TB-P04-T004)
```

Admin Product Workspace:

```text
COMPLETE (TB-P04-T005 Architect ACCEPTED as live functional/interaction foundation; custom Admin visual language is not the final Tooba target)
```

ErrorState retry label i18n gap:

```text
RESOLVED (bounded retryLabel on ErrorState in TB-P04-T004)
```

Grid virtualization:

```text
DEFERRED_NON_BLOCKING
```

Project-wide documentation rule:

```text
All required Tooba-owned Classes / Interfaces / Methods / Properties
must have strong Persian documentation.
```

Known Blockers:

```text
NONE
```

Architecture Status:

```text
CONFIRMED: Modular Monolith with mandatory microservice-readiness
UNRESOLVED: exact bounded contexts, tenant implementation code/ADR lock, locale list, and other P00 design details listed below
```

## Confirmed Requirements

Recorded from Architect-authorized TB-P00-T000. These are durable requirements, not implemented product.

### Product / Quality

- Commercial multilingual e-commerce product.
- Must reach a sellable state quickly without sacrificing production quality.
- SEO is top-tier and non-negotiable.
- UI/UX is commercially critical, production-grade, mobile-intentional, accessible, multilingual, and must not degrade into developer-skeleton screens.
- Digikala/Amazon are references, not architecture truth, and must not be copied.
- Purchased template is an adaptation/reference input, not architecture truth.

### Editions / Deployment

Marketplace edition (currently stated model):

```text
one dedicated marketplace publish/deployment
one marketplace database
multi-seller marketplace behavior
```

Single-store commercial edition (currently stated requirement):

```text
one shared publish/deployment for all single-store customers
many domains
incoming domain -> store/tenant resolution -> correct database
one database per customer/store
theme per store
not multi-vendor
```

Tenant implementation is not finalized.

### Architecture

```text
Modular Monolith
```

Mandatory microservice-readiness:

- module/domain data ownership;
- no direct cross-module DB joins;
- no cross-module table/repository access;
- collaboration through explicit contracts/interfaces/gateways/events;
- future in-process gateway replacement by remote integration without rewriting consuming business logic.

### Other confirmed directions

- Content is broad Content, not Blog-only.
- Semantic Content != Page Composition; landing pages must be reusable/composable.
- Identity identifiers: username, phone, email, national ID, future identifiers.
- Authentication: password, OTP login, optional 2FA/MFA, future external identity providers; extensible to Keycloak without coupling core identity to Keycloak.
- Authorization direction: SpiceDB; relationship-based; do not collapse into fixed role columns.
- Full B2B is after first sellable release; P00 must preserve Party/Organization foundations.
- Never model product price as one scalar. Locale != Market != Currency != Tax Jurisdiction.
- USER Tax policy (architecture input, not implemented law): Iran first-market emphasis; UK/other markets readiness; tax-exclusive base; Tooba calculates tax; configurable effective-dated percentage rules; context override only if enabled; tax-exempt supported; no hard-coded rate/date/law; B2B VAT/invoice out of initial phase. See `docs/architecture/26-tax-architecture.md`.
- P00 must analyze Catalog Product vs Seller Offer / Listing; do not prematurely merge them.
- Search: initial PostgreSQL Full Text Search; future Elasticsearch / OpenSearch; domain logic must not couple to PostgreSQL search internals.
- Caching abstracted so Redis can be added later without redesign. Initial hosting may be public/shared; later dedicated.
- Observability: OpenTelemetry, advanced technical logging, metrics, traces, audit logging. Technical logs and audit/business events remain conceptually separate.
- First-party analytics planned in addition to third-party analytics.
- Media: original + transformed/cached variants; swappable storage/CDN.
- Customer-facing AI agent required; grounded/RAG; no unrestricted direct AI access to internal DBs.
- Product is multilingual. Do not bind Locale to Market or Currency.

## Unresolved P00 Decisions

- exact bounded contexts;
- tenant implementation code, control-plane storage, and ADR lock of the T003 candidate (see `docs/architecture/02-edition-tenant-deployment.md`);
- initial locale list;
- indexation / canonical / hreflang / structured data / sitemap ownership details;
- Identity uniqueness policy per identifier type/edition and whether one Identity may span Single-Store tenants (see `docs/architecture/04-identity-authentication.md`);
- SpiceDB relationship model;
- Catalog Product vs Seller Offer design;
- Pricing/Market/Currency model details (precision, rounding, FX provenance, history);
- Inventory, Cart/Checkout/Order, Payment designs;
- Content + Page Composition designs;
- Media pipeline provider choices (see `docs/architecture/15-media-image-pipeline.md`);
- First-party analytics implementation (see `docs/architecture/16-first-party-analytics.md`);
- AI/RAG retrieval contracts (see `docs/architecture/17-ai-assistant-rag.md`);
- Observability/audit implementation (see `docs/architecture/18-observability-logging-audit.md`);
- Caching/infrastructure abstractions (see `docs/architecture/19-caching-infrastructure-abstractions.md` and P01 foundation `docs/architecture/35-cache-abstraction-foundation.md`);
- frontend/template adaptation strategy (see `docs/architecture/20-frontend-ux-template-adaptation.md`);
- whether/how `shopeiva.zip` is present as a later inventory input.

## Purchased Template

Architect has received `shopeiva.zip`.

Repository presence at TB-P00-T000 execution:

```text
NOT_PRESENT_IN_REPOSITORY
```

Do not copy, unzip, vendor, or modify it until an authorized later P00 inventory task.

## Repository State

```text
Primary branch: main
Required after each task: HEAD == origin/main
```

Do not record a recursive commit SHA in this file.

## Exact Resume Rule

1. Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

2. Read:

```text
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
```

3. Determine current phase, last accepted task, issued-but-unaccepted task, blockers, locked requirements, unresolved decisions, and this resume rule from the repository.

4. Execute only a complete Architect-authorized envelope (`BEGIN_TOOBA_CURSOR_TASK_V1` / `BEGIN_TOOBA_CURSOR_GATE_V1`).

5. Never invent the next task from memory. Do not execute TB-P03-T002 or a P03 Gate unless Architect issues that exact envelope.

P00 discovery inputs (not locked architecture):

```text
docs/architecture/00-technical-inventory.md
docs/architecture/01-capability-domain-map.md
docs/architecture/02-edition-tenant-deployment.md
docs/architecture/03-data-ownership-and-module-contracts.md
docs/architecture/04-identity-authentication.md
docs/architecture/05-spicedb-authorization.md
docs/architecture/06-party-organization-b2b-foundation.md
docs/architecture/07-catalog-product-offer.md
docs/architecture/08-pricing-market-currency.md
docs/architecture/09-inventory-availability-reservation.md
docs/architecture/10-cart-checkout-order.md
docs/architecture/11-payment.md
docs/architecture/12-content-page-composition.md
docs/architecture/13-seo-architecture.md
docs/architecture/14-search-indexing.md
docs/architecture/15-media-image-pipeline.md
docs/architecture/16-first-party-analytics.md
docs/architecture/17-ai-assistant-rag.md
docs/architecture/18-observability-logging-audit.md
docs/architecture/19-caching-infrastructure-abstractions.md
docs/architecture/20-frontend-ux-template-adaptation.md
docs/architecture/21-fulfillment.md
docs/architecture/22-promotion-discount.md
docs/architecture/23-p00-capability-gap-review.md
docs/architecture/24-reviews-ratings.md
docs/architecture/25-returns-rma.md
docs/architecture/26-tax-architecture.md
docs/architecture/27-p00-gate-review.md
docs/architecture/28-platform-foundation-bootstrap.md
docs/architecture/29-observability-error-foundation.md
docs/architecture/30-tenant-edition-database-foundation.md
docs/architecture/31-postgresql-persistence-foundation.md
docs/architecture/32-persian-code-documentation-standard.md
docs/architecture/33-outbox-domain-events-background-foundation.md
docs/architecture/34-masstransit-postgresql-sql-transport.md
docs/architecture/35-cache-abstraction-foundation.md
docs/architecture/36-module-composition-boundary-enforcement.md
docs/architecture/37-identity-authentication-foundation.md
docs/architecture/38-spicedb-authorization-foundation.md
docs/architecture/39-party-organization-membership-foundation.md
docs/architecture/40-session-token-credential-lifecycle.md
docs/architecture/41-authentication-http-boundary.md
docs/architecture/42-catalog-product-variant-foundation.md
docs/architecture/43-seller-offer-listing-foundation.md
docs/architecture/44-pricing-foundation.md
docs/architecture/45-inventory-foundation.md
docs/architecture/46-cart-foundation.md
docs/architecture/47-checkout-order-foundation.md
docs/architecture/48-tax-calculation-foundation.md
docs/architecture/49-payment-foundation.md
docs/architecture/50-promotion-discount-foundation.md
docs/architecture/51-shopeiva-study-reuse-map.md
docs/architecture/52-design-system-foundation.md
docs/architecture/53-professional-data-grid-foundation.md
docs/architecture/54-workspace-interaction-patterns.md
docs/architecture/55-admin-product-workspace.md
docs/architecture/56-storefront-live-slice.md
```

Bridge-Wake-V1 task audit artifact for the current governance work:

```text
docs/ai/tasks/TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1.task.md
```

Historical Bridge-V2 governance migration artifact (evidence only):

```text
docs/ai/tasks/TB-P05-GOV-MIGRATION-BRIDGE-V2.task.md
```

Recorded principle: Seller authorization must bind authenticated actor to Seller Party; requested SellerPartyId is context, never authority.

Resume: `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1`; channel `tooba-main`. P05 = COMPLETE. P06 = COMPLETE. P07 = IN_PROGRESS. TB-P07-T017 = ACCEPTED (Product SEO). TB-P07-T018 / T018-R1 = ACCEPTED (publishing lifecycle). TB-P07-T019 = Product History / Audit (`docs/catalog/PRODUCT-HISTORY.md`). TB-P07-T020 = Product Admin visual polish (workspace edit-across-tabs, shell/list/panel tokens; `docs/evidence/TB-P07-T020/`). Runtimes: Host `:5088`, FE `:3000`, Shopeiva `:3001`. Product Admin: `http://localhost:3000/fa/admin/products`. `USER_VISUAL_ACCEPTED` = NO.


## Current focus (TB-P07-T041-R1)

```text
Admin Grid Server Query Repair — DB-native filter/sort/Skip/Take for NON_TRIVIAL lists (parent TB-P07-T041)
Last accepted baseline: TB-P07-T039; TB-P07-T040 = NOT_ACCEPTED; TB-P07-T041 under repair
USER_VISUAL_ACCEPTED=NO
Worker next = IDLE / waits for Bridge Task (no invented next task)
```

Final HEAD/origin (TB-P07-T041-R1):

```text
65e93945
```
