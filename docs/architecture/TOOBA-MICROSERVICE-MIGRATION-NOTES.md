Tooba Microservice Migration Notes

Purpose

Durable architecture notes for TMAR so high-value migration decisions are not lost if chat/session context disappears.

Primary Goal

Tooba remains a strict Modular Monolith today and must be prepared for low-friction, incremental migration to Microservices later.

The target is not a full rewrite.

The target is:

preserve working business behavior

strengthen bounded-context ownership

extract stable module contracts

remove cross-module structural leakage

eliminate Host business ownership

move new use-cases to CQRS + MediatR

prepare cross-service consistency before physical service extraction

Current conclusion

A full rewrite is NOT required.

Much of Tooba is already structurally favorable for future extraction because:

modules have separate PostgreSQL schemas

modules have separate DbContexts and migrations

raw cross-schema joins are not the intended pattern

Outbox / Integration Event infrastructure already exists

many module boundaries are already explicit

However, migration will not be "zero pain".

The hardest part is the Checkout / Order / Cart / Inventory / Payment consistency chain.

Checkout consistency warning

Current checkout behavior uses a shared physical database and TransactionScope across business actions such as:

Inventory reservation

Order creation

Cart conversion

This works today because modules share one physical PostgreSQL deployment.

It will NOT remain valid after those modules become independent services with separate databases.

A future extraction therefore requires an explicit consistency redesign, not merely moving code to separate processes.

Potential target concepts to design later:

Saga / Process Manager

compensating actions

idempotency

retry semantics

duplicate delivery handling

timeout/recovery

intermediate workflow states

point-of-no-return

inventory reservation compensation

order creation failure recovery

event-driven coordination through Outbox/Integration Events

Do NOT implement the Saga prematurely.
First document the invariants and workflow.

Cross-module transaction lock

Architectural rule to canonicalize:

No NEW business workflow may rely on one shared ACID transaction spanning multiple bounded contexts.

Existing cross-module/shared-database transactions must be inventoried and treated as migration debt.

Before future service extraction, each such workflow must have an explicit distributed-consistency design.

Order.Application warning

Order.Application is both:

a legitimate orchestration point

an over-coupled synchronous hub

Independent repository review found synchronous dependencies across several modules such as:

Cart

Inventory

Offer

Pricing

Promotion

Tax

If converted naively into network calls, checkout could become a chain of multiple synchronous service calls with:

higher latency

cascading failure risk

tighter runtime coupling

Before extracting Order, review which dependencies should become:

Contracts/Gates

local projections/read models

integration events

process-manager interactions

retained synchronous calls only where truly required

Extraction difficulty classes

Lower-risk / peripheral modules

Examples:

Reviews

Wishlist

Content

Story

Notification

Media

Support

AddressBook

CustomerProfile

OperatorProfile

UserPreference

These are candidates for comparatively direct extraction after contracts, operational readiness, monitoring and deployment boundaries are ready.

Medium-risk business modules

Examples:

Catalog

Offer

Pricing

Party

These should be extracted only after:

Contracts cleanup

ownership cleanup

read-gateway cleanup

Host coupling removal

Highest-risk workflow

Cart

Order

Inventory

Payment

Checkout orchestration

This area requires explicit consistency redesign before service separation.

Confirmed architecture debt from TMAR

Confirmed repository findings include:

Cart.Domain -> Offer.Domain

Order.Domain -> Offer.Domain

Pricing.Domain -> Offer.Domain

Payment.Infrastructure -> Wallet.Domain

multiple Application -> Application edges

many Infrastructure -> foreign Application edges

oversized Host

oversized/god files

some questionable Domain ownership

read-side microservice coupling in Host

direct legacy Host writes

localized Domain error strings in legacy code

clock / ID implementation calls in legacy code

direct IMemoryCache bypasses in legacy Host code

TMAR guards are designed so this debt can only shrink.

God-file rule

Large files must not be split blindly.

Before decomposing a critical oversized file:

identify its use-cases and behavioral seams

add focused characterization tests

split incrementally by capability/use-case

preserve public behavior

keep architecture guards green

Current TMAR policy:

new hand-written files over 800 LOC are rejected

oversized legacy files are baselined

legacy oversized files may stay equal or shrink, but must not grow

generated/migration/build files are excluded

Product feature resume gate

Feature development may resume under:

Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

Rules after the gate:

product development may resume

TMAR continues in parallel

ALL new code must follow TMAR architecture locks

new code must not expand legacy debt

ARCH-TX-001: no NEW shared-ACID transaction spanning multiple bounded contexts

Current TMAR sequence

Last Product Task:
TB-P10-T022-R21

Accepted:

TB-TMAR-ARCH-BASELINE

TB-TMAR-FND-001

TB-TMAR-HOST-W1-R1

TB-TMAR-HOST-W2

TB-TMAR-BOUNDARY-V1

TB-TMAR-BOUNDARY-V1-R1

TB-TMAR-CONTRACTS-W1

TB-TMAR-CONTRACTS-W2

TB-TMAR-CONTRACTS-W3

TB-TMAR-CONTRACTS-W4

Current expected architecture wave:
TB-TMAR-CONTRACTS-W5

Future design candidate (not implemented):
TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN

Do not weaken TMAR locks when resuming product work.

Recovery phrase

If chat context is lost, use:

برگردیم به TMAR

Then recover from:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

latest docs/evidence/<Task-ID>/recovery-sot.md

this migration note

Repository source-of-truth is authoritative over stale chat memory.