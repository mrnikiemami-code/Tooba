# SHIPPING-ONE-LEVEL-UI-084 — five-tab shell, one-level services tab, OFF-gate preservation

Task: `TB-SHIPPING-ONE-LEVEL-TABS-084` (parent `TB-SHIPPING-ONE-LEVEL-TABS-083`)
Expected base SHA: `2b80990f8016ab1beb8c946429fcfad13bd5437b`
Actual base SHA: `2b80990f8016ab1beb8c946429fcfad13bd5437b` (exact, zero drift)
Scope: build the merchant-facing one-level five-tab Shipping admin surface behind a fail-closed,
default-OFF gate, without touching checkout authority, payment, rate authority or any persisted data.

## 0. Preflight

`git fetch origin` → `HEAD == origin/main == 2b80990f8016ab1beb8c946429fcfad13bd5437b`, identical to the
expected base; no drift of any kind and no `RECOVERY_CONFLICT`. All pre-existing dirty/untracked paths were
preserved, including the modified admin screens (`admin-fulfillment-work-queue-screen.tsx`,
`admin-order-detail-screen.tsx`) that predate this task; nothing was reset, stashed, rebased or force-pushed.

Inspected before editing: `app/admin/shipping-services-screen.tsx` (766 lines, the working two-level
screen), `app/admin/shipping-services/page.tsx` (the route), `app/admin/shipping-services-api.ts`, the
feature folder's 11 existing tests, the design-system primitives (`Button`, `EmptyState`, `ErrorState`),
the tab patterns used across admin (`campaign-tabs`, `admin-content-language-tabs`, `WorkspaceShell`), and
the frontend architecture guards in `lib/architecture/`.

**Current source of truth and gates, as found:** the legacy two-level `shipping_services` /
`shipping_service_options` catalog is the sole operational read/write authority. The 083 one-level catalog
exists in source only, is UNMIGRATED in Development, and `Tooba:ShippingOneLevelCatalog` has no appsettings
section anywhere. `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow` are untouched.

## 1. What was implemented

New feature-local folder files under `app/admin/shipping-services/`:

| file | role |
| --- | --- |
| `shipping-one-level-tabs-model.ts` | pure, React-free model: tab ids/labels, URL parse/build, code sanitization, keyboard index math, row projection, data state |
| `shipping-one-level-gate.ts` | explicit default-OFF frontend gate seam, with the missing server binding documented in code |
| `shipping-one-level-tabs.tsx` | the five-tab shell and the read-only tab 1 |
| `shipping-one-level-tabs.test.ts` | 14 focused tests |

`app/admin/shipping-services/page.tsx` is the only pre-existing file changed: it now evaluates the gate and
renders **either** the new shell (gate enabled) **or** the untouched legacy screen (gate off).

### Five tabs — exact labels

«سرویس‌ها»، «تعرفه و پوشش»، «بسته‌بندی و کرایه»، «برنامه کاری و ظرفیت»، «بررسی هزینه»

`role="tablist"` / `role="tab"` / `role="tabpanel"` with `aria-selected`, `aria-controls`, roving
`tabIndex`, visible focus via the design-system button styles, and `ArrowRight`/`ArrowLeft`/`Home`/`End`
navigation that wraps. The container is `dir="rtl"` and the tab row is `flex-wrap` so it stays responsive;
the methods table uses `text-start` so it mirrors correctly in RTL while method codes stay `dir="ltr"`.

### Tab «سرویس‌ها»

One independent row per method with a stable `method_code` (rendered `dir="ltr"` monospace), an independent
display label, an explicit `فعال`/`غیرفعال` state, and selection. There is **no** nested
«انواع سرویس (سطح ۲)» editor, **no** option-count column and no `providerKind` in the render tree
(`provider_kind` never becomes a selectable second level). Inactive methods stay visible and inactive.
A method with no translation for the active language shows «بدون برچسب» — no label is invented.

### URL state

`?tab=…&method=…&lang=…`, with defaults omitted. Input is sanitized before it is ever used or re-emitted:
method codes are lowercased, restricted to `[a-z0-9_-]` plus at most one `:`, and truncated to 32
characters (the B2 width of `checkouts.shipping_method_code`); language ids must be canonical UUIDs;
unknown `tab` values fall back to `services`. Selection is applied with `router.replace(..., { scroll: false })`,
so keyboard navigation does not stack history entries.

### The four shell tabs

Honest, non-fake destinations: each renders a scoped notice naming the task that will wire it
(085/086/087/088). There is no Save button, no invented CRUD, and no fabricated data.

### Data state — no legacy fallback

`resolveDataState` yields exactly four states: `unavailable` (gate off), `loading`, `error`, `ready`. An
empty list is `ready`, not an error. There is deliberately **no** path that falls back to the legacy
option tree and claims the one-level catalog is authoritative; with no method data the UI says so.

## 2. Gate behavior — verified, not asserted

The authoritative gate is server-side (`ShippingOneLevelCatalogPolicy.Evaluate` over
`Tooba:ShippingOneLevelCatalog`). This Next.js frontend app has **no** trusted read of that setting: there
is no admin read-only endpoint exposing it, and no server-config seam in the admin app. Per the task's own
instruction for exactly this case, the work delivered is:

- a dormant, **disabled-by-default** explicit read seam (`resolveOneLevelCatalogGate`);
- the gate can only be enabled by a `serverFlag: true` that only a server component could supply; it is
  never inferred from schema existence, method data, query string, or `localStorage`;
- no query-param switch that bypasses the OFF gate exists.

**Missing binding (exact):** a read-only admin endpoint returning
`ShippingOneLevelCatalogPolicy.Evaluate(configuration.GetSection("Tooba:ShippingOneLevelCatalog"))`,
consumed from a server component. Until that exists the new shell stays unmounted and no live UI rollout
is claimed.

### Live browser proof (real, non-fabricated)

A Next.js dev server was already running on `127.0.0.1:3000` (node pid 31580, started 2026-10-11). It was
only navigated to; nothing was restarted or reconfigured.

`GET /admin/shipping-services`:

```
legacyScreen: true   newShell: false   grid: true
createButton: true   rowActionButtons: 5   tablistCount: 1
```

`GET /admin/shipping-services?tab=rates-coverage&method=post%3Aexpress&gate=on` (a deliberate bypass
attempt):

```
legacyScreen: true   newShell: false   createButton: true   rowActionButtons: 5
```

The OFF gate renders the legacy screen with all 5 row actions and the create button intact, and crafted
query parameters cannot switch it. Screenshot: `gate-off-legacy-route.png`. This is live browser evidence
of gate preservation, and it is the *only* live claim made here — the new shell itself was **not** observed
in a browser, because mounting it would require enabling the gate, which is not authorized.

## 3. Safety verification

- No change to `Tooba:ShippingRateAuthority` or `Tooba:ShippingOperationalShadow`, and none to
  `StorefrontShippingService` / `StorefrontCheckoutService`.
- No historical persisted code or paid snapshot was modified; no DB write, migration or backfill.
- The Host was not restarted (pid 33548 still alive on `:5088`), no server reconfiguration, no customer
  price/availability change, no COD settlement, no capacity reservation, no delivery promise.
- New UI is read-only: no write API is imported and no Save control exists.
- The task receipt `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-084.task.md` is intentionally left unstaged.

## 4. Tests actually run

| command | result |
| --- | --- |
| `node --experimental-strip-types --test app/admin/shipping-services/shipping-one-level-tabs.test.ts` | **pass 14, fail 0** |
| `node --experimental-strip-types --test app/admin/shipping-services/*.test.ts` | **pass 131, fail 0** (14 new + 117 pre-existing) |
| `npm run typecheck` | **clean, no errors** |
| `npm run test:architecture` | **pass 10, fail 1** — see below |
| live browser check on `:3000` | legacy screen preserved with gate OFF, bypass attempt ineffective |

`test:architecture`'s single failure is **pre-existing and unrelated**: `FE-SIZE` reports
`OVERSIZED_GROWTH` for `category-admin-screen.tsx` (2230 > 2215), `admin-landing-page-composer.tsx`
(1442 > 1440), `storefront-landing-blocks.tsx` (1033 > 1026), `storefront-shipping.tsx` (1033 > 945) and
`shared-composition-renderer.tsx` (918 > 907). All five are untracked-clean at `HEAD` and were not touched
by this task; the guard's own rule is that oversized files are shrink-only, and those files are not in this
task's change surface. The new files were checked against the same guard's other rules: nothing was added
flat under `app/admin` (the feature lives in the existing `shipping-services/` folder), and no new shared→feature
import was introduced.

## 5. Remaining debt / blockers

1. The server-side gate read binding described in §2 does not exist, so the new shell is dormant. This is
   the single blocker to any live UI rollout and requires separate architect authorization (a backend
   endpoint is out of this task's scope).
2. The one-level catalog has no persisted data anywhere (083), so even with the gate on, tab 1 would show
   the honest empty state until a backfill is authorized.
3. `loadMethods` is an injected seam with no implementation yet; wiring it to a real reader is part of the
   same future authorization.

## 6. Recommended next step (not issued, not run)

085–088 wire the four remaining tabs against existing capabilities behind the same gate, and a separately
authorized task adds the read-only admin gate/method endpoint. The planned 11-task ceiling (081–091) is
unchanged; no 085 task was created or executed.
