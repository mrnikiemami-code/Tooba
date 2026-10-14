# TB-SHIPPING-MVP-ADMIN-050 — Admin `/admin/shipping-services` list/form interaction repair

- Task: `TB-SHIPPING-MVP-ADMIN-050` (Program `TOOBA-SHIPPING-MVP-SIX-TASK-CLOSURE`, milestone 1 of 6)
- Parent: `TB-SHIPPING-CHECKOUT-BINDING-049`
- Mode: `BOUNDED_EXISTING_ADMIN_SHIPPING_SERVICES_VERTICAL_SLICE`
- Pipeline: `BRIDGE-WAKE-V1` / channel `tooba-main` / worker `tooba-worker-01`
- Expected-Base-SHA: `0e9d99df85efc919080d4d9b17256cffc6e5e802`
- Base-SHA: `b44889b92b42ef3b300530bd8a8825f1792c4fb2` (see “Baseline classification”)
- Stop-State: `STOP_FOR_ARCHITECT`

## 1. Baseline classification (harmless fast-forward)

`main` and `origin/main` were both at `b44889b9…`. `Expected-Base-SHA` was its parent.
The only commit between them is the previous task’s own artifact bookkeeping:

```text
b44889b92 TB-SHIPPING-CHECKOUT-BINDING-049: record code/evidence SHA and actual elapsed in result artifacts
files: docs/architecture/evidence/TB-SHIPPING-CHECKOUT-BINDING-049/RESULT.bridge.txt
       docs/architecture/evidence/TB-SHIPPING-CHECKOUT-BINDING-049/results.json
```

No source file, contract, migration, or configuration differs → harmless fast-forward, no
reset/stash/rebase needed. Working tree still carries the pre-existing unrelated dirty files
(`admin-fulfillment-work-queue-screen.tsx`, `admin-order-detail-screen.tsx`,
`storefront-landing-blocks.tsx`) and pre-existing untracked task/evidence files; none were
touched, staged, or reverted.

## 2. Inspected surface (real paths, no guessing)

