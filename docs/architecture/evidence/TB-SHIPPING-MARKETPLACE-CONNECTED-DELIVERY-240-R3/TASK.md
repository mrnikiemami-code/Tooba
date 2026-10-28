PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — correct captured group persistence and finish current trial
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
Mode: MIGRATE_BOTH_GROUP_UNIQUENESS_GATES_AND_COMPLETE_EXISTING_FLOW
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0 — Same deadline, honest remaining time, finish from the actual reached state

Original240-minute delivery contract, NO new clock:53+70+62+31=216 reported minutes used,24 remain before any other focused work. This continuation plans20 including evidence, not a guarantee that all open acceptance fits. Architect explicitly records original estimate was optimistic; full scope remains open and at serious risk. At cumulative240 report precise remaining outcomes and overrun, do not silently extend or call partial final. No automatic241.
Use the already-created THREE priced owner captures/cart; no environment/seller/reference-data bootstrap work again. Checkpoint8: new migration applied on task target + same3-group selection200, checkpoint14: real mixed Order/manualPending. Reserve final6 minutes for task-owned existing UI/payment handoff and minimal exact evidence. If a checkpoint fails, report actual owning failure immediately and preserve usable state; do not spend another hour on a broad proof loop.

## 1 — Exact source, current data and task target

Review base77ed71dfb709df7806018875d833edd9540121db (source769f43ff5c3339ae78c74dacb4929168bf8801a6),240-R2 RESULT/RECEIPTS/ARCHITECT-REVIEW. Safe fast-forward isolated checkout to issue commit.
Reuse task Host5190 Development/Marketplace/TenantNULL/tb239-marketplace/DBtooba_marketplace_tb239, verify PID/binary before restart.
Existing mixed cart01a181e2-0d5e-7000-a94c-87e9b826f94d currentversion read normally, contains3 real offers:
Platform sellerA goods2016500 payable100000(95000+5000), SellerA goods1580500 payable65000(62000+3000),
SellerB goods1951100 payable49500(47000+2500). Realread200, allPRICED, no refused group.
SellerA capture draft01a181dd-6bcb-7000-81eb-576c1f587eef snapshot01a181de-0c80-7000-9c01-ca51a0da05f4;
SellerB draft01a181de-0d04-7000-a79f-1bf723510020 snapshot01a181de-0e59-7000-9127-f5142617340e;
Platform draft01a181ca-e730-7000-bfaf-a5d9e5778dee snapshot01a181ca-e8f8-7000-840c-1a09cbb105e5.
Same realpost/standard/type, two sellers, SellerA has TWO different fulfilled obligations. All actor rights/reference data/publications/stock are already real. Do not remove a group, change method identity or split this cart into different orders merely to evade the index.

## 2 — ARCHITECT DECISION: ONE forward migration, BOTH tables, preserve LEGACY uniqueness

No new owner/responsibility discriminator column. AcceptedEvidence already stores full owner; ShippingGroupId plus exact whole-line membership is existing group identity. Another copied owner column would be redundant.

Root independently confirmed BOTH old unique indexes:
- order.cart_shipping_groups: ux_cart_shipping_groups_cart_seller_method on(cart_id,seller_party_id,method_code)
- order.order_shipping_groups: ux_order_shipping_groups_checkout_seller_method on(checkout_id,seller_party_id,method_code)
Both runtime EF mappings currently have unconditional IsUnique(), so just fixing the first table fails again at final Order.

