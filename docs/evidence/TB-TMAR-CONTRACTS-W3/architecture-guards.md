# Architecture guards — TB-TMAR-CONTRACTS-W3

## Baseline shrinks
| Baseline | Change |
|---|---|
| tmar-infra-to-foreign-application | 30 → 29 (−Returns→Wallet.App) |
| tmar-app-to-app-edges | 15 → 14 (−Order→Offer.App) |

## Unchanged empties
- Domain→foreign Domain: empty
- Infra→foreign Domain: empty

## Contracts cleanliness
Existing `Module_Contracts_projects_do_not_reference_Domain_Infrastructure_or_Host` covers Wallet/Offer Contracts.
