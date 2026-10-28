PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-RECEIVING-SLOT-BOUNDARIES-AND-COMMIT-241-R1
Parent-Task: TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — bounded slot and public commit repair
Mode: FIX_BOUNDED_SLOT_ARITHMETIC_AND_EXPLAIN_REPAIR_PUBLIC_COMMIT_500
Status: PARTIAL
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: db34c6f043dab269a8e85e22615fef82f932ee58
Base-SHA: db34c6f043dab269a8e85e22615fef82f932ee58
Code-SHA: 03b24cc35a4307f220278bc9e79c6df79da4f6f0
Evidence-SHA: PENDING_THIS_COMMIT
automaticNextImplementationTask: NONE

PRIMARY remains http://127.0.0.1:3000 → Host 127.0.0.1:5088 (Host PID 41232 after staged binary swap from
D:\temp\tb241r1-build\out; FE3000 PID 18832 unchanged `next start`). Edition=Marketplace Tenant=NULL
DB=tooba_marketplace_tb239. User Chrome D:\temp\tb241-chrome CDP9350 PID47688 NEVER enumerated/automated/reloaded.
Review cart 01a1846b-aede-7000-850f-de073a511739 untouched.

1 — BOUNDED TICK ARITHMETIC (PASS)
`StorefrontShippingCalculator` slot loop replaced: lengthTicks = minutes×TicksPerMinute; floor(window/length);
construct TimeOnly only from bounded ticks. Focused StorefrontShippingReceivingCalendarTests **24/24**:
09–17/60→8 slots, /120→4; 09–23/120 ends 21–23 no wrap; 09–12/100 one 09–10:40; 1440/481 on 09–17 refuse
`receiving.slot_duration.unfit`; fractional +123 ticks preserved; legacy NULL one full window; closed day none.

2 — AUTHORING PREFLIGHT (PASS)
`PreflightWeeklySchedule` refuses `shipping.schedule.slot_duration.unfit` when any working day cannot hold one
complete slot (09–17/1440 and 09–10/120 measured) with zero Revision/UpdatedAt mutation. NULL duration unchanged.
ShippingAuthoringRevisionSnapshotTests **12/12**. Admin hint text notes discarded tail / min one complete slot.

3 — PUBLIC COMMIT 500 (PASS owning diagnosis + repair)
Host log TraceId `66fcc1116ad89db2014e4a4f6f6fb073`: `ContractOperationException: storefront.captured_selection.intent_invalid`
at `CheckoutDirectory.CapturedAcceptance.cs:148` (`TryParseSelectionRevision` failed). Owning wire defect:
`CommitStorefrontShippingHandler` hardcoded `expectedSelectionRevision: null` and `StorefrontShippingCommitRequest`
had no field — so every public commit dropped the CAS token → IntentInvalid → unmapped → `platform.unexpected` 500.
Catalog `language_id` FromSql noise in same request is non-owning (exception that escaped was IntentInvalid).

Repair: request/command/endpoint forward TEXT revision; `StorefrontOrderResult` maps `storefront.captured_*` codes;
OrderErrorCatalogContributor registers captured selection/preview descriptors.

LIVE PRIMARY (task-owned new buyer 01a1848e-20c6…, cart 01a1848e-2129…, NOT user review cart):
- POST commit WITHOUT revision → **400** `storefront.captured_selection.intent_invalid` (stable FA title; not 500)
- POST commit WITH `expectedSelectionRevision:"1"` → **200** checkout `01a1848e-22ce-7000-8346-e2a0bf170206`
- start-payment → **200** payment `01a1848e-248c…` Pending manual; same-key replay → SAME checkoutId+paymentId
- DB order_shipping_groups ×2: day 2026-11-01 window 09:00-10:00 openTicks=324000000000 closeTicks=360000000000 fmt=2
  (exact chosen first slots).

UNMET IN THIS BOX: task-only publication duration change then Order immutability re-read after THIS commit (241 already
showed Order ticks immutable once; not re-run here). FE `next start` binary not rebuilt (commit helper change is
source-only; Host owns the 500 fix). Original deadline MISSED; focus ≈22min/35.

Focused commands: Order calendar filter 24/24; Shipping authoring snapshot 12/12; Host publish Release → tb241r1-build.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
