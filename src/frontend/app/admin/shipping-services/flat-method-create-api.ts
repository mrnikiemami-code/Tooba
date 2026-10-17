/**
 * کلاینت ادمین برای ایجاد واقعی یک روش canonical یک‌سطحی.
 *
 * TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167
 *
 * - مسیر: `POST /v1/admin/shipping-services/shipping-methods/one-level`.
 * - بدنه فقط کد canonical، metadata اختیاری ارائه‌دهنده و نام زبان‌های واقعی است؛ هیچ
 *   `serviceCode`/`optionCode`/فعال‌بودن ارسال نمی‌شود.
 * - 401/403 ⇒ `denied`؛ خطای سرور ⇒ `error` با کد واقعی؛ پاسخ نامعتبر ⇒ `error`.
 * - هیچ «موفقیت جعلی» ساخته نمی‌شود: نتیجهٔ موفق فقط از هویت پایدار پاسخ سرور می‌آید.
 */

import { adminHeaders, type AdminResult } from "../admin-api.ts";
import {
  createMethodErrorMessage,
  mapCreatedMethod,
  type CreatedMethod,
  type CreateMethodRequest,
} from "./flat-method-create-model.ts";

/** مسیر مجاز ایجاد روش canonical (متد POST). */
export const ONE_LEVEL_CATALOG_ADMIN_CREATE_PATH =
  "/v1/admin/shipping-services/shipping-methods/one-level";

function errorCode(payload: unknown): string {
  if (!payload || typeof payload !== "object") return "";
  const row = payload as Record<string, unknown>;
  const raw = row.errorCode ?? row.ErrorCode ?? row.title ?? row.Title;
  return typeof raw === "string" ? raw.trim() : "";
}

export async function createOneLevelMethod(
  request: CreateMethodRequest,
): Promise<AdminResult<CreatedMethod>> {
  try {
    const response = await fetch(ONE_LEVEL_CATALOG_ADMIN_CREATE_PATH, {
      method: "POST",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      cache: "no-store",
      body: JSON.stringify({
        methodCode: request.methodCode,
        providerKind: request.providerKind ?? null,
        translations: request.translations.map((t) => ({
          languageId: t.languageId,
          name: t.name,
        })),
      }),
    });

    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return {
        state: "denied",
        data: null,
        status: response.status,
        message: "admin.authorization.denied",
      };
    }
    if (!response.ok) {
      const code = errorCode(payload);
      return {
        state: "error",
        data: null,
        status: response.status,
        message: code || "shipping_method.create_failed",
      };
    }

    const mapped = mapCreatedMethod(payload);
    if (!mapped) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: "shipping_method.create_invalid_response",
      };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** متن قابل‌نمایش برای شکست ساخت روش؛ برای موفقیت استفاده نمی‌شود. */
export function createFailureMessage(result: AdminResult<CreatedMethod>): string {
  return createMethodErrorMessage(result.message ?? "", result.status);
}
