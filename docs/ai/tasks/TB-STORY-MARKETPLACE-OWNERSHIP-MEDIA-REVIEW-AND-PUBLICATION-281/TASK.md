# TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281
Parent-Task: STORY-MARKETPLACE-COMPLETION-PLAN-2026-11-19
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Mode: FIRST_CONSOLIDATED_STORY_BACKEND_AND_MEDIA_DELIVERY
Expected-Base-SHA: 6c0914c37eb9de1e915c874718d813270cc8d9f0
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 0. Owner intent; ONE task active

Owner: «خوب تسک بده شروع بشه منتظر باش تا جواب بیاد بعد تسک بعدی هرچه سریع تر تمام بشه کلی توسعه داریم».
Execute ONLY this first consolidated delivery, post actual results and STOP. The second connected operator-UI delivery is PLANNED, NOT ISSUED. Do not dispatch it, delegate, resume old agents or continue automatically.

Product goal: each shop creates/manages its stories; admin reviews them and can create on behalf of a chosen shop. ONE shared editor/list will serve both roles; admin adds moderation and authorized shop selection. Ordinary shopkeepers must not need developer explanations. Orders is the list reference, but the visual/editor delivery follows this task's reviewed result.

Read:
- AGENTS.md, docs/ai/TOOBA-RECOVERY-CONTEXT.md and applicable repository-owned backend/frontend feature instructions.
- docs/planning/story/SOURCE-AUDIT-2026-11-19.md (S01–S13, source confirmed).
- docs/planning/story/STORY-MARKETPLACE-COMPLETION-PLAN.md.
- docs/product/OPERATOR-UX-ACCEPTANCE.md.
- Actual Story Endpoints -> MediatR -> Composer/Directory -> Domain; Media upload/serve policy; Party Contracts; existing test harnesses before executing them.

This task implements the FIRST delivery in that plan: real ownership, authorized media, content readiness, review/lifecycle, actual scheduled visibility, stable command identity/version and scoped grid/read contracts. It is not a fresh AMSC certification, generic CMS rewrite, social network, full theme redesign or ten microtasks.

Verify expected base ancestry. A direct docs-only issuance descendant is allowed; record actual claim SHA and incoming diff. Stop on source/config/schema drift rather than overwriting. Preserve user/untracked work. Normal commits + fast-forward push, no force/amend/rebase/reset/stash. No screenshots, delegation or polling loop. Persist the EXACT issued task bytes in both task/evidence TASK.md paths; do not reconstruct an alternative envelope when the issued file exists.

Decision:
ONE_STORY_OWNED_SHOP_AND_ACTOR_AUTHORITY_A_MEDIA_OWNED_TENANT_SELLER_ASSET_SCOPE_ONE_CONTENT_VERSION_BOUND_REVIEW_AND_PUBLICATION_POLICY_WITH_REAL_TIME_WINDOWS_AND_EXACT_COMMAND_REPLAY_ON_THE_EXISTING_MODULES_WITH_BOUNDED_ADDITIVE_SCHEMA_ONLY_IF_NEEDED

## A. Admin-on-behalf creation and trusted shop identity

Close S01/S09 without copying a seller actor or using seller headers as admin authorization:
- Existing admin global story stays supported. Add an explicit admin-owned intent to create a draft for a chosen SHOP with server-validated identity/eligibility. Invalid/foreign/unavailable shop refuses before a story row is inserted.
- The created story's owner is that shop; actual creator is the admin actor. It appears in THAT shop's list/get, not another shop. Keep business owner, creation actor and origin/provenance distinct; do not mislabel an admin as the seller user.
- Reuse Party/owning seller Contracts for lookup/status/display names; no foreign DbContext/schema join or trust in a browser display name. Do not assume any Party kind is an authorized shop.
- Seller create/get/list/update/item operations derive tenant+shop+actor from the existing owning authorization seam. A user who belongs to more than one shop is scoped by the authorized current shop; actor id alone is not an ownership grant.
- Add real human shop display projections for admin grid/detail and public cards as appropriate, enriched via Contracts. Use a public shop identifier/link only from the authoritative public-shop read, not raw Party Guid as a public URL.
- Do not reopen Returns/shipping/financial scopes.

