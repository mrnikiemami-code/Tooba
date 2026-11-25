# R10-06 — Backend Offer history (implement + verify)

Scope: backend Offer-owned history read projection only. No frontend, no channel, no inventory
provisioning, no Host business logic, no live migration application, no commit.

## Source state

- Branch: `opencode/iso-agents-002`
- Base commit: `571d187c` plus the pre-existing uncommitted R10 channel/inventory working-tree changes.
- This slice adds only Offer-owned history files/changes listed below.
- No database was contacted or mutated. The EF migration was created but **not applied**.

## Migration (created, not applied)

- Name: `20261126120000_R10OfferHistory`
- File: `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/20261126120000_R10OfferHistory.cs`
- Table: `offer.offer_history` (additive, no existing table altered, no backfill)
- Columns: `offer_history_entry_id`, `offer_id`, `occurred_at`, `change_kind`,
  `actor_user_id`, `summary`, `old_value`, `new_value`
- Index: `ix_offer_history_offer_id_occurred_at` on (`offer_id` asc, `occurred_at` desc)
- `OfferDbContextModelSnapshot.cs` updated to include the new entity/table/index.
- Migration generated manually to match the repository's existing manual-migration convention
  (`dotnet ef` would have produced a destructive diff because the checked-in snapshot still records
  the pre-Aggregates entity name `Tooba.Offer.Domain.SellerOffer`).
- Verified without applying: `dotnet ef migrations script ... -o evidence/r10-06-migration.sql`
  emitted `CREATE TABLE offer.offer_history (...)` with all columns, the
  `ix_offer_history_offer_id_occurred_at` index (`occurred_at DESC`) and the migration-history row
  `20261126120000_R10OfferHistory` (`8.0.11`). This command generated SQL only; it did not connect to or
  mutate the dev database.

## New files

- `src/backend/Modules/Offer/Tooba.Offer.Domain/Aggregates/OfferHistoryEntry.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Ports/IOfferHistoryWriter.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Ports/IOfferHistoryReader.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/History/OfferHistorySummaries.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Queries/GetOfferHistory/GetOfferHistoryQuery.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Queries/GetOfferHistory/GetOfferHistoryQueryValidator.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Contracts/Dtos/OfferHistoryDtos.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Adapters/OfferHistoryStore.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Configurations/OfferHistoryConfiguration.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Migrations/20261126120000_R10OfferHistory.cs`
- `src/backend/Modules/Offer/Tooba.Offer.Tests/Application/OfferHistoryTests.cs`

## Modified Offer files

- `OfferDbContext.cs`: `OfferHistoryEntries` DbSet + configuration registration.
- `OfferModule.cs`: scoped `OfferHistoryStore` bound to both reader and writer ports.
- `OfferValidationCodes.cs`: history page/page-size machine codes.
- `CreateOfferCommand.cs`: `Created` entry staged on the same unit of work before `PersistNewAsync`.
- `UpdateOfferCommand.cs`: `CommercialChanged` old/new commercial snapshot.
- `SetOfferPriceCommand.cs`: `PriceChanged` old/new amount/currency via the existing Price read port.
- `SetOfferInventoryCommand.cs`: `InventoryChanged` old on-hand and post-write
  stockItemId/location/on-hand/reason via the existing Inventory read port.
- `AssignOfferFulfillmentCommand.cs`: `FulfillmentChanged` old/new responsibility/type.
- `OfferAdminEndpoints.cs`: `GET /v1/admin/offers/{offerId}/history?page&pageSize` (view gate).
- `OfferSellerEndpoints.cs`: `GET /v1/seller/offers/{offerId}/history?page&pageSize` (seller gate + ownership).
- `OfferEndpointValidatorCoverageGuardTests.cs`: manifest 14→15, validators 7→8, new query classified
  `VALIDATOR_REQUIRED`; assertions were strengthened in count only, none deleted/weakened.
- `OfferHandlerTests.cs` and `OfferTraceTopologyTests.cs`: register the new writer dependency; handler
  test records `Created` and `CommercialChanged` with null actor.

