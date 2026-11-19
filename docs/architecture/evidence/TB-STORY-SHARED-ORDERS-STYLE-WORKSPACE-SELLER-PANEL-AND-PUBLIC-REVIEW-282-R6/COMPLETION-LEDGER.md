# COMPLETION-LEDGER — TB-282-R6

| Item | Production change | Admin before → after | Seller before → after | Proof |
| --- | --- | --- | --- | --- |
| A full form | `StoryManagementScreen` create workspace; Dialog title-only gone | /new was title+placement modal | same title-only modal | R6 mount A |
| B filter | `isListSurface && canReview` | filter on detail/create | none | R6 mount B |
| B location | `shopLabel` + `locationPrefix` | mixed/internal copy | — | R6 mount B/C |
| C destinations | `destinationLabelForItem` maps internal→فروشگاه | raw `internal` | same | R6 mount B/C |
| C catalogs | `story-management-copy.ts` | “Story” in description | — | copy unit + mount |
| D HTTP cookies | `authCookieSecure(request)` | login dropped CSRF on next start | same | cookie-secure 3/3 |
| ZERO write | no MigrationRunner / no live POST | — | — | receipts |
