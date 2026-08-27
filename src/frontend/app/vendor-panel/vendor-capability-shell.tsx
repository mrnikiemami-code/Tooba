import type { ReactNode } from "react";
import Link from "next/link";
import { ShieldCheck } from "lucide-react";

/** پوستهٔ صادقانه برای قابلیت فروشنده بدون backend معتبر. */
export function VendorCapabilityShell({
  title,
  description,
  icon,
}: {
  title: string;
  description: string;
  icon: ReactNode;
}) {
  return (
    <section className="bg-white rounded-2xl border border-gray-200 shadow-sm min-h-[420px]" data-testid="vendor-capability-unavailable">
      <div className="px-5 py-4 border-b border-gray-100 flex items-center gap-3">
        <span className="w-10 h-10 bg-[#2563EB]/10 text-[#2563EB] rounded-xl flex items-center justify-center">{icon}</span>
        <div>
          <h1 className="font-black text-lg">{title}</h1>
          <p className="text-xs text-gray-500 mt-1">{description}</p>
        </div>
      </div>
      <div className="min-h-80 flex flex-col items-center justify-center text-center p-8">
        <ShieldCheck className="w-10 h-10 text-gray-300 mb-4" />
        <h2 className="font-black text-gray-900">این بخش فعلاً در دسترس نیست</h2>
        <p className="max-w-lg text-sm text-gray-500 leading-7 mt-2">
          ساختار پنل Shopeiva حفظ شده است، اما تا اتصال capability معتبر Host هیچ درآمد، تسویه، آمار یا دادهٔ جعلی نمایش
          داده نمی‌شود.
        </p>
        <Link
          href="/vendor-panel"
          className="mt-5 inline-flex items-center justify-center rounded-xl bg-[#2563EB] text-white text-sm font-bold px-4 py-2.5 hover:bg-[#1D4ED8] transition-colors"
        >
          بازگشت به داشبورد
        </Link>
      </div>
    </section>
  );
}
