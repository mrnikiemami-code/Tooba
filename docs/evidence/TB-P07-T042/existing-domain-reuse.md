# TB-P07-T042 — Existing domain reuse

- **Payment**: `CustomerPayment` rows queried in `AdminPaymentsGridQueryEngine`; operational snapshot from `IPaymentAdminDirectory`.
- **Settlement**: `SettlementEntry` via new `ISettlementDirectory.ListEntriesBySellerOrderIdsAsync`; balances/payouts unchanged in Settlement schema.
- **Order**: checkout/seller snapshots for fallback gross/payable when settlement entry missing.
- **Party**: batch `DisplayName` enrich for settlement balances and payout grid (no cross-schema SQL JOIN).

No parallel finance domain introduced in Host or frontend.
