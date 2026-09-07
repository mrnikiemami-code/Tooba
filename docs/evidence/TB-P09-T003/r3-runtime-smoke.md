# R3 runtime smoke

- Host `:5088` health OK
- payment-methods: gateway+manual when enabled
- fresh checkout + providerCode=manual → Pending
- ops: confirm_deposit, reject_deposit, cancel
- confirm → mark_processing available
- Production default ManualCardToCardEnabled=false (disabled unless configured)
- USER_VISUAL_ACCEPTED=NO
