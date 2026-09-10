# Runtime smoke

Ports: Host :5088, FE :3000

| Scenario | Result |
|---|---|
| A auth/guest cart + methods | PASS — 7 catalog methods, post:express 200000 |
| B new address + persist | PASS — draft persists note/date/method |
| C store config tipax off | PASS — tipax omitted from projection |
| D delivery minimum | PASS — early 400 too_early; min accepted |
| E multi-seller formula | PASS — maxPrep applied (unit + runtime prep) |
| F handoff /payment | PASS — checkoutId + shippingAmount on commit; /fa/payment 200 |

Raw: runtime-smoke-raw.json
Payment route is **template-only handoff** (expected; T003 not started).
