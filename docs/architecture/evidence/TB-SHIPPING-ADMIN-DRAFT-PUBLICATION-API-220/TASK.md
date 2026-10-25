PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220
Parent-Task: TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4fc9ccac044aa2a084a833172b645e6b0191f6f8
Mode: BOUNDED_AUTHORIZED_EXISTING_DRAFT_PUBLICATION_HTTP_ACTION
HARD35 minutes / STOP new implementation23 / final12 focused proof+docs+commit.
One correction round max. Concrete API delivery, no generic architecture audit or old-suite replay.

READ TOP AGENTS + canonical recovery +219-R1/ARCHITECT-REVIEW.md + applicable backend/Contracts skill.
219/R1 accepted bounded;214 capture,216 quote,218 composer CLOSED. No new price/calendar/publication store.

DELIVER A REAL PANEL API FOR EXISTING DRAFTS
Under BOTH existing admin and seller authoring route families add:
GET /shipping-services/authoring-drafts/{draftId:guid}/publication
POST /shipping-services/authoring-drafts/{draftId:guid}/publication
POST /shipping-services/authoring-drafts/{draftId:guid}/publication/withdraw
Use actual existing /v1/admin and /v1/seller groups, no Host business logic.

Thin delegates: existing owner authorizer FIRST, correct View for GET and Manage for POSTs,
THEN existing ShippingEndpointBody, THEN ISender typed MediatR query/commands.
Same existing owner/edition/seller grant semantics; no new permission catalog or fake owner/auth client.
No tenant/seller/owner/binding/catalog/price accepted from request body. Route supplies only draft identity.
Unknown/foreign draft is safe authoring.draft_not_found through full scoped directory first.

Contracts-owned request/view DTOs, Application typed query/command handlers+validators, validators registered
in the existing coverage/DI mechanism. Stable error resources via EXISTING module IErrorResourceSet:
FA/EN and exact statuses (including selected_capture.conflict =>409, not generic400/500).
Do not guess resource file paths: use rg in actual repository; no generic global error policy rewrite.
DTOs must distinguish MISSING expectedSelectionVersion from explicit0 (required nullable shape+validator or
existing required-field convention); no accidental default-zero publish from an omitted field.
Existing save/editor-document behavior unchanged; NO implicit publish on normal Save.

GET
Read owned draft to derive binding; read219 selected lookup with trusted mapped owner+binding.
Return draftId/currentDraftRevision, service/option, explicit Absent/Cleared/Selected state,
selectionVersion (0 only for absent), selected snapshot/draft/source revision/format if selected,
and whether THIS draft's current saved revision equals the selected source (data equality, not readiness).
No raw Domain/JSON capture payload leaks. Do not hide a selected older revision or replace it with latest.
Map unavailable/corrupt read to explicit stable refusal, never Selected.

POST PUBLICATION
Body: ExpectedRevision (positive saved draft token) + ExpectedSelectionVersion (>=0,0=absent).
1 scoped draft read, foreign/missing/stale draft rejected before capture;
2 read current pointer and reject a KNOWN stale expected-selection version before any capture write;
3 call EXISTING CaptureRevisionAsync(owner,draftId,ExpectedRevision) ONCE;
4 call EXISTING219 SelectAsync(owner, VERIFIED capture binding, exact draft/revision,
ExpectedSelectionVersion) ONCE. CAS remains final authority for a race after step2.
5 Return success/provenance only from actual captured+selected results. No response derived merely from
dispatch, no automatic retry/new token, no post-pointer-commit lookup of catalog/Geography/context.
Same current saved revision + current pointer version is honest idempotent no-op.
A later ordinary draft edit must leave selected capture unchanged.
Capture and pointer select are TWO explicit commits, not one atomic transaction: if pointer CAS loses
after capture, immutable staging capture may remain but selected pointer stays old; report409, NOT success.
No outer/nested transaction or rollback of accepted214 capture required. Do not claim combined atomicity.

