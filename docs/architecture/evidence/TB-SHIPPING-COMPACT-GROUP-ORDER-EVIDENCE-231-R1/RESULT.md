# RESULT — TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231-R1 — 2026-10-27

Read `docs/architecture/evidence/TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231/{TASK.md,RESULT.md,ARCHITECT-REVIEW.md}`
and `docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md`.
Base `5cd72434e` (fast-forward from `ded6ad91d`). Status: **EXECUTED — the five blocking source facts of the
231 review are repaired inside the SAME unshared value/mapping/migration**, not accepted, no user Visual
ACCEPT, no live writer/HTTP/parity/UI/runtime change.

The user's dedicated review window and the live Host/FE, the runtime data, the buyer account/cart, the admin
draft/rates/week/pointer/assignment and the preview settings were **not** touched: no enumerate, automate,
reload or close of that window, no rebuild/restart, no shared migration apply.

## A — long tokens end-to-end

`ShippingGroupAcceptedEvidence.SourceRevision` and `.SelectionVersion` are now `long` (they were `int`), matching
the Shipping 219/216 `long` source tokens. Both migrate as PostgreSQL `bigint`, with no cast/narrowing anywhere in
the value, the group transfer or the mapping. `SupportedFormatVersion` stays `1` and a non-positive token stays a
stable refusal (`...source_revision.invalid` / `...selection_version.invalid`) — no overflow, no rounding, no
"must-increment" assumption.

Proven with `3_000_000_000` (source) and `4_294_967_296` (selection) — both beyond `int.MaxValue` — persisted and
read back **exactly** through the draft group AND the confirmed order group, and additionally read back from the
raw columns (`accepted_source_revision::text || '/' || accepted_selection_version::text`), not only through EF.

## B — full owner identity replaces the optional responsibility flag

`accepted_responsibility` is gone. The value now carries one required, Order-owned
`ShippingGroupOwner { Kind, TenantKey?, SellerPartyId? }` with the three exact shapes reusing the source capture
semantics: `Store(tenant)` (no seller), `Marketplace()` (neither), `Seller(sellerPartyId)` (no tenant). No
alias, no new owner framework, no Shipping.Domain/Infrastructure reference, no user id, no current-pointer
inference, no invented tenant.

- All-or-none coherence is validated **before** attach/materialize: a missing owner is `...owner.required`, a
  malformed/mixed shape (Seller without id, Store without/with the wrong companion, Marketplace with either,
  unknown enum, blank/oversized tenant) is `...owner.invalid`, and a `Seller` key that differs from the group's
  own `SellerPartyId` is `...owner.mismatch` — the group's seller is still the selling party and is never used
  as the owner for `Store`/`Marketplace`.
- The tenant key is trimmed but **never lowercased/normalised** (`Store-Alpha` ≠ `store-alpha`, `Ordinal`).
- Legacy groups keep the entire evidence `NULL`; there is no heuristic upgrade/backfill.
- The DB enforces the same three shapes (`accepted_owner_kind IN (0,1,2)` plus the tenant/seller nullability
  matrix in `ck_*_accepted_evidence_shape`), so a corrupt persisted row cannot hydrate into a full-looking owner.

## C — actual accepted receiving facts, lossless hours

The invented `DateOnly start..end` range (which the review correctly called out: it even allowed `10-31..11-07`
for a `min2/max3` promise) is replaced by the real accepted facts:

- `ReceivingDate` (the chosen date) + `ReceivingOpenTicks`/`ReceivingCloseTicks`, the actual opening/closing
  bounds of that date, stored as `bigint` ticks `0..TicksPerDay-1` — **not** `time`/`timestamp`, because PostgreSQL
  microsecond precision cannot represent every 100 ns `TimeOnly` tick. `09:00:00.1234567` and `17:00:00.7654321`
  round-trip exactly (asserted from the loaded entity and from the raw column text).
- A same-day positive interval is required (`open < close`, same date); inverted, zero-length, negative or
  `> TicksPerDay-1` bounds are `...receiving_interval.invalid`, a default `ReceivingDate` is
  `...receiving_date.invalid`, and an empty timezone is `...time_zone.invalid`.
- `MinimumDeliveryDays`/`MaximumDeliveryDays` remain optional metadata only — they are **never** mapped onto the
  date endpoints or `LeadDays`, and must be both-absent or a real `0 ≤ min ≤ max ≤ 365` pair.
