import { Suspense } from "react";
import { cookies } from "next/headers";
import { notFound } from "next/navigation";
import { ShippingAuthoringScreen } from "../../../../../features/admin-shipping-authoring";
import { parseLocale } from "../../../../../lib/i18n/locale.ts";
import { readLocaleFromCookieString } from "../../../../../lib/i18n/locale-cookie.ts";

/**
 * TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202 — مسیر نازک ویرایشگر یک‌جای یک پیش‌نویس موجود.
 *
 * این Server Component فقط:
 * - هویت مسیر (`draftId`) را اعتبارسنجی می‌کند؛ شناسهٔ نامعتبر همان «پیدا نشد» است و هیچ
 *   درخواستی ساخته نمی‌شود؛
 * - حالت VIEW/EDIT را فقط به‌عنوان حالت نمایش رابط می‌خواند (تصمیم مالکیت/دسترسی هرگز از آن گرفته
 *   نمی‌شود؛ هر درخواست سرور خودش مجوز را تعیین می‌کند)؛
 * - locale رابط را از همان کوکی canonical می‌خواند و به صفحهٔ عمومی feature می‌دهد.
 *
 * هیچ منطق تجاری/انتقال داده، توکن نشست یا اعتبارنامه‌ای در HTML این مسیر قرار نمی‌گیرد.
 */

export const dynamic = "force-dynamic";
export const revalidate = 0;

const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const ZERO_GUID = "00000000-0000-0000-0000-000000000000";

export default async function AdminShippingAuthoringDraftPage({
  params,
  searchParams,
}: {
  params: Promise<{ draftId: string }>;
  searchParams: Promise<{ mode?: string | string[] }>;
}) {
  const { draftId } = await params;
  if (!GUID.test(draftId) || draftId.toLowerCase() === ZERO_GUID) notFound();

  const { mode } = await searchParams;
  const initialMode = mode === "edit" ? "edit" : "view";
  const locale = parseLocale(readLocaleFromCookieString((await cookies()).toString()));

  return (
    <Suspense fallback={<p className="p-6 text-sm text-gray-500">…</p>}>
      <div className="p-4 lg:p-6">
        {/* key: a different draft identity is a different editing session (no state reuse across A/B). */}
        <ShippingAuthoringScreen key={draftId} draftId={draftId} locale={locale} initialMode={initialMode} />
      </div>
    </Suspense>
  );
}
