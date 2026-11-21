# RESULT — TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R19

**Verdict: PASS within the issued A–G scope.**

Decision:
`OWNING_STOP_AND_SELLER_MEDIA_ROUTES_BOUND_STATE_EFFECT_VERIFIED_SUCCESS_HONEST_APPROVED_UNPUBLISHED_SELLER_RETURN_TO_DRAFT_DB_NATIVE_SHOP_DATE_FILTERS_AND_UPDATED_SORT_SLIDE_LEVEL_PUBLIC_PLAYER_WITH_LOADED_DWELL_CAPTION_AND_DIALOG_SEMANTICS_EXPLICIT_MEDIA_REPLACEMENT_PRESERVING_OLD_MEDIA_ZERO_MIGRATION_ZERO_LIVE_WRITE`

This is a worker PASS. It is **NOT** Architect ACCEPT, **NOT** owner acceptance, **NOT** production readiness.
`automaticNextImplementationTask: NONE` · `Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW`.

- Expected-Base / claim HEAD: `d3de64c2c6692b551c8193e842bdd04fe99ceded` (identical — no source drift arrived).
- Final implementation SHA: `8fb403cfe17b40729f7f582acdf9aea5a238ee89`.
- Bridge row `80faea2c-6881-40fa-a0d8-4bd3a4439179`, claimed `2026-11-21T03:24:18Z`.
- User-review URLs: `/fa/admin/stories`, `/fa/vendor-panel/stories`, `/fa`.

## What was actually wrong, and what changed

| Gate | Root cause found in source | Repair |
| --- | --- | --- |
| A1 admin stop | the binder accepted only `/v1/seller/stories/{id}/stop`, so every admin stop died as `route_binding` and `AdminDisableAsync` was never reached | role-specific bind: seller `/stop`, admin `/disable` (both anchored, path must contain the target id) |
| A2 seller upload | the UI posted `/v1/seller/media/upload-story`; the module maps the group root `/v1/seller/media` (POST), so the upload 404'd | executor + edit flow + binder all use `POST /v1/seller/media` |
| B success truth | any 2xx with a same-id snapshot counted as done — an unchanged `Draft` returned to `approve` was reported as success | `effectSatisfied()` asserts the effect the frozen action owns, and only for fields the reply actually carries; otherwise `story_state_unchanged` (uncertain, intent retained) |
| B refusal truth | an HTML/proxy 4xx was treated as an authoritative no-effect refusal and cleared the frozen intent | only a parseable platform envelope with an owned error code clears; otherwise `story.response.unverified` + uncertainty |
| C display | `Approved` + not-public fell through to «پیش‌نویس», and `Active` always read as published | distinct «تأییدشده، منتشر نشده» / "Approved, not published" |
| D return-to-draft | `Approved + Draft` (never published) offered no repair action at all: `IsSellerContentEditable()` is false, so the seller was stuck with a story it could not edit or publish | seller policy now offers `return_to_draft` for `Active/Scheduled/Disabled/Draft` under `Approved`; the existing owning command executes it |
| E grid | the policy whitelist rejected `shopDisplayName/shopName/startAt/endAt/updatedAt` (400 → the UI silently dropped the filter) and `updatedAt` was not sortable; nullable dates had no translator | whitelist + lawful operators, `updatedAt` sortable, `ApplyNullableDateFilter` translated to SQL for `blank/notBlank/on/before/after/between` |
| F player | video-ness was decided per story (`isVideo` fallback), so a JPEG cover of a video-bearing story waited for `ended` and hung; the photo timer started before the image painted | `itemIsVideo()` per slide (type, then URL extension); dwell gated on `onLoad`; caption rendered; dialog semantics + focus/body-scroll restore; visibility hidden also holds |
| G replacement | edit mode could only keep the old media; there was no lawful way to replace a slide file, and the duration field followed the OLD slide's type | explicit replace file (upload first → owning `update_item`); old media untouched until both succeed; media type from the upload receipt; duration field follows the pending/staged type |
| — product CTA | seller product options offered owned-but-not-public products whose CTA href resolves to `null` | options are intersected with publicly cardable products when the Catalog card lookup is available (optional dependency, no new required edge) |

## Receipts (all run in this pass)

