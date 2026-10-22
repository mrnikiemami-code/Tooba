# TB-SHIPPING-ADMIN-EDITOR-INTEGRATION-CONTRACT-198 — RESULT (source analysis only)

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1 · Channel `tooba-main` · Worker `tooba-worker-01` (cursor)
Parent: `TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197-R1`
Mode: `BOUNDED_ATOMIC_EDITOR_INTEGRATION_CONTRACT_SOURCE_ANALYSIS_ONLY`
Expected-Base-SHA / Base-SHA: `e33ef067b2705b98a82ee977f591eba8c860eded` (`HEAD == origin/main` after a
documentation-only fast-forward from `51ac96037`; the diff was solely the 197 review, recovery pointer and
`AGENTS.md`; no source divergence).

## What was produced

1. `EDITOR-FIELD-API-MATRIX.md` — every approved editor field/action mapped to its real frontend symbol and
   fixture-vs-real status, its authoritative backend fact, existing route/port/domain symbol, wire identity and
   presence/unit meaning, validation owner, read/write support today, the concrete missing fact and the
   recommended home.
2. `ATOMIC-EDITOR-CONTRACT-PROPOSAL.md` — one proposed atomic editor document (GET/PUT), illustrative JSON,
   ordering (all read-only dependency facts before the first tracked mutation, whole-graph preflight, ONE
   revision/timestamp advance, ONE save, response composed without a post-commit fallible read), reuse/refactor
   points, a future acceptance matrix, and the decisions still needing approval.
3. This `RESULT.md`.

## Key source-backed findings

- Decision section 9's atomic requirement is unmet: six independent committing capabilities
  (`ReplaceRegionGroupsAsync`, `ReplaceGroupRateRowsAsync`, `ReplacePresentationAsync`,
  `ReplaceWeeklyScheduleAsync`, `ReplaceMethodSettingsAsync`, `ReplaceTypeAssignmentAsync`) each advance the
  same root `Revision`, so a browser sequence cannot be one all-or-nothing Apply.
- The Domain already provides the exact reuse seam: pure `Preflight*` methods plus `Apply*` that advance the
  revision once. No second rule engine is needed.
- The editor preview is **fixture-driven**: sample shipping-type ids (`ordinary|heavy|…`), 10-node place tree,
  `fa/en` content languages, `IRR` currency, `cutoffHours = 4`, and `status: active|inactive`. TB-188 context is
  the only authority for languages/currency; Fulfillment is the only authority for the type catalog.
- `cutoffHours` is a preview/UI fact only: the approved sentence is "cutoff before start of that working day's
  interval"; weekly schedule persistence has **no** cutoff, and the `0..48` bounds are UI facts, not policy.
- Draft type selection and freight/calculation modes are **selections**, not readiness, funding or carrier
  integration; none of the latter has a backend fact.
- The admin preview owner dropdown is not an authorisation source; no seller-picker seam exists for admin.

## Decisions still requiring approval (recorded, not invented)

1. Cutoff: per method vs per weekday, unit/precision/bounds, closed-day and timezone/DST semantics, draft-only
   vs runtime input.
2. Draft status/activation: only a read-only completeness projection is proposed; no activation authority.
3. Admin seller/tenant selection: needs a real authorisation source before any selector.
4. Carrier registry/credentials and free-funding party: out of scope, no seam exists.

## Recommended first implementation slice

**Slice A — atomic backend editor document in the Shipping module only** (one additive Contracts document,
one Application query + command + validators, one directory method implemented by
`ShippingAuthoringDraftDirectory` reusing the existing `Preflight*`/`Apply*` split, admin+seller routes,
request-classification update). Later, only as dependencies: UI adapter/feature wiring, then live
configured-registry/owner acceptance.

## Actually-run checks

`SOURCE_ANALYSIS_ONLY` — no build, no test, no typecheck, no `dotnet ef`, no Host/server, no browser/HTTP probe,
no database/container/schema/migration/seed, no package/image operation, no cache/output cleanup, no shared
port/process operation. Git read/fetch and a fast-forward documentation pull only.

## Scope compliance

No `src/**`, `AGENTS.md`, skill, manifest, certification, deployment or approved-rules change. Only the three
allowed evidence files plus one recovery pointer line were written. Schema/Data/Flag changes: **NONE**.
Task PASS here means the source analysis is complete — never an approved production API or functional
acceptance.
