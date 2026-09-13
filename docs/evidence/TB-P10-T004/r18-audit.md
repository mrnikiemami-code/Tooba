# R18 audit

`catalog.reservation_policy_audit_events` records level, field, old override, new override, actor, timestamp.

One row per changed field. Secrets are not logged. Reservation-cycle events are not rewritten.

Admin GET `/v1/admin/settings/reservation-policy/audit`.
