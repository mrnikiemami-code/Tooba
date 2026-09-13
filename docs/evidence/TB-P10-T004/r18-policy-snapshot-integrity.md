# R18 policy snapshot integrity

Active cycle `ExpiresAt`, `EffectiveHoldMinutes`, `EffectiveMaxCycles`, and `PolicySource` stay on the stored cycle after Settings edit.

Historical cycle rows keep the snapshot used when that cycle started.

The next StartAsync uses the newly resolved policy (tested: 3-minute offer snapshot stays; next retry uses store 8-minute retry).
