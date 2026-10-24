# TB-208 — Local stack refresh and real existing-draft trial handoff

Status: **READY_FOR_USER_TRIAL** (bounded to the existing-draft authoring slice on the local development stack).
Parent: TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207. Base: `822a8ea8de14630c84fb62a241b2c058bc27a4c5`.
Worker mode: `AUTHORIZED_LOCAL_DEV_MIGRATIONS_HOST_REFRESH_AND_LIVE_EDITOR_HANDOFF`.

This record states only what was executed and observed. It is not release, quote, whole-method-creation,
production-activation or module certification evidence.

## 1. Target identity (reconfirmed before any write, credentials not recorded)

- Host: `127.0.0.1:5088`, environment `Development`.
- Database: `127.0.0.1:5432/tooba_alpha`, connection alias `tenant-alpha`, edition `SingleStore`, tenant `store-alpha`.
- Identity was re-read from the runner's own resolved plan (`edition=SingleStore tenantId=store-alpha
  database=tooba_alpha connectionRef=tenant-alpha`), not guessed from a URL or a settings file name.
- No secret value appears in this file, in any command line, or in any committed script.

## 2. Migration plan and apply (existing `Tooba.MigrationRunner`, tenant-scoped)

The runner was built from current source into a task-owned isolated output
(`D:\temp\tb208-runner\out`; `dotnet build … -c Release` → **0 errors / 42 warnings**), because the
previously checked-out runner binary predates the accepted 196–197 migrations.

`plan --tenant store-alpha` was read for the **complete** descriptor set (33 modules). Pending set:

| Module | Pending |
| --- | --- |
| Shipping | `20261022055446_AuthoringDraftPersistence`, `20261022065333_AuthoringGroupRateRows`, `20261022075711_AuthoringRateRowConstraintParity`, `20261022081351_AuthoringPresentationTranslations`, `20261022085811_AuthoringWeeklySchedule`, `20261022093803_AuthoringMethodSettings`, `20261022195929_AuthoringDraftTypeAssignment` |
| Fulfillment | `20261022101415_ShippingTypeCatalog`, `20261022182208_AddFulfillmentOutboxPendingIndex` |

Total pending = **exactly 9**; every other descriptor reported `pendingMigrations: []` and
`succeeded: true`. No unlisted pending id, no unknown module, no divergent history — the pre-apply
gate of the issued task is satisfied by observation, not assumption.

`apply --tenant store-alpha` exited **0**; all 33 module results reported `result=ok`.

Post-apply read-only verification:

- `shipping.__ef_migrations_history` → **13** rows, ending at `20261022195929_AuthoringDraftTypeAssignment`.
- `fulfillment.__ef_migrations_history` → **12** rows.
- New objects present in `shipping`: `authoring_drafts`, `authoring_rate_rows`, `authoring_region_groups`,
  `authoring_selected_areas`, `authoring_draft_translations`, `authoring_group_titles`,
  `authoring_weekly_schedules`, `authoring_weekly_schedule_days`, `authoring_method_settings`.
- New objects present in `fulfillment`: `shipping_types`, `shipping_type_translations`.

No migration file was edited, no history row was written by hand, no object was adopted by name, and
no same-name outbox index collision was encountered.

### Backup

Quick local backup was attempted and is **unavailable**: no `pg_dump`/`pg_basebackup` on this machine and
no repository backup tooling. Per the issued task this is recorded and is **not** a blocker on this
explicitly disposable local development database. Restore was not simulated.

## 3. Host refresh on `:5088` only

- Old process verified before stopping: PID `24304`, `Tooba.Host.exe`,
  `…\src\backend\Host\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe`, started `2026-10-22 06:41:43`
  (the stale binary that produced the TB-206 `404`s).
- **Deviation:** graceful close (`taskkill` without `/F`) did **not** terminate this console app within a
  15 s finite wait, so the exact verified PID was force-stopped. Nothing else was killed by name.
- Source parity was confirmed before reuse (`git diff --stat a932fa2..HEAD -- src/backend` → empty), so
  the retained TB-207 binary was reused; **no second Host build** was performed.
- Launch: task-owned `D:\temp\tb208-host\run-host.cmd` → `D:\temp\tb207-build\out\Tooba.Host.exe`,
  working directory / content root `D:\temp\tb207-build\out`, `ASPNETCORE_ENVIRONMENT=Development`,
  `ASPNETCORE_URLS=http://127.0.0.1:5088`, `Tooba__ShippingOneLevelCatalog__Enabled=true`
  (the documented intended dev gate).
- **Bounded repair (1 of max 2):** the first launch inherited a trailing space in the environment name
  (`"Development "`) because of a `set X=value &&` cmd line; that suppressed
  `appsettings.Development.json` and produced `503 platform.edition.unconfigured`. The launcher was
  rewritten as a `.cmd` file with one assignment per line and the Host relaunched once. No source change.
- New runtime: PID `10800`, executable `D:\temp\tb207-build\out\Tooba.Host.exe`,
  SHA-256 `9987F7A532067FCE97C9DE511E652C9D799564D998560528913DCBB8997B76E5`,
  started `2026-10-24 20:47:13 (+03:30)`, still running for the user.
- Startup outcomes actually observed in the host log (467 lines, **0 errors**, 3 warnings):
  MassTransit SQL Transport created/upgraded the messaging schema in `tooba_messaging`;
  `LanguageBootstrapHostedService` reported "Language bootstrap skipped: no commerce context is bound at
  host startup" (the documented Development ordering owned by the post-migration localization seed);
  legacy Catalog development bootstraps were skipped because `Tooba:CatalogDemo:RunLegacyBootstraps=false`.
  No seed or backfill is claimed to have written rows merely because its wrapper ran.
