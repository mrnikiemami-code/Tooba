import type { ReactNode } from "react";
import { ShieldCheck } from "lucide-react";

/**
 * پوستهٔ بدون persistence برای قابلیت‌هایی که backend معتبر ندارند.
 * ساختار Shopeiva حفظ می‌شود اما دکمهٔ ذخیرهٔ نمایشی ساخته نمی‌شود.
 */
export function CustomerCapabilityShell({
  title,
  description,
  icon,
}: {
  title: string;
  description: string;
  icon: ReactNode;
}) {
  return (
    <section className="bg-white rounded-2xl border border-gray-100 shadow-sm min-h-[420px]">
      <div className="px-5 py-4 border-b border-gray-100 flex items-center gap-3">
        <span className="w-10 h-10 bg-blue-50 text-[#2563EB] rounded-xl flex items-center justify-center">{icon}</span>
        <div>
          <h1 className="font-black text-lg">{title}</h1>
          <p className="text-xs text-gray-500 mt-1">{description}</p>
        </div>
      </div>
      <div className="min-h-80 flex flex-col items-center justify-center text-center p-8">
        <ShieldCheck className="w-12 h-12 text-gray-300 mb-4" />
        <h2 className="font-black">این بخش هنوز به backend متصل نیست</h2>
        <p className="max-w-lg text-sm text-gray-500 leading-7 mt-2">
          پوستهٔ خریداری‌شده حفظ شده است، اما تا ارائهٔ capability معتبر هیچ داده یا ذخیره‌سازی ساختگی نمایش داده نمی‌شود.
        </p>
      </div>
    </section>
  );
}
