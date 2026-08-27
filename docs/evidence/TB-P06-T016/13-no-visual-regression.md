# 13 — No visual regression (TB-P06-T016)

## Contract

This Task is **routing/SEO only**. Forbidden: CSS, JS behavior, carousel, animation, transition, hover/focus/active, spacing, typography, responsive, Shopeiva structure redesign.

## Diff nature

- Middleware + i18n helpers + LocalizedLink swaps
- Metadata/sitemap additions
- No Shopeiva template CSS/Tailwind geometry edits for locale work

## Critical surfaces

Home / PDP / Blog / Article retain existing component trees; locale prefix is URL + `lang`/`dir` only.

Functional PASS ≠ Visual ACCEPT. Critical-storefront tests remain the guardrail.
