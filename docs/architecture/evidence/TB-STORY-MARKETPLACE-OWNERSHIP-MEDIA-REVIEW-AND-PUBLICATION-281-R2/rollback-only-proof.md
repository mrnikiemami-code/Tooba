# Rollback-only proof (TB-281-R2)

## Status: NOT COMPLETED

No new PostgreSQL rollback-only suite was executed in this pass against `tooba_alpha`.

Focused InMemory Directory facts (not relational):

- `StoryMarketplace281R2DirectoryTests` 4/4
- Retained `StoryMarketplace281R1DirectoryTests` 3/3
- `StoryModuleAmsc001W3R2SetEqualityGuardTests` (route/validator pins for +1 seller query) included in 17/17 focused Host filter

Declared limit: InMemory EF does not prove scoped uniqueness constraints, advisory locks, or fresh-context public reader on real PG.
