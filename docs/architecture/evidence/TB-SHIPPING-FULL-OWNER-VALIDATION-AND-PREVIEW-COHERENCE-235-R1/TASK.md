# TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1

Parent: TB-SHIPPING-FULL-OWNER-FLOW-AND-PREVIEW-CONTRACT-235
Repository: mrnikiemami-code/Tooba | branch main | worker tooba-worker-01 | channel tooba-main
Mode: REPAIR_TWO_LINE_VALIDATION_REGRESSIONS_AND_FINISH_OWNER_VERTICAL_PROOF
Reviewed source: 1662e6f92a3b0e20f44f4b24b0b39d8856b85d60.
Expected base: ISSUE COMMIT CONTAINING THIS TASK. Fetch safely, start from it; preserve unrelated work.
HARD35 / source20 / focused proof+handoff15. One correction round; stop at cutoff and report honest remainder.
No automatic next task. Do NOT run a whole suite to substitute for missing functional facts.

Read AGENTS/TOP recovery, 235/ARCHITECT-REVIEW.md, 235/TASK.md, repository backend feature skill and locked
Shipping decision §29. Retain 235 typed owner policy, full-owner labels, line-id fields, no history fallback and
accepted-group totals. No new owner engine/quote algorithm/framework/schema. Full user delivery scope (all owners,
coupons, existing UI and practical desktop/mobile trial) remains binding; this repair is not a final exclusion.

## A — Validate EACH assignment before deriving a group owner (two confirmed defects)
In CheckoutDirectory.CapturedAcceptance.ResolveGroupOwner:
- Require dictionary lookup AND assignment.OfferId == exact offerId of that cart line, and real seller match.
  A value for another Offer under the right dictionary key must refuse before reservation/order/conversion.
- EACH line must have a known explicit responsibility and real nonempty active assigned type matching evidence.
  Missing/unknown responsibility/type cannot be repaired by a later line.
- Do not use null responsibility as an uninitialized accumulator. Initialize explicitly after validation, compare
  every later line against that value (same responsibility + type). Null-first/valid-second and reverse order both
  refuse; lawful multiple lines sharing one OfferId still work. Preserve existing owning refusal codes.
- Full owner still comes from the shared policy; no user-id owner/default tenant/seller rewrite.

## B — One coherent payment preview and one presentation-cart load
ShippingService.ProjectPaymentDraftAsync ALREADY loads/authorizes CartPage; pass that exact server object into the
internal captured facade instead of a second RequireCartAsync. Preserve Directory's independent authoritative
cart/version check and one whole-cart goods quote. No public caller-supplied CartPage authority.
Captured receipt/groups + exact revision and payment snapshot money/date/method must describe ONE accepted root.
ComposePreviewSnapshot currently overrides ONLY amount and retains old command dates/method. Reuse existing
snapshot composition with an explicit typed captured handoff OR detect an outer/root revision change and conflict.
Do not guess an aggregate time window across independent groups, and do not silently replace an address.
Use a deterministic TEST seam on an existing port boundary to replace the Order selection between outer facade
read and fresh inner acceptance. Response must be coherent accepted money/date/groups/revision or typed conflict,
with zero writes/reservation. A changed Shipping POINTER is NOT this test.
Legacy unchanged; historical mapping remains Order-only; no metadata read after durable commit.

## C — Finish ONE actual owner vertical proof, not manually seeded accepted owners
Reuse existing harness / actual CheckoutDirectory and StorefrontShippingService with real OrderDbContext on the
task-owned disposable 127.0.0.1:15433/order_tb231/tb231. Identity asserted BEFORE DDL; normal migration chain;
existing postgres17-alpine image; remove container + verify port closed. No shared DB, no manual ALTER/adoption.
Controlled Contracts are allowed and explicitly labelled, but strict:
- Marketplace cart with at least two real seller business ids and three partitions: Marketplace-owned Platform,
  Seller A, Seller B. Same service/option; owner-specific distinct captured snapshots/drafts/translations and
  lawful own rows/money/dates/week. Geography strict; current real assignment/type/catalog shapes.
- Drive REAL read planner -> REAL 232 selection writer/store -> public payment preview -> public commit ->
  Order persisted owners/evidence. Do not SeedMarketplaceOwnerDraftAsync/manual accepted evidence as the positive.
  Preserve ordinary atomicity/CAS/idempotency; no new production testing hook.
- Pointer/quote fixtures key FULL requested owner+binding and refuse unknown owners; they must NOT return the
  same capture for every owner. Assert own labels/captures never cross, one219 per distinct full binding per
  resolution, one216 per selected group and one whole-cart goods quote per evaluation.
- Swap Seller A/B owner responses and assert typed refusal with zero reservation/order; preserve the lawful
  same-OfferId multi-line case. SingleStore exact tenant control remains passing.
- Add focused RED->GREEN cases for foreign Offer payload and null-first/valid-second assignment (and reversed),
  and the deterministic preview-root replacement above. No production authority weakened for fixture success.
- Existing merchant permission tests already green; no need to replay all33 or add another permission system.
If proof setup exceeds the time box, STOP with exact completed facts; do not replace it with policy-only tests
or claim all-owner vertical complete. Never broaden to live activation to make a test pass.

## Validation / delivery
Run new exact facts + directly affected existing public-edge/selection tests ONCE on the same isolated target;
compile affected Order projects. No fullOrder/Shipping/Host/solution/guards/frontend or 200/564/566/25/68/124/
36/28/31/50/119/188/194/285/310/312 confidence loop. Root will review source independently.
Append precise corrections to 235/RESULT.md; persist this TASK verbatim and RESULT under this task evidence dir.
Update ALL THREE recovery TOPs (AGENTS.md; START-HERE-IF-CHATGPT-IS-LOST.md; SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md).
State actual proof vs gaps, timing/corrections, gates/migrations/runtime. Exact-path commit/push/fetch parity;
no amend/force/reset/stash/rebase/history rewrite; leave unrelated CRLF drift. Do not run old poster/probe scripts.

## Preservation / remaining release requirements
NO frontend source/HMR/browser/user-tab access, Host/FE refresh/rebuild/restart, live business write, shared
migration/schema/baseline change. USER VISIBLE review window is USER UNSAVED WORK: never enumerate/read/automate/
reload/close. Local previewON/count7 unchanged; checked-in gatesOFF. 231/232 sharedNOTAPPLIED/EF snapshot gap
retained. TB185 paused/defaultOFF/PendingAllocation. Coupons still safe-refuse in THIS repair only; common
coupon read-selection-final parity, existing frontend connection, Order-only history and complete practical
desktop/mobile trials remain required delivery work. Do not mark final sellable/ready or Visual ACCEPT.
Retain known230empty-key/inflight/decoder, Offer API,227mobile residual,225-R1lost LOCAL edits (no recovery).
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE; no236 envelope.
