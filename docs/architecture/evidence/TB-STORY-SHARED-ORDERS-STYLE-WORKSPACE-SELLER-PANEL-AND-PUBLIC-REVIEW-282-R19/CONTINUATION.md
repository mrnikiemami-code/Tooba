PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R19
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R18
Instruction-Revision: 2
Continuation-Of: SAME_R19_NOT_A_NEW_TASK
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Complete every remaining R19 obligation and prove one connected owner-ready delivery
Mode: COMPLETE_EXISTING_R19_IMPLEMENT_VERIFY_SERVE
Expected-Base-SHA: 0de110784dadb883c91bbf0ea88f5c6e23832103
Original-Expected-Base-SHA: d3de64c2c6692b551c8193e842bdd04fe99ceded
Issued-Date: 2026-11-21 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Binding scope — finish, do not reduce it
Owner explicitly orders completion of SAME R19. Existing 8fb403cfe implementation is PARTIAL. Preserve its fixes; do not restart or create R20.
Read the ORIGINAL docs/ai/tasks/<Task-ID>/TASK.md (A–F, obligations 1–16), its exact evidence copy, ARCHITECT-REVIEW.md, current AGENTS/applicable feature rules and docs/product/OPERATOR-UX-ACCEPTANCE.md. This continuation resolves the rejected handoff; original scope remains binding. The worker-created A–G subset is NOT the scope and cannot replace the original.
One worker, no delegation/screenshots/polling loops. Work through implementation, focused tests, served verification and evidence in this task. Do not return an analysis-only phase, a cosmetic-only PASS, or “ready” with mandatory checks skipped. Internal commits/checkpoints are allowed; if a session limit intervenes, resume SAME task. Do not hide real blockers: a blocked mandatory gate means PARTIAL/BLOCKED, never PASS_WITHIN_ISSUED_SCOPE.

## Remaining implementation — all rows required
| Gate | Required completion |
| --- | --- |
| 1. Real success, not matching id | Remove the identity-only bypass in effectSatisfied/verifyStoryTarget. Approve/publish cannot succeed from {storyId}, version-only or unchanged Draft. Bind each action to actual owning response/version/effect, including item updates and exact replay/tombstones. If an endpoint truly returns a partial reply, add a small additive owning receipt or lawful same-command verification; do not bless arbitrary missing fields. Keep malformed/wrong-target/unchanged replies pending. |
| 2. Honest no-effect vs uncertainty | A parsed arbitrary errorCode is not proof of an authoritative terminal refusal. Validate owning error contract/no-effect semantics before retiring only the matching envelope. HTML/proxy 4xx, transport loss, 408/425/429 and ambiguous 5xx retain exact uncertainty; storage barriers do not imply the older command never ran. Report true refusal with reason, genuine unknown as unknown. |
| 3. Durable media replacement | Replace standalone upload-then-unpersisted-update with a staged Story-owned journal through existing intent/executor authority. Freeze file digest, item id, requested fields/version and occurrence identities BEFORE sending. Lost upload response resumes that upload; verified upload freezes asset+UPDATE bytes; lost UPDATE retries ONLY UPDATE, without reupload. Remount, cancel, repeat Save and recovery must not orphan/repeat uploads or change intended fields. Preserve same item/order and old media until bound update succeeds. Metadata/product-only edits upload nothing. No literal digest “pending”. |
| 4. Permission/context correctness | Apply existing registered Story capability authority on server reads/writes through lawful ports; generic panel access alone must not bypass feature permission. No invented permission taxonomy or grant seed. Test panel-Allow/Story-Deny direct API, lawful Allow, seller no moderation, cross-shop denial. Stop silent demo-shop fallback inside Story; show choose-context state. Preserve VendorShell/login. |
| 5. Real query state and filters | Refresh CURRENT search/filter/sort/page/scroll, not defaultQuery; deterministic last-row page deletion. Maintain R16 typing/cancel selection stability; clear old context selection. Finish admin AND seller status/review/publication filters and retained shop/date/updated sorting. Normalize/execute/count/page against same set. Prove nullable date SQL translation on existing PostgreSQL rollback harness if supported, otherwise explicit provider limitation (never “SQL proven” from InMemory). |
| 6. Routes/language/error/leave | Preserve fa/en prefix on new/detail/back/recovery; grid receives locale/direction; copy and dates follow existing authorities/registry. Detail 404/error/denied has back/retry; context switch cannot keep stale denial or a list under old detail URL. Dirty title/window/slide/file changes protected on sidebar, Back, reload and dialog cancel through supported existing navigation/DS patterns. |
| 7. Product picker | Gate reads by open dialog/product target, debounce, abort/discard stale response by actor/shop/search/page epoch; error/empty/loading/more states. Preserve stored target/name independently of picker pages. Late other-shop results must never overwrite current options. Keep product identity, not a new offer-target design. |
| 8. Authoring and messages | Finish shared New/Edit and preview with real player behavior, clear primary next step and field errors after interaction only. Remove always-appended pending/warning/message sections: owner explicitly means IN-PAGE messages, not just toast timing. Use native transient result toast; unresolved intents remain reachable via discreet toolbar action/count and user-opened bounded recovery, not an appended details panel. Compact selected-count confirmation; no full title list/result modal/toast storm. Clear stale operation/field errors appropriately; do not erase history or genuine uncertainty. Consistent confirmations/cancel for destructive stop/return/remove as required by original task. |
| 9. Public rail/player/eligibility | Fix HomeStoriesSection too: a video-containing story's JPEG cover remains an image; no sample image pretending to be merchant content. Keep modal caption/loaded-photo timing, real video ended, mute/touch/RTL keys; proper focus trap/restore and scroll lock. Revalidate on meaningful events/window boundary so an open page does not play stopped/expired content indefinitely. Reuse public authority, bounded expiry timer permitted, no polling loop. Complete DB public-window/locale/market narrowing and avoid serial product-read amplification using lawful contracts/bounded work. Preview agrees with public playback. |
| 10. Production serve | Diagnose/fix Html404 build cause minimally, without suppressing checks or blanket config changes. Final Next production build must succeed for READY_FOR_OWNER_TEST. Publish changed backend and restart Host5088 with TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1; prior receipts saying Host “untouched” do not serve new code. Serve matching FE3000 production artifact; record identity and health. No new ports/env/database stacks. |

