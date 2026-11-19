PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R3
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R2
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Verified response retention and a real published-product picker
Mode: TWO_REMAINING_SOURCE_CONFIRMED_GAPS_ONLY
Expected-Base-SHA: 9776ab9623aadb2d58f001113ce8269039b11a94
Issued-Date: 2026-11-19 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Goal / protected progress
Only S1/S2 from R2 ARCHITECT-REVIEW. Retain served merchant editor, routes, VendorShell, role-specific lookups, Tehran/Jalali date controls, frozen-byte replay, Media asset-PK pipeline and existing proofs. No new UI redesign/backend wave/feature phase.
One worker, no delegation/subagent/screenshots/polling/automatic next. Verify main/origin/base (docs-only issuance delta permitted). Exact task bytes/receipt; no rebase/amend/force/reset/stash.
Read AGENTS and applicable frontend/backend feature skills + R2 review; architecture/OPERATOR-UX/seller integration preserved. Do not reopen 281/Gate0/Media/unrelated landing.

## S1 — successful outcome requires actual bound evidence
In story-executor finalizeResponse, HTTP 2xx alone is not completion. Implement action-specific validation against the REAL existing endpoint wire BEFORE clear or child advancement:
- Empty/non-JSON/malformed 200, mapper null/undefined, missing/wrong Story id/shop/target or incomplete required snapshot => unverified/unknown outcome; same stored bytes remain, no success toast/navigation/next child.
- Creates validate real Story identity and owning scope; updates/item/review/schedule bind returned Story target; upload validates complete Ready media identity/metadata; use exact actual API contract, not arbitrary payload or permissive mapper defaults. Arrays/composites only if an actual route returns them.
- Network/read/parse/map exceptions become controlled unresolved outcomes, not an uncaught promise or deletion. Pending recovery visible immediately, even if detail load fails. Successful exact retry with genuine bound response clears only matching evidence.
- Validate stored envelope action <-> role <-> allowed Story/Media method/path <-> target <-> body fields/version/occurrence and upload-child stage. Nonempty strings alone are not validation; never replay an arbitrary stored URL or another module command.
- All storage access including getter/enumeration inside safe error handling. Unavailable/unreadable evidence is an explicit blocking result, never catch=>[]/missing. No possibly-sent evidence overwritten.
- Recognize actual previous v1/v2 storage namespaces. A current actor's recoverable valid old envelope may be handled only with sufficient frozen proof; otherwise show safe blocked evidence. Never guess old actor/body, leak another actor's contents, clear legacy silently or allow a conflicting replacement by changing namespace.
- Current exact-byte/actor scope/child compare-and-advance remain. Do not introduce broad no-effect refusals, auto-retries, polling, blind discard, or a new backend ledger.

Focused proof (REAL mounted executor/workspace, not regex):
1) unknown -> 200 empty/non-JSON/{} / wrong Story target / malformed mapped data: envelope BYTE-IDENTICAL, no success or navigation; valid same-command replay then clears.
2) upload response malformed never starts add; valid upload + lost add reply resumes add only, bound to same asset/key/body.
3) storage getter/enumeration throw and malformed/legacy current-scope evidence => visible blocked/no send; other actor not exposed.
4) known action with wrong route/method/target/body binding => zero fetch; valid actual endpoint fixture passes.
At least the malformed-success retention fact must fail against PRE-FIX production function (or temporary precise negative control), then restore and final green. A separate broken helper returning true is not this experiment.

## S2 — real published catalogue search/page/count
Remove ICatalogDevelopmentPublishedProductSampler and fixed40 from production Story options. Reuse actual Catalog/Offer public/admin read authority through legal Contracts/adapter/CQRS, not a new search framework or cross-module table access.
- Product options accept server search + page/cursor + bounded page size; return consistent total/count or next cursor from SAME eligible set. Query before paging, enrich selected page only. No take-first40/client search claiming full catalogue.
- Platform selection works without a shop and without DEVELOPMENT sampler registration. Seller/admin-for-shop retain actual authorized shop/Offer eligibility, never card winner/first seller. Disabled/nonpublic/foreign-only options handled by existing owning policy.
- UI searchable combobox uses actual server search and loads next page lawfully, preserves selected product/name, handles empty/error. Use existing design-system picker pattern; no raw product id or wholesale table/editor rewrite.
- Minimal additive read DTO/query/Contract method allowed, no schema/migration/seed/grant/config change. Existing route/count guards only change for actual contract additions.
Proof: >40 eligible persisted/controlled products, matching item outside first page found by server search; next page not duplicate; count/cursor correct; no development sampler; two shops cannot borrow each other's eligibility; selected valid product still navigates canonical public slug.
Use real owning query with controlled Contracts where appropriate; provider/fixture limits explicit. No live fixture or business mutation.

## Validation / current serving / handoff
Only new focused S1/S2 tests + affected Story guards, tsc/Next build, relevant backend build/Host publish if changed. No whole-repo/whole-Host/Gate0/Media baseline sweep. Real failure fix/rerun allowed, declare runs truthfully.
Verify actual current Host5088/Next3000 identity/build-id/routes/health/ready/platform, skip-bootstrap as existing. Read-only DOM/text at1440/390 for affected recovery/picker states with controlled HTTP distinguished from live data; no screenshot/live mutation.
Known critical-storefront unrelated landing failure remains recorded; don't restyle/weaken its guard. Run affected required storefront checks if its Story components changed, accurately scoped.
One canonical tooba_alpha 127.0.0.1:5432 SingleStore/store-alpha. ZERO migration/DDL/backfill/seed/reset/cleanup/restore/live Story-Media POST/upload/provider/notification/key operation. Controlled/rollback-only fixtures committedRows=0, no second DB/Docker/EnsureCreated.
Preserve all user data, Returns acceptance/payout backlog, seller shell/theme/auth. Generic Media pipeline untouched. Normal push/fetch parity, no staged scratch/keys/binaries.
Keep R2 report historical; correction addendum for 2xx completion/dev-sampler assertions. RESULT/RECEIPTS/S1-S2 test ledger + current wire handoff + short Persian user-review URLs/checklist. AGENTS/recovery/START-HERE/Story plan reflect actual state; one active task only.
Success only when S1 cannot erase possibly-sent evidence on malformed replies and S2 genuinely searches the full eligible catalogue; focused proof and serving verified. Required omissions cannot be called PASS limitations.
ONE canonical result: exact Task-ID, Status, Expected/Base/Code/Evidence/Final SHA+parity, S1/S2 states, actual validation/limits, User-Work-Preserved YES, User-Review-URLs, Recovery-Next-Task NONE, Next-Recommended-Task NONE, automaticNextImplementationTask NONE, STOP_FOR_ARCHITECT_AND_USER_REVIEW. Worker PASS != Architect/user acceptance. Then STOP.

END_TOOBA_TASK

