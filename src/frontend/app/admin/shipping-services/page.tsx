import { Suspense } from "react";
import { cookies } from "next/headers";
import { ShippingServicesScreen } from "../shipping-services-screen.tsx";
import { ShippingServicesFlatScreen } from "./flat-screen.tsx";
import {
  ONE_LEVEL_CATALOG_ADMIN_READ_PATH,
  mapHostOneLevelMethods,
  resolveOneLevelActiveLanguageId,
  resolveOneLevelCatalogGateFromHost,
  type OneLevelCatalogGate,
} from "./shipping-one-level-gate.ts";
import { buildUpstreamAuthHeaders, hostBaseUrl } from "../../../lib/server/host-client.ts";
import { acceptLanguageForLocale, parseLocale } from "../../../lib/i18n/locale.ts";
import { readLocaleFromCookieString } from "../../../lib/i18n/locale-cookie.ts";

/**
 * صفحهٔ سرویس ارسال ادمین.
 *
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146
 *
 * مدل قفل‌شده: **یک روش قابل‌انتخاب مشتری = یک ردیف مستقل مدیریتی**. این صفحه فهرست تختِ روش‌ها را
 * نشان می‌دهد و برای هر ردیف فقط «مشاهده / ویرایش / حذف» دارد؛ تعرفه، برنامه کاری و بسته‌بندی داخل
 * چهار زبانهٔ غیرتودرتوی همان روش مدیریت می‌شوند.
 *
 * تنها مرجع ردیف‌ها کاتالوگ canonical یک‌سطحی سرور است
 * (`GET /v1/admin/shipping-services/shipping-methods/one-level`):
 *
 *   - خواندن فقط در سمت سرور انجام می‌شود؛ اعتبارنامه هرگز به HTML/مرورگر نمی‌رسد و هیچ bearer‌ای
 *     log نمی‌شود؛
 *   - هیچ query string، localStorage یا وجود دادهٔ روش نمی‌تواند دروازه را روشن کند؛
 *   - خطای شبکه/مجوز/پاسخ نامعتبر ⇒ fail-closed و همان صفحهٔ دوسطحی فعلی؛
 *   - `gate=Enabled` ولی `ready=false` ⇒ دروازه خاموش می‌ماند؛ کاتالوگ مهاجرت‌نشده هرگز «فعال» یا
 *     «خالیِ موفق» جا زده نمی‌شود.
 *
 * هیچ نوشتنی، هیچ قیمت‌گذاری و هیچ تغییری در `Tooba:ShippingRateAuthority` یا
 * `Tooba:ShippingOperationalShadow` در این مسیر وجود ندارد.
 */

export const dynamic = "force-dynamic";
export const revalidate = 0;

interface HostRead {
  status: number;
  payload: unknown;
}

async function readOneLevelCatalogFromHost(): Promise<HostRead> {
  try {
    const cookieHeader = (await cookies()).toString();
    const locale = parseLocale(readLocaleFromCookieString(cookieHeader));
    const headers = await buildUpstreamAuthHeaders();
    const response = await fetch(`${hostBaseUrl()}${ONE_LEVEL_CATALOG_ADMIN_READ_PATH}`, {
      headers: { ...headers, "Accept-Language": acceptLanguageForLocale(locale) },
      cache: "no-store",
    });
    const payload = await response.json().catch(() => null);
    return { status: response.status, payload };
  } catch {
    // در دسترس نبودن Host ⇒ fail-closed؛ هرگز به‌صورت خوش‌بینانه روشن نمی‌شود.
    return { status: 0, payload: null };
  }
}

export default async function AdminShippingServicesPage() {
  const hostRead = await readOneLevelCatalogFromHost();
  const gate: OneLevelCatalogGate = resolveOneLevelCatalogGateFromHost(hostRead);
  const initialMethods = gate.state === "enabled" ? mapHostOneLevelMethods(hostRead.payload) : [];
  const activeLanguageId = gate.state === "enabled" ? resolveOneLevelActiveLanguageId(hostRead.payload) : "";

  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        {gate.state === "enabled" ? (
          <ShippingServicesFlatScreen initialMethods={initialMethods} activeLanguageId={activeLanguageId} />
        ) : (
          <ShippingServicesScreen />
        )}
      </div>
    </Suspense>
  );
}
