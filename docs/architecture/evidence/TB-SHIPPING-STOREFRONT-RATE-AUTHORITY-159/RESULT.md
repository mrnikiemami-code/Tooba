# TB-SHIPPING-STOREFRONT-RATE-AUTHORITY-159 — Result (PARTIAL)

Baseline / HEAD / origin/main: `bfb9245bb6fce0f8a4b65b40c68090879413e667` (verified equal before and after; see
`production-diff-proof-159.txt`).

## Status: PARTIAL — semantic scope lacks authority; nothing shipped

The bounded mission was to implement the smallest reviewed, owner-correct customer pricing seam for
explicitly enabled Shipping-owned rate authority, preserving default-off behaviour. A candidate seam was
designed, written and compiled, then **reverted in full** because it could not be made owner-correct
without changing Shipping's own deterministic pricing semantics — a certified-module change outside this
task, and one that must not be smuggled in.

Precise blocked shape: `OWNER-AUTHORITY-BLOCKER-159.md`.

### The blocker in one paragraph

The only existing owner read seam is `IShippingRateQuoteLookup` (`Tooba.Shipping.Contracts.Rates`). Wiring
the storefront price to it was the intended minimal seam, but the owner engine treats a
provider/service-level row (`OptionCode == null`) as a valid fallback for an option-specific request
(`ShippingRatePolicy.MatchesOption` line 147-149; `ShippingRatePolicyEngine.Quote` line 86-90). A single
`ServiceCode = "tipax", OptionCode = null` row would therefore price **both** `tipax:standard` and
`tipax:express`. That is provider-level tariff inheritance across sibling methods, forbidden by the locked
Admin flat-method contract §C, by this task's HARD SAFETY, and already recorded by the owner's own read
model as unattributable (`LegacySharedUnresolved`,
`ShippingMethodFinancialStatusEvaluator.cs:64-82`). The owner module thus already asserts the correct rule
while its quote engine still honours the opposite fallback. Order may not compensate by re-implementing the
rule: the seam's contract explicitly refuses to expose the policy ("so that the consumer (Order) cannot
re-implement business rules").

Two further facts that make "the Admin amount is the customer quote" provably insufficient today:

1. A rate row alone does not price anything: the owner engine also requires an active coverage grant, and
   the TB-158 Admin-created `tipax:standard` row has `activeCoverageGrantCount: 0`
   (`financial-status-standard-after-ui-create-158.json`) → `NotCovered`.
2. The storefront has no trusted parcel measurement, so conditioned owned policies correctly fail closed
   with `MeasurementRequired` (`ProjectStorefrontShippingQuery.cs:63-71`).

## What was actually produced

* No production change of any kind. `git status --porcelain -- src/backend` and
  `git diff --stat HEAD -- src/backend` are both **empty** (`production-diff-proof-159.txt`).
* A candidate seam was compiled successfully and then removed:
  `dotnet build src/backend/Modules/Order/Tooba.Order.Infrastructure/Tooba.Order.Infrastructure.csproj`
  → `Build succeeded. 0 Error(s)` (19 pre-existing warnings, none new from the removed files).
* Deterministic owner-side analysis with exact source citations
  (`OWNER-AUTHORITY-BLOCKER-159.md`), including the required additive owner change.

## Acceptance status per contract

| Item | Status |
| --- | --- |
| A. Focused deterministic owner/contract tests for enabled authority | NOT DELIVERED — would pin a price that the owner rule forbids; the blocker is documented instead with exact citations |
| B. Private authenticated end-to-end customer quote/write/read/payment-draft | NOT ATTEMPTED — blocked upstream (no safe seam exists to observe); TB-158 already established the authenticated-quote gate (`401 checkout.authentication_required`) |
| C. Build + focused tests | Build of changed project tree: PASS (0 errors). No test project was run because no production/test source was retained. `tooba-frontend-feature` skill not applicable — frontend untouched |
| D. Evidence + default-off/enabled provenance | DELIVERED (`OWNER-AUTHORITY-BLOCKER-159.md`, `no-production-diff-159.txt`, `production-diff-proof-159.txt`) |

## Limitations / honesty notes

* Nothing is claimed about customer-facing authoritative pricing: no price was changed, observed or
  asserted for a customer, and no PASS is claimed from tests.
* `src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx` was already dirty before this task and
  is unrelated; it was preserved untouched and deliberately not committed.
* Shared services `:3000`, `:5088`, `:5432/tooba_alpha`, RabbitMQ/Redis and shared containers were not
  touched. No scratch stack was needed because no runtime path was executed.

## Required next step (Architect decision)

Authorize the owner-side strict option-identity change in Shipping (additive Contracts mode + owner engine
behaviour + owner tests + coverage-grant and parcel-measurement decisions) before any Order storefront
pricing seam is wired. Details and ordering in `OWNER-AUTHORITY-BLOCKER-159.md` §"Required owner-side
change".

STOP_FOR_ARCHITECT. No automatic next implementation task.
