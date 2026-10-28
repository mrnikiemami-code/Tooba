# RESULT — TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R1

Status: **PARTIAL** — one real Platform-owned captured delivery now prices and completes a REAL order end to end
(manual Pending), while the two Seller-owned partitions remain blocked at seller-scope capability provisioning.

## Delivered (actual, measured on this task Host 5190 / DB `tooba_marketplace_tb239`)

| # | Item | Actual outcome |
|---|------|----------------|
| 1 | Canonical languages | `localization.languages` **2** (`fa-IR` default, `en-US`) created by the EXISTING `LanguageBootstrapDevelopmentSeed.PostMigration.EnsureAsync` inside the assigned Marketplace scope |
| 2 | Real geography | `geography` 1 country / 1 area / 1 city + 6 name translations from the new Geography-owned Development seed: `IR`, `IR-DEV-TEH`, `IR-DEV-TEH-THR` (documented local seed codes, not official subdivisions) |
| 3 | Fulfillment catalog | type `heavy` **ACTIVE** (`01a181c7-7863-7000-925f-b6f64ed4694b`), service `post` (`01a181c7-3366-7000-b072-b41701e2a0ff`) + option `standard`, one-level method `post:standard` **ACTIVE** |
| 4 | Assignments | `ARM-LN-01` → Platform, `ARM-SC-01` → Seller, `DGS-LN-01` → Seller (all on `heavy`) |
| 5 | Platform capture | draft `01a181ca-e730-7000-bfaf-a5d9e5778dee` rev 6 → snapshot `01a181ca-e8f8-7000-840c-1a09cbb105e5`, `Selected`, selectionVersion 1 |
| 6 | Priced owner partition | projection 200 with owner-keyed group `cap\|platform\|marketplace\|…`, type `Heavy`, goods **2,016,500**, freight **95,000**, packaging **5,000**, payable **100,000**, earliest `2026-10-31`, real snapshot/area/rate ids |
| 7 | Selection → preview → StartPayment | `PUT /shipping/selection` **200** (revision 1), `GET /payment-draft` **200**, `POST /start-payment` **200** |
| 8 | Durable order + manual Pending | `checkoutId 01a181cf-afd3-7000-a33f-7ea2262fbe6e`, `paymentStatus Pending`, `providerCode manual`, `awaitingManual=1`, amount **2,116,500** IRR (= 2,016,500 goods + 100,000 payable), `reservationCount 1` |
| 9 | Order-storage evidence | `order.order_shipping_groups`: owner kind `1` (Platform), freight 95,000, packaging 5,000, payable 100,000, collect 0, source revision 6, selection version 1, receiving `2026-10-31`, ticks `324000000000`–`612000000000`, `Asia/Tehran` |

## Not delivered — exact blockers

1. **Seller-owned authoring/publication (blocks two of three owner partitions).** The seller draft route
   (`POST /v1/seller/shipping-services/authoring-drafts`) refuses `403 shipping.authoring.owner_scope_denied`:
   the effective seller grant needs a Seller-scope role + `role_permissions` + `user_role_assignments` +
   `platform_seller_ceilings` row. The admin access-control surface hard-codes `AccessOwnerScopeKind.Platform`
   (`CreateRoleAsync`/`AssignAsync`), and the seller-scope surface requires a seller `accesscontrol.manage` that no
   seller currently holds. No lawful API path exists in one session; I did not bypass it with SQL.
   Consequence: groups `cap|seller|…` return `storefront.captured_preview.no_selected_capture`, and a 3-line
   mixed-owner cart cannot be selected (all-or-none), so the **mixed-owner** order is unmet.
2. **Partial-applicability coupon** — depends on the mixed-owner cart; not exercised.
3. **Duplicate same-key StartPayment** — replay returned the SAME durable `checkoutId` (no duplicate checkout) but
   `checkoutReplayed:false` with a NEW `paymentId`/`attemptId`, i.e. a second payment attempt for one checkout.
   Reported honestly, not repaired (payment-attempt idempotency is outside this task's allowed edits).
4. **Existing-UI captures (1440/390/360, payment stepper)** — no time remained after the API closure; **no PNG
   was produced**, and none is claimed.
5. **Request-binding quirk observed**: `StorefrontStartPaymentBody` bound case-sensitively here — camelCase JSON
   was silently ignored (defaulted version 0 → `checkout.version.conflict`) while PascalCase bound. Not changed.

## Preserved

No change to user `3000/5088`, handoff `3100/5188`, or `D:\temp\tb238-handoff-chrome`. No DB drop/reset/reseed, no
schema/migration/baseline edit, no business SQL, no shared history rewrite. Checked-in gates stay OFF; only named
local env vars were ON. Seller/offer/stock seeds not re-run. `TB185` paused/default-OFF/PendingAllocation.

## Timing

240-report: 123 focused minutes consumed before this continuation; this slice added ≈62 minutes of the same
240-minute ceiling → **≈185 consumed, ≈55 remaining**. STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.
