# Admin Form Mode — VIEW / EDIT

الگوی سبک برای فرم‌های Admin که باید **صریحاً** بین مشاهده و ویرایش جابه‌جا شوند.

## قرارداد

| مفهوم | معنا |
|--------|------|
| `ViewMode` | نمایش خوانا (نه inputهای خاکستری disabled) |
| `EditMode` | ورود فقط با اقدام «ویرایش» وقتی `canEdit` |
| `canView` / `canEdit` | از Host/AdminPanelAccess؛ FE مرجع امنیت نیست |
| `isDirty` | تغییرات ذخیره‌نشده |
| `onEdit` / `onSave` / `onCancel` | انتقال حالت |

## استفاده

```ts
import { useAdminFormMode } from "../../design-system";

const form = useAdminFormMode({ canView: true, canEdit });
// form.mode === "view" | "edit"
// form.onEdit() → EDIT
// پس از ذخیره: form.onSaved() → VIEW
// انصراف: form.onCancel() → VIEW و discard
```

ماشین حالت خالص (`createAdminFormModeState`, `reduceAdminFormMode`, …) بدون React هم قابل‌تست است.

## قوانین

1. باز کردن جزئیات → پیش‌فرض **VIEW**
2. دکمهٔ ویرایش فقط اگر `canEdit`
3. Save اصلی، Cancel فرعی
4. ناوبری با dirty → تأیید (قرارداد Tooba / `confirm`)
5. **بازسازی گستردهٔ همهٔ Admin لازم نیست** — صفحات جدید این الگو را بگیرند

مرجع پیاده‌سازی: Category Admin General (`category-admin-screen.tsx`).
