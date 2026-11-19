# CURRENT — TB-281-R3 REVIEWED PARTIAL; ONLY 281-R4 ISSUED — 2026-11-19

Reviewed code 46c639c4c6cdd5f1ae0e3af2afbeb03f0dcf3dca; actual final head 14b2395eea3585b7e12af71d135c84c77ce7f9c2 is its docs-only addendum.
Read docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R3/ARCHITECT-REVIEW.md.
Only issued task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R4. Exact TASK.md: docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R4/TASK.md.
Close A full version/occurrence transport (including broken delete/reorder), B actual Media save/ack failure proof, C canonical rollback-only PostgreSQL facts. No new product scope, migration, seed or live business mutation. Retain landed R3 checks/public attribution/scoped schema; old 57/57 is not missing behavior proof.
One worker tooba-worker-01/tooba-main; issued does not mean running. Await result and review; no task chain.
Seller Story UI MUST use purchased-template EXISTING VendorShell and /vendor-panel/stories, existing story.view/menu/auth/context. No parallel panel/menu/login. See SELLER-PANEL-INTEGRATION-REQUIREMENT.md.
Shared Orders-style UI delivery is PLANNED NOT ISSUED. Worker PASS != Architect ACCEPT != user acceptance. Returns acceptances/payout backlog preserved; no key operations.
automaticNextImplementationTask NONE.

# HISTORICAL — superseded by the CURRENT checkpoint above

Read docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R3/{RESULT.md,RECEIPTS.md,COMPLETION-LEDGER.md}. Worker PARTIAL: shared admin/seller content gates, submit version/occurrence, storyId hash, Catalog public seller id, Media recording InMemory, tenant-scoped occurrence PK, Host skip-bootstrap. OPEN: PG rollback G-matrix; full admin composite occurrence transport. UI PLANNED_NOT_ISSUED. VendorShell /vendor-panel/stories mandatory for future UI. automaticNextImplementationTask NONE; STOP_FOR_ARCHITECT.

# HISTORICAL — 281-R2 REVIEWED PARTIAL; 281-R3 WAS ISSUED

## یادآوری اجباری معمار — استوری داخل پنل Seller قالب (۱۹ نوامبر ۲۰۲۶)

صاحب محصول تأکید کرد استوری باید در پنل فروشندهٔ موجود قالب تکمیل شود، نه پنل تازه. سورس بررسی شد: `vendor-panel/layout.tsx` -> `VendorShell`؛ منوی «استوری‌ها» با مجوز `story.view` و مسیر `/vendor-panel/stories` از قبل وجود دارد؛ صفحه همان feature مشترک را با قابلیت فروشنده می‌گیرد. مرجع اجباری تسک UI آینده: `docs/planning/story/SELLER-PANEL-INTEGRATION-REQUIREMENT.md`. پوسته، ناوبری و auth/context موجود حفظ شوند؛ منو/پنل/ورود موازی ساخته نشود. این فقط یادداشت است؛ تسک فعال R2 و انتظار نتیجه‌اش تغییر نکرده، UI هنوز NOT ISSUED و automaticNextImplementationTask NONE.

Read `docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1/ARCHITECT-REVIEW.md` and exact `docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2/TASK.md`. Source reviewed at ae947c863867437c13008029971a77eeb90edd41. Preserve fixed public Items loading, role mapper and version plumbing. Remaining original work is not a new product scope: safe Media upload/ownership + admin seam, all-route durable replay, stop/edit action cycle, seller query/admin shop-name/CTA/public identity, Media CQRS/tenant and actual G1–G7 controlled/rollback proof. R1 voluntarily stopped with these OPEN and no identified external blocker. Do not count seven facts/pins as full completion.

Active issued task TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2, same single tooba-worker-01/tooba-main; completion ledger then implementation/proof, not another planning-only slice. Connected shared UI remains PLANNED NOT ISSUED. One narrowly authorized process-only no-Development-bootstrap option may be used/added per envelope to refresh Host safely without Pricing or other seed writes. Existing migrations immutable; any necessary additive repair follows exact backup/runner rules. No live business mutation/fixture/reset/key operation. Record exact task-hash/claim clarification; preserve historical reports. Returns acceptances and payout backlog unchanged. automaticNextImplementationTask NONE; STOP_FOR_ARCHITECT.

# HISTORICAL — TB-STORY-281-R1 partial checkpoint

Worker PARTIAL on `TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1`. Read evidence under `docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1/`. Parent 281 PARTIAL preserved. UI delivery two NOT ISSUED. Expected-Base `860c6a2c72842c8971d8b9f9885e26cdb0f3f657`. ZERO live Story/Media business write. automaticNextImplementationTask NONE; STOP_FOR_ARCHITECT.

# HISTORICAL — TB-STORY-281 FIRST CONSOLIDATED DELIVERY ISSUED

Active issued task was `TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281`. Parent worker PARTIAL is historical; R1 continued unfinished A–G/B1–B7.

# HISTORICAL — Story source audit/planning checkpoint

Owner requested reading the actual Story code and FIRST delivering a report/plan. Read `docs/planning/story/SOURCE-AUDIT-2026-11-19.md` and `docs/planning/story/STORY-MARKETPLACE-COMPLETION-PLAN.md`. Source reviewed at `e242d4dd9cc69c9094187caf838bb60d0cd87c70`; no local tests/runtime or business writes performed. One existing shared seller/admin UI is retained as the base. Target: Orders-style grid, seller-owned drafts/media/review, admin moderation plus creating ON BEHALF OF a validated shop, real scheduled publication and honest storefront. Audit records S01–S13, including missing admin-for-shop ownership, incomplete editor/media scope, empty publication, Scheduled visibility gap and non-persistent public controls. Plan proposes TWO cohesive delivery packages (can be one implementation task if bounded), NOT ten microtasks. No implementation/task/Bridge dispatch is authorized by this planning checkpoint.

activeTask NONE; automaticNextImplementationTask NONE; next step OWNER_REVIEW_OF_REPORT_AND_PLAN. TOOBA-UX-001 applies: shopkeepers must understand the product without technical explanations. Preserve 280-R1 bounded Architect ACCEPT (user test still pending), 279-R3 acceptance, all prior reports and future refund-method/batch-payment backlog. No source/schema/data/seed/reset/runtime/config/key change. Do not mistake audit findings or the proposed product flow for implemented capability, new permissions or Marketplace acceptance.

# HISTORICAL — TB-280-R1 accepted UX and owner-test checkpoint

## نیاز آیندهٔ ثبت‌شده — روش بازگشت وجه و واریز گروهی (۱۹ نوامبر ۲۰۲۶)

به درخواست صاحب محصول، `docs/planning/returns/REFUND-PAYOUT-METHODS-AND-BATCH-PAYMENTS-BACKLOG.md` ثبت شد: نقدی، کارت‌به‌کارت، شبا با پایا/پل/ساتنا، برگشت به پرداخت اولیه و کیف پول فعال؛ پرداخت تکی/گروهی با نتیجهٔ مستقل و جلوگیری از پرداخت تکراری. ساخت فایل یا دسته به معنی پرداخت نیست. برنامهٔ آتی است: NOT ISSUED؛ هیچ اجرا یا تغییر قرارداد مالی فعلی مجاز نشده. مرحلهٔ جاری همچنان تست کاربر پس از پذیرش محدود 280-R1 است.

Read `docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1/ARCHITECT-REVIEW.md`. Independent source/test-assertion review at bb03fbd54416e8c4a436ae40a176b5d715649b11 accepts the issued UX1/UX2/UX3 closure: approved/bound money confirmation, per-condition clean review, per-product unit summary. No further repair task for these findings. The next step is the owner's visual/functional test, NOT another implementation task. User acceptance NOT CLAIMED; no financial/provider/full Marketplace acceptance.

Review URL: http://127.0.0.1:3000/fa/admin/returns/01a1eefb-ea96-7000-838d-9a0b37661c8b . Live positive-money/mixed flows remain mounted-fixture proof, current live case received0. Architect did not run local tests or browser. Worker served FE BUILD_ID xUhUnq_b5Okl6j0RYf2Jl; Host/backend unchanged. Historical 280 PARTIAL and all earlier reviews preserved; 279-R3 ACCEPT unchanged. TOOBA-UX-001 remains mandatory. No live mutation, demo reset, schema or key operation performed/authorized by this review. activeTask NONE; automaticNextImplementationTask NONE; awaiting USER review.

# HISTORICAL — TB-280-R1 worker PASS checkpoint

Read `docs/ai/tasks/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1/TASK.md` and
`docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280-R1/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,three-ux-fixes-proof.md,user-review.md,issuance-receipt.json}`.
Status **PASS within the issued UX1/UX2/UX3 frontend closure**, decision
`ONE_AUTHORITATIVE_REVIEWED_MONEY_DISPLAY_CONDITION_BOUND_RECEIVED_GOODS_REVIEW_AND_PER_PRODUCT_UNIT_SUMMARY_ON_THE_EXISTING_RETURNS_WORKSPACE_WITH_EXACT_COMMAND_SAFETY_PRESERVED_ZERO_BACKEND_CHANGE_AND_ZERO_LIVE_BUSINESS_WRITE`,
NOT Architect ACCEPT, NOT user functional/visual acceptance. Expected-Base-SHA `9075e2fa4325a2de60cbd17ebfe908d9d9ee74ed`
(verified ancestor); claim HEAD `4ce50d00690f3abdb058289f4a82d63275d136fb` was docs-only issuance. Parent 280 remains PARTIAL pending
review of this repair. 279-R3 ACCEPT preserved. Backend/schema/live write ZERO. FE proof: M1–M7 7/7, retained mounts
34/34, unit 49/49, tsc 0, next build 0 (`BUILD_ID xUhUnq_b5Okl6j0RYf2Jl`). Host 5088 preserved; Next 3000 refreshed.
Live case still receiving-only (read-only). User-Acceptance NOT_CLAIMED. automaticNextImplementationTask NONE;
STOP_FOR_ARCHITECT_AND_USER_REVIEW.

# HISTORICAL — TB-280-R1 ISSUED (docs-only envelope)

Active task was issued for one consolidated UX1/UX2/UX3 frontend closure. Expected base
`9075e2fa4325a2de60cbd17ebfe908d9d9ee74ed`. See CURRENT worker PASS above for the executed result.

# HISTORICAL — TB-280 Architect PARTIAL checkpoint

Read `docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280/ARCHITECT-REVIEW.md` FIRST. Worker head 83f56842f139c8512974aa872b67b0d3b6e97d9e independently source-reviewed. Zero-received gating, next-step hierarchy and main copy improved; final product/user acceptance NOT granted. UX1: refund confirmation displays requested rather than approved/bound money. UX2: full-receipt implicit baseline prefills aggregate accepted into usable, causing immediate errors for damaged/mixed goods; zero-condition fields remain. UX3: headline sums pcs and grams. Keep these as ONE bounded frontend closure, not a backend redesign or long task chain. No next implementation issued; automaticNextImplementationTask NONE.

279-R3 fixed-scope Architect ACCEPT remains preserved. Follow `docs/product/OPERATOR-UX-ACCEPTANCE.md` for every UI task. Preserve exact intent/version safety and all owning write guards. No live POST/write, fixture/reset, migration or key operation is authorized by this review. Owner visual/functional acceptance remains pending. Historical worker PASS and prior reports remain unchanged.

# HISTORICAL — TB-280 worker checkpoint

Read `docs/ai/tasks/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280/TASK.md` and
`docs/architecture/evidence/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,ux-before-after.md,user-review.md,issuance-receipt.json}`.
Status **PASS within the issued A–E operator-UX scope**, decision
`PLAIN_BUSINESS_LANGUAGE_ONE_CLEAR_NEXT_ACTION_CONTEXTUAL_REVIEW_OF_REAL_RECEIVED_GOODS_AND_PROGRESSIVE_HISTORY_ON_THE_EXISTING_RETURNS_WORKSPACE_WITH_ACCEPTED_OWNER_GUARDS_PRESERVED`,
NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT sellable/Marketplace readiness. Expected-Base-SHA
`9aaf5bcd486ed63271ef431be20b7e24e8bb1951` (verified ancestor); claim HEAD `2fb3791f0c1bd124ee9877f163918a25fdfa585a` was
the docs-only issuance, so no source drift arrived. 279-R3 backend closure remains Architect ACCEPT within its own fixed
scope; this UX increment is a separate product issue, not a reopening.

- `ReturnWorkPolicy.Describe` publishes `publish_resolution` only for a coherent receipt with `Received > 0m` (was the
  requested-line count), so nothing-arrived cases offer «ثبت دریافت کالا» and never a zero-ceiling review; a complete
  reviewed plan is still never re-published and every write rule is untouched.
- ONE ordered action plan: the first server-published code is a full-width primary button with a one-sentence
  explanation, the rest are quiet buttons under «اقدام‌های دیگر»; `data-testid="admin-return-action-*"` identities kept,
  no capability invented.
- The next-step card sits directly under the status card; receipts/refund attempts/decisions moved behind ONE collapsed
  `<details data-testid="admin-return-history">` titled «سابقه»; outstanding rejected custody stays visible.
- Review dialog: per-line reason is labelled الزامی with a 3-character minimum and refused locally before any bytes leave;
  an untouched dialog shows no errors because an empty portion means zero accepted.
- «برنامهٔ تفکیک»، «نسخهٔ تصمیم»، «نسخهٔ بررسی‌شده»، «فقط افزودنی» and the lock/storage wording are gone from the working
  area and from `return.resolution.*`/`return.correction.*` copy; the technical versions/occurrence keys still travel on
  the wire unchanged.
- Proof: `ReturnsPlainLanguageWorkflow280Tests` 4/4, `ReturnsOperatorWorkflow275Tests` + `ReturnsDecisionCorrection279DirectoryTests`
  36/36, mounted workspace 276+277+279+280 27/27, FE unit 49/49, `tsc` 0, `next build` 0 (`BUILD_ID xUU4TGZ4j0_hnS1DumpHH`),
  `dotnet build Tooba.slnx -c Release` 0 errors, Host publish 0 errors.
- Served: Host `5088` PID `38424`, dll SHA256 `147D033E79F774EE2F24925C501CF0DCE823571529F3ACE0004EBA37C096605B`
  (`/health` 200, `/health/ready` 200 SingleStore + messaging Healthy); Next `3000` PID `36664`, chunk
  `7944-e29aaa760b08cd39.js` 200 carries the new copy and none of the removed jargon.
- Live read-only: `01a1eefb-ea96-7000-838d-9a0b37661c8b` still `Requested`, original `updated_at`, received 0, attempts 0,
  receipts 0, decision version 0, `canPublishResolution=false`, `actionCodes=["record_receipt"]`; DOM 1440/390 overflow 0.
- Zero migration/DDL/seed/backfill/reset/cleanup, zero live business write, zero POST, zero money/stock/provider effect.

Known limits: mounted proofs are jsdom (behaviour, not geometry); the live check is read-only DOM/text (no screenshots);
the stored case has no received goods so the positive review dialog rests on the mounted real component and a fixture;
«اختیاری» still appears in the shared admin chunk for unrelated surfaces; the 279-R3 controlled-port limits and the
contained key exposure (`CONTAINED_PENDING_REVIEWED_RETIREMENT`) are unchanged and out of scope.
STOP_FOR_ARCHITECT_AND_USER_REVIEW; automaticNextImplementationTask: NONE.

# SUPERSEDED — MERCHANT PRODUCT UX REQUIRED; TB-280 ISSUED — 2026-11-18

The product owner requires a sellable interface for ordinary shopkeepers with limited technical literacy. Passing technical tests does not establish product usability. Read the permanent cross-feature rule `docs/product/OPERATOR-UX-ACCEPTANCE.md` (TOOBA-UX-001) for EVERY subsequent UI task.

Active issued task: `TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280`; read `docs/ai/tasks/TB-RETURNS-SHOPKEEPER-PLAIN-LANGUAGE-WORKFLOW-AND-REVIEW-280/TASK.md` and its issuance receipt. Await the single Cursor worker's Bridge claim; do not start another task. Fix the existing Returns presentation in ONE focused increment: everyday language, one clear next action, review only actual received goods, contextual fields and progressive history. Preserve exact retained intents and all accepted owning write guards. Only the specifically bounded read-only work-capability correction is allowed beside the frontend work. T1–T7 are the focused acceptance scenarios. No live business POST/write, demo reset, migration, provider/money/stock effect or screenshot. User visual/functional acceptance remains pending.