These are repairs in the existing feature, not permission to replace DS/grid/auth/Media infrastructure. Extract cohesive hooks/components as needed to remove the oversized screen's state coupling, not cosmetic file moves. Keep title-first/logical-end actions, saved views, pagination, media/cover identity, legitimate review lifecycle and earlier sound fixes.

## Required proof — completion checklist
Create COMPLETION-CHECKLIST.md using ORIGINAL A–F/1–16 plus rows 1–10 above. Each obligation must link production method + actual test/served observation + result; mark missing evidence MISSING, not “done by code/tsc”.
1. Actual owning method/endpoint + real serializer contract on controlled/rollback data: create own-shop/platform draft, upload/add, metadata/product edit, submit, approve-only, approved-unpublished, publish/schedule, public eligibility, stop, seller return/replace/resubmit, reject, delete and exact recovery. Keep actor/shop/version/occurrence assertions. Test actual response against frontend verifier; a mock always returning desired Approved is not integrated proof.
2. Real workspace/AppDataGrid: nondefault query before/after mutation; selected 2 eligible+1 skipped; reason per-character/backspace; cancel zero commands; actor switch; invalid/late products; fa/en routing; route errors/dirty navigation; valid and failed replacement upload and update, remount/exact retry with no extra upload. All critical side effects captured at transport.
3. Precise negative controls/new regression failure on relevant old behavior: identity-only success; HTML404/unknown retry; storage barrier; late cross-shop response; failed detail; replacement upload loss and update loss; preserved current query. Revert only temporarily, record failure, restore and final green. Do not weaken old safety assertions.
4. Real served browser at 390/1440: admin+VendorShell+public actual active states, typography/overflow, query/selection, no appended messages, native toast colors/expiry, loaded-photo dwell, actual video ended, caption/CTA, hidden/resume, focus trap and expired/stopped revalidation. Read-only or clearly labelled controlled HTTP on REAL served UI; jsdom/HTTP200/chunk grep is not browser proof.
5. Controlled 1000-plan ACTUALLY rendered through confirmation/completion/on-demand recovery: bounded nodes/one summary toast, not helper-only. No all-pages selection expansion or 1000 live commands.
Run affected focused FE/backend tests + tsc + final Next production build; publish affected backend. No whole solution/Host historical sweep. Keep unrelated six failures disclosed with prior controlled baseline, not a reason to skip issued work; do not relocate Catalog architecture merely to silence unrelated W6 guard, and do not widen foreign coupling. If that architecture dependency truly blocks an issued gate, provide concrete evidence, not an invented scope exemption.

## Data / operational boundaries unchanged
Canonical tooba_alpha@127.0.0.1:5432, SingleStore store-alpha, Host5088 FE3000 only. ZERO live Story/Media mutations for worker proof; use existing controlled/rollback harness (committed live rows=0). No migration/DDL/seed/backfill/reset/restore/cleanup/grants/keys/provider/money/stock, no connection/edition/config bypass. Do not create another DB or use EnsureCreated on canonical store.
Historical old-story deletion/four CTA rows are REPORTED prior mutations; inspect read-only, do not delete/restore or rename historical activity ZERO. Preserve R6 bootstrap effects UNKNOWN, VendorShell/languages/Returns/payout/user data. Owner may deliberately test their own records; that does not authorize worker live fixture writes.
Normal commits/push/fetch parity; no force/rebase/amend/reset/STASH. Preserve original TASK.md/historical reports. Save this exact revision as CONTINUATION.md in BOTH original task/evidence folders, hash/receipt included. No new Task-ID/Bridge row solely to evade incomplete R19.

## Finish gate / final response
PASS_COMPLETE_R19 is allowed ONLY when EVERY mandatory original obligation is implemented and its appropriate proof exists, with production frontend build and matching backend/frontend served. Historical unrelated failures can be disclosed; omitted issued gates cannot be called “known limits” and still PASS.
Otherwise return PARTIAL or BLOCKED with exact missing gate/reason/evidence. Never claim delivery on compile+chunk strings; do not make owner discover known defects.
Record actual final code/evidence SHAs, fresh origin parity, Host DLL hash/skip-bootstrap/health/ready/store-alpha, Next BUILD_ID + production mode, tested routes and actual provider boundaries. Update all FIVE CURRENT pointers and final A–F completion ledger consistently. Short Persian owner handoff with actions/URLs and genuine limits.
Worker completion != Architect ACCEPT != OWNER acceptance. Stop for review; no automatic next implementation task and no R20.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
