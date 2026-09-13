# TB-P10-T004-R24 — Runtime A–Z

Host `:5088` + FE `:3000`. Dev customer `09111111111` / `123456`. Script `docs/evidence/TB-P10-T004/_r24-runtime.mjs`. Failed 0.

| Letter | Case | Result |
| --- | --- | --- |
| A | Anonymous Cart | PASS 200 |
| B | Login gate | PASS 401 authentication_required |
| C | OTP login | PASS 200 user `01a0996c-b8d9-7000-9854-80b3f59e8d7c`; `/fa/login`+`/en/login` 200 |
| D | Cart merge | PASS 200 lines=1 res 34→34 orders 23→23 |
| E | Atomic success | PASS checkout `01a09993-9bdc-7000-b251-2b608e43d8d9`; one Cycle #1 event |
| F | Inventory failure | PASS 400 `checkout.rejected`; no checkout; commits 22→22; res 34→34 |
| G | Open unpaid 0/2 | PASS first commit |
| H | Open unpaid 1/2 | PASS second `01a09993-9e24-7000-9da7-c181759d080a` |
| I | Open unpaid 2/2 | PASS 409 open_unpaid; cart Active |
| J | Hide still counts | PASS hide 200 after expire; still 409 |
| K | Cancel frees slot | PASS open=1 |
| L | Cancel no churn refund | PASS history kept |
| M | Churn max | PASS 409 reservation_commit; res unchanged |
| N | Payment retry no churn | PASS 25→25 |
| O | Cycle #2 no extra churn | PASS retry=200 events=1 |
| P | Manual AwaitingAdmin | PASS `01a09993-c0d4-7000-9fc5-041f25f03e11` open=1 |
| Q | Pay frees slot | PASS SellerOrder Paid |
| R | Concurrent last open | PASS wins=1 blocks=1 winner `01a09993-b6b4-7000-94fa-015b2702ae07` |
| S | Concurrent last churn | PASS wins=1 churnBlocks=1 |
| T | Flash-sale policy 3/1/2 | PASS 200; abuse blocked by churn/open limits |
| U | Admin Settings | PASS get/save/validation + identity + reservation-policy |
| V | Security | PASS anon 401; admin PUT 401 |
| W | Multi-seller financial | PASS allocation suite + StoreShipping source |
| X | Decimal 1.25 | PASS suite exists (focused Host green) |
| Y | Network / no polling | PASS cart 200; no checkout polling |
| Z | Logout isolation | PASS 204/401 |

Raw: `_r24-runtime-raw.json`
