# Spec Delta — offer multi-currency pricing (R11)

## ADDED Requirements

### Requirement: MC-01 — Authoritative system currency reference
Pricing SHALL expose the authoritative active system currency reference from trusted commercial configuration and SHALL reject unsupported, inactive or display-unit currency codes with a stable typed error.

#### Scenario: unknown currency rejected
- **WHEN** a price write requests an unsupported currency code
- **THEN** Pricing SHALL fail closed with a stable currency error and SHALL NOT write a price row

#### Scenario: active currency accepted
- **WHEN** a price write requests an active configured system currency for the store market
- **THEN** Pricing SHALL accept it under the existing advisory lock and revision CAS

### Requirement: MC-02 — Multiple authored offer prices
Pricing SHALL allow one current base authored price per offer per active system currency while preserving the store default currency as the primary list and quote amount; Offer SHALL read those prices only through Pricing contracts.

#### Scenario: two currencies on one offer
- **WHEN** a seller records an IRR price and a USD price for the same offer
- **THEN** the detail SHALL expose two current base price rows with independent revisions and the store default currency remains primary

#### Scenario: independent revision CAS
- **WHEN** the USD row is updated with its own revision
- **THEN** the IRR row and its revision SHALL remain unchanged

### Requirement: MC-03 — IRR precision
IRR authored prices SHALL be integer amounts; the server SHALL round/enforce the currency scale and the UI SHALL not accept or display fractional IRR.

#### Scenario: fractional IRR
- **WHEN** an IRR price with a fractional amount is submitted
- **THEN** the authored value SHALL follow the IRR scale policy and never persist a fractional display

### Requirement: MC-04 — Registry locale and direction
All new pricing labels SHALL come from the shared message catalogs for every configured language and the shared workspace SHALL use the registry direction.

#### Scenario: third locale
- **WHEN** the Offer pricing tab is opened in a configured RTL or LTR locale
- **THEN** labels, direction and currency presentation SHALL follow the language registry, not a feature-local authority

### Requirement: MC-05 — UAT operational corrections preserved
The Offer workspace SHALL NOT show a market column, duplicated variant labels or excess placeholder copy; authorized edit-mode inventory position provisioning SHALL work through existing owner APIs; the header SHALL remain contained without unintended overflow.

#### Scenario: edit-mode inventory provisioning
- **WHEN** an authorized admin edits an offer with an eligible location and no position
- **THEN** the shared inventory tab SHALL provision the position through the existing owner endpoint and reveal the physical adjustment controls

### Requirement: MC-06 — Regression
Existing channel, create/edit, inventory, history, quote and single-currency behavior SHALL remain unchanged.

#### Scenario: prior single-currency flow
- **WHEN** only the store default currency price exists
- **THEN** list/quote/checkout resolution SHALL be identical to the pre-change behavior
