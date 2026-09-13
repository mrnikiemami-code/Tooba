# R15 reservation policy

Keys:

| Key | Default | Range | Layer |
| --- | --- | --- | --- |
| `ReservationCycle:InitialReservationHoldMinutes` | 120 | 1…43200 | Platform |
| `ReservationCycle:RetryReservationHoldMinutes` | 120 | 1…43200 | Platform |
| `ReservationCycle:MaxReservationCycles` | 3 | 1…20 | Platform |
| Store columns on `store_hold_policy_settings` | null = inherit | same | Store |
| `catalog.reservation_cycle_policy_overrides` scope `offer` / `category` | null fields inherit | same | Offer / Category |

120 minutes matches existing 2h online/manual initial holds so R15 does not silently shrink production TTL. Tests/runtime use Offer overrides (10/5/3 and 3/2/2) for mixed-line proof.

No cooldown key. Payment unpaid hours remain a separate Payment timeout.
