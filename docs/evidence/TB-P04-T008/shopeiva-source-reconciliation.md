# TB-P04-T008 — Shopeiva source reconciliation

Inspected tree:

```text
D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva
```

## Actual framework

| Fact | Evidence |
| --- | --- |
| Framework | Next.js App Router (JSX), not Vue/Nuxt |
| `next` | `16.2.6` (`package.json` dependencies) |
| `react` / `react-dom` | `19.2.4` |
| Tailwind | `tailwindcss` `^4` with `@tailwindcss/postcss` `^4` |
| Cart UI | JSX: `src/app/cart/page.jsx`, `src/components/cart/CartClient.jsx`, `CartItems.jsx`, `CartSummary.jsx`, `CartEmpty.jsx` |

## Prior statements

- **TB-P04-T006 was accurate:** Shopeiva purchased source is Next.js 16.2.6 / React 19.2.4 / Tailwind 4.
- **TB-P04-T007 Repair RESULT was inaccurate** where it described an original Vue/Nuxt tree later Tailwind-ported into React/Next. That described Tooba’s *port into* `src/frontend` (Next 15 + Tailwind 3), not the purchased Shopeiva tree.

## Direct reuse feasibility

The purchased Shopeiva tree cannot be mounted as-is inside Tooba `src/frontend` (Next 15 / Tailwind 3 / TypeScript). Cart layout, empty state, qty controls, summary, and badge patterns are reused visually. Line identity and totals come from Tooba Cart/Pricing, not Shopeiva `cartStore`.