Explicitly authorize ONE NEW Order-owned forward migration after236 that replaces BOTH indexes with UNIQUE PARTIAL indexes on the SAME existing keys, with predicate accepted_snapshot_id IS NULL. Retain exact old index names if practical, make runtime EF HasFilter/HasDatabaseName match the real migrated shape. This preserves the legacy one-seller/method constraint while lawful groups with COMPLETE captured evidence can coexist. Existing group PK, exact one-line membership UNIQUE indexes, FKs and all-or-none evidence CHECKs remain untouched. No owner column, methodcode suffix, captured JSON copy, newtable, or generalized schema relaxation.
Use plain DROP of the known index then CREATE UNIQUE with explicit WHERE on BOTH tables in migration transaction; fail on contradictory target objects, no IF NOT EXISTS adoption. Down restores the original unconditional indexes and must fail atomically if captured duplicates make downgrade unsafe; never delete/merge groups to force rollback.
Do NOT edit applied20261016/231/232/236 migrations or regenerate stale EF baseline. The new migration must be recognized by normal runner. Snapshot is pre-existing stale: do not pretend future scaffolding is clean; narrowly updating existing represented index metadata is allowed only if that table already exists there, no wholesale regeneration.

NORMAL runner plan/apply on VERIFIED TASK DB tooba_marketplace_tb239 ONLY explicitly authorized. Inspect actual pending set: expected only this new Order migration; unrelated pending modules require reporting before applying. No reset/drop/manualDDL, store-alpha/userDB not touched. Keep readback of pg_indexes BOTH predicates, actual migration history and row counts.
IMPORTANT CORRECTION:240-R2 says231/232 sharedNOTAPPLIED, but237-R1 applied them and239 migrated task Marketplace; accepted evidence columns exist live. State actual per-target applied history, do not repeat the stale claim.

Focused proof sufficient: real3-group save/readback and final Order keeping THREE group ids/owner/row/money/date/ticks on one binding; same SellerA appears in2 groups. Exact duplicate-line membership still refuses. One narrow legacy collision control still refused by preserved constraint/planner. No fullsuite/disposableDB setup if it displaces live target/handoff.

## 3 — Tighten the just-added planner seam, preserve authority and legacy behaviour

Root reviewed CartShippingGroupPlanner.ResolveMethod:
- nonblank ObligationKey currently uses FirstOrDefault then returns BEFORE seller and declared service/option validation;
- no exact key match falls back to seller+method, recreating the wrong-price bug;
- duplicate-seller/method collapse was relaxed for ALL requests, including legacy no-key calls.

Fix in same planner: a supplied captured ObligationKey MUST have exactly ONE eligible row matching exact server key, seller, canonical method/service/option; zero/ambiguous/foreign/contradictory match refuses, NO fallback. Continue existing declared-identity checks before return. Do not trust browser provenance or a label.
Legacy absent-key requests retain their original seller+method duplicate rejection and existing matching behaviour. Only authoritative captured keyed commitments permit SAMEseller/SAMEmethod over DISJOINT whole lines. Keep duplicate/unknown/missing line refusal and exact evidence attachment. Add focused facts for zero/duplicate-key/foreignseller/foreignbinding and legacy duplicate control; do not loosen tests to match current regression.

## 4 — Payment repair is only partially reviewed; no external-provider exactly-once claim

Worker manualHTTP test returned identical payment+attempt for same-key concurrency, useful but not full provider side-effect proof.
Root SOURCE CONFIRMED gateway.InitiateAsync happens BEFORE the contested SaveChanges. Therefore both concurrent branches may call the provider before the loser catches23505. Do NOT certify paid/PSP/provider exactly-once from one persisted row or manualPending.
Also current unique-conflict detector uses ConstraintName.Contains("idempotency_key") and loser path does not call the normal _actorAccess.EnsureActorCanSeeAsync. Tighten to EXACT known payment schema/table/constraint identity and reapply existing actor-access check before returning winner. Winner replay must follow the same operation identity and attempt-selection rules as existing lawful replay, not a new first-row policy.
If externalprovider concurrency is not corrected within this manual-only delivery, keep that limit explicit and do not activate/certify it. No newprovider orchestrator, in-memory lock or swallowed provider effects. No real-money calls. Preserve existing narrower manual path and typed conflicts. The task does NOT authorize claiming complete payment certification from this.
checkoutReplayed false remains an honest response defect; fix through existing result/replay mapping if directly reachable, never fabricate true without identifying original stored checkout. CamelCase binder failure did NOT reproduce, so no serializer change.

