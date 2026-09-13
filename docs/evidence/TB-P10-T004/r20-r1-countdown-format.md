# R20-R1 countdown format

Shared formatter: `src/frontend/lib/reservation-countdown.ts`

- `< 60m`: `MM:SS` (`09:30`, `59:59`)
- `>= 60m`: `HH:MM:SS` (`01:00:00`, `23:59:00`, `24:00:00`)
- never giant-minute `1439:18` / `1440:00`

Reused by:

- Cart pending (`formatCountdown`)
- Admin reservation card (`formatReservationCountdown` re-export)

Accessibility (minute resolution, `aria-live="off"`):

- FA: `۲۳ ساعت و ۴۷ دقیقه تا پایان مهلت رزرو`
- EN: `23 hours and 46 minutes remaining`

Browser:

- FA cart manual review `01a098ef-2d53-7000-aec6-5d95e6c9e4fe`: visible `23:47:20`, no `1439:xx`
- Admin FA same order: `23:46:49`
- EN cart: `23:46:36` + English aria-label

Screenshot: `docs/evidence/TB-P10-T004/r20-r1-cart-fa-manual-countdown.png`

Server `ExpiresAt` / `holdEndsAt` unchanged. Local 1s tick only. One refresh at zero unchanged.
