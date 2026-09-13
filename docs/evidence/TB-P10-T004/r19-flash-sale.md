# R19 flash-sale mixed-policy

Required scenario in `Flash_sale_mixed_policy_uses_three_then_one_and_blocks_cycle_three`:

- Offer A override: Initial 3 / Retry 1 / Max 2
- Offer B inherit Store: 10 / 5 / 3
- Order resolve: 3 / 1 / 2

Runtime:

- Cycle #1 EffectiveHoldMinutes=3; ExpiresAt = start+3m
- Payment correlate does not change ExpiresAt or CycleNumber
- After expiry, Cycle #2 EffectiveHoldMinutes=1
- After Cycle #2 close, created=2 >= max=2; RetryLimitReached; no Cycle #3

Customer/admin retry-limit copy remains R16/R17 localized strings.
