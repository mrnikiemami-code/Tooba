# TB-P10-T004-R7 — Runtime
Host :5088. Raw: r7-runtime-raw.json ok=true.

| Scenario | Result |
| --- | --- |
| A active review confirm | PASS confirm 200; paid ExpiresAt=null |
| B released + stock → reacquire | PASS confirm 200; old Released; new reservation; durable |
| C released + no stock | PASS confirm 400 `inventory.supply.unavailable`; payment pending |
| D CheckOnly | PASS 200; reservation count unchanged |
| E paid recover durable | PASS outcome Recovered |
| F atomic multi-line | PASS EnsureOrderSupply rollback ReleaseAsync |
| G decimal | PASS PaidOrderReservationLifecycle qty 1.25 |
| H settings precedence | PASS ManualPaymentReview/Initial/Online/Cart keys |