279-R3 remains Architect ACCEPT within its fixed backend scope; this is a separate product UX issue, not a reopening of that closure. Base `9aaf5bcd486ed63271ef431be20b7e24e8bb1951`. Key exposure remains CONTAINED_PENDING_REVIEWED_RETIREMENT, a separate pending review; no key operation is authorized. automaticNextImplementationTask NONE. Preserve all historical evidence and user work.

# HISTORICAL — 279-R3 fixed-scope Architect acceptance checkpoint

Read `docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3/ARCHITECT-REVIEW.md` FIRST. Source/evidence reviewed at `44e2cc6b3ea168b0e93de9103b861149d0f21fe7` (implementation `32b48c5b53ea263cd0aae00a5da59f34b4749671`). ACCEPT within issued B1/B2 + C1/C2 + D: required new-decision version after replay; legacy/incoherent held occupancy; actual Inventory quantity2 not3 with durable replay; failed-child shared-budget refusal/lawful control; safe historical posting untracking. This increment is CLOSED; no further repair task for those findings.
Last Architect-accepted Returns closure: `TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3`. Retain repaired R2 workflows and all historical reviews; original PARTIAL verdicts are not rewritten. Worker focused receipts21/21 +4/4 and guards34/34 were assessed against actual source/test assertions; Architect did not execute the local Windows runtime.
activeTask NONE; automaticNextImplementationTask NONE; no next implementation issued. User/provider/live financial/full Marketplace acceptance not claimed. No active served walkthrough; history URLs remain read-only, no automatic demo. Key security state remains CONTAINED_PENDING_REVIEWED_RETIREMENT, separate from accepted Returns closure; no key operation authorized. Move on only to the user's next chosen development.

# SUPERSEDED — worker/issuance checkpoint (historical)

Read `docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3/{RESULT.md,RECEIPTS.md,closure-matrix.md,owner-proof.md,user-review.md,result-contract.txt}` FIRST. Claim HEAD was `e7db94398e663b604a536b82bd86670bb50f72db` (permitted docs-only issuance on expected base `dd5c1d31fcd65bce7164c115599aacb5a4e33083`); reviewed code `f867dcb7163608915112c7450196c49bc8a3ba37`.
Status PASS within the issued B1/B2/C1/C2/D scope only: every NEW approve/reject after the stored-replay path now requires the reviewed decision revision (missing or stale ⇒ `return.correction.version_stale`, zero rows) and the reachable admin adapter never invents one; rejected/cancelled held goods with no allocation rows stay occupied through the one `ReturnHeldGoodsPolicy` (several plausible slices ⇒ typed `LegacyAmbiguous` block, incoherent receipt ⇒ `return.recovery.evidence_conflict` before any row); the real first refund restocks the accepted `2` (never `3`); the real failed-child retry refuses a 60+60 budget overrun on a 100 payment and mints exactly one deterministic child in the lawful control; the 277-R1 `RESULT.bridge.txt` is out of the index with an exact scoped ignore and its local copy preserved.
Proof actually run: Returns owner classes 21/21 + 4/4, Host AMSC returns guard 34/34, backend Release build 0 errors, Host publish 0 errors (declared deviation: one whole `Tooba.Returns.Tests` run 187/187). Host `5088` PID 39776 dll `04CD98A7…34C5`, `/health` + `/health/ready` 200 SingleStore/messaging Healthy, `store-alpha`; Next `3000` unchanged; both stored cases read GET-only and still `Completed` with their original `updated_at`.
ZERO migration/live business write/live POST/provider/stock/money effect. Security exposure remains `CONTAINED_PENDING_REVIEWED_RETIREMENT`; no rotation/revocation/history purge. Parents 279/279-R1/279-R2/278 stay PARTIAL; no Architect/user acceptance; no active case exists so no served operator walkthrough is claimed. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

# SUPERSEDED — 279-R3 final narrow closure envelope issued; AWAIT BRIDGE CLAIM — 2026-11-18

User explicitly requests completion in ONE task. Read `docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3/TASK.md` and `docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R3/issuance-receipt.json` FIRST. Expected base `dd5c1d31fcd65bce7164c115599aacb5a4e33083`; subsequent issuance delta is docs-only, verify ACTUAL claimed HEAD/no source drift. Canonical repo bytes win over transport formatting; never reconstruct/overwrite the envelope or receipt.
FIXED acceptance scope: B1 required decision version for NEW approve/reject after replay; B2 unallocated rejected/incoherent held goods stay occupied or typed blocked; C1 actual Directory Inventory argument 2 not3 + replay; C2 actual failed-child budget refusal 60+60>100 plus lawful control; D safe historical posting untracking. Keep R2 repaired wire/quantum/custody/customer intent and all prior useful work. No new cosmetic/architecture audit or micro-task chain.
ZERO migration/live business write/new demo/reset/provider/stock/money effect/key operation. Security exposure remains CONTAINED_PENDING_REVIEWED_RETIREMENT, independently reported; do not rotate/delete to obtain green status. Explicit-path normal commits/push only, no rebase/amend/force; focused tests only, no broad suite loops/screenshots/delegation. Parent history remains PARTIAL pending evidence. Next Architect review is bounded to issued gates/direct regressions/truthful evidence/protected state. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

# SUPERSEDED — previous source-review checkpoint (historical)

Reviewed final source `f867dcb7163608915112c7450196c49bc8a3ba37`. Read `docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2/ARCHITECT-REVIEW.md` FIRST. Retain repaired real operator wire, per-condition quantities, quantum, accepted+owed custody trim, legacy slice ambiguity, customer unmount/v1/exact-origin retention and current-tree key untracking.
Remaining blocking B1: NEW approve/reject can omit decision version; resolution-version gate is not a substitute. B2: rejected/cancelled legacy received goods WITHOUT allocation rows disappear from occupancy; incoherent receipt can still release as held=0. Add exact owner regressions and the still-missing controlled-port budget/Inventory proof. One narrow closure boundary, no broad redesign/micro-task chain.
Key exposure remains CONTAINED_PENDING_REVIEWED_RETIREMENT; no secret rotation/deletion authorized. Historical 277-R1 bridge posting still tracked despite report: preserve locally before normal untracking/ignore. No new task issued here; parents remain PARTIAL, no user/provider/financial acceptance. STOP_FOR_ARCHITECT; activeTask NONE; automaticNextImplementationTask NONE.

# SUPERSEDED — worker/checkpoint report (historical)

Read `docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2/TASK.md` and
`docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R2/`
(`RESULT.md`, `closure-matrix.md`, `connected-wire-proof.md`, `quantity-capacity-proof.md`, `retained-intent-proof.md`,
`key-containment.md`, `migration-and-backup-proof.md`, `RECEIPTS.md`, `result-contract.txt`).
Status PASS within the issued A–G scope; base `6e3d1873f7f9926c2906ae4e26547a6c9ef97eeb`; code `3b1ce75d9` on top of
`22a0ec148` (containment) and `ff69838cf` (issuance). 279/278/279-R1 remain PARTIAL; not Architect or user acceptance.
Key exposure is `CONTAINED_PENDING_REVIEWED_RETIREMENT` (Git containment only; no rotation). Proof: Returns 177/177,
Host 37/37, FE unit 29/29, mounted 34/34, tsc 0, next build 0, build/publish 0 errors. STOP_FOR_ARCHITECT;
automaticNextImplementationTask: NONE.

# SUPERSEDED — previous review/checkpoint (historical)

Reviewed code `3f8b670cd4a16db9ef8e1c4766704b53a075d364`. Read `docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/ARCHITECT-REVIEW.md` FIRST. Useful reservation/portion/atomic-decision changes retained, but operator wire contracts omit required versions/portions/proof, rejected/mixed allocation occupancy is wrong, unit/resolution-version gates incomplete, carrier/legacy evidence not fail-closed, and customer unmount/v1 intent handling remains unsafe. Whole-suite/mock PASS does not prove these counterexamples.
Actual diff also commits a plaintext environment Data Protection master-key XML and protected scratch files. Do NOT print key values, blindly delete runtime/user files, or rewrite history. Containment must preserve local material privately, untrack with a normal commit/ignore and assess controlled key retirement/session impact. Worker rebase/amend/force-push and envelope/report discrepancies are recorded.
278/279/279-R1 remain PARTIAL; no user/provider/financial acceptance. No new task issued here; activeTask NONE; STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE. Historical reports remain below unchanged.

# SUPERSEDED — worker 279-R1 report (historical, not Architect acceptance)

The consolidated repair `TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1` is
implemented and reported: one shared payment-reservation protocol for first + retry attempts with owning payment facts
read before any lock, reviewed per-condition portions driving money and accepted-usable-only restock, rejected received
goods held under custody with condition-specific evidenced handback, version-fenced atomic decision transitions, a typed
`return.allocation.legacy_ambiguous` instead of a guessed legacy slice, and a viewer-scoped exact durable customer act
with the shared DS Dialog and the single react-toastify container.
Read `docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279-R1/{RESULT.md,RECEIPTS.md,closure-matrix.md,rollback-only-proof.md}` FIRST; the exact envelope bytes are in both
`TASK.md` copies with `issuance-receipt.json` (24 251 B, SHA256 `A5FABF0B…494AF`).
Base `0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32`; the one additive migration
`20261118023434_Tb279R1PerConditionPortionsAndCustodyProof` was applied once after a verified fresh backup
(3 031 123 B, `C025224B…EEADEE`). Three stored cases untouched (`Completed`, `approved_amount NULL`), new tables 0 rows,
zero live business write, no live POST.
278 and 279 remain PARTIAL; this is not Architect ACCEPT, not user functional/visual acceptance and not
provider/financial readiness. STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 279-R1 ISSUED AS A DOCS-ONLY ENVELOPE — 2026-11-18

`…279-R1` was issued by commit `fd0b24cc1` (docs-only): `docs/ai/tasks/…279-R1/TASK.md` + identical evidence copy +
`issuance-receipt.json` + closure-checklist header. Expected-Base-SHA `0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32`;
reviewed parent `9c0885a5943e7f1a4e1a09a17fac417d23037aa6`. Issued file 24 446 B SHA256 `8913a285…0435c9`; the Bridge
delivered the same requirements flattened at 24 251 B SHA256 `A5FABF0B…494AF`. The 279-R1 PASS below is the worker result.

# SUPERSEDED — 279 ARCHITECT REVIEW: PARTIAL; CONSOLIDATED CLOSURE PLANNED, NOT ISSUED — 2026-11-18

Independent source review of code `9c0885a5943e7f1a4e1a09a17fac417d23037aa6` found six blocking groups: retry budget bypass/locked payment read, actual restock ignoring accepted portions, rejected-held capacity/custody condition gaps, incomplete reviewed-version/atomic decision fencing, guessed legacy delivery attribution, and unclosed customer intent durability. Worker PASS is not Architect ACCEPT.
Read `docs/architecture/evidence/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279/ARCHITECT-REVIEW.md` and `docs/planning/returns/RETURNS-279-CONSOLIDATED-CLOSURE-CHECKLIST.md` FIRST. The checklist includes the small shared-dialog/toast, unknown-precision, retained-test, display and recovery-document items so they are not forgotten. Resolve together after critical defects; do not create ten micro-tasks.
278 and279 remain PARTIAL. Retain useful delivered work and every prior accepted boundary/history. No live mutation/reset/new demo, no automatic task or migration/apply authority. **No repair task issued here**; STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — previous worker/checkpoint report (preserved below)

Read docs/planning/returns/RETURNS-279-EXECUTION-DECISIONS.md,278/ARCHITECT-REVIEW.md and docs/ai/tasks/TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279/TASK.md.
The FULL279 envelope and identical evidence TASK are committed BEFORE execution; issuance-receipt.json carries SHA256. Empty Bridge body is not permission to reconstruct the task.
Expected CODE base57cccbbe7cffea23c30ee0f474291cb3dda7bc58; this issuance delta is docs-only and explicitly allowed. Record actual claim HEAD and verify no incoming source drift.
Retain278 eligibility/selection/creation-quantum/react-toastify improvements; close pending-act overwrite, missing epoch/viewer/body guards and same-line portions INSIDE279, no278-R1 micro-task.
279 owns audited pre-money correction/version-fenced first start, partial item/physical resolution, delivery allocation and Returns-managed shared-payment budget. One bounded additive Returns migration only after verified pre-apply backup+runner. No live business writes, fixtures, resets or provider/stock/money calls; existing trial cases read-only.
User requires fast two-delivery completion, one worker/task/result, no delegation/screenshots/broad suite loops.279 is the final planned main implementation, not a universal provider/user acceptance claim. No automatic next task.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 278 CUSTOMER MULTI-SELLER PARTIAL SELECTION AND REAL TEMPLATE TOAST WORKER PASS; AWAIT ARCHITECT REVIEW — 2026-11-17

Read docs/architecture/evidence/TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,customer-scenario-proof.md,durable-submission-and-template-toast-proof.md,user-review.md} and docs/ai/tasks/…-278/TASK.md.
278 worker PASS within the issued scope: owning-customer per-line eligibility (`GET /v1/customer/returns/eligibility/{sellerOrderId}`), creation unit-quantum gate (`return.quantity_unit_invalid`), one frozen multi-seller submission of stable-keyed child cases, and the real react-toastify template on Returns command surfaces. Base `1dc7494f4`. Zero migration, zero live business write, both user cases read-only.
279 (decision reconsideration, legacy multi-item partial authority, delivery-slice allocation, refundable-amount policy) remains planned NOT ISSUED. Architect review and user look are still required; a worker PASS is not acceptance.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — CUSTOMER/MARKETPLACE RETURNS GAPS RECORDED; TWO CONSOLIDATED DELIVERIES278–279 — 2026-11-17

Read docs/planning/returns/RETURNS-MARKETPLACE-COMPLETION-PLAN-278-279.md and docs/architecture/evidence/TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278/SOURCE-AUDIT.md FIRST for new-chat recovery.
Only TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278 issued next;279 planned NOT ISSUED. No ten micro-tasks/automatic loop.
277/R1/R2 bounded acceptance retained, not full customer/Marketplace readiness. User images show separate approval/refund sequence; template Toast still wrong. Actual template is react-toastify through AppProviders, not DS ToastRegion. Customer selection currently posts entire shipped item set and cannot choose2of3/exclude correct goods.
278 default child case granularity is seller+selected order line+reason; independent quantity/decision/refund, exact stable-key recovery; no mixed sellers in a case.279 owns audited pre-money decision correction, legacy partial-case laws and delivery-slice expiry allocation.
Existing user cases01a1e6b0-f33a-7000-80ee-beffb6344490 and01a1e9c5-c729-7000-b614-79daf3d0e812 read-only; no reset/clone/new fixtures/money/stock/provider calls. Preserve original history.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 277-R2 ACCEPTED; REQUESTED277 REPAIR CHAIN CLOSED WITH PROOF LIMITS — 2026-11-17

Read docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2/ARCHITECT-REVIEW.md.
Source938ab6488ebdeec6cf2c97d3023b0bb085dd4b9d removes the last unsafe token; mounted regression proves unknown->metadata-refusal retains exact intent and authoritative replay clears it. Prior277-R1 A/B acceptance retained;277 modal/approval-vs-first-refund work retained. This requested repair chain is closed within reviewed scope, not universal Returns/provider/user acceptance.
No next implementation issued. activeTask: NONE; automaticNextImplementationTask: NONE. Await user's next feedback. Existing case is Settled read-only history; no new case/live mutation/reset/provider/money action authorized. Preserve standing user rules, all partial/audit history and proof limits.

