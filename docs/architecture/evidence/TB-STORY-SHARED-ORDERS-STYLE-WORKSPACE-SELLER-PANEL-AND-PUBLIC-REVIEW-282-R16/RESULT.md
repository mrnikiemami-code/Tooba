# RESULT — TB-282-R16

Status: **PASS**
Decision: `STORY_OWNED_GRID_CALLBACKS_STABLE_REJECT_TYPING_NO_REFETCH_SELECTION_PRESERVED_SERVED_CONTROLLED_PROOF_ZERO_BACKEND_ZERO_LIVE_WRITE`
NOT Architect ACCEPT, NOT owner acceptance.
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW

## Expected base
- Expected-Base-SHA: `d7c36bf836f66365772f2f95a24a48e5b23628ae` (ancestor)
- Claim HEAD: `351c0e73eba611add824473f2d2694cbeacf954e`
- Bridge row: `514e987d-00c8-4421-b5b8-57867d02599f`

## Root cause
`StoryManagementScreen` passed inline `onDenied`/`onError`/`onOk` (and related action lambdas) into `StoryLiveGrid`. Those identities entered `queryAdapter` / column / bulk deps. `AppDataGrid.load` depends on `queryAdapter`; `useEffect([load, defaultQuery])` force-loads on identity change. Reject-reason controlled input therefore rebuilt the adapter and cleared selection on each character.

## Repair
- Parent: stable `useCallback` grid status/delete/submit handlers; submit via `runSubmitRef`.
- `StoryLiveGrid`: callback refs for denied/error/ok/open/submit/delete/admin-op; `queryAdapter` deps only `capabilities.mode` / `reloadToken` / `reviewFilter`; columns/bulk no longer depend on callback identities.
- Lawful ineligible main-storefront row remains skipped (no moderation bypass).

## Pre-fix observation
Mounted R16 fact **failed** before stabilization (~2400ms class; typing/open path unstable under prior inline callbacks / selection race). Owner report + source chain confirmed forced grid reload.

## Proof
- R16 mount **1/1**; retained R14 **4/4** + R15 **2/2**; `tsc` 0
- Served next-dev FE3000 controlled HTTP on real UI: baseline=afterOpen=afterType queryReads **1**; selection «انتخاب‌شده: ۳» retained; eligible 2 / skipped 1; cancel rejectPosts **0**
- Deliberate review-filter change still increments queryReads
- Host5088 skip-bootstrap health/ready 200; DLL SHA256 `D5DF9070…E0599BEE`
- ZERO backend / live Story-Media write (reject/approve/delete POSTs blocked in served control)

## Architecture / i18n
FE integration-only; AppDataGrid contract unchanged; `storyCopy` locale catalogs and VendorShell preserved. No architecture defect beyond the unstable callback wiring that this task closed.

Code SHA: `6450d2684f58b5861ea650404f5e37395d771342`

User-Review-URLs: `/fa/admin/stories`, `/fa/vendor-panel/stories`, `/fa`
