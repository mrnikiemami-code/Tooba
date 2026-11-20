# RESULT — TB-282-R18

Task: `TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R18`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: cursor
Expected-Base-SHA: `1d02e87d49049e96f4d175284b96165b4c3b4777`
Claim HEAD (docs-only issuance): `583ce6e6cc34924320cf1f79c90e12f838d14e45`
Final implementation SHA: `0ba8caa56f0175a6d3b8a82766a6e914d4ad5fa2`
Bridge row: `d3fc091e-0cf8-4f43-b960-f2c022426655`

Status: **PASS** within the issued A–B scope. NOT Architect ACCEPT, NOT owner acceptance.

Decision:
`STORY_FEEDBACK_3000MS_NO_PAUSE_SEMANTIC_SEVERITY_COLLAPSED_USER_INVOKED_RECOVERY_SINGLE_REFUSAL_REASON_AND_APPROVE_OFFERED_ONLY_WHEN_THE_REAL_APPROVE_COMMAND_ACCEPTS_ZERO_MIGRATION_ZERO_LIVE_WRITE`

## A — feedback that actually disappears, correct severity

| Fact | Evidence |
| --- | --- |
| Same toast mechanism/container/theme | `story-toast.ts` calls `toast.success/error/warning/info` on the existing `react-toastify` container from `AppProviders`; no `ToastContainer` prop, theme or CSS change |
| App's 3000ms duration, no hover/focus extension | Story toasts pass `{ autoClose: 3000, pauseOnHover: false, pauseOnFocusLoss: false, closeOnClick: true }` per toast — the global container keeps its own defaults |
| Served expiry measured | one toast after clicking «تأیید» for the non-ready story: still 1 at **4149 ms**, **0 at 4450 ms** after the click, with no manual close and no focus/hover interaction. It never re-appeared (single sample series, count never >1) |
| Served severity | toast class `Toastify__toast--error` (1 error, 0 info, 0 warning, 0 success) for a **cannot-run** action; previously `toast.info` |
| No permanent banner | `story-pending-recovery` is now `<details>` (collapsed by default) with a count + «باز کردن»; served page with no stored command renders **0** such elements (no yellow card) |
| Storage preserved | unresolved commands stay in the existing intent store and replay exactly; mount test opens the control and the stored approve envelope is still retryable (`story-recovery-retry` present, no auto-send) |
| Single refusal names the reason | `formatAdminBatchToast` returns «تأیید نشد: <reason>» with `error` severity for a one-row definite failure; reason comes from a server-code→Persian map (`story.content.notReady`, `story.media.forbidden`, `story.version.stale`, …) or the server message, never a bare «۱ مورد انجام نشد» |
| Unknown stays uncertain | transport/uncertain outcomes → `warning` + «نتیجهٔ … نامشخص است»; never success/failure |
| Bounded bulk copy | grouped reasons capped at 3 groups, counts only, no titles; numbers in Persian digits |
| Pre-send blocks are definite, not unknown | in-flight/storage/envelope blocks were previously counted as `unknown` although nothing was sent; they now report `failed` with a plain reason (no wire effect) |

## B — real approval path: diagnosed cause and fix