- A group's chosen date is required when evidence is present: the evidence date must equal the draft's
  `SelectedDeliveryDate` (`...schedule.mismatch`), and `ApplySchedule` now refuses any date/window change while
  evidence exists **before mutation**, so stale evidence can never survive a changed customer schedule. The
  window text is compared as the group's own stored representation — no human label is parsed. Legacy groups
  (no evidence) keep the exact old behaviour.

## D — exact financial facts, coherent fixture

- Every accepted amount is checked against `numeric(18,2)` **before** attach: negative → `...amount.invalid`, a
  non-zero fraction beyond 2 decimals or a value above `9_999_999_999_999_999.99` →
  `...amount.unrepresentable`. Nothing is rounded, truncated, computed or re-priced.
- Conservation for this Manual captured slice: `freight + packaging == payableAtCheckout + carrierCollect`
  (`...amount.conservation_invalid`); one side is never recomputed and no payer/funder is invented. Explicit `0`
  is lawful, the maximum storable value is accepted, and a split (`90,000 + 30,000` for `100,000 + 20,000`) is
  lawful. The DB repeats the same conservation and non-negativity checks.
- Group coherence is unchanged and still checked on both sides: currency (`Ordinal`) and the payable amount must
  equal the group's, and the new owner/schedule checks are added on the same boundary. Payment/Settlement
  semantics were not touched.
- The historical-replacement fixture is now **coherent**: a fresh planner quote of `157,000` with a group amount
  of `157,000` and accepted `150,000 + 7,000` (no `150,000 + 7,000` against a `125,000` payable any more). The
  stored order group keeps its old snapshot/owner/money/date/hours/tokens; only the explicitly replaced draft
  group carries the new coherent quote.

## E — mapping / migration honesty

`ConfigureAcceptedEvidence` maps the new scalar set and the nested owned owner (22 columns, prefixed `accepted_`);
the 231 migration (still **WRITTEN, shared NOT APPLIED**) was corrected in place, which this task explicitly
authorizes because the schema is unshared/unaccepted.

- The 19-column list became the corrected 22-column list; `ADD COLUMN` is now plain (**no `IF NOT EXISTS`**) and
  `CREATE INDEX` is unconditional, so a pre-existing contradictory same-named object fails the migration instead
  of being silently adopted. `accepted_responsibility` / `accepted_receiving_start_date` /
  `accepted_receiving_end_date` never existed in a shared DB.
- Two CHECK constraints per table make the storage genuinely all-or-none and shape-checked at the DB level
  (`ck_*_accepted_evidence_complete`, `ck_*_accepted_evidence_shape`), so a partial or contradictory row is
  refused by PostgreSQL itself — proven by raw `UPDATE`s, not by constructor-only unit tests.
- No earlier/shared migration was edited, no redundant migration was added, no unrelated entity was rebaselined.
  The EF model snapshot is a **pre-existing stale baseline** (it contains neither `CartShippingDraftGroup` nor
  `OrderShippingGroup`); it was deliberately **not** regenerated. Instead the runtime model was compared against
  the migrated database for these two tables: the 22 non-shadow evidence columns of each entity (plus the nested
  owner) are exactly the 22 `accepted_*` columns that exist, all nullable, with `bigint` for both tokens and both
  tick columns, `numeric(18,2)` for all four amounts, and both CHECK constraints present. The snapshot/future
  scaffolding is therefore still NOT certified, and that gap is recorded, not hidden.

## F — focused proof only

Task-owned disposable PostgreSQL `127.0.0.1:15433/order_tb231/tb231` (identity re-asserted by
`OrderEvidenceRelationalTestTarget` before any DDL; container removed afterwards and port 15433 verified closed).
Normal Order migration chain applied once on the fresh database.

- `ShippingGroupAcceptedEvidenceTests` **11/11** — two groups on one shared snapshot keeping distinct
  area/row/owner/money/date/fractional hours/tokens through materialization; all three owner forms; legacy plan
  with no evidence; 18 invalid/partial inputs refused; owner shape matrix; owner/seller mismatch; interval and
  date edges; money representability/conservation/zero/max/split; incoherent currency/payable/date/owner refused
  before any mutation; `ApplySchedule` refusing a stale-evidence schedule and restoring legacy behaviour once the
  evidence is explicitly cleared; per-group replacement isolation.
- `ShippingGroupAcceptedEvidenceStorageTests` **5/5** — provider round-trip of owner/token/hours/money with raw
  column readback; coherent draft replacement leaving the stored order group untouched; legacy `NULL` group;
  DB-level all-or-none + shape rejection of 8 corrupt `UPDATE`s with the intact row preserved and an explicit
  full clear still lawful; compact scalar storage shape (22 columns, zero `json`/`jsonb`, zero NOT NULL, four
  `bigint`, four `numeric(18,2)`, both CHECKs, one snapshot index, runtime model = actual columns).
