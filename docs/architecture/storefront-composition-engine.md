# Storefront composition engine

Shared Home + Landing composition model (TB-P10-T018 foundation).

## Model

```text
SectionType
  → Variant
    → Settings Schema (controlled keys only)
    → Data Source Contract
    → Responsive Contract (code-owned)
    → Surface Role (page|section|alternate|accent)
    → Capability flags (homeAllowed, landingAllowed)
```

Instances are an ordered list of sections with stable IDs, enabled flag, variantKey, typed settings, and surfaceRole.

## Rules

- One registry for Home and Landing.
- No arbitrary CSS/HTML/JS or unvalidated JSON settings.
- Mobile behavior is system-owned per Variant responsive contract.
- Industry templates are versioned presets from the same registry (editable Drafts).
- Prefer Variants over proliferating SectionTypes.
- Only four user-editable global surface colors; local surfaces remain derived (Appearance locks).
- Data sources must reflect backend truth; heuristic/deferred kinds are labeled, not faked.

## Code foundation

- `src/frontend/lib/storefront-composition/types.ts`
- `registry.ts` — SectionTypes, Variants, industry template seeds
- `responsive-contracts.ts` — per-variant contracts
- `settings.ts` — controlled settings + forbidden keys
- `size-presets.ts` — Compact/Medium/Large/ExtraLarge
- `landing-adapter.ts` — legacy Landing/Home type maps

## Renderer contract

One future shared renderer serves Home, Landing, and Landing-as-Home using the same registry, responsive contracts, Appearance/theme, ProductCard, and semantic surfaces. Canonical Home fallback remains until migration is explicitly accepted.
