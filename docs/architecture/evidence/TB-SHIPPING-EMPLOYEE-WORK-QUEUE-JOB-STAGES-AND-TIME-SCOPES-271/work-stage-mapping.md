# TB-271 — job-stage mapping, precedence and timing semantics (written BEFORE the code)

Scope: ONE read projection over the **already existing** physical work units of the Admin shipping queue
(`AdminFulfillmentWorkUnitProjection`). No new lifecycle, no new status, no new dispatch permission, no shipping
calculation and no database state.

The classifier reads only facts the queue row already carries from the owning module:

| Fact | Real owner / meaning |
| --- | --- |
| `WorkUnitKind` (`Group`/`Parcel`/`Package`/`Fulfillment`) | the physical scope of the row (TB-260-R1) |
| `WorkUnitState` | that unit's own persisted state (`ShipmentStatus` / `ConsolidatedPackageStatus` / `FulfillmentStatus`) |
| `AvailableActionCodes` | exactly the operations the owning aggregate + capability rules already offer (`ProjectParcelActionCodes`, `ProjectPackageActionCodes`, `GroupActionCodes`) |
| `PendingDispatch` | the existing "physical work still open" fact (a `Created` parcel counts) |
| `PreparationStartUtc` / `TransferDepartureUtc` / `HandoffDeadlineUtc` | the accepted plan's own instants (policy v2+), already projected |
| `PlannedHandoffTimeZone` | the accepted carrier zone of the plan |

A stage is therefore a **read label over an existing server-owned fact**, never an authorization: every action stays
owned and guarded by the same commands the detail page calls.

## 1. Terminal versus active (decided first)

A row is **history (terminal)** when the projection itself says it owns nothing forward:

- `WorkUnitKind == Fulfillment` (the projection only emits this when no physical unit is open), or
- `WorkUnitState ∈ {Delivered, Cancelled, Failed}`, or
- `!PendingDispatch && AvailableActionCodes.Count == 0`.

This is what makes a **returned source that is already fully allocated/settled** terminal: its parcel is `Returned`,
`PendingDispatch` is false and it offers no `resend_shipment`/`resolve_damaged_goods` code. It is NOT active merely
because the state word is «Returned». Conversely a returned parcel that still owns received usable capacity (or a
damaged decision) keeps a non-empty code list and stays an active «برگشتی‌ها» row.

History never contributes to active totals and is only reachable through the quiet explicit history view.

## 2. The five job stages

