# Media failure / recording-store proof (R3)

Owner: `MediaDirectory.UploadForSellerAsync` + `IMediaObjectStore` recording fixture.

| Case | Fact | Limit |
| --- | --- | --- |
| Success | one Saved key; asset+binding+occurrence; fresh InMemory context reads same asset id | InMemory EF, not live object store |
| Exact replay | same occurrence+bytes → same asset; Saved.Count stays 1 | |
| Changed bytes | OwnerForbidden; no new object/row | |
| Store failure | StorageUnavailable; 0 assets/occurrences; Saved empty | |
| Foreign shop authorize | OwnerForbidden on AuthorizeForStoryAsync | |
| Unique/winner | PostgresException 23505 scoped to asset_owner_occurrences; identical intent folds to winner | concurrent DbUpdateException branch not stress-raced on live PG |

IsUniqueViolation no longer matches generic `unique`/`duplicate` message substrings.