WITHDRAW
Body: required ExpectedSelectionVersion >=0.
Owned draft first derives binding; EXISTING219 ClearAsync once, no capture.
Stale version409; clear retains/increments token per219, already-cleared current no-op.
Return actual clear result; no invented activation flag or customer-ready assertion.
Rollback/history selection UI/API is not this slice;219 internal rollback capability retained.

PUBLICATION IS STILL SELECTED REVISION INTENT
Do not infer customer eligibility/readiness or blanket Store-only rule. No conversion to legacy policies,
no freeing/CarrierApi fallback, no guessed owner=Seller, no new Active/CanCheckout truth.
No readiness engine introduced: existing quote refusals remain authoritative. This API is preparation for
the next list/editor controls + actual fulfillment-plan consumer, not a Storefront-connected claim.

TINY ADJACENT SOURCE HYGIENE (NO R2 ENVELOPE)
In219 LoadPersistedPointerAsync, after Entry.ReloadAsync check Entry.State==Detached and return null.
Missing-row reload can detach without throwing, so do not return that reference as a live token.
Preserve catch/identity/clean-tracker semantics. No schema/transaction/repository rewrite; do not turn this
into another deleted-row/concurrency test framework. State this as source guard if not exercised.

FOCUSED EXECUTED PROOF
Use real route delegates + real owner authorizer/body reader/MediatR/validators/handlers.
Prefer existing authoring endpoint harness, extending test composition narrowly for real214/219 provider
rather than copying huge harness. No package/new project or production hooks.
Real PostgreSQL disposable target214:127.0.0.1:15432/shipping_tb214/tb214, same isolation helper,
existing local postgres17 image ONE task container, live identity before DDL, chain only disposable DB.
No shared5432/Docker restart/Host/network server required; route delegates in-process are sufficient.

One new focused endpoint/handler class:

View-only GET works; malformed unauthorized POST denied BEFORE body reads/dispatch/capture/select.
Seller SingleStore/ungranted scope denied with zero mutation; legitimate Marketplace seller keeps owner.
Missing fields/nonpositive draft revision/negative selection version rejected before handler mutation.
Real POST captures exact revision+selects pointer, GET exposes same provenance/version; draft revision/
UpdatedAt unchanged by publication. Same current publication no-op keeps version/snapshot.
Ordinary subsequent draft edit leaves selected read old; explicit next POST selects new capture at the
expected version; stale draft/selection returns409 and leaves pointer unchanged.
Withdraw retains/increments version, stale token cannot resurrect, same-cleared current is no-op;
unknown/foreign draft safe404, no pointer/capture of foreign content.
Pointer-failure after successful capture returns refusal and leaves old pointer unchanged (staging capture
may remain, explicitly asserted); success needs no after-commit enrichment.
Script only narrow failure seam if needed, explicitly label it; do not claim real race if controlled.
No global buyer/write acceptance claim from in-process endpoint tests.
One ShippingTests build + EXACT new class. No12/9/43/18/8/119/194/16 or wildcard/full-suite replay.

BOUNDS
No frontend/list change in220 (that is next221, not forgotten), no Order consumer, new schema/migration,
shared migration, live publish of user draft, HTTP probe/browser, Host/FE restart, flags, seeds.
214/219 migrations still NOT shared-applied. Preserve user draft/current revision/unsaved tabs,
Host5088FE3000, nine previously applied migrations, TB185paused/defaultOFF/PendingAllocation and§29.
Local nonproduction authorization remains valid for later necessary integration refresh, do not reask.

FINISH
Persist task verbatim + RESULT under evidence/TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220/.
Update TOP canonical recovery +AGENTS+new-chat guide with exact commands/counts and NOTLIVE.
Record remaining next: connect publication actions + direct new editor from service LIST, then server
fulfillment owner/type/group membership/measurement authority and current projection/selection/commit.
No further calculator or generic audit.
Stage exact paths only, commit/push/fetch parity, actual elapsed/corrections. No gitadd-A/reset/rebase/
stash/forcepush, Bridge interaction or historical post-result.js. STOP_FOR_ARCHITECT,
Automatic-Next-Task NONE; no221 envelope.