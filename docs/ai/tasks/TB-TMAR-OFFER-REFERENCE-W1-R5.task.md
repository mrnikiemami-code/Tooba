PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-OFFER-REFERENCE-W1-R5

Parent-Task:
TB-TMAR-OFFER-REFERENCE-W1-R4

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
FINAL_VERIFICATION_AND_REPAIR

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Track:
OFFER_REFERENCE_MODULE

Title:
Offer Golden Module R5 — Full Final Verification, Residual Repair, and Reference-Pattern Lock

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Review of R4

R4 is accepted as READY_FOR_FINAL_VERIFICATION, not as final completion.

The Architect directly re-read current GitHub main after R4.

Current verified GitHub tip at Architect review:
e8c8fb95d93532723b942f6ea46f7590c49480fe

Verified good state:

per-use-case Command folders exist

per-use-case Query folders exist

IOfferSellerPanel is gone

List/Get endpoint responses now come directly from ISender

Create/Patch are cohesive MediatR use cases

SellerPanelComposer no longer injects Offer/Pricing/Inventory DbContexts

SellerPanelComposer no longer shapes Offer list/detail

Domain→Contracts leak removed

TypeForwarders removed

Offer Infrastructure no longer references Catalog.Application / Party.Application

no Persian prose reported in Offer Domain/Application/Infrastructure

R4 tests/build passed

However, R5 MUST independently verify the complete module and repair any remaining defect before COMPLETE is allowed.

Do not assume R4 evidence is sufficient.
Read the actual repository.

1. Known residual concern discovered by Architect

Current OfferSellerEndpoints.cs still contains:

catch (InvalidOperationException ex) when (ex.Message is "offer.not_found")

This is a magic-string exception compatibility seam.

A Golden module must not depend on string-matched InvalidOperationException for expected business outcomes.

R5 must trace the producer of offer.not_found across Pricing/Inventory/other owner gates and replace this with a stable typed/semantic owner-boundary outcome.

Do not merely change the catch string.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/known-residuals.md

2. Git / Recovery Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected baseline at Architect review:
e8c8fb95d93532723b942f6ea46f7590c49480fe

Before mutations verify:

branch main

HEAD == origin/main

staged = 0

no unexpected tracked modifications

protected commit 18ca10c9 remains ancestor

stashes untouched

user work preserved

User .rar archives are user work:

do not delete

do not stage

do not rename

do not overwrite

If HEAD advanced legitimately after Architect review:

record exact SHA

continue only if the worktree is safe and no competing Offer task changed the same surface

Forbidden:

git reset

git clean

destructive checkout/restore

unsafe rebase

stash pop/drop

broad git add .

3. FULL PHYSICAL STRUCTURE VERIFICATION

Verify on disk and solution metadata:

src/backend/Modules/Offer/

Expected projects:

Tooba.Offer.Domain

Tooba.Offer.Application

Tooba.Offer.Contracts

Tooba.Offer.Infrastructure

Tooba.Offer.Endpoints

Tooba.Offer.Tests

Expected Visual Studio logical grouping:
Modules/Offer

Verify:

all six projects registered exactly once

no stale old Offer project path elsewhere

no duplicate Offer project entry

no abandoned old files outside canonical Offer module root

no empty ceremonial folder

no root dumping of production responsibilities

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/physical-structure-final.md

4. FULL NAMESPACE / ASSEMBLY OWNERSHIP SCAN

Scan every non-generated Offer .cs file.

Verify:

namespace matches project/folder responsibility

no Tooba.Offer.Domain namespace inside Contracts assembly

no Contracts namespace in Domain assembly

no Infrastructure namespace in Application/Domain

no Host namespace inside Offer projects

no duplicate public type names representing the same concept without explicit mapping rationale

SalesChannel ownership is unambiguous

OfferStatus ownership is unambiguous

no TypeForwarder / type shim remains

Scan repository references to Offer types for stale namespace compatibility residue.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/namespace-ownership-final.md

5. DOMAIN FINAL GATE

Inspect every Offer Domain source file.

Verify:

aggregate invariants live in Domain

no localized user-facing prose

no arbitrary InvalidOperationException for expected business rules

semantic error codes are stable

no Guid.NewGuid()

no UuidV7.New()

no direct system clock

no foreign module reference

no Contracts dependency

no persistence/HTTP concerns

no Price truth

no Inventory truth

no Catalog EF relationship

seller is Party ID, not authentication User ID

state transitions are coherent and tested

Return Policy state representation is not duplicated inconsistently

If string-valued Domain concepts such as "Default", "Custom", "NonReturnable" remain, determine whether they should be a Value Object/enum under the current reference standard.
Do not change merely for aesthetics; repair if type safety/invariant ownership is materially weak.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/domain-final.md

6. APPLICATION / MEDIATR FINAL GATE

Inspect all Offer Commands, Queries, Handlers, Mappings, Ports.

Verify EVERY endpoint-visible Offer use case follows:

