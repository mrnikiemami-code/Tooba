# TB-P07-T001-R3 — Admin menu localization

## Changes
- Added `src/frontend/app/admin/admin-chrome-messages.ts` with FA/EN nav + filter operator labels.
- Refactored `admin-shell.tsx` to use label keys (no raw English: fulfillment/refund/payout/Schema).
- Localized DataGrid `FilterControl` operators to Persian defaults (شامل / برابر با / …).

## Persian nav samples
| Before | After |
| --- | --- |
| ارسال / fulfillment | ارسال و تحویل |
| مرجوعی / refund | مرجوعی و بازپرداخت |
| صف payout | صف پرداخت به فروشنده |
| Schema رده | طرح ویژگی رده |

## English
English locale maps remain available via `adminNavLabels("en")` / `filterOperatorLabels("en")` when `document.documentElement.lang` starts with `en`.
