# Spec Delta — offer-workspace architecture closure

## ADDED Requirements

### Requirement: AQ-R9R1-01 — Central registry locale
Offer routes and lists SHALL use the shared registry-backed locale route/middleware and public language read contract; no feature query parameter, feature-local locale authority or admin-language deep import SHALL be introduced.

#### Scenario: third locale route
- **GIVEN** an active registry locale with a route prefix
- **WHEN** the offer route is opened under that prefix
- **THEN** the offer-owned surface SHALL use its direction/culture and message catalog

### Requirement: AQ-R9R1-02 — Cohesive workspace source
The workspace SHALL keep orchestration, local detail and rendering in cohesive files below the existing source-size guard without changing R8 layout or R9 behavior.

#### Scenario: size guard
- **GIVEN** the changed workspace files
- **WHEN** the frontend architecture guard runs
- **THEN** no new oversized file or growth against baseline SHALL be attributed to this task

### Requirement: AQ-R9R1-03 — Shared create-and-apply
Admin and seller create SHALL share one create-and-apply use case; route pages SHALL provide transport only and the use case SHALL verify every requested value on independent reread.

#### Scenario: partial create
- **GIVEN** a created Draft whose later domain write fails
- **THEN** the created ID SHALL remain recoverable and the mismatch/failure SHALL be explicit

### Requirement: AQ-R9R1-04 — Public language ownership
The public active-language read SHALL use a Localization-owned query and public DTO, not the admin query/DTO or admin edit metadata.

#### Scenario: public list
- **GIVEN** an unauthenticated/public caller
- **THEN** only active public language fields SHALL be returned