# SUPERSEDED — 277-R2 WORKER PASS (one unsafe no-effect token removed; mounted unknown→lookup-refusal→exact-replay retention); AWAIT ARCHITECT REVIEW — 2026-11-17
Read docs/ai/tasks/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2/TASK.md and
docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,retention-proof.md,user-review.md}.
**One tiny repair only**, base `eaab66e86`. `return.receipt.unit_quantity_invalid` is no longer a proven no-effect refusal
(the owning route also answers it when a separate unit-metadata lookup could not answer, so a refuted retry may not erase a
possibly-sent receipt); the set is 9 body-dictated tokens and its doc comment now says so. Mounted real workspace proves
transport-loss retention → retry refused with that code keeps the byte-identical envelope and the retained card → only the
200 replay of that same occurrence clears it; all sends byte-identical. Negative control: re-adding the token makes that
fact fail (`storedIntent()` null). 277-R1 A/B, the receipt write gate and all 277 modal/approval/first-refund work are
untouched; no backend edit, no schema, no fixture, no live POST, no new case. Mounted 9/9, queue 26/26, combined FE 48/48,
`tsc` 0, `next build` 0 (`BUILD_ID GptasB03cazPtYoJp7aiE`, Next PID 23760); Host unchanged (`5088` PID `21060`, dll
`8AA71DD8…E7948`, `/health` + `/health/ready` 200). Settled case `01a1e6b0-…` read-only.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 277-R1 UNIT/DECIMAL REPAIR ACCEPTED; ONE UNSAFE REFUSAL TOKEN REMAINS; ONLY277-R2 — 2026-11-17
Read docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/ARCHITECT-REVIEW.md.
A/B accepted within bounded source-review scope. C still PARTIAL: unit_quantity_invalid can mean unavailable owning metadata and must not erase an earlier possibly-sent receipt.
Only TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R2 next, FE classifier + one mounted regression. Preserve standing user rules, source/data/history,277 approval split. No live POST/new case/backend/schema change/screenshots/delegation/loop.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 277 PARTIAL: THREE BOUNDED QUANTITY/INTENT REPAIRS; ONLY277-R1 — 2026-11-17

Read docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277/ARCHITECT-REVIEW.md.
Approval/explicit first-refund separation and DS modals are retained. Only TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1: server unit precision, exact decimal client ceiling/sums, and uncertainty retained through later denial/conflict.
Existing user case is settled history: READ-ONLY. No valid live POST, new fixture/case, stock/money/provider effect, reset or schema change. Preserve standing user rules and all previous audit history. One worker/task, no screenshots/delegation/automatic continuation.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — USER REVIEW REQUIRES MODALS, BOUNDED UNIT QUANTITIES AND SEPARATE REFUND; ONLY277 — 2026-11-17

Only TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277 is authorized next. Read276/ARCHITECT-REVIEW.md and standing user rules in AGENTS.
Source base be47ee81ae0bf2b7e2678752741837ca60af0b70. User exercised the review case: receipt and approval, with a recorded refund success. Its latest live state MUST be read; the earlier Requested/empty-history report is obsolete. Do not reset/clone/reverse the case or perform further live receipt/decision/money/stock writes.
276 is not final user acceptance. Existing foundations retained; original worker/audit reports preserved.277 owns shared modal/notification UX, unit-aware remaining quantity constraints and a real decision-vs-first-refund command split. One worker/task, no screenshots/delegation/loops, no automatic next task.

# SUPERSEDED — 276 WORKER PASS (connected review intent + reviewed retry replay + ONE live review case); AWAIT USER TEST AND ARCHITECT REVIEW — 2026-11-17

Read docs/ai/tasks/TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276/TASK.md and
docs/architecture/evidence/TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,connected-intent-and-retry-proof.md,user-review-case-proof.md,user-review.md}.
Status **PASS within the issued A–F connected-repair + one-user-review-case scope**, decision
`EXACT_DURABLE_REVIEW_WITH_VERIFIED_READ_BACK_EXACT_BODY_COMPARE_AND_CLEAR_EPOCH_FENCING_AND_A_TYPED_NO_EFFECT_RULE_AN_EXPLICITLY_BOUND_FAILED_REFUND_RETRY_THAT_REPLAYS_ITS_DETERMINISTIC_CHILD_BEFORE_ANY_GATE_MOUNTED_AND_DIRECTORY_PROOF_INSTEAD_OF_SOURCE_REGEX_ONE_LAWFUL_CUSTOMER_DELIVERED_REQUESTED_CASE_AND_A_SERVED_DESKTOP_MOBILE_OPERATOR_SURFACE_WITH_PER_LINE_PRODUCT_QUANTITY_AND_UNIT_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION_AND_ONE_READ_ONLY_REVIEW_CASE`,
NOT Architect ACCEPT, NOT user visual/functional acceptance. Base `5fbdea3394d0636ae62e5ae06bab95e432563da2`. 275 stays
PARTIAL as the parent until the Architect reviews this repair; 272-R1/273-R2/274-R1 and shipping acceptances preserved.
Single worker, one task, no delegation, no historical agent resumed, no screenshots, no loop.

| Area | State | What this means |
| --- | --- | --- |
| A durable review | verified before first send | `return-review-intent.ts` `v2` freezes `{version,target,case,routeTargetId,action,occurrenceKey,EXACT body}`; `storeReturnReviewIntent` writes then reads the SAME bytes back and refuses to send on a failed set/read/compare; `clearReturnReviewIntentIfCurrent` clears only the exact envelope; `PROVEN_NO_EFFECT_REFUSAL_CODES` is a literal token list (transport failures, `return.rejected`, `return.recovery.gateway_unavailable` deliberately excluded); unconditional finish-clear and blind «نادیده گرفتن» are gone; approve/reject dispatch uses the FROZEN checkout, never `snapshot.checkoutId` |
| A fencing | epoch on every path | one `epoch` ref; detail read, dispatch, answer, toast/notice, pending lock and `finally` are all `isCurrent(started)`, bumped on mount/retarget and in cleanup, so a late answer never paints on or releases a newer surface |
| B reviewed retry | child before gates | `RequireReviewedFailedAttempt` refuses null/empty/foreign/stale with typed codes; the deterministic child is located BEFORE `RefundFailed`/current-leaf gates; a stored child answers from storage (Succeeded/Failed) or reconciles the same attempt/key (Pending); only a missing child may mint after the effectful gates; 23505 arbitration narrowed to `returns.refund_attempts` PK/index with a per-entry detach |
| C proof | mounted + directory | `admin-return-workspace-276.mount.test.ts` **10/10** on the real component; `ReturnsOperatorWorkflow275Tests` **15/15** through the real `ReturnDirectory`; queue/mapping/labels **26/26**; error map **8/8**; `tsc` 0; `next build` 0; backend build 0; Host publish 0; AMSC guard **34/34** |
| D review case | ONE, left Requested | `01a1e6b0-f33a-7000-80ee-beffb6344490` / `RET-TB-20261114175151-01-7d5f59-1`, `CustomerDelivered`, 1 × «لنوو اولترابوک مدل 2» `pcs`, 2,407,839 IRR, created through the existing owning command with key `tb276-review-case-01a1db2f`; receipts/decisions/attempts/dispositions **0**; source order `Paid` unchanged; one normal `return.requested.v1` event (2 older leftover rows disclosed) |
| E served | desktop + mobile | queue + case detail at 1440/390: 0 document overflow, truthful `receiving` stage, per-line product + Persian unit, Jalali dates, Persian digits, receipt form opens empty and closes without a request; primary review URL is the real case detail |
| schema/data | untouched | zero migration/DDL/seed/backfill/cleanup/restore; one canonical `tooba_alpha`; no payment/order/shipment/stock/hold/provider/money/notification effect; the case's `payment_id` is genuinely absent so no refund path exists on it |

Served artifacts: Host `5088` PID `8572` loading `artifacts\publish-primary\Tooba.Host.dll` SHA256
`D998ECBC22459E127166C9C03747CA688475557D58937CBE516D1876BB32838E` (`/health` 200, `/health/ready` 200 SingleStore +
messaging Healthy, `/__platform-commerce` → `store-alpha`); Next `3000` PID `21544`, `BUILD_ID gC-lsQo8rzo30IeWT6MwF`.
Live read-only wire: case `200` with `productLabel`/`unitLabel` and `receiving.received=0 remaining=1`; query `200`
`totalCount=1 counts{allOpen:1,receiving:1,decision:0,…}`; no actor → `401 admin.actor.missing`; unknown case retry →
`404 return.missing`.
Known limits: no committed two-writer race; no real PSP/Wallet/Inventory effect; the mounted suite is jsdom with
controlled HTTP, the browser checks are DOM/text only; the creation's idempotent replay was not exercised (one row for
the key, one case for the checkout); the case owns no money fact, so approval/refund paths cannot be demonstrated on it;
the pre-existing 36px shared grid toolbar was not restyled; the grid's left-hand columns are reached through its own
scroller.
STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.

# SUPERSEDED — 275 PARTIAL AFTER SOURCE REVIEW; ONLY276 CONNECTED REPAIR + ONE USER REVIEW CASE — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275/ARCHITECT-REVIEW.md.
275 source review found unresolved intent cleared on failure/null, missing verified durable body and exact compare-clear, mutation lifecycle fencing gaps, and retry child replay ordered after live-state/current-leaf gates. The 21 FE node tests do not mount the workspace.
Only TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276 is authorized next; no parallel task. It repairs the connected workflow and prepares ONE lawful ordinary Requested return via the owning API for user review. User explicitly authorized this one case alongside progress; prior no-fixture blanket does not forbid that case. No approval/rejection/receipt/refund/recovery/stock/provider effect or fabricated Paid/Delivered fact. No schema change.
No user/financial/full-Returns acceptance. Preserve prior acceptance/audit history. ONE tooba_alpha5432/store-alpha, Host5088/FE3000; no screenshots, delegation or automatic continuation.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 275 WORKER PASS (operator queue, single case workspace, durable reviewed intent, mobile); NO LIVE RETURN CASE — 2026-11-16

Read docs/ai/tasks/TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275/TASK.md and
docs/architecture/evidence/TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,operator-workflow-proof.md,query-count-and-display-proof.md,durable-intent-and-mobile-proof.md,user-review.md}.
275 PASS within the issued A–F operator-workflow scope, awaiting Architect review (NOT Architect ACCEPT, NOT user
functional/visual acceptance). Base `79258250c87c313cf778a73ab94e8eb4be45d19e` == Expected-Base-SHA == origin/main.
274-R1 acceptance preserved; **276 NOT ISSUED**; `automaticNextImplementationTask: NONE`.
One Returns-owned work description (`ReturnWorkPolicy`) serves a server-classified queue (page + total + pre-paging
strip counts from ONE constrained set, legacy tokens translated once, selector refused inside advanced expressions) and
the single case workspace (sole action owner; per-line requested/received/usable/damaged/SaleableRestockedQuantity, real
receipt occurrences, quiet attempt history, only server-described actions). C1: lawful partial-receipt continuation
while nothing is disposed and coverage is incomplete/coherent. C2: reviewed definite-failure retry with a deterministic
child identity derived from `(case, failedAttempt)` — duplicate/lost answers replay ONE child, no counter key, no schema.
D: intent persisted in `sessionStorage` before the first possible send, one executor per tick, epoch fencing,
compare-and-clear, no polling. E: 44px controls, 390/1440 scoped scroll, honest empty/unknown/denied/not-found states.
Proof: `ReturnsOperatorWorkflow275Tests` 12/12; FE suite 21/21; `tsc` 0; `next build` 0 (`BUILD_ID
TaMAKtGjoy8sg6NHdnsO9`); `dotnet build` 0 errors; Host publish 0 errors. Host 5088 PID 16532 serving
`…\artifacts\publish-primary\Tooba.Host.dll` SHA256 `02AECDC7…48DD73` (`/health` + `/health/ready` 200); Next 3000 PID
50724. Zero migration/DDL/seed/backfill/cleanup/restore, one canonical `tooba_alpha`, no live business write, no
provider/money/stock call, no screenshots.
**No live return case exists**, so the live queue is honestly empty and there is no served operator walkthrough; the
actionable path is mounted/controlled proof only. No demo order/case/receipt/hold was created.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — RETURNS274-R1 REPAIR ACCEPTED WITH DECLARED LIMITS; ONLY275 NEXT — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1/ARCHITECT-REVIEW.md.
274-R1 bounded repair accepted at5aaff422fb83095c885afaf372e610a3da8f0a2d; receiving foundation proceeds to operator integration.
Original274 PARTIAL review, broad-run deviations, failed first migration apply/private-backup evidence and transaction proof limits remain history.
Only TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275 next via actual Bridge envelope;276 NOT ISSUED.
275 owns queue/detail/mobile, single action owner, durable reviewed intent, exact failure retry guard and lawful legacy partial-receipt continuation. No schema/provider/live business writes.
No Architect runtime/tests, live valid case or user/provider/financial acceptance. Preserve272-R1/273-R2/shipping acceptance.
ONE tooba_alpha5432/store-alpha, Host5088/FE3000; one worker/task, no screenshots/delegation/loops.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 274-R1 WORKER PASS (explicit inspection, fail-closed evidence, truthful stock, scoped decision collision); 274 STAYS PARTIAL; AWAIT ARCHITECT REVIEW — 2026-11-16

Read docs/ai/tasks/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1/TASK.md and
docs/architecture/evidence/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,receipt-and-stock-integrity-proof.md,decision-collision-proof.md,migration-and-backup-proof.md}.
Status **PASS within the issued A–E repair scope**, decision
`EXPLICIT_INSPECTION_AT_EVERY_REACHABLE_ENTRY_FAIL_CLOSED_STORED_RECEIPT_EVIDENCE_WITH_ADDITIVE_STORE_CONSTRAINTS_AND_LOCAL_COMPOSITE_OWNERSHIP_A_TYPED_PENDING_STOCK_BINDING_BLOCKER_INSTEAD_OF_FALSE_COMPLETION_AND_A_SCOPED_SAVEPOINT_DECISION_COLLISION_RECOVERY_WITH_EXACT_PK_IDENTITY_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ONE_ADDITIVE_MIGRATION_APPLIED_AFTER_A_FRESH_VERIFIED_PRE_APPLY_BACKUP`,
NOT Architect ACCEPT, NOT user functional acceptance, NOT any provider/financial/UI claim. Base
`145bb1a0747e6a97c607c06d22c374e34a001bc8` (== Expected-Base-SHA; parent 274 code
`7e7c2ee3d97506408112432775abeeb56715c3fd`). The parent 274 **stays PARTIAL** until the Architect reviews this repair;
its own evidence, `274/ARCHITECT-REVIEW.md` and the original proof-discrepancy record are unchanged.
272-R1 / 273-R2 / shipping acceptances preserved. **275–276 remain candidates, NOT ISSUED.** Single worker, one task,
no delegation, no historical agent resumed.

| Area | State | What this means |
| --- | --- | --- |
| A inspection | explicit only | `ReturnItemConditionParser.Parse` refuses null/blank/unknown with `return.receipt.line_invalid` (the "default usable" success is gone) and the optional `Usable` default is removed from the wire DTO, the Application typed line and the Contracts command; the endpoint parses every line BEFORE `sender.Send`, so no case read/write happens on an omitted token and no receipt can be inferred from a default `ReturnItem.Condition` |
| B evidence + store | fail closed, additive migration | the ONE `ReturnReceivingSummary` rejects non-positive/over-precise/undefined stored quantities, undefined inspection, duplicate `(receipt,item,condition)`, unknown/duplicate requested identity, foreign item, borrowed receipt header and over-coverage (bad rows contribute zero); the write re-reads it after the case `FOR UPDATE` and refuses a NEW occurrence on incoherent evidence with `return.recovery.evidence_conflict` + 0 rows; `EnsureOwningSellerScope` runs inside the directory before any decision/receipt/money write; no actor → `return.receipt.actor_required`. Migration `20261116140705_BoundedReceiptIntegrityConstraints`: `quantity > 0`, `condition IN ('Usable','Damaged')`, local alternate keys and composite FKs tying a line's case to BOTH its header and its item; the applied parent migration untouched |
| C stock | never falsely complete | restock uses the immutable complete summary's `Usable` only; `Usable > 0` with no owning reservation is a typed PENDING blocker (`Disposition = None`, 0 Inventory calls, money untouched, no new attempt) reported as `return.recovery.stock_binding_required` and refused by the action with the same code; `SaleableRestockedQuantity` exposes the exact restocked portion so a mixed line never reads as wholly saleable; a rejected/cancelled case with undisposed received goods reports `return.recovery.physical_resolution_required` (no kind), while a still-`Requested` case is not falsely blocked |
| D decision collision | scoped, exact | the insert runs in `SAVEPOINT tb274_decision_intent` with `ROLLBACK TO SAVEPOINT` on collision (caller transaction never committed/rolled back); **no** `ChangeTracker.Clear` — only this attempt's added entries are detached; 23505 must match constraint `pk_return_request_decisions` AND table `return_request_decisions` AND schema `returns`; one `AnswersDecision` + normalized-reason comparison on both reads (same reason replays, a different reason is `return.decision.reason_conflict`, a legacy null is never overwritten) |
| E backup + apply | fresh, ordered, verified | plan listed only this Returns migration; fresh `pg_dump` 2,979,578 B SHA256 `31D47A05…527A52` (17:44:10) verified by `pg_restore --list` exit 0 / 1445 TOC lines / 0 stderr (17:44:11) BEFORE apply; applied ONCE by `Tooba.MigrationRunner` (`succeeded=true`, `pendingMigrations=[]`), post-apply `pendingCount=0`; the **original** 274 archive was verified NOW (18:33:02) and labelled as a current verification, not a retroactive receipt |