## B. Real seller media path using Media ownership, not admin access

Close S06 and support image/video selection in the NEXT shared editor:
- Provide seller-authorized upload/list/get or the smallest equivalent owning contract needed by Story, with server-derived tenant+shop+actor. Preserve existing admin Media routes/policies.
- Reuse Media's object store/upload pipeline, MIME/size/error handling and CQRS. No parallel filesystem uploader inside Story. Admin may upload on behalf of an authorized shop without giving the seller access to admin credentials or the global library.
- Existing MediaAsset stores CreatedByUserId but NOT tenant/shop ownership. That actor field is insufficient (employees and multiple shops). Persist positively provable scope for NEW seller assets in a minimal Media-owned additive binding/metadata design. Existing unscoped assets remain admin/legacy; do not assign them to shops by guess, backfill or broad grant.
- Story may bind only a valid/Ready image/video that its owner may use. Wrong owner/tenant, missing/Failed/wrong-kind asset refuses with typed error. Resolve the authoritative public media URL through Media; do not accept an arbitrary id paired with an attacker-chosen URL as ownership proof.
- For new seller-owned submissions/publications, raw external MediaUrl is not a bypass of the asset ownership gate. Preserve pre-existing admin/legacy media read compatibility without granting seller access to legacy/global assets. Narrowly document how legacy admin static URLs are validated and stay usable.
- Do not expose another shop's private metadata/library. Distinguish authorized metadata/use from a published asset's public serving URL; do NOT claim that existing globally served legacy blobs became confidential. If new unreviewed uploads require private serving, enforce that through the owning Media scope rather than changing every legacy public file route.
- Test upload/storage failures and duplicate occurrence. A binary whose save succeeded but DB binding failed must not be returned as an authorized ready asset. Controlled object-store ports are allowed; do not upload a live fixture to canonical storage.

## C. One readiness and effective-publication authority

Close S04/S05/S08 using the existing Story owner:
- Drafts may be incomplete. Submit, approve/publish and schedule require playable content: at least one lawful non-empty slide, valid cover/asset facts and a valid destination if supplied. No empty published card that opens nothing.
- Keep review status and effective publication state conceptually separate but derived by ONE canonical policy used by read/actions/public filtering. Publish positive capability/action metadata for the next UI; frontend constants alone are not permission.
- Future start MUST become visible when due, with no manual click required. End boundary MUST stop visibility. Before start: hidden; exactly start: eligible; exactly end: hidden. Choose the smallest coherent solution over current Status/Schedule fields; no needless daemon/framework. Persisted Scheduled need not be rewritten if the authoritative effective read can answer truthfully and efficiently.
- Disabled/rejected/draft never become public merely because a timestamp arrives. A deliberate republish with a past end must require valid replacement schedule or an explicit lawful clear; never label “published” while public eligibility still excludes it.
- Add deliberate admin publish-now / approve-and-publish or approve-and-schedule owning intent with truthful atomic outcome. Review and state transition must not be two hidden HTTP calls that can be reported as one success after a partial failure.
- Preserve existing approve-only semantics where exposed; do not silently redefine a shipped route's meaning. New action code/command can express the composite operation explicitly.
- No new fixed 24-hour rule, automatic renewal or invented market policy.

## D. Change/review lifecycle, stale review and exact replay

