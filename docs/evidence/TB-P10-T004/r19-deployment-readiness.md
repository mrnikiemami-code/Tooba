# R19 deployment readiness (R15–R18 schema/config)

| Artifact | Role |
| --- | --- |
| `20260913033000_AddReservationCycles` | cycles + unique (checkout, cycle_number) + events |
| `20260913033100_AddReservationCyclePolicy` | nullable Store/Category/Offer overrides |
| `20260913080000_AddReservationPolicyAudit` | settings audit; not cycle rewrite |
| `ReservationCycle` in appsettings | platform default 120/120/3 |

- Nullable override = inherit; all-null Category/Offer row deleted
- Orders without cycle history: projections empty/safe; Admin compact "—"
- No startup mass mutation
- No dev-only resolver
- Worker CloseExpiredDue is hosted, not client