Endpoint → ISender → Request → Handler → Domain/Ports

No ceremonial MediatR:

no send-and-ignore

no handler whose result is bypassed by a second service

no duplicate legacy façade doing the same use case

Verify:

one externally meaningful use case per folder

command/query and handler names/namespace match folder

no broad OfferRequests.cs, OfferHandlers.cs, OfferQueries.cs, OfferQueryHandlers.cs

no DbContext in Application

no HTTP types

no Host types

no foreign Application references

IClock / IIdGenerator used at application boundary

seller scope enforced inside handlers for seller-scoped operations

Create/Patch coherence is in Application, not endpoint

stable errors use canonical constants, not ad-hoc literal codes

Also scan for:

"offer.missing"

"offer.status.unsupported"

any raw semantic-code literal
If canonical constants exist or should exist, normalize them.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/application-final.md

7. CONTRACTS FINAL GATE

Inspect all Offer.Contracts files.

Verify:

contracts expose only cross-module/public boundary concepts

no Application implementation type leakage

no Domain implementation type leakage

namespace is Tooba.Offer.Contracts...

DTOs are transport/boundary DTOs, not persistence entities

ports are stable and narrowly scoped

no Host/UI type

no localization text

no duplicate domain authority

Classify every public Contracts type:

inbound DTO

outbound port

public lookup contract

integration event

enum/value representation

Anything not justified must move/remove.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/contracts-final.md

8. INFRASTRUCTURE FINAL GATE

Inspect every Offer.Infrastructure file.

Verify:

persistence only where appropriate

OfferStore does not contain Application use-case decisions

EF configurations under Persistence/Configurations

migrations under module-owned Persistence/Migrations

no foreign Application dependency

no Host dependency

no Pricing/Inventory/Catalog/Party DbContext

no cross-module SQL join

no cross-module DB FK

no localized prose

no business-rule duplication

outbox/domain-event adapter role explicit

module DI contains composition only, not business logic

no open/pass-through fake security guard in production

Trace all DI registrations and ensure no stale IOfferDirectory, IOfferSellerPanel, or removed implementation remains.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/infrastructure-final.md

9. ENDPOINT FINAL GATE

Inspect all Offer.Endpoints.

Verify:

thin transport-only layer

auth actor extraction allowed

use cases through ISender

no DbContext

no repository

no Host BFF

no business sequencing that belongs to Application

no direct Domain mutation

no hardcoded localized response text

semantic errors map by stable code/category

no string-matched exception fallback

Price / Inventory compatibility routes

Current URLs remain under Offer route surface but writes use owner Contracts.

This is acceptable ONLY if:

endpoint acts as a transport alias/facade

Pricing owns price validation/write semantics

Inventory owns stock/default-location/write semantics

no Offer business decision is introduced

no Host persistence is involved

owner error contracts are typed/semantic and stable

route behavior remains compatible

If these conditions fail, repair before completion.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/endpoints-final.md

10. REMOVE MAGIC EXCEPTION SEAMS

Search whole repository for Offer/Pricing/Inventory seller-route paths using:

InvalidOperationException

ex.Message

"offer.not_found"

string exception matching

string-coded transport fallback

Trace exact producer.

Replace expected business failure transport with:

canonical semantic exception/error
OR

typed Contracts result/error agreed by owner module

No expected business outcome may rely on parsing exception text.

Add guard where practical.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/error-seam-repair.md

11. HOST FINAL OFFER-LEAK SCAN

