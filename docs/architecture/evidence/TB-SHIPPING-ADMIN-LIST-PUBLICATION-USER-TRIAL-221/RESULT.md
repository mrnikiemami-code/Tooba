# TB-SHIPPING-ADMIN-LIST-PUBLICATION-USER-TRIAL-221 — RESULT

Parent: `TB-SHIPPING-ADMIN-DRAFT-PUBLICATION-API-220`
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: `cursor`
Date: 2026-10-25 · Scope: frontend feature + one bounded backend consistency fix + required local runtime/schema
refresh + real mounted trial.

## 1. What was added

### 1.1 Primary admin list = the owner's real authoring methods

* `src/frontend/app/admin/shipping-services/page.tsx` — `/admin/shipping-services` now renders the
  authoring-method list. The previous flat catalog screen stays reachable **only** through the explicit
  secondary link `?legacy=1`; a failed list read is reported as itself and never silently falls back.
* `src/frontend/app/admin/shipping-services/shipping-services-authoring-list-client.tsx` — client boundary
  with the app-owned saved-view store (feature never imports the app layer).
* `src/frontend/features/admin-shipping-authoring/ui/authoring-method-list-screen.tsx` — list screen built on
  the existing `AppDataGrid` + `useLegacyAdminGridDirectProps` (columns, CSV/Excel export, saved views,
  pagination). Rows are EXISTING authoring drafts from the existing authorized
  `GET /v1/admin/shipping-services/authoring-drafts`; View/Edit `router.push` straight to
  `/admin/shipping-services/authoring/{draftId}?mode=view|edit` — no intermediate legacy dialog and no
  fuzzy match onto a legacy `post:standard` row. Stable row identity is the server draft id.
* `src/frontend/features/admin-shipping-authoring/api/authoring-method-list-api.ts` — strict decoder for the
  draft list + the authoritative authoring context, plus the display-name helpers. Exactly **two** GETs per
  load (`Promise.all`); there is no per-row GET.
* Display name is derived from the authoritative `defaultContentLanguageId` of
  `GET /v1/admin/shipping-services/authoring-context`. A missing translation is rendered as the honest
  "no name" fallback; an unreadable context withholds the name (warning banner) instead of guessing a
  language. Duplicate bindings are flagged per row.

### 1.2 Additive list projection (backend)

* `ShippingAuthoringDraftView.cs` — additive `MethodTranslations` (`init`, default `[]`) on
  `ShippingAuthoringDraftItem`, so the list carries stored names in ONE roundtrip.
* `ShippingAuthoringDraftCommands.cs` (`Map`) — initializes `MethodTranslations` from `draft.Translations`.
* `ShippingAuthoringDraftDirectory.cs` (`ListAsync`) — includes `draft.Translations` in the query.
* No constructor/shape break: existing positional callers keep working.

### 1.3 Publication controls inside the existing editor feature

* `ui/authoring-publication-panel.tsx` — one cohesive panel: stored status, phase vs the confirmed
  revision, version line, snapshot id, confirm dialogs for publish/withdraw, explicit "read status".
* `model/authoring-publication.ts` — pure state model: synchronous in-flight gate (`beginWrite` returns
  `null`), write only from clean VIEW mode with a known baseline revision, `stale` latch on
  403/409/validation, `unknown` latch on an unestablished outcome, and both latches clear **only** by a
  successful explicit read. Publication never touches/repopulates the editor document.
* `api/authoring-publication-api.ts` — GET publication, POST publish (body `expectedRevision` +
  `expectedSelectionVersion`, both exact integers), POST withdraw (`expectedSelectionVersion`). String
  enums and omitted nulls are decoded; an absent key equals an explicit null; a contradictory
  state/provenance pair is `untrusted`, never repaired into a fabricated token.
* `ui/authoring-problem-view.tsx` — extracted save-failure/local-issue views so `authoring-screen.tsx`
  stays at 775 lines (< 800 guard) while composing the panel; `authoring-screen.tsx` now mounts
  `AuthoringPublicationPanel` with the confirmed revision.
* `messages/authoring-messages.ts` / `index.ts` — FA/EN strings and exports.

### 1.4 Bounded backend consistency fix (issued)

* `ShippingAuthoringPublicationCommands.cs` `WithdrawShippingAuthoringPublicationHandler` — a cleared
  result is reported as `Absent` when the cleared pointer has no row (`cleared.Value.Exists == false`) and
  stays `Cleared` for an existing row. Verified live: the withdrawal response carried `Cleared` + version 2
  because the row genuinely existed; the source path for a row-less clear now returns `Absent`.

## 2. Verification executed

| Check | Result |
| --- | --- |
| `node --experimental-strip-types --test features/admin-shipping-authoring/list-publication-221.test.ts` | **14 / 14 pass**, 0 fail, 0 skip |
| `npx tsc --noEmit` | exit 0, no output |
| `frontend-import-boundary` | pass |
| `frontend-flat-folder` | 2 / 2 pass |
| `frontend-feature-boundary` | pass |
| `frontend-test-discovery` | 3 / 3 pass |
| `frontend-source-size` | **fails on 4 files this task did not touch** — pre-existing baseline drift: `app/admin/category-admin-screen.tsx` 2230 > 2215, `app/admin/landing-pages/admin-landing-page-composer.tsx` 1442 > 1440, `app/storefront/storefront-landing-blocks.tsx` 1033 > 1026, `lib/storefront-composition/shared-composition-renderer.tsx` 918 > 907. `git status` for those paths is empty (unmodified by 221); `authoring-screen.tsx` is 775. Recorded honestly, not repaired here. |

