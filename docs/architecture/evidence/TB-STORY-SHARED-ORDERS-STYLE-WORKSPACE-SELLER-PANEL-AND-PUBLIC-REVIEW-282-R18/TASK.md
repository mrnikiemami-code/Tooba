PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R18
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R17
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Expire Story feedback, correct severity and close actual approval failure
Mode: OWNER_REJECTED_FEEDBACK_AND_APPROVAL_REPAIR
Expected-Base-SHA: 1d02e87d49049e96f4d175284b96165b4c3b4777
Issued-Date: 2026-11-20 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Observed failure / confirmed facts
Owner: messages must disappear; “cannot approve” must not be blue; actual seller Story approval still fails. Screenshots show pending yellow banner, red “1 مورد انجام نشد”, then unknown warning on the same awaiting-review shop story 01a04246-b309-7000-b1de-681552e70532. Do not claim a cause of approval failure from these screenshots.
Source: openAdminOp uses toast.info for zero eligibility; formatAdminBatchToast drops definitive single-row error reason. AppProviders already has autoClose=3000 but pauses on hover/focus loss; Story unconditionally renders a persistent story-pending-recovery banner for stored commands.
R17 toast/compact confirmation and R16 typing fixes retained. Owner does NOT want permanent message cards or result Dialogs. Read AGENTS/applicable instructions. One worker; no delegation/screenshots/polling.

## A — match existing app feedback, actually disappear
Use EXISTING toast mechanism/theme/colors; no restyling/global ToastContainer change. Story cannot-run/definite failure = existing error style; success = existing success; genuine uncertain/partial = existing warning. No blue info for “operation cannot run”. Keep one toast per action/batch; preserve grouped counts without full name lists.
Story feedback must close automatically using the app's 3000ms duration, including when focus/hover would otherwise keep this Story message alive; no autoClose:false, repeated recreation or timer reset on rerender.
Remove permanent yellow message presentation. Preserve unresolved command storage and exact retry: expose recovery through a compact user-invoked control/count, collapsed by default, not a persistent warning card. Do not erase pending commands to hide the UI. Recovery must remain discoverable and scoped, and disappear only after matching verified completion; no new recovery framework.
Single refusal must show short Persian reason (“تأیید نشد: …”), not just “1 مورد انجام نشد”. Mixed/bulk feedback stays bounded with grouped reasons/counts. Actual unknown says uncertainty, never definite failure/success.

## B — diagnose and fix the real approval path
Read-only current story snapshot/actionCodes/contentReady/media identity and available response/log evidence first. Reproduce the failure on REAL owning approval method/contract in controlled tests, not a fake that returns a desired Approved snapshot. Distinguish actual business refusal, HTTP/auth/version error, malformed success, wrong response binding and stale scope. Record concrete method/errorCode/status/body shape before changing it; no speculative weakening.
Repair the demonstrated cause end-to-end within Story-owned integration/Contracts as necessary. No changing grants/auth/bootstrap or bypassing server lifecycle, version/media ownership/readiness checks. If this existing URL-only/invalid-media item is legitimately ineligible, show the exact actionable reason and lawful repair path; do not approve it falsely, upload/re-add automatically or mutate existing data. Show a valid eligible Submitted story CAN approve with lawful media, and invalid media cannot. Align offered actions with actual backend prerequisites where this is the defect.
Keep current exact occurrence/body replay: loss/unverified 200 remains stored/unknown; genuine same-command completion updates review state and clears only its envelope. A terminal refusal must not be relabelled unknown merely because a stored intent exists; don't clear genuine uncertainty or mint a fresh conflicting command. Seller cannot self-approve.

## Proof / delivery
Actual mounted workspace + existing ToastContainer: zero-eligible error color/type, genuine success, definite refusal reason, unknown warning; one toast then removed after 3000ms, no automatic recovery/result card; opening recovery replays exact command. Retain R16 char typing/selection and R15 loss/epoch safety. Do not only spy on toast API or assert helper strings.
REAL approval owner test: valid Submitted + lawful asset -> Approved, expected version/key; explicit invalid fixture -> concrete refusal; actual response binds FE verifier. Rollback/controlled harness only, committedRows=0, no second DB/seed. Affected tests + tsc; backend focused tests/publish ONLY if backend production changes, skip-bootstrap=1.
Served FE3000 controlled/read-only browser: measure toast expiry without manual close, correct semantic colors, no permanent banner, recovery accessible. Capture zero live Story/Media commands. Report active route and source identity; no live approve/upload trial claimed.
No whole baseline sweep/build-blocker project. Host5088 skip-bootstrap=1, Next3000 next-dev; Html404 production blocker unchanged. ZERO migration/DDL/grant/seed/config/business-data/provider/money/stock/key operations; preserve VendorShell/i18n registry/date layout/Returns/payout/user data/R6 effects UNKNOWN.

Persist exact TASK.md at docs/ai/tasks/<Task-ID>/TASK.md and docs/architecture/evidence/<Task-ID>/TASK.md + receipt. Verify base ancestry (docs-only issuance descendants allowed); normal commit/push/fetch parity, no force/rebase/amend/reset/stash or keys/binaries staged. Update five CURRENT pointers.
Short result: concrete approval cause+fix or lawful refusal, observed toast expiry/colors, actual owner test vs controlled UI evidence, code/final SHA/limits. Color-only repair is NOT closure. Worker PASS != Architect ACCEPT != owner acceptance. No automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
