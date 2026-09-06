# TB-P09-T002-R1 — No N+1

## Notes list

1. Load ≤50 notes from Order checkout directory.
2. Collect distinct `CreatedByUserId`.
3. One `GetManyAsync` + one `GetContactsAsync`.
4. Map each note through the in-memory label dictionary.

## Operational history page

1. Compose full draft timeline (system + user actor ids).
2. Page in memory (`Skip`/`Take`).
3. Resolve **only actor ids present on the current page** via the same batch pair.
4. No Identity/profile call per row.

## Evidence of bounded calls

Distinct id set size ≤ page size (history ≤ 50) or notes limit (50). Repeated actors share one dictionary entry.
