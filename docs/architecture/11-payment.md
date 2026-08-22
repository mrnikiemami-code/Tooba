# Tooba — Payment Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T012
```

Documentation only. No PSP SDK, webhooks, or PCI implementation.

```text
Order != Payment
Payment != Payment Provider
Payment Intent != Payment Attempt
```

## A. Core Separation

Payment owns payment **attempts/results** and PSP references. Order owns commercial snapshots. Catalog/Inventory/Fulfillment do not capture money.

## B. Payment Ownership

Owns: payment intent, attempts, provider refs, amounts/currency as paid, statuses, refund/void commands, reconciliation facts.

Must not own: order lines as catalog, stock, shipments, prices as SoT.

## C. Payment Intent vs Attempt

**Intent** = customer/checkout obligation to pay amount X in currency C for Order/Checkout id.  
**Attempt** = one try against a provider/method. Multiple attempts per intent allowed. Success is a fact on an attempt, not a flag on Order alone.

## D. Idempotent Payment Initiation

Initiate keyed by idempotency key (checkout/order/command id). Duplicate click ≠ double charge.

## E. Provider Adapter Boundary

Domains use `IPaymentProvider` (conceptual). Stripe/Zarinpal/etc. stay in adapters. Provider not chosen.

## F. Redirect / Callback / Webhook Flow

Browser redirect and server webhooks are **transport**. Truth is Payment module after **authenticated** provider event + amount check. UI success page is not SoT.

## G. Callback/Webhook Security

Verify signature/shared secret, replay window, source. Unknown tenant fail closed. No unsigned body trust.

## H. Duplicate / Out-of-Order Events

Idempotent apply by provider event id. Out-of-order: ignore stale if current state is already terminal success/fail per guardrails (AG).

## I. Payment Status Model

Candidate (not locked enum): Pending, Authorized, Captured/Succeeded, Failed, Cancelled, Refunded (full/partial). Distinct from Order commercial status (T011).

## J. Authorization vs Capture

Preserve auth-then-capture for later. Initial sellable path may capture immediately. Architecture must not assume only one.

## K. Amount & Currency Validation

Webhook amount/currency must match intent. Mismatch fail closed; do not mark Order paid.

## L. Multi-Currency

Charge currency is the intent currency (T009 snapshot). Do not convert in Payment ad hoc. FX belongs to Pricing/Order snapshot.

## M. Payment Method

Methods are adapters: card redirect, wallet, transfer, COD. Method ≠ Order type.

## N. COD / Offline / Manual Payments

First-class Payment attempts with manual/COD adapter. Still Payment-owned; not a boolean on Order replacing the module.

## O. B2B Payment Readiness

Invoice/net terms later as methods + credit policy (T007/T009). Not Identity.

## P. Payment Before vs After Order

Align T011: prefer durable Order/Intent id before capture. Sequence `NEEDS_LATER_P00_DETAIL`. Must compensate Inventory/Checkout on fail.

## Q. Refund

Refund is Payment command referencing original capture. Returns/RMA may **instruct** a refund (`docs/architecture/25-returns-rma.md`); Payment executes via adapter. Return approved ≠ refund completed. Snapshot refund amounts including historical tax. Payment does not calculate Tax (`docs/architecture/26-tax-architecture.md`).

## R. Void / Cancel Authorization

Void unused auth; distinct from refund of captured funds.

## S. Partial Payment / Partial Refund Readiness

Preserve multiple captures/refunds against one intent/order without redesign. Not implemented now.

## T. Marketplace Payment Implications

Customer may pay marketplace; seller settlement is **not** the customer Payment attempt. Split/payout later. Do not store settlement in Catalog.

## U. Single-Store

Same Payment module. One merchant account config per tenant/store via platform, not Host parse in Payment.

## V. Tenant / Provider Configuration

Per-deployment/tenant provider credentials live in secrets/config, resolved after trusted tenant context.

## W. Secrets

Keys never in logs, git, or client. Rotate via config. Webhook secrets similarly.

## X. Order Integration

Payment emits paid/failed; Order updates **payment refs/status projection**, not line rewrite. Order does not call PSP.

## Y. Inventory Integration

Payment fail → Inventory release (T010/T011). Payment success may allow commit. Events, not table writes.

## Z. Fulfillment Integration

Ship after policy (typically paid or COD accepted). Fulfillment listens to Order/Payment facts, not PSP.

## AA. Reconciliation

Compare provider reports vs Payment ledger vs Order totals. Differences = ops/audit, not silent Catalog edits.

## AB. Payment Ledger / Accounting Boundary

Payment ledger ≠ full accounting. Accounting/export later. Do not duplicate Order commercial snapshot as the only money truth for PSP.

## AC. Observability

Trace checkout/order/payment/attempt/provider ids. No PAN/CVV.

## AD. Audit

Security/business events: initiate, succeed, fail, refund, webhook accepted/rejected. Separate from debug logs.

## AE. PCI / Sensitive Payment Data Direction

No raw card storage in Tooba. Hosted fields/redirect. No extra PCI claim beyond “do not store PAN.”

## AF. Failure Matrix

| Case | Direction | Fail closed? |
| --- | --- | --- |
| Provider down | Pending/fail; no paid | Yes |
| Signature invalid | Reject webhook | Yes |
| Amount mismatch | Do not mark paid | Yes |
| Duplicate webhook | Idempotent | N/A |
| Tenant mismatch | Reject | Yes |
| Refund > captured | Deny | Yes |

## AG. State Transition Guardrails

No Captured → Pending. No double capture without new attempt. Terminal fail does not accept late success without explicit replay policy.

## AH. Cache

Do not cache “paid” without Payment SoT. Short TTL for UX polling only.

## AI. Testing Strategy — Architecture Level

Future: idempotent initiate, webhook replay, amount mismatch, auth vs capture, refund, multi-tenant secret isolation. No tests now.

## AJ. Data Ownership Matrix

| Concept | Owner |
| --- | --- |
| Intent/attempt/PSP refs | Payment |
| Order totals snapshot | Order |
| Quote | Pricing |
| Reservation | Inventory |
| Provider HTTP | Adapter |

## AK. Decision Summary

### RECOMMENDED_FOR_ADR

1. Order != Payment != Provider.
2. Intent vs Attempt.
3. Idempotent initiation.
4. Provider adapter/ACL.
5. Signed webhooks; UI not SoT.
6. Amount/currency match required.
7. Status distinct from Order/Fulfillment.
8. Auth vs capture possible.
9. Refunds/voids in Payment.
10. Secrets never logged.
11. No PAN storage.
12. Fail closed on mismatch/unknown tenant.
13. Marketplace customer payment ≠ seller settlement.
14. Compensation with Inventory/Checkout on fail.

### NEEDS_LATER_P00_DETAIL

- Order vs payment sequence
- PSP choice
- Auth-capture default
- Split settlement
- COD ops process

### DEFERRED

- Implementation, SDK, PCI SAQ, accounting product, UI, ADR, Shopeiva

PaymentSucceeded is a server-confirmed fact. Analytics purchase observations derive from Order/Payment; Analytics does not own capture/settlement. See `docs/architecture/16-first-party-analytics.md`. Payment telemetry (latency, PSP errors) is technical observability, not Payment SoT. See `docs/architecture/18-observability-logging-audit.md`. Payment != Fulfillment; COD is still a Payment attempt. See `docs/architecture/21-fulfillment.md`.
