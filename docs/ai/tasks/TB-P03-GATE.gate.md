# Tooba — TB-P03-GATE — Commerce Core Acceptance Gate

BEGIN_TOOBA_CURSOR_GATE_V1

Protocol-Version: 1
Gate-ID: TB-P03-GATE
Phase: P03 — Commerce Core
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Depends-On: TB-P03-T009
Architect-Decision-On-Dependency: ACCEPTED

Objective

Perform the final evidence-based acceptance gate for P03 — Commerce Core.

Do NOT add new product features.

Review and validate together:

TB-P03-T001 Catalog Product & Variant
TB-P03-T002 Seller Offer / Listing
TB-P03-T003 Pricing + Repair
TB-P03-T004 Inventory
TB-P03-T005 Cart + Recovery Review
TB-P03-T006 Checkout & Order + Repair
TB-P03-T007 Tax + Repair
TB-P03-T008 Payment + Repair
TB-P03-T009 Promotion & Discount

P03 may pass only if the entire commercial flow is coherent end-to-end.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

9616603f90e2c28ea17d04c88fe5db9b6db952b9

Require:

branch = main
HEAD == origin/main
safe/known working tree

Unsafe/ambiguous state => RECOVERY_CONFLICT.

No force push.
No history rewrite.
No destructive reset.
No silent stash.

Core Commerce Invariants

Verify all remain true:

Catalog Product != Seller Offer
Product != Price
Offer != Price
Product != Inventory
Offer != Inventory
Cart != Order
Order != Payment
Order != Fulfillment
Promotion != Base Price
Tax is separate from Pricing

Forbidden regressions:

Product.Price
Product.Stock
Product.SellerId
Offer.Price
Offer.Stock
Seller == User
Membership == Authorization

Catalog

Verify:

Product = descriptive truth
Variant belongs to Product
typed attributes
Category / Brand descriptive ownership
multilingual-ready model
publication != purchasability
no seller/price/stock in Catalog

Offer / Marketplace

Verify:

one Variant supports multiple Seller Offers
Seller identity = Party/Organization reference
Seller != User
seller SKU scoped appropriately
SalesChannel canonical
SingleStore still uses Offer abstraction

Pricing

Verify:

Pricing owns Money
OfferId target
Market != Locale
Market != Currency
Currency != Tax Jurisdiction
SalesChannel explicit
effective dating
overlap ambiguity prevented
authored price != FX-derived display price
base price = Tax Exclusive

No Product.Price or Offer.Price.

Inventory

Verify:

Inventory owns stock truth
OnHand / Reserved / Available semantics
multi-location
seller/Offer-specific stock
atomic/concurrency-safe reserve
last-unit oversell prevented
release/consume semantics

No stock fields in Product/Offer.

Cart

Verify:

Cart line targets OfferId
authenticated ownership
anonymous high-entropy access seam
pricing quote is non-authoritative
inventory hold ownership
reservation expiry/release
multi-seller Cart supported
concurrency safety
Cart != Order

Both future conversion modes preserved:

RequestToReserve
OnlinePurchase

Checkout / Order

Verify:

Cart converts to Order/Checkout structure
RequestToReserve != unpaid OnlinePurchase
BuyerPartyId != PlacedByUserId / ActingUser
pricing revalidated at checkout
historical snapshots immutable
seller-specific lifecycle possible
idempotent checkout
same Cart cannot create two independent checkouts

Verify repaired durable Cart→Checkout invariant:

unique CartId mapping / equivalent persistence invariant
same or different IdempotencyKey cannot duplicate checkout
reconciliation retry-safe

Classify absence of background Cart reconciliation worker as deferred/non-blocking only if duplicate commercial Order remains impossible.

Promotion

Verify deterministic commercial sequence:

Pricing
→ Promotion
→ Tax
→ Order Snapshot

Verify:

Promotion does not rewrite Pricing
percentage/fixed amount typed
effective dating
eligibility/scope
Stackable vs Exclusive deterministic
coupon normalization
checkout re-evaluation
immutable Order discount snapshot

Usage/redemption quotas may remain deferred only if no implemented behavior claims them.

Tax

Verify:

Base Price = Tax Exclusive
Tax calculated separately
no hard-coded tax rate/date/law
TaxJurisdiction explicit
effective-dated TaxRule
deterministic rounding