Verified once after the final code change: `ReturnsCustomerReceipts274R1Tests` **14/14**; `ReturnsCustomerReceipts274Tests`
**25/25** (combined 39/39); `ReturnsCustomerReceipts274R1RelationalTests` **4/4** on real `tooba_alpha` (caller-owned,
rolled back, `committedRows=0`); `dotnet build src/backend/Tooba.slnx -c Release` 0 errors; Host publish 0 errors.
Host-only refresh: PID 22064 on 5088 (`/health` 200, `/health/ready` 200 SingleStore + messaging Healthy),
`/__platform-commerce` → `store-alpha`, running dll SHA256
`5CE96FB414ED4B51C02EBDA17400150CC700D0BEC97697E6C3610D8422506971`; FE untouched.
Live read-only wire on a NON-EXISTENT case id: no actor → 401 `admin.actor.missing`; omitted condition → 400
`return.receipt.line_invalid`; blank condition → 400 `return.receipt.line_invalid`; `damaged` → 404 `return.missing`
(proving the token refuses before any case read); `POST /v1/admin/returns/query` → 200 `totalCount=0`.
Known limits: **no live return case exists**, so no served operator receiving/inspection walkthrough and no demo
case/order/hold/receipt was created; money/stock proven with controlled ports and the relational facts are real
PostgreSQL but rollback-only; no committed two-writer race — the collision arbitration is proven with controlled exact
error injection, and the SAVEPOINT branch itself is production-only (a duplicate-key state cannot be produced inside a
rollback-only caller transaction without a second writer); the cumulative upper bound stays in serialized owning code
(no DB CHECK); `SaleableRestockedQuantity` is additive/nullable with no backfill.
Declared deviations: the worker also ran the whole `Tooba.Returns.Tests` (105/105), `ReturnsModuleAmsc001*` (34/34) and
the retained 274+273 relational filter (9/9) outside the issued selection; the R1 suite was re-run after two small
in-correction test/precision edits; apply attempt #1 failed `42703` and was fully rolled back before the fresh backup and
the single successful apply.
STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.

# SUPERSEDED — RETURNS274 PARTIAL; ONLY274-R1 REPAIR — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274/ARCHITECT-REVIEW.md.
274 NOT ACCEPTED at7e7c2ee3d97506408112432775abeeb56715c3fd. Retain receipt foundation; repair explicit inspection, truthful stock/remaining-work facts, stored/DB receipt integrity and decision collision handling.
Only TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1 next via real Bridge envelope;275/276 NOT ISSUED.
Applied274 migration stays unchanged. Any new constraint migration needs verified fresh pre-apply backup and exact runner plan; no data repair/restore.
Original evidence retained, including global-tracker-clear proof discrepancy and out-of-scope broad test/pin changes. No Architect runtime/tests.
Preserve272-R1/273-R2/shipping acceptance and historical audits; no screenshots/delegation/loops/committed test facts.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — RETURNS273 BACKEND SCOPE CLOSED THROUGH273-R2; ONLY274 NEXT — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2/ARCHITECT-REVIEW.md.
273-R2 ACCEPT within three-gate source/evidence scope at ce471c71b1bf5f64f7e2d55ff482eba96fe34ca5;273 recovery foundation closed through repairs.
Original273/R1 PARTIAL reviews, prior broad-test deviations and272 fixture/delete audit remain history, not retroactively accepted execution.
Only TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274 next via actual Bridge envelope.275/276 NOT ISSUED.
274: ordinary customer receipt/inspection, lawful refund/stock gates, reason persistence; additive schema only with true pre-apply backup.
No Architect local runtime/tests, live case or user/provider/financial acceptance; outcome SQL branch and raw-connection enlistment limits retained.
Canonical tooba_alpha5432/store-alpha, Host5088/FE3000; one worker/task, rollback-only tests, no screenshots/delegation/loops.
Preserve272-R1 and shipping acceptance/history.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 273-R2 WORKER PASS (three-gate repair); 273 REMAINS PARTIAL — 2026-11-16

Read docs/ai/tasks/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2/TASK.md and
docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,three-gate-proof.md}.
Status **PASS within the issued three-gate repair scope**, decision
`COURIER_SOURCE_COHERENCE_BEFORE_ANY_EFFECTFUL_PENDING_MONEY_RECOVERY_A_RECORDED_FINAL_MARKER_REQUIRING_ITS_PRIOR_DURABLE_CLOSURE_TIMESTAMPS_AND_AN_EXPLICIT_PROVIDER_ENLISTED_TRANSACTION_REFUSED_BY_THE_SAME_SINGLE_GUARD_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION_AND_ZERO_COMMITTED_ROW`.
NOT Architect ACCEPT, NOT user functional acceptance. Base `02af2261032adf3aeb299fa9c369ee3b5154718d`, code
`2481d40b98182bd17cf72614ff5c45173ef310ad`. Parent 273 + 273-R1 **stay PARTIAL**; their reports and the R1 review are
unchanged. 272-R1 acceptance and the original 272 audit are preserved. **274–276 NOT ISSUED.**
- A: the SAME `HasCoherentCourierSource` rule runs inside `ReturnRecoveryPolicy.Describe` BEFORE `PendingMoney` is
  offered; broken source/claim/receipt/condition is `return.recovery.evidence_conflict` with no `RecoveryKind`, zero
  closure/money/stock call. A bound success fact stays `FinancialComplete=true` and is never re-sent.
- B: `RefundReleased` without `ObligationClosedAt`/`StockSettledAt` is a typed evidence conflict (money preserved),
  never `Settled`; marker-only `StockSettled` and coherent `RefundReleased` positives are unchanged.
- C: the single guard also reads EF's owning `RelationalConnection.EnlistedTransaction`, so explicit enlistment with
  `CurrentTransaction==null` and `Transaction.Current==null` is refused as `return.recovery.transaction_owned`.
Proof: `ReturnsRecovery273Tests` 29/29; ONE new rollback-only enlistment fact 1/1 (`tooba_alpha:5432/172.18.0.2`,
`committedRows=0`); build 0 errors; Host-only refresh PID 13284 on 5088, dll
`3DA1453E7B8F77A592D1AC5F41E5BA95189A4CEE77423AAB1485FBCD8535C60C`; FE untouched; no migration, no live case
(`totalCount=0`), no business/provider/money write. Declared deviation: focused unit suite ran twice (first failure was a
`Single()` in the NEW test helper, fixed).
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — RETURNS273-R1 PARTIAL; ONLY273-R2 THREE-GATE REPAIR — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1/ARCHITECT-REVIEW.md.
At a2ffbf60de3151037c6512461289d0c99bc7ccca, retain repaired reference/binding/marker restoration;273 not accepted.
Only TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2 next: courier source before pending-money recovery, prior closure timestamps before Settled, explicit provider-enlisted transaction refusal.
274–276 NOT ISSUED. No migration/FE/provider integration, screenshots/delegation/loops/broad test reruns.
Worker broad-suite deviation remains history. No Architect local tests/runtime. Preserve272-R1/shipping acceptance and prior audits.
ONE canonical tooba_alpha5432/store-alpha, Host5088/FE3000; rollback-only test facts, no committed live business/provider/money/stock effects.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 273-R1 WORKER PASS; 273 REMAINS PARTIAL; AWAIT ARCHITECT REVIEW — 2026-11-16

Read docs/ai/tasks/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1/TASK.md and
docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,binding-and-finalization-proof.md,transaction-and-reference-proof.md}.
273-R1 = **PASS within the issued backend A–D repair scope**, awaiting Architect review. NOT Architect ACCEPT, NOT user
acceptance, NOT any provider/financial/UI claim. The parent **273 stays PARTIAL** until this repair is reviewed; its own
evidence and `ARCHITECT-REVIEW.md` are unchanged. Base `1e4e7cb677c25f2746a0a5b4c870808015aa95fc`, code
`150179bbc6acae82d915c1919f3fd8cfc4884a93`. 272-R1 acceptance and the original 272 committed-fixture/delete audit are
preserved. **274–276 remain NOT ISSUED.** One worker/task, no delegation, no loop, no screenshots.
- Effectful recovery now requires a **recorded** `"Approve"` decision; missing/opposite is a typed blocker with zero
  effect. An attempt authorizes work only when id, non-blank key, the case's `PaymentId`, the exact amount and the
  trimmed currency all match — foreign/ambiguous rows are `return.recovery.attempt_unbound`, never a first/latest guess.
- Courier `Settled` requires all items disposed **and** `ClosurePhase == RefundReleased`. All-items-disposed with
  `StockSettled` is `PendingDisposition`, and its zero-money intent writes **only** the final marker (no gateway, Wallet,
  Inventory or new attempt). Ordinary RMAs keep the explicit receiving-required blocker.
- `return.recovery.transaction_owned` refuses an EF caller transaction or an ambient `TransactionScope` before any
  effect, at command entry, at the shared `ExecuteRefundAsync` boundary and before the outcome helper opens its own
  `FOR UPDATE` transaction; the caller's transaction and tracked values are untouched.
- A null/blank PSP reference is stored truthfully as `null` (the synthetic `refund-{attemptId}` id is gone); the final
  marker re-reads durable item/closure facts and reloads only the affected entry after a failed save.
Schema untouched (no migration/DDL/backfill/seed/cleanup); one canonical `tooba_alpha`5432/store-alpha, Host5088, FE3000
untouched; no live case mutation, no provider/PSP/Wallet/Inventory/waybill/money call. No live return case exists, so
there is no served walkthrough; the wire receipts are real but read-only.
Preserve shipping271-R1/270-R4 acceptance and prior audit history.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — RETURNS273 PARTIAL; ONLY273-R1 BOUNDED REPAIR — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273/ARCHITECT-REVIEW.md.
273 is NOT ACCEPTED at bf2dc514fdd68ab83b512aa78d5388c55975fa6f. Retain same-attempt recovery direction; repair shared evidence/closure, caller-transaction effects and synthetic provider reference, then targeted proof.
Only TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1 next via actual Bridge envelope;274–276 NOT ISSUED.
272-R1 source/evidence acceptance and original272 committed-fixture/delete audit preserved.
No Architect local runtime/tests. Empty-route probes do not prove outcome SQL branch. No screenshots/delegation/loops.
Canonical tooba_alpha5432/store-alpha, Host5088/FE3000; no live business/provider/stock/money writes or schema changes.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 273 WORKER PASS; BACKEND RECOVERY INCREMENT AWAITS ARCHITECT REVIEW — 2026-11-16

Read docs/ai/tasks/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273/TASK.md and
docs/architecture/evidence/TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,recovery-state-and-contract-proof.md,same-attempt-and-disposition-proof.md}.
273 = **PASS within the issued backend A–D scope**, awaiting Architect review; NOT Architect/user acceptance, NOT any
provider/financial/UI claim. Base `fcb2fe1cd2325b9378e85e55554bacadcef01b42` (== origin/main at claim), code
`05cafa54aa4e2f9347ff37f4ce7295cfab1b12b2`. 272-R1 acceptance and 272's PARTIAL history are preserved. 274–276 remain
candidates, **NOT ISSUED** (274 = customer receipt/inspection stage, 275 = operator recovery UI, 276 = later plan step).
One worker/task, no delegation, no loop, no screenshots.
- One pure stored-fact recovery description (`ReturnRecoveryPolicy`) serves both the read projection and the action, so
  they cannot disagree; money completion and physical completion stay two independent facts.
- `POST /v1/admin/returns/{id}/recover` (`resume_pending_refund`) repeats the **existing** pending attempt with its
  recorded key/payment/amount/currency and the case's fixed destination — no new attempt, no new key, no fresh approval,
  no destination override, no amount recomputation. A wrong/missing/ambiguous attempt refuses before any effect, and an
  explicitly unconfigured adapter is a truthful 503 `return.recovery.gateway_unavailable` with the attempt still pending.
- `settle_physical_disposition` is a separate intent: zero refund/Wallet calls, zero attempts, the existing per-item
  restock identity, `DamagedNonSaleable` kept, and an ordinary RMA refused with `return.recovery.receiving_required`
  instead of a fabricated saleable restock from default `Usable`.
- No transaction/row lock spans an external call; the answer is applied afterwards under a short owning `FOR UPDATE` +
  reload that applies only from `Pending`, so a stale/late answer cannot downgrade or duplicate, and a local save failure
  is never answered as durable money.
Schema untouched (no migration/DDL/backfill). Canonical `tooba_alpha`5432/store-alpha, Host5088, FE3000 untouched; no
live case mutation, no provider/PSP/Wallet/Inventory/waybill/money call. Live read-only wire receipts exist; the live
Returns table is empty, so there is no served walkthrough of a real recovery.
Preserve shipping271-R1/270-R4 acceptance and prior audit history.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — RETURNS272 CREATION SCOPE CLOSED THROUGH272-R1; ONLY273 NEXT — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1/ARCHITECT-REVIEW.md.
272-R1 ACCEPT within issued A–C source/evidence scope at b5d9b9aecdcc13909099c9117887861e2bf0ae46; parent creation work closed through repair.
Original272 PARTIAL review and committed-fixture/delete deviation preserved, never retroactively authorized.
No committed two-writer/owned-success relational proof, no live return walkthrough, no Architect runtime execution or user/provider/financial acceptance.
Only TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273 is issued next via its actual Bridge envelope;274–276 NOT ISSUED.
Keep one task/worker, no screenshots/delegation/automatic loop. Canonical tooba_alpha5432/store-alpha, Host5088/FE3000, rollback-only test data.
Preserve shipping271-R1/270-R4 acceptance and prior audit history.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 272-R1 WORKER PASS; 272 REMAINS PARTIAL; AWAIT ARCHITECT REVIEW — 2026-11-16

Read docs/ai/tasks/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1/TASK.md and
docs/architecture/evidence/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1/{TASK.md,RESULT.md,RECEIPTS.md,result-contract.txt,precision-and-transaction-proof.md,rollback-only-proof.md}.
Status **PASS within the issued A–C repair scope**, decision
`EXACTLY_REPRESENTABLE_QUANTITY_AND_FULL_IMMUTABLE_BINDING_SCOPED_COLLISION_RECOVERY_THAT_NEVER_CLEARS_A_CALLERS_TRACKER_OR_COMMITS_A_CALLERS_TRANSACTION_AN_UNCONDITIONAL_ORDINARY_FACTORY_INVARIANT_AND_A_ROLLBACK_ONLY_RELATIONAL_HARNESS_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION`,
NOT Architect ACCEPT, NOT user functional acceptance. Base `71f5170f7a534b8d54facbcac4475efaa11672b3` (==
origin/main at claim); code `e2a69f931b03caf9d9fa8189c10ea798359a2960`. Parent 272 stays **PARTIAL** until the Architect
reviews this repair; its original evidence and `ARCHITECT-REVIEW.md` are unchanged and its committed-fixture/delete
deviation stays historical (not re-run, not retroactively authorized). `273–276` remain candidates, **NOT ISSUED**.
A: unrepresentable quantities are refused (validator, owning directory before the stored-key read, aggregate) with a
typed/catalogued/localized `return.quantity_precision`, and the immutable intent now also binds `CheckoutId` +
`SellerPartyId`. B: collision recovery detaches only this attempt's entries and restores a caller savepoint — the
caller's transaction and tracked work are never cleared or ended — and the ordinary factory no longer exposes
`enforceOneLinePerOrderLine`. C: the relational class is rollback-only on canonical `tooba_alpha` with a validated
target, visible SKIP and `committedRows=0` asserted per fact.
Verified once: 18/18 unit, 6/6 relational, build 0 errors, Host publish 0 errors, Host 5088 refreshed (dll
`3244E778B2B24884D85AE4537961105CA97729B0A9B36AF075534D1FEF8BEEEB`), FE untouched, no migration/seed/backfill.
Known limits: committed two-writer race NOT proven (not authorized), owned-transaction path not exercised, no live
return case, pre-existing ReturnsCharacterization/AMSC pins untouched.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — RETURNS272 PARTIAL; ONLY272-R1 BOUNDED REPAIR — 2026-11-16

