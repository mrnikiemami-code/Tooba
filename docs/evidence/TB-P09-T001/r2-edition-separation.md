# R2 — Edition separation

| Edition | Settlement payment/refund handlers |
|---------|-------------------------------------|
| Marketplace | Registered |
| SingleStore | Not registered |

Tests: `MarketplaceSettlementEventPathTests.Marketplace_registers_settlement_payment_and_refund_handlers_singlestore_does_not` (+ existing `SettlementFoundationTests` gate).

Runtime Host with `Tooba__Edition=Marketplace` reports edition Marketplace / connection `marketplace`. Dev default remains SingleStore in `appsettings.Development.json` (unchanged for normal SingleStore work).
