# R15 policy snapshot

Every `Start` copies `EffectiveHoldMinutes`, `EffectiveMaxCycles`, `PolicySource`, and Reason onto the cycle row.

Later Settings edits do not mutate historical cycles. Projection reads the snapshot from the stored row, not live Settings.
