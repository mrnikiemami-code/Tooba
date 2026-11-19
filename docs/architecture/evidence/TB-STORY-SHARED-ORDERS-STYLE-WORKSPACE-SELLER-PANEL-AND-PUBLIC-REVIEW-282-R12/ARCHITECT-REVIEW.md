# ARCHITECT REVIEW — TB-282-R12 — 2026-11-20

Decision: ACCEPT_LAYOUT_REPAIR_FOR_OWNER_REVIEW.
Reviewed code d07ce9d90c0fefca6df53613ffc4a39ac167a393 / evidence main 99c1e1181750c5c32a7e71412c9534d3e7f464bc.
Independent GitHub source/diff/receipts review; Architect did not independently rerun Windows DOM geometry/tests.

Accepted: shared StoryPublicationWindowFields used by create/edit. No-end moved to section legend/header, equal desktop grid columns and single-column mobile. End fields stay mounted, clock disabled and date interaction/value writes suppressed while no-end; retained values; separate checkbox label, nested labels removed. Existing date parsing/null/required-end behavior unchanged.
Three worker-run controlled facts prove requireEnd semantics, create/edit toggle retention, mounted end clock disabled and no nested label. Receipts report active admin edit desktop day/time top differences0px, RTL start-right and no overflow; mobile start-then-end/no overflow and retained end controls; admin creation end controls present/disabled.
Seller geometry not separately measured; same shared component/source reused in VendorShell. Date-picker suppression uses pointer-events plus guarded change; it is not separately certified as native keyboard-disabled. No backend/lifecycle/identity change; no save or business mutation.
Scope acceptance is layout repair only, not full Story workflow, owner visual acceptance or production-readiness. Existing production Html404 prerender blocker persists with next-dev serving. R11 evidence limits/R6 unknown effects retained.

State: AWAITING_OWNER_REVIEW
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW

No new task automatically. Owner checks the previously reported toggle on existing detail and create at desktop/mobile. Preserve all existing Story/date/retry behavior, VendorShell, Returns/payout/user work.
