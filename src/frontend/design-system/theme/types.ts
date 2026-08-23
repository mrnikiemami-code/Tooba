/**
 * حالت رنگ پوسته. کد اجرایی از پایگاه‌داده پذیرفته نمی‌شود.
 */
export type ColorScheme = "light" | "dark";

/**
 * جهت نوشتار محصول. RTL پیش‌فرض بازار اول است ولی LTR باید درجهٔ یک باشد.
 */
export type TextDirection = "rtl" | "ltr";

/**
 * قرارداد تم قابل‌تایپ برای اتصال آینده به تنظیمات مستأجر.
 * فقط دادهٔ اعلانی است؛ اسکریپت ذخیره‌شده اجرا نمی‌شود.
 */
export interface ThemeContract {
  colorScheme: ColorScheme;
  direction: TextDirection;
  brandAssetKey?: string;
}