- `OrderEvidenceRelationalTargetIsolationTests` **12/12** (offline target guard) and the directly affected
  existing facts `OrderShippingGroupMaterializerTests` + `CartShippingGroupPlannerTests` + schedule/identity
  classes re-run unchanged: **50/50** focused tests passed in one run (no whole-Order / Shipping / 119 / 188 /
  194 / Host / solution / guard sweep).
- Builds: `Tooba.Order.Domain`, `Tooba.Order.Infrastructure` and `Tooba.Order.Tests` compile with 0 errors
  (pre-existing unrelated warnings only).

## HONEST LIMITS

- No production writer composes this evidence yet: every real draft/order group still stores `NULL`, and
  `SaveSelectionAsync`/`CommitAsync` still refuse with `storefront.captured_preview.not_ready`. The
  "no mutable Shipping draft needed" history claim remains a **storage** fact; no historical reader endpoint
  exists.
- The EF model snapshot stays stale by decision; only the touched groups were proven equivalent at runtime.
  `migrations add` would still re-scaffold until the separate baseline problem is fixed.
- `ApplySchedule` has no production caller today (`internal`, only reachable from the planner/test); its guard is
  therefore proven against the internal method through `InternalsVisibleTo("Tooba.Order.Tests")` added to
  `Tooba.Order.Domain`, not through a live customer path.
- Conservation covers only the Manual captured slice; Free/funded/Carrier-collect payment models remain
  unavailable and were not invented.
- Retained, NOT fixed here: the 230-R1 empty-key destination failure/retry gap and in-flight new-key wait,
  cross-owner label map/decoder id-coercion limits, Offer API proof gaps, the 227 mobile empty-note residual and
  the unrecovered 225-R1 user LOCAL tab edit loss.
- Elapsed ≈16 min vs HARD35 (one in-module correction round: a test fixture that broke conservation and a
  same-timestamp draft group replacement). Within the box this time, but still not a process PASS claim.

## NEXT (Architect)

Unchanged and still mandatory before any captured checkout: a trusted server composer that revalidates the
selected captured revision + cart/version + Offer/type/quote/date and writes this corrected evidence inside the
existing atomic boundary, then an Order-storage-only historical read that refuses partial/corrupt rows.
Selection/commit parity was deliberately NOT implemented here.

STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. TB185 paused, default-OFF, PendingAllocation preserved.

---

## CORRECTION APPENDED BY 231-R2 (2026-10-27) — the storage-shape claim below was too strong

`TB-SHIPPING-COMPACT-EVIDENCE-NULLSAFE-CHECKS-231-R2` corrected the SHAPE CHECK in the SAME still-unshared,
still-unapplied migration and proved the database directly. Everything above about the value/owner/token/time/
money contract, the transfer path, the 22 columns and the materialization/readback facts stands as recorded.

What was wrong here: the optional delivery-day branch `(min IS NULL AND max IS NULL) OR (min >= 0 AND …)` returns
`UNKNOWN` for a half pair (`min NULL / max 3`, `min 2 / max NULL`) and PostgreSQL ACCEPTS `UNKNOWN` in a CHECK,
so "the DB repeats the domain's all-or-none day rule" was not true at SQL level. The same reviewed CHECK also
accepted a blank/space-padded Store tenant, empty required/optional-present UUIDs, and any three-character
currency (`123`, `A$!`). The eight raw `UPDATE` refusals in this task's proof simply never covered those shapes.

Now (231-R2): the half pair is `FALSE` via explicit `IS NOT NULL`, the tenant must be non-blank and already
trimmed, the four required UUIDs plus optional-present type and Seller id must be non-empty, and the currency
must match `^[A-Z]{3}$`. Direct RED then GREEN on a recreated task-owned `15433/order_tb231` proves both tables:
`4/10` failed on the reviewed CHECK, `10/10` pass after the correction, each refusal asserted as SQLSTATE `23514`
plus the exact constraint name with the row left intact.

Still true and unchanged: migration `20261027150000_AddShippingGroupAcceptedEvidence` is WRITTEN and
`shared-NOT-APPLIED`; there is no production writer, so real draft/order groups keep NULL evidence and
`SaveSelectionAsync`/`CommitAsync` still refuse `storefront.captured_preview.not_ready`; no historical reader
exists. SQL is not claimed to preserve out-of-scale money (`numeric(18,2)` coerces before the CHECK) — exact
amounts stay the application's authority. No shared migration, no Host/FE restart, no browser, no user review
window access.
