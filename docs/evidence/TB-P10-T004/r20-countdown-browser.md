# R20 countdown browser

- Initial display from server `holdEndsAt` / `serverTime` (`remainingSecondsFromServer`).
- Local `setInterval` 1s tick; no fetch inside the timer.
- Cycle #1 screenshot showed `00:17` then `00:16` without layout jump.
- Payment fail (sandbox `outcome=failed` at 04:00:16Z) while holdEndsAt=04:00:23Z did not reset ExpiresAt.
- At zero: one refresh (`shouldRefreshOnceAtZero`); expired copy + retry CTA.
- Cycle #2 showed `00:49` with «مهلت رزرو مجدد».
- Network: one `pending-payments` resource on cart load (`pendingFetches=1`). No per-second polling.
- Manual review hold displayed as `1439:18` (24h review minutes via `MM:SS`) — readable, not a new timer source.

Store Settings 1/1/2 minutes used for future cycles only (no timer hack).