| Token | Label | Decided by (actual source semantics) | What the employee must do |
| --- | --- | --- | --- |
| `preparation` | آماده‌سازی و بسته‌بندی | a `Group` row whose codes contain `mark_processing` or `mark_packed` (its own remaining lines) | process / pack THAT group's remaining quantity |
| `ready_to_send` | آمادهٔ ارسال | `create_shipment` (allocate a parcel from packed work) · `plan_shipment_dispatch` (confirm the linked parcel's OWN plan) · `assign_tracking`/`correct_tracking` on a `Created` parcel · a `Created` package without tracking (`assign_consolidated_package_tracking`) | create the parcel / confirm its own plan / record tracking — **not** dispatch |
| `handoff` | تحویل به حامل | `dispatch_shipment`, or `dispatch_consolidated_package` **without** `assign_consolidated_package_tracking` (i.e. the package's own prerequisite is already satisfied) | hand the physical unit to the carrier (the server operation is actually offered) |
| `delivery` | پیگیری تحویل | `WorkUnitState ∈ {Dispatched, InTransit}` / `deliver_shipment` / `deliver_consolidated_package` / `record_non_delivery` after dispatch | follow the delivery; record its outcome |
| `returns` | برگشتی‌ها | `WorkUnitState ∈ {NonDelivered, Returning, Returned}` or codes `start_return` / `record_return_receipt` / `resend_shipment` / `resolve_damaged_goods` | start/receive the return, then the source-owned resend / damaged / Returns decision |
| `history` | سابقهٔ ارسال | terminal rule in §1 | nothing (read-only archive) |

### Precedence (evaluated in this order, ONE stage per row)

1. **history** — terminal rule above (a settled returned source must never look like active return work).
2. **returns** — an actual return transit / receipt / remaining source decision.
3. **preparation** — processing or packing of the row's own remaining group quantity.
4. **handoff** — a carrier handoff operation that the server already offers.
5. **delivery** — a parcel/package already out of the warehouse (`Dispatched`/`InTransit`).
6. **ready_to_send** — parcel allocation, the linked parcel's own plan, or a required tracking code.
7. otherwise **history** (a state this projection does not treat as forward work).

`correct_tracking` is deliberately **below** `dispatch_shipment` in the precedence: once the parcel has both a tracking
code and the dispatch operation, the employee's job is the handoff, not a tracking correction. `plan_shipment_dispatch`
is deliberately **above** `correct_tracking` on an unplanned linked parcel, because the server withholds
`dispatch_shipment` until the plan exists — the progress shown is the actual outstanding server-offered job, while the
row's capability list is unchanged.

A **blocked active** row (no executable action at all, e.g. legacy missing capture, missing own plan, payment/source/
hold/package prerequisite) is NOT moved to another stage and NEVER hidden: it keeps the stage implied by its physical
state and carries the precise Persian blocker the server already sends (`NextStepBlockerFa`).

## 3. Relevant time per stage (one meaning per stage)

| Stage | Persisted fact used | Persian label | If the fact is absent |
| --- | --- | --- | --- |
| preparation | `PreparationStartUtc` | «آخرین مهلت شروع آماده‌سازی» | undated («موعد ارسال مشخص نشده») |
| ready_to_send | `TransferDepartureUtc` | «آخرین مهلت خروج به سمت حامل» | undated |
| handoff | `HandoffDeadlineUtc` | «آخرین مهلت تحویل به حامل» | undated |
| delivery | none (no stored SLA) | «پیگیری جاری؛ مهلت مشخصی ثبت نشده» | always undated |
| returns | none (no stored SLA) | «پیگیری جاری؛ مهلت مشخصی ثبت نشده» | always undated |
| history | none | «—» | always undated |

- `PreparationStartUtc` is the latest instant to **start** preparation. It is never displayed as a packing-finish
  time and no pick/pack duration is invented from it.
- The carrier handoff instant is the accepted **acceptance close** (half-open `[open, close)`), NOT a booked
  appointment and NOT the departure deadline; the departure instant keeps its own name in `ready_to_send`.
- The customer receiving date stays a separate commitment column and is never used as warehouse due time.
- No instant is recomputed here: the values come from the accepted plan exactly as the queue already projects them.
  A missing/invalid zone is never replaced by the browser zone (undated instead).

### Time scope (the only primary time control)

`today` (default) · `future` · `all`.

| Row job time vs injected server `now` (accepted zone) | Bucket |
| --- | --- |
| instant `<= now` (including exactly at the cutoff) | `past_due` — half-open: at the cutoff it is already late |
| instant `> now` and the local day equals today | `due_today` |
| local day after today | `future` |
| no usable instant / no stored SLA | `undated` |

- `today` = `past_due` + `due_today` + `undated` (overdue first, unknown/ongoing work visible with its own count).
- `future` = only `future` (a tomorrow handoff does not remove today's preparation: that row's relevant fact is its
  preparation start, which is today).
- `all` = everything.
- Default sort key `workPriority`: `past_due` (earliest instant first) → `due_today` (earliest first) → `undated` →
  `future` (earliest first), with `WorkUnitKey` as the stable tie-break. An explicit user sort still wins.

## 4. Count badges (explicit dimensions)

Let `S` be the classified set after search / column / advanced filters (and after any legacy token that is still
supported), **before** paging and before the selected stage/time.

| Badge | Definition |
| --- | --- |
| stage badge `x` | `count(S where jobStage == x AND timeMatches(row, selectedTime))`, history excluded from the five |
| «همه» stage badge | `count(S active where timeMatches(row, selectedTime))` |
| history badge | `count(S where jobStage == history)` — the archive is time-independent |
| time badge `t` | `count(S where stageMatches(row, selectedStage) AND timeMatches(row, t))` |
| page `totalCount` | `count(S where stageMatches AND timeMatches)` — the same rows the page is cut from |
| «موعد مشخص نشده» count | the `undated` subset of the current selection, always explicit |

Counts and the page come from ONE classification pass of ONE set. Nothing is tallied from a fetched page, and a
package row still owns its locked members so no count is inflated.

## 5. Legacy URL tokens (deliberate, documented mapping)

The old `?filter=` tokens are mapped once at entry to a new (stage, time) pair, then the legacy token is removed so
two authorities can never combine:

| legacy `filter` | stage | time |
| --- | --- | --- |
| `dispatch_preparation_due`, `needs_action` | `all` | `today` |
| `dispatch_overdue`, `dispatch_today`, `dispatch_due`, `dispatch_unspecified` | `all` | `today` |
| `dispatch_upcoming` | `all` | `future` |
| `in_transit` | `delivery` | `all` |
| `ready_to_process`, `ready_to_pack` | `preparation` | `all` |
| `ready_to_ship` | `ready_to_send` | `all` |
| `delivered`, `problem` | `history` | `all` |
| `all` / unknown / absent | `all` | `today` |

Server side, the new `jobStage`/`timeScope` fields are classified (never SQL-prefiltered) and, when present, the old
`queueFilter` timing token is ignored — no silent combination. `jobStage`/`timeScope` are rejected in the advanced
filter expression (they cannot be evaluated in SQL, so pretending otherwise would be a lie).
