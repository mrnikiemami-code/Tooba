import { z } from "zod";

/**
 * بررسی‌های ایستا/زمان اجرا برای مالکیت Design System.
 * قیمت از رشتهٔ ازپیش‌قالب‌بندی‌شده می‌آید؛ اگر مصرف‌کننده محاسبه کند خارج از قرارداد است.
 */
export const moneyViewSchema = z.object({
  amount: z.string().regex(/^-?\d+(\.\d+)?$/),
  currency: z.string().length(3),
});

/**
 * Drawer باید با inset منطقی (start) باز شود نه left/right ثابت.
 */
export function drawerUsesLogicalStart(source: string): boolean {
  return source.includes("start-0") && !source.includes("left-0") && !source.includes("right-0");
}

/**
 * IconButton بدون برچسب قابل‌قبول نیست.
 */
export function iconButtonRequiresLabel(label: string): boolean {
  return label.trim().length > 0;
}
