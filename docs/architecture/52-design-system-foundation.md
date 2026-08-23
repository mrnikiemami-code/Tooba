# Tooba — Design System Foundation

Status:

```text
Foundation implemented — not visual ACCEPT, not storefront ACCEPT
```

Task:

```text
TB-P04-T002
```

Tooba owns this Design System. Shopeiva is reference input only. Backend/module boundary is not a UI boundary.

## Design principles

- Semantic tokens over raw brand paint. Reference red is not danger.
- Light and dark are first-class via class strategy on `html`.
- RTL and LTR are first-class via `dir` plus logical CSS (`start`/`end`), not `dir=rtl` alone.
- Primitives receive pre-resolved view data. They do not call Pricing, Promotion, Inventory, or Party.
- Workspaces compose primitives; they are not one CRUD screen per module.
- No large UI kit takeover. Native HTML + Tailwind 3 + existing Tooba Next 15.

## Token architecture

Declared in `src/frontend/app/globals.css` and mapped in `src/frontend/tailwind.config.ts`.

Layers:

1. Reference (`--ref-*`) — raw notes from study, not product roles.
2. Semantic (`--color-*`, `--space-*`, `--radius-*`, `--shadow-*`, `--z-*`, `--motion-*`, `--type-*`, `--density-*`).
3. Tailwind aliases (`bg-primary`, `text-danger`, `rounded-ds`).

Roles: background, surface, surface-elevated, foreground, muted, border, primary, secondary, success, warning, danger, info, focus.

Catalog: `src/frontend/design-system/tokens/roles.ts`.

## Theme model

Typed `ThemeContract` (`colorScheme`, `direction`, optional `brandAssetKey`). `ThemeProvider` applies class `dark` and `dir`/`lang` on `html`. No executable tenant script from database. Tenant theme editor is out of scope.

next-themes was KEEP in T001 for Shopeiva, not present in Tooba frontend; a small owned provider is used instead of adding that package.

## RTL / LTR

- Root `dir` from theme.
- Drawer uses `start-0`, never `left-0`/`right-0`.
- Tooltip/popover use logical `start`.
- Inputs may set `dir="ltr"` only as explicit islands (`ltrIsland`) for email/phone/code.
- Mixed-language content is allowed; direction is presentation, not market/currency.

## Typography

CSS roles: display, title, body, caption. Stack is system/Tahoma for Persian readability without shipping font files in this task. Licensed IRANSans pipeline remains later.

## Primitive taxonomy

`src/frontend/design-system/primitives/core.tsx` — Button, IconButton, Input, Textarea, Select, Checkbox, Radio, Switch, Badge, Chip, Separator, Card, Skeleton, Spinner, Alert, EmptyState, ErrorState, Field.

`overlays.tsx` — Tooltip, Popover, Dialog, Drawer, Tabs, Accordion, ToastRegion.

`commerce.tsx` — MoneyDisplay, PricePresentation, DiscountBadge, AvailabilityBadge, QuantityControl, RatingDisplay, MediaAspectBox, SellerIdentityDisplay, Stack, Cluster, PageContainer, StickyActionBar.

## Form patterns

react-hook-form + zod + `@hookform/resolvers` on the internal showcase only. Field wires label, hint, error, `aria-describedby`, `aria-invalid`. Domain forms are out of scope.

## Feedback

Inline Field errors, Alert, ToastRegion (`aria-live="polite"`), Skeleton, Spinner, EmptyState, ErrorState with consumer retry.

Shopeiva `react-toastify` is not copied (REPLACE in T001).

## Overlay behavior

Dialog uses native `<dialog>` (`showModal`, Escape, focus). Drawer: logical start, Escape, backdrop click. Popover is lightweight (no full-page trap). Tooltip is hover/focus CSS.

## Responsive foundations

PageContainer, Stack, Cluster, grid via Tailwind, StickyActionBar. Mobile-first padding. No storefront chrome in primitives.

## Commerce presentation primitives

View-only. `MoneyDisplay` formats a supplied numeric string with `Intl`; it does not compute exclusive/inclusive/tax/promotion. `PricePresentation` receives exclusive, final, flag. Badges receive labels/availability booleans.

## Accessibility baseline

min-h-11 controls, visible `:focus-visible`, semantic labels, IconButton throws if label empty, reduced-motion media query, live region for toasts. No frontend component test runner; invariants in `invariants.ts` plus typecheck/lint/build.

## Dependency choices

Added: `lucide-react` (T001 KEEP icon strategy; used on showcase IconButton), `react-hook-form`, `zod`, `@hookform/resolvers` (T001 KEEP form stack; Tooba frontend lacked them).

Not added: Radix/shadcn kit, next-themes, Tailwind 4, Next 16.

## Future Data Grid

Not started. Grid must consume these tokens/primitives later under a new envelope.

## Future Workspace

Not started. Product/Order/Seller/Customer workspaces must compose this system later; they must not fork colors per module.
