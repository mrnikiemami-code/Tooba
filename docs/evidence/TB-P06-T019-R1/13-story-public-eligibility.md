# 13 — Story public eligibility (TB-P06-T019-R1)

## Rules

`IsPublicationEligible()`:

- Admin origin **or**
- Seller origin with `ReviewStatus.Approved`

`IsPubliclyVisible(now)`:

- Publication eligible **and**
- `Status == Active` **and**
- StartAt/EndAt window OK

## Query hardening (`GetPublicStoriesAsync`)

SQL prefilter: `(Origin == Admin || ReviewStatus == Approved)` **and** `Status == Active`, then in-memory `IsPubliclyVisible`.

## Not public

| State | Public? |
|---|---|
| Seller Draft (`None`) | No |
| Submitted | No |
| Rejected | No |
| Approved but not Active | No |
| Approved + Active (in window) | Yes |
| Admin Active (seed) | Yes |

Seed seller titles (`پیش‌نویس فروشنده`, `در انتظار بازبینی`) excluded from public in tests.
