# error-localization — TB-TMAR-HOST-SECURITY-AMC-001

## Hard-coded Persian PlatformHttpException title sites (count = 8)

| # | File | Line | HTTP | Code | Hard-coded title (literal) |
| --- | --- | --- | --- | --- | --- |
| 1 | Seller/SellerPanelAccess.cs | 59 | 401 | seller.actor.missing | هویت بازیگر احراز نشده است. |
| 2 | Seller/SellerPanelAccess.cs | 70 | 400 | seller.identity.missing | شناسهٔ فروشنده نامعتبر است. |
| 3 | Seller/SellerPanelAccess.cs | 88 | 401 | seller.actor.missing | هویت بازیگر احراز نشده است. |
| 4 | Seller/SellerPanelAccess.cs | 92 | 503 | seller.authorization.unavailable | سرویس مجوز در دسترس نیست. |
| 5 | Seller/SellerPanelAccess.cs | 118 | 503 | seller.authorization.unavailable | سرویس مجوز در دسترس نیست. |
| 6 | Seller/SellerPanelAccess.cs | 121 | 403 | seller.authorization.denied | دسترسی به این فروشنده مجاز نیست. |
| 7 | Seller/HostPartySellerAuthorizer.cs | 64–65 | 403 | seller.authorization.denied | مجوز تنظیمات فروشنده وجود ندارد. |
| 8 | Seller/HostSupportSellerAuthorizer.cs | 48 | 403 | seller.authorization.denied | مجوز پشتیبانی وجود ندارد. |

Violation class: Host Security must not embed user-facing Persian presentation text; machine code + catalog localization required.

## Stable error-code matrix (Security/Seller producers)

| Code | Producer(s) | HTTP | Descriptor / catalog | EN (OrderErrors.resx) | FA (OrderErrors.fa.resx) | Duplicate constant? | Hard-coded title? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| seller.actor.missing | SellerPanelAccess | 401 | OrderErrorCatalogContributor + OrderErrors | Actor identity is required. | هویت بازیگر احراز نشده است. | Also Order SellerOrderErrors | YES (sites 1,3) |
| seller.identity.missing | SellerPanelAccess | 400 | Order catalog | Seller identity is invalid. | شناسهٔ فروشنده نامعتبر است. | Also Order SellerOrderErrors | YES (site 2) |
| seller.authorization.denied | SellerPanelAccess; HostParty; HostSupport | 403 | Order catalog; FoundationErrorCodes also names code; Party/Support reference shared | Seller party access denied. | دسترسی به این فروشنده مجاز نیست. | Multi-module code refs | YES (sites 6–8; Party/Support titles diverge from Order FA) |
| seller.authorization.unavailable | SellerPanelAccess | 503 | Order catalog | Authorization service unavailable. | سرویس مجوز در دسترس نیست. | — | YES (sites 4,5) |

## Checkout (non-PlatformHttp)

| Code | Producer | Mechanism | Catalog |
| --- | --- | --- | --- |
| checkout.authentication_required | CheckoutIdentityGate | SemanticException | FoundationErrorCodes / Foundation catalog |

## HostOrderSellerAuthorizer

Catches `PlatformHttpException` → returns `SemanticError(ex.ErrorCode)` — no new title invent.

## Message-classification scan (Security tree)

| Pattern | Count under Host/Security |
| --- | --- |
| ex.Message / exception.Message parsing | ZERO |
| Message.Contains / StartsWith / Equals | ZERO |
| InvalidOperationException remapping | ZERO (Checkout uses SemanticException) |

## Notes for W1 (not executed)

- Sites 1–6 titles already match Order FA catalog strings for the same codes (still illegal as Host literals).
- Sites 7–8 invent capability-specific Persian titles while reusing `seller.authorization.denied` — status/code OK, presentation non-canonical.
