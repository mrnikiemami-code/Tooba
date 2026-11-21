# ARCHITECT REVIEW — TB-282-R19

Date: 2026-11-21 (Asia/Tehran)
Implementation reviewed: 8fb403cfe17b40729f7f582acdf9aea5a238ee89
Reported merge head: d7d86e4af4c3a3dce2bb3d566b750feb0f9d45e3
Actual reviewed origin/main: abb102c3bab9219c0c12b69e2f6d22a5ca1826a1 (additional commit is RESULT/result-contract documentation only).
Decision: PARTIAL_NEEDS_COMPLETION_WITHIN_SAME_R19
Local owner exploration is allowed; NOT full acceptance or release readiness. No R20 issued.

## Scope discrepancy
Canonical TASK.md has A–F and numbered obligations 1–16 plus connected verification. Worker RESULT/ledger relabel a selected subset as an “issued A–G scope”. That smaller scope is not the issued task. Preserve positive work, but PASS_WITHIN_ISSUED_SCOPE is not accepted for the complete R19.
Architect read current source delta, original TASK, actual new tests and receipts; did not independently run Windows Host/browser/tests.

## Accepted source improvements (bounded)
Role-specific stop route now binds admin /disable and seller /stop. Seller media uses the mapped group root. Approved/unpublished copy and seller Approved+Draft return action added. Admin grid normalize/filter/sort mappings added. Public modal renders caption, gates photo timer on image load and adds dialog/body-scroll/focus restore/visibility handling. Explicit replacement control preserves the old row until update.
Worker reports real-directory return-to-draft and real grid over InMemory 3/3, FE units/types green. New “R19_MOUNT 5/5” consists of THREE pure binding/verifier facts and TWO mounted components, not five real workflow mounts. Player test asserts img/caption/dialog nodes, not elapsed dwell or video ended. InMemory engine cannot prove PostgreSQL SQL translation.

## Remaining concrete defects and missing issued work
| Original task | Finding |
| --- | --- |
| A3 completion | effectSatisfied explicitly returns true for a same-id reply without state; test explicitly asserts identity-only approve succeeds. Version presence does not impose version progress; absent action state bypasses checks. This does not close “matching storyId alone insufficient”. Actual endpoint/serializer receipt matrix still missing. |
| A4 uncertainty | HTML/unparsed 4xx now retained, positive. Any parsed arbitrary errorCode is treated as authoritative; no owning-code/no-effect validation. story-admin-ops uncertainty classifier unchanged for 408/425/429/ambiguous 5xx and existing storage-barrier ambiguity. |
| A5 permissions/context | No change to Story feature authorization vs generic panel access; no dedicated deny-with-panel-allow proof, and silent demo context fallback remains. |
| B6–10 list/routes | Some admin filters repaired. Current-query refresh still recreates adapter and reloads default query. basePath remains unprefixed. Product-search effect still has no debounce/cancel/epoch; detail failure remains toast + null without route error/back state. No comprehensive locale, denied/context, unsaved sidebar/back, seller status-filter or grid locale/date closure. |
| C11–13 authoring/feedback | Real full public-player preview not integrated; explicit replacement happy path added but no durable upload-to-update journal. A standalone upload success is cleared before update envelope exists; on lost upload, generic recovery retries upload then only refreshes, never resumes frozen update. Metadata digest is literal “pending”. Existing pending details remain appended in page rather than user-invoked toolbar recovery. No complete dirty/cancel/1000-plan actual-render proof. |
| D14–16 public | Modal caption and image gating improved; HomeStoriesSection unchanged and still picks video based on story.isVideo/any video then loads cover as video, with sample-image fallback. Public eligibility refresh/window-boundary logic and DB public-query optimization unchanged. Role semantics alone are not a focus trap. |
| E/F served/build | No browser geometry, actual ended runtime, full connected owner/serializer tests or measured 1000 UI. Production Html404 build blocker unchanged. Receipts say running Host untouched although backend source changed; no refreshed Host DLL identity proving the new backend is served. HTTP 200/chunk strings do not establish active workflow correctness. |

## Replacement recovery counterexample from source
StoryManagementScreen replacement creates action=upload envelope with digestHex “pending”; once upload succeeds it clears through runDurableStoryMutation and creates an update_item only afterwards. If upload response is lost, replayPending's generic success handler handles create/delete specially but has NO continuation for a replacement upload. The uploaded asset and original intended update are not durably joined. If update response is lost, clicking normal Save again uploads first and can leave another asset before hitting the stored update barrier. This contradicts C12 exact staged recovery/no repeat upload. No live request made to demonstrate it.

## Process/provenance limits
Receipts disclose stash push/pop despite explicit no-stash boundary, and broader backend/solution checks despite focused scope. Record these as deviations; do not discard baseline results or infer code corruption. Baseline six failures are worker-controlled observations, not independently rerun by architect.
Catalog coupling finding may remain disclosed without an unrelated architecture migration; it does not excuse omitting the other explicitly issued feature obligations.
Preserve R6 effects UNKNOWN. Earlier old-story deletion/four test-row creation remain REPORTED historical live mutations; R19 no-write claim is scoped to this pass.

## Owner testing / stop
Owner explicitly asked to test now. They may explore current localhost admin/seller/public UI using intentional test records. Explain which fixes to check and that completeness/release is still pending; do not ask them to test away missing engineering proof.
State: PARTIAL_LOCAL_OWNER_EXPLORATION_ALLOWED
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R19
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
Remaining work belongs to the SAME R19, not another automatically issued task. No new worker dispatch performed in this review. No production/user acceptance.
