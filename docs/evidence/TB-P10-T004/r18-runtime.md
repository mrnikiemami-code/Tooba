# R18 runtime A–J

Executed as focused Host in-memory + source proofs (Host :5088 was not required for policy write/read).

| Case | Result |
| --- | --- |
| A Store default save + effective | PASS — ReplaceStore 1/43200/20 persists; preview source=store |
| B Category override | PASS — category initial 15 overrides store 30 |
| C Offer override | PASS — offer 3/2 inherit max from category |
| D clear Offer → Category effective | PASS — after null PUT, initial source=category |
| E clear Category → Store effective | PASS — after null PUT, initial source=store |
| F active cycle not extended | PASS — ExpiresAt unchanged after Settings |
| G future cycle uses new policy | PASS — retry cycle hold=8 from updated store |
| H Offer flash-sale 2-minute retry | PASS — offer retry=2 and FlashSaleStricter |
| I permissions | PASS — Admin RequireAuthorized; Seller 403 path; no catalog permission |
| J Admin audit event | PASS — 3 field audit rows for store write |
