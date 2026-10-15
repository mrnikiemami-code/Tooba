# SHIPPING-FULL-OPERATIONS-077 — admin packaging fee and freight payer editor

Task: `TB-SHIPPING-FULL-OPERATIONS-077` · Parent: `...-076` · Timebox: 45 minutes HARD

## What problem this slice solves

075 built the Shipping-owned packaging policy (fixed fee, currency, destination scope, option
identity) and the freight payer mode (`Prepaid` / `CollectOnDelivery`); 076 exposed a read-only ADMIN
operational quote. Neither was reachable from the admin UI, so a customer asking "can I set a
packaging fee and say who pays the freight" had no answer in the product. This slice closes that
user-facing gap with **presentation only** — no new backend route, no new page, no price authority
change.

## One focused dialog, not a merged mega-form

`/admin/shipping-services` already carries per-service row actions (`نرخ و پوشش`, `برنامه کاری
ارسال`). A fourth action, «هزینه بسته‌بندی و پرداخت کرایه» (`Coins`), opens
`ShippingPackagingDialog` for that service `code`. It is deliberately **not** merged with rates or
schedules: three different commercial facts keep three different surfaces.

| File | Responsibility |
| --- | --- |
| `shipping-packaging-api.ts` | Typed client for the four existing 075 routes |
| `shipping-packaging-model.ts` | Pure form parsing, validation, grouping, Persian wording, error mapping |
| `shipping-packaging-dialog.tsx` | The dialog (list, inline edit, toggle, create) |
| `shipping-services-screen.tsx` | Minimal composition only (+22 lines: icon, target state, action, dialog) |

## Real routes, real re-reads, no mock data

The client speaks the exact routes the Shipping module already registers:

| Operation | Route |
| --- | --- |
| List | `GET /v1/admin/shipping-services/{serviceCode}/packaging-policies` |
| Create | `POST /v1/admin/shipping-services/{serviceCode}/packaging-policies` |
| Update | `PUT /v1/admin/shipping-services/{serviceCode}/packaging-policies/{id}` |
| Active | `PATCH /v1/admin/shipping-services/{serviceCode}/packaging-policies/{id}/active` |

Nothing is patched in memory and nothing is persisted locally: every create, update and toggle is
followed by a real `GET` re-read. `serviceCode` stays authoritative from the route, and the body
echoes it for the backend's own mismatch check. Destination scope comes only from the existing
`ShippingDestinationPicker` (Geography catalog); the option list comes only from the real Fulfillment
shipping-method tree. No ID is ever invented.

## Money is a quotation fact, not a transaction

The dialog states the boundary in the UI itself:

- **Payer mode is a quote fact.** Shipping says who owes the carrier freight and when. Real
  collect-on-delivery collection/settlement is **not** wired into Checkout or Payment.
- **Free shipping does not waive packaging.** A free-freight threshold waives carrier freight only;
  the independent packaging component survives it.
- **No policy = explicit zero.** An empty list means "packaging fee zero / default prepaid", never a
  hidden charge. A persisted `0` is a real charged policy and is displayed as such.

## Validation and error honesty

`parsePackagingFee` accepts only a non-negative value with at most two decimals, and **never rounds
silently**: `1.005` is refused rather than charged as something the user did not type. Currency must
be three Latin letters. Option identity is validated against the service's real option list *before*
any call, so the browser never manufactures `shipping.service_option.unknown`.

Backend failures surface as field-adjacent Persian messages through a stable code map
(`shipping.packaging.policy.scope_duplicate`, `..._fee_negative`, `..._currency_invalid`,
`..._freight_payer_mode_invalid`, `shipping.service_code.unknown`, `shipping.service_option.unknown`,
`shipping.destination_scope.invalid`, `admin.authorization.denied`, `host-unreachable`). A
`validation.failed` envelope is unwrapped to its actionable child code so the user sees the real
reason, 401/403 render as a visible access-denied state, and no path shows a fake success.

Editing keeps the persisted scope and option identity: the update body re-sends the row's own
`countryId`/`administrativeAreaId`/`cityId`/`serviceAreaId` and `optionCode`, so editing a fee can
never silently move a policy to another option or destination.

## Mission B — NOT STARTED

The ADMIN operational-quote inspector (Mission B) was **not started**. The task authorizes it only if
it fits safely inside the 45-minute HARD bound and explicitly says to prioritize Mission A otherwise.
Mission A (component, model, client, tests, TypeScript, browser round-trip and wire proof) consumed
the budget; a second API client + model + dialog + live proof could not be done without overrunning
the bound, so it was deliberately deferred rather than half-built. No code, route or placeholder for
it was added.

## Verification

- `node --experimental-strip-types --test app/admin/shipping-services/shipping-packaging-api.test.ts`
  → 13 passed, 0 failed.
- `npm run typecheck` (`tsc --noEmit`) → clean.
- Architecture guards: import-boundary, flat-folder, feature-boundary and test-discovery guards all
  pass. The shrink-only source-size guard reports 5 **pre-existing** `OVERSIZED_GROWTH` entries
  (`category-admin-screen.tsx`, `admin-landing-page-composer.tsx`, `storefront-landing-blocks.tsx`,
  `storefront-shipping.tsx`, `shared-composition-renderer.tsx`). All five are byte-identical to
  `HEAD` (no diff) and none is touched by this task — a documented pre-existing condition, not a
  regression introduced here.
- Live Development proof: `log-live-admin-packaging-proof.txt` (browser round-trip plus the
  non-mutating wire probe `probe-admin-packaging.ps1`).
