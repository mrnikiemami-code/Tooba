import { Suspense } from "react";
import { cookies } from "next/headers";
import { ShippingServicesAuthoringListClient } from "../shipping-services-authoring-list-client.tsx";
import { parseLocale } from "../../../../lib/i18n/locale.ts";
import { readLocaleFromCookieString } from "../../../../lib/i18n/locale-cookie.ts";

/**
 * صفحهٔ ادمین روش‌های ارسال برای مالک **مارکت‌پلیس**.
 *
 * TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223: این مسیر فقط یک **مرز رابط کاربری** برای سطح مالک
 * مارکت‌پلیس است و همان فهرست/فرم موجود را با `surface="marketplace"` رندر می‌کند. هیچ مالک/فروشنده/
 * Tenant از query یا بدنه خوانده نمی‌شود؛ مالک مجاز همان است که سرور پس از `fulfillment.view` resolve
 * می‌کند. در نصب SingleStore این صفحه باید وضعیت صریح «در دسترس نیست» را نشان دهد و هیچ روشِ فروشگاه را
 * زیر عنوان مارکت‌پلیس جا نزند.
 */
export const dynamic = "force-dynamic";
export const revalidate = 0;

export default async function AdminShippingServicesMarketplacePage() {
  const locale = parseLocale(readLocaleFromCookieString((await cookies()).toString()));

  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        <ShippingServicesAuthoringListClient locale={locale} surface="marketplace" />
      </div>
    </Suspense>
  );
}
