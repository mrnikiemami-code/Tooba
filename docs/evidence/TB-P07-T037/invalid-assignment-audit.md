# Invalid Assignment Audit

Live `GET /v1/admin/catalog/demo/assignment-integrity` (see `live-proof.json`):

- PrimaryAtL1OrL2 = 0
- DisplayAtL1OrL2 = 0
- DuplicatePrimaryAndAdditional = 0
- MultiplePrimary = 0
- MissingPrimary = 0
- OrphanAssignments = 0

Seed path already L3-only; integrity service + post-seed `EnsureCleanOrThrow` prevent regressions.
