/**
 * خوانندهٔ کلاینتیِ کاتالوگ canonical یک‌سطحی برای صفحهٔ «سرویس ارسال».
 *
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146
 *
 * تنها مسیر خوانده‌شده همان مسیر فقط-خواندنیِ مجوزدار ادمین است
 * (`GET /v1/admin/shipping-services/shipping-methods/one-level`). هیچ مسیر قدیمی دو‌سطحی و هیچ
 * پارامتر query‌ای که دروازه را دور بزند وجود ندارد.
 *
 * دروازه با همان تابع fail-closed شناخته‌شدهٔ `resolveOneLevelCatalogGateFromHost` ارزیابی می‌شود؛
 * پس خطای مجوز/شبکه/پاسخ نامعتبر و همچنین «دروازهٔ روشن ولی کاتالوگ مهاجرت‌نشده» هیچ‌وقت به‌عنوان
 * «فهرست خالیِ موفق» جا زده نمی‌شوند.
 *
 * هر دلیلِ شکست یک کد پایدار و مستقل دارد (`shipping_catalog.gate_disabled`، `not_ready`،
 * `invalid_response`، `load_failed`، `host-unreachable`) تا لایهٔ نمایش بتواند «شکست پیکربندی»
 * را از «شکست گذرا» جدا کند؛ پیش‌تر همهٔ این‌ها یک رشته بودند و UI همه را «تلاش دوباره» می‌دید.
 */
import { adminHeaders, type AdminResult } from "../admin-api.ts";
import {
  ONE_LEVEL_CATALOG_ADMIN_READ_PATH,
  mapHostOneLevelMethods,
  resolveOneLevelActiveLanguageId,
  resolveOneLevelCatalogGateFromHost,
  type OneLevelCatalogGate,
} from "./shipping-one-level-gate.ts";
import type { ShippingCatalogFailureCode } from "./shipping-catalog-gate-presentation.ts";
import type { OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

/** کاتالوگ خوانده‌شده: ردیف‌های روش + زبان فعال مؤثر که سرور اعلام کرده است. */
export type OneLevelCatalogRead = {
  methods: readonly OneLevelMethodRow[];
  activeLanguageId: string;
};

/** کد شکست پایدار برای هر دلیلِ خاموش‌بودن دروازه؛ هر دلیل، نمایش و رفتار خودش را دارد. */
export function catalogFailureCodeForGate(gate: OneLevelCatalogGate): ShippingCatalogFailureCode {
  if (gate.state === "enabled") return "shipping_catalog.invalid_response";
  switch (gate.reason) {
    case "catalog-not-ready":
      return "shipping_catalog.not_ready";
    case "untrusted-source":
      return "shipping_catalog.invalid_response";
    default:
      return "shipping_catalog.gate_disabled";
  }
}

export async function loadOneLevelCatalog(): Promise<AdminResult<OneLevelCatalogRead>> {
  try {
    const response = await fetch(ONE_LEVEL_CATALOG_ADMIN_READ_PATH, {
      headers: adminHeaders(),
      cache: "no-store",
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "shipping_catalog.load_failed" };
    }
    const gate = resolveOneLevelCatalogGateFromHost({ status: response.status, payload });
    if (gate.state !== "enabled") {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: catalogFailureCodeForGate(gate),
      };
    }
    return {
      state: "ok",
      data: {
        methods: mapHostOneLevelMethods(payload),
        activeLanguageId: resolveOneLevelActiveLanguageId(payload),
      },
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
