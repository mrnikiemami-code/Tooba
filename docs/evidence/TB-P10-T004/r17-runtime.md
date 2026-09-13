# R17 runtime A–J

Proven against `AdminReservationCycleMapper.ToAudit` / `ToSummary` (same path as Admin GET + grid page map) and R15 `ReservationCycleProjection`. Server clock in tests: `2026-09-13T08:00:00Z`.

| Case | Result |
| --- | --- |
| A. Active Cycle #1 | PASS — رزرو فعال / فعال #1 / 600s from server ExpiresAt |
| B. Expired Cycle #1 | PASS — مهلت رزرو پایان یافته / پایان‌یافته #1 |
| C. Cycle #2 after retry | PASS — current #2; history keeps cycle 1 stored 30min/offer snapshot |
| D. Failed reacquire + shortage | PASS — رزرو مجدد ناموفق + copy + both line shortages |
| E. Max cycles | PASS — retry-limit copy; used/max visible |
| F. Manual review | PASS — دلیل «در انتظار بررسی پرداخت» |
| G. Paid/Committed | PASS — رزرو پس از پرداخت نهایی شد / نهایی‌شده |
| H. Cancelled/released | PASS — رزرو با لغو سفارش آزاد شد |
| I. Orders grid no N+1 | PASS — GetProjectionsAsync only; no GetProjectionAsync in grid engines |
| J. FA/EN layout | PASS — dir rtl/ltr; EN labels on mapper + Inventory reservation heading |

No TB-P10-T005. USER_VISUAL_ACCEPTED=NO.
