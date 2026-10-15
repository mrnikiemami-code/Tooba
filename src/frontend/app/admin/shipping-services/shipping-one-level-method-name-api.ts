/**
 * کلاینت ادمین برای تنها نوشتن مجاز نام روش یک‌سطحی.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-094
 *
 * قواعد:
 * - مسیر، کد روش canonical و زبان را حمل می‌کند؛ بدنه فقط `name` است.
 * - هیچ `serviceCode`/`optionCode`/`providerKind` در بدنه نیست و هیچ شناسهٔ گزینه‌ای ارسال نمی‌شود.
 * - 401/403 ⇒ `denied`؛ خطای سرور ⇒ `error` با کد خطای واقعی؛ پاسخ نامعتبر ⇒ `error`.
 * - هیچ «موفقیت جعلی» ساخته نمی‌شود: فقط پاسخ تأییدشدهٔ سرور برگردانده می‌شود.
 */

import { adminHeaders, type AdminResult } from "../admin-api.ts";
import { oneLevelMethodTranslationPath } from "./shipping-one-level-method-name-model.ts";

/** نتیجهٔ تأییدشدهٔ سرور برای نام یک روش در یک زبان. */
export type OneLevelMethodNameSaved = {
  methodCode: string;
  languageId: string;
  name: string;
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

function pick(row: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (row[key] !== undefined) return row[key];
  }
  return undefined;
}

/** پاسخ سرور را محافظه‌کارانه نگاشت می‌کند؛ نبود هویت پایدار ⇒ `null` (پاسخ نامعتبر). */
function mapSaved(raw: unknown): OneLevelMethodNameSaved | null {
  const row = asRecord(raw);
  if (!row) return null;
  const methodCode = text(pick(row, "methodCode", "MethodCode"));
  const languageId = text(pick(row, "languageId", "LanguageId"));
  const name = text(pick(row, "name", "Name"));
  if (!methodCode || !languageId || !name) return null;
  return { methodCode, languageId, name };
}

function errorCode(payload: unknown, fallback: string): string {
  const row = asRecord(payload);
  return text(row?.errorCode ?? row?.ErrorCode ?? row?.title ?? row?.Title) || fallback;
}

/**
 * نام نمایشی یک روش یک‌سطحی را در یک زبان ذخیره می‌کند.
 *
 * `PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/translations/{languageId}`
 */
export async function saveOneLevelMethodName(
  methodCode: string,
  languageId: string,
  name: string,
): Promise<AdminResult<OneLevelMethodNameSaved>> {
  const url = oneLevelMethodTranslationPath(methodCode, languageId);
  try {
    const response = await fetch(url, {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ name }),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: errorCode(payload, "shipping_method_name.save_failed"),
      };
    }
    const mapped = mapSaved(payload);
    if (!mapped) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: "shipping_method_name.invalid_response",
      };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
