# Host/Seller — Seller-R3 — Migration

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R3
**Parent:** TB-TMAR-HOST-SELLER-AMC-001-R2
**Skill:** `.cursor/skills/tooba-architecture-migrate/SKILL.md`

## 1. Target architecture

```
GET /v1/seller/settings    -> PartySellerSettingsEndpoints
PUT /v1/seller/settings    -> PartySellerSettingsEndpoints
        |  ISender (MediatR 12.5) + ApiResponseFactory + IPartySellerAuthorizer
        v
Tooba.Party.Application
   Seller/Queries/GetSellerSettingsQuery + Handler        (capability: seller settings read)
   Seller/Commands/UpdateSellerSettingsCommand + Handler  (capability: seller settings write)
   Seller/Validators/UpdateSellerSettingsCommandValidator (transport validator, write only)
   Seller/Models/PartySellerSettingsModels                (read/write models)
   Seller/PartySellerSettingsErrorCodes                   (stable codes + transport validation codes)
        v
Tooba.Party.Contracts
   IPartySellerSettings + PartySellerSettingsSnapshot + PartySellerSettingsWrite
        v
Tooba.Party.Infrastructure
   Seller/PartySellerSettingsAdapter : IPartySellerSettings   (delegates to IPartyDirectory)
   PartyModule registers IPartySellerSettings
```

Authorization (Host-owned platform security, unchanged R1A boundary):

```
Party Endpoints -> IPartySellerAuthorizer  (port, Party.Endpoints-owned)
                        ^
Host/Security/Seller/HostPartySellerAuthorizer  (thin adapter, R1A namespace)
                        -> ISellerPanelAccess.RequireAuthorizedAsync
                        -> IPlatformEffectiveAccessReader.GetEffectivePermissionsAsync
```

## 2. Created files

| File | Purpose |
| --- | --- |
| `Modules/Party/Tooba.Party.Contracts/IPartySellerSettings.cs` | Party-owned persistence seam + `PartySellerSettingsSnapshot` / `PartySellerSettingsWrite` |
| `Modules/Party/Tooba.Party.Application/Seller/PartySellerSettingsErrorCodes.cs` | `seller.settings.missing` / `seller.settings.rejected` constants + transport validation codes |
| `Modules/Party/Tooba.Party.Application/Seller/Models/PartySellerSettingsModels.cs` | `PartySellerSettingsView` (`partyId`/`canManage` shape), read/write models |
| `Modules/Party/Tooba.Party.Application/Seller/Queries/GetSellerSettingsQuery.cs` | MediatR query `IRequest<Result<PartySellerSettingsView>>` + handler (404 `seller.settings.missing` on absent profile) |
| `Modules/Party/Tooba.Party.Application/Seller/Commands/UpdateSellerSettingsCommand.cs` | MediatR command `IRequest<Result<PartySellerSettingsView>>` + handler (400 `seller.settings.rejected` on domain reject) |
| `Modules/Party/Tooba.Party.Application/Seller/Validators/UpdateSellerSettingsCommandValidator.cs` | FluentValidation transport validator for the write command |
| `Modules/Party/Tooba.Party.Infrastructure/Seller/PartySellerSettingsAdapter.cs` | Implements `IPartySellerSettings` over `IPartyDirectory` (no `DbContext`, no EF) |
| `Modules/Party/Tooba.Party.Endpoints/Tooba.Party.Endpoints.csproj` | New canonical Endpoints project (Application + Contracts + BuildingBlocks) |
| `Modules/Party/Tooba.Party.Endpoints/Seller/IPartySellerAuthorizer.cs` | Neutral Party seller auth port (`RequireViewAsync` / `RequireManageAsync`) |
| `Modules/Party/Tooba.Party.Endpoints/Seller/PartySellerSettingsEndpoints.cs` | Both HTTP routes + `PartySellerSettingsWriteRequest` transport record |
| `Modules/Party/Tooba.Party.Endpoints/PartyEndpointModule.cs` | `AddPartyEndpointPresentation` + `MapPartyEndpoints` (`/v1/seller/settings` group) |
| `Modules/Party/Tooba.Party.Endpoints/Errors/PartyErrorCatalogContributor.cs` | Registers Party-owned error descriptors (404/400) |
| `Modules/Party/Tooba.Party.Endpoints/Resources/PartyErrorResources.cs` (+ `PartyErrors.resx`, `PartyErrors.fa.resx`) | Party error resource set owning `seller.settings.*` keys |
| `Host/Tooba.Host/Security/Seller/HostPartySellerAuthorizer.cs` | Thin Host adapter inside the R1A boundary |
| `Host/Tooba.Host.Tests/Architecture/HostSellerAmcR3GuardTests.cs` | Durable R3 architecture guard |

