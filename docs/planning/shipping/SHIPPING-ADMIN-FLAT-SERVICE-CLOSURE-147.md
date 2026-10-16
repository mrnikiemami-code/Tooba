# TB-SHIPPING-ADMIN-DEV-AUTH-VISIBILITY-147 — Flat admin screen visible in the ordinary Development flow

Parent: `TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146`. Base SHA `07a808797a7d8d3d3ee1982fde378c907c48f73c`.

## 1. Defect (reproduced before the fix)

`/fa/admin/shipping-services` rendered the **legacy provider editor** («انواع سرویس (سطح ۲)», six per-row icons,
provider dropdown) instead of the flat one-row-per-method table, whenever the visitor had no **server-side** admin
session. `page.tsx` resolved the gate from its own server read; `buildUpstreamAuthHeaders()` cannot see the
Development admin actor (it lives in browser `localStorage`), so
`GET /v1/admin/shipping-services/shipping-methods/one-level` answered
`403 admin.authorization.denied`, the gate failed closed and the old screen was rendered as a silent fallback.
With a real session the flat screen rendered, which is why TB-146 PASSed while the user still saw the old page.

Not a Next cache/build issue: the Host read is `dynamic = "force-dynamic"`, `revalidate = 0`, `cache: "no-store"`.

## 2. Change (narrow, TB-146 continuation)

New file `src/frontend/app/admin/shipping-services/flat-screen-gate.tsx` (client):

- when the server read already succeeded (`serverResolved`) the SSR flat payload renders immediately — the
  authenticated admin fast path is unchanged and needs no client fetch;
- otherwise the gate performs **one** canonical read through the *same* browser Admin transport every admin page
  already uses (`prepareAdminDevActor()` from `admin-api.ts` + `loadOneLevelCatalog()` from
  `flat-method-catalog-api.ts`) and mounts `ShippingServicesFlatScreen` **only** on a real
  `gate=Enabled` **and** `ready=true` answer;
- `401`/`403` ⇒ localized «دسترسی ندارید» panel; unready/disabled/unreachable ⇒ localized error panel with
  «تلاش دوباره»; both carry `admin-shipping-services-{denied,error}` test ids. A generation counter prevents a
  stale attempt from overwriting newer state (React StrictMode double-effect in Development).
- nothing else can enable the gate: no query parameter, no build flag, no method-list length, no hardcoded
  fallback, and `localStorage` is never read in these files (the identity seam reads it, unchanged).

`page.tsx` now renders `<ShippingServicesFlatScreenGate>` and **no longer imports or renders
`ShippingServicesScreen`**: there is no silent legacy fallback left on this route.

Regression guard updated in `shipping-one-level-tabs.test.ts` and `shipping-one-level-method-name.test.ts`:
the page must compose the gate, must not contain `ShippingServicesScreen`, and must not resolve the gate from a
URL, `localStorage` or a client bypass; the gate must use the canonical client read, must mount the flat screen
only from that answer and must expose explicit denied/error/retry states instead of falling back.

## 3. Proof

Real shared Development stack, no restart/rebuild/cache deletion/DB write (see
`docs/architecture/evidence/TB-SHIPPING-ADMIN-DEV-AUTH-VISIBILITY-147/`):

| scenario | result |
| --- | --- |
| ordinary Development flow, `GET /api/auth/me` = 401, browser dev actor `01a036c2-…` | **7 rows, 21 row actions** (7 view / 7 edit / 7 delete), 0 legacy row actions, 0 «انواع سرویس», footer «نمایش ۱ تا ۷ از ۷ ردیف» |
| real authenticated admin session | 7 rows / 21 row actions from the SSR payload, no re-resolve |
| no identity and catalogue read = 403 | explicit localized denied panel + retry, 0 flat rows, 0 legacy markers, 0 «انواع سرویس» |

Focused frontend folder suite **213/213 pass**, `npm run typecheck` exit 0. Architecture guards 10/11; the single
failure is the pre-existing unrelated `FE-SIZE` baseline (5 files, none touched here).

## 4. Remaining blockers (unchanged from TB-146)

- no canonical **create-method** endpoint on the Host → «سرویس جدید» keeps its honest blocked dialog;
- no live **save/reopen write round-trip** proof (shared Development DB is read-only in this task).
