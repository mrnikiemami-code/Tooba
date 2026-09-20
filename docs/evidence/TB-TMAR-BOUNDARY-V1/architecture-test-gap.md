# TB-TMAR-BOUNDARY-V1 Architecture Test Gap

## Why confirmed Domain→Offer edges were not caught
`ArchitectureBoundaryTests.Domain_does_not_reference_infrastructure_or_host` only forbids Domain→Infrastructure/Host/Persistence.
It does **not** forbid Domain→foreign Domain.

## Why Payment.Infrastructure→Wallet.Domain was not caught
Infra tests forbid foreign **Infrastructure/Persistence** references.
They do **not** forbid Infrastructure→foreign **Domain** (or foreign Application).

## Why Order.Application hub was partially visible
`TmarFoundationTests.App_to_app_edges_do_not_expand_beyond_baseline` already freezes App→App edges via `tmar-app-to-app-edges.json`.
Hub was known/baselined; independent review correctly flagged it as extraction risk, not as an unguarded expansion.

## False negatives fixed in this task
Added exact baselines + tests (no wildcards):
- `Baselines/tmar-domain-to-foreign-domain.json`
- `Baselines/tmar-infra-to-foreign-domain.json`
- `TmarFoundationTests.Domain_to_foreign_Domain_edges_do_not_expand_beyond_baseline`
- `TmarFoundationTests.Infrastructure_to_foreign_Domain_edges_do_not_expand_beyond_baseline`

Baselines are designed to **shrink** (tests also fail if baselined edges disappear without editing baseline — forces conscious shrink commits).

## Remaining known gap (documented, not expanded here)
Infrastructure→foreign Application (e.g. Payment.Infrastructure→Wallet.Application) is still unfrozen beyond ModuleContracts rules. Prefer Contracts wave over another ad-hoc baseline unless expansion appears.