Close S07/S13 in the same module:
- Seller can request/perform an explicit return of its own approved story to draft for editing; the simple bounded policy is to stop the old publication, then re-review changed content. Do not build simultaneous live/draft versions in this slice.
- Seller can explicitly stop its own publication. It cannot approve itself or reactivate unreviewed changes. Submission freeze and rejection reason remain meaningful; correction returns to the existing workflow.
- Admin-for-shop content uses the same owner/content policy, with admin authority for review. Do not turn admin origin into an unbounded bypass for shop-owned changes.
- Carry the expected content/version seen by the operator for NEW effectful edits/reviews/publication transitions. Reject stale or missing required review version BEFORE changing content/review/publication. EF concurrency alone is not a substitute for binding the review to the content that was shown.
- Stable occurrence + full immutable intent binding for create/add/media upload and effectful lifecycle intents. Exact retry answers the recorded result, conflicting same identity refuses, repeated submission does not add duplicate stories/slides or issue a second external upload.
- Store replay/version authority in the owning module with minimal feature-local design; do not create a generic workflow/lease/UoW platform. Authorization still runs on replay. A response loss is not a known failure.
- Preserve transaction ownership, scoped tracking and typed error semantics. No global ChangeTracker.Clear, swallowing unrelated unique constraints or transaction spanning external object-store effects.
- Validate CTA against actual allowed type/scheme and owning target where a shop-specific product target is supported. Do not let a seller advertise an unrelated shop's product by supplying a foreign id. Reuse existing owning read ports; never import foreign internals.

## E. Grid, detail and public contracts ready for one shared UI

- Admin and seller query endpoints use server paging/search/filter; same normalized policy, seller scope ALWAYS mandatory and not supplied as a permissive filter.
- Admin can search/filter shop by human name via owning Party read. Return page/count from the same scope and predicates; no separate full-list fetch for a headline.
- Stable tie-break paging; no raw-id fallback as the normal shop label. Unavailable display fact is honest, never an invented name.
- Detail read independent of the grid page; carries actual owner/display facts, status/review, version, readiness/blocker, media/slides and available actions needed by both panels.
- Public read keeps only eligible stories, returns real shop attribution and canonical published media. Add bounded public shop scope if supported by the existing public identity authority; never expose seller draft/admin facts or make shop filter bypass tenant/review.
- Language validation must accept the configured registry's legitimate locale codes (including hyphenated values) and not hardcode fa/en. Keep no-filter legacy semantics explicit; do not invent translations.
- Appended fields/actions only; preserve numeric enum values. Document real contract deltas and typed localized errors, using configured locale/fallback authority.

Frontend limit in this task:
Only typed story-api mapping/transport and the smallest existing handler argument plumbing needed to send new required versions/occurrence ids may change. Freeze observed versions from the reviewed row/detail, never back-fill them from a fresh read just to make an old review succeed.
NO grid restyle, new editor, public-player/social redesign, new layout or “UI ready” claim. Those belong to delivery two after Architect review. If an old client now gets a typed refusal because it lacks a required reviewed value, disclose exact compatibility impact; do not silently retain a bypass. Provide a concise contract handoff so delivery two is fast.

## F. Bounded schema permission and canonical environment

Canonical ONLY: tooba_alpha at 127.0.0.1:5432, SingleStore/store-alpha, Host5088, Next3000. No second DB/container/port/settings/keys/edition change.

This task explicitly permits the MINIMAL ADDITIVE Story/Media schema necessary for trusted new asset scope, actor provenance and durable occurrence/version results. Prefer existing sufficient columns. At most one coherent additive migration per owning Story/Media module (designer/snapshot included); no unrelated module DDL, cross-module FK or automatic legacy data repair.
Before ANY apply: inspect generated SQL/model, verify the plan lists only intended pending migrations, create a fresh real private pg_dump of canonical DB, record bytes+SHA256, verify pg_restore --list success BEFORE apply. Then use Tooba.MigrationRunner for store-alpha, one successful apply per migration, verify history/model/plan pendingCount0. No manual DDL/Down/editing already-applied migration.
On apply failure, establish complete transactional rollback and fix the same unapplied migration; get a FRESH verified backup before retry. Do not leave a knowingly broken intermediate migration followed by a cosmetic normalization migration.

Aside from authorized additive schema/history: ZERO committed business/test fixtures. No live story create/update/submit/approve/disable/upload, no media object writes, no seed/backfill/reset/delete/cleanup/restore, no mutation of existing stories or user data. No key read/rotation/revocation/history rewrite.
Tests may use isolated in-memory/control ports, task-owned temporary test directories and canonical PostgreSQL CALLER-OWNED rollback-only transactions. Clean only task-owned temporary test files, never canonical storage. Do not invoke StoryFoundationTests unchanged: that harness creates Docker DBs and seeds, which is outside this envelope.

