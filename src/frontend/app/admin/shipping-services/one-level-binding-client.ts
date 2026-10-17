/**
 * تنها کلاینت خواندن پیوند authoritative یک روش یک‌سطحی.
 *
 * چرا این ماژول جدا شد: پیش‌تر چهار زبانه (تعرفه/پوشش، برنامه کاری، بسته‌بندی، استعلام) و مدل
 * فعال‌سازی هرکدام نسخهٔ خودشان از `fetch` + نگاشت را داشتند. هر نسخه خطای شبکه را به یک حالت
 * می‌رساند و نتیجه این بود که «سرور در دسترس نیست»، «دسترسی رد شد» و «پاسخ نامعتبر» همه با یک
 * پیام («پاسخ سرور معتبر نبود») نمایش داده می‌شدند. حالا:
 *
 *   - transport فقط یک‌جا زندگی می‌کند (یک تست، یک رفتار)؛
 *   - هر علت شکست کد حالت مستقل خودش را دارد (`denied` / `unreachable` / `untrusted`)؛
 *   - افزودن حالت تازه یعنی یک عضو در union و یک case در پیام‌ها، نه پنج ویرایش پراکنده.
 *
 * هویت هرگز از متن `methodCode` ساخته نمی‌شود؛ ورودی همان کد canonical است و پاسخ سرور تنها
 * منبع `serviceCode`/`optionCode` می‌ماند.
 */
import { adminHeaders } from "../admin-api.ts";
import {
  mapHostBinding,
  oneLevelBindingPath,
  type OneLevelBindingState,
} from "./shipping-one-level-rates-model.ts";
import { sanitizeMethodCode } from "./shipping-one-level-tabs-model.ts";

/** خواندن پیوند یک روش؛ هر شکست به حالت صریح و غیرحدسی نگاشت می‌شود. */
export async function loadOneLevelBinding(methodCode: string): Promise<OneLevelBindingState> {
  const code = sanitizeMethodCode(methodCode);
  if (!code) return { state: "unresolved" };

  let response: Response;
  try {
    response = await fetch(oneLevelBindingPath(code), {
      headers: adminHeaders(),
      cache: "no-store",
    });
  } catch {
    // قطع ارتباط/شبکه: این «پاسخ نامعتبر» نیست و با تلاش دوباره ممکن است حل شود.
    return { state: "unreachable" };
  }

  if (response.status === 401 || response.status === 403) return { state: "denied" };

  const payload = await response.json().catch(() => null);
  if (!response.ok) {
    // پاسخ HTTP خطا ولی سرور در دسترس است: دسترسی/وضعیت سرور، نه دادهٔ معتبر.
    return response.status >= 500 ? { state: "unreachable" } : { state: "untrusted" };
  }
  return mapHostBinding(payload);
}
