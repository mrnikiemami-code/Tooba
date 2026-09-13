# TB-P10-T004-R23 — Runtime A–N

Host `:5088` + FE `:3000`. Dev customer `09111111111` / `123456`. Script `docs/evidence/TB-P10-T004/_r23-runtime.mjs`. Failed 0.

| Case | Result |
| --- | --- |
| A unpaid Order #1 | PASS checkout `01a09987-c777-7000-9025-9776bc703662`; one Cycle #1 event |
| B unpaid Order #2 | PASS `01a09987-c9dd-7000-94cb-7b3fda93a59c`; open=2 |
| C third blocked | PASS 409 `checkout.open_unpaid_limit_reached`; reservations 21→21; cart Active |
| D hide still blocked | PASS hide 200 after expire; third still 409; count unchanged |
| E cancel frees slot | PASS cancel B; open=1 |
| F replacement | PASS `01a09987-d201-7000-b7c5-ab45970e5256` |
| G cancel no churn refund | PASS history kept |
| H churn max | PASS 409 `checkout.reservation_commit_limit_reached`; reservations 22→22 |
| I payment retry no churn | PASS commits 10→10 |
| J Cycle #2 | PASS retry 200; events for A still 1 |
| K pay frees slot | PASS sandbox 200; SellerOrder Paid after payment outbox |
| L concurrent last slot | PASS wins=1 blocks=1 (200/409 open_unpaid) winner `01a09987-e312-7000-895f-402f89b7d43b` |
| M Admin settings | PASS GET 2/30/3; save 200; invalid 400 |
| N AuthenticatedOnly | PASS shipping 401 |

Raw: `_r23-runtime-raw.json`
