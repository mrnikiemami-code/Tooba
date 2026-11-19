# TB-282-R12 RESULT

Status: **PASS** within issued layout-only scope (worker). NOT Architect ACCEPT. NOT owner acceptance.

Decision: `PUBLICATION_WINDOW_NO_END_HEADER_STABLE_END_COLUMNS_CREATE_EDIT_RTL_DESKTOP_MOBILE_GEOMETRY_VERIFIED_ZERO_BACKEND`

## Before / after
- Before: «بدون پایان» sat inside the end column above end date/time, so unchecking pushed end fields below start (misaligned rows; nested `<label>`).
- After: shared `StoryPublicationWindowFields` — heading + no-end in fieldset legend/header; start/end equal `md:grid-cols-2` columns; end controls always mounted, disabled+dimmed when no-end checked; values retained; no nested labels.

## Geometry (served, read-only)
- Edit `…/stories/01a04211-7dd7-7000-83dc-5c79db49abc4` at **1440×900**: start/end day tops equal (diff 0); clock tops equal (diff 0); start right of end (RTL); overflowX=0; toggle keeps end mounted.
- Same form at **390×844**: start then end vertical order; overflowX=0; end stays mounted when no-end on.
- Create `/fa/admin/stories/new`: no-end in header; end mounted+disabled by default; nested label absent.

## Proof
- FE R12 mount **3/3**; retained R10 schedule/copy units OK
- ZERO backend/schema/auth/live Story mutation
- Next: `next-dev` :3000 (production Html/404 blocker unchanged, out of scope)

## Limits
- Geometry measured via CDP on active admin forms; seller VendorShell shares same component (source). No screenshots. No save exercised.

Final code SHA: `d07ce9d90c0fefca6df53613ffc4a39ac167a393`

automaticNextImplementationTask: NONE  
STOP_FOR_ARCHITECT_AND_USER_REVIEW
