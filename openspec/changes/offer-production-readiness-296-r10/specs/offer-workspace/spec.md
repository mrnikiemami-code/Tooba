# Spec Delta — offer production readiness (R10)

## ADDED Requirements

### Requirement: R10-PUBLIC-LOCALE — Public language ownership
The public active-language endpoint SHALL use a Localization-owned public query/DTO with active registry fields and no admin edit metadata.

#### Scenario: public language list
- **GIVEN** a public caller
- **THEN** only active public language fields SHALL be returned

### Requirement: R10-SHARED-CREATE — Shared create/apply
Admin and seller create SHALL use one shared create/apply use case with independent reread matching of requested values.

#### Scenario: partial create
- **GIVEN** a later domain write fails after Draft creation
- **THEN** the created ID SHALL remain recoverable
