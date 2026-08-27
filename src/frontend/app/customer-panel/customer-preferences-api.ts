/**
 * ترجیح locale مشتری — Host `/v1/customer/preferences` از طریق BFF.
 * کوکی مرورگر هم‌زمان نوشته می‌شود؛ بدون persistence جعلی فقط‌محلی.
 */

import { ensureCsrfCookie } from "../../lib/auth/browser-session.ts";
import { parseLocale, type Locale } from "../../lib/i18n/locale.ts";
import { writeBrowserLocaleCookie } from "../../lib/i18n/locale-cookie.ts";
import { customerAuthHeaders } from "./customer-api.ts";

export interface CustomerPreferences {
  locale: Locale;
}

/** خطای قابل تشخیص API ترجیحات. */
export class CustomerPreferencesApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

/** نگاشت پاسخ Host به ترجیحات مشتری. */
export function mapCustomerPreferences(payload: unknown): CustomerPreferences | null {
  const item = recordOf(payload);
  if (!item) return null;
  const raw = item.locale ?? item.Locale;
  if (raw == null || String(raw).trim() === "") return null;
  return { locale: parseLocale(String(raw)) };
}

/** پیام خطای کاربرپسند برای UI تنظیمات. */
export function customerPreferencesErrorMessage(error: unknown): string {
  if (error instanceof CustomerPreferencesApiError) {
    if (error.status === 401) return "برای ذخیرهٔ زبان نشست معتبر لازم است.";
    if (error.status === 400) return "مقدار زبان معتبر نیست.";
    return error.message || "ذخیرهٔ ترجیح زبان انجام نشد.";
  }
  return "ارتباط با سرور برقرار نشد.";
}

/** ترجیح locale را از Host می‌خواند؛ نبود API → null. */
export async function loadCustomerPreferences(): Promise<CustomerPreferences | null> {
  try {
    const response = await fetch("/api/customer/preferences", {
      credentials: "include",
      headers: customerAuthHeaders(),
    });
    if (!response.ok) return null;
    return mapCustomerPreferences(await response.json().catch(() => null));
  } catch {
    return null;
  }
}

/**
 * locale را در Host ذخیره می‌کند و کوکی مرورگر را هم‌تراز می‌کند.
 */
export async function saveCustomerPreferences(locale: Locale): Promise<CustomerPreferences> {
  await ensureCsrfCookie();
  const response = await fetch("/api/customer/preferences", {
    method: "PUT",
    credentials: "include",
    headers: customerAuthHeaders(true),
    body: JSON.stringify({ locale }),
  });
  const payload = await response.json().catch(() => null);
  if (!response.ok) {
    const title =
      payload && typeof payload === "object" && "title" in payload
        ? String((payload as Record<string, unknown>).title)
        : "ذخیرهٔ ترجیح زبان انجام نشد.";
    throw new CustomerPreferencesApiError(response.status, title);
  }
  const mapped = mapCustomerPreferences(payload) ?? { locale: parseLocale(locale) };
  writeBrowserLocaleCookie(mapped.locale);
  return mapped;
}
