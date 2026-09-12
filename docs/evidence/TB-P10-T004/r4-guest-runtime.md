# TB-P10-T004-R4 — Guest Runtime

See `r4-runtime-raw.json` step G.

- Valid committed cartId + guest secret → 200
- Wrong guest secret → denied
- Proof is Order/Payment-scoped and survives Cart finalization on FE via `paymentResultProof`
