import { Settings } from "lucide-react";
import { ShieldCheck } from "lucide-react";

/** تنظیمات Admin فعلاً بدون capability معتبر Host. */
export default function AdminSettingsPage() {
  return (
    <section className="bg-white rounded-2xl border border-gray-200 shadow-sm min-h-[420px]" data-testid="admin-capability-unavailable">
      <div className="px-5 py-4 border-b border-gray-100 flex items-center gap-3">
        <span className="w-10 h-10 bg-[#2563EB]/10 text-[#2563EB] rounded-xl flex items-center justify-center">
          <Settings className="w-5 h-5" />
        </span>
        <div>
          <h1 className="font-black text-lg">تنظیمات</h1>
          <p className="text-xs text-gray-500 mt-1">پیکربندی سامانهٔ مدیریت</p>
        </div>
      </div>
      <div className="min-h-80 flex flex-col items-center justify-center text-center p-8">
        <ShieldCheck className="w-10 h-10 text-gray-300 mb-4" />
        <h2 className="font-black text-gray-900">این بخش فعلاً در دسترس نیست</h2>
        <p className="max-w-lg text-sm text-gray-500 leading-7 mt-2">
          ساختار ناوبری حفظ شده است، اما تا capability معتبر Host هیچ تنظیمات یا دادهٔ جعلی نمایش داده نمی‌شود.
        </p>
      </div>
    </section>
  );
}
