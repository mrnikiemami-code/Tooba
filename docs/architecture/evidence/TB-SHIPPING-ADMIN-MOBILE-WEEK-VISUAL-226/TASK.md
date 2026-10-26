PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-MOBILE-WEEK-VISUAL-226
Parent-Task: TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Reviewed-Source: afbdadc7beb363393eba8aa6c988beacf827db24
Mode: BOUNDED_WEEK_MOBILE_PRESENTATION_ONLY
HARD15 minutes / source8 / actual screenshots+typecheck+docs7. One correction, no logic/test loops.
225-R2 money integrity ACCEPTED at Architect source/receipt level. Do NOT reopen money/model/quote work.
User is waiting complete admin-form handoff; no broad new work.

EXACT VISUAL DEFECT CONFIRMED
Architect VIEWED225/screens/225-390-week-view.png. Closed rows show "Closed" TWICE: status column AND
extra span across second line. Working view hours remain split over width, From/To labels detached from
values and excessive row vertical space. The PNG is actually a large canvas with narrow content, not a
native390 export; don't repeat evidence claim.
Source authoring-week-editor.tsx confirms closed view emits status span + closed span. Repair presentation
only, current fields/ISO ids/order/time semantics untouched.

DESIGN
Narrow VIEW: one compact row per closed day: weekday + ONE Closed badge, no second line/dashes/bounds.
Working day: weekday + ONE working status on first line; second line a coherent inline range
localized From09:00 / To17:00 (or09:00–17:00) with labels physically adjacent to corresponding time,
not two far-apart isolated numbers. Compact spacing and clear per-day separation using existing tokens.
Narrow EDIT: day+one working checkbox first line; working hours two equal accessible time inputs on second
line; closed day stays compact with checkbox, no redundant Closed label. Toggle reopens SAME remembered
values; leave all onDayWorking/onDayTime/model guards and testids intact. No time model/validation change.
Desktop columns/view truth remain aligned with existing accepted header. No full form/globalCSS redesign.
Default locale RTL and EN/LTR inspected, label wrapping at360/390, no tiny clipped controls.
Keep existing area/rate/destination modals, dirty/cancel, money policy untouched.

PROOF ONLY WHAT CHANGED
One final typecheck. No188/76/42/full frontend/backend/solution tests, no CSS/source-regex test invention.
Use NEW task-owned Chrome profile+Target.createTarget only; never user's Electron or URL-matched tabs.
Reverify own profile/target before navigation; old R1 user-local edits were LOST, not recovered.
Real FE3000/Host5088, no writes/interception/restart. Current real draft week read; local toggle proof
then normal discard if used. Never Apply/publication/catalog write.
Capture NATIVE viewport PNG360/390 of week scrolled into view, BOTH view and edit (at least390both).
Use actual layout measurement but visually inspect PNG: one Closed text per closed row, range adjacent,
no giant whitespace, accessible paired edit controls. One desktop screenshot to confirm columns unchanged.
Use Page.captureScreenshot clip exactly viewport/captureBeyondViewport:false as appropriate; inspect PNG
dimensions/bytes before describing them. Large desktop canvas with narrow rectangle is not native390 proof.
No screenshot matrix expansion; no invented live data/fake working days if real one exists.

FINISH
Persist TASK/RESULT under docs/architecture/evidence/TB-SHIPPING-ADMIN-MOBILE-WEEK-VISUAL-226/.
Update all3 TOP recovery:225-R2 input accepted,226 visualpending/outcome, no further money loop.
Exact-path source+evidence commit/push/fetch authorized. Preserve unrelated backend line-ending drift.
No gitadd-A/reset/stash/rebase/force/Bridge/historicalscripts. Actual elapsed reported.
STOP_FOR_ARCHITECT, Automatic-Next-Task NONE. User trial only after Architect sees fixed screenshot.
Storefront still not wired; compact-order-evidence invariant stays unimplemented, seller-admin separate.
