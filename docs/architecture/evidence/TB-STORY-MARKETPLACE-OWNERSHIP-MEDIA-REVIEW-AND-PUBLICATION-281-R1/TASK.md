TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1
Parent-Task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Mode: ONE_CONSOLIDATED_FIRST_DELIVERY_COMPLETION_AND_REGRESSION_REPAIR
Expected-Base-SHA: 860c6a2c72842c8971d8b9f9885e26cdb0f3f657
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

0 — Finish delivery ONE, not a new redesign

Owner already instructed: start one task, wait for its result/review before issuing the next, finish promptly because much other development remains.
TB-281 returned PARTIAL. This ONE continuation closes its original unfinished A–G scope plus concrete source-review regressions B1–B7. The shared Orders-style list/editor delivery remains PLANNED NOT ISSUED until this backend/media delivery is reviewed. No automatic chain, delegation, old agent resumption, screenshots or polling loop.

Read the EXACT issued envelope and:

docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281/ARCHITECT-REVIEW.md (B1–B7).
Parent TASK.md and its A–G acceptance matrix, RESULT/RECEIPTS.
docs/planning/story/{SOURCE-AUDIT-2026-11-19.md,STORY-MARKETPLACE-COMPLETION-PLAN.md}.
AGENTS.md and repository backend feature rules; frontend rules only for bounded transport plumbing.

Expected base is the worker's pushed code above. A direct docs-only issuance/review descendant is allowed; verify actual claim HEAD, ancestry and incoming diff. Do not overwrite source/config/schema drift or unrelated user work.
Normal commits/fast-forward push only; no force/amend/rebase/reset/stash. Exact issued bytes stay unchanged at both TASK.md paths, hash in issuance-receipt.json.
No generic CMS/authorization/tenant/idempotency framework, social network, redesign of unrelated modules or AMSC waves. Complete the existing owning module paths and required proof in THIS task rather than stopping after another four happy-path facts.

Decision:
ONE_COMPLETE_PERSISTED_STORY_PUBLIC_READER_AND_ROLE_SCOPED_ACTION_PROJECTION_FAIL_CLOSED_SHOP_MEDIA_BINDING_EXACT_SCOPED_OCCURRENCE_REPLAY_AND_VERSION_FENCED_OWNING_COMMANDS_WITH_ATOMIC_MEDIA_OWNERSHIP_AND_THE_ORIGINAL_G1_G7_PROOF

A — B1: repair the actual persisted public reader

StoryDirectory.GetPublicStoriesAsync currently filters with IsPubliclyVisible/HasPlayableContent before loading ignored Items; a fresh database-materialized story therefore has Items.Count0 and is removed before AttachItems.

