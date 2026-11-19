# TB-281-R5 rollback-only PostgreSQL proof

Canonical: `127.0.0.1:5432/tooba_alpha`. Connection env `TB281R5_CONNECTION` / `TB281R4_CONNECTION` / default admin local. Collection `PostgresSerial`.

## C2_fresh_context_public_expired_admin_grid_shop_filter_and_seller_isolation

- Write fixtures inside caller transaction on StoryDbContext A.
- Read via **fresh** StoryDbContext B sharing the same DbTransaction.
- Public reader: publishable visible; future + **expired** schedule excluded.
- `AdminStoryGridQueryEngine` with Party port: `shopDisplayName contains Alpha` page + totalCount; other shop excluded.
- `SellerQueryAsync` isolation to shopA.
- Rollback → independent context `Stories.Title.StartsWith(runTag)` count **0**.

## C2_two_context_stale_version_owning_command_zero_extra_occurrence

- Seller create then two contexts share transaction.
- Writer1 `SellerUpdateAsync` advances version.
- Writer2 `SellerUpdateAsync` with **original** version → `story.version.stale`; occurrence count for tag unchanged.
- Rollback → independent story + occurrence tagged counts **0**.

## C2_media_ef_current_ambient_scope_and_enlistment_refuse_before_store

- EF CurrentTransaction refuse; TransactionScope ambient refuse; CommittableTransaction enlistment refuse (CurrentTransaction null, Transaction.Current null, EnlistedTransaction same).
- Zero store saves; independent media occurrence tagged count **0**.

No EnsureCreated / DDL / committed business rows.
