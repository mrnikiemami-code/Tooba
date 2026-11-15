# TB-270 — RESULT — PARTIAL (design/ownership only, zero behaviour change)

Decision string:
`RESEND_REPLACEMENT_OWN_DISPATCH_PLAN_OWNERSHIP_AND_SCHEDULING_SEAM_DECIDED_WITH_SOURCE_VERIFIED_MISSING_INPUT_BLOCKER_AND_ZERO_CODE_CHANGE`

Status: **PARTIAL**. Not Architect ACCEPT, not user acceptance, not shipping completion, no system/provider/financial
claim. One worker, one task, no delegation, no historical agent resumed.

## 1. Root

A resend or healthy-stock replacement parcel is a **new physical attempt**. Today it has no plan of its own
(`AdminFulfillmentWorkUnitProjection.BuildParcelRow` gives every `ResendOfShipmentId != null` parcel `plans = []`), so it
is classified `Unspecified` and shown under «موعد ارسال مشخص نشده». That is truthful — the alternative would lend the
shipped parcel's expired promise. The actual gap is that **no lawful path exists to give the attempt its own plan**, and
the reason is a missing input, not a missing formatter:

* the accepted group evidence stores the plan's **outputs** (handoff window/zone, transport days, prep/handoff minutes,
  the three deadlines) but **not** the carrier/customer/processing **week shapes** that produced them
  (`ShippingGroupAcceptedEvidence.cs:552-610`);
* the only existing Contracts read that returns those weeks
  (`IShippingCapturedRevisionQuoteLookup.QuoteAsync`) requires a `Destination` plus measurement inputs and returns the
  weeks **only when the rate quote succeeds** (`Selection == null` otherwise —
  `ShippingCapturedRevisionQuoteLookup.cs:680-745`, request shape `ShippingCapturedRevisionQuoteContracts.cs:283-291`);
* Order no longer owns those inputs: `OrderShippingGroup` has no destination columns, and the destination ids that exist
  belong to the seller's **current** draft (`CartShippingDraft.cs:87-96`), which must not be mixed with a historical
  acceptance.

## 2. Delivered by this run

1. `ownership-and-plan.md` — the required A decision: verified call path, stored facts with file/line, the genuinely
   missing inputs, the chosen minimal seam (`IShippingAttemptSchedulingLookup`, an additive Shipping Contracts read over
   the already-captured payload, no rate/coverage/destination, no second engine), the Order-owned durable attempt-plan
   row shape, and the exact write ordering.
2. `timing-and-readiness-proof.md` — B: the two readiness sources that really exist (usable origin receipt for resend,
   the purpose-bound hold/parcel creation for replacement), what may never be used as readiness, and the verbatim
   meaning of preparation-start / departure / handoff (no double counting, one allowance each).
3. `replay-and-write-boundaries.md` — C/D design: identity/uniqueness guards, the submit order, the recovery route at
   every interruption boundary, and the read boundaries (own plan only, first dispatch unchanged, history never
   actionable).
4. The 269 Architect checkpoint, persisted where the envelope asked for it
   (`.../TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269/ARCHITECT-REVIEW.md`).
5. The received task file, persisted verbatim at `docs/ai/tasks/.../270/TASK.md` and in this folder.

**No source code, migration, Contracts type, command, endpoint, projection, queue reader, frontend surface or test was
changed.** `git diff` against the work base contains documentation only.

## 3. Why the implementation was not attempted (explicit, not a substitute PASS)

The durable write path cannot be built honestly without first adding the narrow Shipping scheduling-inputs Contracts
seam described in `ownership-and-plan.md` §4. Every available shortcut is explicitly forbidden by the task:

| Shortcut | Why it is forbidden |
| --- | --- |
| reuse the original accepted handoff/date | the parcel is a **new** attempt and its goods were just returned; that is the exact "borrowing" the task names |
| compute from browser/order "now" | `nowUTC` is not evidence that the goods exist or that preparation has started |
| re-quote with the current draft's destination | mixes a historical acceptance with current configuration |
| pass null measurements to the existing quote lookup | returns `MeasurementRequired` with **no weeks** for weight/count rate modes — it would silently produce "no plan" or a fabricated one |
| persist a plan without the weeks | would require inventing a weekly shape; forbidden |
| write a demonstration plan/order to obtain a live example | the environment boundary forbids new fixtures/demo mutations for proof |

Building the seam plus the Order-owned table/migration/Contracts/reader/command/preview/queue wiring/review dialog and its
focused proofs is a multi-stage implementation. Starting it and stopping mid-way would leave a tree that cannot compile
or a parcel that could exist without its plan, which is worse than an explicit, reviewable blocker. This run therefore
stopped at the decision boundary and reports it rather than landing a partial write path.

## 4. Verified environment state (read-only)

* Host `5088` `/health` **200**, `/health/ready` **200**.
* FE `3000` `/fa/admin/fulfillments` **200** (served build unchanged from TB-269, `BUILD_ID UmAnnlHYHCf-Sa5-dTMkm`).
* No DB write, no migration, no fixture, no settings edit, no provider call, no cleanup; the user's orders, the
  Returned-source/Delivered-resend trial history and the shipping draft were not touched.

## 5. Unproven / not implemented (complete list)

1. No Shipping scheduling-inputs Contracts seam (design only).
2. No durable `"order".shipment_dispatch_plans` row, migration, entity or EF configuration.
3. No `plan_shipment_dispatch` command/endpoint, no review→submit, no replay/conflict implementation, no concurrency proof.
4. No preview of feasible customer dates for a specific attempt (server-side calendar for a resend/replacement).
5. No queue or detail change: a resend/replacement parcel still reads `Unspecified` (unchanged, truthful) and no row
   acquires a new plan or a new action.
6. No frontend review step inside the existing resend/replacement dialog; no Persian rendering of a new-attempt plan.
7. No focused test of any of the above (nothing to test).
8. No live eligible unplanned parcel was created or inspected by mutation; whether one exists today is not claimed.

## 6. Recommendation for the next issue

Split the remaining work at the seam that was missing, so each increment is verifiable:
**270-R1** = the additive `IShippingAttemptSchedulingLookup` Contracts seam + its focused Shipping test (no Order change);
**270-R2** = the Order-owned attempt plan (table/migration/Contracts reader/bridge), the `plan_shipment_dispatch` command
with the reviewed selection, replay/conflict, the preview read, the queue/detail own-plan wiring, and the existing-dialog
review step — proven on the cases already listed in `replay-and-write-boundaries.md` §4.

`271` (job-stage filters) remains **NOT ISSUED**.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.
