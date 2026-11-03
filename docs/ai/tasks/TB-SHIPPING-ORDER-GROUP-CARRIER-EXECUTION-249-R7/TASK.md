PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R7
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R6
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — make first parcel submission recoverable after lost response
Mode: BIND_OCCURRENCE_BEFORE_FIRST_CREATE_AND_PROVE_RESPONSE_LOSS_RECOVERY
Expected-Base-SHA: 76e609065ddc3ac2bd64291117ceaa1a2644bf9a
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

PURPOSE
Close the remaining real UI-to-server occurrence gap. Preserve R6 weight authority, distinct identical parcels, durable provider claim, owner isolation and mounted proofs. No broad redesign.

SOURCE-CONFIRMED GAP
build-carrier-shipment-create-body.ts omits packageDispatchId when absent. The actual component sends packageDispatchIdByGroup[groupId] ?? null on FIRST create and learns identity only from the returned response.
ShippingCarrierShipmentDirectory mints a new occurrence for a no-locator call, converges only on matching Pending/Unknown, and deliberately creates a new occurrence after Succeeded.
Thus the first outgoing request has no durable identity for recovery when the response is lost. R6's explicit-same-occurrence relational tests do not cover that actual first-request path. Do not claim the existing provider claim deduplicates two different newly minted occurrences.

REQUIRED REPAIR
Establish and retain the parcel occurrence identity BEFORE any create side effect. Choose the smallest consistent existing-contract solution (durable prepared server occurrence or a validated durable client request identity). Bind it to owner+checkout+group+payload. Every first create, retry, response-loss recovery and refresh must reference the same intended occurrence.
Client storage remains a locator/hint, never authorization. Scope retained state to authenticated owner/checkout/group. A failed list/read must not be treated as evidence no operation exists; block uncertain resend and offer explicit reconciliation.
Only an explicit new-parcel action, after confirmed prior state/remaining quantity, may establish a NEW occurrence. Do not silently mint because a response timed out, list failed or component remounted.
Keep legitimate separate identical parcels lawful. Keep terminal payload-conflict validation, no-resend Pending/Unknown and exact allocation linkage.

TARGETED PROOF

Mount the ACTUAL shared component. First create already contains stable occurrence identity. Simulate server accepting/succeeding then losing HTTP response; repeat action and remount/reload recovery must use same identity and resolve same operation. No second allocation/provider call.
Same occurrence barrier on disposable PG with real adapters remains one create/allocation. Cover concurrent first-request path chosen by this implementation; nullable/legacy requests must have explicit safe behavior, not an unproven idempotency promise.
List/capability refresh failing after uncertain create blocks blind resubmit/new occurrence. Existing Pending/Unknown stays reconciliation-only.
Explicit new parcel with identical payload gets a different identity, succeeds only for remaining lawful quantity; replay both identities unchanged.
Retain R6 weight and physical fields tests. Run only relevant component/relational checks, typecheck/build. Controlled provider only.

PRESERVATION / DELIVERY
Canonical project D:\Users\User\source\repos\SarvNewVer FE3000 -> Host5088, SingleStore/store-alpha/tooba_alpha. Same professional UI; no screenshots, detached forms, alternate final runtime or extra broad features. No Tipax credentials/live calls. Never fulfill/cancel/pay user orders.
Provision/clean only identified task-owned disposable PostgreSQL; do not test-write to user database. Any necessary additive migration uses private backup+normal runner+readback, no history rewrite.
Inspect actual base and preserve intervening main changes. No force/reset/stash/rebase. Deploy canonical only after focused proof; preserve DataProtection and user data.
Update AGENTS.md, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md, docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md. Record exact remaining external verification; no unqualified full acceptance.
Preserve TASK verbatim under docs/ai/tasks/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R7/TASK.md and evidence TASK/RESULT/RECEIPTS.
Reconcile evidence-SHA wording: R6 RECEIPTS contains an interim evidence SHA; distinguish receipt commit from final pushed envelope SHA append-only.
Return actual source/runtime/test facts. STOP_FOR_ARCHITECT, no automatic next task.
END_TOOBA_TASK
