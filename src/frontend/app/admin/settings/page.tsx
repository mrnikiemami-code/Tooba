"use client";

import { useEffect, useState } from "react";
import { Clock, Globe2, Hash, Palette, Save, Settings, ShieldAlert, ShieldCheck, User } from "lucide-react";
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
import {
  loadStoreQuantitySettings,
  saveStoreQuantitySettings,
  type QuantityRoundingMode,
} from "../quantity-settings-api";
import {
  loadHoldPolicySettings,
  saveHoldPolicySettings,
  type HoldPolicySettingsView,
  type PaymentMethodHoldView,
} from "../hold-policy-settings-api";
import {
  ReservationPolicyEditor,
  draftFromView,
  readReservationWrite,
  type ReservationPolicyDraft,
} from "../reservation-policy-editor.tsx";
import {
  loadCheckoutIdentitySettings,
  saveCheckoutIdentitySettings,
  type CheckoutIdentitySettingsView,
} from "../checkout-identity-settings-api.ts";
import {
  loadCheckoutAbuseSettings,
  saveCheckoutAbuseSettings,
  type CheckoutAbuseSettingsView,
} from "../checkout-abuse-settings-api.ts";
import {
  loadAppearanceSettings,
  saveAppearanceSettings,
  type AppearanceSettingsView,
} from "../appearance-settings-api.ts";
import { AdminAppearanceSettingsForm } from "../admin-appearance-settings.tsx";

