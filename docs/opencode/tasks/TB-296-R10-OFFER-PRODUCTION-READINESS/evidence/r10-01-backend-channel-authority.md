# R10-01 backend — Offer channel authority (worker evidence)

- Date: 2026-11-26
- Branch: opencode/iso-agents-002
- Base commit (checked out, not committed): 571d187cbe9850b5c59576d3b3e3f91fade76807
- Scope: backend R10-01 only. No frontend, no Host business logic, no Inventory/History,
  no migrations, no unrelated modules. Changes remain uncommitted.
- Note: the worktree already contained uncommitted, pre-existing R10-03 inventory-provisioning
  changes (Inventory contracts/DI, ListEligibleInventoryLocationsQuery,
  EnsureOfferInventoryPositionCommand, OfferInventoryProvisioningAdminEndpoints). This slice did
  not create or revert them; it only adds the channel-authority entries to shared files.

## Behavior implemented
- Offer-owned `OfferChannelPolicy` derives allowed channels solely from `ToobaEdition`
  (`SingleStore => Direct`, `Marketplace => Marketplace`, Unset/unknown => empty).
- `GetAllowedOfferChannelsQuery` returns edition names as `Result<IReadOnlyList<string>>`; when the
  edition is unset/unknown it returns an empty success (fail-closed for creation), no invented default.
- `GET /v1/admin/offers/allowed-channels` (product.view) and
  `GET /v1/seller/offers/allowed-channels` (seller authorized) dispatch a thin MediatR query.
- `SellerOfferCreateRequest`/`AdminOfferCreateRequest` gain trailing `string? Channel = null`.
  Endpoints accept enum names case-insensitively, default from the edition policy (never a hardcoded
  Marketplace), and raise `offer.channel.required` / `offer.channel.not_allowed` (400) on failure.
- `CreateOfferHandler` injects `ICurrentEdition` and rejects a channel the edition policy disallows
  before any write.
- `FindActiveOfferQuery.Channel` is optional; handler/endpoints resolve the edition default and the
  same policy, no fixed Marketplace.
- Channel is carried through DTO -> command -> persisted aggregate -> reread; the aggregate stores
  `Channel { get; init; }`, so it is immutable after creation.
- Added stable codes `offer.channel.required`, `offer.channel.not_allowed` with 400 descriptors and
  English/Persian resources.
- Endpoint validator coverage manifest now includes `GetAllowedOfferChannelsQuery`
  (NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY); no entry deleted or weakened.

## Files changed (R10-01)
Added:
- src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Policies/OfferChannelPolicy.cs
- src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Queries/GetAllowedOfferChannels/GetAllowedOfferChannelsQuery.cs
- src/backend/Modules/Offer/Tooba.Offer.Endpoints/Errors/OfferChannelRequestResolver.cs
- src/backend/Modules/Offer/Tooba.Offer.Tests/Application/OfferChannelPolicyTests.cs

Modified:
- src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/CreateOffer/CreateOfferCommand.cs
- src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Queries/FindActiveOffer/FindActiveOfferQuery.cs
- src/backend/Modules/Offer/Tooba.Offer.Contracts/Dtos/SellerOfferPanelDtos.cs
- src/backend/Modules/Offer/Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs
- src/backend/Modules/Offer/Tooba.Offer.Endpoints/Admin/OfferAdminEndpoints.cs
- src/backend/Modules/Offer/Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs
- src/backend/Modules/Offer/Tooba.Offer.Endpoints/Errors/OfferErrorCatalogContributor.cs
- src/backend/Modules/Offer/Tooba.Offer.Endpoints/Resources/OfferErrors.resx
- src/backend/Modules/Offer/Tooba.Offer.Endpoints/Resources/OfferErrors.fa.resx
- src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferEndpointValidatorCoverageGuardTests.cs
- src/backend/Modules/Offer/Tooba.Offer.Tests/Application/OfferHandlerTests.cs
- src/backend/Modules/Offer/Tooba.Offer.Tests/Observability/OfferTraceTopologyTests.cs

## Commands and results
1. `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore -v minimal`
   - Result: Build succeeded. 0 Warning(s), 0 Error(s). Elapsed ~00:00:10.63.

2. `dotnet test src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj --no-restore -v minimal`
   - Result: Passed. Failed: 0, Passed: 113, Skipped: 0, Total: 113.
   - Covers new OfferChannelPolicy/resolver/query tests, OfferHandler create-channel tests,
     endpoint validator coverage manifest, error catalog/resource tests, physical/manifest/architecture
     guards, and trace topology.

## Test coverage added
- Policy: SingleStore allows only Direct; Marketplace allows only Marketplace; Unset/unknown allow none;
  `Default` returns null when none allowed.
- Query handler (fake ICurrentEdition via DI): projects edition names; empty success when unset.
- Resolver: case-insensitive enum-name parsing, edition default (not Marketplace), fail-closed for
  unpermitted/unknown channels (`ChannelNotAllowed`) and for no-allowed-channel (`ChannelRequired`).
- Create handler: rejects a disallowed channel with zero writes; persists and rereads the allowed
  channel, and keeps it unchanged across an update.

## Not run (out of this slice)
- No live DB migration, no connected/browser persistence proof, no frontend changes. Full R10-01
  P/N/B connected matrix remains for the owning connected run.
