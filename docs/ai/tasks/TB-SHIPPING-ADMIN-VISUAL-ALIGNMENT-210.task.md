TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Parent-Task: TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2
Expected-Base-SHA: 3f14b3beebddc5265fdd23b078bfd6ba38ff92b7
Mode: BOUNDED_USER_SCREENSHOT_GRID_TABS_AND_WEEK_ALIGNMENT_REPAIR
Hard-Timebox:20 minutes
Stop-New-Implementation:minute13; final7 typecheck/actual visual inspection/evidence/commit/push
Automatic-Next-Task:NONE
Stop-State:STOP_FOR_ARCHITECT

User request — VISUAL REJECTION, not a functional proof request

User has supplied TWO current screenshots after209-R2 and says “tabs/holiday controls not aligned;10/20”.
Current UI has NO user Visual ACCEPT. Do not defend earlier changes as professional just because tests pass.
Fix visual alignment in the existing editor. This is authorized by actual user feedback, not an automatic proof task.

Architect inspected both supplied images:

Main two tabs share one outer width, but General inner max-w-4xl and Regions full outer width form
different visible content axes. Type is max-w-sm outside the text card, so type/name/settings/week edges differ.
Language chips have different active/inactive border/height styling.
Week uses md:grid-cols-2 DAYS with inner justify-between. Day labels on one side, working checkboxes
on opposite edges, time pairs on another line; across the7days their columns do not line up.
Region toolbar/actions/title/rate labels need the same edge/baseline rhythm.
No more label-renaming-only handoff.
Scope / source owner

Read root AGENTS, TOP recovery, frontend-feature SKILL and FRONTEND-ARCHITECTURE-STANDARD.
Use existing design-system/tokens/forms/spacing. Same feature:
ui/authoring-screen.tsx, authoring-general-tab.tsx, authoring-week-editor.tsx,
authoring-groups-editor.tsx / rates-editor only if necessary for common alignment.
No backend, DTO, API, session/model/save policy, new library/framework/globalCSS, legacy grid/sidebar redesign.
Preserve real user data, current selected tab/form intent and pending edits; user's browser must not be reloaded.
Use isolated browser profile for task inspection.

Required exact layout — one grid, no guesswork

ONE content width/alignment contract for BOTH main panels:
header/tabbar/General fields/Regions cards use consistent outer inline edges and paddings.
Remove the nested narrow/offset General-only layout. Constrain the whole page once instead.
Type control and name/description cards align on the SAME form axis; a control may be narrower within
an explicitly aligned grid, never a floating unrelated max-w box.
Independent settings use equal-height cards with legends/options on common baselines.

Main tabs and content-language tabs:
consistent explicit height, vertical centering, padding and border thickness in selected/unselected
states; active border/color change must not shift geometry. RTL/LTR logical alignment, keyboard/focus preserved.
Content languages remain database-driven, no hardcodedfa/en list or replacement of real labels.

WEEK = SEVEN CONSISTENT ROWS, not two columns of independent day cards:
desktop header + common columns [day name] [working/closed control] [start] [end].
Every day label starts at the same x, every working checkbox/status control shares the same column,
all start inputs and all end inputs share equal widths/heights/x positions.
Reuse existing accessible checkbox/switch pattern with label; no raw floating checkbox separated from its day.
At390px each day may use a consistent two-line grid (day/status together, from/to together) without overflow.
View mode uses the same weekday rhythm as text/status, no editable controls.
Do not change weekday ids/order semantics, remembered closed-day hours, validation, fractions, timezone value,
or one-root-week authority. No schedule policy or automatic time defaults.

Form rhythm:
existing readable font sizes, uniform control/button heights, labels above fields, consistent16/24 spacing
using existing tokens/classes. Align area toolbar/title/fees and avoid tiny detached action chips where changed.
No new banners, decorative cards, explanatory technical prose or unrelated visual redesign.
Keep narrow viewport padding and primary actions reachable; no horizontal overflow.

Real inspection is the completion gate

Runtime remains FE3000/Host5088. Same populated real draft:
01a16eeb-70b3-7000-87c2-564c82835496
/admin/shipping-services/authoring/{id}?mode=view
No Apply, HTTP writes, seed, migration, backend build/restart; zero domain mutation.
Before/after values/revision must be unchanged; current actual state, not assumedrev2.

Inspect BOTH General and Regions EDIT at the SAME viewport1440x900 and same sidebar state.
Actual screenshots must be matched viewport, not giant canvas around an emulated390 viewport.
Inspect week fully: all7rows, working/closed controls and from/to columns.
Record DOM bounding boxes for main panel edges, tab heights, seven status controls and start/end inputs;
within each intended column, x/width alignment difference<=2CSSpx desktop.
Don't claim measurement from screenshot pixels alone; read actual DOM boxes.
Inspect390x844 for General/week and Regions; no horizontal overflow or clipped actions/labels.
Add EN/LTR check only for the affected alignment (canonical cookie locale); no broad visual matrix or old A–E replay.
One npm run typecheck. No full tests/suite/source-regex tests for CSS.
Focused existing week/form regressions only if behavior was accidentally touched; otherwise no new logic tests.
One correction maximum. If unfinished at13min, stop implementation and report exactPARTIAL; do not exceed20min
for polish/evidence and relabel timingPASS.
Keep Host/FE running and stop only task-owned browser/harness; preserve user's active browser and unsaved work.
Report / recovery

docs/architecture/evidence/TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210/RESULT.md:
actual source/components changed, before->after alignment decision, exact viewport+bounding boxes,
screenshots General/Regions/week actually inspected at matcheddesktop andmobile,
unchanged revision/data, typecheck status, actual claim/implementation-stop/finish.
User Visual ACCEPT remains PENDING until user judges; do not grade your own UI or claim “professional” from measurements.
TOP recovery + AGENTS current pointer: user scored10/20/rejected alignment; active210, preserve accepted functional209.
No migration/save/regression replay. Scoped commit/push/fetch parity; stop for Architect review.
