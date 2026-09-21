# Settlement audit — TB-TMAR-NEXT-MODULE-BATCH-005

## Cross-module boundary
- Settlement.Infrastructure → Order.Contracts, Payment.Contracts, Returns.Contracts only.
- Payment settlement reader moved to `Payment.Contracts` (`IPaymentSettlementReader`); Payment.Application port deleted.
- Baseline `tmar-infra-to-foreign-application.json` Settlement→Application edges removed.

## Physical
- Domain split: Aggregates/*, Entities/CommissionPolicy, Events/*, ValueObjects/*.
- Infrastructure: Directories/, Bridges/, Gateways/, Handlers/, Messaging/, Observability/, DependencyInjection/.
- Application: Ports/.
- Root dump = 0.

## Foundation
- SettlementDirectory injects IClock + IIdGenerator.
- Domain factories take explicit Guid ids; callers use `_ids.NewId()`.
- No Guid.NewGuid / UtcNow / UuidV7.New in Settlement module production sources.

## Host residuals (blocking COMPLETE)
- `SettlementPanelComposer` still constructs `AdminPayoutGridQueryEngine` with `SettlementDbContext` + `PartyDbContext`.
- `AdminPayoutGridQueryEngine` still lives under Host/Grid (query authority in Host).
- `SettlementEndpoints` still uses manual `Results.Json` + `ex.Message` on InvalidOperationException (not ApiResponseFactory/Result centralization).

## Guards residual
- Dedicated Settlement.Tests ArchitectureGuard project not added.