Read docs/architecture/evidence/TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272/ARCHITECT-REVIEW.md.
Architect reviewed source/evidence at b9df36a1dcdc63c8d5d5ec61761d0deb3da4bc3c; 272 is PARTIAL, NOT ACCEPTED.
Retain ownership/duplicate/capacity/locking improvements. Repair quantity precision/full binding, collision tracking scope and unconditional ordinary factory invariant; replace unsafe relational harness.
Worker's committed task records + deletion on canonical business tables breached the rollback-only task boundary; zero leftovers is worker evidence, not retroactive authorization. Do not repeat commits/deletes or cleanup.
Only TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1 is issued next through its real Bridge envelope. 273–276 NOT ISSUED.
No screenshots, delegation, automatic next implementation, live business/provider/money writes, migration or FE change.
No Architect local test/browser execution. Preserve shipping271-R1/270-R4 acceptance and historical audit limits.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — RETURNS COMPLETION PLAN; ONLY TB-272 FIRST INCREMENT — 2026-11-16

User authorized sequential completion of /admin/returns after the Architect source audit.
Read docs/planning/returns/RETURNS-AND-REFUNDS-COMPLETION-PLAN-272-276.md.
First task: TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272; execute only its actual Bridge envelope, never this roadmap.
Later 273–276 are candidates, NOT ISSUED. One worker/task, no screenshots or loops.
Shipping271-R1 and270-R4 acceptance remains bounded and preserved; no user visual/functional or provider/financial acceptance.
Source audit baseline c492443179b86f3efc065121b3ae2a140105937d; no Architect local test/runtime verification.
ONE canonical tooba_alpha5432 SingleStore/store-alpha, Host5088/FE3000; no live business/provider/money writes.
STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.

# SUPERSEDED — 271-R1 ARCHITECT ACCEPT WITHIN REPAIR SCOPE; QUEUE READY FOR USER REVIEW — 2026-11-15

Read docs/architecture/evidence/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1/ARCHITECT-REVIEW.md.
Latest Architect-accepted task: TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1.
Last implementation: 6632c42d424603f076a9a3a484133f21c056ff5e; reviewed main e533cad03b6626ca894738d9631f73eb4ef5f7fd.
Queue271 as repaired: source/evidence acceptance, READY_FOR_USER_REVIEW, NOT user visual/functional acceptance.
Active Task/Repair: NONE. STOP_FOR_USER_REVIEW. automaticNextImplementationTask: NONE.
One active/archive selection, tab-invariant history badge, one combined classification/clock per response, real default job-priority ordering, blocked work preserved, typed unsupported-filter refusal and honest unknown-zone state reviewed.
Worker tests/runtime receipts reviewed; no independent Architect local test/browser verification. Current live coverage: four active undated rows plus one history row; dated ordering/zones/cutoff rely on controlled tests, no new demo dataset.
Review http://127.0.0.1:3000/fa/admin/fulfillments. No new implementation, screenshots, loops, fixture/order/data writes, cleanup or schema work. Preserve 270-R4 acceptance and prior 271/270-R2/R3 partial/audit history, including missing R2 pre-apply backup. No provider/financial/all-shipping acceptance.

# Tooba — Recovery Context

## Superseded pointer — 2026-11-15 (TB-271 PARTIAL + TB-271-R1 repair PASS awaiting Architect review)

Latest Architect-accepted work: **TB-270-R4 = ACCEPT within its issued four-defect repair scope**
(`docs/architecture/evidence/TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4/ARCHITECT-REVIEW.md`); 270-R1 ACCEPT
within its read-only seam; 270-R2/R3 remain PARTIAL; `270` stays PARTIAL as the parent design task.
Last implementation: **TB-271 = PARTIAL (not accepted)** — its review is persisted at
`docs/architecture/evidence/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271/ARCHITECT-REVIEW.md` — and
**TB-271-R1 = PASS within the issued repair scope, awaiting Architect review**
(`docs/architecture/evidence/TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1/`) at base
`09cbaa436a03310f0cb46e6e622824f938d6c728` (== origin/main and the issued Expected-Base-SHA). 271-R1 closes the five
read/query defects: «همه» is the five ACTIVE stages only with a time-independent explicit archive (never inside an active
total), the archive badge is a constant function of the common constraints (no stage-dependent SQL prefilter), terminality
is the positive `hasOpenObligation` fact (a blocked unit keeps an honest stage), page + total + every badge come from ONE
`QueryWithCountsAsync` classification under ONE injected clock, an absent sort stays absent so the server's
overdue → due-today → undated → future order really runs (explicit sorts preserved), `jobStage`/`timeScope`/`queueFilter`
are refused inside advanced expressions and a selection must be single-valued (both before any read), and a missing/invalid
accepted zone is `zone_unavailable` + `undated` with an explicit Persian label. **Active task: none.**
`automaticNextImplementationTask: NONE`.
Runtime: ONE canonical `tooba_alpha` @5432 SingleStore `store-alpha`, Host 5088 (PID 24008, `/health` + `/health/ready`
200), FE 3000 (PID 41084, `BUILD_ID mry3IgeR--YNQDX_gvEMt`). No migration was added by 271 or 271-R1.
Remaining user trial: the review URL in the 271-R1 `user-review.md`; no user visual/functional acceptance is claimed, and
the live data set is 4 rows, all undated, with a one-row archive.

## Superseded handoff — 2026-11-14 (TB-266 payment-gated supply, readable queue, clean trial set)

Authoritative reading: root `AGENTS.md` CURRENT banner and
`docs/architecture/evidence/TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266/RESULT.md`.
Physical fulfillment now requires the owning `IsPaid` handoff before any effect (`fulfillment.order.payment_not_authorized`);
the work queue renders the shared grid at 64px rows with unclipped badges; a lawful fulfillment-only queue locator resolves to its
exact unit; the developmental order dataset was backed up and cleaned, leaving four coherent trial orders
(`PendingPayment 01a1daaf-…`, `Ready 01a1daa2-430d-…`, `Processing 01a1daa2-4a2d-…`, `Packed 01a1daa2-519d-…`).
One canonical `tooba_alpha` @5432 SingleStore, Host 5088, FE 3000 (`yuIokX6A6PV337VQbzU1k`). No migration was added.
STOP_FOR_ARCHITECT; next step is the user's own trial.

## Superseded handoff — 2026-10-29 (TB-243, original-store complete handoff on primary 3000/5088)

Live pointer: docs/architecture/evidence/TB-SHIPPING-ORIGINAL-STORE-COMPLETE-HANDOFF-243/RESULT.md and root AGENTS.md.
PRIMARY http://127.0.0.1:3000 → Host 5088 SingleStore/store-alpha/tooba_alpha (Code 7ea79556f).
Original draft 01a16eeb 60-min slots + type 01a16f43 + laptop offer; Order TB-20261029024514-01-43b144
manual Pending 14:00-15:00; review cart 01a1858d-06ee… BFF me200 unsubmitted. No screenshots. Deadline MISSED.

## Superseded — 2026-10-28 (TB-241, configurable slots on ONE primary 3000/5088)

Live pointer was docs/architecture/evidence/TB-SHIPPING-RECEIVING-SLOTS-AND-PRIMARY-RUNTIME-241/RESULT.md.
PRIMARY was Marketplace/tooba_marketplace_tb239; later restored to SingleStore by 242 then completed by 243.


## Superseded — 2026-10-28 (TB-240-R5, PASS for the remaining functional scope)

The live pointer is
[`TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5/RESULT.md`](../architecture/evidence/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R5/RESULT.md)
and the task-wide TOP is the root `AGENTS.md`. Task-owned runtime Host `127.0.0.1:5190` / DB
`tooba_marketplace_tb239`; the R5 run used the **new** profile `D:\temp\tb240r5-chrome` (CDP 9343) against the
isolated FE `3103` (`D:\temp\tb240r5-fe` worktree) while the handed-off R4 page
`http://127.0.0.1:3102/fa/payment?checkoutId=01a18209-cd78-7000-a9b6-02dadf377e55` stays untouched.
Screenshots and visual assessment are permanently NOT worker deliverables (explicit user override).

Delivered in R5: a real fresh browser cart → two priced captured groups with own money/dates → ONE selection PUT
from a same-tick double click → a local-cache-free pre-commit payment form (own group rows, payable 3,652,100,
manual enabled) → ONE start-payment POST from a same-tick double click → manual `PendingPayment` order
`TB-20261028113839-01-bbb9bf` with 2 seller orders and 2 accepted-evidence order groups → same-key retry returning
the same checkout/payment; plus a NEW destination-failure run (old money gone, retry visible, typed name kept,
ONE projection restores the price). Source: pending-status read failure is now `unknown` and cannot reopen a
fresh pay form.
Still NOT delivered: a third group through the UI (this DEV catalog has only two real sellable products), any PSP
or paid state, coupon re-run in this slice, `unknown`-via-forced-5xx, and every release/activation claim.
Deadline: R4's 43-minute figure was an arithmetic error against its own timestamps (14 min); cumulative is
labelled UNVERIFIED and the 240-minute commitment is MISSED.

## Superseded — 2026-10-28 (TB-240-R4, PARTIAL)

The live pointer is
[`TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4/RESULT.md`](../architecture/evidence/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R4/RESULT.md)
and the task-wide TOP is the root `AGENTS.md`. Task-owned runtime Host `127.0.0.1:5190` / FE `3102` / DB
`tooba_marketplace_tb239`; usable authenticated page
`http://127.0.0.1:3102/fa/payment?checkoutId=01a18209-cd78-7000-a9b6-02dadf377e55`.
Screenshots and visual assessment are permanently NOT worker deliverables (explicit user override).
Remaining before any delivery claim: UI double-click network counters on the real controls, a fresh
pre-commit payment form in a NEW cart, and the 230-R1 failed-destination/retry functional re-run.

## Current Shipping handoff — 2026-10-28 (TB-240-R3, bounded PASS)

The live pointer is
[`TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3/RESULT.md`](../architecture/evidence/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3/RESULT.md)
and the task-wide TOP is the root `AGENTS.md`.

## Superseded — 2026-10-28 (TB-240-R2, BLOCKED)

The live pointer is
[`TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R2/RESULT.md`](../architecture/evidence/TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R2/RESULT.md)
and the task-wide TOP is the root `AGENTS.md`.

240-R2 is **BLOCKED**, not accepted. Both real sellers are now commissioned through the AccessControl-owned
Development prelude (`EnsureMarketplaceSellerAccessAsync`, existing `seller-owner` role, real
`ISellerDevContextStore` pairs, idempotent, local opt-in) and their captures publish through their own seller APIs on
the SAME `post:standard` binding; payment same-key concurrency was repaired in `PaymentDirectory` (winner PaymentId AND
AttemptId, no 500). A 3-offer mixed cart projects three PRICED owner groups (Platform 100000 / SellerA 65000 /
SellerB 49500). The mixed-owner SELECTION then fails `23505` on the EXISTING
`ux_cart_shipping_groups_cart_seller_method`: one seller with a Platform-fulfilled and a Seller-fulfilled group on one
cart using the same one-level method is structurally refused, and relaxing that needs a migration this task forbids.
Not delivered: mixed order/manual Pending, 3-group payment draft/StartPayment, coupon fact, Order-only mixed read, FE
build/screenshots, usable URL handoff. Architect decision needed: mixed-owner persistence contract (per-group owner
discriminator vs relaxed index), then re-Issue 240-R3. Nothing is activated; checked-in gates stay OFF; TB185 paused/
default-OFF/PendingAllocation. `STOP_FOR_ARCHITECT`, no automatic next task.

## Historical Shipping handoff — 2026-10-28 (TB-237-R1)

The live pointer is
[`TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1/RESULT.md`](../architecture/evidence/TB-SHIPPING-STOREFRONT-LIVE-WIRE-COMPLETION-237-R1/RESULT.md)
and the task-wide TOP is the root `AGENTS.md`.

A booted SingleStore buyer now completes read → selection `PUT` 200 → payment preview 200 → committed checkout
`01a1807a-81f0-7000-a1be-091f589a6c1b` (`TB-20261028030754-01-faee55`) on task FE `127.0.0.1:3100` / Host
`127.0.0.1:5188`. Migrations 231/232/236 are APPLIED on that verified non-production `tooba_alpha`. Still open: live
coupon trial, double-click/failed-refresh negatives, and the Marketplace/each-real-seller owner trial. Nothing is
activated for users; checked-in gates stay OFF; TB185 remains paused/default-OFF/PendingAllocation.
`STOP_FOR_ARCHITECT`, no automatic next task.

## Historical Shipping handoff — 2026-10-22

The current Shipping task/acceptance pointer is
[`SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md`](../planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md).
TB-189 accepted from source review; TB-190 issued, result pending. One active task;
no automatic next task and no TB-185 performance resumption. Older phase/task
pointers below are historical for this Shipping track, not executable next work.
Fetch current main and read the active task's exact recovery/base rules first.

Canonical repository:

```text
https://github.com/mrnikiemami-code/Tooba
```

Branch:

```text
main
```

Current Phase:

```text
P10 — Builder Acceptance + Product Source Finalization
```

Pipeline Mode:

```text
BRIDGE-WAKE-V1
Channel: tooba-main
```

Latest Accepted TMAR Checkpoint — authoritative (reconciled by TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT)

```text
Latest-Accepted-Implementation-Wave: TB-TMAR-HOST-CONFIGURATION-AMC-001-W1
Latest-Accepted-Task: TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT
Implementation-Commit: 32719977bc6408490fe5945d75dedaa5c2f7af4c
Implementation-Commit-Kind: IMPLEMENTATION_COMMIT
Result-Evidence-Docs-Stamp: bb96efab0f863d66f4978798aa7b9afde343d4f9
Result-Evidence-Docs-Stamp-Kind: RESULT_EVIDENCE_DOCS_STAMP_NOT_IMPLEMENTATION
Mode: HOST_CONFIGURATION_AMC_001_W2_CERTIFY_ONLY
Configuration-Production-File-Count: 9
Configuration-Production-Type-Count: 9
Path-Namespace-State: EXACT_Tooba.Host.Configuration
Certification-State: HOST_CONFIGURATION_AMC_CERTIFIED
Boundary-State: HOST_CONFIGURATION_PLATFORM_BOUNDARY_CERTIFIED
TrustedProxies-State: FAIL_FAST_CERTIFIED
PrimaryDomain-State: FAIL_FAST_CERTIFIED
Production-Code-Change: ZERO
Host-Persistence-State: HOST_PERSISTENCE_AMC_CERTIFIED_PRESERVED
Host-Outbox-State: HOST_OUTBOX_AMC_CERTIFIED_PRESERVED
Host-Observability-State: HOST_OBSERVABILITY_AMC_CERTIFIED_PRESERVED
Host-Messaging-State: HOST_MESSAGING_AMC_CERTIFIED_PRESERVED
Host-Health-State: HOST_HEALTH_AMC_CERTIFIED_PRESERVED
MultiTenancy-Certification-State: HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
Errors-Certification-State: HOST_ERRORS_AMC_CERTIFIED_PRESERVED
Host-Security-State: HOST_SECURITY_AMC_CERTIFIED_PRESERVED
Whole-Host-Admin-State: HOST_ADMIN_FULLY_CERTIFIED
Current-Host-Checkpoint: Configuration
Next-Host-Folder-Started: false
Stale-Current-Pointer-State: ZERO
workflowStop: USER_REVIEW_HOST_CONFIGURATION_AMC_001_W2_CERT
Next-Task: USER_REVIEW_HOST_CONFIGURATION_AMC_001_W2_CERT
Next-Task-Gate: USER_DECISION_REQUIRED_NO_AUTOMATIC_NEXT_IMPLEMENTATION_TASK
Automatic-Next-Implementation-Task: NONE
```


Current recovery authority:

```text
Use ONLY the "Latest Accepted TMAR Checkpoint — authoritative" block above and docs/architecture/tmar-current-state.json for resume/current-task decisions.
Older product/module recovery sections below are historical context unless explicitly marked CURRENT.
```


TMAR Module Recovery (Worker PASS — awaiting Architect):