Focused test coverage: strict decoding, string enums, omitted nulls, absent/cleared/selected provenance
rules, duplicate-click gate, view-only publish, already-current refusal, stale-and-hold-then-read,
unknown-and-hold-then-read, phase comparison, failed-read-keeps-latch.

## 3. Live runtime / schema refresh (required by the task)

* Existing local target: `127.0.0.1:5432/tooba_alpha`, edition `SingleStore`, tenant `store-alpha`.
* `Tooba.MigrationRunner plan --tenant store-alpha` reported exactly the two expected pending migrations and
  `pendingCount=0` for every other module.
* `Tooba.MigrationRunner apply --tenant store-alpha` applied them; final state from the runner's own
  `Migration Apply` lines:
  * Shipping `current=20261025130021_SelectedCapturedRevision pendingCount=0 result=ok`
  * Order `current=20261016100000_AddOrderShippingGroups pendingCount=0 result=ok`
  * Settlement `current=20261016140000_AddSettlementShippingGroupFees pendingCount=0 result=ok`
* Host rebuilt into an isolated output and left running on `127.0.0.1:5088` (Development,
  `SingleStore`/`store-alpha`, PID 26600); `/health` = `200 {"status":"ok"}`.
* Frontend dev server running on `:3000` (PID 28288, `.env.local` → `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`).
* Dev admin identity used for the trial: the existing Development-only `X-Tooba-Dev-Actor-User-Id` actor
  `01a036c2-970e-7000-8eb7-94bf5cc2d8db` (no new identity, no header bypass introduced).

## 4. Real mounted trial facts (draft `01a16eeb-70b3-7000-87c2-564c82835496`)

1. `GET …/authoring-context` → 200 with default content language `01a06a53-4139-7000-bb9b-3078a0d2e5c4 (fa-IR)`.
2. `GET …/authoring-drafts` → one draft, `revision 2`, `serviceCode post`, one region group with one Country
   area, `methodTranslations: [{ fa-IR, "پیشتاز" }]` — the additive field is really on the wire.
3. `GET …/{draftId}/publication` before any action → `state Absent`, `selectionVersion 0`, no provenance.
4. List page (`/admin/shipping-services`, 1440×900): one row `پیشتاز | post | بازبینی 2 | 1 محدوده`, grid
   columns عملیات/نام روش/پیوند روش/بازبینی ذخیرهشده/محدودههای ارسال, saved-view + export controls, and the
   secondary `?legacy=1` link. Row actions View/Edit navigate to the editor (verified: URL became
   `…/authoring/01a16eeb…?mode=view`).
5. Editor VIEW: publication panel shows «انتشار روش», «این روش هنوز منتشر نشده است.», publish enabled,
   withdraw disabled, plus the honest note that the selection is not yet wired to final settlement.
6. Publish → confirm dialog «بازبینی 2 بهعنوان نسخهٔ منتشرشده انتخاب شود؟ …» → accepted →
   panel «یک نسخهٔ منتشرشده انتخاب شده است.», «نسخهٔ انتخاب: 1», snapshot id, «نسخهٔ منتشرشده همان بازبینی
   فعلی است (بازبینی 2)». `GET …/publication` → `state Selected`, `selectionVersion 1`,
   `selectedSourceRevision 2`, `selectedMatchesCurrentDraftRevision true`.
7. Withdraw → confirm «انتخاب نسخهٔ منتشرشدهٔ این روش برداشته شود؟» → accepted → «انتشار این روش برداشته
   شده است.», `نسخهٔ انتخاب: 2`; `GET …/publication` → `state Cleared`, `selectionVersion 2`, no provenance.
8. Publish again (after clear) → `state Selected`, `selectionVersion 3`, same snapshot id, still matching
   revision 2 — i.e. the pointer version advances once per real change and a clear cannot be bypassed.
9. **The draft itself was never touched**: `GET …/authoring-drafts` still reports `revision 2` with the
   unchanged `updatedAt 2026-10-24T19:05:52`; publication wrote no draft row, reseeded no form and no
   editor "Apply" was pressed.
10. Legacy screen `?legacy=1` still renders the old flat catalog (7 rate policies with codes) — preserved,
    not replaced.
11. Narrow viewport 390×844: list has `scrollWidth == innerWidth` (0 overflow) and shows the grid's own
    responsive columns; the publication panel is 326px wide inside 390 with status/version/current lines and
    the "already current" explanation when publish is correctly unavailable.

Screenshots: `screenshots/01…08` (1440 list, editor absent/published/republished, confirm dialog, 390 list,
390 published panel, 1440 top-of-panel).

## 5. NOT done / NOT live (explicit)

* Publication is still **selection intent only**: there is NO Order/Settlement/Storefront consumer, no
  projection/selection/commit, no price or eligibility effect, and no activation/readiness claim.
* The primary list shows existing drafts and opens View/Edit; creating a *new* method is still the legacy
  catalog's job (`?legacy=1` → «سرویس جدید»). No new draft was created by this task.
* The legacy catalog and the authoring list read different authorities and are intentionally not merged; the
  legacy screen is reachable only through the explicit secondary link.
* `frontend-source-size` fails only on four files untouched by this task (pre-existing drift, §2).
* No production/deployment claim, no Visual ACCEPT claim, no whole-method creation, no auth policy change;
  TB-185 stays paused, default-OFF / PendingAllocation unchanged.
