# Spec Delta — offer-shipping-assignment

## Purpose
An offer's assigned shipping service(s) determine the checkout-eligible methods for the offer's shop/destination/
time window, fail-closed, in both SingleStore and Marketplace, with no phantom or cross-seller methods.

## ADDED Requirements

### Requirement: REQ-SHIP-01 — Assignment drives eligibility
The system SHALL offer a shipping method at checkout for an offer only when that method is explicitly assigned to
the offer, published and currently valid, and owned by the offer's real seller (Marketplace) / store (SingleStore).

#### Scenario: D01 assigned published service offered
- **GIVEN** a published method assigned to an offer for its seller/store/destination
- **WHEN** checkout eligibility is read for a cart containing that offer
- **THEN** exactly that method SHALL be offered

#### Scenario: D02 unassigned offer unavailable
- **GIVEN** an offer with no assigned service
- **WHEN** checkout eligibility is read
- **THEN** the offer SHALL be unavailable with a typed reason and no default method SHALL appear

### Requirement: REQ-SHIP-02 — Exclusions are fail-closed
Unpublished, expired, or foreign (cross-seller) assigned methods SHALL be excluded and SHALL never introduce a phantom
method; a group with no common remaining method SHALL be ineligible per the approved no-common-method policy.

#### Scenario: D03 unpublished excluded
- **GIVEN** an offer assigned a method that is not currently published/valid
- **WHEN** checkout eligibility is read
- **THEN** that method SHALL NOT be offered

#### Scenario: D04 foreign excluded
- **GIVEN** an offer assigned a method owned by a different seller
- **WHEN** checkout eligibility is read
- **THEN** the method SHALL be excluded with no cross-seller leakage

### Requirement: REQ-SHIP-03 — Multiple methods and sellers
The system SHALL offer all valid same-type methods (no dedupe by type) and SHALL isolate methods per seller group.

#### Scenario: D05 two same-type methods
- **GIVEN** a seller with two distinct valid methods of the same type
- **WHEN** checkout eligibility is read
- **THEN** both methods SHALL be offered

#### Scenario: D06 two sellers
- **GIVEN** two sellers each with a valid method in one cart
- **WHEN** checkout eligibility is read
- **THEN** each group SHALL show only its own seller's method

### Requirement: REQ-SHIP-04 — Destination, window, edition parity, and policy gates
Eligibility SHALL honor destination and time-window rules from Shipping facts, behave identically in SingleStore and
Marketplace, and SHALL NOT implement fee/threshold or no-common-method policy until it is explicitly approved.

#### Scenario: D08 destination-specific
- **GIVEN** a method valid for destination A and not B
- **WHEN** eligibility is read for A and for B
- **THEN** it SHALL be offered for A and excluded for B

#### Scenario: D07/D09/D13 policy gated
- **GIVEN** no approved decision for no-common-method, window semantics, or fee/threshold
- **WHEN** those cases arise
- **THEN** the test SHALL be BLOCKED_ON_DECISION and no behavior SHALL be invented
