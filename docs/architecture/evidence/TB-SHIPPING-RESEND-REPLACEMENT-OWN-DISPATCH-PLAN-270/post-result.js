// Posts the TB-270 worker result, completes the task row and returns the heartbeat to Idle.
// Run ONLY after every doc/commit/push step of this task is finished (nothing follows it).
const fs = require("node:fs");
const path = require("node:path");
const contract = fs.readFileSync(path.join(__dirname, "result-contract.txt"), "utf8");
const TASK_ROW = "dc286be7-5eb6-48fd-ba5e-7a1769bf459b";
const BASE = "http://127.0.0.1:17321";

const summary = [
  "TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270 — RESULT",
  "",
  "STATUS: PARTIAL (design/ownership only; ZERO source change)",
  "DECISION: RESEND_REPLACEMENT_OWN_DISPATCH_PLAN_OWNERSHIP_AND_SCHEDULING_SEAM_DECIDED_WITH_SOURCE_VERIFIED_MISSING_INPUT_BLOCKER_AND_ZERO_CODE_CHANGE",
  "NOT CLAIMED: Architect ACCEPT, user visual/functional acceptance, all-shipping completion, any durable write path, any provider/financial/customer-consent outcome.",
  "",
  "ROOT (source-confirmed at 8e9f42cf7): a resend or healthy-stock replacement is a NEW physical attempt. Fulfillment's BuildParcelRow deliberately gives every shipment with ResendOfShipmentId != null plans = [] (lines 203-208), so the timing authority classifies it Unspecified with no handoff/customer date. That is correct — the alternative, PlansOf(sellerOrderId, shippingGroupId), would lend the shipped parcel's EXPIRED promise to the new attempt. The real gap is that no lawful path exists to give the attempt its own plan.",
  "",
  "THE EXACT MISSING INPUT (the reason this run stopped at the decision):",
  " - The Order-owned accepted evidence stores the plan's OUTPUTS only: handoff date/open/close ticks/zone, transport working days, policy version, schedule snapshot/revision, preparation start, transfer departure, handoff deadline, preparation/handoff minutes (ShippingGroupAcceptedEvidence.cs:552-610).",
  " - It does NOT store the carrier/customer/processing WEEK SHAPES that produced them, so a new attempt cannot be rescheduled from Order rows.",
  " - The only Contracts read that returns those weeks is IShippingCapturedRevisionQuoteLookup.QuoteAsync; its request requires DestinationIdentifiers + optional measurements (ShippingCapturedRevisionQuoteContracts.cs:283-291) and its Selection (hence Settings/CarrierWeek/ReceivingWeek/ProcessingWeek) is null unless a rate row matched (ShippingCapturedRevisionQuoteLookup.cs:680-745).",
  " - Order no longer owns those inputs: OrderShippingGroup has no destination columns (12-79); the destination ids live on the seller's CURRENT CartShippingDraft (87-96), which must not be mixed with a historical acceptance; the group's weight/item count were cart facts at submission and were never persisted. Passing nulls makes weight/count rate modes answer MeasurementRequired with NO weeks.",
  "",
  "DESIGN DELIVERED (evidence folder):",
  " 1 ownership-and-plan.md — verified call path (endpoint -> orchestrator -> owning directory -> projection with file/line); per-owner facts that already exist; the missing inputs; the chosen minimal seam IShippingAttemptSchedulingLookup (one additive Shipping Contracts read over the already captured, destination-independent ShippingAuthoringRevisionPayload: MethodSettings + customer week + carrier week + processing week, reusing the existing GetCapturedRevisionAsync/ReadPayload path, no rate/coverage/destination, no second engine); the Order-owned \"order\".shipment_dispatch_plans shape (PK shipment_id, unique occurrence_key, Pending->Committed, full binding + readiness provenance + plan triple + customer commitment + snapshot/revision/selection/format + prep/handoff minutes); and the write ordering that makes a parcel-without-plan impossible (Order generates the parcel id, plan row first, owning create with that exact id, then commit) with an interruption/recovery table.",
  " 2 timing-and-readiness-proof.md — resend readiness = the source parcel's physical origin receipt (ReturnedAt) and never earlier than the new parcel's creation; replacement readiness = its purpose-bound non-expiring hold / linked parcel creation; never the original order instant, the original dispatch date or a browser clock; a damaged receipt is never resend capacity; ONE preparation allowance and ONE transfer allowance counted once by the existing authority; PreparationStartUtc is a deadline, never \"packing finished\"; the original customer date may be proposed only if still feasible for the new readiness, otherwise a deliberate newly feasible selection.",
  " 3 replay-and-write-boundaries.md — identity/uniqueness guards, exact submit order, recovery at every interruption boundary, read boundaries (own plan only for a resend/replacement, first dispatch keeps the group plan, no sibling lending, history never actionable, no per-read recomputation) and the focused cases the implementation owes.",
  " 4 The 269 Architect checkpoint was persisted at docs/architecture/evidence/TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269/ARCHITECT-REVIEW.md as the envelope instructed.",
  "",
  "NOT IMPLEMENTED (complete, no substitute PASS): (1) no Shipping scheduling-inputs Contracts seam; (2) no attempt-plan table/migration/entity/EF config; (3) no plan_shipment_dispatch command/endpoint, no review->submit, no replay/conflict, no concurrency proof; (4) no feasible-date preview for a specific attempt; (5) no queue or detail change — a resend/replacement still reads Unspecified (unchanged, truthful) and no row gained a plan or an action; (6) no frontend review step; (7) no focused test (nothing to test); (8) no live eligible unplanned parcel was created or inspected by mutation, so none is promised.",
  "",
  "WHY NOT A HALF-BUILT WRITE PATH (explicit): every available shortcut is forbidden by the task itself — reusing the original accepted handoff/date is exactly the borrowing the task names; nowUTC is not evidence the goods exist; re-quoting with the current draft's destination mixes a historical acceptance with current settings; null measurements silently yield no weeks or a fabricated plan; persisting a plan without the weeks means inventing a weekly shape; writing a demo order/plan for a live example is outside the environment boundary. Starting the seam+table+command+preview+dialog and stopping mid-way would leave a tree that cannot compile or a parcel that could exist without its plan — worse than an explicit, reviewable blocker.",
  "",
  "TESTS / BUILDS: none run, deliberately. Nothing in src/** changed, so no build, suite, publish or restart was warranted and no unaffected suite was re-run. Served FE remains TB-269's BUILD_ID UmAnnlHYHCf-Sa5-dTMkm.",
  "",
  "ENVIRONMENT (read-only): Host 5088 /health 200, /health/ready 200; FE 3000 /fa/admin/fulfillments 200. No migration, DDL, business SQL, fixture, settings edit, stock write, cleanup, provider/waybill call or money movement; user orders, the Returned-source/Delivered-resend trial history and the shipping draft were not touched.",
  "",
  "GIT: expected base 8e9f42cf72af54101c102a60d6fdfce7be01e473 == actual work base == local origin/main at start (ancestor checks exit 0); one transient TLS EOF on fetch with no incoming delta to integrate. Code-SHA NONE (no source change). Evidence commit 97c5c02b677a838f912776ca63e37ff610bbf839 (12 files, documentation only; git diff --stat HEAD empty for tracked code). Contract pin f81695019a459ce3156ca63f1f5735f1893c0b43; the poster commit is 4f7cf39828c2c803f21a4beab0b1fca41bee5aeb and HEAD == origin/main was re-verified after EACH push (97c5c02b6 -> f81695019 -> 4f7cf3982), including the final poster commit itself, so the tree transmitted with this result is in parity and carries no source change. No force/amend/reset/rebase/stash; the eight pre-existing untracked scripts/tb266r1-*.mjs were left untouched and not committed.",
  "",
  "RECOMMENDATION: 270-R1 = the additive IShippingAttemptSchedulingLookup Contracts seam + one focused Shipping test (no Order change); 270-R2 = the Order-owned attempt plan (table/migration/Contracts/reader) + the reviewed plan_shipment_dispatch command with replay/conflict + feasible-date preview + queue/detail own-plan wiring + the review step inside the EXISTING resend/replacement dialog, proven on the cases in replay-and-write-boundaries.md §4. 271 (job-stage filters) remains NOT ISSUED.",
  "",
  "REVIEW URL: http://localhost:3000/fa/admin/fulfillments and http://localhost:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations — CURRENT read-only state only; no eligible unplanned parcel is promised and no new action exists. A resend/replacement row still shows «موعد ارسال مشخص نشده» because it has no plan of its own; that unchanged, truthful behaviour was intentionally not faked.",
  "Evidence: docs/ai/tasks/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270/TASK.md and docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,ownership-and-plan.md,timing-and-readiness-proof.md,replay-and-write-boundaries.md,user-review.md}",
  "",
  "STOP_FOR_ARCHITECT",
  "automaticNextImplementationTask: NONE",
  "",
  "--- result-contract.txt ---",
  contract,
].join("\n");

const post = async (url, payload) => {
  const r = await fetch(BASE + url, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: payload === undefined ? undefined : JSON.stringify(payload),
  });
  const text = await r.text();
  console.log(url, r.status, text.slice(0, 400));
  return r.status;
};

(async () => {
  await post("/api/workers/heartbeat", {
    workerId: "tooba-worker-01",
    channelId: "tooba-main",
    agentType: "cursor",
    status: "Working",
  });
  const s = await post("/api/results", {
    channelId: "tooba-main",
    taskId: TASK_ROW,
    content: summary,
  });
  if (s >= 300) return;
  await post(`/api/tasks/${TASK_ROW}/complete`, undefined);
  await post("/api/workers/heartbeat", {
    workerId: "tooba-worker-01",
    channelId: "tooba-main",
    agentType: "cursor",
    status: "Idle",
  });
})();
