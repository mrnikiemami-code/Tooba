# Recovery Start — TB-TMAR-NEXT-MODULE-BATCH-004

Claim: `e5469a85-a7d4-472b-9246-eff084e52a3f`
Channel: `tooba-main`
Worker: `tooba-worker-01`
Baseline: `d91bf987dacf691d9df664ea478036845bf263ec` (== origin/main)
Parent: `TB-TMAR-NEXT-MODULE-BATCH-003-R1` (Architect accepted)

## Scope

Recover ONLY:

- Fulfillment → COMPLETE_REFERENCE_PATTERN
- Returns → COMPLETE_REFERENCE_PATTERN

Allowed bounded contract extraction: Order / Inventory / Payment / Wallet / Notification / Settlement / Host (compile-only seams).

## Explicit non-scope

- Tax / Pricing physical review
- Checkout resume / W6
- Frontend
- Broad Order or Settlement recovery

## Start states (inherited)

- Notification / Support / Wallet / Payment / Inventory / Promotion / Offer: COMPLETE_REFERENCE_PATTERN
- Foundation: RESULT_PATTERN_FOUNDATION_COMPLETE
- Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
- Tax / Pricing: DEFERRED_PHYSICAL_REVIEW_BY_USER

## Defects at start

### Fulfillment.Infrastructure → foreign Application

- Order.Application (`IOrderFulfillmentReader`, cancel gate)
- Inventory.Application (`IInventoryDirectory`)
- Payment.Application (unused csproj/usings; events already Contracts)

Root-dumped production files; `FulfillmentDirectory.cs` ~47KB god-file; Domain root dump; clock/id bypass; empty DbUpdateException catches; localized exception prose.

### Returns.Infrastructure → foreign Application

- Order.Application, Fulfillment.Application, Payment.Application (+ Payment.Domain `PaymentStatus`), Inventory.Application
- Wallet.Contracts already correct
- ReturnSettlementBridge: N/A Settlement dependency (reads ReturnsDbContext only)

Root dump; UtcNow/NewGuid; localized prose; broad catch with intentional fail-mark (not empty).

## Strategy

1. Extract minimal public ports/DTOs into owning `*.Contracts`
2. Retarget Fulfillment/Returns Infrastructure to Contracts-only foreign refs
3. Physical folders + matching namespaces; split Directory by cohesion
4. Adopt IClock/IIdGenerator; remove bypass/silent catch/prose
5. Architecture + behavior characterization tests
6. Evidence + selective commit/push + Bridge Result