```text
TB-TMAR-WALLET-GOLDEN-001 — Wallet Endpoints Ownership + MediatR CQRS + Host Error/Idempotency Cleanup
Wallet-State: COMPLETE_REFERENCE_PATTERN
Wallet-HTTP-Ownership: MODULE_ENDPOINTS
Wallet-Endpoints-State: REAL_PROJECT_PRESENT
Wallet-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Wallet-Host-Endpoints: REMOVED
Wallet-Host-Business-Authority: NONE
Wallet-Host-DbAuthority: NONE_EXCEPT_DEV_BOOTSTRAP_ALLOWLIST
Wallet-CrossModule-Boundary: CONTRACTS_ONLY
Wallet-Result-Adoption: HTTP_USE_CASES_ADOPTED
Wallet-Error-Classification: STABLE_CODES_ONLY
Wallet-Prose-Mapping: NONE
Wallet-Unexpected-Exception-Swallow: NONE
Wallet-Idempotency-Generation: IIdGenerator
Wallet-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Wallet-Architecture-Guards: ENFORCED
Wallet-Behavior-Preservation: VERIFIED

TB-TMAR-PAYMENT-GOLDEN-001 — Payment Endpoints Ownership + MediatR CQRS + Host Payment Authority Removal
Payment-State: COMPLETE_REFERENCE_PATTERN
Payment-HTTP-Ownership: MODULE_ENDPOINTS
Payment-Endpoints-State: REAL_PROJECT_PRESENT
Payment-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Payment-Host-Webhook: REMOVED
Payment-Host-StorefrontComposer: REMOVED
Payment-Host-AdminGridEngine: REMOVED
Payment-Host-Business-Authority: NONE
Payment-Reconciliation-Host-Role: SCHEDULER_ONLY
Payment-CrossModule-Boundary: CONTRACTS_ONLY
Payment-Result-Adoption: HTTP_USE_CASES_ADOPTED
Payment-Error-Classification: STABLE_CODES_ONLY
Payment-Prose-Mapping: NONE
Payment-Unexpected-Exception-Swallow: NONE
Payment-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Payment-Architecture-Guards: ENFORCED
Payment-Behavior-Preservation: VERIFIED
Recovery-Next-Task: TB-TMAR-PROMOTION-GOLDEN-001

TB-TMAR-PAYMENT-GOLDEN-001 — Payment Endpoints Ownership + MediatR CQRS + Host Payment Authority Removal
Payment-State: COMPLETE_REFERENCE_PATTERN
Payment-HTTP-Ownership: MODULE_ENDPOINTS
Payment-Endpoints-State: REAL_PROJECT_PRESENT
Payment-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Payment-Host-Webhook: REMOVED
Payment-Host-StorefrontComposer: REMOVED
Payment-Host-AdminGridEngine: REMOVED
Payment-Host-Business-Authority: NONE
Payment-Reconciliation-Host-Role: SCHEDULER_ONLY
Payment-CrossModule-Boundary: CONTRACTS_ONLY
Payment-Result-Adoption: HTTP_USE_CASES_ADOPTED
Payment-Error-Classification: STABLE_CODES_ONLY
Payment-Prose-Mapping: NONE
Payment-Unexpected-Exception-Swallow: NONE
Payment-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Payment-Architecture-Guards: ENFORCED
Payment-Behavior-Preservation: VERIFIED
Recovery-Next-Task: TB-TMAR-PROMOTION-GOLDEN-001
Support-State: COMPLETE_REFERENCE_PATTERN
Notification-State: COMPLETE_REFERENCE_PATTERN
Returns-State: COMPLETE_REFERENCE_PATTERN
Fulfillment-State: COMPLETE_REFERENCE_PATTERN
Settlement-State: COMPLETE_REFERENCE_PATTERN
Cart-State: COMPLETE_REFERENCE_PATTERN
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Frontend-Production-Changes: NONE
Next-Recommended-Task: TB-TMAR-PROMOTION-GOLDEN-001
```

Prior TMAR Cart golden (awaiting Architect / USER_CART_REVIEW_CHECKPOINT):

```text
TB-TMAR-CART-GOLDEN-001-R1 — Cart Exception/Result Semantics Closure
Cart-State: COMPLETE_REFERENCE_PATTERN
Cart-HTTP-Ownership: MODULE_ENDPOINTS
Cart-Endpoints-State: REAL_PROJECT_PRESENT
Cart-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Cart-Host-Routes: REMOVED
Cart-Host-Composer: REMOVED
Cart-Host-CatalogDbAuthority: NONE
Cart-CrossModule-Boundary: CONTRACTS_ONLY
Cart-Exception-Classification: STABLE_CODES_ONLY
Cart-Prose-Mapping: NONE
Cart-Unexpected-Exception-Swallow: NONE
Cart-Result-Adoption: HTTP_USE_CASES_ADOPTED
Cart-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Cart-Architecture-Guards: ENFORCED
Cart-Behavior-Preservation: VERIFIED
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Frontend-Production-Changes: NONE
Next-Recommended-Task: USER_CART_REVIEW_CHECKPOINT
```
Prior TMAR batch (BATCH-004 tip before Cart golden):

```text
TB-TMAR-NEXT-MODULE-BATCH-004 — Fulfillment + Returns Golden Recovery Batch
Fulfillment-State: COMPLETE_REFERENCE_PATTERN
Fulfillment-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Fulfillment-CrossModule-Boundary: CONTRACTS_ONLY
Returns-State: COMPLETE_REFERENCE_PATTERN
Returns-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Returns-CrossModule-Boundary: CONTRACTS_ONLY
Behavior-Preservation: VERIFIED
Notification-State: COMPLETE_REFERENCE_PATTERN
Support-State: COMPLETE_REFERENCE_PATTERN
Wallet-State: COMPLETE_REFERENCE_PATTERN
Payment-State: COMPLETE_REFERENCE_PATTERN
Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN
Inventory-State: COMPLETE_REFERENCE_PATTERN
Promotion-State: COMPLETE_REFERENCE_PATTERN
Tax-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State: DEFERRED_PHYSICAL_REVIEW_BY_USER
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes: NONE
Batch-State: COMPLETE
Module-Recovery-State: NEXT_REFERENCE_BATCH_004_COMPLETE
Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-005
```

Implemented Under Review:

```text
TB-P10-T022-R21 — Product Source Final Integration «پیشنهاد شگفت‌انگیز» additive-only (PromotionCampaign/AMAZING, CampaignId=null); locks 428–430; Appearance YES; Builder NO; awaiting Architect review
TB-P10-T022-R20 — Amazing Campaign Admin Workspace (list/workspace/offers/pricing); locks 422–427; Architect-ACCEPTED before R21; Appearance YES; Builder NO
TB-P10-T022-R19 — Campaign-aware Cart/Checkout quote integrity (MerchandisingCampaignId + ICampaignCartPriceAuthority); locks 417–421; Architect-ACCEPTED before R20; Appearance YES; Builder NO
TB-P10-T022-R18 — Campaign promotional pricing via AuthoredPrice QualifierKind=MerchandisingCampaign + TOOBA-CAPABILITY-MAP; locks 412–416; Architect-ACCEPTED before R19; Appearance YES; Builder NO
TB-P10-T022-R17 — Product Showcase PromotionCampaign source (پیشنهاد شگفت‌انگیز) Builder→Preview→Publish→Storefront; locks 405–411; Architect-ACCEPTED before R18; Appearance YES; Builder NO
TB-P10-T022-R16 — Amazing Offers Runtime Resolver + Real Test Data (active/future selection, availability filter, Dev seed, locks 399–404); Architect-ACCEPTED before R17; Appearance YES; Builder NO
TB-P10-T022-R15 — Merchandising Promotion Foundation (Campaign + Type AMAZING + membership + translations; promo price deferred; no Builder/Storefront); Architect-ACCEPTED before R16; Appearance YES; Builder NO
TB-P10-T022-R14 — Amazing Offers / Promotion Engine Architecture Audit (docs only; zero schema/UI/storefront code); Architect-ACCEPTED before R15; Appearance YES; Builder NO
TB-P10-T022-R13-R4 — Product Showcase Motion & Proportion Polish (calm autoplay, no sweep, proportionate cinematic, upright explorer); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R3 — Product Showcase Distinct Variant V2 (visually distinct sunny/money/cinematic/cinematic-plus/explorer + autoplay); Architect-ACCEPTED before R13-R4; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R2-R2 — Product Showcase Final Closure (Admin→exact published Landing route sunny+cinematic; real Git SHA; user-work 18ca10c9 preserved); Architect-ACCEPTED before R13-R3; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R2-R1 — Product Showcase Variant Visual Closure (real Admin Appearance/Review proof; user-work 18ca10c9 preserved); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R2 — Product Showcase Variant Expansion (sunny/سانی, money/مانی, cinematic/سینمایی, cinematic-plus/سینمایی پلاس, explorer/کاشف; Embla rails; ProductCard unchanged); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R1-R1 — Hero Slider Builder Visual Closure Repair; Architect-ACCEPTED before R13-R2; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13-R1 — Hero Slider Builder Repair (six variants, height presets, structured CTA, visible validation); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P11-T001 — Admin Completion Kickoff (Recovery SoT reconciliation + production-grade Admin gap audit); retained for later — not executed further; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R13 — Final Builder Hardening (end-to-end completeness/consistency across 10 industry Template packs); Architect-ACCEPTED after worker Result; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R12C — Industry Template Catalog Batch C (shoes, plants, beauty); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R12B — Industry Template Catalog Batch B (tile-ceramic, interior-decor, home-appliances); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R12A — Industry Template Catalog Batch A (auto-parts, building-materials, tools-hardware); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R11 — Variant Picker V2 (human design names, real component previews, live carousel/tab behavior, Review live preview); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R10 — Store Page Editor Workspace (Template Apply materialization, unified section list, drag/drop + arrow reorder, insert/delete/disable); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R9-R1 — Store Pages SSR Performance Repair (Landing latency, canonical resolver dedupe, Store+locale+page cache invalidation); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R9 — Store Pages Foundation (Home/Landing type, /landing/{slug}, Home set/restore, page SEO, sitemap, AppDataGrid); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R8-R2 — Store Preview Asset Isolation Repair (dedicated Preview-Fake content/media; zero Template Fashion asset reuse in Store fake fill); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R8-R1 — R8 Runtime Visual Closure (FE+Host evidence PNGs; article magazine-rail sample marker repair; preview-only partial-fillers fixture); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R8 — Store Preview Variant-Fidelity Repair (preview-only fake fill through real Section/Variant components); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R7 — Preview context unification (strict Sample/Store isolation, preview placeholders, dynamic locale, focal crop); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R6 — Exact Product/Category/Brand Template Catalog parity + Fashion category media fidelity; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R5 — Fashion Template Catalog Persistence (exact schema mirror + 8/15 Fashion seed + Sample purity); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R4 — Fashion Template Live Preview Pilot (real /template-preview/fashion + Admin iframe Desktop/Tablet/Mobile, isolated demo pack); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R3 — Template Selection Wireframe (device modes, seed-pack UX skeleton, distinct industry previews); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R2 — Builder Visual/Workflow Repair R2 (Landing AppDataGrid, blank Add Section, fixed wizard shell, template V2, banner/brand/workspace); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022-R1 — Builder Admin UX Repair (Orders-grid Resource Selector, section wizard, theme isolation, Story display-only); USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T022 — Builder Acceptance Candidate — Completeness Audit, Visual Consistency, Final UX/Runtime Hardening; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T021 — Variant Expansion Wave 2 + Builder visual polish + industry template fidelity; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T020 — Native StoryRail/BannerShowcase + first variant wave + template start UX; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T019 — Shared composition renderer + existing Shopeiva variants + visual Admin variant picker; USER_VISUAL_ACCEPTED=YES (Appearance); Builder USER_VISUAL_ACCEPTED=NO
TB-P10-T018 — Home+Landing composition engine foundation (shared registry, responsive contracts, Landing adapter); USER_VISUAL_ACCEPTED=YES (Appearance); Builder not visually accepted
TB-P10-T017-R5 — Section-context composition (inherit-by-default, local derived surfaces, card-area guard); USER_VISUAL_ACCEPTED=YES (Appearance accepted with T018)
TB-P10-T017-R4 — Component-level theme compliance (derived Card/Elevated/Input; shared primitives; crawler); USER_VISUAL_ACCEPTED=NO
TB-P10-T017-R3 — Storefront-wide theme coverage (route inventory + shared surfaces + crawler); USER_VISUAL_ACCEPTED=NO
TB-P10-T017-R2 — PLP/Landing product-card media well height (Tailwind aspect-[4/5] emitted); USER_VISUAL_ACCEPTED=NO
TB-P10-T017-R1 — PDP first-paint 500 repair (ThemeToggle without ThemeProvider throw); USER_VISUAL_ACCEPTED=NO
TB-P10-T017 — Final Surface Theme Visual Acceptance Refresh (four-role demo pack across Storefront pages); USER_VISUAL_ACCEPTED=NO
TB-P10-T016 — Storefront Semantic Surface Architecture (PageBackground/SectionSurface/SectionAlternate/SectionAccent, Neutral+PaletteTint, Admin four-role preview); USER_VISUAL_ACCEPTED=NO
TB-P10-T015 — Storefront Background Tint Option (Neutral/PaletteTint, curated tint tokens, Admin UX, Home/Landing inherit); USER_VISUAL_ACCEPTED=NO
TB-P10-T014 — Appearance + Landing Final Demo & Visual Acceptance Pack (seeded palettes/pages/menu, inspection guide, screenshot pack); USER_VISUAL_ACCEPTED=NO
TB-P10-T013 — Storefront Menu Management (Admin UX, Menu/MenuItem, Header/Landing integration, demo seed); USER_VISUAL_ACCEPTED=NO
TB-P10-T012 — Landing Page Composer (Admin UX, preview, publish, Home selection, storefront section renderer); USER_VISUAL_ACCEPTED=NO
TB-P10-T011 — Landing Section foundation (PageSection, approved registry, controlled sources); USER_VISUAL_ACCEPTED=NO
TB-P10-T010 — Landing Page foundation (Page model, Home selection, dynamic slug, reserved routes); USER_VISUAL_ACCEPTED=NO
TB-P10-T009-R2 — Product Card Skin Admin UX + console integrity; USER_VISUAL_ACCEPTED=NO
TB-P10-T009-R1 — Product Card Skin visual proof (persistent screenshots, Admin preview, cross-surface); USER_VISUAL_ACCEPTED=NO
TB-P10-T009 — Product Card Skin System (classic/clean/elevated/glass, Admin selection, storefront-wide); USER_VISUAL_ACCEPTED=NO
TB-P10-T008-R1 — Light/Dark visual integrity repair (canonical dark brand-emphasis + product-card surfaces); USER_VISUAL_ACCEPTED=NO
TB-P10-T008 — Storefront Light/Dark ThemeMode (Admin persist, SSR first paint, UserChoice toggle); USER_VISUAL_ACCEPTED=NO
TB-P10-T007 — Storefront token completion across Home, PDP, Shipping, and remaining brand-bound surfaces; USER_VISUAL_ACCEPTED=NO
TB-P10-T006 — Store Appearance Admin (curated palette selection, preview, persistence, cache invalidation, live Storefront apply); USER_VISUAL_ACCEPTED=NO
TB-P10-T005-R1 — Appearance foundation runtime + recovery repair (live /appearance, migration, Last Architect-accepted = TB-P10-T005-R1); USER_VISUAL_ACCEPTED=NO
TB-P10-T005 — Storefront Appearance Foundation (canonical Store appearance, semantic tokens, curated palette, future page extension contracts); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1-R4 — Storefront auth/cart network churn repair (canonical session cache, one merge per login, no anonymous 401 storm); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1-R3 — Storefront account identity + logout cart persistence + exact conversion boundary; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1-R2 — Recipient visual verification gate (Payment + Customer + Admin browser proof); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1-R1 — Recipient name canonicalization repair (explicit First/Last win; legacy RecipientName fallback only); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24-R1 — Authenticated storefront continuity repair (cart through login/shipping + canonical account header + FirstName/LastName); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R24 — Final identity + limits + atomic checkout integration gate; USER_VISUAL_ACCEPTED=YES
TB-P10-T004-R23 — Max open unpaid orders + reservation churn anti-abuse + Admin Settings; USER_VISUAL_ACCEPTED=YES
TB-P10-T004-R22-R1 — First canonical Login + checkout identity policy + cart merge; USER_VISUAL_ACCEPTED=YES
TB-P10-T004-R21-R1 — Accepted pending-order UX + atomic checkout commit gate; USER_VISUAL_ACCEPTED=YES
TB-P10-T004-R20-R1 — Visual Gate Repair (long-hold countdown + Admin history localization); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R20 — Reservation UX Visual Runtime Gate; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R19 — Reservation Lifecycle Final Integration Gate; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R18 — Reservation Policy Admin UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R17 — Admin Reservation Cycle Audit UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R16 — Customer Pending-Payment UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R15 — Reservation Cycle Foundation; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R14 — Paid Checkout Re-Initiation Guard; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R13 — Committed Checkout Ownership + Active Cart Rotation; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R12 — P10 Final Lifecycle Regression + Production Readiness; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R11 — Paid Projection Financial Consistency (seller totals + StoreShipping); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R10 — Unpaid Order Expiry + Hold Policy Settings UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R9 — Cart lifetime vs Inventory hold separation; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R8 — Admin Orders + Payments Supply UX; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R7 — Order Supply Capability Foundation; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R6 — Historical Paid/Manual Orders Inventory Recovery; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R5 — Manual Payment Review Reservation Lifecycle; USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R4 — Payment Result Ownership + Polling Repair (stop 401 after cart finalize); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R3 — Payment Completion Runtime Proof (A–J + Converted-cart payment GetAsync fix); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R2 — Storefront Payment Completion Repair (sandbox simulator + card-to-card proof/tracking + cart finalization); USER_VISUAL_ACCEPTED=NO
TB-P10-T004-R1 — Authenticated Checkout Runtime Proof (real session E2E + payment actor ownership fix); USER_VISUAL_ACCEPTED=NO
TB-P10-T004 — Storefront Checkout Final Gate (Cart→Shipping→Payment→Order E2E hardening); under repair via R1; USER_VISUAL_ACCEPTED=NO
TB-P10-T003-R1 — Payment shipping StoreShipping allocation (no first-seller shortcut); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P10-T003 — Storefront Payment (Store-enabled methods, no fake card form, authoritative payable+initiate); USER_VISUAL_ACCEPTED=NO
TB-P10-T002-R1 — Storefront Shipping multi-seller runtime gate (Scenario E real 2+ sellers); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P10-T002 — Storefront Shipping (addresses, Store-enabled methods, delivery minimum, /payment handoff); USER_VISUAL_ACCEPTED=NO
TB-P10-T001 — Storefront Cart foundation (real ATC, mini-cart, /cart recommendations); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T022-R4 — Paid-order reservation REAL runtime proof (TTL/expiry/dispatch/cancel-restore/decimal); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T022-R3 — Paid-order reservation commit clears cart TTL (ExpiresAt=null); USER_VISUAL_ACCEPTED=NO
TB-P09-T022-R2 — Customer Tracking UI Proof (owned session + consolidated package primary rendering); USER_VISUAL_ACCEPTED=NO
TB-P09-T022-R1 — Consolidated Package visual runtime repair (FE Admin 500 readiness + real browser smoke); under repair via R2; USER_VISUAL_ACCEPTED=NO
TB-P09-T022 — Consolidated Package admin/runtime completion (central package lifecycle + history); under repair via R2; USER_VISUAL_ACCEPTED=NO
TB-P09-T021-R1 — Consolidated Package customer tracking access/projection repair; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T021 — Consolidated Package foundation (multi-seller master package orchestration); under repair via R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T020-R2 — Cancel/Restore reservation rebinding + localized inventory error UX; USER_VISUAL_ACCEPTED=NO
TB-P09-T020-R1 — P09 final-gate repair: partial-dispatch composed status + multi-seller runtime; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T020 — P09 final gate: partial-dispatch remainder + multi-shipment continuation; under repair via R2; USER_VISUAL_ACCEPTED=NO
TB-P09-T019 — Admin returns & refunds queue finalization; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T018 — Admin fulfillment queue finalization; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T017 — Orders menu final gate; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T016 — Whole-order cancel until first dispatched quantity; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T015 — Quantity & invoice final gate; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T014-R1 — Invoice header LineCount vs TotalQuantity semantic repair; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T014 — Quantity & financial lock completion (lock registry, UoM admin, invoice header aggregates); under repair via R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T013-R1 — Offer/Fulfillment EF history replay-safe + Floor/Ceiling/Nearest runtime proof; USER_VISUAL_ACCEPTED=NO
TB-P09-T013 — Quantity decimal foundation (UoM, product policy, offer min/max, global rounding, numeric(18,6)); under repair via R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T012 — Reference-locked fulfillment UX (capability-driven selection/bulk/kebab/shipment); USER_VISUAL_ACCEPTED=NO
TB-P09-T011 — Orders Fulfillment UX Regression Repair (cancel precedence, grid refresh, bulk toolbar, single-selection, row kebab, return display dedup); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T010 — Fulfillment Action Scope & Sequence Repair (whole-order pack/process hide, pack-after-processing, exact pack_selected, row kebab); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T009-R1 — Cancelled-order restore financial gate (block restore after completed seller payout/settlement); Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T009 — Order Corrective Actions (rollback matrix, payment/shipment/cancel restore, whole-order dedupe); Architect-accepted after R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T008 — Shipping & Delivery Operations Center — Cross-Order Work Queue; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T007 — Order Financial History + Scope-Aware Operational History; Architect-accepted; USER_VISUAL_ACCEPTED=NO
TB-P09-T006-R1 — Offer Return Policy + Shipping Provider Forms Final Runtime Proof; USER_VISUAL_ACCEPTED=NO
TB-P09-T006 — Offer return policy + dynamic shipping provider forms; Architect-accepted after R1; USER_VISUAL_ACCEPTED=NO
TB-P09-T005-R1 — Seller-Scoped Fulfillment Final Proof runtime matrix; USER_VISUAL_ACCEPTED=NO
TB-P09-T005 — Seller-scoped line/quantity fulfillment ops, reverse rules, return deadline UI; USER_VISUAL_ACCEPTED=NO
TB-P09-T004 — Orders Operational UX Foundation (grid kebab ops, اقلام و ارسال shell, shipment modal; USER_VISUAL_ACCEPTED=NO)
TB-P09-T003-R3 — Admin Orders Repair R3 (production-safe configurable manual/card-to-card payment; USER_VISUAL_ACCEPTED=NO)
TB-P09-T003-R2 — Admin Orders Repair R2 (manual/card-to-card confirm, fresh order E2E, list return/refund visibility; USER_VISUAL_ACCEPTED=NO)
TB-P09-T003-R1 — Admin Orders Repair (ops matrix/menu portal, grid maxWidth, note delete rule, clean E2E; USER_VISUAL_ACCEPTED=NO)
TB-P09-T003 — Admin Orders Final Operational Gate (View-only nav + E2E verify + defect-only shipment fixes; USER_VISUAL_ACCEPTED=NO)
TB-P09-T002-R1 — Order Operational History Repair (human actor resolution; USER_VISUAL_ACCEPTED=NO)
TB-P09-T002 — Order Detail Operational Completeness (notes, operational history, invoice/receipt; Architect-accepted)
TB-P09-T001-R2 — Marketplace settlement event-path proof (MassTransit/outbox handlers; USER_VISUAL_ACCEPTED=NO)
TB-P09-T001-R1 — Order Operations Foundation Repair (domain cancel guard + real post-settlement return runtime; USER_VISUAL_ACCEPTED=NO)
TB-P09-T001 — Order Operations Foundation (contextual admin actions, return eligibility SoT, settlement-safe returns; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R5 — P08 repair (locale backlink, history clarity, fonts/video/category UX; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R4 — P08 repair (article locale identity, history pager, CKEditor fonts, DAM contentTypePrefix video; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R3 — P08 final visual/functional repair (DB languages, loading feedback, Full Edit density, author readiness, CKEditor font/DAM; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R2 — P08 final visual repair (Full Edit, author combobox, AppCategoryTree picker, CKEditor CMS toolbar, history wording; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016-R1 — Content demo mojibake/???? label cleanup (repair of T016 visual cleanliness; USER_VISUAL_ACCEPTED=NO)
TB-P08-T016 — P08 Content Final Gate (technical/worker gate; USER_VISUAL_ACCEPTED=NO)
TB-P08-T015 — Article comments moderation, contextual help, wording cleanup, workspace polish — Architect-accepted
TB-P08-T014 — Article publication readiness, draft preview, publish lifecycle, history, Jalali schedule — Architect-accepted
TB-P08-T013 — Content taxonomy two-level categories + ContentTags — Architect-accepted
TB-P08-T012-R1 — Article TipTap → CKEditor 5 + DAM (repair of T012 editor) — Architect-accepted
TB-P08-T012 — Article workspace editor, media/SEO/category save repairs (under repair via R1; editor replaced)
TB-P08-T011 — Article language tabs, draft-first create, author picker, locale policy — Architect-accepted
TB-P08-T010-R1 — P08 Final Gate Repair (seed scope, public Content APIs, locale 308) — Architect-accepted
TB-P08-T010 — Content Visual Final Gate (USER_VISUAL_ACCEPTED=NO)
TB-P08-T009-R2 — Content Authorization Fail-Closed Repair
TB-P08-T009-R1 — Content Permission Enforcement Repair (backend content.* authorization)
TB-P08-T009 — Content Integration Gate (under repair via R2)
TB-P08-T008 — Public Content Taxonomy + Author routes + storefront home locale consistency
TB-P08-T007-R2 — Article action dialog completion (publish/unpublish canonical Dialog)
TB-P08-T007-R1 — Article destructive UX repair (canonical Dialog replaces window.confirm)
TB-P08-T007 — Article CRUD UX hardening (create route, VIEW/EDIT, delete/archive)
TB-P08-T006 — Content SEO + Publication + Locale Routing (canonical public blog, scheduling, sitemap)
TB-P08-T005 — Article Media DAM integration (inline/gallery/featured/SEO via existing DAM)
TB-P08-T004 — Article Editor Workspace (language-first create, TipTap, tabs, locale lock)
TB-P08-T003 — Content Author management (public profiles, DAM, Article relation)
TB-P08-T002 — Article Category Taxonomy (Content-owned, language-aware AppCategoryTree)
TB-P08-T001 — under repair (parent; NOT accepted)
TB-P08-T001-R2 — Language reference integrity (Code/UrlPrefix lock after Content use)
TB-P08-T001-R1 — Persisted Language registry repair (DB-backed localization.languages)
TB-P07-T043 — Order Detail visual fidelity polish (accepted baseline)
```

USER_VISUAL_ACCEPTED:

```text
YES
```

Worker Next State:

```text
IDLE — waits for Bridge Task (no invented next task; do NOT invent TB-P10-T023; do NOT start TB-P11-T002; TB-P11-T001 retained for later; Builder USER_VISUAL_ACCEPTED=NO)
```

Issued but not accepted:

