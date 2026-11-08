PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4
Parent-Task: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — finish immutable workflow recovery and explicit parcel reuse
Mode: BIND_COMPLETE_OCCURRENCE_INTENT_AND_RESTORE_EXPLICIT_REUSE_ONLY_EXECUTION
Expected-Base-SHA: 8617161188232acfc4f70d6e363b906e85e14953
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Bounded review decision

R3 physical-origin fail-closed restoration, occurrence-vs-payload distinction, normal preflight, canonical deployment and updated recovery are accepted as implemented bounded improvements.
Do not redo those changes or repeat the whole earlier matrix. R3 is still PARTIAL for end-to-end recovery/reuse because the source below does not implement the complete immutable intent it claims.
Close only these remaining production-path defects. No visual redesign, unrelated architecture audit, provider feature or test loop.

Read applicable AGENTS/feature skills and current recovery. Persist exact task. Safe main, preserve unrelated edits.
Locked product rule unchanged: Marketplace-only cross-seller grouping, same proven physical origin and accepted service/date/hour. Store/Seller stay separate; no data promotion.

2. Source-grounded defects

A — Complete immutable intent is not recorded.
operational-group-occurrence-store.ts saves only a GUID. OperationalCard recomputes selections/reuseShipmentIds from the CURRENT remaining quantities on each render/remount.
Server MarketplaceOperationalGroupWorkflow stores only constituent ShippingDispatchKeys; it has no durable complete request containing all member selections and reuse IDs before the first allocation.
Consequences:

failure after member A allocation but before B can allow the same occurrence to change B's payload;
changing reuse membership is not covered by RecordedQuantitiesMatch;
after reload, consumed quantities/parcel projections alter the request sent under the old occurrence;
per-member conflicts checked during execution may arise after another member was newly written.

Implement ONE immutable occurrence envelope, durably claimed before any allocation:
authorized actor/owner scope + checkout + operational member set + exact canonical line quantities + explicit reuse IDs + accepted method/identity needed for replay.
Same occurrence with a different full envelope → typed intent_conflict BEFORE any write, including changing only an as-yet-unallocated member or reuse IDs.
Use existing durable operation infrastructure if appropriate. If additive storage is necessary, do it; a new column/table is preferable to claiming an incomplete record is sufficient.
Maintain occurrence identity separate from payload fingerprint: identical next occurrence remains allowed.

Record exact constituent allocations and final package/result as the operation progresses, or derive from its exact durable occurrence links without sweeping other parcels.
Read/reconcile unresolved operations without requiring remaining packed quantity or a still-enabled create action.
Successful replay must not depend on current remaining quantities. Do not allocate new work to answer a completed-operation read.
Actor/owner scope and server authorization still apply on read/retry; browser storage is a hint, not authority.

B — Explicit reuse-only execution is broken.
BuildPlan calls AcceptedGroupShipmentCreateBinder.Resolve even when a member has reuse IDs but no line selections.
That binder converts empty selections to null/default selection; it can refuse an exhausted member or bind all remaining lines.
Later execution's member.Lines.Count==0 branch checks only RecordedOccurrenceParcels and never adds member.ReuseShipmentIds.
Implement explicit plan variants (create, reuse, create+reuse, replay) or an equivalent clear model:

reuse-only must not call the create binder as if all lines were selected;
reuse-only must use exactly the reviewed reusable parcel IDs without creating any new parcel;
replay may read only the exact occurrence result;
same seller with multiple accepted groups must resolve reuse by shipment.ShippingGroupId, not sellerOrderId alone (current reuseOwner scan sees each group under the same seller snapshot and may call a valid parcel ambiguous);
requested already-packaged parcels are refused unless this is reconciliation of this exact recorded occurrence/result;
full plan including create/reuse conflicts validated before first write.

C — Preserve the reviewed client request through uncertainty.
Persist the complete prebound envelope before the first POST, scoped to actual viewer/owner + checkout/group. Do not store credentials/PII.
On network/5xx/cancellation/response loss, freeze that envelope and retain occurrence; refresh/remount offers explicit reconcile/retry of that SAME envelope, independent of creation readiness.
Do not reconstruct an unresolved POST from fresh remaining quantities, auto-checked parcels or edited inputs.
Known pre-write refusal may allow explicit correction according to server contract; cannot silently mutate an occurrence that already has side effects.
New intentional operation only after authoritative completion/resolution; clear/new occurrence then. Handle storage unavailable honestly using in-memory state plus authoritative server reconciliation, not blind remint.
Keep current synchronous single-flight, catch/finally, one inline localized error and compact UI.
An actor switch must not silently reuse another actor's local operation hint.

3. Small focused acceptance set

Use task-owned disposable PostgreSQL, real Order/history/facts + production workflow/Fulfillment; do not mutate real user orders.
Add only the missing cases:

Two valid explicit reusable member parcels, zero new selections, zero remaining packed → one package, ZERO new parcels.
Same seller has two accepted group members plus a second seller: reuse each parcel by its exact group; foreign group refused.
Inject failure AFTER A is durably created but BEFORE B. Same occurrence changes only B quantity OR reuse set → typed intent_conflict, no extra writes. Retry original envelope finishes with exact A parcel.
All-reuse occurrence replay and mixed create+reuse retry return the same recorded package; concurrent same occurrence has one allocation/result. Keep existing R3 occurrence-next-package proof, do not rerun repeatedly unless changes break it.
Actual jsdom mount: partial server progress changes projected remaining quantities, then response lost + remount → original envelope retained and reconcile available even if create readiness false; no implicit new occurrence or extra parcel. Test actor change/storage-unavailable behavior at the owning state seam.
Assert exact links/counts/money/history conservation. No source regex/static HTML substituted for event/recovery tests.
Run focused affected backend tests, mounted workflow tests, typecheck/build/publish. Broader R3 matrix once only if shared changes warrant it; no six consecutive repeats.
4. Canonical deployment and actual handoff

Deploy corrected code from D:\Users\User\source\repos\SarvNewVer on FE3000→Host5088.
Keep SingleStore/store-alpha/tooba_alpha; preserve DataProtection and local CapturedPreview Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7, checked-in OFF.
Necessary additive migration: private verified backup, existing scoped MigrationRunner, applied ID/readback in receipts. No reset/reseed/business SQL/manual DDL/accepted-history rewrite.
No live Tipax/account request; no real user order/cart/pack/create/dispatch/cancel/payment/merchant edits. No screenshots.
Authorized read-only UI:
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf
Store groups remain independent. Controlled Marketplace fixture proves positive behavior without changing canonical edition.
Preserve existing look; verify no relevant overlay/overflow regression, no new redesign matrix. Record final deployed code/build identity and health/read-only authorized DOM.

5. Recovery and delivery

Append limited Architect review correction to R3: normal occurrence/replay/origin/deploy improvements accepted; whole-envelope persistence, explicit reuse-only and partial-progress remount were not proven/complete.
Update:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Task/evidence directory TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4 contains readable exact TASK.md, RESULT.md, RECEIPTS.md.
Preserve provider-deferred status, TB185 paused and known retained issues. Keep origin vs responsibility distinct.
Commit/push normally, local==origin/main; no reset/stash/force/amend/rebase.
Return complete worker envelope with exact Base/Code/Evidence SHA, actual executed proof, migration/deployment ledger, canonical URL and elapsed time.
Do not broaden PASS to untested recovery. Missing this scope → exact PARTIAL/BLOCKED.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
