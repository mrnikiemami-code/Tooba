# Selection-UX — TB-P09-T012

Checkbox only when `capability.selectable`. Qty input only when selectable and max>1 for pack/unpack/ship.

Selected rows use `bg-blue-50/70`. Selection count bar: `{n} قلم انتخاب شده`.

`isIncompatibleSelection` is false when `selectedCount <= 1` (T011 preserved). Mixed hint only for 2+ lines with empty bulk intersection:

`برای عملیات گروهی، اقلام هم‌مرحله را انتخاب کنید.`

Runtime E on single: packed row `unpack` ∩ processing row `pack_selected` = empty shared codes.
