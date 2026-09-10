# Runtime matrix A–O
| ID | Scenario | Result |
|---|---|---|
| A | Auth happy | N/A justified (guest-primary + shell smoke; no Playwright login E2E) |
| B | Guest happy | PASS |
| C | Multi-seller | PASS |
| D | Price change | PASS (focused tests) |
| E | Inventory race | PASS (focused tests) |
| F | Shipping invalidated | PASS |
| G | Earlier delivery | PASS |
| H | Payment amount spoof | PASS |
| I | Idempotency | PASS |
| J | Refresh/back shells | PASS |
| K | Wrong guest | PASS |
| L | FA/EN | PASS |
| M | Visual smoke | PASS (desktop browser + HTML) |
| N | Recommendation ATC | PASS (guard + cart shell) |
| O | Paid reservation TTL | PASS (focused tests) |
Raw: runtime-matrix-raw.json / _runtime.mjs
