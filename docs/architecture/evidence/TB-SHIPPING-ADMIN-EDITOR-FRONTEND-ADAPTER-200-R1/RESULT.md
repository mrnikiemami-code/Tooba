# TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1 — Worker Result

Mode: BOUNDED_BASELINE_PRESERVATION_SESSION_EPOCH_AND_UNKNOWN_OUTCOME_REPAIR
Base SHA: `29cf0117eeaa087c7edc5e4cf1a0240d1a9f5e8e` (fast-forward only from `44d742848`)
Parent: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200 (review disposition REPAIR_REQUIRED)
Claim: 2026-10-22T23:25:10Z · Result: 2026-10-22T23:52:00Z · Elapsed ≈ 27 min (25-min hard box exceeded by ~2 min during final evidence/push; no new coding after the test loop closed)

Scope: existing feature only (`src/frontend/features/admin-shipping-authoring/**` + its focused tests).
No backend, schema, package/config, baseline, AGENTS/skill, shared lib/auth transport, route, page,
preview or design-system change. Public type/result shapes changed only inside this feature.

## Regression-first evidence (executed before repair)

New suite `features/admin-shipping-authoring/repair-200-r1.test.ts` was written first and run on the
unchanged TB200 implementation: **0 passed / 12 failed**. Observed behaviour, not a source argument:

- R200-01: with a one-group baseline and a method-text-only edit, the pre-fix session produced
  `BASELINE_GROUPS 1 UNSAVED true` and the exact body
  `{"expectedRevision":4,"regionGroups":[],"methodTranslations":[{"languageId":"…","name":"y","description":null}]}`
  — i.e. `regionGroups:[]` (whole-collection DELETE) from a text-only edit.
- R200-02: `epoch` did not exist on the session; `loadSucceeded`/save completions ignored it, and the
  A→B→A reopen reused `generation:1`, so the old A completion was accepted.
- R200-03: after `saveFailed(outcomeUnknown)` an `edited` action reset `save` to `clean`.
- R200-04: the decoder accepted a zero GUID (`"must reject: 00000000-…"` returned `ok:true`).

Post-repair: same suite **12/12 pass**, and the whole feature set (repair + adapter + session)
**25 passed / 0 failed**, `tsc --noEmit` exit 0.

## R200-01 — Baseline-aware whole-tree replacement

`buildEditorDocumentBody(baseline, edits, resolver)` now takes the confirmed server document and
serializes:

| Intent | Serialized `regionGroups` |
| --- | --- |
| `edits.regionGroups` omitted | Complete retained baseline tree: same `groupId`, order, areas (re-resolved), every row id and exact decimal text; `titles: []` |
| `edits.regionGroups: []` | `[]` — the only path that removes the collection |
| `edits.regionGroups: [...]` | The operator's ordered replacement with active-language title patches |

`methodTranslations` is `[]` when untouched (contract: no text change), so an untouched inactive
language is never echoed as an active write. Every retained selection still goes through the
ancestry resolver: a stored selection with no authoritative chain returns `needsResolution` and NO
body, never `[]` or an omitted group. The baseline object is not mutated (asserted by JSON snapshot).

Active-language boundary: a method/title intent for a language id that is not in the baseline's own
active registry is rejected as `invalidIntent` (`languageUnavailable`) — never silently filtered into
a successful smaller patch.

## R200-02 — Never-reused session epoch

`EditorSessionState.epoch` replaces the per-draft `generation`. It is monotonic and never reset:

- `loadStarted` → `epoch + 1`;
- `discardConfirmed` → `epoch + 1` (both switch and same-draft discard);
- `reconcileStarted` → `epoch + 1`.

Every `loadSucceeded` / `loadFailed` / `saveStarted` / `saveSucceeded` / `saveFailed` /
`reconcileSucceeded` / `reconcileFailed` carries the epoch; a completion whose epoch (or in-epoch
`requestId`) does not match returns the SAME state object. Proven: A load pending → switch B →
reopen A → old A success/failure ignored; old A save success ignored even when the numeric request
id is reused after discard/reopen; same-draft close/discard/reload invalidates old work; legitimate
current-epoch completion succeeds; duplicate Apply still gated.

## R200-03 — Latched uncertain commit

