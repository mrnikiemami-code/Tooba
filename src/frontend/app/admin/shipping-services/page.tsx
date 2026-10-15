import { Suspense } from "react";
import { cookies } from "next/headers";
import { ShippingServicesScreen } from "../shipping-services-screen.tsx";
import { ShippingOneLevelTabs } from "./shipping-one-level-tabs.tsx";
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
 * صفحهٔ سرویس ارسال.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-085
 *
 * این صفحه یک Server Component است و تنها مرجع روشن‌شدن دروازهٔ کاتالوگ یک‌سطحی، پاسخ
 * مجوزدار سرور (`GET /v1/admin/shipping-services/shipping-methods/one-level`) است:
 *
 *   - خواندن فقط در سمت سرور انجام می‌شود؛ اعتبارنامه هرگز به HTML/مرورگر نمی‌رسد و
 *     هیچ bearer‌ای log نمی‌شود؛
 *   - هیچ query string، localStorage یا وجود دادهٔ روش نمی‌تواند دروازه را روشن کند؛
 *   - خطای شبکه/مجوز/پاسخ نامعتبر ⇒ fail-closed و همان صفحهٔ دوسطحی فعلی با همهٔ عملیات سالم؛
 *   - `gate=Enabled` ولی `ready=false` ⇒ دروازه خاموش می‌ماند؛ کاتالوگ مهاجرت‌نشده هرگز
 *     «فعال» یا «خالیِ موفق» جا زده نمی‌شود.
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
          <ShippingOneLevelTabs gate={gate} activeLanguageId={activeLanguageId} initialMethods={initialMethods} />
        ) : (
          <ShippingServicesScreen />
        )}
      </div>
    </Suspense>
  );
}
