# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001 — Migration

## Implemented on branch

Branch: chatgpt/host-root-boundaries-001

### Host removals

- removed CheckoutReservationHoldPolicy.cs
- removed CommerceHoldPolicy.cs
- removed GlobalUsings.SettlementApp.cs
- removed GlobalUsings.SettlementDomain.cs
- removed UnpaidOrderExpiryHostedService.cs
- removed UnpaidOrderExpiryHostOptions.cs
- removed corresponding business registrations from Host Program.cs

### Catalog

Added a read-only boundary:
- IStoreHoldPolicyHoursReader in Catalog.Contracts
- existing IStoreHoldPolicySettingsPort extends that reader
- Catalog.Infrastructure registers the reader through the existing persistence implementation

No Catalog persistence type is exposed.

### Payment

Moved hold resolution to:
- Tooba.Payment.Infrastructure/Adapters/CommerceHoldPolicySource.cs

The implementation:
- owns Payment platform/method hold semantics
- consumes Catalog only via Tooba.Catalog.Contracts.Reservation.IStoreHoldPolicyHoursReader
- implements Tooba.Payment.Contracts.Hold.ICommerceHoldPolicySource
- contains no Tooba.Host, Catalog Infrastructure, or Catalog Domain reference

Payment module registers the source.

### Order

Added:
- Integrations/Payment/CheckoutReservationHoldPolicyAdapter.cs
- ReservationCycle/UnpaidOrderExpiryWorker.cs
- ReservationCycle/UnpaidOrderExpiryWorkerOptions.cs

The adapter consumes Payment.Contracts only and preserves the existing Order checkout port.

The worker is Order-owned and preserves the Host worker's neutral platform seams and runtime behavior.

## Root target state

All six requested root Host files are absent on the working branch.