Hard outcome distinctions:

TAX_EXEMPT
ZERO_RATE
NO_APPLICABLE_RULE
CALCULATION_ERROR

No applicable rule/error must not silently become zero tax.

Order tax snapshot must remain immutable after rule changes.

Payment

Verify:

Order != Payment
client cannot choose payment amount
amount/currency derived from Order snapshot
RequestToReserve does not require payment
OnlinePurchase supports payment
initiation != success
callback text != verified success
provider verification required
no PAN/CVV storage

Verify durable repaired path:

Payment Succeeded
→ Outbox
→ payment.succeeded.v1
→ MassTransit PostgreSQL SQL Transport
→ Order-owned consumer
→ Order local transaction

Verify:

durable Order-side Inbox/idempotency
duplicate delivery harmless
amount/currency rechecked
process crash/restart recoverable
Payment remains verification source of truth

No Payment Infrastructure access to OrderDbContext.

Multi-Seller Commercial Flow

Verify the complete Marketplace path supports:

one Catalog Variant
→ multiple Seller Offers
→ independent inventory
→ one multi-seller Cart
→ seller-scoped Order lifecycle
→ customer Payment allocation across seller orders

without forcing one seller identity onto the whole checkout.

Settlement/payout remains out of scope.

SingleStore Integrity

Verify SingleStore did NOT collapse architecture.

Required:

Product
Offer
Pricing
Inventory
Cart
Order
Tax
Promotion
Payment

remain conceptually separated even though the store has one commercial owner.

No direct Price/Stock fields added to Product as a shortcut.

Cross-Module Boundary Gate

Verify:

no cross-module database FK
no foreign module DbContext/repository access
no mega DbContext
contracts/interfaces/events used across modules
Domain/Application remain independent of provider/transport SDKs

Architecture tests must cover real Commerce modules, not only vacuous rules.

Tenant Isolation

Verify all relevant modules use trusted tenant/deployment resolution:

Catalog
Offer
Pricing
Inventory
Cart
Order
Tax
Promotion
Payment

No module parses Host internally.

Tenant A data must not leak into Tenant B.

Marketplace remains distinct from SingleStore tenant context.

Event / Outbox / Messaging

Verify:

module-owned Outbox remains coherent
MassTransit 8.5.10
PostgreSQL SQL Transport
no RabbitMQ

Payment→Order durable projection must exercise the real intended transport path or equivalent current integration evidence.

No MassTransit types in Domain/Application.

Package Audit

Report exact current versions for:

.NET
EF Core
Npgsql
MassTransit
MassTransit.SqlTransport.PostgreSQL
Authzed.Net
OpenTelemetry
Next.js
React
Tailwind

Verify no accidental introduction of:

MassTransit.RabbitMQ
RabbitMQ.Client
MassTransit 9.x
Redis authorization cache
real payment-provider SDK

unless separately architect-approved.

Do not upgrade packages in this Gate unless current validation is broken.

Persian Documentation Gate

Hard requirement:

all required Tooba-owned Classes / Interfaces / Methods / Properties
have strong meaningful Persian documentation

Verify:

CS1591 remains build error
no blanket suppression
generated exclusions narrow

Weak/name-echo documentation = Gate failure.

Full CURRENT Validation

Run NOW; do not inherit previous results.

Backend:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

Require:

Build warnings = 0
Build errors = 0
Failed = 0
Skipped = 0

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run build

Return to root:

git diff --check
git status --short --branch

All available PostgreSQL / SpiceDB / MassTransit SQL Transport integration tests must run.

No skip caused by infrastructure contention is acceptable.

Source-of-Truth Reconciliation

Review:

AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md

They must agree on:

P02 = COMPLETE
P03 Gate = IN PROGRESS
Last Architect Accepted Task = TB-P03-T009
Current Gate = TB-P03-GATE
P03 is NOT COMPLETE before Architect ACCEPT

Do not invent the next phase name if SoT already defines it.

If roadmap defines the next phase, report it exactly.

Concern Classification

Reconcile every carried concern and classify:

BLOCKER
REPAIR_BEFORE_NEXT_PHASE
DEFERRED_NON_BLOCKING
RESOLVED

At minimum include:

