# TB-P10-T004-R12 — Dead-code audit

| Candidate | Verdict |
| --- | --- |
| Cart `ReserveAsync` | Gone — CartDirectory has no ReserveAsync |
| Cart `ReleaseAsync` on line change/remove | KEEP — leftover historical `cart_lines.reservation_id` (LOCK-SF-054) |
| Duplicate reacquire | KEEP single `EnsureOrderSupply` |
| Old reservation-not-active UI | No raw mapping found on customer/result pages |
| Payment-result polling | `shouldPollStorefrontPayment` stops Succeeded/Failed/Cancelled/Expired and manual AwaitingAdmin |
| First-seller shipping | StoreShipping target remains |
| cartId-only ownership DTO | Guest secret + cart still required (runtime P) |
| Duplicate timeout constants | Settings resolver + clamp only |

No proven-dead production path removed in R12 (no speculative refactor).
