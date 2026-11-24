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
