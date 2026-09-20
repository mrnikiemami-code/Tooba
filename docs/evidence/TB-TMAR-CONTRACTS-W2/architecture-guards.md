# Architecture guards — TB-TMAR-CONTRACTS-W2

## Baseline shrinks
| Baseline | Change |
|---|---|
| tmar-infra-to-foreign-application | 33 → 30 (−Payment→Wallet.App, −Pricing→Offer.App, −Inventory→Offer.App) |
| tmar-app-to-app-edges | 16 → 15 (−Pricing→Offer.App) |
| tmar-cross-context-transaction-files | NEW (CheckoutDirectory) |

## Contracts cleanliness
Existing `Module_Contracts_projects_do_not_reference_Domain_Infrastructure_or_Host` covers Offer/Wallet Contracts.
