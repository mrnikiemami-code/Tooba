# TB-281-R5 media failure / transaction proof

## B — RefuseConstrainedTransaction

Source: `MediaDirectory.Story.cs` `RefuseConstrainedTransaction` + `HasEnlistedProviderTransaction`.

Refuses before object-store when any of:

1. `_db.Database.CurrentTransaction is not null`
2. `System.Transactions.Transaction.Current is not null`
3. relational `IRelationalConnection.EnlistedTransaction is not null`

Typed code: **`MediaErrorCodes.ValidationFailed`** (honest; no fabricated `media.story_upload.ambient_transaction_unsupported`).

Proven on PostgreSQL in `StoryMarketplace281R5RelationalTests.C2_media_ef_current_ambient_scope_and_enlistment_refuse_before_store` with pre-action asserts of each shape and `store.Saved` empty; independent tagged occurrence count 0 after.

## C3 — caller entity / after-save / committedRows

- `Pre_save_failure_preserves_caller_owned_tracked_entity_state`: real `MediaAsset` staged then attached Unchanged; after forced SavingChanges failure state/id/storageKey/name unchanged; attempt object cleaned.
- `After_save_throw_fresh_context_sees_durable_asset_binding_occurrence_and_replay`: durable rows on **fresh** MediaDbContext; object Exists; replay same MediaAssetId.
- `Relational_rollback_only_independent_committed_rows_zero_for_test_owned_ids`: refused upload under caller EF tx; independent context `AssetOwnerOccurrences` LongCount for runTag = 0.
