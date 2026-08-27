# 14 — Story i18n / market (TB-P06-T019-R1)

## Backend

- Optional `Locale` / `Market` on Story; validated length + matching helpers (`StoryRules.MatchesLocale`).
- Public list filters by request locale (existing T017 behavior retained).
- Seller and admin create/update pass locale/market through the same commands.

## Frontend management

- Shared Persian copy in `story-management-copy.ts` for Admin and Seller panels (RTL-native panel language).
- No separate seller translation fork.
- Storefront `/fa` and `/en` Story binding unchanged (locale filter on public API).

Brief: management UI is shared FA copy; public i18n remains locale-aware projection without storefront drift.