type AdminSettingsTab = "profile" | "locale" | "quantity" | "holds" | "identity" | "limits" | "appearance";

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
  const [roundingMode, setRoundingMode] = useState<QuantityRoundingMode>("Nearest");
  const [holds, setHolds] = useState<HoldPolicySettingsView | null>(null);
  const [holdDraft, setHoldDraft] = useState({
    cartPersistenceHours: "",
    onlinePaymentHoldHours: "",
    manualPaymentInitialHoldHours: "",
    manualPaymentReviewHoldHours: "",
  });
  const [methodDraft, setMethodDraft] = useState<PaymentMethodHoldView[]>([]);
  const [identity, setIdentity] = useState<CheckoutIdentitySettingsView | null>(null);
  const [identityDraft, setIdentityDraft] = useState<"AuthenticatedOnly" | "GuestAllowed">("AuthenticatedOnly");
  const [limits, setLimits] = useState<CheckoutAbuseSettingsView | null>(null);
  const [limitsDraft, setLimitsDraft] = useState({
    maxOpenUnpaidOrdersPerCustomer: "2",
    reservationCommitWindowMinutes: "30",
    maxCheckoutCommitsPerCustomerInWindow: "3",
  });
  const [appearance, setAppearance] = useState<AppearanceSettingsView | null>(null);
  const [appearanceDraft, setAppearanceDraft] = useState("tooba-blue");
  const [appearanceThemeDraft, setAppearanceThemeDraft] = useState("LightOnly");
  const [appearanceSkinDraft, setAppearanceSkinDraft] = useState("classic");
  const [reservationDraft, setReservationDraft] = useState<ReservationPolicyDraft>({
    initial: "",
    retry: "",
    max: "",
    inheritInitial: true,
    inheritRetry: true,
    inheritMax: true,
  });

  async function refresh() {
    setDenied(false);
    setLoadError(null);
    setProfile(undefined);
    await prepareAdminDevActor();
    const [profileResult, prefsResult, roundingResult, holdResult, identityResult, limitsResult, appearanceResult] = await Promise.all([
      loadOperatorProfile(),
      loadOperatorPreferences(),
      loadStoreQuantitySettings(),
      loadHoldPolicySettings(),
      loadCheckoutIdentitySettings(),
      loadCheckoutAbuseSettings(),
      loadAppearanceSettings(),
    ]);
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
    if (roundingResult.ok) {
      setRoundingMode(roundingResult.data.globalRoundingMode);
    }
    if (holdResult.ok) {
      applyHoldView(holdResult.data);
    }
    if (identityResult.ok) {
      setIdentity(identityResult.data);
      setIdentityDraft(identityResult.data.policy);
    }
    if (limitsResult.ok) {
      setLimits(limitsResult.data);
      setLimitsDraft({
        maxOpenUnpaidOrdersPerCustomer: String(limitsResult.data.maxOpenUnpaidOrdersPerCustomer),
        reservationCommitWindowMinutes: String(limitsResult.data.reservationCommitWindowMinutes),
        maxCheckoutCommitsPerCustomerInWindow: String(limitsResult.data.maxCheckoutCommitsPerCustomerInWindow),
      });
    }
    if (appearanceResult.ok) {
      setAppearance(appearanceResult.data);
      setAppearanceDraft(appearanceResult.data.paletteKey);
      setAppearanceThemeDraft(appearanceResult.data.themeMode);
      setAppearanceSkinDraft(appearanceResult.data.productCardSkin);
    }
  }

  function applyHoldView(view: HoldPolicySettingsView) {
    setHolds(view);
    setHoldDraft({
      cartPersistenceHours: view.cartPersistence.hours == null ? "" : String(view.cartPersistence.hours),
      onlinePaymentHoldHours: view.onlinePaymentHold.hours == null ? "" : String(view.onlinePaymentHold.hours),
      manualPaymentInitialHoldHours: view.manualInitialHold.hours == null ? "" : String(view.manualInitialHold.hours),
      manualPaymentReviewHoldHours: view.manualReviewHold.hours == null ? "" : String(view.manualReviewHold.hours),
    });
    setMethodDraft(view.methods);
    if (view.reservationCycle) {
      setReservationDraft(draftFromView(view.reservationCycle));
    }
  }

  function parseHours(raw: string): number | null {
    const trimmed = raw.trim();
    if (!trimmed) return null;
    const parsed = Number(trimmed);
    return Number.isFinite(parsed) ? parsed : null;
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

  async function onSaveRounding() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await saveStoreQuantitySettings(roundingMode);
    if (result.denied) {
      setDenied(true);
      setBusy(false);
      return;
    }
    if (!result.ok) {
      setError("ذخیرهٔ گرد کردن مقدار انجام نشد.");
      setBusy(false);
      return;
    }
    setRoundingMode(result.data.globalRoundingMode);
    setSuccess("گرد کردن سراسری مقدار ذخیره شد.");
    setBusy(false);
  }

  async function onSaveHolds() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const reservation = readReservationWrite(reservationDraft);
    if (!reservation.ok) {
      setError(reservation.message);
      setBusy(false);
      return;
    }
    const result = await saveHoldPolicySettings({
      cartPersistenceHours: parseHours(holdDraft.cartPersistenceHours),
      onlinePaymentHoldHours: parseHours(holdDraft.onlinePaymentHoldHours),
      manualPaymentInitialHoldHours: parseHours(holdDraft.manualPaymentInitialHoldHours),
      manualPaymentReviewHoldHours: parseHours(holdDraft.manualPaymentReviewHoldHours),
      methods: methodDraft,
      ...reservation.body,
    });
    if (result.denied) {
      setDenied(true);
      setBusy(false);
      return;
    }
    if (!result.ok) {
      setError(result.message ?? "ذخیرهٔ مهلت پرداخت انجام نشد.");
      setBusy(false);
      return;
    }
    applyHoldView(result.data);
    setSuccess("مهلت پرداخت و نگهداری سبد ذخیره شد.");
    setBusy(false);
  }

  async function onSaveIdentity() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await saveCheckoutIdentitySettings(identityDraft);
    if (result.denied) {
      setDenied(true);
      setBusy(false);
      return;
    }
    if (!result.ok) {
      setError(result.message ?? "ذخیرهٔ هویت خرید انجام نشد.");
      setBusy(false);
      return;
    }
    setIdentity(result.data);
    setIdentityDraft(result.data.policy);
    setSuccess("هویت مشتری در فرایند خرید ذخیره شد.");
    setBusy(false);
  }

  async function onSaveLimits() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const maxOpen = Number(limitsDraft.maxOpenUnpaidOrdersPerCustomer);
    const windowMinutes = Number(limitsDraft.reservationCommitWindowMinutes);
    const maxCommits = Number(limitsDraft.maxCheckoutCommitsPerCustomerInWindow);
    if (!Number.isInteger(maxOpen) || !Number.isInteger(windowMinutes) || !Number.isInteger(maxCommits)) {
      setError("مقدار واردشده معتبر نیست.");
      setBusy(false);
      return;
    }
    const result = await saveCheckoutAbuseSettings({
      maxOpenUnpaidOrdersPerCustomer: maxOpen,
      reservationCommitWindowMinutes: windowMinutes,
      maxCheckoutCommitsPerCustomerInWindow: maxCommits,
    });
    if (result.denied) {
      setDenied(true);
      setBusy(false);
      return;
    }
    if (!result.ok) {
      setError(result.message ?? "ذخیرهٔ محدودیت سفارش انجام نشد.");
      setBusy(false);
      return;
    }
    setLimits(result.data);
    setLimitsDraft({
      maxOpenUnpaidOrdersPerCustomer: String(result.data.maxOpenUnpaidOrdersPerCustomer),
      reservationCommitWindowMinutes: String(result.data.reservationCommitWindowMinutes),
      maxCheckoutCommitsPerCustomerInWindow: String(result.data.maxCheckoutCommitsPerCustomerInWindow),
    });
    setSuccess("محدودیت سفارش‌های باز و رزرو ذخیره شد.");
    setBusy(false);
  }

  async function onSaveAppearance() {
    setBusy(true);
    setError(null);
    setSuccess(null);
    const result = await saveAppearanceSettings(appearanceDraft, appearanceThemeDraft, appearanceSkinDraft);
    if (result.denied) {
      setDenied(true);
      setBusy(false);
      return;
    }
    if (!result.ok) {
      setError(result.message ?? "ذخیرهٔ ظاهر فروشگاه انجام نشد.");
      setBusy(false);
      return;
    }
    setAppearance(result.data);
    setAppearanceDraft(result.data.paletteKey);
    setAppearanceThemeDraft(result.data.themeMode);
    setAppearanceSkinDraft(result.data.productCardSkin);
    setSuccess("ظاهر فروشگاه ذخیره شد.");
    setBusy(false);
  }

  function onCancelAppearance() {
    setAppearanceDraft(appearance?.paletteKey ?? "tooba-blue");
    setAppearanceThemeDraft(appearance?.themeMode ?? "LightOnly");
    setAppearanceSkinDraft(appearance?.productCardSkin ?? "classic");
    setError(null);
    setSuccess(null);
  }

  function onCancelHolds() {
    if (holds) applyHoldView(holds);
    setError(null);
    setSuccess(null);
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
          <p className="text-sm text-gray-500 mt-1">پروفایل، زبان، ظاهر فروشگاه، و یک گرد کردن سراسری مقدار</p>
        </div>

        <div className="flex overflow-x-auto border-b border-gray-200 scrollbar-hide">
          {(
            [
              { id: "profile" as const, label: "پروفایل", icon: User },
              { id: "locale" as const, label: "زبان", icon: Globe2 },
              { id: "quantity" as const, label: "مقدار", icon: Hash },
              { id: "holds" as const, label: "مهلت‌ها", icon: Clock },
              { id: "identity" as const, label: "هویت خرید", icon: ShieldCheck },
              { id: "limits" as const, label: "سفارش باز", icon: ShieldAlert },
              { id: "appearance" as const, label: "ظاهر فروشگاه", icon: Palette },
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
          {activeTab === "appearance" ? (
            <AdminAppearanceSettingsForm
              view={appearance}
              draftKey={appearanceDraft}
              draftTheme={appearanceThemeDraft}
              draftSkin={appearanceSkinDraft}
              busy={busy}
              readOnly={readOnly}
              error={error}
              onSelect={setAppearanceDraft}
              onSelectTheme={setAppearanceThemeDraft}
              onSelectSkin={setAppearanceSkinDraft}
              onSave={() => void onSaveAppearance()}
              onCancel={onCancelAppearance}
            />
          ) : activeTab === "limits" ? (
            <form
              className="space-y-5"
              onSubmit={(event) => {
                event.preventDefault();
                void onSaveLimits();
              }}
              data-testid="admin-settings-limits-form"
            >
              <h2 className="text-sm font-bold text-gray-900">کنترل سفارش‌های پرداخت‌نشده و سوءاستفاده از رزرو</h2>
              <p className="text-sm text-gray-500 leading-7">
                ظرفیت سفارش باز با تعداد سفارش‌های قابل پرداخت یا در انتظار بررسی اندازه‌گیری می‌شود. سهمیه رزرو جدا است و با لغو برنمی‌گردد.
              </p>
              <p className="text-xs text-amber-800 bg-amber-50 border border-amber-100 rounded-xl px-3 py-2 leading-6">
                لغو سفارش ظرفیت سفارش باز را آزاد می‌کند، اما سهمیه رزرو استفاده‌شده در بازه زمانی را بازنمی‌گرداند.
              </p>
              <label className="block rounded-xl border border-gray-100 p-4">
                <span className="text-sm font-medium text-gray-800">حداکثر سفارش‌های بازِ پرداخت‌نشده</span>
                <p className="text-xs text-gray-500 mt-1">هر مشتری همزمان حداکثر این تعداد سفارش قابل پرداخت یا در انتظار بررسی می‌تواند داشته باشد.</p>
                <input
                  type="number"
                  min={limits?.minOpenUnpaid ?? 1}
                  max={limits?.maxOpenUnpaid ?? 20}
                  value={limitsDraft.maxOpenUnpaidOrdersPerCustomer}
                  onChange={(event) => setLimitsDraft((current) => ({ ...current, maxOpenUnpaidOrdersPerCustomer: event.target.value }))}
                  className="mt-3 w-32 px-3 py-2 bg-gray-50 rounded-xl text-sm border border-gray-200"
                  data-testid="admin-settings-max-open-unpaid"
                />
              </label>
              <label className="block rounded-xl border border-gray-100 p-4">
                <span className="text-sm font-medium text-gray-800">بازه کنترل رزروهای پیاپی</span>
                <p className="text-xs text-gray-500 mt-1">لغو سفارش سهمیه استفاده‌شده در این بازه را برنمی‌گرداند.</p>
                <div className="mt-3 flex items-center gap-2">
                  <input
                    type="number"
                    min={limits?.minWindowMinutes ?? 1}
                    max={limits?.maxWindowMinutes ?? 10080}
                    value={limitsDraft.reservationCommitWindowMinutes}
                    onChange={(event) => setLimitsDraft((current) => ({ ...current, reservationCommitWindowMinutes: event.target.value }))}
                    className="w-32 px-3 py-2 bg-gray-50 rounded-xl text-sm border border-gray-200"
                    data-testid="admin-settings-commit-window"
                  />
                  <span className="text-xs text-gray-500">دقیقه</span>
                </div>
              </label>
              <label className="block rounded-xl border border-gray-100 p-4">
                <span className="text-sm font-medium text-gray-800">حداکثر شروع رزرو در این بازه</span>
                <p className="text-xs text-gray-500 mt-1">هر شروع موفق رزرو اولیه یک سهمیه مصرف می‌کند؛ تلاش پرداخت روی همان سفارش سهمیه جدید نمی‌گیرد.</p>
                <input
                  type="number"
                  min={limits?.minCommits ?? 1}
                  max={limits?.maxCommits ?? 30}
                  value={limitsDraft.maxCheckoutCommitsPerCustomerInWindow}
                  onChange={(event) => setLimitsDraft((current) => ({ ...current, maxCheckoutCommitsPerCustomerInWindow: event.target.value }))}
                  className="mt-3 w-32 px-3 py-2 bg-gray-50 rounded-xl text-sm border border-gray-200"
                  data-testid="admin-settings-max-commits"
                />
              </label>
              <button
                type="submit"
                disabled={busy || readOnly}
                className="w-full py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold disabled:opacity-70"
                data-testid="admin-settings-save-limits"
              >
                ذخیره محدودیت سفارش
              </button>
            </form>
          ) : activeTab === "identity" ? (
            <form
              className="space-y-5"
              onSubmit={(event) => {
                event.preventDefault();
                void onSaveIdentity();
              }}
              data-testid="admin-settings-identity-form"
            >
              <h2 className="text-sm font-bold text-gray-900">هویت مشتری در فرایند خرید</h2>
              <p className="text-sm text-gray-500 leading-7">
                سبد می‌تواند ناشناس بماند. ارسال، تسویه و پرداخت در حالت ورود الزامی نیازمند حساب مشتری است.
              </p>
              <label className="flex items-start gap-3 rounded-xl border border-gray-100 p-4">
                <input
                  type="radio"
                  name="checkout-identity"
                  checked={identityDraft === "AuthenticatedOnly"}
                  onChange={() => setIdentityDraft("AuthenticatedOnly")}
                  data-testid="admin-settings-identity-authenticated"
                />
                <span>
                  <span className="block text-sm font-bold">ورود الزامی</span>
                  <span className="block text-xs text-gray-500">پیش‌فرض فروشگاه</span>
                </span>
              </label>
              <label className="flex items-start gap-3 rounded-xl border border-gray-100 p-4">
                <input
                  type="radio"
                  name="checkout-identity"
                  checked={identityDraft === "GuestAllowed"}
                  onChange={() => setIdentityDraft("GuestAllowed")}
                  data-testid="admin-settings-identity-guest"
                />
                <span>
                  <span className="block text-sm font-bold">خرید مهمان مجاز</span>
                  <span className="block text-xs text-amber-700 mt-1">
                    خرید مهمان می‌تواند کنترل سفارش‌های پرداخت‌نشده و محدودیت‌های رزرو موجودی را کاهش دهد.
                  </span>
                </span>
              </label>
              {identity?.warningFa && identityDraft === "GuestAllowed" ? (
                <p className="text-xs text-amber-700">{identity.warningFa}</p>
              ) : null}
              <button
                type="submit"
                disabled={busy || readOnly}
                className="w-full py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold disabled:opacity-70"
                data-testid="admin-settings-save-identity"
              >
                ذخیره هویت خرید
              </button>
            </form>
          ) : activeTab === "holds" ? (
            <form
              className="space-y-5"
              onSubmit={(event) => {
                event.preventDefault();
                void onSaveHolds();
              }}
              data-testid="admin-settings-hold-form"
            >
              <p className="text-sm text-gray-500 leading-7" data-testid="admin-settings-hold-helper-fa">
                مهلت‌ها از تنظیمات فروشگاه خوانده می‌شوند. خالی‌گذاشتن یعنی استفاده از مقدار پیش‌فرض سامانه. مدت نگهداری سبد موجودی را رزرو نمی‌کند.
              </p>
              <p className="text-xs text-gray-400 leading-6" dir="ltr" data-testid="admin-settings-hold-helper-en">
                Store values override platform defaults. Empty inherits. Cart persistence does not reserve inventory.
              </p>
              {(
                [
                  ["cartPersistenceHours", holds?.cartPersistence, "ساعت"] as const,
                  ["onlinePaymentHoldHours", holds?.onlinePaymentHold, "ساعت"] as const,
                  ["manualPaymentInitialHoldHours", holds?.manualInitialHold, "ساعت"] as const,
                  ["manualPaymentReviewHoldHours", holds?.manualReviewHold, "ساعت"] as const,
                ]
              ).map(([key, view, unit]) => (
                <div key={key} className="rounded-xl border border-gray-100 p-4">
                  <label className="text-sm font-medium text-gray-800">{view?.labelFa ?? key}</label>
                  <p className="text-xs text-gray-500 mt-1">{view?.helperFa}</p>
                  <p className="text-[11px] text-gray-400 mt-1" dir="ltr">{view?.labelEn} — {view?.helperEn}</p>
                  <div className="mt-3 flex items-center gap-2">
                    <input
                      type="number"
                      min={1}
                      value={holdDraft[key]}
                      onChange={(e) => setHoldDraft((current) => ({ ...current, [key]: e.target.value }))}
                      disabled={busy}
                      className="w-32 px-3 py-2 bg-gray-50 rounded-xl text-sm border border-gray-200"
                      data-testid={`admin-settings-hold-${key}`}
                    />
                    <span className="text-xs text-gray-500">{unit}</span>
                    <span className="text-xs text-gray-400">
                      مؤثر: {view?.effectiveHours ?? "—"} {unit}
                      {view?.source === "platform" ? " (پیش‌فرض سامانه)" : " (فروشگاه)"}
                    </span>
                  </div>
                </div>
              ))}
              <div className="rounded-xl border border-gray-100 p-4 space-y-3" data-testid="admin-settings-hold-methods">
                <p className="text-sm font-medium text-gray-800">override روش پرداخت</p>
                <p className="text-xs text-gray-500">در صورت نیاز، مهلت هر روش پرداخت جداگانه ذخیره می‌شود.</p>
                {methodDraft.map((method, index) => (
                  <div key={method.providerCode} className="grid grid-cols-1 md:grid-cols-4 gap-3">
                    <p className="text-sm font-bold self-center">{method.labelFa}</p>
                    <input
                      type="number"
                      min={1}
                      placeholder="آنلاین"
                      value={method.onlinePaymentHoldHours ?? ""}
                      onChange={(e) => {
                        const value = e.target.value === "" ? null : Number(e.target.value);
                        setMethodDraft((rows) => rows.map((row, i) => (i === index ? { ...row, onlinePaymentHoldHours: value } : row)));
                      }}
                      className="px-3 py-2 bg-gray-50 rounded-xl text-sm border border-gray-200"
                      data-testid={`admin-settings-hold-method-${method.providerCode}-online`}
                    />
                    <input
                      type="number"
                      min={1}
                      placeholder="ثبت مدرک"
                      value={method.manualPaymentInitialHoldHours ?? ""}
                      onChange={(e) => {
                        const value = e.target.value === "" ? null : Number(e.target.value);
                        setMethodDraft((rows) => rows.map((row, i) => (i === index ? { ...row, manualPaymentInitialHoldHours: value } : row)));
                      }}
                      className="px-3 py-2 bg-gray-50 rounded-xl text-sm border border-gray-200"
                      data-testid={`admin-settings-hold-method-${method.providerCode}-initial`}
                    />
                    <input
                      type="number"
                      min={1}
                      placeholder="بررسی"
                      value={method.manualPaymentReviewHoldHours ?? ""}
                      onChange={(e) => {
                        const value = e.target.value === "" ? null : Number(e.target.value);
                        setMethodDraft((rows) => rows.map((row, i) => (i === index ? { ...row, manualPaymentReviewHoldHours: value } : row)));
                      }}
                      className="px-3 py-2 bg-gray-50 rounded-xl text-sm border border-gray-200"
                      data-testid={`admin-settings-hold-method-${method.providerCode}-review`}
                    />
                  </div>
                ))}
              </div>
              {holds?.reservationCycle ? (
                <ReservationPolicyEditor
                  view={holds.reservationCycle}
                  draft={reservationDraft}
                  onDraftChange={setReservationDraft}
                  canEdit
                  busy={busy}
                  locale={locale === "en" ? "en" : "fa"}
                  testId="admin-settings-reservation-policy"
                />
              ) : null}
              <div className="flex gap-3">
                <button
                  type="submit"
                  disabled={busy}
                  className="flex-1 py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold disabled:opacity-70"
                  data-testid="admin-settings-save-holds"
                >
                  <Save className="w-4 h-4 inline-block ml-1" />
                  ذخیره مهلت‌ها
                </button>
                <button
                  type="button"
                  disabled={busy}
                  onClick={onCancelHolds}
                  className="px-4 py-2.5 rounded-xl border border-gray-200 text-sm font-bold"
                  data-testid="admin-settings-cancel-holds"
                >
                  انصراف
                </button>
              </div>
            </form>
          ) : activeTab === "quantity" ? (
            <form
              className="space-y-4"
              onSubmit={(event) => {
                event.preventDefault();
                void onSaveRounding();
              }}
              data-testid="admin-settings-quantity-form"
            >
              <p className="text-sm text-gray-500 leading-7" data-testid="admin-settings-rounding-helper-fa">
                یک حالت گرد کردن سراسری برای نرمال‌سازی مقدار و محاسبات مالی جدید. سفارش‌ها و فاکتورهای تاریخی بازنویسی نمی‌شوند.
              </p>
              <p className="text-xs text-gray-400 leading-6" dir="ltr" data-testid="admin-settings-rounding-helper-en">
                Applies to new quantity and financial calculations only. Historical orders and invoices are not recalculated.
              </p>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                {(
                  [
                    { id: "Floor" as const, fa: "رو به پایین", en: "Floor" },
                    { id: "Ceiling" as const, fa: "رو به بالا", en: "Ceiling" },
                    { id: "Nearest" as const, fa: "نزدیک‌ترین مقدار", en: "Nearest" },
                  ] as const
                ).map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    disabled={busy}
                    onClick={() => setRoundingMode(item.id)}
                    className={`p-4 rounded-xl border-2 transition-all text-start ${
                      roundingMode === item.id
                        ? "border-[#2563EB] bg-[#2563EB]/5"
                        : "border-gray-200 hover:border-gray-300"
                    } ${busy ? "opacity-70 cursor-not-allowed" : ""}`}
                    data-testid={`admin-settings-rounding-${item.id.toLowerCase()}`}
                    aria-pressed={roundingMode === item.id}
                  >
                    <p className="text-sm font-medium text-gray-800">{item.fa}</p>
                    <p className="text-xs text-gray-500 mt-1" dir="ltr">
                      {item.en}
                    </p>
                  </button>
                ))}
              </div>
              <button
                type="submit"
                disabled={busy}
                className="w-full py-2.5 bg-[#2563EB] text-white rounded-xl text-sm font-bold hover:bg-[#1D4ED8] transition-colors shadow-lg shadow-[#2563EB]/30 flex items-center justify-center gap-2 disabled:opacity-70"
                data-testid="admin-settings-save-rounding"
              >
                <Save className="w-4 h-4" />
                ذخیره گرد کردن مقدار
              </button>
            </form>
          ) : activeTab === "profile" ? (
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
