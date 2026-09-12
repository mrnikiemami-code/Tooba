# TB-P10-T004-R14 — Concurrency

| Race | Result |
| --- | --- |
| Success vs new initiate (new key) | HasSucceeded (or pending.Length==0 / Paid) → 409; at most one Succeeded financial outcome |
| Double-click same key | first creates; second replays existing row |
| Duplicate success Verify | `Status==Succeeded` returns NewlySucceeded=false |
| Retry after success | 409 |

Directory `InitiateAsync` checks Succeeded on the checkout **before** `CustomerPayment.Open`.