```text
TB-P06-T010-R1 = AWAITING_ARCHITECT_ACCEPT
TB-P06-T022 = ACCEPTED
TB-P06-T023 = CLOSED_BY_REPAIR
TB-P06-T023-R1 = ACCEPTED
TB-P06-T024 = REPAIRED_BY_TB-P06-T024-R1
TB-P06-T024-R1 = FUNCTIONALLY_COMPLETE
TB-P06-T024-R2 = ACCEPTED
TB-P06-T025 = ACCEPTED
TB-P06-T026 = REPAIRED_BY_TB-P06-T026-R1
TB-P06-T026-R1 = ACCEPTED
TB-P06-T027 = ACCEPTED
TB-P06-T028 = REPAIRED_BY_TB-P06-T028-R1
TB-P06-T028-R1 = ACCEPTED
TB-P06-T029 = ACCEPTED
COMMERCIAL_READINESS_GATE = PASSED
TB-P07-T001 = SUPERSEDED
TB-P07-T001-R1 = SUPERSEDED
TB-P07-T001-R2 = SUPERSEDED
TB-P07-T001-R3 = SUPERSEDED
TB-P07-T001-R4 = SUPERSEDED
TB-P07-T001-R5 = ACCEPTED
TB-P07-T002 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R1 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R2 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T002-R3 = AWAITING_ARCHITECT_ACCEPT
TB-P07-T004 = ACCEPTED
TB-P07-T005 = ACCEPTED
TB-P07-T005-R1 = ACCEPTED
TB-P07-T006 = AWAITING_ARCHITECT_ACCEPT
CATEGORY_TRANSLATIONS = LIVE
CATEGORY_SLUG_HISTORY = LIVE
CATEGORY_ROUTE_RESOLVER = LIVE
CATEGORY_TREE_CONTRACT = LIVE
CATEGORY_ADMIN_UI = GENERAL_AND_TRANSLATIONS_EDITOR
USER_VISUAL_ACCEPTED = NO
SERVER_GRID_PAGE_ONLY_ENRICH = LIVE
SERVER_GRID_FILTER_UX = LIVE
SERVER_GRID_COMMUNITY_FEATURES = LIVE
CURRENT_FOCUS = CATEGORY_ADMIN_UI
CANONICAL_GRID = APP_OWNED_AG_GRID_COMMUNITY_WRAPPER
AG_GRID_ENTERPRISE = FORBIDDEN
SERVER_GRID_QUERY_CONTRACT = LIVE
FIRST_REAL_ADMIN_INTEGRATION = LIVE
CATEGORY_ATTRIBUTE_SCHEMA = LIVE
CATEGORY_ATTRIBUTE_INHERITANCE = LIVE
PRODUCT_TYPED_ATTRIBUTES = LIVE
PRODUCT_VARIANT_AXES = LIVE
VARIANT_COMBINATION_FOUNDATION = LIVE
PRODUCT_SEO = LIVE
PRODUCT_PUBLISHING = LIVE
FULL_VARIANT_MATRIX = DEFERRED
FACETED_SEARCH_INTEGRATION = DEFERRED
SUPPORT_BACKEND = LIVE
CUSTOMER_TICKETS = LIVE
SELLER_TICKETS = LIVE
ADMIN_SUPPORT = LIVE
SUPPORT_NOTIFICATIONS = LIVE
SUPPORT_ACCESS_CONTROL = LIVE
SUPPORT_DEMO_SEED = LIVE_DEV_ONLY
SUPPORT_USER_PREVIEW = READY
WALLET_LEDGER = LIVE
GIFTCARD = LIVE
CUSTOMER_WALLET_UI = LIVE
ADMIN_WALLET_UI = LIVE
WALLET_DEMO_SEED = LIVE_DEV_ONLY
WALLET_BROWSER_PROOF = LIVE
WALLET_USER_PREVIEW = READY
WALLET_CHECKOUT = LIVE
WALLET_CHECKOUT_FULL_PAYMENT = LIVE
REFUND_TO_WALLET = LIVE
WALLET_MIXED_TENDER = DEFERRED
WALLET_CHECKOUT_BROWSER_PROOF = LIVE
REFUND_TO_WALLET_BROWSER_PROOF = LIVE
WALLET_CHECKOUT_USER_PREVIEW = READY
CUSTOMER_SETTINGS = LIVE
SELLER_SETTINGS = LIVE
ADMIN_PROFILE_SETTINGS = LIVE
REAL_PREFERENCES = PARTIAL
NOTIFICATION_PREFERENCES = DEFERRED
SECURITY_SETTINGS = DEFERRED
SETTINGS_DEMO_SEED = LIVE_DEV_ONLY
SETTINGS_USER_PREVIEW = READY
ACCESS_CONTROL_CENTER = LIVE
ACCESS_CONTROL_DEMO_SEED = LIVE_DEV_ONLY
ACCESS_CONTROL_BROWSER_PROOF = LIVE
ACCESS_CONTROL_USER_PREVIEW = READY
PRODUCT_SALE_FLOW = LIVE
SELLER_PRODUCT_MANAGEMENT = LIVE
SELLER_OFFER_MANAGEMENT = LIVE
PRICING_PATH = LIVE
INVENTORY_PATH = LIVE
CART_CHECKOUT_PAYMENT = LIVE
SELLER_ORDER_FULFILLMENT = LIVE
CUSTOMER_ORDER_TRACKING = LIVE
ADVANCED_VARIANT_ARCHITECTURE = DEFERRED
SELLABLE_PRODUCT_FLOW_LIVE = YES
SELLABLE_DEMO = YES
PRODUCTION_PAYMENT_FOUNDATION_READY = YES
REAL_PSP_PROVIDER_CONFIGURATION_REQUIRED = YES
REAL_BANK_PAYMENT_PROVEN = NO
PRODUCTION_GO_LIVE_READY = NO
PUBLIC_LOCALE_ROUTING = PREFIXED
MULTILINGUAL_SEO = LIVE
HREFLANG = REAL_VARIANTS_ONLY
UNPREFIXED_PUBLIC_URLS = REDIRECTED
PAGE_COMPOSITION = LOCALE_ROUTE_COMPATIBLE
STORY_LIVE = WITH_EXACT_SHOPEIVA_UI
SHARED_STORY_MANAGEMENT = LIVE
COMMERCIAL_PANEL_WAVE1 = LIVE
COMMERCIAL_PANEL_WAVE2 = LIVE
SELLER_PROMOTIONS = LIVE
SELLER_REVIEWS = LIVE
ADMIN_PROMOTIONS = LIVE
ADMIN_REVIEWS = LIVE
TRANSACTIONAL_NOTIFICATIONS_LIVE = YES
NOTIFICATION_BACKEND = LIVE
CUSTOMER_NOTIFICATION_UI = LIVE
SELLER_NOTIFICATION_UI = LIVE
NOTIFICATION_UNREAD = LIVE
NOTIFICATION_DEEP_LINKS = LIVE
SPICEDB_AUTHORIZATION = LIVE
ACCESS_CONTROL_CENTER = LIVE
ACCESS_CONTROL_DEMO_SEED = LIVE_DEV_ONLY
ACCESS_CONTROL_BROWSER_PROOF = LIVE
ACCESS_CONTROL_USER_PREVIEW = READY
ADMIN_ROLE_MANAGEMENT = LIVE
SELLER_ROLE_MANAGEMENT = LIVE
SELLER_PERMISSION_CEILING = LIVE
USER_ROLE_ASSIGNMENT = LIVE
UI_CAPABILITY_PROJECTION = LIVE
RESOURCE_SCOPE_FOUNDATION = LIVE
REAL_DATA_SCOPE_SELECTORS = LIVE
CATEGORY_SCOPED_ORDER_POLICY = LIVE_FOUNDATION
CATEGORY_SCOPED_ORDER_ACCESS = LIVE
CATEGORY_SCOPED_ORDER_FILTERING = LIVE
NOTIFICATION_VISUAL_PARITY = PROVEN
REALTIME_NOTIFICATIONS = DEFERRED
FAKE_NOTIFICATIONS = FORBIDDEN
PANEL_NAVIGATION = HONEST_LIVE_ONLY
PRESENTATION_CRITICAL_FAKE_ACTIONS = FORBIDDEN
BLOG_CONTENT = LIVE
STOREFRONT_UI = LIVE_AUDITED
CUSTOMER_PANEL = LIVE_AUDITED
SELLER_PANEL = LIVE_AUDITED
ADMIN_PANEL = LIVE_AUDITED
STOREFRONT_UI = LIVE_AUDITED
CUSTOMER_UI = LIVE_AUDITED
SELLER_UI = LIVE_AUDITED
ADMIN_UI = LIVE_AUDITED
MULTILINGUAL_UI = AUDITED
RTL_LTR = AUDITED
MULTILINGUAL_SEO = AUDITED
VISUAL_CONTRACT = SHOPEIVA_LOCKED
NEW_UI_RULE = SOURCE_DERIVED_NATIVE_FIT
COMMERCIAL_UI_READINESS = AUDITED
FAKE_DATA_ACTION_AUDIT = AUDITED
CSS_JS_MOTION_PARITY = AUDITED
SETTLEMENT_BACKEND = LIVE
PAYOUT_BACKEND = LIVE_FOUNDATION
SETTLEMENT_SELLER_UI = LIVE
SETTLEMENT_ADMIN_UI = LIVE
SINGLE_STORE_SETTLEMENT = NOT_APPLICABLE
REFUNDS_BACKEND = LIVE_FOUNDATION
FULFILLMENT_BACKEND = LIVE
FULFILLMENT_CUSTOMER_UI = LIVE
FULFILLMENT_SELLER_UI = LIVE
FULFILLMENT_ADMIN_UI = LIVE
RETURNS_BACKEND = LIVE
RETURNS_CUSTOMER_UI = LIVE
RETURNS_SELLER_UI = LIVE
RETURNS_ADMIN_UI = LIVE
INVENTORY_RESTOCK = LIVE
UI_PRIORITY = BACKEND_TO_SHOPEIVA_LIVE_INTEGRATION
VISUAL_CONTRACT = SHOPEIVA_LOCKED
AUTH_SESSION_STORAGE = HTTPONLY_COOKIE_SERVER_SIDE_PROPAGATION
PRODUCTION_OTP = PROVIDER_BACKED_FAIL_CLOSED
AUTHENTICATION = BEARER_SESSION_ID
AUTHORIZATION = SPICEDB_REBAC
MESSAGING_TRANSPORT = MASSTRANSIT_POSTGRESQL_SQL_TRANSPORT
HOME_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PDP_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PRODUCT_FULLY_READY = NO
TB-P05-GATE = ACCEPTED
TB-P02-T002 = ACCEPTED
TB-P02-T003 = ACCEPTED
TB-P02-T004 = ACCEPTED
TB-P02-T005 = ACCEPTED
TB-P02-GATE = ACCEPTED
TB-P03-T001 = ACCEPTED
TB-P03-T002 = ACCEPTED
TB-P03-T003 = ACCEPTED
TB-P03-T004 = ACCEPTED
TB-P03-T005 = ACCEPTED
TB-P03-T006 = ACCEPTED
TB-P03-T008 = ACCEPTED
TB-P03-T009 = ACCEPTED
TB-P03-GATE = ACCEPTED
TB-P04-T001 = ACCEPTED
TB-P04-T002 = ACCEPTED
TB-P04-T003 = ACCEPTED
TB-P04-T004 = ACCEPTED
TB-P04-T005 = ACCEPTED
TB-P04-T006 = ACCEPTED
TB-P04-T007 = ACCEPTED
TB-P04-T008 = ACCEPTED
TB-P04-T009 = ACCEPTED
TB-P04-T010 = ACCEPTED
TB-P04-GATE = ACCEPTED
TB-P05-T001 = ACCEPTED
TB-P05-T001/T002/T003/T004/T005/T006/T007/T008 = ACCEPTED
TB-P05-T009 = ACCEPTED
TB-P05-T009-REPAIR-01 = ACCEPTED
TB-P05-GOV-MIGRATION-BRIDGE-V2 = ACCEPTED
TB-P05-T010 Bridge-V2 = ACCEPTED
TB-P05-T011 Bridge-V2 = ACCEPTED
TB-P05-T012 Bridge-V2 = ACCEPTED
TB-P05-T013 Bridge-V2 = ACCEPTED
TB-P05-T014 = ACCEPTED
TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1 = ACCEPTED
TB-P05-T015 = ACCEPTED
TB-P05-T015-R1 = ACCEPTED
TB-P05-T016 = ACCEPTED
TB-P05-T016-R1 = ACCEPTED
TB-P05-T017 = ACCEPTED
TB-P05-T017-R1 = ACCEPTED
TB-P05-T017-UNBLOCK-01 = ACCEPTED
TB-P05-T018 = ACCEPTED
TB-P05-T018-UNBLOCK-01 = ACCEPTED
TB-P05-T019 = ACCEPTED
TB-P05-T020 = ACCEPTED
TB-P05-T021 = ACCEPTED
TB-P05-T022 = ACCEPTED
TB-P05-T023 = ACCEPTED
TB-P05-T024 = ACCEPTED
TB-P05-T025 = ACCEPTED
P00 = COMPLETE
P01 = COMPLETE
P02 = COMPLETE
P03 = COMPLETE
P04 = COMPLETE
P05 = COMPLETE
P06 = COMPLETE
P07 = IN_PROGRESS
TB-P06-T001 = ACCEPTED
SpiceDB Authorization Foundation = COMPLETE
Party / Organization / Membership Foundation = COMPLETE
Session / Token / Credential Lifecycle = COMPLETE
Authentication HTTP Boundary = COMPLETE
Catalog Product & Variant Foundation = COMPLETE
Seller Offer & Listing Foundation = COMPLETE
Pricing Foundation = COMPLETE
Inventory Foundation = COMPLETE
Cart Foundation = COMPLETE
Checkout & Order Foundation = COMPLETE
Tax Calculation Foundation = COMPLETE
Payment Foundation = COMPLETE
Promotion & Discount Foundation = COMPLETE
P03 Commerce Core Gate = COMPLETE
P04 Experience Foundation = COMPLETE (TB-P04-GATE ACCEPTED)
P05 Operational Surface Integration = COMPLETE (TB-P05-GATE ACCEPTED)
P06 Core API Integration / Operational Hardening = IN_PROGRESS
TB-P05-T010 legacy transport version = RETIRED / NOT EXECUTED
Identity & Authentication Foundation = COMPLETE
```

## Recovered Architect procedure

Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Then read:

```text
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
```

Then determine from the repository and Bridge, not from chat memory:

- current phase;
- last accepted task;
- issued-but-unaccepted task;
- blockers;
- locked / confirmed requirements;
- unresolved decisions;
- exact resume rule.

Never invent the next task from memory. Execute only a Task actually dispatched
by Bridge on channel `tooba-main`.

## Confirmed (not implemented)

- Modular Monolith with microservice-readiness rules.
- Commercial multilingual e-commerce; SEO non-negotiable; production-grade UI/UX.
- Locale != Market != Currency.
- SpiceDB authorization direction; Keycloak-extensible identity without Keycloak coupling.
- Catalog Product vs Seller Offer must be analyzed, not prematurely merged.
- Purchased template `shopeiva.zip` is not in the repository; Architect-verified archive facts are recorded in `docs/architecture/00-technical-inventory.md`.

## Resume rule

1. fetch origin and compare HEAD with origin/main;
2. inspect working tree; do not destroy unknown work;
3. read Project State / Roadmap / Pipeline docs;
4. recover latest accepted/active task from the repository and Bridge;
5. follow `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1`; Worker is normally IDLE/OFFLINE between Tasks; External Watchdog sends `BRIDGE-WAKE` when a Pending Task appears; no continuous polling while idle; one Worker has one active Task; Worker PASS is not Architect ACCEPT; `SYSTEM-BRIDGE-ALERT` is not a Result and must not be emitted merely because the Worker is offline between Tasks;
6. TB-P05-GATE is Architect-accepted. P05 = COMPLETE. P06 = COMPLETE. P07 = IN_PROGRESS. TB-P06-T029 = ACCEPTED. COMMERCIAL_READINESS_GATE = PASSED. TB-P07-T001/R1/R2/R3/R4 = SUPERSEDED. TB-P07-T001-R5 = AWAITING_ARCHITECT_ACCEPT — Admin+Seller UI/UX polish; `CURRENT_FOCUS` = UI_UX_ONLY; `ADMIN_UI` = PRIORITY_1; `SELLER_UI` = PRIORITY_2; `BACKEND_FEATURE_EXPANSION` = FROZEN; T001 catalog/variant foundation kept. `FULL_VARIANT_MATRIX` = DEFERRED; `FACETED_SEARCH_INTEGRATION` = DEFERRED; `VISUAL_CONTRACT` = SHOPEIVA_LOCKED — not `USER_VISUAL_ACCEPTED` / `PRODUCT_FULLY_READY` / `PRODUCTION_GO_LIVE_READY` / `SELLER_PANEL_COMPLETE` / `FULL_VARIANT_MATRIX_LIVE`. Last implementation: TB-P07-T020 Product Admin visual polish (`docs/evidence/TB-P07-T020/`). Runtimes: Host `:5088`, FE `:3000`, Shopeiva `:3001`. HOME/PDP visual review remains OPEN_FOR_USER_FEEDBACK.
7. P00 architecture docs remain `docs/architecture/00` through `27`. Bootstrap layout: `docs/architecture/28-platform-foundation-bootstrap.md`. Observability/error foundation: `docs/architecture/29-observability-error-foundation.md`. Tenant/edition/database foundation: `docs/architecture/30-tenant-edition-database-foundation.md`. PostgreSQL persistence foundation: `docs/architecture/31-postgresql-persistence-foundation.md`. Persian documentation standard: `docs/architecture/32-persian-code-documentation-standard.md`. Outbox/events/background foundation: `docs/architecture/33-outbox-domain-events-background-foundation.md`. MassTransit PostgreSQL SQL Transport: `docs/architecture/34-masstransit-postgresql-sql-transport.md`. Cache abstraction foundation: `docs/architecture/35-cache-abstraction-foundation.md`. Module composition and boundary enforcement: `docs/architecture/36-module-composition-boundary-enforcement.md`. P01 gate evidence: `docs/evidence/TB-P01-GATE.md`. Identity authentication foundation: `docs/architecture/37-identity-authentication-foundation.md`. SpiceDB authorization foundation: `docs/architecture/38-spicedb-authorization-foundation.md`. Party organization membership foundation: `docs/architecture/39-party-organization-membership-foundation.md`. Session/token/credential lifecycle: `docs/architecture/40-session-token-credential-lifecycle.md`. Authentication HTTP boundary: `docs/architecture/41-authentication-http-boundary.md`. Catalog product/variant foundation: `docs/architecture/42-catalog-product-variant-foundation.md`. Seller offer/listing foundation: `docs/architecture/43-seller-offer-listing-foundation.md`. Pricing foundation: `docs/architecture/44-pricing-foundation.md`. Deep Shopeiva Study and Professional Data Grid remain mandatory before serious UI.


## Current focus (TB-P07-T042)

```text
Order Detail + Marketplace Finance UX — admin order detail finance breakdown, receipts grid, finance nav group
USER_VISUAL_ACCEPTED=NO
Worker next = IDLE / waits for Bridge Task (no invented next task)
Architect note: commit 9d350a9a = user-directed orders/products grid polish (outside architect task scope)
```

Final HEAD/origin (TB-P07-T042):

```text
babdb350ed580e04257814e8270ed1907ce755e4
```

## TMAR HOST EVACUATION — CANONICAL RECOVERY OVERRIDE

Marker: `TMAR-HOST-EVACUATION-V1`

Canonical protocol:
`docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`

Current method:
- Finish active Fulfillment Host evacuation before Fulfillment structure certification.
- Then traverse `src/backend/Host/Tooba.Host` folder-by-folder/file-by-file in repository order.
- Read every Host production file completely and create a member-level Content Disposition Map.
- A file may split across multiple module owners.
- Never delete a Host production file before every live responsibility is rehomed and parity is proven.
- If a destination module lacks proper Endpoints/CQRS/MediatR/validation/foldering/contracts boundaries, repair that destination before completing the Host-file evacuation.
- Tests/guards are focused proof after ownership repair; they are not the navigation strategy.
- Final target: Host = thin platform/composition shell only.

Recent accepted recovery facts:
- Settlement ARCH-COMPLETE-002 STRUCTURE_CERTIFIED at `54b1c8ff1f6e9214a5b5c16b6103f0285bd2a37e`; SoT stamp `01d15f3cb1ad38f0e91ed65e990e32c4f9d19876`.
- Fulfillment audit R1 corrected inventory to 15 endpoint requests = 10 validator-required + 5 no-validator-required.
- Fulfillment pre-cert repair accepted at `16062d45bde71476da35f9e20622f1b6b5637fa8`; ten transport validators are present.
- `FulfillmentReturnsGridAliases.cs` was syntax-only alias residue with zero production consumers; its underlying canonical types were not deleted.
- Fulfillment Host evacuation COMPLETE at task `TB-TMAR-FULFILLMENT-HOST-EVACUATION-001`: all three Host-specific authorizers rehomed to `Tooba.Fulfillment.Endpoints` after a full Content Disposition Map; `HOST_FULFILLMENT_SPECIFIC_FILES = ZERO`; generic Host seams `HostAdminPanelAccess`/`HostSellerPanelAccess`/`HostPlatformEffectiveAccessReader`; evidence `docs/evidence/TB-TMAR-FULFILLMENT-HOST-EVACUATION-001/fulfillment-host-evacuation.md`.
- Intended next task: `TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001`.

After Fulfillment, do NOT automatically continue by uncertified-module list. Start Host traversal with AccessControl, then AddressBook, then subsequent Host folders in repository order.
