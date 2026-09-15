# Storefront responsive variant contracts

Every registered Variant has a code-owned Responsive Contract:

- `columns` — desktop / tablet / mobile
- `height` — preset name or auto
- `itemVisible` — approximate visible items
- `notesFa` — short Persian note

Admin never configures breakpoints, mobile columns, px widths, or arbitrary mobile heights.

Desktop may expose bounded height presets (Compact, Medium, Large, ExtraLarge). Mobile maps automatically into safe capped behavior via `SIZE_PRESET_CONTRACTS`.

Authoritative registry: `src/frontend/lib/storefront-composition/responsive-contracts.ts`.
Validation: every Variant must resolve a contract (`composition-registry.guard.test.ts`).
