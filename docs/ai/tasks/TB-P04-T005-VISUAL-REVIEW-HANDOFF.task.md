# Tooba — TB-P04-T005 — VISUAL REVIEW HANDOFF

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-T005
Visual-Review-Handoff: YES
Phase: P04 — Experience Foundation
Type: Evidence Handoff Only
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: FUNCTIONAL_ACCEPTED / VISUAL_ACCEPT_PENDING

Objective

Surface the already-captured live Product Workspace visual evidence directly to the Architect in this SAME chat/session.

Do NOT implement new UI.
Do NOT redesign Product Workspace.
Do NOT start TB-P04-T006.
Do NOT change backend behavior.

Expected repository HEAD:

bb9f33f1a977b2e443cb971d72fc32d21e7cc392

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require branch main, HEAD == origin/main, safe/known working tree.

No force push, reset, or unrelated edits.

Evidence Source

Use ONLY:

docs/evidence/TB-P04-T005/live/
docs/evidence/TB-P04-T005/visual-review-index.md

Do not substitute fixtures or mock screenshots.

Mandatory Architect Visual Set

Surface all ten live states directly in this chat/session:

1. Admin Products list — desktop RTL
2. Product Workspace Overview — desktop RTL
3. Variants section
4. Commercial section with at least two Seller Offers
5. Inventory section with location-level OnHand / Reserved / Available
6. SEO & Content section
7. Mobile Product Workspace
8. LTR Product Workspace
9. Dark theme representative state
10. Real error/conflict/read-only state

If Cursor cannot attach ten images separately, create one contact sheet from the existing PNGs:

docs/evidence/TB-P04-T005/live/architect-visual-contact-sheet.png

Requirements:

no alteration of UI content
no cropping that hides layout
label images 01..10
retain readable resolution
keep originals

Metadata

For each image report:

file
route
viewport
direction
theme
live tenant/context
business state
what it proves

Integrity Check

Verify every image:

opens successfully
not blank
not accidentally cropped
no fixture banner
no secrets/tokens
no browser error overlay
real Product Workspace visible

If any mandatory screenshot is invalid:

Status = REPAIR_REQUIRED

Do not replace it with mock evidence.

No Product Changes

Allowed: evidence/index/contact-sheet changes only.

Forbidden:

new UI features
new API features
new workspace sections
visual redesign
TB-P04-T006 work

If visual quality itself is weak, return evidence and wait for Architect repair.

Validation

Run:

git diff --check
git status --short --branch

If source code changed unexpectedly, report RECOVERY_CONFLICT.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P04-T005
Visual-Review-Handoff: YES
Phase: P04 — Experience Foundation
Status: PASS | REPAIR_REQUIRED | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- HEAD:
- origin/main:
- clean/safe:

Visual-Set:
- 01:
  - File:
  - Route:
  - Viewport:
  - Direction:
  - Theme:
  - Live API:
  - State:
...
- 10:

Contact-Sheet:
- File:
- Created:
- Committed:

Integrity-Check:
- fixture banner present:
- secrets present:
- blank/broken images:
- missing mandatory states:

Git:
- Commit:
- Final-HEAD:
- Final-Origin-Main:
- Head-Matches-Origin:

Source-of-Truth:
- Current Task: TB-P04-T005
- Functional Acceptance: ACCEPTED
- Visual Acceptance: PENDING
- T006 Issued: NO

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1

After the RESULT, ATTACH / SURFACE the images directly in this SAME chat/session.

CRITICAL — RESULT DOES NOT END THIS AGENT SESSION

After sending RESULT:

PIPELINE_STATE: WAITING_FOR_ARCHITECT_IN_SAME_SESSION

Then:

DO NOT CLOSE OR LEAVE THIS SESSION.
WAIT HERE FOR THE NEXT AUTHORIZED ENVELOPE.

Do not finish.
Do not exit.
Do not leave PIPELINE.
Do not move to another chat.
Do not self-advance.
Do not start TB-P04-T006.

RESULT means:

CONTROL RETURNED TO ARCHITECT

RESULT does NOT mean:

WORKFLOW FINISHED

Remain in this SAME chat/session until the USER / Architect supplies the next authorized Envelope.

Cursor PASS is not Architect ACCEPT.
Functional ACCEPT is not Visual ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