Host-only publish/refresh is authorized if production backend changed, gracefully stop the old lock-holder, use existing publish directory/config and health checks. FE rebuild only if its permitted transport plumbing changed. Record actual binaries/build IDs. Served validation: GETs or read-only grid-query POST only; no business mutation probes.

## G. Proof — own actual boundaries, not merely route counts

Required focused scenarios (group into cohesive suites):
1. Seller A create/upload/add/submit -> not public until approval; B and another tenant cannot list/get/edit/bind A's asset or story; unauthorized replay also refused.
2. Admin creates for A with its real actor, visible in A not B; invalid/foreign/ineligible shop yields zero story rows. Admin global legacy story remains readable.
3. Empty/no-ready-media/foreign media/invalid CTA submit/publish refuse; incomplete draft permitted. Image and supported video positive path uses actual owning Media port.
4. Future schedule before/start/end boundaries through BOTH policy and actual Directory public reader. Disabled never resurrects. Past-end republish honest.
5. Approve-and-publish/schedule binds seen version atomically; stale edit/review refuses with no new mutation. Return-to-draft and own stop revoke public visibility, changed content requires review again.
6. Same occurrence+body replay vs conflict for creation/item/upload/lifecycle; no duplicate row/object-store call; controlled lost response/external failure + local-save failure stays honest.
7. Admin/seller query page/count/search/shop filter and foreign scope constraints; detail independent of page. Registered DI resolves real services, not a hand-built fake that bypasses wiring.

Include real PostgreSQL rollback-only facts for NEW constraints/scopes, version/atomic state changes and DB query behaviour where provider semantics matter. Explicit preflight client loopback/5432/tooba_alpha + server current_database/port before writes; missing/noncanonical config => visible SKIP, never a secret printed. Each fact asserts committedRows=0 after rollback. Do not commit rows to manufacture public walkthroughs or concurrency proof. Controlled ports may prove external failure/replay but must be labelled.

Run only directly relevant new/changed Story/Media/Host seam tests and affected exact contract guards if changed, then Release build + Host publish. No whole-solution/Host/Returns test sweep, broad AMSC campaign or source-regex-only proof. Add real behavioral facts for each boundary, not just catalog pins. Fix in-scope failures in this same task; do not create a microtask for each discovered integration gap.

## H. Durable result and second-delivery handoff

Persist unchanged TASK.md at:
docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281/TASK.md
docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281/TASK.md

Evidence directory includes RESULT.md, RECEIPTS.md, result-contract.txt, ownership-and-media-proof.md, review-and-time-window-proof.md, replay-and-relational-proof.md, migration-and-backup-proof.md (or truthful zero-schema receipt), and ui-contract-handoff.md.

ui-contract-handoff.md must show exact request/response examples and a capability/action matrix for the SHARED admin/seller editor: create/edit/submit/review/publish/schedule/stop/reopen, seller asset picker, shop labels and typed blockers. It is input to the next task, not permission to implement it.

Update CURRENT recovery pointers and story plan checkpoint with exact proved state and remaining UI delivery; keep original audit/history. Preserve Returns280-R1/279-R3 acceptance and future payout backlog.

Canonical result:
Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281
Status: PASS/PARTIAL within issued first delivery
Expected-Base-SHA: 6c0914c37eb9de1e915c874718d813270cc8d9f0
Base-SHA:
Code-SHA:
Evidence-SHA:
Final-HEAD:
HEAD-Equals-Origin-Main:
Admin-On-Behalf-State:
Seller-Media-Scope-State:
Readiness-And-Time-Window-State:
Reviewed-Version-And-Replay-State:
Scoped-Grid-And-Public-Contract-State:
Schema-State:
Live-Business-Write-State: ZERO
Technical-Validation:
Served-State:
Evidence-Limits:
UI-Delivery-State: PLANNED_NOT_ISSUED
User-Acceptance: NOT_CLAIMED
Marketplace-Readiness: NOT_CLAIMED
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT

Finish this task, push normally, report actual evidence and STOP. Do not begin delivery two before the Architect reviews the result and issues its concrete task.

