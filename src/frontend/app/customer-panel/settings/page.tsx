"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Bell, Globe2, LockKeyhole, Settings, ShieldCheck, User } from "lucide-react";
import { type Locale } from "../../../lib/i18n/locale.ts";
import { readBrowserLocaleCookie, writeBrowserLocaleCookie } from "../../../lib/i18n/locale-cookie.ts";
import {
  customerPreferencesErrorMessage,
  loadCustomerPreferences,
  saveCustomerPreferences,
} from "../customer-preferences-api.ts";

type SettingsTab = "language" | "security" | "notifications";

/**
 * تنظیمات مشتری — پروفایل زنده؛ locale با Host + کوکی؛ امنیت/اعلان بدون toggle جعلی.
 */
export default function CustomerSettingsPage() {
  const [activeTab, setActiveTab] = useState<SettingsTab>("language");
  const [locale, setLocale] = useState<Locale>("fa");
  const [busy, setBusy] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loadingPrefs, setLoadingPrefs] = useState(true);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      const fromApi = await loadCustomerPreferences();
      if (cancelled) return;
      if (fromApi) {
        setLocale(fromApi.locale);
        writeBrowserLocaleCookie(fromApi.locale);
      } else {
        setLocale(readBrowserLocaleCookie());
      }
      setLoadingPrefs(false);
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  async function selectLocale(next: Locale) {
    setBusy(true);
    setError(null);
    setSaved(false);
    try {
      const savedPrefs = await saveCustomerPreferences(next);
      setLocale(savedPrefs.locale);
      setSaved(true);
      window.setTimeout(() => setSaved(false), 2000);
    } catch (cause) {
      writeBrowserLocaleCookie(next);
      setLocale(next);
      setError(customerPreferencesErrorMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  const tabs: { id: SettingsTab; label: string; icon: typeof Globe2 }[] = [
    { id: "language", label: "زبان", icon: Globe2 },
    { id: "security", label: "امنیت", icon: LockKeyhole },
    { id: "notifications", label: "اطلاعیه‌ها", icon: Bell },
  ];

  return (
    <main className="space-y-6" data-testid="customer-settings-page">
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
              data-testid="customer-settings-profile-cta"
            >
              رفتن به پروفایل
            </Link>
          </div>
        </div>
      </section>

      <div className="max-w-3xl mx-auto">
        <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden shadow-sm">
          <div className="p-4 md:p-6 border-b border-gray-200">
            <div className="flex items-center gap-2">
              <Settings className="w-5 h-5 text-[#2563EB]" />
              <h1 className="text-lg font-bold text-gray-900">تنظیمات حساب</h1>
            </div>
            <p className="text-sm text-gray-500 mt-1">ترجیح زبان زنده است؛ امنیت و اعلان بدون backend معتبر نیستند</p>
          </div>

          <div className="flex overflow-x-auto border-b border-gray-200 scrollbar-hide">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              return (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-2 px-4 py-3 text-sm font-medium transition-all whitespace-nowrap border-b-2 ${
                    activeTab === tab.id
                      ? "border-[#2563EB] text-[#2563EB]"
                      : "border-transparent text-gray-500 hover:text-gray-700"
                  }`}
                  data-testid={`customer-settings-tab-${tab.id}`}
                >
                  <Icon className="w-4 h-4" />
                  {tab.label}
                </button>
              );
            })}
          </div>

          <div className="p-4 md:p-6">
            {activeTab === "language" ? (
              <div className="space-y-4" data-testid="customer-settings-locale">
                <p className="text-sm text-gray-500 leading-7">
                  زبان در Host و کوکی ذخیره می‌شود و در بازدید بعدی ویترین اعمال می‌شود. پنل بدون prefix مسیر است.
                </p>
                {loadingPrefs ? (
                  <p className="text-sm text-gray-400">در حال دریافت ترجیح زبان...</p>
                ) : (
                  <div className="grid grid-cols-2 gap-3">
                    {(
                      [
                        { id: "fa" as const, label: "فارسی" },
                        { id: "en" as const, label: "English" },
                      ] as const
                    ).map((item) => (
                      <button
                        key={item.id}
                        type="button"
                        disabled={busy}
                        onClick={() => void selectLocale(item.id)}
                        className={`p-4 rounded-xl border-2 transition-all ${
                          locale === item.id
                            ? "border-[#2563EB] bg-[#2563EB]/5"
                            : "border-gray-200 hover:border-gray-300"
                        } ${busy ? "opacity-70 cursor-not-allowed" : ""}`}
                        data-testid={`customer-settings-locale-${item.id}`}
                        aria-pressed={locale === item.id}
                      >
                        <p className="text-sm font-medium text-gray-700">{item.label}</p>
                      </button>
                    ))}
                  </div>
                )}
                {saved ? (
                  <span className="text-xs font-bold text-emerald-600" data-testid="customer-settings-locale-saved">
                    ذخیره شد
                  </span>
                ) : null}
                {error ? (
                  <p className="text-xs font-bold text-amber-600" data-testid="customer-settings-locale-error">
                    {error} — کوکی محلی به‌روز شد.
                  </p>
                ) : null}
              </div>
            ) : null}

            {activeTab === "security" ? (
              <div
                className="rounded-xl border border-dashed border-gray-200 p-4"
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
              </div>
            ) : null}

            {activeTab === "notifications" ? (
              <div
                className="rounded-xl border border-dashed border-gray-200 p-4"
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
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </main>
  );
}
