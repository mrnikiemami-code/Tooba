# TB-281-R5 CORRECTION-ADDENDUM (R4 overstated claims)

R4 historical RESULT/RECEIPTS/matrix files are **not rewritten**. This addendum records where R4 text exceeded executed assertions and how R5 closed them.

| R4 claim | Actual R4 fact | R5 correction |
| --- | --- | --- |
| Seller/Admin item update occurrence required end-to-end | Owner ignored DTO key on SellerUpdate/AdminUpdateItem/SellerUpdateItem | A1 owning RequireOccurrenceKey + replay + record |
| Update hash covers all written fields | AdminUpdate hashed title/CTA only; pipe-join ambiguity | A2 length-prefixed per-field hashes + delimiter conflict test |
| Schedule replay after success | Version/content gates before TryReplay → VersionStale on original | A3 replay before gates |
| Occurrence unique arbitration on SaveChanges | Sequential replay only; no exact PK fold | A4 SaveChangesWithOccurrenceArbitrationAsync + fault-injection fold |
| Media ambient/caller protection | Only EF CurrentTransaction proven; report named ambient | B Transaction.Current + EnlistedTransaction; code stays ValidationFailed |
| C1 fresh context + expired + admin grid shop filter | Same context; no expired; no AdminStoryGridQueryEngine | C2 fresh-context package |
| C2 two-context EF concurrency + winner | EnsureExpectedVersion direct + raw insert | C2 two-context owning command stale; A4 winner fold |
| C3 Media committedRows=0 independent + caller entity | Printed phrase; no staged caller entity; same-context after-save | C3 real caller entity + fresh context + independent occ count |
