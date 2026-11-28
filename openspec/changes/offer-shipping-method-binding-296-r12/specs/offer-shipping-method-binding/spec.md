# Spec Delta — offer shipping method binding

## ADDED Requirements

### Requirement: SHIP-REF (SHIP-01–03)
The Offer shipping tab SHALL list only published active Shipping methods matching the offer owner and shipping type, with localized names, stable identity, search/paging and explicit empty/failed/disabled states.

#### Scenario: owner/type filtering
- **WHEN** the shipping type or responsibility changes
- **THEN** candidates are re-resolved from the owning Shipping contract and no cross-owner, inactive or other-type method is shown.

### Requirement: SHIP-STORE (SHIP-04–08)
Offer SHALL persist mode (Automatic default, Specific explicit nonempty) and link rows atomically with revision CAS and history.

#### Scenario: one-Save multiselect
- **WHEN** a Specific set is saved
- **THEN** the assignment and every link are written in one transaction and an exact reread returns the same set.

### Requirement: SHIP-AUTH (SHIP-09–11)
Admin SHALL require `fulfillment.manage`; seller may only restrict methods for self-fulfilled offers within existing grants; forged/cross-owner/other-type/inactive methods SHALL be rejected server-side.

#### Scenario: cross-owner denial
- **WHEN** a seller submits a method owned by another seller or a platform policy
- **THEN** the write is rejected and nothing is persisted.

### Requirement: SHIP-CAS (SHIP-12–14)
Concurrent distinct intents SHALL produce exactly one winner; identical retry SHALL reconcile without duplicate facts; stale different intent SHALL be rejected with no partial writes.

#### Scenario: concurrent distinct intents
- **WHEN** two different method sets are saved at the same Offer revision
- **THEN** exactly one commits and the other receives a typed concurrency conflict with no partial link/history write.

### Requirement: SHIP-ELIG (SHIP-15–20)
Quote, selection and checkout SHALL intersect the persisted allowlist with actual owner/type/publication eligibility; group shipping SHALL use intersection; stale methods SHALL never be eligible; captured orders SHALL remain unchanged.

#### Scenario: checkout revalidation
- **WHEN** a method becomes withdrawn after offer save
- **THEN** the stale method stays visible in authoring with a warning but cannot be selected or paid, and existing captured orders are unchanged.

### Requirement: SHIP-UI (SHIP-21–23)
The approved shipping TSX SHALL be reused with shared Design System components, registry locale/direction/resources, all states and 390×844 popup reachability; other tabs remain visually unchanged.

#### Scenario: mobile reachability
- **WHEN** the picker is opened at 390×844
- **THEN** every option is reachable within viewport or scroll, with keyboard Escape/Enter and RTL logical alignment.

### Requirement: SHIP-REG (SHIP-24–26)
Affected suites and AQ-01..08 SHALL pass, OpenSpec strict SHALL validate, and the immutable SHIP-01..26 ledger plus machine gate SHALL be executed with negative controls and exact fixture cleanup.

#### Scenario: full ledger
- **WHEN** the issued matrix is executed
- **THEN** every SHIP ID is PASS/FAIL/NOT_RUN with raw evidence and no invented PASS.
