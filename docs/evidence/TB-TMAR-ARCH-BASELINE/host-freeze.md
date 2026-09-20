# Immediate Host Freeze Policy

## Forbidden NEW (while TMAR recovery active)

- Host business DbContext writes (`Add`/`Update`/`Remove`/`SaveChanges`)
- Host `BeginTransaction` on business DbContexts
- Host business rules / pricing / inventory / buy-box / campaign eligibility decisions
- Host domain ownership / new entities
- Host cross-module business write orchestration
- Expanding direct foreign-module DbContext reads beyond existing legacy sites

## Allowed

HTTP transport · auth/session · middleware · DI/composition · serialization · endpoint mapping · existing temporary read composition (no expansion)

## Enforcement design (before full migration)

Add architecture tests that fail when NEW Host files/methods match write patterns beyond an allowlist of legacy paths:

1. Snapshot allowlist of current Host write files (from host-audit.md)
2. CI greps Host for `SaveChangesAsync` / `BeginTransaction` / `DbContext` writes
3. Diff against allowlist — any new path = fail
4. Same for `IMemoryCache` outside CacheRegistration/MemoryToobaCache
5. Same for new `PrimaryOffer` / pricing decision helpers outside allowlist

Do not remove legacy yet — freeze expansion only.
