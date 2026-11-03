PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R6
Parent-Task: TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R5
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — close identical-parcel identity and physical-weight authority
Mode: REPAIR_REVIEWED_DISPATCH_AND_WEIGHT_DEFECTS_WITH_REAL_COMPONENT_PROOF
Expected-Base-SHA: 481ec58bb91ea85b787236119241c3fbb01e8e89
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER INTENT
Finish the existing shipping delivery on the user's canonical project, backend, frontend and database. Retain product weight in grams and length/width/height in centimetres throughout commands, queries, persistence and editor. Product dimensions MUST NOT be summed to invent package dimensions. Do not request Tipax credentials or run real carrier calls. No screenshots. Preserve the current professional UI.

ARCHITECT REVIEW
R5 is PARTIAL pending the following concrete source defects and missing executable UI proof. Preserve its working durable claim, external-attempt marker, allocation reuse and owner isolation.

A. DISTINCT IDENTICAL PARCELS VS REPLAY
ShippingCarrierDispatchIntent.Derive currently derives dispatch identity from owner+checkout+group+line quantities+physical dimensions. Two legitimate separate parcels containing one unit of the SAME order line, with identical weight/dimensions, derive the SAME operation key. After the first succeeds, Create returns that first operation instead of allocating/sending the second parcel.
Implement a durable identity for the parcel occurrence distinct from its payload fingerprint. Replays of the SAME occurrence must converge; a deliberately NEW second occurrence must remain distinct even when its payload is identical. Do not restore a random ID on every retry, infer a new occurrence on timeout, or weaken Pending/Unknown no-resend.
Use existing owning Shipping/Fulfillment seams; keep the execution snapshot immutable. Validate any locator against owner/checkout/group. Bind payload fingerprint to occurrence; conflicting reuse must refuse.
The existing UI must retain/reuse the occurrence identity across double-click, retry, refresh and recovery as appropriate, and create a new one only for an explicit new parcel. Do not add a detached generic carrier form.

B. PRESENT TYPED WEIGHT MUST NOT HEAL THROUGH LEGACY
CatalogPhysicalSpecification permits scale 3. ResolveEffectiveFacts passes TryToWholeGrams(typedWeight) into ResolveEffectiveWeightGrams; typed 500.5 becomes null, then legacy 500 is returned as if typed weight were absent. This violates the documented present-invalid/variant-override policy.
Make write validation and shipping consumption coherent. Either enforce whole grams at every typed write boundary with a clear localized error, or support fractional grams end-to-end deliberately. Never silently round/truncate or treat a present unusable typed value as absent. Existing fractional persisted values must not silently fall back to legacy.
Cover product and variant presence/inheritance, equal/conflicting legacy values and invalid typed values. Keep centimetre decimals and nullable unspecified fields; no fabricated physical values or data backfill.

C. REAL COMPONENT PROOF
R5 explicitly did not mount the actual carrier React form. Mount the shared existing component with controlled API fixtures, exercise package/sender completion -> readiness refresh -> create -> replay/unknown/typed refusal. Assert visible action state, exact request count and persisted occurrence identity behavior; source-token tests and decode/body construction alone are insufficient.
Exercise actual product physical editor fields through save/read roundtrip in an isolated fixture (g/cm labels, nullable clearing, validation). Preserve the existing layout and design tokens; no screenshots or unrelated redesign.

REQUIRED TARGETED PROOF

Disposable PostgreSQL: order line qty2; parcel A qty1 succeeds; explicitly new identical parcel B qty1 succeeds; distinct operation/dispatch IDs, exactly two allocations/provider calls, total qty2; replay A/B adds nothing; third parcel refused as exhausted.
Barrier concurrency for SAME occurrence: one provider call/allocation; payload-conflicting reuse typed refusal; cross-owner locator refused; Pending/Unknown never blind resend.
Weight regression: typed product/variant 500.5 with legacy500 never resolves silently to500; typed whole equal/conflict; variant null inheritance; invalid explicit variant never heals from parent/legacy.
Verify money and collect totals remain conserved across the two identical parcels. Do not add unrelated monetary redesign.
Actual React interaction proof above plus focused affected tests/typecheck/build. Avoid broad sweeps or mirroring implementation tests.

ENVIRONMENT AND DELIVERY
Validate actual checkout/base first. If main advanced, preserve changes and record actual base; no reset/stash/rebase/force.
Provision an identified task-owned disposable PostgreSQL target yourself for relational proof; never point destructive tests at tooba_alpha. Clean only owned test resources.
Canonical final deployment remains D:\Users\User\source\repos\SarvNewVer FE3000 -> Host5088, SingleStore/store-alpha/tooba_alpha. No final alternate app/DB/port. Any necessary additive migration: owning module, private backup, normal runner, explicit target and readback. No history rewrite or manual business SQL. Preserve DataProtection keys and all user data/cart/order state. Do not fulfill/cancel/pay a real user order.
Keep Manual/Free behavior and Tipax deferred. Controlled-provider software proof is not live Tipax acceptance.

RECOVERY AND RESULT
Update ALL three recovery authorities, including the architect recovery document (not only AGENTS/start-here):

AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Keep prior history append-only. Record exact remaining external-only verification and any unmet software proof honestly.
Preserve TASK verbatim under docs/ai/tasks/TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249-R6/TASK.md and evidence TASK/RESULT/RECEIPTS.
Report code/evidence SHA, actual base, tests executed, canonical runtime source, migration ledger, and separate software completion from provider verification. No screenshots, no credentials in evidence, no automatic next task.
END_TOOBA_TASK