Read-only snapshot of the owner's story `01a04246-b309-7000-b1de-681552e70532`
(admin dev actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db`, GET only):

```
reviewStatus=Submitted contentReady=False origin=Seller status=Draft version=3
actionCodes=update,add_item,delete,reject        (after fix)
item: mediaAssetId=<empty>  mediaUrl=/images/stories/2.jpg  mediaType=image
```

**Cause (proven on the owning method, not inferred from screenshots):**
`StoryDirectory.AdminApproveAsync` (and `AdminApproveAndPublishAsync`) call
`EnsureStoredContentAuthorizedAsync`/`HasPlayableContent` before any review mutation. A shop story whose
item keeps only a legacy `MediaUrl` and no owned `MediaAssetId` is therefore never approvable
(`story.content.notReady`). `StoryPublicationPolicy.ProjectActionCodes` nevertheless advertised `approve`
for every `Seller + Submitted` row, so the workspace offered an action the server always refuses; the
refusal then surfaced as the generic «۱ مورد انجام نشد» the owner saw.

**Fix (Story-owned policy only; no lifecycle/version/media/ownership gate weakened):**
`ProjectActionCodes` now adds `approve` / `approve_and_publish` / `approve_and_schedule` only when
`ready` (`HasPlayableContent`) is true. `reject` stays offered for exactly this case because it has no
readiness gate and is the lawful repair path (admin rejects → seller replaces the slide with an owned
asset → resubmit → approve succeeds). Nothing is approved falsely, no asset is re-added automatically and
no stored row is mutated.

**Owner test on the real method** (`StoryApprovalReadiness282R18Tests`, controlled InMemory harness, no
external DB, `committedRows=0`):
- lawful Submitted story with an authorized media asset → action codes include `approve`; real
  `AdminApproveAsync` → `Approved`, version+1, `ReviewedByActorUserId` set, approve/reject codes removed,
  one `admin_approve` occurrence bound to story + seller.
- legacy URL-only fixture (written directly into the controlled store because current commands cannot
  produce that shape) → `ContentReady=false`, no `approve*` codes, `reject` present; real
  `AdminApproveAsync`/`AdminApproveAndPublishAsync` throw `story.content.notReady` with **zero** occurrence
  rows and version unchanged; admin reject → seller `SellerUpdateItemAsync` with an owned asset → resubmit
  → the same owning approve command succeeds.
- Seller self-approve remains impossible (seller surface has no approve route; unchanged).

**Replay safety retained:** loss/unverified 200 stays stored as uncertain; a genuine same-command
completion clears only its envelope. A terminal 4xx refusal now clears its own envelope (`story-executor`
`isTerminalRefusal`), so a stale command cannot mask recovery as a later bogus storage/conflict outcome,
and 408/425/429 stay retryable-uncertain.

## Served (read-only) — 5088 + next-dev 3000

```
Host 5088      pid 23016, Tooba.Host.dll SHA256 CF23A240C168702BFD46DF9E8372F314D45C21EF9CD50E559E41A9B8ACB79925
               /health 200, /health/ready 200, /__platform-commerce store-alpha/SingleStore
               startup log: AccessControl InMemory projection recovery eligible=True users=3 tenantMembers=1 partyMembers=2
Next 3000      next dev (no production BUILD_ID; Html404 production build blocker unchanged)
/fa/admin/stories          6 rows, no denial, overflowX 0 at 1440 and 390, 0 permanent banners
/fa/vendor-panel/stories   5 rows in VendorShell, no denial, overflowX 0 at 390 and 1440
```

Zero live Story/Media mutation: DB row still `review_status=1` (Submitted), `status=0` (Draft),
`version_token=3`, `updated_at=2026-08-27 06:00:00+00`, `reviewed_by_actor_user_id=NULL`,
`story.command_occurrences` for that story = 0, items = 1 and `media_asset_id IS NULL` = 1.

## Scope / safety

ZERO migration/DDL/seed/backfill/reset, ZERO grant/auth/bootstrap change, ZERO config/appsettings/port/key
change, ZERO live Story/Media/approval/upload request. VendorShell, i18n registry (fa + en copy keys),
date layout, Returns/payout/user data and R6 startup effects UNKNOWN preserved.

## Known limits / deviations

- Served browser proof is a controlled read-only UI measurement on next-dev; the definite server refusal
  path is proven in the mounted test, not by a live approve POST (no live moderation was authorized).
- The owner's stored story is genuinely ineligible (no owned media asset). The workspace now says exactly
  why and offers `reject`; repairing that specific business record requires the seller to upload the
  slide again — not performed here.
- `frontend-source-size.guard` reports 28 pre-existing violations. `StoryManagementScreen.tsx` was already
  oversized at HEAD (2714 lines) and is now 2727 (+13 net) while literals moved into the copy catalog; no
  baseline was widened.
- `dotnet test --filter "~Story"` shows 12 failures that all sit in files untouched by R18 (Story
  development seed URL-only fixture, a 281R1 test missing an occurrence key, Story AMSC/AMC architecture
  guards about Catalog coupling/route maps, plus Content/Product history tests matched by the substring).
  R18's own focused suite and the Story domain/relay tests pass; these are pre-existing and unrelated.
- R16 typing/selection and R15 loss/epoch proofs were not re-derived here; their tests remain and the R17
  mount suite still passes.

STOP_FOR_ARCHITECT_AND_USER_REVIEW · automaticNextImplementationTask: NONE
