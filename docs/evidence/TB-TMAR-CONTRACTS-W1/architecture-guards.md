# Architecture guards — TB-TMAR-CONTRACTS-W1

## Baselines shrunk

| Baseline | Before | After |
|---|---:|---:|
| tmar-domain-to-foreign-domain.json | 3 | 0 |
| tmar-infra-to-foreign-domain.json | 1 (Payment→Wallet.Domain) | 0 |

App→App and Infra→foreign Application baselines unchanged (no new edges).

## New / extended tests

- `Module_Contracts_projects_do_not_reference_Domain_Infrastructure_or_Host`
- `Payment_Infrastructure_does_not_reference_Wallet_Domain`
- Existing Domain/Infra foreign Domain guards (now empty baselines)
- Source-size inventory refreshed (1721 files; oversized still 55; no LOC growth)

## Created Contracts projects

- Tooba.Offer.Contracts — dependency-clean
- Tooba.Wallet.Contracts — dependency-clean