## History semantics

- No fabricated rows: entries are appended only after a successful owning write; no historical backfill.
- Actor: `null` for every recorded command because no current Offer command carries an actor id.
- Read: seller callers must own the offer (same `offer.not_found` fail-closed response); admin callers
  pass no seller filter. Page ≥ 1 and pageSize clamped 1..100 (plus overflow-safe skip).
- Transaction: `AppendAsync` only stages on the scoped `OfferDbContext`; `CreateOffer` commits the Offer
  and history in a single `SaveChangesAsync`, and update/fulfillment/price/inventory commit history in the
  same Offer `SaveChangesAsync` as their tracked Offer write.

## Acceptance-case evidence (R10-06)

- `R10-06-P` (recorded mutation → correct event/time/scope):
  `OfferHandlerTests.Create_and_update_stage_history_entries_with_unknown_actor`,
  `OfferHistoryTests.Reader_orders_by_occurred_at_descending_and_pages`,
  `OfferHistoryTests.Seller_history_read_returns_the_owned_offer_page`,
  `OfferHistoryTests.Writer_stages_the_entry_until_the_single_offer_save_changes`.
- `R10-06-N` (foreign seller denied, no disclosure):
  `OfferHistoryTests.Seller_history_read_requires_ownership_and_never_fabricates_a_reader_call`,
  `OfferHistoryTests.History_read_of_a_missing_offer_is_not_found_and_does_not_query_history`.
- `R10-06-B` (empty/unknown actor, stable paging):
  `OfferHistoryTests.Reader_clamps_page_and_page_size_to_one_and_one_hundred`,
  `OfferHistoryTests.Admin_history_read_has_no_seller_filter`,
  `OfferHistoryTests.History_query_validator_rejects_out_of_range_paging`,
  `OfferHistoryTests.OfferDbContext_maps_offer_history_and_the_R10_migration_is_discoverable`.

## Commands and results

- `dotnet test src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj`
  → PASS: Failed 0, Passed 123, Skipped 0 (net8.0).
  Log: `evidence/r10-06-offer-tests.log`
- `dotnet build src/backend/Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj`
  → Build succeeded, 0 errors.
  Log: `evidence/r10-06-offer-build.log`
- `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj`
  → Build succeeded, 0 errors.
  Log: `evidence/r10-06-host-build.log`
- `dotnet ef migrations script --project Tooba.Offer.Infrastructure --startup-project Tooba.Offer.Infrastructure --context OfferDbContext --no-build -o evidence/r10-06-migration.sql`
  → generated SQL contains the additive `offer.offer_history` table, descending index and migration id.
  Output: `evidence/r10-06-migration.sql`

## Build-blocking repair of pre-existing uncommitted artifacts (transparency)

The pre-existing R10 working tree contained literal `\n` escape text where real newlines were intended.
Those files did not compile before this slice. They were repaired to the intended statements only:

- `src/backend/BuildingBlocks/Tooba.BuildingBlocks/Presentation/Errors/FoundationErrorCatalogContributor.cs`
  (new `concurrency.conflict` descriptor was also given the valid `ErrorDescriptor` shape; the
  non-existent `Retryable`/`Arguments` named arguments were replaced with `SafeTitleFallback`).
- `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/Persistence/Configurations/SellerOfferConfiguration.cs`
- `src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Persistence/Configurations/AuthoredPriceConfiguration.cs`
- `src/backend/Host/Tooba.Host/Errors/ToobaExceptionHandler.cs`

No channel/inventory business behavior was changed by this slice.

## Not done / not claimed (by instruction)

- Migration not applied to any database; no `tooba_alpha` mutation.
- No live-DB GET/SQL persistence proof is claimed for history; the reader/writer are proven against the
  owning EF model (InMemory) and the migration SQL was generated, but durable PostgreSQL proof requires the
  owner-applied migration and is therefore NOT_RUN here.
- No frontend changes.
- No commit.