Load/prove playable slides before applying the policy (or an equivalent correct owning DB predicate + shared policy). Do not weaken readiness or return empty published cards merely to make records visible.
Preserve tenant/review/origin/status/locale/market/time conditions. Scheduled becomes visible at exact start; hidden at exact end; Disabled/Draft/Rejected stay hidden.
Test through actual Directory with a FRESH context/materialization, not only an in-memory aggregate already holding slides. Prove active and due-scheduled positive, empty/failed-media negative and exact time boundaries.
Keep batch reads/no per-story media/Party N+1 where practical. No data rewrite or seed required.
B — B2/B7: complete trusted shop and media authority at every seam
Party/Media authority must never be optional success:
Remove null-port bypasses. Mandatory dependencies via real DI, or a typed unavailable refusal before effect, never return “eligible” or accept client asset id/URL when an owning service is missing.
Replace new StoryDirectory(db)-only fixtures with explicit recording owning ports that enforce/test real positive and negative outcomes. Prove registered DI uses the same path.
Shop lookup must prove an authorized/eligible shop in the intended tenant context, not merely a supplied Guid/string. Do not infer ownership from CreatedByUserId. Replays still authorize the caller.
Cover/item/readiness:
Validate every NEW shop-owned cover and slide create/update path, including admin acting for a shop, through Media owner tenant+shop+Ready/kind facts.
A video mediaType paired with an image asset (or vice versa), missing/Failed/foreign asset, mismatched id+URL or raw external URL bypass must refuse typed BEFORE mutation.
Revalidate actual required media facts at submit/approve/publish/schedule; an arbitrary non-empty Guid in a pure policy is not proof that Media says Ready.
Preserve narrow lawful legacy ADMIN static media compatibility without granting legacy/global assets to sellers or rewriting existing rows. Unknown assets are not legacy assets by default.
Add the missing admin-on-behalf shop upload/use seam under admin authority + validated owner; do not impersonate seller auth.
Preserve already-public legacy blob-serving behavior unless the new owning path specifically requires a bounded fix; report whether confidentiality of unpublished binary bytes is proven. Ownership/list-use authorization and public binary serving are different claims.
Endpoint architecture:
New seller/admin-for-shop Media routes remain thin auth/transport -> ISender -> validated Media-owned request/handler -> real Media service. Keep stable typed Result/catalog/localized errors and narrow Host auth composition.
Remove the duplicate Story-specific tenant hashing/constants from Media Endpoints. Reuse a canonical compatible context/identity seam through an appropriate existing shared/Contracts boundary; preserve existing stored Story tenant ids. No foreign Domain/Infrastructure reference and no mass tenant-key rewrite.
Relevant guard updates reflect actual route/request/validator coverage; do not broaden an exception to bless direct endpoint business execution.
C — B3/B4: exact replay and coherent upload/command durability
Media:
Require bounded nonblank occurrence identity for NEW seller/admin-for-shop uploads; bind the full immutable intent including authoritative tenant/shop, file bytes digest, normalized filename/type and other material inputs. Different bytes/type/name under the same identity => typed conflict, not old success.
The existing UploadAsync saves Ready asset before owner/occurrence binding; repair this new seller path so a failed local binding cannot leave a returned or reusable “unscoped legacy Ready” asset and retry cannot blindly upload again.
Use coherent durable claim/result and exact scoped duplicate arbitration. No DB transaction/lock spans external object-store upload. One committed operation must map to one authorized asset/result; unresolved storage/local-save outcome stays unresolved, not a manufactured failure/success.
On failure, only task-attempt-owned storage compensation may run with positive identity proof; no global deletes or canonical user storage cleanup. Controlled test stores/temporary task directories are authorized.
A duplicate/concurrent occurrence must be folded under actual constraints; scope keys safely across tenants/shops. Never swallow unrelated unique violations or erase caller tracking.
Prove changed-byte conflict before another object-store save, lost binding save and exact retry, plus controlled concurrent/constraint recovery without claiming a committed live stress harness.
Story:
Extend stable occurrence/full immutable binding to original admin/seller create, add-slide and all effectful lifecycle intents required by parent D; not just admin-for-shop create.
Replace delimiter-concatenated HashIntent with an unambiguous canonical encoding (distinct fields containing | must remain distinct).
Scope durable identities consistently with lookup/DB constraints; tenant A/B may use the same client occurrence without another tenant's unclassified PK failure or data leak.
Exact authorized recorded replay is checked BEFORE the version gate for a NEW effect. Retrying a completed stop/approve/return must answer its stored result, not stale-version-refuse its own success. Changed body/target/action/version under the same occurrence refuses without new writes.
Bind reviewed versions on every relevant existing route (approve/reject/enable/schedule/edit/add/update/remove/reorder/submit as applicable), not only the new composite endpoints. A caller must not use an old endpoint to bypass the same reviewed-content rule.
Update typed client transport and smallest existing handler plumbing where needed: freeze the version/occurrence actually reviewed, never fetch current version to bless an old intent. No UI restyle/editor work in this task.
Preserve atomic content/review/publication save. Failure in a composite action must not leave mutated tracked state that a same-scope retry accidentally saves as approval. Short owning transaction only when needed; never commit/rollback a caller's transaction or Clear the tracker.
D — B5/B6: finish role-scoped reads, actions, query and target facts
Map the authorized role separately: seller responses MUST NOT union admin actions. Same canonical role/state policy supplies list, query, detail and mutation responses; remove the grid's default-value shadow projection.
No caller-selected isAdmin flag grants authority. Use the endpoint/owning auth seam to choose scope.
Stop -> Disabled/Approved -> return-to-draft/edit -> resubmit must remain reachable in the seller's published actions and lawful under the command. Effective expired/past-end states must not advertise an invalid publish-now.
Add seller server query endpoint with mandatory tenant+shop, real page/count/filter/search and stable tie-break. Admin shop HUMAN-NAME search/filter uses Party Contracts, not cross-schema joins. Do not use take-limited Party matches as an unqualified exact whole-grid count without a defined complete result.
Enrich real admin/seller list/detail/mutation/grid and public read with batch authoritative shop names; populate optional public identity/link ONLY from canonical public shop facts. Raw Party Guid is not a fabricated public id. Add supported public shop scope and cross-shop negative proof without draft leakage.
Validate seller CTA product targets against the actual owning accepted product/offer/shop read, including update and later submit/publish paths. Foreign/nonexistent target refuses without mutation; controlled real-directory positive and negative tests. No invented product rows/permissions.
Keep locale acceptance compatible with configured legitimate locale codes; no fa/en list or duplicate registry. All new capabilities must be present in the ui-contract-handoff, with exact request examples, required versions/keys and typed blockers.
The public player's geometry/social UI remains for delivery two; do not expand this repair into analytics or social persistence.
E — original proof matrix COMPLETED on real seams

The parent's original G1–G7 remain required, grouped into a small cohesive suite. Architecture pins and pure Domain facts are supplementary, not substitutes.