Search all src/backend/Host/**, not only SellerPanelComposer.

No Host file may:

mutate SellerOffer

query OfferDbContext for business/read-model behavior

shape Offer seller DTOs

own Offer business rule

resolve Offer title/price/stock by cross-module DB queries

create Offer-specific return policy business decisions

Host may:

compose modules

map modules

auth/transport/runtime/platform work

Also verify SellerPanelComposer is now genuinely non-Offer.

Do not refactor unrelated Catalog/Order debt in R5 unless required to remove an Offer leak.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/host-offer-leak-final.md

12. CROSS-MODULE DEPENDENCY GRAPH FINAL

Generate actual project-reference and source-level dependency graph for Offer.

Required final rules:

Domain:

BuildingBlocks only as justified

Application:

Domain

Offer.Contracts

BuildingBlocks

stable foreign Contracts only

Infrastructure:

Application

module persistence/building blocks

no foreign Application

Endpoints:

Application

Offer.Contracts

owner Contracts for compatibility aliases only

no Infrastructure

Contracts:

only approved BuildingBlocks dependency if required

Verify no dependency cycles.

Produce:

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/dependency-final.md

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/dependency-final.json

13. LANGUAGE / LOCALIZATION FINAL SCAN

Scan all non-generated Offer production source.

Rules:

identifiers English

namespaces English

XML docs/comments English

no Persian prose in Domain/Application/Infrastructure

endpoint localization only through localizer/resources

no hardcoded Persian fallback response

no mixed-language architecture comments

generated migrations exempt only where generated

Also scan Offer-related Host residue.

Do not mass-edit unrelated modules.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/language-final.md

14. SOURCE SIZE / COHESION FINAL

Measure every Offer production file.

Report:




300 LOC




500 LOC




800 LOC

largest 10 files

Rules:




800 => FAIL unless generated migration/snapshot exemption

500–800 requires explicit cohesion review

no God Service / God Handler / God Endpoint

no multiple unrelated public use cases hidden in one file

Also record SellerPanelComposer LOC as external evidence that Offer extraction materially reduced it.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/source-size-final.md

15. ANTI-PATTERN FINAL GATE

Scan Offer plus Offer-related Host/owner compatibility seams for:

polling

magic sleeps

magic timeouts/intervals

catch-ignore

exception swallowing

broad catch converting everything

string-matched expected exceptions

hardcoded first-item/first-seller shortcuts

default/fallback owner creation in wrong module

service locator

direct clock

direct random ID

test-only production branch

suppressions

#pragma used to hide design issues

baseline widening

TODO/FIXME deferring mandatory Golden defect

obsolete compatibility shim

duplicated handler/service path

endpoint business orchestration

cross-module DbContext access

Expected:
AntiPattern-Gate: CLEAN

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/antipattern-final.md

16. BEHAVIOR / TEST FINAL VALIDATION

Run all relevant tests, not only focused happy paths.

Required:

full Tooba.Offer.Tests

affected Pricing tests

affected Inventory tests

affected Catalog/Party contract tests if modified

focused Host seller tests

architecture guards

solution/backend build

Add or improve tests if final scan exposes unguarded rule.

Minimum Offer behavior coverage:

create

create initial active state where supported

return policy

quantity limits

seller scoped get

seller scoped list

wrong seller / forbidden

update

lifecycle transitions

archive cannot reactivate

duplicate seller SKU

duplicate active listing

missing variant

missing seller

enriched read title

price enrichment

inventory availability

missing enrichment data

price compatibility route

inventory compatibility route

owner typed error propagation

No fake assertions.
No skipped tests to pass gate.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/validation-final.md

17. GOLDEN GUARD LOCK

Architecture tests must permanently prevent regression.

At minimum guards must fail on:

Domain → Contracts

TypeForwardedTo

Contracts namespace leak

foreign Application reference from Offer

DbContext in Application/Endpoints

Host Offer business write/read shaping

IOfferSellerPanel

broad command/query dumping files

send-and-ignore CQRS

Persian Domain/Application/Infrastructure prose

source >800 LOC

direct ID/time bypass

string-matched expected exceptions

Offer endpoint dependency on Host

Offer Infrastructure use-case orchestration

stale compatibility shims

If current guard is brittle text matching, improve it where reasonably possible.
Do not over-engineer a compiler/analyzer project in this wave.

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/golden-guards.md

18. FINAL COMPLETION DECISION

Only after all scans/repairs/tests:

Return exactly one:

PASS state

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Next-Recommended-Task: USER_REVIEW_OFFER

Allowed only if:

zero mandatory residual defects

no "grandfathered" Offer defect that contradicts Golden pattern

no hidden Host Offer ownership

no magic error compatibility seam

CQRS real

namespaces clean

folders clean

contracts clean

tests green

guards green

build green

frontend untouched

INCOMPLETE state

Module-Recovery-State: INCOMPLETE
Next-Recommended-Task: TB-TMAR-OFFER-REFERENCE-W1-R6

If ANY mandatory defect remains.

Do not label COMPLETE because a deadline/task number was reached.

19. Recovery Docs

Update truthfully:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only if facts changed

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/recovery-sot.md

If COMPLETE:
record Offer as the actual Golden Reference Module to be used for future backend module recovery.

20. Git Discipline

Only task-owned files.

No broad git add ..

Before commit:
git diff --cached --name-only

Do not stage/delete user .rar files.

At end:

HEAD == origin/main after push

no staged files

no unexpected tracked modifications

user work preserved

stashes untouched

21. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Architect-Verified-R4-State
TMAR-Execution-Mode
Frontend-Production-Changes
Physical-Structure
Namespace-Ownership
Domain-Final
Application-MediatR-Final
Contracts-Final
Infrastructure-Final
Endpoints-Final
Magic-Exception-Seam
Host-Offer-Leak-Scan
Dependency-Graph
Language-Localization
Source-Size
AntiPattern-Gate
Golden-Guards
Focused-Validation
Full-Validation
Residual-Defects
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Blockers
User-Work-Preserved
Product-Resume-Safety
Module-Recovery-State
Next-Recommended-Task

Expected only if objectively clean:
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Next-Recommended-Task: USER_REVIEW_OFFER

Otherwise:
Module-Recovery-State: INCOMPLETE
Next-Recommended-Task: TB-TMAR-OFFER-REFERENCE-W1-R6

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK