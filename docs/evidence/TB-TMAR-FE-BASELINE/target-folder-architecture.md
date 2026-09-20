# Target frontend folder architecture — TB-TMAR-FE-BASELINE

Derived from repository evidence. Canonical root remains **`src/frontend`** (not `src/Web`).

## Layers

1. **Route layer** — `app/**/page.tsx`, `layout.tsx`, route `loading`/`error`: thin composition/transport only (FE-ARCH-001).
2. **Feature layer** — target `src/frontend/features/<capability>/` (catalog, pricing-display, cart, checkout, promotions, appearance, content, admin-catalog, admin-orders, …). Own screens, panels, feature-local API wrappers.
3. **Shared UI** — `design-system/` primitives only (no business screens).
4. **Shared technical lib** — `lib/` (i18n, auth/csrf, host URL, locale routing). No feature screens.
5. **API/data-access** — prefer feature-local `*-api.ts` or `lib/host/` shared HTTP helpers; stop growing `admin-api.ts`.
6. **Tests** — co-located `*.test.ts` / `*.guard.test.ts` beside owners; architecture guards under `lib/architecture/`.

## Import direction

`route → feature → design-system|lib`

Forbidden (new): `design-system|lib → feature/app business modules` except baselined debt.

## Near-term target shape (conceptual)

```text
src/frontend/
  app/                    # routes only (thin)
  features/
    catalog/
    cart/
    checkout/
    content/
    promotions/
    appearance/
    admin-catalog/
    admin-orders/
    ...
  design-system/
  lib/
  public/
```

Physical moves only after FE-F1+ import ownership is clear (ARCH-FOLDER-001 / FE roadmap).
