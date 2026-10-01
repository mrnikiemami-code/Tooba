# Dependency classification — Host/Transport AMC-001

| Dependency | Location | Classification | Rationale |
| --- | --- | --- | --- |
| `Tooba.Persistence.OutboxMessage` | BuildingBlocks `Tooba.Persistence` | **GENERIC_PLATFORM_ALLOWED** | Shared platform outbox row shape used to rehydrate deserialize+commerce context; not module-owned persistence / not a commerce DbContext |
| `IIntegrationEventSerializer` | BuildingBlocks `Tooba.Persistence` | **GENERIC_PLATFORM_ALLOWED** | Platform type-map serializer for integration envelopes; neutral across modules |
| `WorkerCommerceContextFactory` | Host `Outbox/OutboxWorkerSeams.cs` (`Tooba.Host`) | **GENERIC_PLATFORM_ALLOWED** | Host process worker commerce-context reconstruction from durable envelope columns; not module Application/Domain |
| `IIntegrationEventHandler<>` | `Tooba.BuildingBlocks` | **GENERIC_PLATFORM_ALLOWED** | Neutral handler contract; consumer never branches on concrete module event types |
| `ICommerceContextAssigner` | `Tooba.BuildingBlocks` | **GENERIC_PLATFORM_ALLOWED** | Platform commerce-context assignment seam |
| `ICurrentTenant` | `Tooba.BuildingBlocks` | **GENERIC_PLATFORM_ALLOWED** | Platform tenant invariant check (envelope TenantId vs assigned context) |

## Foreign-layer scan

Module `.Application` / `.Domain` / `.Infrastructure` / `.Persistence` references under Host/Transport = **ZERO**.  
Module DbContext usage = **ZERO**.  
No `FOREIGN_LAYER_BLOCKER` found; no neutral-seam repair required.
