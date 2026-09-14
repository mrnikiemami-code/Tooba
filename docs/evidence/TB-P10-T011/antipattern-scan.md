# Anti-pattern scan

| Check | Result |
| --- | --- |
| Arbitrary query language / SQL | CLEAN — forbidden keys rejected |
| Executable HTML/CSS/JS | CLEAN — RichText rejects `<>`; no markup fields |
| Giant page JSON blob endpoint | CLEAN — typed section DTOs |
| Unvalidated polymorphic config | CLEAN — per-type validators |
| Cross-store refs | CLEAN — catalog isolation + entity exists |
| Delete/recreate reorder | CLEAN — SetSortOrder on same IDs |
| Section/Menu builder UI | CLEAN — API only |

Scan: CLEAN.
