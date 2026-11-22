# Spec Delta

## Purpose

Editing an existing seller offer must change only the section the operator deliberately edited, with the owning seller, offer and inventory position bound on the server.

## ADDED Requirements

### Requirement: SKU-only edit preserves every stock position
When the seller changes only the SKU, the system SHALL send one offer patch and SHALL NOT write price or stock.

#### Scenario: Two positions remain 3 and 7
- **GIVEN** one offer has stock positions 3 and 7
- **WHEN** the seller changes only its SKU and saves
- **THEN** exactly one owning offer patch SHALL be sent
- **AND** independent reload/GET SHALL show the new SKU, the same price, the same reservation and positions 3 and 7

### Requirement: Draft and cleared limits are lawful
A Draft offer SHALL be editable without a status change, and explicit removal of both purchase limits SHALL persist.

#### Scenario: Draft SKU edit
- **GIVEN** an offer is Draft
- **WHEN** only the SKU is changed
- **THEN** the patch SHALL not require a status transition and the offer SHALL remain Draft

#### Scenario: Clear both limits
- **GIVEN** both purchase limits have values
- **WHEN** the operator clears both and saves
- **THEN** an independent read SHALL return both limits as null

### Requirement: Price and SKU edits are independent
A stale form that changes only SKU SHALL NOT overwrite a newer price written by another session.

#### Scenario: Concurrent price and SKU
- **GIVEN** two sessions opened the same offer
- **WHEN** session one saves a new price and session two saves only a new SKU from its older form
- **THEN** an independent read SHALL contain both the new price and the new SKU

### Requirement: Stock edits target one named position
A stock write SHALL bind the owning offer, seller and one stock position; a write without a target SHALL be refused when the offer has multiple positions.

#### Scenario: Update the second position
- **GIVEN** positions 3 and 7 exist
- **WHEN** the seller sets the second position to 8
- **THEN** independent read SHALL show 3 and 8 with total 11
- **AND** SKU, price, reservation and the first position SHALL be unchanged

#### Scenario: Ambiguous or foreign target
- **GIVEN** an offer has multiple positions or a caller names a position of another seller/offer
- **WHEN** a stock write omits the target or names the foreign target
- **THEN** the write SHALL be refused with no stock change

### Requirement: A response must prove the requested effect
A successful UI state SHALL require the response to match the expected offer, seller and value; an unrelated, stale or disconnected result SHALL not show success.

#### Scenario: Wrong response
- **GIVEN** a save expects offer A and value V
- **WHEN** the response names another offer/seller or returns a stale value
- **THEN** the UI SHALL show a non-success state and an independent GET SHALL decide the durable outcome

#### Scenario: Disconnect after send
- **GIVEN** a write is sent but the response is lost
- **WHEN** the client cannot read the response
- **THEN** the client SHALL not claim success or automatically re-send; reconciliation SHALL use an independent read

### Requirement: Cancel and mobile editing are safe
Cancel SHALL send no write and mobile editing SHALL remain reachable at 360×640 with an open, dirty form.

#### Scenario: Cancel
- **GIVEN** the form has an unsaved change
- **WHEN** the operator cancels
- **THEN** zero write requests SHALL be sent and reopening SHALL show the prior values

#### Scenario: Mobile geometry
- **GIVEN** the form is open and dirty at 360×640
- **WHEN** the operator follows normal scrolling
- **THEN** the position identity, save and cancel controls SHALL be readable, reachable and clickable with zero horizontal document overflow

### Requirement: Independent section edits survive
Saving or failing one section SHALL not clear or commit an unsaved edit in another section.

#### Scenario: Two sections
- **GIVEN** the operator changes stock and another section in one session
- **WHEN** the first section saves or fails
- **THEN** the second section's input SHALL remain uncommitted until deliberately saved
- **AND** after the deliberate save and reload both values SHALL be correct

#### Scenario: No-change save
- **GIVEN** the form has no changes
- **WHEN** the operator presses save
- **THEN** zero write requests SHALL be sent
