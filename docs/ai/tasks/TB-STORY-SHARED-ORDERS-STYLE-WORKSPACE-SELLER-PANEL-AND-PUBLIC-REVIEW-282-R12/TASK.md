PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R12
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R11
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Align publication dates when no-end is toggled
Mode: OWNER_REPORTED_LAYOUT_ONLY
Expected-Base-SHA: 882f7f7bad03d29e1cba00cf2f34bfcbdf8874fe
Issued-Date: 2026-11-20 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Fix
Owner screenshot: /admin/stories/01a04211-7dd7-7000-83dc-5c79db49abc4. Unchecking “بدون پایان” puts end date/time below the start fields. Source: StoryManagementScreen.tsx create/edit publication-window sections place a min-h-11 checkbox row ONLY above end fields; nested label elements also exist.

Read applicable frontend feature instructions. Make ONE small presentation repair in shared admin/seller create/edit:
- Put “بازهٔ انتشار” and “بدون پایان” control in the section header, outside date/time field columns.
- Desktop RTL: two equal columns, START on right and END on left; headings aligned, date pickers on same row and clock fields on same row, same heights/gaps.
- Keep end date/time positions stable while toggling; when no-end is checked show those controls disabled with clear visual treatment. Preserve existing no-end null serialization and required-end validation. Do not erase selected values or invent default dates/times.
- Mobile: single column start then end, header wraps neatly, no clipping/horizontal document overflow.
- Replace nested label wrappers with valid div/fieldset structure and individually associated accessible labels; checkbox must not focus/toggle unrelated picker.
- Apply consistently to BOTH creation and existing editor, admin and existing VendorShell. Reuse shared local markup where helpful; no shared DS/grid/global CSS rewrite.

## Verify / stop
On actual served form at1440 and390, toggle no-end on/off and inspect date/time bounding rectangles: desktop start/end date top and clock top equal (<=2px difference), no overflow, mobile order correct. Read-only interaction only; no saving/upload/delete/submit. No screenshots.
One focused check of checkbox independence and retained values/null-vs-required-end semantics if affected; tsc. Reuse existing schedule checks; no broad regression/proof phase. Refresh FE3000 and record actual mode/chunk identity; existing production Html404 blocker remains disclosed, not part of this task.
PASS only if the reported toggle layout is fixed in both form states. If active page cannot be inspected, report that limitation as PARTIAL instead of claiming geometry from jsdom/source. Finish this small repair inside this task; no planning-only delivery.

ZERO backend/schema/data/auth/grant/seed/business mutation. Preserve R5–R11 behavior, date conversion/validation, local recovery, all user data/Returns/payout work.
One worker, no delegation/screenshots/polling loop. Persist exact task+receipt, verify base ancestry (docs-only issuance descendant allowed), normal commit/push/fetch parity; no force/rebase/amend/reset/stash.
Update CURRENT pointers: only R12 layout repair; result awaits Architect then owner. Return concise before/after, actual geometry/limits and code/final SHA. No automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
