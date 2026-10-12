import { acceptLanguageForLocale } from "../i18n/locale.ts";
import { readBrowserLocaleCookie } from "../i18n/locale-cookie.ts";

/**
 * Shared admin HTTP result/auth header primitives (technical lib — not capability UI).
 */
export const ADMIN_DEV_ACTOR_HEADER = "X-Tooba-Dev-Actor-User-Id";
export const ADMIN_ACTOR_STORAGE_KEY = "tooba.adminActorUserId";
export const DEFAULT_ADMIN_ACTOR_ID = "";

/**
 * هدرهای پایهٔ درخواست Admin؛ مرز مشترک همهٔ کلاینت‌های Admin.
 *
 * زبان نمایش با Accept-Language اعلام می‌شود: سرور (RequestLocaleResolver) زبان
 * عنوان/جزئیات خطا را فقط از همین هدر می‌گیرد و بدون آن DefaultCulture = "en" است،
 * یعنی متن انگلیسی در UI فارسی نشت می‌کند.
 */
export function adminRequestHeaders(extra?: Record<string, string>): Record<string, string> {
  return {
    Accept: "application/json",
    "Accept-Language": acceptLanguageForLocale(readBrowserLocaleCookie()),
    ...(extra ?? {}),
  };
}

export type AdminLoadState = "ok" | "denied" | "error";

export interface AdminResult<T> {
  state: AdminLoadState;
  data: T | null;
  status: number;
  message?: string;
}
