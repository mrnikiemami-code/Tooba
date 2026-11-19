# TB-281-R5 COMPLETION LEDGER

method → assertion → provider → observed

| Method / fact | Assertion | Provider | Observed |
| --- | --- | --- | --- |
| A1 SellerUpdate / AdminUpdateItem / SellerUpdateItem | missing key zero-write OccurrenceRequired; success+same-key replay; changed body/item conflict | InMemory StoryDirectory | PASS |
| A2 update hash | same-key changed locale/market/cover-id/cover-url conflict; delimiter pair conflict | InMemory | PASS |
| A3 AdminSetSchedule | success then exact original replay; time conflict; stale NEW key VersionStale | InMemory seller→submit→approve path | PASS |
| A4 unique identity | exact schema/table/constraint true; wrong false | Reflection IsCommandOccurrenceUniqueViolation | PASS |
| A4 winner fold | 23505 pk_command_occurrences → one winner row | InMemory fault injection RaceWinnerInterceptor | PASS |
| A4 unrelated unique | propagates DbUpdateException; zero occurrences | InMemory interceptor | PASS |
| B Media refuse | CurrentTransaction / Transaction.Current / EnlistedTransaction → ValidationFailed; 0 store calls | PostgreSQL MediaDirectory | PASS |
| C2 fresh+grid | public excludes future+expired; AdminStoryGridQueryEngine shopDisplayName; seller query isolation; independent committedRows=0 | PostgreSQL rollback-only | PASS |
| C2 two-context stale | writer2 owning SellerUpdate VersionStale; zero extra occurrence; committedRows=0 | PostgreSQL shared tx two contexts | PASS |
| C3 Media caller entity | Unchanged tracked asset preserved across pre-save UploadFailed | InMemory + interceptor | PASS |
| C3 after-save fresh | asset+binding+occurrence visible on fresh context; replay same id | InMemory | PASS |
| C3 media committedRows | independent occ LongCount=0 after rollback refuse | PostgreSQL | PASS |
| Negative control | blank RequireOccurrenceKey → A1 FAIL; restore PASS | local patch | PASS |
| Story AMSC | 36/36 incl. Persian XML on Occurrences.cs | Host.Tests | PASS |