OTel package split
/__platform-* diagnostics
config-backed tenant registry
Npgsql / MassTransit NodaTime constraint
SQL Transport admin/runtime credential split
generic durable Inbox/dedup
MassTransit delayed redelivery/scheduler
T006 custom Outbox vs MassTransit EF Outbox future review
process-local cache until Redis
Identity real OTP delivery provider
Keycloak/OIDC
WebAuthn/passkey
rate-limit/anti-fraud product
CONDITIONAL_PERMISSION caveats
Redis authorization cache
Cart background conversion reconciliation worker
Promotion usage/redemption quota ledger
real payment PSP
refund/capture/void
seller settlement/payout
Fulfillment/Shipment
Returns/RMA implementation
commercial UI

Do not silently drop any concern.

Mandatory Future UX Sequence

Verify SoT still preserves:

Deep Shopeiva Study
Template reuse map
Design System extraction
Professional reusable Data Grid
Workspace interaction patterns
serious UI implementation
Visual evidence
Architect visual ACCEPT

Also preserve:

Backend/module boundary != UI boundary
Weak UI/UX = product failure

No commercial UI work is required in this Gate.

Gate Evidence

Create:

docs/evidence/TB-P03-GATE.md

Include:

commerce invariant matrix
multi-seller flow evidence
SingleStore boundary evidence
Pricing/Promotion/Tax ordering evidence
Inventory concurrency evidence
Cart/Order idempotency evidence
Payment durability evidence
tenant isolation evidence
package audit
Persian documentation evidence
full validation
concern classification
final gate recommendation

SoT State Before Architect Review

Use:

Last Architect Accepted Task: TB-P03-T009
Current Gate: TB-P03-GATE
Current Phase: P03 — Commerce Core
Gate State: AWAITING_ARCHITECT_ACCEPT

Do NOT mark P03 COMPLETE.

Save Gate Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P03-GATE.gate.md

Do not summarize or condense.

Gate Repairs

Only bounded repairs required for P03 coherence are allowed.

Do not start the next phase.

Material architecture conflict => BLOCKED.

Git

If evidence/SoT or bounded gate repairs changed:

git add .
git commit -m "chore run Tooba P03 commerce core gate [TB-P03-GATE]"
git push origin main
git fetch origin

Require:

HEAD == origin/main

No force push.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Gate-ID: TB-P03-GATE
Phase: P03 — Commerce Core
Status: PASS | REPAIR_REQUIRED | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Catalog:
- ...

Offer:
- ...

Pricing:
- ...

Inventory:
- ...

Cart:
- ...

Checkout-Order:
- ...

Promotion:
- ...

Tax:
- ...

Payment:
- ...

Multi-Seller-Flow:
- ...

SingleStore-Integrity:
- ...

Cross-Module-Boundaries:
- ...

Tenant-Isolation:
- ...

Messaging-Outbox:
- ...

Package-Audit:
- ...

Persian-Documentation:
- ...

Validation:
- backend restore:
- backend build:
- build warnings:
- build errors:
- backend tests:
- backend passed:
- backend failed:
- backend skipped:
- postgres/spicedb/masstransit integration tests:
- frontend install:
- frontend typecheck:
- frontend lint:
- frontend build:
- git diff --check:

Concern-Classification:
- ...

Gate-Evidence:
- File:

Git:
- Commit:
- Push:
- Final-HEAD:
- Final-Origin-Main:
- Final-Status:
- Head-Matches-Origin:

Source-of-Truth:
- Last-Architect-Accepted-Task:
- Current-Gate:
- Current-Phase:
- Gate-State:
- Next-Phase-From-Roadmap:
- Recovery-Ready:

Gate-Recommendation:
- P03_GATE_PASS | REPAIR_REQUIRED | BLOCKED

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1
CRITICAL — DO NOT LEAVE PIPELINE

After sending RESULT:

WAIT HERE for the USER / Architect to provide the next valid task
in this SAME chat/session.

You MUST remain inside the Tooba Architect-controlled pipeline.

Do NOT:

close this chat/session
end the agent workflow
leave PIPELINE mode
treat RESULT as the end of the work
move to another chat
wait outside this pipeline
invent the next task or gate
infer the next phase
prepare next-phase work
execute any next-phase task

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Only when a new valid Envelope is provided in this SAME chat/session may you execute the next task.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_GATE_V1
