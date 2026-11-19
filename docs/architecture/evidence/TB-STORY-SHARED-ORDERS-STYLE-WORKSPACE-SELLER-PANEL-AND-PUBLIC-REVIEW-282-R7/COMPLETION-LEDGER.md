# COMPLETION-LEDGER — TB-282-R7

| Item | Production change | Admin before → after | Seller before → after | Proof |
| --- | --- | --- | --- | --- |
| A create/add | one draft + post-load slide dialog | double push + dialog on /new | same | R7 mount A both roles |
| A preview | detail `story-detail-preview` | empty forever on /new | same | R7 mount A |
| A dirty leave | DS unsaved Dialog | SPA back ignored dirty | same | R7 dirty test |
| A unknown create | empty 200 retains pending | false success risk | — | R7 unknown test |
| B destinations | shop only when GUID resolves | internal→فروشگاه+path | same | R7 B + R6 updated |
| B origin | creator Admin/Seller | Admin→ویترین اصلی | — | R7 B |
| C cookies | loopback HTTP only | any HTTP insecure | same | cookie-secure 6/6 |
| C Host | skip-bootstrap=1 | R6 bootstrap UNKNOWN | — | receipts |
| ZERO write | no MigrationRunner / no live Story POST | — | — | receipts |
