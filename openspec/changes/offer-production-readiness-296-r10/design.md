# Design — offer-production-readiness-296-r10

## Context
Baseline product commit 295d8241d277f4320f90ee9e5e75e907dfa5905f. R9-R1 remains the working visual/functional baseline, not accepted production readiness. The exact current owner contracts and existing architectural locks must be traced before implementation.

## Goals / Non-Goals
- Goals: R10-01..14, all 42 mandatory cases, eight AQ gates, independent readback and owner UAT readiness.
- Non-goals: unrelated architecture evacuation, new visual design, funding/commission/settlement, bulk workspace, unsupported shipping service or multi-currency policy.

## Decisions
1. Offer owns listing identity and channel enforcement. Enumerated channel names are not authorization. Establish the existing policy and use one authoritative allowed-channel contract in selection, lookup and create.
2. Inventory owns eligible locations and idempotent provisioning after a persisted Offer exists. Pre-create drafts reference eligible location IDs, never fabricated stock positions.
3. One shared feature use case orchestrates role-specific typed transports; server-owning commands handle validation and persistence. Reconcile each committed domain independently; retain failed changes. Atomic conflict protection belongs to the affected owning write boundaries, not client pre-read.
4. History is a projection of real recorded changes and reliable new owner events; no fabricated historical data or generic Host business authority.
5. Catalog owns unit precision/step/rounding. Promotion funding remains outside R10.
6. Canonical shared Localization registry/resolver/provider supplies actual URL prefix, direction, calendar and culture. No hardcoded additional language, feature-specific override or unsafe type cast. Apply only minimal necessary shared changes and test adjacent consumers.
7. Route composition, feature behavior, typed transport and shared rendering responsibilities stay separated. Existing architecture guard thresholds and historical baselines remain immutable. No new Host business code.

## Risks / Trade-offs
- A genuinely undecided channel or commercial policy must be isolated and disclosed, not guessed.
- If atomic protection requires migration, document module-owned plan and obtain required authorization before applying it. No live migration.
- Existing unrelated failures must be enumerated as exact baseline versus candidate sets, not waived for Offer-owned files.
- UI and domain outcomes may be partially committed across module boundaries; show truthful state and safe reconciliation rather than pretend distributed atomicity.

## Implementation planning gate
Before product edits, worker SHALL add source-grounded architecture plan and delivery manifest mapping actual UI→transport→authorization→CQRS→domain→persistence→independent read. These decisions set boundaries, not evidence that all contracts already exist. Document actual files/owners, test fixtures, and material branch coverage. No automatic next task.
