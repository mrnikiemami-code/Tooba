# Shipping methods

- Source: active `ShippingService` (+ options) seeded/admin catalog, filtered by `Tooba:ShippingMethods:EnabledCodes`.
- Labels from Language Registry translations.
- Zero methods → empty UI + continue disabled.
- Runtime C: deactivating `tipax` removed all `tipax*` methods from projection.
