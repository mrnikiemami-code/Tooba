# R17 cycle history

`ToAudit` maps `ReservationCycleProjection.History` (stored snapshots) plus `ListEventsAsync`.

Each row: cycle number, start, end/expiry, status, reason, stored EffectiveHoldMinutes / EffectiveMaxCycles / PolicySource. Current Settings are not read.

Events: Reacquire requested/failed, retry limit, start/expire/commit/cancel/policy/manual review.

ReacquireFailed overlays Expired when that is the latest event. All shortage lines from current supply status (not first-line-only).
