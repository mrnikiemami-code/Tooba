# 17 — Visual fidelity

Task: TB-P06-T024-R1

## Access Control Center (T024 baseline preserved)

| Aspect | Status |
|--------|--------|
| Existing ACC layout/tabs/cards | Unchanged structure |
| ScopeEditor styling | Source-derived Shopeiva pattern (white card, `#2563EB`, rounded-xl, RTL) |
| Search input + list | Native Tailwind — no foreign select/tree library |
| Badges (scope, ceiling, platform-only) | Consistent with T024 badge language |
| Hover/focus on list rows | `hover:bg-[#2563EB]/5`, selected `bg-[#2563EB]/10` |
| Mobile | ACC responsive grids unchanged; ScopeEditor wraps |

## Selector additions

Only **source-derived** UI added:

- Scope type dropdown
- Search field
- Scrollable result list
- Selected resource chip

No appended foreign components.

## Seller Orders UI

- No CSS/Tailwind changes to order list/detail components in this repair.
- Scoped data may show fewer rows — not a visual regression.

## Navigation shells

- Admin/Seller sidebar geometry unchanged.
- Items hidden via capability filter — no layout shift beyond fewer links.

## Unauthorized deviation

**NONE claimed** in code review — no new animation libraries, no spacing/typography overrides on locked surfaces.

## Visual ACCEPT status

Functional PASS ≠ Visual ACCEPT per AGENTS.md.  
No screenshot baselines captured in this folder. User visual review **OPEN** — compare `:3000` ACC vs `:3001` Shopeiva settings patterns manually.

## Critical storefront

Home/PDP not touched — `test:critical-storefront` not required for this task scope.
