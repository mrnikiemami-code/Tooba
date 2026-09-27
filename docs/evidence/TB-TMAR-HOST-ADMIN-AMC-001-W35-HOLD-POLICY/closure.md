# W35-HOLD-POLICY closure

## Outcome
Host Admin HoldPolicySettingsEndpoints evacuated to Catalog-owned aggregate facade. Admin `*.cs` count **18 → 17**.

## Proof
- Catalog.Endpoints: HoldPolicySettingsEndpoints GET/PUT `/v1/admin/settings/hold-policy`
- Catalog.Application: GetHoldPolicySettingsQuery / SaveHoldPolicySettingsCommand + composer/models/validator
- Catalog.Contracts: IStoreHoldPolicySettingsPort (cart/payment hour fields)
- Order.Contracts: IReservationCyclePolicyPreviewPort (store editor preview; no Order.Application from Endpoints)
- Payment.Contracts: IPaymentHoldSettingsGateway.GetPlatformDefaults
- Cart.Contracts: ICartPersistenceHoursSource
- Program: MapHoldPolicySettingsEndpoints removed; CatalogEndpointModule registers it
- Guard: HostAdminAmcW35HoldPolicyGuardTests
- StoreAppearance: untouched

## SoT
- docs/architecture/tmar-current-state.json → hostAdminAmcHoldPolicy
