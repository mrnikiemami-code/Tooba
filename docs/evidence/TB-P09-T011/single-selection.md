# Single-selection compatibility

`isIncompatibleSelection(selectedCount, shared, perLine)` returns false when `selectedCount <= 1`.

A lone eligible line never shows «ردیف‌های انتخاب‌شده در وضعیت‌های متفاوت یا ناسازگار هستند.» even if only seller-level `pack_selected` exists (no `orderLineId`).
