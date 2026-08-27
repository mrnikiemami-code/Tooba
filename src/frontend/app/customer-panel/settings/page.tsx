"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Bell, Globe2, LockKeyhole, Settings, ShieldCheck, User } from "lucide-react";
import { type Locale } from "../../../lib/i18n/locale.ts";
import { readBrowserLocaleCookie, writeBrowserLocaleCookie } from "../../../lib/i18n/locale-cookie.ts";

/**
 * تنظیمات مشتری — هویت حساب از پروفایل زنده؛ locale با کوکی (بدون URL prefix)؛
 * امنیت/اعلان بدون backend معتبر.
 */
export default function CustomerSettingsPage() {
  const [locale, setLocale] = useState<Locale>("fa");
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    setLocale(readBrowserLocaleCookie());
  }, []);

  function selectLocale(next: Locale) {
    const normalized = writeBrowserLocaleCookie(next);
    setLocale(normalized);
    setSaved(true);
    window.setTimeout(() => setSaved(false), 2000);
  }

  return (
    <main className="space-y-6" data-testid="customer-settings-page">
      <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 md:p-6">
        <div className="flex items-center gap-3">
          <span className="w-10 h-10 bg-[#2563EB]/10 text-[#2563EB] rounded-xl flex items-center justify-center">
            <Settings className="w-5 h-5" />
          </span>
          <div>
            <h1 className="font-black text-lg">تنظیمات</h1>
            <p className="text-xs text-gray-500 mt-1">هویت حساب زنده است؛ امنیت و اعلان‌ها هنوز متصل نیستند</p>
          </div>
        </div>
      </div>

      <section className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5" data-testid="customer-settings-profile-link">
        <div className="flex items-start gap-3">
          <span className="w-10 h-10 bg-emerald-500/10 text-emerald-600 rounded-xl flex items-center justify-center shrink-0">
            <User className="w-5 h-5" />
          </span>
          <div className="min-w-0 flex-1">
            <h2 className="font-black text-base text-gray-900">هویت و پروفایل حساب</h2>
            <p className="mt-1 text-sm text-gray-500 leading-7">
              نام نمایشی و فیلدهای پروفایل از <code className="text-xs bg-gray-50 px-1 rounded">/v1/customer/profile</code>{" "}
              خوانده و ذخیره می‌شوند.
            </p>
            <Link
              href="/customer-panel/profile"
              className="mt-3 inline-flex items-center gap-2 rounded-xl bg-[#2563EB] text-white text-sm font-bold px-4 py-2.5 hover:bg-[#1D4ED8] transition-colors"
            >
              رفتن به پروفایل
            </Link>
          </div>
        </div>
      </section>

      <section
        className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5"
        data-testid="customer-settings-locale"
      >
        <div className="flex items-start gap-3">
          <span className="w-10 h-10 bg-[#2563EB]/10 text-[#2563EB] rounded-xl flex items-center justify-center shrink-0">
            <Globe2 className="w-5 h-5" />
          </span>
          <div className="min-w-0 flex-1">
            <h2 className="font-black text-base text-gray-900">ترجیح زبان</h2>
            <p className="mt-1 text-sm text-gray-500 leading-7">
              زبان در کوکی ذخیره می‌شود و در بازدید بعدی از مسیرهای{" "}
              <code className="text-xs bg-gray-50 px-1 rounded">/fa</code> یا{" "}
              <code className="text-xs bg-gray-50 px-1 rounded">/en</code> ویترین اعمال می‌شود. پنل بدون prefix مسیر است.
            </p>
            <div className="mt-4 flex flex-wrap items-center gap-2">
              <button
                type="button"
                onClick={() => selectLocale("fa")}
                className={`rounded-xl px-4 py-2.5 text-sm font-bold transition-colors ${
                  locale === "fa"
                    ? "bg-[#2563EB] text-white"
                    : "bg-gray-50 text-gray-700 border border-gray-200 hover:border-[#2563EB]/40"
                }`}
                data-testid="customer-settings-locale-fa"
                aria-pressed={locale === "fa"}
              >
                FA · فارسی
              </button>
              <button
                type="button"
                onClick={() => selectLocale("en")}
                className={`rounded-xl px-4 py-2.5 text-sm font-bold transition-colors ${
                  locale === "en"
                    ? "bg-[#2563EB] text-white"
                    : "bg-gray-50 text-gray-700 border border-gray-200 hover:border-[#2563EB]/40"
                }`}
                data-testid="customer-settings-locale-en"
                aria-pressed={locale === "en"}
              >
                EN · English
              </button>
              {saved ? (
                <span className="text-xs font-bold text-emerald-600" data-testid="customer-settings-locale-saved">
                  ذخیره شد
                </span>
              ) : null}
            </div>
          </div>
        </div>
      </section>

      <section
        className="bg-white rounded-2xl border border-dashed border-gray-200 shadow-sm p-5"
        data-testid="customer-settings-security-unavailable"
      >
        <div className="flex items-start gap-3">
          <span className="w-10 h-10 bg-gray-100 text-gray-400 rounded-xl flex items-center justify-center shrink-0">
            <LockKeyhole className="w-5 h-5" />
          </span>
          <div>
            <h2 className="font-black text-base text-gray-900">امنیت حساب</h2>
            <p className="mt-1 text-sm text-gray-500 leading-7">
              تغییر رمز، نشست‌ها و احراز دومرحله‌ای تا capability معتبر Host در دسترس نیست. ذخیرهٔ جعلی وجود ندارد.
            </p>
            <p className="mt-3 inline-flex items-center gap-1.5 text-[11px] font-bold text-gray-400">
              <ShieldCheck className="w-3.5 h-3.5" />
              فعلاً در دسترس نیست
            </p>
          </div>
        </div>
      </section>

      <section
        className="bg-white rounded-2xl border border-dashed border-gray-200 shadow-sm p-5"
        data-testid="customer-settings-notifications-unavailable"
      >
        <div className="flex items-start gap-3">
          <span className="w-10 h-10 bg-gray-100 text-gray-400 rounded-xl flex items-center justify-center shrink-0">
            <Bell className="w-5 h-5" />
          </span>
          <div>
            <h2 className="font-black text-base text-gray-900">ترجیحات اعلان</h2>
            <p className="mt-1 text-sm text-gray-500 leading-7">
              ترجیحات اعلان و کانال‌های اطلاع‌رسانی بدون backend معتبر ذخیره نمی‌شوند.
            </p>
            <p className="mt-3 inline-flex items-center gap-1.5 text-[11px] font-bold text-gray-400">
              <ShieldCheck className="w-3.5 h-3.5" />
              فعلاً در دسترس نیست
            </p>
          </div>
        </div>
      </section>
    </main>
  );
}
