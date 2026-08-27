# 22 — Shopeiva source map (TB-P06-T012)

## Primary reference

```
../SarvNewVerRequirment/reference/shopeiva/src/components/vendor/panel/wallet/wallet.jsx
```

(Relative to repo root: sibling requirement tree under `SarvNewVerRequirment`.)

## Convergence matrix

| Shopeiva element | Tooba target | Decision |
|---|---|---|
| Wallet page layout (balance + transactions) | `vendor-wallet-ui.tsx` | **PORT** — structure preserved |
| Withdraw modal | `showWithdraw` modal in wallet UI | **PORT** — wired to payout request API |
| Transaction type icons (credit/debit) | Lucide `TrendingUp` / `TrendingDown` | **MATCH** — semantic equivalent |
| Deposit / charge wallet | — | **REMOVE** — honest unavailable; no fake money in |
| Saved bank cards | — | **REMOVE** — out of scope; no placeholders |
| Primary accent | Shopeiva theme green | **ADAPT** — Tooba blue `#2563EB` |
| Admin settlement grid | Shopeiva admin wallet/settlement (if present) | **ADAPT** — Tooba admin DataGrid pattern from T010 |

## Admin surfaces

No direct Shopeiva admin settlement JSX in requirement tree for this task — admin screens follow established Tooba admin DataGrid conventions (fulfillments/returns) with settlement-specific columns.

## Fidelity statement

Structural and interaction parity for seller wallet; color token intentionally Tooba-branded. No visual regression to other Shopeiva-locked storefront surfaces (wallet is vendor panel only).
