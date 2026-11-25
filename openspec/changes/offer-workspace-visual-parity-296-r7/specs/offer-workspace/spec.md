# Spec Delta — offer-workspace (R7 visual parity)

## ADDED Requirements

### Requirement: REQ-OW-R7-01 — Approved visual parity
The shared offer workspace SHALL match the approved OpenDesign 295 layout: sticky four-part summary header, numbered RTL tab bar, card hierarchy, control density and responsive action bar.

#### Scenario: seven tabs and header
- **GIVEN** an existing offer
- **WHEN** the workspace renders
- **THEN** the header summary, numbered tabs in order, and only the selected tab panel SHALL match the approved layout at 360/768/1440

### Requirement: REQ-OW-R7-02 — Orders-standard offer lists
Admin and seller offer lists SHALL use the shared AppDataGrid with the Orders list hierarchy: right heading/breadcrumb, left create action, product first in RTL, compact standard row actions at the logical end, understandable currency/Jalali formatting.

#### Scenario: admin list
- **GIVEN** the admin offer list
- **WHEN** rows render
- **THEN** seller name, real variant code/media (when present), Persian digits, Jalali dates and the standard compact action cell SHALL be shown

### Requirement: REQ-OW-R7-03 — Honest and natural content
The workspace SHALL NOT expose raw UUIDs, internal architecture vocabulary, or saveable controls without a real contract; unsupported sections SHALL show short human states and SHALL NOT fabricate records.

#### Scenario: gated tabs
- **GIVEN** discounts or history with no contract
- **WHEN** the tab opens
- **THEN** it SHALL show a short unavailable state and no fake records or save controls

### Requirement: REQ-OW-R7-04 — Save verification and unsaved guard
Every saved domain SHALL use its owning route; shipping changes SHALL join the unsaved guard, and shipping success SHALL be confirmed by an independent detail re-read.

#### Scenario: shipping save
- **GIVEN** an admin changes responsibility
- **WHEN** changes are saved
- **THEN** the workspace SHALL re-read the offer and only report success if the stored responsibility/type match