| Check | Command | Result |
| --- | --- | --- |
| Focused owning-method proof | `dotnet test … --filter FullyQualifiedName~StoryWorkflow282R19Tests` | **3/3 passed** |
| Story regression set | `dotnet test … --filter FullyQualifiedName~Story` | 174 total · 168 passed · **6 failed — all 6 fail identically at HEAD** (baseline re-run with the work stashed: 6 failed / 10 passed of the same 16-test subset) |
| Backend Release build | `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors** (234 warnings, pre-existing) |
| Frontend types | `npm run typecheck` | **0 errors** |
| Story unit + mount | `npm run test:stories` | **22/22** then **6/6** |
| R19 mounted proof | `npx tsx --tsconfig tsconfig.mount-tests.json --test app/stories/management/story-management-282r19.mount.test.ts` | **5/5** |
| Served pages | GET `/fa/admin/stories`, `/fa/vendor-panel/stories`, `/fa` | **200 / 200 / 200** |
| Served chunks | dev chunk `app/admin/stories/page.js` | carries «تأییدشده، منتشر نشده» ×1, `/v1/seller/media` ×3, `/disable` ×11, `return-to-draft` ×3, «تعویض فایل این اسلاید» ×1; storefront chunk carries `story-caption` |
| Stale route | `rg "seller/media/upload-story" src` | only the negative-control assertion inside the R19 test |

## Focused backend proof detail (`StoryWorkflow282R19Tests`)

1. `Approved_draft_story_offers_and_executes_seller_return_to_draft` — on the REAL `StoryDirectory`: seller create → add
   owned media → submit → `AdminApproveAsync` yields `Approved + Draft`; the seller snapshot offers `return_to_draft`
   and not `approve`/`publish_now`; `SellerReturnToDraftAsync` returns `Draft` at `version+1` and records the
   `seller_return_to_draft` occurrence with the owning story and shop.
2. `Admin_grid_policy_accepts_shop_and_date_filters_and_updated_sort` — the four previously-400 fields normalize and
   `updatedAt desc` survives.
3. `Admin_grid_query_applies_nullable_start_date_filter_and_updated_sort` — two real stories (one with `StartAt`, one
   without) through the real engine: `startAt notBlank` returns only the scheduled-window row, sorted by `updatedAt`.

## Zero-change guarantees

- **Zero** migration / DDL / seed / backfill / reset / restore / config / grant change.
- **Zero** live Story or Media business write: this pass issued no create/update/approve/reject/publish/stop/delete POST
  to Host; the served checks were GET page/chunk reads only.
- Story schema, occurrence tables, Media assets and Party/access data are untouched.
- Existing VendorShell, menu, `story.view` and route `/vendor-panel/stories` are unchanged (no parallel panel/menu/login).
- R16 typing/selection, R17 toast presentation and R18 approval-readiness behaviour are preserved.

## Architecture findings (reported, not silently "fixed")

1. **Pre-existing module-boundary violation.** `Tooba.Story.Infrastructure.csproj` carries a direct
   `ProjectReference` to `Tooba.Catalog.Contracts`, so the module's own AMC W6 guard
   (`Story_has_zero_foreign_Application_Infrastructure_Domain_coupling`) fails. This reference exists at
   `d3de64c2` and is **not** introduced by R19; the W1 production-edge guard still passes. R19 added one more Catalog
   Contracts port usage *inside the already-existing* `StoryProductOptionsAdapter`, i.e. it followed the current
   (non-conforming) shape rather than widening the violation with a new dependency. The clean remediation is an
   Application-owned port in `Tooba.Story.Application` plus a Catalog/Host-side adapter registered in composition —
   a module relocation that is outside the issued scope, so it is reported for a separate bounded task.
2. **Pre-existing test drift (6 failures, identical at HEAD).** `StoryFoundationTests` (×2) use placeholder media ids
   that the real Media port refuses (`story.media.unavailable`); `StoryMarketplace281R1DirectoryTests` calls
   `AdminApproveAndScheduleAsync` without the now-required occurrence key (`story.occurrence.required`);
   `ProductHistoryTests` reads a Catalog file that no longer exists at that path; `ContentArticlePublicationWorkflowTests`
   expects `InvalidOperationException` while the product now raises `content.publish.not_ready`. None of these touch a
   line changed by R19.
3. **Product-option narrowing is a behaviour change by design**: the seller picker no longer offers owned-but-unpublished
   products, because the Story CTA would render an unreachable href. It is wired as an optional dependency, so a host
   without the Catalog card lookup keeps the previous behaviour instead of failing.

## Known limits

- No browser/geometry measurement in this pass: the served evidence is HTTP status + emitted chunk content, and the
  mounted evidence is `tsx` DOM behaviour (jsdom), not pixels. No RTL/overflow re-measurement was performed.
- The public player proof covers slide-level photo vs video detection, loaded-photo dwell gating and caption rendering in
  the mounted component; it is not a real `<video>` runtime `ended` measurement.
- Grid filters were proven on the real engine over the controlled InMemory provider (SQL translation is asserted by
  construction — nullable dates go through `EF.Property`, not in-memory compilation — but not executed on PostgreSQL).
- Owner approval/publication regression for the legacy `MediaUrl`-only record is unchanged from R18: that record still
  needs a seller owned-asset re-upload.
- The `Html404` production-build blocker and the pre-existing production-build limitation are unchanged; serving here was
  `next dev` on port 3000 with Host 5088 already running.

## Final push
- Issued envelope arrived concurrently as `1428cda05`; merged without source drift (`git diff 8fb403cfe..HEAD -- src/` empty), final pushed HEAD `d7d86e4af`.

