"use client";

import { useEffect, useState } from "react";
import { Globe2, Save, Settings, User } from "lucide-react";
import { ErrorState, faWorkspaceMessages } from "../../../design-system";
import { type Locale } from "../../../lib/i18n/locale.ts";
import { readBrowserLocaleCookie, writeBrowserLocaleCookie } from "../../../lib/i18n/locale-cookie.ts";
import { prepareAdminDevActor } from "../admin-api";
import {
  loadOperatorPreferences,
  loadOperatorProfile,
  saveOperatorPreferences,
  saveOperatorProfile,
  type OperatorProfile,
} from "../operator-settings-api";

type AdminSettingsTab = "profile" | "locale";

/**
 * تنظیمات اپراتور Admin — پروفایل شخصی + locale؛ بدون سوئیچ سراسری جعلی.
 */
export default function AdminSettingsPage() {
  const [activeTab, setActiveTab] = useState<AdminSettingsTab>("profile");
  const [profile, setProfile] = useState<OperatorProfile | null | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [success, setSuccess] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [displayName, setDisplayName] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [bio, setBio] = useState("");
  const [locale, setLocale] = useState<Locale>("fa");

  async function refresh() {
    setDenied(false);
    setLoadError(null);
    setProfile(undefined);
    await prepareAdminDevActor();
    const [profileResult, prefsResult] = await Promise.all([loadOperatorProfile(), loadOperatorPreferences()]);
    if (profileResult.state === "denied") {
      setDenied(true);
      setProfile(null);
      return;
    }
    if (profileResult.state !== "ok" || !profileResult.data) {
      setLoadError(profileResult.message ?? "admin.operator.profile-unavailable");
      setProfile(null);
      return;
    }
    setProfile(profileResult.data);
    setDisplayName(profileResult.data.displayName);
    setFirstName(profileResult.data.firstName);
    setLastName(profileResult.data.lastName);
    setBio(profileResult.data.bio);
    if (prefsResult.state === "ok" && prefsResult.data) {
      setLocale(prefsResult.data.locale);
      writeBrowserLocaleCookie(prefsResult.data.locale);
    } else {
      setLocale(readBrowserLocaleCookie());
    }
  }

  useEffect(() => {
    void refresh();
  }, []);

  async function onSaveProfile() {
    if (!profile?.editable) return;
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await saveOperatorProfile({
      displayName: displayName.trim(),
      firstName: firstName.trim() || undefined,
      lastName: lastName.trim() || undefined,
      bio: bio.trim(),
    });
    if (result.state === "denied") {
      setDenied(true);
      setBusy(false);
      return;
    }
    if (result.state !== "ok" || !result.data) {
      setError("ذخیرهٔ پروفایل اپراتور انجام نشد.");
      setBusy(false);
      return;
    }
    setProfile(result.data);
    setDisplayName(result.data.displayName);
    setFirstName(result.data.firstName);
    setLastName(result.data.lastName);
    setBio(result.data.bio);
    setSuccess("پروفایل اپراتور ذخیره شد.");
    setBusy(false);
  }

  async function onSelectLocale(next: Locale) {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await saveOperatorPreferences(next);
    if (result.state === "denied") {
      setDenied(true);
      setBusy(false);
      return;
    }
    if (result.state !== "ok" || !result.data) {
      writeBrowserLocaleCookie(next);
      setLocale(next);
      setError("ذخیرهٔ زبان در Host انجام نشد؛ کوکی محلی به‌روز شد.");
      setBusy(false);
      return;
    }
    setLocale(result.data.locale);
    setSuccess("ترجیح زبان ذخیره شد.");
    setBusy(false);
  }

  if (denied) {
    return (
      <section data-testid="admin-settings-page">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این Actor مجوز مشاهده یا ویرایش پروفایل اپراتور را ندارد."
          onRetry={() => void refresh()}
          retryLabel={faWorkspaceMessages.retry}
        />
      </section>
    );
  }

  if (profile === undefined) {
    return (
      <section className="bg-white rounded-2xl border border-gray-200 shadow-sm p-8 text-center text-gray-500" data-testid="admin-settings-loading">
        در حال دریافت تنظیمات اپراتور...
      </section>
    );
  }

  if (!profile) {
    return (
      <section data-testid="admin-settings-page">
        <ErrorState
          title="پروفایل اپراتور در دسترس نیست"
          detail={loadError ?? undefined}
          onRetry={() => void refresh()}
          retryLabel={faWorkspaceMessages.retry}
        />
      </section>
    );
  }

  const readOnly = !profile.editable;

  return (
    <section className="max-w-3xl mx-auto" data-testid="admin-settings-page">
      <div className="bg-white rounded-2xl border border-gray-200 overflow-hidden shadow-sm">
        <div className="p-4 md:p-6 border-b border-gray-200 bg-gradient-to-r from-[#2563EB]/5 to-transparent">
          <div className="flex items-center gap-2">
            <Settings className="w-5 h-5 text-[#2563EB]" />
            <h1 className="text-lg font-bold text-gray-900">تنظیمات اپراتور</h1>
          </div>
          <p className="text-sm text-gray-500 mt-1">پروفایل شخصی و ترجیح زبان — بدون سوئیچ سراسری سامانه</p>
        </div>

        <div className="flex overflow-x-auto border-b border-gray-200 scrollbar-hide">
          {(
            [
              { id: "profile" as const, label: "پروفایل", icon: User },
              { id: "locale" as const, label: "زبان", icon: Globe2 },
            ] as const
          ).map((tab) => {
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
                data-testid={`admin-settings-tab-${tab.id}`}
              >
                <Icon className="w-4 h-4" />
                {tab.label}
              </button>
            );
          })}
        </div>

        <div className="p-4 md:p-6">
          {activeTab === "profile" ? (
            <form
              className="space-y-4"
              onSubmit={(event) => {
                event.preventDefault();
                void onSaveProfile();
              }}
              data-testid="admin-settings-profile-form"
            >
              {readOnly ? (
                <p className="rounded-xl bg-amber-50 border border-amber-100 text-amber-800 text-sm px-4 py-3">
                  پروفایل فقط‌خواندنی است.
                </p>
              ) : null}

              <div>
                <label className="text-sm font-medium text-gray-700">نام نمایشی</label>
                <input
                  type="text"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  disabled={readOnly || busy}
                  className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] disabled:opacity-60"
                  data-testid="admin-settings-display-name"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium text-gray-700">نام</label>
                  <input
                    type="text"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    disabled={readOnly || busy}
                    className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] disabled:opacity-60"
                    data-testid="admin-settings-first-name"
                  />
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-700">نام خانوادگی</label>
                  <input
                    type="text"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    disabled={readOnly || busy}
                    className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] disabled:opacity-60"
                    data-testid="admin-settings-last-name"
                  />
                </div>
              </div>

              <div>
                <label className="text-sm font-medium text-gray-700">بیوگرافی</label>
                <textarea
                  rows={3}
                  value={bio}
                  onChange={(e) => setBio(e.target.value)}
                  disabled={readOnly || busy}
                  maxLength={200}
                  className="w-full mt-1 px-4 py-2.5 bg-gray-50 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-[#2563EB] resize-none disabled:opacity-60"
                  data-testid="admin-settings-bio"
                />
                <p className="text-[11px] text-gray-400 mt-1">{bio.length}/200</p>
              </div>

              {!readOnly ? (
                <button
                  type="submit"
                  disabled={busy || !displayName.trim()}
                  className="w-full py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-[#1D4ED8] transition-colors shadow-lg shadow-[#2563EB]/30 flex items-center justify-center gap-2 disabled:opacity-70"
                  data-testid="admin-settings-save-profile"
                >
                  {busy ? (
                    <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  ) : (
                    <>
                      <Save className="w-4 h-4" />
                      ذخیره پروفایل
                    </>
                  )}
                </button>
              ) : null}
            </form>
          ) : (
            <div className="space-y-4" data-testid="admin-settings-locale">
              <p className="text-sm text-gray-500 leading-7">ترجیح زبان اپراتور در Host و کوکی ذخیره می‌شود.</p>
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
                    onClick={() => void onSelectLocale(item.id)}
                    className={`p-4 rounded-xl border-2 transition-all ${
                      locale === item.id
                        ? "border-[#2563EB] bg-[#2563EB]/5"
                        : "border-gray-200 hover:border-gray-300"
                    } ${busy ? "opacity-70 cursor-not-allowed" : ""}`}
                    data-testid={`admin-settings-locale-${item.id}`}
                    aria-pressed={locale === item.id}
                  >
                    <p className="text-sm font-medium text-gray-700">{item.label}</p>
                  </button>
                ))}
              </div>
            </div>
          )}

          {success ? (
            <p className="mt-4 text-xs font-bold text-emerald-600" data-testid="admin-settings-success">
              {success}
            </p>
          ) : null}
          {error ? (
            <p className="mt-4 text-xs font-bold text-red-600" data-testid="admin-settings-error">
              {error}
            </p>
          ) : null}
        </div>
      </div>
    </section>
  );
}