## 3. Edited files

| File | Change |
| --- | --- |
| `Host/Tooba.Host/Seller/SellerSettingsEndpoints.cs` | **DELETED** — both routes evacuated to Party |
| `Host/Tooba.Host/Program.cs` | `AddPartyEndpointPresentation()`, `MapPartyEndpoints()` (replacing `MapSellerSettingsEndpoints()`), `IPartySellerAuthorizer -> HostPartySellerAuthorizer` registration, and `GetSellerSettingsQuery` assembly added to CQRS registration |
| `Modules/Party/Tooba.Party.Infrastructure/PartyModule.cs` | registered `IPartySellerSettings -> Seller.PartySellerSettingsAdapter` |
| `Modules/Party/Tooba.Party.Application/Tooba.Party.Application.csproj` | added `Tooba.BuildingBlocks` + `Tooba.Party.Contracts` references |
| `Host/Tooba.Host/Tooba.Host.csproj` | added `Tooba.Party.Endpoints` project reference |
| `src/backend/Tooba.slnx` | registered the new `Tooba.Party.Endpoints` project |
| `Host/Tooba.Host.Tests/SettingsFoundationTests.cs` | assertions re-pointed to the Party surface + `HostPartySellerAuthorizer` (no assertion weakened) |
| `Host/Tooba.Host.Tests/Architecture/HostSellerAmcR1GuardTests.cs` | added `HostPartySellerAuthorizer.cs` to the boundary/thin-adapter assertions (no assertion weakened) |
| `Host/Tooba.Host.Tests/Architecture/HostSellerAmcR2GuardTests.cs` | Host/Seller file-count and mapping expectations adjusted for R3 (5 -> 4 files, settings routes gone) |
| `Host/Tooba.Host.Tests/TmarDurableGuardTests.cs` | durable recovery checkpoint assertions advanced to the R3 stop |

## 4. Behavior preservation technique

- Route path and verbs are identical: `GET /v1/seller/settings`, `PUT /v1/seller/settings`.
- `SellerPartyId` still comes from the seller authorization boundary, never from the request body.
- The success JSON still returns exactly `partyId`, `displayName`, `legalName`, `description`,
  `supportPhone`, `supportEmail`, `addressLine`, `updatedAt`, `canManage` (PUT returns
  `canManage = true`).
- The stable codes are preserved: `seller.settings.missing` (404), `seller.settings.rejected` (400),
  `seller.authorization.denied` (403, owned by the Foundation/Host boundary, not re-registered).
- `seller.settings.view` is mandatory on the read route (absence = 403 fail-closed);
  `seller.settings.manage` is optional on the read route (absence only yields `canManage = false`)
  and mandatory on the write route. The exact prior Host semantics are preserved.
- Domain reject on write (`InvalidOperationException` from Organization update) maps to the same
  400 `seller.settings.rejected`.
- `ex.Message` HTTP classification is gone; the platform denial now flows through the canonical
  `ApiResponseFactory` / `SemanticError` path with the unchanged `seller.authorization.denied` code.
- No schema/migration change: the Party Organization profile storage is untouched.

## 5. Route ownership after R3

Host-owned seller routes = 2: `GET /v1/seller/dashboard`, `GET /v1/seller/dev-contexts`.
Party-owned seller routes = 2 (the migrated settings set). Duplicate ownership = 0.

## 6. Not migrated (explicitly retained)

`GET /v1/seller/dashboard` still composes the Order dashboard summary via Order CQRS from
Host/Seller, and `SellerPanelComposer.GetSellerDisplayAsync` keeps the Party display lookup used by
that dashboard shell. `SellerDevActorBootstrap.cs` is untouched. Seller-R4..R6 remain NOT_STARTED.
