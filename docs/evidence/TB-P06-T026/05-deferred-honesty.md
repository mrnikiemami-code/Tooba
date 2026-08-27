# 05 — Deferred honesty (checkout spend + refund-to-wallet)

Task: TB-P06-T026

## Checkout wallet spend

**DEFERRED** — Order/Payment checkout does not debit wallet. No customer UI for "pay with wallet". Mixed tender not modeled.

## Refund-to-wallet

**DEFERRED** — Returns/Refund pipeline does not credit wallet ledger. No automatic refund→wallet path in this task.

## Fake deposit / top-up / bank cards

**NOT PORTED** — Shopeiva deposit/withdraw/card UI intentionally hidden on Customer Wallet/Gift Card pages.

## Seller customer-wallet authority

**NONE** — Seller panel does not manage customer wallets. Vendor `/vendor-panel/wallet` remains settlement-facing (deferred gift-cards nav unchanged for seller gift inventory).
