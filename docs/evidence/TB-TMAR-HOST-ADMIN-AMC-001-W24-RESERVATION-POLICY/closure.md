# W24-RESERVATION-POLICY closure

## Outcome
PASS — Host Admin ReservationPolicy* evacuated into Order (HTTP/CQRS/composition) with Catalog Contracts persistence port.

## Proof
- Host Admin files deleted: `ReservationPolicyAdminEndpoints.cs`, `ReservationPolicyAdminComposer.cs`, `ReservationPolicyAdminModels.cs`
- Order owns `MapReservationPolicyAdminEndpoints` + `MapReservationPolicySellerEndpoints` via `OrderEndpointModule`
- Catalog: `IStoreReservationPolicySettingsPort` / `StoreReservationPolicySettingsPort`
- Program: no `MapReservationPolicyAdminEndpoints`
- Host Admin `*.cs` count: **32**
- Guard: `HostAdminAmcReservationPolicyGuardTests`
- Older Admin-count guards updated 35 → 32

## HoldPolicy note
`HoldPolicySettingsEndpoints` retained in Host (BLOCK). Local reservation write helpers inlined for compile continuity; view mapping uses Order `ReservationPolicyComposer`.

## SoT
- `docs/architecture/tmar-current-state.json` → `hostAdminAmcReservationPolicy`
- `docs/evidence/TB-TMAR-HOST-ADMIN-AMC-EMPTY/progress.md` updated
