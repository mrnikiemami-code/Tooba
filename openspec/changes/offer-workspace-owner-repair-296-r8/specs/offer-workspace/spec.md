# Spec Delta — offer-workspace (R8 owner repair)

## ADDED Requirements

### Requirement: REQ-OW-R8-01 — Independent product and variant selection
The create workspace SHALL select a product first, then only variants of that product, SHALL display real Catalog human axis labels, and SHALL clear a prior variant when the product changes.

#### Scenario: product change
- **GIVEN** a selected product and variant
- **WHEN** the user selects a different product
- **THEN** the prior variant SHALL be cleared and only the new product's variants SHALL be offered

#### Scenario: no sellable variant
- **GIVEN** a product with no published variant
- **WHEN** the product is selected
- **THEN** the workspace SHALL show a clear empty message and SHALL NOT create a record

### Requirement: REQ-OW-R8-02 — Standard modal pickers
Seller, product and variant pickers SHALL be modal controls with server search, paging, current selection, loading/error/empty states and close-on-select, and SHALL NOT expand the form height.

#### Scenario: selection survives paging
- **GIVEN** an open picker with a selection
- **WHEN** the user moves between pages and back
- **THEN** the current selection SHALL remain marked

### Requirement: REQ-OW-R8-03 — Active-locale offer UI
Offer-owned titles, fields, tabs, messages, errors, filters, columns and actions SHALL use the active locale catalog and direction; numbers, currency and dates SHALL use the active culture/calendar; unknown languages SHALL follow the project fallback.

#### Scenario: language navigation
- **GIVEN** an unsaved draft
- **WHEN** the active language changes through navigation
- **THEN** the draft and selections SHALL be restored and the new direction SHALL apply

### Requirement: REQ-OW-R8-04 — Create and duplicate safety
Create SHALL create one real Draft only after a valid seller/product/variant identity, SHALL expose a real id and enable allowed tabs, and SHALL explain/link an existing non-archived listing instead of creating a duplicate or exposing a technical error code.

#### Scenario: duplicate listing
- **GIVEN** an existing non-archived listing for the seller/variant/channel
- **WHEN** create is attempted
- **THEN** the user SHALL see natural copy and, when authorized, a link to the existing offer

### Requirement: REQ-OW-R8-05 — Amount and number integrity
Displayed numbers SHALL follow the active locale; amount input SHALL normalize Persian/English digits and support typing, paste, deletion and selection; empty amount SHALL NOT become zero; IRR SHALL not save decimals; SKU/UUID SHALL retain meaning.

#### Scenario: empty amount
- **GIVEN** an existing price
- **WHEN** the amount is cleared and saved
- **THEN** the save SHALL be blocked and the stored amount SHALL remain unchanged

### Requirement: REQ-OW-R8-06 — Independent inventory locations
Saving one stock location SHALL update only that location's draft; a later location failure SHALL retain its draft and reason; retry SHALL execute only remaining locations and SHALL NOT create duplicate adjustments.

#### Scenario: partial inventory failure
- **GIVEN** two changed locations
- **WHEN** the first saves and the second fails
- **THEN** the first saved value SHALL be shown, the second draft/reason SHALL remain, and retry SHALL send only the second

### Requirement: REQ-OW-R8-07 — Save safety and truthful outcomes
Save/create SHALL use try/catch/finally and a synchronous lock; partial, complete, failed and uncertain outcomes SHALL be distinguished; stale snapshots SHALL not overwrite another session; dropped connections SHALL NOT trigger blind retry.

#### Scenario: connection dropped
- **GIVEN** a price write loses connection
- **WHEN** the save resolves as uncertain
- **THEN** the draft SHALL remain, no retry button SHALL be shown, and an explicit server-state reload SHALL be offered

### Requirement: REQ-OW-R8-08 — Real channel and readable lists
Both offer lists SHALL consume the real Offer channel and human variant label, use the shared AppDataGrid with product first/actions last, and SHALL only expose filters/sorts supported by the server contract.

#### Scenario: channel display
- **GIVEN** an offer with a channel
- **WHEN** the list renders
- **THEN** the active-locale channel label SHALL match the offer detail
