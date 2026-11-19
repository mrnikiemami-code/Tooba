# ARCHITECT REVIEW — TB-282-R16

Date: 2026-11-20 (Asia/Tehran)
Code: 6450d2684f58b5861ea650404f5e37395d771342
Evidence/head: 7f9dd5ef073cbbec257ec69b69194510b99e71b0
Expected base d7c36bf836f66365772f2f95a24a48e5b23628ae is ancestor (GitHub compare ahead).
Decision: ACCEPT_REPORTED_REJECT_TYPING_REPAIR_FOR_LOCAL_OWNER_CHECK
No full product, release or owner acceptance. No next implementation task issued.

## Source review
Only production delta is StoryManagementScreen.tsx. Stable parent status/action handlers and current callback refs in StoryLiveGrid remove dialog-state callback identities from queryAdapter, columns and bulk actions. Query adapter still changes for mode/reloadToken/reviewFilter. This addresses the confirmed forced-load chain without modifying AppDataGrid/shared effect or disabling reads. Current callback refs avoid freezing old action closures. Existing scope subscription/epoch/reload and durable executor paths retained.
No server authorization/lifecycle relaxation: main-storefront “موبایل” remains skipped where reject is not allowed.

## Evidence reviewed, not independently rerun
The real mounted R16 test selects 2 Submitted + 1 ineligible, opens Reject, dispatches a native input/change event per Persian character with a render flush, backspaces and retypes. It asserts query reads unchanged, selected count unchanged, focus/text, eligible/skipped counts; cancels with zero reject; reopens and confirms two commands with reason, versions 21/22, distinct keys and no ineligible URL; checks per-title results. A deliberate review-filter change still causes a read.
Worker reports this test failed pre-fix (~2400ms) and passed after fix; R16 1/1, R14 4/4, R15 2/2, tsc 0. The pre-fix receipt is a summary, not a full saved failure log.
Served next-dev controlled-HTTP receipt reports reads baseline/open/type = 1/1/1, selected count 3 retained, 2 eligible + 1 skipped, cancel rejectPosts=0. This is real served UI with substituted HTTP data, not a live business rejection.
Architect inspected commit/source and committed test/receipts; did not run the user's Windows browser/Host/tests.

## Specific coverage limits
Do not claim every R16 requested observation was established: new test asserts selected count rather than an explicit selected-id set; targeted dispatch assertions provide complementary identity evidence. It does not independently exercise IME composition, nondefault sort/page/scroll retention, or actor/scope selection invalidation. Those are not newly certified. Existing R14/R15 direct helper epoch tests cover stop-before-later-dispatch, not full mounted actor switching. No invented new browser geometry or live moderation proof.
The reported typing/refetch failure has a source-level repair and directly relevant mounted + served controlled evidence; these remaining limits do not justify another automatic implementation task.

## Handoff and recovery
State: AWAITING_LOCAL_OWNER_CHECK
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW
Owner rechecks selected row(s) -> Reject -> type/correct reason -> cancel/reopen. Eligible submitted shop stories may be rejected; platform main stories retain their lawful distinct lifecycle. Owner may then perform intentional test-record moderation; no such live mutation performed here.
Host5088 skip-bootstrap=1 / Next3000 next-dev and health/ready are worker-reported. Production Html404 build blocker remains; no production-ready claim. Preserve critical-storefront limitation, R6 bootstrap effects UNKNOWN, VendorShell, language registry, Returns/payout/user data.
ZERO backend/schema/auth/grant/seed/live Story-Media changes in R16; architect commit is docs only.