| Concern | Path |
| --- | --- |
| Route | `src/frontend/app/admin/shipping-services/page.tsx` |
| Screen | `src/frontend/app/admin/shipping-services-screen.tsx` |
| API client | `src/frontend/app/admin/shipping-services-api.ts` |
| Existing focused test | `src/frontend/app/admin/admin-grid-migration.test.ts` (ship row-identity guard) |
| Backend endpoint (read only) | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs` |
| Backend write path (read only) | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/ShippingServiceDirectory.cs` |
| Backend error codes (read only) | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Errors/FulfillmentErrorCodes.cs` |

The screen is the **only** admin route for shipping services, and all mutations already go through
the module-owned Fulfillment endpoints (`GET/POST/PUT /v1/admin/shipping-services[/{id}]`,
`POST /{id}/deactivate`). No second page, no duplicated CRUD, no new ownership layer was created.
Largest service is `post`; dev data shows 5 services (`post`, `tipax`, `snapp_courier`,
`store_courier`, `in_person`) with `snapp_courier` inactive.

## 3. Shortcomings found (exact, user-visible)

1. **Two of the four primary flows failed silently.** All action feedback was rendered *inside*
   the dialog (`{message ? …}` inside `<Dialog>`), so:
   - a failed `loadAdminShippingService` on “ویرایش” set the message while the dialog stayed
     **closed** → nothing visible;
   - a failed `deactivateAdminShippingService` set the same message, again invisible;
   - the message was never cleared on success/open → stale errors leaked into later dialogs.
2. **Invalid form input was silently dropped instead of reported** (`save()`):
   - `translations.filter(t => t.name.trim())` → a service could be saved with **no display name**
     at all (storefront then shows the raw service code);
   - `options.filter(o => o.code.trim())` → a half-typed level-2 option row silently vanished;
   - `sortOrder: Number(sortOrder) || 0` → e.g. `abc` silently became `0`;
   - a duplicate service code went to the database and came back as a raw
     `shipping_service.code_duplicate` code.
3. **Destructive deactivation had no confirmation and used a bespoke text button.** One click
   removes the service from the storefront shipping list; the row actions were hand-rolled instead
   of the admin-standard confirmed row-action cell (as in `admin-campaigns-screen.tsx`).

Supporting backend facts (read, not modified): service code uniqueness is enforced server-side and
option codes have a unique index on `(shipping_service_id, code)` — and the storefront method code is
composed as `service:option`, so duplicate option codes both hit a DB unique violation and create
duplicate checkout method identities. A colon inside a service code would also corrupt that
composition.

## 4. Change delivered (one cohesive slice)

**“Every list/form action reports its outcome, and the form refuses invalid input before it calls the API.”**

New pure model `src/frontend/app/admin/shipping-services/shipping-services-form-model.ts`:

- `validateShippingServiceForm(fields, { activeLanguageId, otherServiceCodes })` → explicit issues
  (code required / canonical `^[a-z0-9_]+$` / max 64 / unique; numeric non-negative `sortOrder`;
  display name required for the **active** language, max 128; option code required + canonical +
  unique within the form; option name required for the active language).
- `buildShippingServiceWriteRequest(fields)` → canonical payload (trim/lowercase code, parsed
  `sortOrder`, trimmed names) — replaces the silent `filter`/`|| 0` behaviour.
- `mapShippingServiceErrorFa(code, fallback)` → stable Fulfillment codes to fa text, raw codes never
  reach the UI.
- helpers: `normalizeShippingServiceCode`, `parseShippingServiceSortOrder`,
  `activeTranslationName`, `activeOptionTranslationName`.

`src/frontend/app/admin/shipping-services-screen.tsx`:

- page-level feedback banner (`data-testid="admin-shipping-services-notice"`, `role="status"` /
  `role="alert"`, success/error tone) that is visible with the dialog both open and closed;
  cleared on plain refresh and on dialog open, set on save/deactivate/open-edit-load outcomes;
- inline form issue list (`data-testid="admin-shipping-service-form-errors"`) rendered from the
  pure validator before any API call; `save()` and `deactivate()` now surface mapped fa messages;
- row actions replaced with the admin-standard `AppGridRowActionsCell` + `AppGridRowAction[]`
  (edit / destructive deactivate with `confirm` and `disabled: busy`);
- one resolved `activeLanguageId` drives the language tab, the service name input, and the
  level-2 option name input (previously the option name was bound to raw `langTab` and could be a
  no-op), with append-if-missing so typing always lands in a translation row;
- `sortOrder` input marked `inputMode="numeric"`; the save button is no longer pre-disabled by
  `!code.trim()` (that was silence; the validator now explains what is missing).

Explicitly **not** changed: route, URL, API client contracts, Fulfillment endpoints/validators,
grid identity (`ShippingServiceGridRow = ShippingServiceListItem & { id: string }` with
`id: item.shippingServiceId`), saved-view wiring, checkout amounts/promises/enabled shipping
options, Host/DI, migrations/seeds. No Shipping rate/coverage control was added or implied, because
the 048 rate model is still not persisted or wired.

## 5. Validation actually run

| Check | Command | Result |
| --- | --- | --- |
| Typecheck | `cd src/frontend && npx tsc --noEmit` | PASS (0 errors) — `log-typecheck.txt` |
| Focused tests (new model + existing grid guard) | `node --experimental-strip-types --test app/admin/shipping-services/shipping-services-form-model.test.ts app/admin/admin-grid-migration.test.ts` | PASS 16/16 — `log-test-focused.txt` |
| Architecture guards | `npm run test:architecture` | 10/11 — 1 pre-existing failure (§7) — `log-test-architecture.txt` |
| Admin suite | `npm run test:admin` | 131/134 — 3 pre-existing failures (§7) — `log-test-admin.txt` |

`npm run test:critical-storefront` was **not** required and not run: no shared storefront component,
no Home/PDP surface, no design-system file was touched (only `app/admin/**`). Unit tests were never
reported as browser verification.

## 6. Live browser verification (read-only, no writes)

Dev frontend was already serving `http://127.0.0.1:3000/admin/shipping-services`; the existing screen
rendered against the real backend, so the change was exercised end-to-end **without mutating data**:

- List renders 5 services with the new icon row actions (`ویرایش`, `غیرفعال‌سازی` with
  `shipping-service-edit-*` / `shipping-service-deactivate-*` test ids); inactive `snapp_courier`
  correctly shows only `ویرایش`.
- “سرویس جدید” opens the create dialog (`admin-shipping-service-dialog`), active tab `فارسی`,
  level-2 option labels read `نام (فارسی)` for both pre-seeded option rows (confirms the
  `activeLanguageId` fix).
- Submitting an empty form (click `ذخیره`) rendered exactly the expected issues
  `کد سرویس الزامی است.` and the active-language display-name message, the dialog stayed open, and
  no request was issued — client-side validation blocks the call.
- `انصراف` closes the dialog (DOM present but hidden per the design-system `Dialog`).

**Not verified in browser (stated honestly, needs real-browser review):**
- a real create/edit **save round-trip** and the success notice banner;
- a real **deactivate** including the native confirm dialog;
- the error-tone notice for a failed save/deactivate.
These were deliberately avoided to prevent writing to the dev database and changing live checkout
shipping options; they are covered by the pure-validator/builder tests plus the screen wiring guard.

## 7. Pre-existing, unrelated guard failures (not caused by, not fixed by this task)

`npm run test:architecture` fails only on the shrink-only source-size baseline; the four files are
untouched by this task and their on-disk content already exceeds the baseline at `HEAD`
(`git show HEAD:<file>` LOC == working-tree LOC, verified):

| file | baseline | HEAD/work LOC |
| --- | --- | --- |
| `src/frontend/app/admin/category-admin-screen.tsx` | 2215 | 2230 |
| `src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx` | 1440 | 1442 |
| `src/frontend/lib/storefront-composition/shared-composition-renderer.tsx` | 907 | 918 |
| `src/frontend/app/storefront/storefront-landing-blocks.tsx` | 1026 | 1033 |

`npm run test:admin` fails 3 pre-existing tests unrelated to this slice (`admin-error-map.test.ts`
content-article copy, the landing section-chooser labels guard, and the `TB-P10-T022` landing-composer
leak guard — i.e. the same pre-existing storefront-composition/landing drift). Fixing these would be
out-of-scope edits to unrelated files, so they are reported, not repaired. New files added here are
227 and 178 LOC, well under the 800 threshold, and no new file was created directly under
`app/admin/` (FE-FOLDER-001 kept green — the new module lives in the existing `shipping-services/`
route folder).

## 8. Change-path manifest

```text
M src/frontend/app/admin/shipping-services-screen.tsx
A src/frontend/app/admin/shipping-services/shipping-services-form-model.ts
A src/frontend/app/admin/shipping-services/shipping-services-form-model.test.ts
A docs/planning/shipping/SHIPPING-MVP-ADMIN-050.md
A docs/architecture/evidence/TB-SHIPPING-MVP-ADMIN-050/**
```

Code-SHA and elapsed minutes are recorded in
`docs/architecture/evidence/TB-SHIPPING-MVP-ADMIN-050/results.json` / `RESULT.bridge.txt`.

## 9. Remaining debt / next-task boundary

- A live create/edit/deactivate round-trip still needs a real-browser pass with a disposable
  service in a non-critical environment (Architect decision), because it necessarily writes Fulfillment
  catalog rows.
- `providerKind` and the service `code` remain free text for *new* services. The Fulfillment
  shipment path only accepts the five registered method codes
  (`ShippingMethodRegistry`), so a brand-new code is administrable here but cannot be used to create
  a shipment yet. Constraining that belongs to the task that actually integrates rates/coverage,
  not to this bounded UX task.
- Shipping rate/coverage administration is still intentionally absent (048 model not persisted).