`saveFailed(outcomeUnknown)` sets `commitUncertain: true`. It survives later `edited` actions (status
stays `outcomeUnknown`) and `selectApplyRequest` refuses with reason `uncertain`; `saveStarted` is
ignored, so no replay and no client-side revision bump. `outcomeUnknown` is never treated as a known
failure.

Two explicit exits only:
1. `reconcileStarted` / `reconcileSucceeded` (an explicit fresh read at a new epoch). Stale
   reconciliation results are ignored; success installs the confirmed server document, clears the
   latch, keeps the operator's edits dirty (nothing silently discarded) and allows a NEW deliberate
   save.
2. `discardConfirmed` → baseline becomes `null`, load `idle`, edits cleared, latch retained, so the
   old baseline cannot be resubmitted; a fresh `loadStarted` + `loadSucceeded` is mandatory and clears
   the latch.

Conflict / validation / error behaviour and same-revision no-op adoption are preserved (tested).

## R200-04 — Read-contract and target integrity

Decoder now rejects: empty GUIDs (server guarantees genuine identities), `type.draftId` differing
from the root draft id, duplicate content-language ids, a declared-default count other than exactly
one, a `defaultContentLanguageId` that is not the declared default, a configured schedule that is not
exactly seven unique ISO weekdays, and malformed `TimeOnly` text (`HH 00–23`, `mm/ss 00–59`,
fraction 1–7 digits — `99:99:99` rejected). No timezone/DST/cutoff policy was added and raw
text/time/decimal representation is untouched.

Write target integrity: a resolved ancestry must match the requested `kind`, carry the canonical
required ancestors for that kind, omit every forbidden deeper ancestor, and resolve to the SAME
terminal id as the read selection. Same-kind-but-different-id, missing terminal, wrong kind and
extra ancestors all return `invalid-intent` (never rewritten to another destination).

## Public signature delta (inside the feature only)

- `buildEditorDocumentBody(baseline: EditorDocument, edits: EditorEdits, resolver)` (was
  `(expectedRevision: number, edits, resolver)`).
- `EditorRequestBody` gains `{ status: "invalidIntent"; issues: EditorIntentIssue[] }`.
- `EditorSessionState`: `generation` → `epoch`; adds `commitUncertain`, `reconcile`,
  `reconcileFailure`; `switchRequested` on a clean session switches immediately.
- New actions `reconcileStarted` / `reconcileSucceeded` / `reconcileFailed`; `loadStarted` keeps the
  composed state for a same-draft reload and is refused while an unknown commit is latched.
- `EditorApplyRequest` blocked reasons add `invalid-intent` and `uncertain`.

## Commands actually executed

| Command | Count | Result |
| --- | --- | --- |
| `node --experimental-strip-types --test …repair-200-r1.test.ts` (pre-fix) | 1 | 0 pass / 12 fail (recorded above) |
| Same only, during repair (bounded iteration) | 3 | converged to 12/12 |
| `node --experimental-strip-types --test` repair + adapter + session | 1 | 25 pass / 0 fail |
| `npx tsc --noEmit` | 2 | 2 errors (test-helper narrowing), then exit 0 |

No build, whole-discovery suite, Next server, browser/HTTP probe, backend test, DB/container, or live
network probe was run. The four unrelated source-size guard offenders were NOT re-run or fixed; no
baseline was widened.

## Process ledger (exact, not relabelled)

TB200 reported three corrections against an allowance of two. This R1 adds: three pre-fix test-run
iterations during the repair loop (bounded, test-only), one test-helper type narrowing fix, and one
fixture assertion corrected to assert the exact decimal text instead of a `JSON.stringify`
round-trip. These are recorded as-is; no retrospective compliance claim is made.

## Limitations

- The write-side ancestry port remains intentionally unimplemented until the UI task supplies it from
  the existing authoritative Geography resolution path; until then an unresolved retained selection
  correctly returns `needs-resolution`.
- `reconcile*` is a minimal explicit state transition, not a generic workflow; localized error
  presentation and the rendered screen remain the next dependency after Architect review.
- No visual, live-registry or production acceptance is claimed.

Status: PENDING_ARCHITECT_REVIEW. Worker PASS != Architect ACCEPT.
