# Localization Boundary Audit — TB-TMAR-NEXT-MODULE-BATCH-004-R2

## Contract
- New: `Tooba.Localization.Contracts` — `ILanguageLookup` + `LanguageLookupSnapshot`
- Infra: `LanguageLookupBridge` registered in `LocalizationModule`
- Fulfillment.Application consumes **Contracts only** (list language fallback)
- `HostShippingServiceLanguageGate` uses `ILanguageLookup` (no Localization.Application)

## Preserved semantics
Resolve by Code / Culture / UrlPrefix / Culture prefix, else default language, else first.

## Scope
No broad Localization recovery; Contracts extract only.
