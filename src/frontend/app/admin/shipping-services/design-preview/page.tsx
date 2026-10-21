import { Suspense } from "react";
import { ShippingDesignPreviewScreen } from "./design-preview-screen.tsx";

/**
 * TB-SHIPPING-ADMIN-UI-PREVIEW-186 — مسیر اختصاصی پیش‌نمایش طراحی «سرویس ارسال».
 *
 * این مسیر:
 * - در همان پوستهٔ احراز‌شدهٔ Admin قرار می‌گیرد (`app/admin/layout.tsx` + `AdminShell`) و هیچ
 *   gate/auth جدیدی دور نمی‌زند؛
 * - هیچ خواندن Host/سرور، هیچ credential، هیچ فراخوانی شبکه، و هیچ داده‌ای از فروشگاه واقعی ندارد؛ کل
 *   محتوا fixture صریح و درون‌حافظه است؛
 * - رفتار `/admin/shipping-services` را تغییر نمی‌دهد و تا تأیید ظاهری کاربر جایگزین مسیر زنده
 *   نمی‌شود.
 */
export default function AdminShippingDesignPreviewPage() {
  return (
    <Suspense fallback={null}>
      <div className="p-4 lg:p-6">
        <ShippingDesignPreviewScreen />
      </div>
    </Suspense>
  );
}