- Frontend `:3000` (PID `19108`, started `2026-10-22 06:45:12`) was deliberately left untouched.

Restart recipe for the user: run `D:\temp\tb208-host\run-host.cmd` (see file; no secrets in it).

## 4. Real API through the shared frontend

All reads used the existing documented dev-actor mechanism already used by the app
(`X-Tooba-Dev-Actor-User-Id` from the same dev-context actor plus `Accept`/`Accept-Language`); no token
was invented, no auth guard was weakened, no owner/tenant/edition authority was supplied by the client.

| Route | Result |
| --- | --- |
| `GET /v1/admin/dev-context` | `200` `{"actorLabel":"admin-actor@tooba.local","tenantId":"store-alpha"}` |
| `GET /v1/admin/shipping-services` | `200`, active services incl. `post` (`activeOptionCount=1`) |
| `GET /v1/admin/shipping-services/authoring-drafts` (via `:3000`) | `200 {"drafts":[]}` before creation, then the one draft below |
| `GET /v1/admin/shipping-services/authoring-types` | `200 {"types":[]}` — real empty active-type catalog, **not** seeded |
| `GET …/authoring-drafts/{id}/editor-document` (via `:3000`) | `200`, full document |

### Draft created for the trial (minimal, real binding)

- `draftId` = `01a16eeb-70b3-7000-87c2-564c82835496`
- binding: `serviceCode=post`, `optionCode=null` (service-level binding of an **existing** active service)
- `revision=1`, `owner=Store`, `regionGroups=[]` (lawful empty initial tree)
- Created through the existing authorized `POST /v1/admin/shipping-services/authoring-drafts` → `201`.
  No raw SQL business row, no invented registry fact, no type seed, no clone/delete of other drafts.

Exactly **one** business write exists in this task: that single draft creation. No existing authored
content was overwritten and no `PUT` was issued by any harness or check.

## 5. Live integration defect found and repaired (one focused repair)

The real backend returns `owner.ownerKind` (canonical `ShippingAuthoringOwnerView.OwnerKind`), while the
frontend strict decoder required `owner.kind`. The page therefore mounted, called the real route, received
`200`, and still rendered `خواندن پیش‌نویس ناموفق بود.` — a genuine wire-contract mismatch that the
earlier synthetic fixtures could not expose (they used `kind`).

Repair, inside the feature's own transport boundary and with no API/DTO change:

- `src/frontend/features/admin-shipping-authoring/api/editor-document-decode.ts` — read the canonical
  `ownerKind` field (reason path `owner.ownerKind`).
- `src/frontend/features/admin-shipping-authoring/api/editor-document-adapter.test.ts` — the wire fixture
  and its negative case now use the real field name.

Focused check actually run: `node --test features/admin-shipping-authoring/api/editor-document-adapter.test.ts`
→ **7 pass / 0 fail**. No suite-wide run and no `npm run typecheck` were claimed.

## 6. Mounted real-route outcome (no interception)

Harness: `scripts/mount-read.mjs` — an isolated headless profile that installs **no** `Fetch` fulfilment,
replaces no response and rewrites no request. Every `/v1` call below is real traffic to the refreshed Host.

URL: `http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view`

Observed on the mounted page:

- View mode banner and the real draft title; no route blocker and **no** load-error panel.
- `/v1` responses observed: `access-control/me/capabilities` `200`, `authoring-types` `200`,
  `editor-document` `200` (GET only — no `PUT`).
- Truthful states rendered from real data: owner «همین فروشگاه» (server-authorized, not selectable);
  type «نوعی انتخاب نشده است» with the note that the active type catalog is empty; three **configured**
  content languages (fa-IR rtl default, en-US ltr, Russian ltr) each «بدون ترجمه» and «۰ از ۳ زبان
  ترجمه شده»; settings «پیکربندی نشده‌اند»; weekly schedule «پیکربندی نشده است» and read-only.
- Edit reachability (no mutation): «ویرایش» → edit mode with Apply disabled and no dirty state →
  «انصراف از ویرایش» exited back to view mode with no load error. `regionGroups` is empty, so the
  groups/rates surface correctly shows nothing to edit.

## 7. Explicit limits

- Type catalog is genuinely empty; a real type must be created through the normal catalog flow before the
  type/editor binding can be exercised. No invented type id was seeded.
- Authoring **write** acceptance on this draft was not exercised; Apply persistence is not claimed.
- Behavior for a draft with regions/rates/week/settings content was not re-proven here; 202–205-R3
  evidence for those surfaces is unchanged.
- No quote, activation, whole-method creation, integration acceptance or certification is claimed.

## 8. Task-owned helpers and changed paths

- `docs/ai/tasks/TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208.task.md` (verbatim task).
- `docs/architecture/evidence/TB-SHIPPING-ADMIN-LOCAL-STACK-REFRESH-AND-USER-TRIAL-208/` — this file,
  `scripts/parse-plan.mjs`, `scripts/mount-read.mjs`.
- `src/frontend/features/admin-shipping-authoring/api/editor-document-decode.ts`,
  `…/editor-document-adapter.test.ts`.
- Retained runtime: `D:\temp\tb207-build\out` (Host, still running) and `D:\temp\tb208-runner\out`
  (task-owned runner build; safe to delete). Browser profile `D:\temp\tb208-browser` is stopped; no
  build artifact, log or credential is committed.

## 9. Timing

Task claimed `2026-10-24T17:11:25Z`; handoff record written `2026-10-24T17:26Z` (~15 min) — inside the
40-minute timebox with no proof loop. Deviations: forced stop of the verified old Host (graceful close
was ineffective), and one bounded relaunch caused by the trailing-space environment bug.
