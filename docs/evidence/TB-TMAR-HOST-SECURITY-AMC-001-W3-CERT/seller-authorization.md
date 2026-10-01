# seller-authorization — TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT

## Panel core

- `HostSellerPanelAccess` = sole `ISellerPanelAccess` DI implementation
- `SellerPanelAccess` = internal helper (authenticated session wins; DevActorHeader Development-only; SellerPartyHeader context-only)
- Failures: SemanticException + SellerSecurityErrorCodes (actor 401 / identity 400 / unavailable 503 / denied 403)
- PlatformHttpException expected-failure usage: ZERO
- Hard-coded runtime titles: ZERO
- Fail-open: ZERO

## Pass-through authorizers (8)

Catalog, Notification, Offer, Promotion, Return, Reviews, Settlement, Story — panel gate only via ISellerPanelAccess.

## Order adapter

- Success tuple unchanged
- Catches `SemanticException` only; returns `ex.Error`
- No PlatformHttpException catch / broad Exception / Message classification

## Capability adapters

- Party + Support: panel gate then `IPlatformEffectiveAccessReader`; deny → seller.authorization.denied / SemanticException
- GlobalWithinOwner + !DeniedByCeiling preserved

## Active / dead

- Reviews adapter PRESENT + DI active
- Dead adapters: 0
