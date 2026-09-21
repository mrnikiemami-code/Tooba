# Recovery Start — TB-TMAR-NEXT-MODULE-BATCH-005

- Claim: `6d383a9a-c3e2-4f62-9e04-a0c3fab82721` (Claimed)
- Baseline HEAD: `d04343601ad6d9d59911d9ded163afd24e16944f` == `origin/main`
- Channel: `tooba-main` / Worker: `tooba-worker-01`
- Mode: FAST-SAFE Silent Bridge
- Parent: TB-TMAR-NEXT-MODULE-BATCH-004-R4 (ACCEPTED)

## Scope (BATCH-005 only)

Recover **Cart** + **Settlement** to COMPLETE_REFERENCE_PATTERN:

1. Cross-module boundaries → `*.Contracts` only (Cart: Catalog/Inventory; Settlement: Payment)
2. Physical folders + namespace alignment; root dump = 0; split god-files
3. Foundation: IClock / IIdGenerator; no UtcNow / Guid.NewGuid / UuidV7.New bypass; no hidden DI fallbacks
4. Host Settlement thin transport (no SettlementDbContext business/query authority; no manual semantic envelopes / ex.Message)
5. Architecture guards + focused characterization tests + `dotnet build src/backend/Tooba.slnx`

## Out of scope

- Tax / Pricing physical review
- Checkout resume (W6 / process manager)
- Frontend
- Broad Order recovery
- BATCH-006 invention

## Verified defects at start

- `Tooba.Cart.Application.csproj` → Catalog.Application + Inventory.Application
- `CartDirectory.cs` → `using Tooba.Catalog.Application` + Inventory.Application.*; `DateTimeOffset.UtcNow`; `?? new QuantityNormalizer()`
- `CartDomain.cs` → `UuidV7.New()` in factories; root god-file
- `Tooba.Settlement.Infrastructure.csproj` → Payment.Application
- `SettlementPaymentBridge.cs` → Payment.Application + Payment.Domain
- `SettlementDomain.cs` / `SettlementDirectory.cs` → Guid.NewGuid / UtcNow; root dumps
- Host `SettlementPanelComposer` → `SettlementDbContext` for AdminPayoutGridQueryEngine
- Host `SettlementEndpoints` → manual `Results.Json` + `ex.Message`
