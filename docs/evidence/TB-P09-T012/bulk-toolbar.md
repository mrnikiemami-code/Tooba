# Bulk-Toolbar — TB-P09-T012

Contextual from capability intersection (`intersectLifecycleCodes`):

- no selection + `mark_processing` → شروع پردازش
- selection sharing `mark_processing` → شروع پردازش انتخاب‌شده‌ها
- no selection + `mark_packed` → بسته‌بندی همه اقلام آماده
- selection sharing `pack_selected` → بسته‌بندی انتخاب‌شده‌ها
- selection sharing `unpack` → بازگشت از بسته‌بندی انتخاب‌شده‌ها
- mixed (2+ , empty intersection) → no invalid bulk + mixed hint

Single eligible line never mixed (T011).
