"use client";

import { Suspense } from "react";
import { ShippingServicesScreen } from "../shipping-services-screen.tsx";
import { ShippingOneLevelTabs } from "./shipping-one-level-tabs.tsx";
import { resolveOneLevelCatalogGate } from "./shipping-one-level-gate.ts";

/**
 * صفحهٔ سرویس ارسال.
 *
 * TB-SHIPPING-ONE-LEVEL-TABS-084
 *
 * رفتار دروازه (fail-closed): دروازهٔ کاتالوگ یک‌سطحی در سمت سرور تصمیم می‌گیرد و تنها راه
 * روشن‌شدن آن یک پرچم سمت سرور است. فرانت این اپ در حال حاضر به آن پرچم دسترسی ندارد، پس
 * `resolveOneLevelCatalogGate({})` همیشه `disabled` برمی‌گرداند و مسیر زیر دقیقاً همان صفحهٔ
 * دوسطحی فعلی را با تمام عملیات و دیالوگ‌های سالم رندر می‌کند.
 *
 * عمداً هیچ سوئیچی از query string، localStorage یا وجود دادهٔ روش خوانده نمی‌شود؛ بنابراین
 * هیچ پارامتری نمی‌تواند دروازهٔ خاموش را دور بزند. تا وقتی بایند فقط-خواندنی سرور اضافه نشود،
 * شل جدید dormant می‌ماند و «رول‌اوت زنده» ادعا نمی‌شود.
 */
export default function AdminShippingServicesPage() {
  const gate = resolveOneLevelCatalogGate({});

  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">در حال بارگذاری…</p>}>
      <div className="p-4 lg:p-6">
        {gate.state === "enabled" ? (
          <ShippingOneLevelTabs gate={gate} />
        ) : (
          <ShippingServicesScreen />
        )}
      </div>
    </Suspense>
  );
}
