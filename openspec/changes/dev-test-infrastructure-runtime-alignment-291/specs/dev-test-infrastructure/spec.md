# Spec Delta — dev-test-infrastructure

## Purpose
A single, versioned dev/test runtime contract and a one-command delivery proof, so frontend/backend/database stay
aligned and changed pages are verifiably reachable — reused across domains and tasks instead of per-task harnesses.

## ADDED Requirements

### Requirement: REQ-INFRA-01 — Single source of truth
The dev/test runtime (bases, db, discovery endpoints, pages, headings, backup) SHALL be defined only in
`docs/opencode/test-infra/DEV-TEST-ENV.json`.

#### Scenario: a harness reads config
- **GIVEN** a test harness needs backend/frontend/db/actor info
- **WHEN** it runs
- **THEN** it SHALL read DEV-TEST-ENV.json / runtime discovery, not hardcoded old ports/dbs/ids

### Requirement: REQ-INFRA-02 — Delivery proof, not home-200
Delivery SHALL be accepted only with proof of: frontend build id + reachability, backend health+provenance, database
reachability + migration presence, and real access (expected heading present) to each changed page.

#### Scenario: changed page verified
- **GIVEN** a changed offer page
- **WHEN** verify-dev-alignment runs
- **THEN** it SHALL fetch the page and confirm 200 AND the expected heading text is present; otherwise FAIL

### Requirement: REQ-INFRA-03 — Alignment by one command
`scripts/dev/verify-dev-alignment.ps1` SHALL report git SHA, frontend BUILD_ID, backend dll SHA, DB migration column,
and per-page results, and return non-zero on any mismatch.

#### Scenario: mismatch fails
- **GIVEN** a stale frontend build or wrong db
- **WHEN** the verify script runs
- **THEN** it SHALL return FAIL (non-zero)
