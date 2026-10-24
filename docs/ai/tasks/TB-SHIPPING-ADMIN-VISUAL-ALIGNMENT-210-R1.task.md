PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1
Parent-Task: TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 705921b85a9675dd6af994213ef908f54a4f237d
Mode: BOUNDED_MOBILE_WEEK_LABELS_AND_TRUTHFUL_VIEW_COMPLETION
Hard-Timebox: 12 minutes; stop new implementation at minute7; final5 for inspection/evidence/commit/push.
One correction maximum. If not done stop with honest PARTIAL. No automatic next task.

USER GOAL
Complete actual requested week alignment quickly. User rejected UI10/20; don't self-grade.
210 desktop edges/chips/seven-row alignment are accepted. DO NOT redesign or replay desktop/functional work.

READ FIRST
Top AGENTS + canonical recovery +210/ARCHITECT-REVIEW.md.
Read applicable existing frontend skill/standard, reuse accepted feature/design-system.
Prior explicit local-development authorization persists. No approval question.

EXACT UNFINISHED ITEMS

Current mobile week actually has three lines per day: day/status; start fullwidth; end fullwidth.
Both fields have no visible From/To labels when the desktop header is hidden.
Implement TWO-line layout: day/status first; paired equal-width start/end with visible localized labels second.
Give each time input an accessible label including weekday+From/To. Preserve existing data-testid.
Desktop same shared column x/width geometry, seven rows and existing time/value/callback semantics.
Use CSS/responsive approach consistent with feature; don't add a framework or viewport state store.
In VIEW the current range is placed under Status while From/To headers remain empty.
Render textual working/closed status under Status and each working bound in its corresponding column;
closed day shows honest dash/empty time display, no editable controls, no policy/default change.
Keep mobile view two-line rhythm if times displayed. No duplication of formatted time range.
REAL evidence: previous week-edit-390x844.png shows only TOP General, NOT week. Scroll actual
week into view in isolated browser and capture visible mobile rows/paired labels. Capture actual390x844
viewport bytes, not1440 canvas with mobile rectangle. Bounded EN/LTR week check via canonical localecookie,
not content-language switch. No broad locale/viewport matrix.

SCOPE
Existing feature ui/authoring-week-editor.tsx + messages only if needed; minimal related presentation
file only if strictly necessary. No backend/API/DTO/Domain/model/session/savepolicy/transport change.
No price/timezone/DST/cutoff rules, no value default/rewrites; preserve stored fractions/remembered closed
hours and callbacks. No globalCSS/designsystem/config/package change.

LIVE TARGET / NO WRITE
http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
FE3000/Host5088 already up. Use isolated browser; do not reload user's unsaved tab.
ZERO Apply/POST/PUT/PATCH/DELETE/seed/migration/reset/backendbuild/restart. Preserve actual revision and
stored content. Prior rev2 observation isn't authority to reset current user edits.
Task may open Edit and inspect local controls, exit without writing.

FINISH
One typecheck if source changed. No unit tests of CSS/source regex, no fullsuite, no save replay.
Actual pixels: FA mobile week + EN/LTR affected week + one desktop week check confirming retained columns.
Record viewport dimensions, row layout and labels; if screenshot does not show week DON'T claim it.
No user Visual ACCEPT claim. Record real elapsed/timebox deviations honestly, no invented timestamps.
Evidence docs/architecture/evidence/TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1/RESULT.md + images.
Append210 correction (mobile screenshot didn't show week; three not two lines; EN not executed).
Update TOP canonical recovery +AGENTS checkpoint only, preserve historical sections.
Scoped commit/push fetch parity; leave shared runtime and user data alone.
Return worker result; STOP_FOR_ARCHITECT. Do not emit further envelopes or repeat190–209.
