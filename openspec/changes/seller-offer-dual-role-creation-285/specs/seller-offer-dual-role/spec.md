# Spec Delta

## ADDED Requirements

### Requirement: Admin creates an offer for one seller
An authorized admin SHALL create a Draft Offer for a selected seller and published Catalog variant, and the owning seller SHALL read the same Offer id.

#### Scenario: Admin creates for seller A
- **GIVEN** authorized admin, seller A and a published Catalog variant
- **WHEN** admin creates one Draft Offer and reopens it
- **THEN** the Offer stores seller A and a single stable id
- **AND** seller A reads the same id from its own list/detail while seller B does not

### Requirement: Role and owner authorization
Every read/write SHALL be authorized server-side for the exact role and seller scope.

#### Scenario: View-only and foreign denial
- **GIVEN** admin view-only, seller A and seller B
- **WHEN** they invoke read/create/edit/price/inventory
- **THEN** only the authorized role and owner succeed
- **AND** denied requests persist zero changes

### Requirement: Creation is resumable without duplicate Offer
A retry of the same creation intent SHALL return the original Offer and only remaining steps SHALL run.

#### Scenario: Lost create response
- **GIVEN** the first post created or may have created a Draft
- **WHEN** the same request identity is retried
- **THEN** no second Offer is created
- **AND** the page reconciles the original id and completes remaining price/stock steps

### Requirement: Published picker is server searched and paged
The Catalog picker SHALL search and page published variants in SQL beyond the newest 100 products.

#### Scenario: Old item
- **GIVEN** more than 100 published products and an older exact code
- **WHEN** the operator searches and pages the picker
- **THEN** the old item is returned by the server
- **AND** non-published items are excluded and selection survives paging/search

### Requirement: Offer lists are server paged
Seller and admin lists SHALL apply owner scope, search, sort and bounded pagination in the server query.

#### Scenario: Lists above 200 offers
- **GIVEN** more than 200 offers and distinct SKU search keys
- **WHEN** seller or admin searches, sorts and pages
- **THEN** totals and pages are server-derived
- **AND** no full-array browser filtering is used

### Requirement: Safe cancel and honest partial state
Cancel before first write SHALL send zero business writes; partial progress SHALL remain a recoverable Draft with no false success.

#### Scenario: Cancel and partial failure
- **GIVEN** a create form with unsaved input or a partial Draft
- **WHEN** cancel, malformed response, permission refusal or retry occurs
- **THEN** no false success is shown
- **AND** cancel sends no write; partial Draft is disclosed and recoverable with the same id

### Requirement: Usable dual-role experience
Admin and seller creation SHALL reuse existing shells/design system, support fa/en, RTL/LTR, 360x640 and accessible controls.

#### Scenario: Mobile and language
- **GIVEN** admin and seller pages at 360x640 and desktop in fa/en
- **WHEN** the operator selects, searches, edits, saves and resumes
- **THEN** controls are readable, keyboard reachable and at least 44px for primary actions
- **AND** there is no horizontal document overflow
- **AND** an Impeccable before/after critique is recorded

### Requirement: Ownership and delivery stay bounded
Catalog, Offer, Pricing, Inventory, Party and AccessControl SHALL retain ownership; no unapproved schema, Stage 3 or discount work SHALL be added.

#### Scenario: Delivery audit
- **GIVEN** the implementation and evidence
- **WHEN** diff, contracts, strict OpenSpec and the OpenCode verifier are reviewed
- **THEN** no unsafe cross-domain write or migration exists
- **AND** hashes, cleanup and all eight REQs are independently checked