Must include:
R1 fresh persisted active/scheduled public Directory read at before/start/end; Disabled/empty/unready negatives.
R2 registered Party/Media dependencies, A/B/tenant isolation, missing authority fail-closed, cover AND slide AND admin-for-shop MIME/owner gates and valid shop/creator provenance.
R3 real MediaDirectory with recording object store: full-file replay/conflict, failed binding save, unresolved/retry and scoped key reuse/constraint recovery; zero unintended object/asset/binding result.
R4 real Directory version/occurrence counterexamples on old and new endpoints/adapters: stale review zero writes, exact replay zero duplicate effect, delimiter-distinct body conflict, same key in separate authorized scopes, atomic composite-save failure.
R5 role-specific grid/list/detail/action policy consistency, stop/edit chain, seller query/count/admin shop-name filter, public attribution/scope and CTA foreign-product refusal.

Use actual DI/request/handler for relevant HTTP seams; no hand-built directory with absent authority.
Add canonical PostgreSQL caller-owned ROLLBACK-ONLY facts for public fresh-context materialization/query, new uniqueness/scoping, version/atomic state and Media metadata failure constraints. Validate client loopback/5432/tooba_alpha and server database/port before writes; absent/noncanonical => visible SKIP, no secret printed. Assert committedRows=0 per fact on separate post-rollback read.
External object store is controlled in tests; no canonical file upload. Contexts sharing a caller transaction may prove fresh materialization without a committed fixture. Clearly distinguish deterministic failure injection from real duplicate/locking proof.
No second DB/container. Do not invoke unchanged Docker/seed StoryFoundationTests. No whole-solution/Host/Returns test sweeps or stress campaign. Run actual affected new suites, exact changed contract guards, FE transport tests/typecheck, Release build/publish once after final fixes; rerun only when a fix justifies it.

F — applied schema/history and safe additive repair envelope

The two TB-281 migrations already applied are IMMUTABLE:
Media 20261118233932_Tb281SellerAssetOwnerScope
Story 20261118233937_Tb281CreatedByAndOccurrences.
Do not edit/reapply/Down them, rewrite rows, reset/reseed or normalize history.

A MINIMAL additional migration per affected owning Story/Media module is permitted ONLY where required by exact intent/durable-state/scoped constraints above. Reuse existing columns where sufficient; no unrelated DDL or generic lease tables.
Before each apply: inspect SQL/model, plan only intended pending migrations, FRESH private pg_dump with bytes/hash and successful pg_restore --list BEFORE apply, normal Tooba.MigrationRunner store-alpha apply, verify model/history/post-plan pending0. No manual DDL, backfill, data repair or business fixtures. If no new migration needed, record that rather than inventing one.

Parent backup receipt: preserve actual historical pre-apply evidence if it exists. If absent, say so; verify its archive NOW with actual verification time/exit/bytes/hash/TOC, do not call this retroactive pre-apply proof.

Canonical: ONE tooba_alpha 127.0.0.1:5432 SingleStore/store-alpha; Host5088/Next3000. Aside from authorized migration history, ZERO committed business/test rows. ZERO live create/edit/submit/review/publish/stop/upload/stock/payment/notification, no existing story/order/history mutation. No restore/delete/seed/reset/settings/connection/port/key operation.
Host refresh permitted after graceful stop and fresh publish with existing config. FE build/restart only for permitted transport change. Read-only GET/grid-query POST only; no business mutation probe. Record actual served health/artifact identity.

G — finish evidence and STOP, next UI task still not issued

Evidence: docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1/ contains exact TASK.md, RESULT.md, RECEIPTS.md, result-contract.txt, source-findings-closure.md (B1–B7 -> source+test), directory-and-media-proof.md, rollback-only-proof.md, schema-and-backup-proof.md and ui-contract-handoff.md.
Exact task also at docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1/TASK.md.
Preserve parent PARTIAL report and review. Compact CURRENT pointers in AGENTS/recovery/START-HERE and plan checkpoint, no rewriting historic acceptance. Returns280-R1/279-R3 and payout-method backlog unchanged.
Declare any genuine remaining blocker precisely; no “PASS within tiny self-selected tests” if a required B/G path is still broken. Keep in-scope test-discovered defects and fix them in this SAME repair.

Result contract:
Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1
Status: PASS/PARTIAL
Expected-Base-SHA: 860c6a2c72842c8971d8b9f9885e26cdb0f3f657
Base-SHA:
Code-SHA:
Evidence-SHA:
Final-HEAD:
HEAD-Equals-Origin-Main:
B1-Persisted-Public-Read:
B2-FailClosed-Shop-And-Media:
B3-Atomic-Upload-Replay:
B4-AllPath-Version-And-Occurrence:
B5-RoleScoped-Projection:
B6-Queries-CTA-And-PublicIdentity:
B7-Owning-CQRS-And-Tenant:
G1-G7-Proof-State:
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

Post the result after normal commit/push and STOP. Do not start the shared UI delivery or issue another task.