## 5 — Run from selection to usable existing UI, no more setup detours

After migration, use same real3-group intent/current revision: selection200 -> paymentpreview200 -> StartPayment -> real3-group Order/manualPending. Own evidence must remain independent, reserve/convert once, no partialcart, no fakehold/mockports/interception. Normal actual APIs first, then existing UI.
Remaining original acceptance still binding: partial/invalid/clear coupon, authorized original OrderGET after changing ONLYtaskDEVpublication, foreign denial, UI doubleclick and failedrefreshretry, desktop1440/mobile390/360 PAYMENT native views. Do as much of this through reused scripts/data as fits, and mark every unmet item explicitly at deadline. Do not label a schema fix or manualAPIorder final delivery.
Use isolated task FE3102→5190 and fresh task-owned browser profile/targets. Original abandoned3102/9339 are closed; reuse verified freeports. Do not build another CDPframework. Show actual existing shipping choices/save/payment; never inject payload/text or fake login. Hand off usable authenticated URL/window if reached, then do not automate it again. Actual PNGwidth==innerWidth and no horizontal overflow; no mobile proof from desktop/shipping-onlyshots.

## 6 — Architecture, bounds and canonical evidence

Allowed changes: new Order migration and matching runtime index mappings; exact keyed planner/legacy correction; narrow existing Payment winner-authority/index matching; directly blocking existing frontend mapping. No parallel authority, price/calendar/discount engine, foreignDbContext, Host businessSQL, newownerfield or historical repricing.
Preserve original USER3000/5088, handoff3100/5188+D:\temp\tb238-handoff-chrome, usertabs/unsavedwork/watchedsource/HMR, userdraft/rates/pointer/assignments, review230capture and handoffcart. NEVER enumerate/read/automate/reload/close/editthose. Task5190+isolatedFE3102 only, verifiedPIDrestart not kill-by-name. No reset/reseed.
Checked-in gatesOFF/namedlocaltaskON, TB185paused/defaultOFF/PendingAllocation. Retain238unclassified22001,227mobileempty-note and unrecovered225-R1LOCALedits.
Run only new affected facts/build/typecheck needed for changed source; no fullHost/Order/Payment/Shipping/solution confidence loop.

Received task verbatim in docs/ai/tasks/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3/TASK.md + evidence TASK.md. RESULT/RECEIPTS/screens in own evidence folder. ALL3TOPs and deadline ledger accurate, histories preserved:216+actual minutes, no newclock or automatic241. Exact-path normal commit/push HEAD:main/fetch parity; no amend/force/reset/stash/rebase/CRLFchurn. FullSHAs, actual runtime binary/DB/migration parity; sanitized artifacts no credentials.
Result leads usableURL + stage, ledger for mixedowner/coupon/duplicate/history/mobile, actual statuses/ids/artifacts or precise blockers. Root reviews source/receipts, root did not run Windows tests. No inferred user Visual ACCEPT/PSP/paid/activation claim.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

## RESULT CONTRACT — exactly one canonical result, NO diagnostic probe body

Use actual FULLSHA or NONE, no commentary inside SHA fields. Append the task-end marker as the final transmitted line after this template.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3
Parent-Task: TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — correct captured group persistence and finish current trial
Mode: MIGRATE_BOTH_GROUP_UNIQUENESS_GATES_AND_COMPLETE_EXISTING_FLOW
Status: PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: actual full issue commit SHA
Base-SHA: actual full source start SHA
Code-SHA: actual full code SHA or NONE
Evidence-SHA: actual full evidence SHA or NONE
automaticNextImplementationTask: NONE

UsableURL+stage, required acceptance ledger, exact new migration target/history/index predicates,
sanitized receipts, original240-minute cumulative deadline accounting, remaining blockers and preservation.
END_TOOBA_WORKER_RESULT
```

END_TOOBA_TASK
