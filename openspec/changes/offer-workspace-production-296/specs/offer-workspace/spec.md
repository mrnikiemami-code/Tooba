# Spec Delta — offer-workspace

## ADDED Requirements

### Requirement: REQ-OW-01 — Table infra reuse
Offer tables SHALL use the public AppDataGrid (same as products/campaigns), not a bespoke grid.

#### Scenario: tables reuse AppDataGrid
- **GIVEN** admin and seller offer lists
- **WHEN** rendered
- **THEN** they SHALL use AppDataGrid with saved views/export/advanced+status filters

### Requirement: REQ-OW-02 — Shared 7-tab workspace
A single shared, role-gated workspace SHALL provide the 7 tabs in order with only the current tab rendered.

#### Scenario: tabs
- **GIVEN** the offer workspace
- **WHEN** a tab is selected
- **THEN** only that tab's content SHALL render and entered data SHALL persist across switches

### Requirement: REQ-OW-03 — No fabricated capability
Capabilities lacking an existing backend contract SHALL be marked as gaps and NOT implemented with invented behavior.

#### Scenario: unsupported domain
- **GIVEN** a domain without a verified contract (e.g., per-offer discount definitions)
- **WHEN** building the tab
- **THEN** the missing contract SHALL be documented and no fake UI SHALL be shipped

### Requirement: REQ-OW-04 — Real owning contracts only
Writes SHALL go through the existing owning Offer/Pricing/Inventory/Fulfillment routes and SHALL verify the server read-back or return.

#### Scenario: price and inventory persistence
- **GIVEN** an existing offer
- **WHEN** the operator saves a price or a per-location inventory adjustment
- **THEN** the request SHALL use the owning route and a fresh server read SHALL confirm the stored value

#### Scenario: fulfillment responsibility
- **GIVEN** an admin editing an offer
- **WHEN** responsibility is assigned
- **THEN** the existing admin fulfillment assignment route SHALL be used and seller surfaces SHALL remain read-only

### Requirement: REQ-OW-05 — Honest gated tabs
Gated tabs SHALL remain reachable, explain the missing contract, and SHALL NOT render fabricated discounts, events, services, or multi-currency rows.

#### Scenario: history and discounts
- **GIVEN** an edit-mode workspace
- **WHEN** discounts or history is selected
- **THEN** the tab SHALL state the missing Promotion/history contract and SHALL show no fabricated records

### Requirement: REQ-OW-06 — Edit identity lock and computed buyability
After creation the seller and variant SHALL be immutable, and buyability SHALL be derived from status, price and availability rather than a manual toggle.

#### Scenario: edit identity
- **GIVEN** an existing offer in edit mode
- **WHEN** the identity tab renders
- **THEN** seller, product, variant and channel SHALL be read-only and buyability SHALL show a derived reason when not buyable
