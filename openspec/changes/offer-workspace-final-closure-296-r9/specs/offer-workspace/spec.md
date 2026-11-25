# Spec Delta — offer-workspace (R9 final closure)

## ADDED Requirements

### Requirement: R9-FEEDBACK — Shared feedback surfaces
Offer operations SHALL use the existing shared Toast for transient success (3000ms) and the existing shared MessageBox for blocking operation errors, duplicate existing-offer navigation, uncertain/reload and dirty-exit confirmation; field validation SHALL remain inline and no duplicate persistent banner or parallel notifier SHALL exist.

#### Scenario: successful save
- **GIVEN** a save succeeds
- **THEN** one transient toast SHALL appear and no persistent success banner SHALL remain

### Requirement: R9-INVENTORY — Correct reason recovery and warehouse identity
Inventory SHALL clear obsolete validation on correction, accept a valid reason after a blank attempt, show a localized human warehouse name, preserve ownership/reserved constraints and restore isolated runtime values with an independent reread.

#### Scenario: blank then valid reason
- **GIVEN** a changed stock position
- **WHEN** the user tries blank reason then enters a valid reason and saves
- **THEN** the second save SHALL succeed and an obsolete reason error SHALL not remain

### Requirement: R9-UNIT — Catalog quantity authority
Offer conditions/inventory SHALL read Catalog unit code/name/precision/step/rounding and validate integer/fractional quantities, bounds and min/max against the owning contract.

#### Scenario: fractional unit
- **GIVEN** a product unit with decimal precision
- **WHEN** a quantity is entered
- **THEN** client and owning validation SHALL accept only values allowed by precision/step/rounding

### Requirement: R9-CHANNEL — Channel authority
Offer create SHALL NOT present a hardcoded channel as a user selection. It SHALL use a real allowed-channel authority if one exists; otherwise it SHALL document the exact gap and preserve the authoritative create/duplicate behavior.

#### Scenario: no authority
- **GIVEN** no allowed-channel contract exists
- **WHEN** the create form renders
- **THEN** the channel SHALL be shown as system policy, not an invented selectable option

### Requirement: R9-PRECREATE — Supported editable tabs
Once a valid seller/product/variant/channel identity is selected, supported editable tabs SHALL be usable as local drafts before persistence; explicit save SHALL create only one Draft and apply authorized domains with per-domain outcomes, while unsupported inventory/history gaps remain honest.

#### Scenario: pre-create price
- **GIVEN** a valid identity and no offer yet
- **WHEN** the user edits price and saves
- **THEN** exactly one Draft SHALL be created and the price SHALL be applied through Pricing

### Requirement: R9-LOCALE — Registry-backed offer locale
Offer routes/lists SHALL select locale from the DB language registry (including an existing third active locale) and format offer-owned copy/numbers/dates/currency/labels with that locale and direction, without mutating the registry or inventing a parallel locale system.

#### Scenario: third locale
- **GIVEN** an active third registry locale
- **WHEN** an offer route is opened with that locale
- **THEN** offer-owned copy and direction SHALL follow it, with the official fallback for missing keys

### Requirement: R9-RECOVERY — Scoped draft recovery
Draft recovery SHALL be keyed by actor/tenant/seller/offer, SHALL preserve shipping and unsaved fields across locale/tab/modal/error transitions, SHALL clear discarded/committed drafts and SHALL not overwrite newer server state.

#### Scenario: committed draft
- **GIVEN** a successful save
- **WHEN** the page reloads
- **THEN** the committed session draft SHALL not be restored

### Requirement: R9-OUTCOME — Truthful save outcomes
All changed domains SHALL be independently reread; partial failures SHALL preserve remaining edits; uncertain outcomes SHALL block retry until reconciliation; saving flags SHALL clear on exceptions. Atomic optimistic concurrency SHALL be used where it exists and its absence SHALL be recorded, not claimed.

#### Scenario: unknown outcome
- **GIVEN** a write loses connection
- **THEN** the UI SHALL offer reconciliation and SHALL NOT blind-retry

### Requirement: R9-MULTISTOCK — Real multi-position failure
Multi-position stock ownership SHALL be proven on real isolated runtime data with two positions, preserving the first success, the second unsaved draft/reason, retrying only the remainder and restoring independently.

#### Scenario: second position fails
- **GIVEN** two real positions changed
- **WHEN** the second write fails
- **THEN** the first saved value and second unsaved value/reason SHALL be independently verified and restored

### Requirement: R9-FIXTURES — Safe fixtures
Temporary grants/stock/offer fixtures SHALL use explicit IDs and baseline snapshots, clean up in finally, verify restoration and archive only task-created records without deletion or unrelated writes.

#### Scenario: fixture cleanup
- **GIVEN** temporary grants/stock/offer fixtures
- **WHEN** the run succeeds or fails
- **THEN** finally cleanup SHALL restore and verify every baseline

### Requirement: R9-VERIFY — Independent acceptance matrix
R9 SHALL record PASS/FAIL/NOT_RUN for all twelve requirements with real runtime, tests, build/host/db identity, timings, rereads, Impeccable audit, limitations and STOP_FOR_ARCHITECT.
#### Scenario: requirement matrix
- **GIVEN** the twelve issued requirements
- **WHEN** verification finishes
- **THEN** every requirement SHALL have PASS/FAIL/NOT_RUN with cited evidence
