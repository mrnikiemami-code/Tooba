# 19 — Authorization proof (TB-P06-T020)

Date: 2026-08-27  
Primary citations: `PromotionPanelTests.cs`, `ReviewsFoundationTests.cs` (+ Postgres seller scope).

## Promotions — `PromotionPanelTests`

| Case | Test / assertion | Result claimed |
|---|---|---|
| Seller own create + list | `Seller_own_create_list_activate_and_admin_deactivate_with_foreign_deny` — `CreateForSellerAsync` forces `SellerPartyId`; list A contains; list B excludes | Own allow |
| Foreign get / activate / update | Foreign seller `GetForSellerAsync` → null; `ActivateForSellerAsync` / `UpdateForSellerAsync` throw | Foreign deny |
| Activate + coupon evaluate | Own activate; `EvaluateAsync` with code applies discount; without code → 0 | Discount safety |
| Admin list / filter / deactivate | `ListForAdminAsync`; filter by seller; `DeactivateForAdminAsync` → Expired; re-eval → 0 | Admin oversight |
| Host routes + panel access | `Host_registers_seller_and_admin_promotion_routes_with_panel_access` — `/v1/seller/promotions`, `/v1/admin/promotions`, `SellerPanelAccess` / `AdminPanelAccess`, `MapPromotionEndpoints`, checkout `couponCode` not hard-null | HTTP boundary |

File: `src/backend/Host/Tooba.Host.Tests/PromotionPanelTests.cs`

## Reviews — `ReviewsFoundationTests` / Postgres

| Case | Test / assertion | Result claimed |
|---|---|---|
| Seller list route; no seller moderation/reply routes | `Seller_host_list_exists_without_seller_response_or_moderation_routes` — `/v1/seller/reviews` + `SellerPanelAccess`; `SellerResponseSupported: false`; no `/v1/seller/reviews/{…}`; DTO omits `AuthorUserId` / `ProductId` / `SellerReply` | Contract honesty |
| Foreign seller party header | `Foreign_seller_party_header_is_denied_by_seller_panel_access` — 403 `seller.authorization.denied` | Foreign deny |
| Own products only | `ReviewsPostgresTests.Seller_scoped_list_includes_own_products_and_excludes_foreign` — own ProductIds included; foreign ProductId excluded | Seller isolation |
| Admin moderation | Existing admin publish/reject contracts remain (`/v1/admin/reviews…`) | Admin moderation |

File: `src/backend/Host/Tooba.Host.Tests/ReviewsFoundationTests.cs`

## Notifications

N/A — DEFERRED; no recipient isolation tests required this wave.

## Customer review submit

Unchanged permissions (existing customer POST path not rescope).

## Verdict

Promotion own/foreign/admin and review own/foreign/admin coverage cited above; notifications not in scope.
