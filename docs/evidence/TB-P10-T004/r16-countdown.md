# R16 countdown

- Display `MM:SS` from `holdEndsAt` + `serverTime` + client receive skew (`remainingSecondsFromServer`)
- Local `setInterval` 1s is presentation only
- Copy: «موجودی این سفارش تا پایان این زمان برای شما نگه داشته می‌شود.» / retry «مهلت رزرو مجدد»
- Failed payment keeps the same `HoldEndsAt`
- At `00:00`, `shouldRefreshOnceAtZero` triggers **one** `onRefresh` (one POST pending-payments)
- No per-second API, no 1.5s poll, no FE release/extend
