# Spec Delta — dev-seed-stale-catalog-variant

## Purpose

A Development seed Offer must never be reused merely because its `SellerSku` matches; it is valid only while the
Catalog variant it references still exists. A stale dev-seed Offer is rebuilt on a live variant so the
Development bootstrap remains repeatable after a Catalog demo reset, without relaxing Inventory's integrity guard.

## ADDED Requirements

### Requirement: REQ-SEED-01 — Reuse validates the Catalog variant
The Offer Development seed gateway SHALL reuse a surviving seed Offer only when the Catalog variant referenced by
its `CatalogVariantId` still exists.

#### Scenario: S01 stale OOS Offer points at a deleted variant
- **GIVEN** a Development seed Offer (`SellerSku=DEV-SEED-OOS`) whose Catalog variant was deleted by a Catalog demo reset
- **WHEN** the Development bootstrap runs
- **THEN** the gateway SHALL NOT treat the row as valid and SHALL resolve a live replacement variant

### Requirement: REQ-SEED-02 — Stale dev-seed Offer is rebuilt on a live variant
When the referenced variant no longer exists, the gateway SHALL rebuild only that marked dev-seed Offer on a live
(preferably published) Catalog variant, keeping exactly one Offer per `SellerSku`; a subsequent bootstrap run
SHALL reuse the rebuilt Offer with stable ids and no duplicate seller/Offer.

#### Scenario: S02 bootstrap after reset
- **GIVEN** a stale OOS seed Offer after a Catalog demo reset
- **WHEN** the normal Development bootstrap runs against the fixed code
- **THEN** the Host SHALL become healthy and the OOS seed SHALL complete without `inventory.catalog_variant.missing`

#### Scenario: S03 second bootstrap is idempotent
- **GIVEN** the rebuilt OOS seed Offer on a live variant
- **WHEN** the Development bootstrap runs a second time
- **THEN** no duplicate Offer/seller/membership SHALL be created and the links SHALL stay valid

### Requirement: REQ-SEED-03 — Inventory and roles stay authoritative
Inventory SHALL continue to fail closed on an invalid variant, and existing dev seller/admin principals SHALL NOT
be escalated or altered beyond the task-owned fixture.

#### Scenario: S04 Inventory guard remains effective
- **GIVEN** an invalid variant presented to the Inventory boundary
- **WHEN** opening a stock position
- **THEN** Inventory SHALL return the typed `inventory.catalog_variant.missing` error and write no stock

#### Scenario: S05 seller/admin roles intact
- **GIVEN** the Development fixture roles
- **WHEN** the repair runs
- **THEN** authorized actors remain authorized with no unsafe grant change

### Requirement: REQ-SEED-04 — Bounded cleanup and honest evidence
The repair SHALL touch only task-owned fixture data, leave the canonical Host/frontend and shared dev data
untouched, and record source→observed evidence with a validating producer; unproven connected cases SHALL be
`NOT_RUN`, never fabricated `PASS`.

#### Scenario: S06 canonical untouched
- **GIVEN** canonical Host 5088 and frontend 3000
- **WHEN** the task completes
- **THEN** both remain healthy and no shared DB is mass-reset or dropped

#### Scenario: S07 build, scope and provenance
- **GIVEN** the candidate sources
- **WHEN** verification runs
- **THEN** the Host graph builds with 0 errors, the diff is limited to the Offer seed files, and the producer
  records observed facts with a hash

### Requirement: REQ-SEED-05 — No eligible target means withdraw, never a fake variant
When a marked Development OOS seed Offer references a deleted Catalog variant and there is no eligible live
Catalog target (the Catalog Demo intentionally seeds all-Draft, so no published variant and no live template), the
system SHALL NOT attach the Offer to a fabricated variant and SHALL NOT leave it active/publicly exposed; the
owning Offer gateway SHALL withdraw (archive) the marked dev-seed row via the aggregate, leaving no active dangling
OOS Offer, no Inventory command on a missing variant, and no Promotion member referencing a dangling Offer. The
behavior SHALL be idempotent on a subsequent bootstrap.

#### Scenario: S08 no eligible target withdraws the stale OOS
- **GIVEN** a stale marked OOS Offer and an all-Draft Catalog with no live published/template variant
- **WHEN** the normal Development bootstrap runs
- **THEN** the OOS Offer SHALL become Archived (not Active), no new Offer or fake variant SHALL be created, no
  Inventory stock command SHALL target the missing variant, and a second bootstrap SHALL be a no-op
