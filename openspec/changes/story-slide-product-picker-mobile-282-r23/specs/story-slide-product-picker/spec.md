# Spec Delta

## Purpose

The shared slide editor lets an operator identify, change and preserve the product destination without losing the current choice or the save path.

## ADDED Requirements

### Requirement: Current product is explicit
The slide editor SHALL show the current product name and an accessible selected marker before and during product search.

#### Scenario: Existing product is easy to identify
- **GIVEN** an existing slide has product A
- **WHEN** the operator opens the slide editor
- **THEN** a selected-product card SHALL show A's name and a non-colour selected marker

#### Scenario: Selected option is marked
- **GIVEN** product A is selected
- **WHEN** A appears in the product results
- **THEN** its option SHALL expose `aria-pressed`, a check icon and a visible selected label

### Requirement: Selection survives lookup changes
The current product SHALL remain selected and named while search text, result pages, loading, errors or retry change the visible options.

#### Scenario: Search removes the current product
- **GIVEN** product A is selected and is not on the next result page
- **WHEN** the operator searches for another name or loads more results
- **THEN** A SHALL remain the current target and its current name SHALL remain visible

#### Scenario: Lookup fails
- **GIVEN** product A is selected
- **WHEN** the product lookup returns 503
- **THEN** an error with retry SHALL appear, no empty state SHALL replace it, and A SHALL remain selected

### Requirement: Slide save preserves media and sends the selected product
Saving an existing slide with product B SHALL send exactly one owning `update_item` PUT with B as `ctaTarget`, the current expected version and occurrence, while preserving the slide's existing media identity.

#### Scenario: Product B is saved
- **GIVEN** an existing slide has product A and media M
- **WHEN** the operator selects B and saves the slide
- **THEN** one PUT SHALL carry B, expected version and occurrence, with no upload, add or delete
- **AND** an independent reload or GET SHALL show B as the selected product

#### Scenario: Cancel keeps product A
- **GIVEN** product A is selected and the operator changes to B
- **WHEN** the operator cancels or uses Escape and confirms discard if asked
- **THEN** no mutation SHALL be sent and reopening the slide SHALL show A
