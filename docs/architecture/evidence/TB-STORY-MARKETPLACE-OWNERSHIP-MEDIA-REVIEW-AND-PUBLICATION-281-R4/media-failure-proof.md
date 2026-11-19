# TB-281-R4 Media failure proof

## R3 correction addendum

`MediaStorySellerUpload281R3Tests` fact formerly named as SaveChanges-failure was renamed to an honest replay assertion. Real SaveChanges injection lives in R4.

## InMemory recording + interceptor (`MediaStorySellerUpload281R4Tests`)

1. **Db fail before commit**: store succeeds; interceptor throws before commit → zero durable asset/owner/occurrence; attempt object deleted; caller entity instances untouched.
2. **After-save throw**: commit succeeds then throw → fresh read shows asset+binding+occurrence; retry returns same asset; object not deleted; no second asset.

## Exact unique identity

`IsOccurrenceUniqueViolation` requires all of:
- `PostgresException.SqlState == 23505`
- `SchemaName == media`
- `TableName == asset_owner_occurrences`
- `ConstraintName == pk_asset_owner_occurrences`

Substring OR matching is not used.

## Ambient transaction

If `CurrentTransaction` is non-null, upload refuses with typed `media.story_upload.ambient_transaction_unsupported` **before** object store. Proven on relational C3 (InMemory does not support ambient transactions).

## Limits

- Controlled interceptor / recording store only; no live object-store or production outbox.
- Unrelated unique violations do not fold into winner replay.
