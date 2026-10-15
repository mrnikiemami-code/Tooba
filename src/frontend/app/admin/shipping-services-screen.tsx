"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Ban,
  Bike,
  Calculator,
  Clock,
  Coins,
  Package,
  Pencil,
  Plus,
  Store,
  Trash2,
  Truck,
} from "lucide-react";
import {
  AppDataGrid,
  Dialog,
  ErrorState,
  faWorkspaceMessages,
  createClientGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../design-system";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import {
  AppGridRowActionsCell,
  type AppGridRowAction,
} from "../../design-system/app-data-grid/app-grid-row-actions";
import { resolveAdminChromeLocale } from "./admin-chrome-messages.ts";
import { loadAdminLanguages } from "../../features/admin-languages";
import {
  deactivateAdminShippingService,
  loadAdminShippingService,
  loadAdminShippingServices,
  writeAdminShippingService,
  type ShippingServiceListItem,
  type ShippingServiceOptionWrite,
  type ShippingServiceTranslationWrite,
} from "./shipping-services-api.ts";
import {
  activeTranslationName,
  buildShippingServiceWriteRequest,
  mapShippingServiceErrorFa,
  normalizeShippingServiceCode,
  validateShippingServiceForm,
  type ShippingServiceFormIssue,
} from "./shipping-services/shipping-services-form-model.ts";
import { createHostSavedViewStore } from "./saved-view-store";
import { ShippingRatesCoverageDialog } from "./shipping-services/shipping-rates-coverage-dialog.tsx";
import { ShippingOperatingScheduleDialog } from "./shipping-services/shipping-operating-schedule-dialog.tsx";
import { ShippingPackagingDialog } from "./shipping-services/shipping-packaging-dialog.tsx";
import { ShippingOperationalQuoteDialog } from "./shipping-services/shipping-operational-quote-dialog.tsx";
import type { SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";

const GRID_VIEW_KEY = "admin-shipping-services";

/**
 * ردیف گرید = آیتم API + هویت ردیف اجباری گرید.
 * `AppDataGrid` و `useLegacyAdminGridDirectProps` روی `{ id: string }` قید دارند و
 * شناسهٔ پایدار canonical همین `shippingServiceId` است؛ پس `id` فقط یک پرتابِ
 * هویتی است و نام فیلد API/DTO تغییر نمی‌کند. عملیات ردیف همچنان با
 * `shippingServiceId` هدف می‌گیرد (همان الگوی admin-campaigns-screen).
 */
type ShippingServiceGridRow = ShippingServiceListItem & { id: string };

const ICON_OPTIONS = [
  { value: "post", label: "پست" },
  { value: "tipax", label: "تیپاکس" },
  { value: "courier", label: "پیک آنلاین" },
  { value: "bike", label: "پیک" },
  { value: "store", label: "حضوری" },
  { value: "truck", label: "کامیون" },
] as const;

const COLOR_OPTIONS = [
  { value: "blue", label: "آبی", className: "bg-blue-500" },
  { value: "amber", label: "کهربایی", className: "bg-amber-500" },
  { value: "emerald", label: "سبز", className: "bg-emerald-500" },
  { value: "violet", label: "بنفش", className: "bg-violet-500" },
  { value: "rose", label: "صورتی", className: "bg-rose-500" },
  { value: "cyan", label: "فیروزه‌ای", className: "bg-cyan-500" },
] as const;

function boolFa(value: boolean): string {
  return value ? "بله" : "خیر";
}

function emptyServiceTranslations(languages: SupportedLocaleDefinition[]): ShippingServiceTranslationWrite[] {
  return languages
    .filter((row) => row.languageId)
    .map((row) => ({ languageId: row.languageId as string, name: "", description: "" }));
}

function emptyOptionTranslations(languages: SupportedLocaleDefinition[]): { languageId: string; name: string }[] {
  return languages
    .filter((row) => row.languageId)
    .map((row) => ({ languageId: row.languageId as string, name: "" }));
}

function ServiceIcon({ iconKey, colorKey }: { iconKey: string; colorKey: string }) {
  const color = {
    blue: "bg-blue-100 text-blue-700 ring-blue-200",
    amber: "bg-amber-100 text-amber-700 ring-amber-200",
    emerald: "bg-emerald-100 text-emerald-700 ring-emerald-200",
    violet: "bg-violet-100 text-violet-700 ring-violet-200",
    rose: "bg-rose-100 text-rose-700 ring-rose-200",
    cyan: "bg-cyan-100 text-cyan-700 ring-cyan-200",
  }[colorKey] ?? "bg-blue-100 text-blue-700 ring-blue-200";
  const Icon =
    iconKey === "bike" ? Bike
      : iconKey === "store" ? Store
        : iconKey === "courier" || iconKey === "tipax" ? Package
          : Truck;
  return (
    <span className={`inline-flex size-9 items-center justify-center rounded-xl ring-1 ${color}`}>
      <Icon className="size-4" aria-hidden />
    </span>
  );
}

export function ShippingServicesScreen() {
  const [state, setState] = useState<"loading" | "ok" | "denied" | "error">("loading");
  const [rows, setRows] = useState<ShippingServiceGridRow[]>([]);
  const [languages, setLanguages] = useState<SupportedLocaleDefinition[]>([]);
  // پیام خطای بارگذاری اولیه (فقط برای ErrorState صفحه).
  const [message, setMessage] = useState<string | undefined>(undefined);
  // بازخورد عملیات (ذخیره/غیرفعال‌سازی/خواندن) — همیشه در سطح صفحه دیده می‌شود،
  // چون خطای عملیات گرید تا پیش از این داخل مودال بسته رندر می‌شد و بی‌صدا می‌ماند.
  const [notice, setNotice] = useState<{ tone: "success" | "error"; text: string } | undefined>(undefined);
  // ایرادهای اعتبارسنجی فرم؛ تا پیش از این مقادیر ناقص با filter بی‌صدا حذف می‌شدند.
  const [formIssues, setFormIssues] = useState<ShippingServiceFormIssue[]>([]);
  const [busy, setBusy] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  // بخش نرخ/پوشش خدمت انتخاب‌شده؛ null یعنی بسته است.
  const [ratesTarget, setRatesTarget] = useState<{ code: string; name: string } | null>(null);
  // بخش «برنامه کاری ارسال» خدمت انتخاب‌شده؛ null یعنی بسته است.
  const [scheduleTarget, setScheduleTarget] = useState<{ code: string; name: string } | null>(null);
  // بخش «هزینه بسته‌بندی و نحوه پرداخت کرایه» خدمت انتخاب‌شده؛ null یعنی بسته است.
  const [packagingTarget, setPackagingTarget] = useState<{ code: string; name: string } | null>(null);
  // بخش «بررسی هزینه و شرایط ارسال» خدمت انتخاب‌شده؛ null یعنی بسته است.
  const [quoteTarget, setQuoteTarget] = useState<{ code: string; name: string } | null>(null);
  const [code, setCode] = useState("");
  const [providerKind, setProviderKind] = useState("post");
  const [iconKey, setIconKey] = useState("post");
  const [colorKey, setColorKey] = useState("blue");
  const [isActive, setIsActive] = useState(true);
  const [sortOrder, setSortOrder] = useState("10");
  const [translations, setTranslations] = useState<ShippingServiceTranslationWrite[]>([]);
  const [options, setOptions] = useState<ShippingServiceOptionWrite[]>([]);
  const [langTab, setLangTab] = useState("");
  const [saving, setSaving] = useState(false);

  /**
   * زبان فعال فرم. تک‌منبع حقیقت برای نام سرویس و نام نوع سرویس؛ پیش از این نام نوع سرویس
   * فقط به `langTab` بسته بود و در نبود تب فعال، تایپ کاربر بی‌اثر می‌شد.
   */
  const activeLanguageId =
    translations.find((t) => t.languageId === langTab)?.languageId ?? translations[0]?.languageId ?? "";

  const refresh = useCallback(async (opts?: { notice?: string; tone?: "success" | "error" }) => {
    setState("loading");
    const locale = resolveAdminChromeLocale();
    const [listResult, langsResult] = await Promise.all([
      loadAdminShippingServices(locale),
      loadAdminLanguages(),
    ]);
    if (listResult.state === "denied" || langsResult.state === "denied") {
      setState("denied");
      return;
    }
    if (listResult.state !== "ok" || langsResult.state !== "ok" || !listResult.data || !langsResult.data) {
      setState("error");
      setMessage(listResult.message ?? langsResult.message);
      return;
    }
    setRows(listResult.data.map((item) => ({ ...item, id: item.shippingServiceId })));
    setLanguages(langsResult.data.filter((row) => row.active && row.languageId));
    setState("ok");
    setNotice(
      opts?.notice
        ? { tone: opts.tone ?? "success", text: opts.notice }
        : undefined,
    );
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  function openCreate() {
    const next = emptyServiceTranslations(languages);
    setEditingId(null);
    setCode("");
    setProviderKind("post");
    setIconKey("post");
    setColorKey("blue");
    setIsActive(true);
    setSortOrder(String((rows.at(-1)?.sortOrder ?? 0) + 10));
    setTranslations(next);
    setLangTab(next[0]?.languageId ?? "");
    setOptions([{
      code: "express",
      isActive: true,
      sortOrder: 10,
      translations: emptyOptionTranslations(languages).map((t) => ({
        ...t,
        name: languages.find((l) => l.languageId === t.languageId)?.urlPrefix === "en" ? "Express" : "پیشتاز",
      })),
    }, {
      code: "standard",
      isActive: true,
      sortOrder: 20,
      translations: emptyOptionTranslations(languages).map((t) => ({
        ...t,
        name: languages.find((l) => l.languageId === t.languageId)?.urlPrefix === "en" ? "Standard" : "معمولی",
      })),
    }]);
    setFormIssues([]);
    setNotice(undefined);
    setDialogOpen(true);
  }

  const openEdit = useCallback(async (serviceId: string) => {
    setFormIssues([]);
    const result = await loadAdminShippingService(serviceId);
    if (result.state !== "ok" || !result.data) {
      // پیش از این پیام فقط داخل مودالِ بسته رندر می‌شد و کاربر هیچ بازخوردی نمی‌گرفت.
      setNotice({
        tone: "error",
        text: mapShippingServiceErrorFa(result.message, "سرویس ارسال خوانده نشد؛ دوباره تلاش کنید."),
      });
      return;
    }
    const merged = emptyServiceTranslations(languages).map((blank) => {
      const existing = result.data!.translations.find((row) => row.languageId === blank.languageId);
      return existing ?? blank;
    });
    setEditingId(serviceId);
    setCode(result.data.code);
    setProviderKind(result.data.providerKind);
    setIconKey(result.data.iconKey);
    setColorKey(result.data.colorKey);
    setIsActive(result.data.isActive);
    setSortOrder(String(result.data.sortOrder));
    setTranslations(merged);
    setLangTab(merged[0]?.languageId ?? "");
    setOptions(result.data.options.map((o) => ({
      shippingServiceOptionId: o.shippingServiceOptionId,
      code: o.code,
      isActive: o.isActive,
      sortOrder: o.sortOrder,
      translations: emptyOptionTranslations(languages).map((blank) => {
        const existing = o.translations.find((t) => t.languageId === blank.languageId);
        return existing ?? blank;
      }),
    })));
    setNotice(undefined);
    setDialogOpen(true);
  }, [languages]);

  async function save() {
    const fields = {
      code,
      providerKind,
      iconKey,
      colorKey,
      isActive,
      sortOrder,
      translations,
      options,
    };
    // اعتبارسنجی پیش از فراخوانی API: ورودی نامعتبر نباید بی‌صدا حذف شود یا به خطای پایگاه‌داده برسد.
    const issues = validateShippingServiceForm(fields, {
      activeLanguageId,
      otherServiceCodes: rows
        .filter((row) => row.shippingServiceId !== editingId)
        .map((row) => row.code),
    });
    if (issues.length > 0) {
      setFormIssues(issues);
      setNotice(undefined);
      return;
    }

    setFormIssues([]);
    setSaving(true);
    const result = await writeAdminShippingService(editingId, buildShippingServiceWriteRequest(fields));
    setSaving(false);
    if (result.state !== "ok") {
      setNotice({
        tone: "error",
        text: mapShippingServiceErrorFa(result.message, "ذخیره ناموفق بود؛ دوباره تلاش کنید."),
      });
      return;
    }

    const savedName = activeTranslationName(translations, activeLanguageId)
      || normalizeShippingServiceCode(code);
    setDialogOpen(false);
    setNotice(undefined);
    await refresh({
      notice: editingId ? `سرویس «${savedName}» ذخیره شد.` : `سرویس «${savedName}» ایجاد شد.`,
      tone: "success",
    });
  }

  const deactivate = useCallback(async (row: ShippingServiceGridRow) => {
    setBusy(true);
    const result = await deactivateAdminShippingService(row.shippingServiceId);
    setBusy(false);
    if (result.state !== "ok") {
      setNotice({
        tone: "error",
        text: mapShippingServiceErrorFa(result.message, "غیرفعال‌سازی ناموفق بود؛ دوباره تلاش کنید."),
      });
      return;
    }

    await refresh({ notice: `سرویس «${row.name}» غیرفعال شد.`, tone: "success" });
  }, [refresh]);

  const rowActions = useMemo<AppGridRowAction<ShippingServiceGridRow>[]>(
    () => [
      {
        id: "edit",
        label: "ویرایش",
        icon: Pencil,
        testId: (row) => `shipping-service-edit-${row.shippingServiceId}`,
        onClick: (row) => openEdit(row.shippingServiceId),
      },
      {
        id: "rates",
        label: "نرخ و پوشش",
        icon: Truck,
        testId: (row) => `shipping-service-rates-${row.shippingServiceId}`,
        onClick: (row) => {
          setNotice(undefined);
          setRatesTarget({ code: row.code, name: row.name });
        },
      },
      {
        id: "schedule",
        label: "برنامه کاری ارسال",
        icon: Clock,
        testId: (row) => `shipping-service-schedule-${row.shippingServiceId}`,
        onClick: (row) => {
          setNotice(undefined);
          setScheduleTarget({ code: row.code, name: row.name });
        },
      },
      {
        id: "packaging",
        label: "هزینه بسته‌بندی و پرداخت کرایه",
        icon: Coins,
        testId: (row) => `shipping-service-packaging-${row.shippingServiceId}`,
        onClick: (row) => {
          setNotice(undefined);
          setPackagingTarget({ code: row.code, name: row.name });
        },
      },
      {
        id: "quote",
        label: "بررسی هزینه و شرایط ارسال",
        icon: Calculator,
        testId: (row) => `shipping-service-quote-${row.shippingServiceId}`,
        onClick: (row) => {
          setNotice(undefined);
          setQuoteTarget({ code: row.code, name: row.name });
        },
      },
      {
        id: "deactivate",
        label: "غیرفعال‌سازی",
        icon: Ban,
        variant: "destructive",
        visible: (row) => row.isActive,
        disabled: () => busy,
        // غیرفعال‌سازی، سرویس را از فهرست ارسال فروشگاه بیرون می‌برد؛ پس تأیید صریح لازم دارد.
        confirm: (row) =>
          `سرویس «${row.name}» غیرفعال شود؟ پس از غیرفعال‌سازی در فروشگاه نمایش داده نمی‌شود.`,
        testId: (row) => `shipping-service-deactivate-${row.shippingServiceId}`,
        onClick: (row) => deactivate(row),
      },
    ],
    [busy, deactivate, openEdit],
  );

  const columns = useMemo<GridColumnDef<ShippingServiceGridRow>[]>(() => [
    {
      id: "icon",
      header: "",
      accessor: (row) => row.iconKey,
      cell: (row) => <ServiceIcon iconKey={row.iconKey} colorKey={row.colorKey} />,
      width: 56,
      minWidth: 56,
    },
    { id: "name", header: "نام", accessor: (row) => row.name, width: 180, minWidth: 120, flex: 1.2 },
    {
      id: "code",
      header: "کد",
      accessor: (row) => row.code,
      cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.code}</span>,
      width: 120,
      minWidth: 90,
    },
    {
      id: "options",
      header: "انواع سرویس",
      accessor: (row) => row.optionCount,
      cell: (row) => `${row.activeOptionCount.toLocaleString("fa-IR")} / ${row.optionCount.toLocaleString("fa-IR")}`,
      width: 120,
      minWidth: 100,
    },
    {
      id: "active",
      header: "فعال",
      accessor: (row) => row.isActive,
      cell: (row) => (
        <span className={row.isActive ? "rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-bold text-emerald-700" : "rounded-full bg-gray-100 px-2 py-0.5 text-xs font-bold text-gray-600"}>
          {boolFa(row.isActive)}
        </span>
      ),
      width: 90,
      minWidth: 80,
    },
    {
      id: "actions",
      header: "عملیات",
      accessor: () => "",
      exportable: false,
      sortable: false,
      width: 150,
      minWidth: 130,
      maxWidth: 200,
      cell: (row) => <AppGridRowActionsCell row={row} actions={rowActions} compact />,
    },
  ], [rowActions]);

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(rows, columns)(query),
    [columns, rows],
  );
  const savedViewStore = useMemo(() => createHostSavedViewStore(GRID_VIEW_KEY), []);
  const gridProps = useLegacyAdminGridDirectProps({
    gridId: GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  if (state === "denied") {
    return <ErrorState title="دسترسی ندارید" detail="برای مشاهده سرویس ارسال مجوز لازم است." />;
  }
  if (state === "error") {
    return <ErrorState title="خطا" detail={message ?? faWorkspaceMessages.retry} onRetry={() => void refresh()} retryLabel={faWorkspaceMessages.retry} />;
  }

  const activeLang = translations.find((t) => t.languageId === activeLanguageId);

  return (
    <main className="space-y-4" data-testid="admin-shipping-services-screen">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-black text-gray-900">سرویس ارسال</h1>
          <p className="mt-1 text-sm text-gray-500">
            تعریف روش‌های ارسال دو‌سطحی (مثلاً پست ← پیشتاز / معمولی) با نام چندزبانه.
          </p>
        </div>
        <button
          type="button"
          className="inline-flex items-center gap-2 rounded-lg bg-[#2563EB] px-3 py-2 text-sm font-bold text-white"
          onClick={openCreate}
          data-testid="admin-shipping-service-create"
        >
          <Plus className="size-4" aria-hidden />
          سرویس جدید
        </button>
      </div>

      {state === "loading" ? (
        <p className="text-sm text-gray-500">در حال بارگذاری…</p>
      ) : (
        <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
          <div className="overflow-x-auto p-2 md:p-4">
            <AppDataGrid<ShippingServiceGridRow> {...gridProps} />
          </div>
        </section>
      )}

      {notice ? (
        <p
          role={notice.tone === "error" ? "alert" : "status"}
          className={
            notice.tone === "error"
              ? "rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700"
              : "rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700"
          }
          data-testid="admin-shipping-services-notice"
        >
          {notice.text}
        </p>
      ) : null}

      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title={editingId ? "ویرایش سرویس ارسال" : "سرویس ارسال جدید"}
        showCloseButton={false}
        size="xl"
      >
        <div className="max-h-[75vh] space-y-4 overflow-y-auto" data-testid="admin-shipping-service-dialog">
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="block space-y-1">
              <span className="text-xs font-bold text-gray-600">کد سرویس</span>
              <input
                value={code}
                onChange={(e) => setCode(e.target.value)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
                placeholder="post"
                dir="ltr"
              />
            </label>
            <label className="block space-y-1">
              <span className="text-xs font-bold text-gray-600">Provider Kind</span>
              <input
                value={providerKind}
                onChange={(e) => setProviderKind(e.target.value)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
                dir="ltr"
              />
            </label>
            <label className="block space-y-1">
              <span className="text-xs font-bold text-gray-600">آیکن</span>
              <select
                value={iconKey}
                onChange={(e) => setIconKey(e.target.value)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
              >
                {ICON_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </label>
            <label className="block space-y-1">
              <span className="text-xs font-bold text-gray-600">رنگ آیکن</span>
              <select
                value={colorKey}
                onChange={(e) => setColorKey(e.target.value)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
              >
                {COLOR_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </label>
            <label className="block space-y-1">
              <span className="text-xs font-bold text-gray-600">ترتیب</span>
              <input
                value={sortOrder}
                onChange={(e) => setSortOrder(e.target.value)}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
                inputMode="numeric"
                dir="ltr"
              />
            </label>
            <label className="flex items-center gap-2 pt-6 text-sm font-semibold text-gray-700">
              <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
              فعال
            </label>
          </div>

          <div className="flex items-center gap-3 rounded-xl border border-gray-100 bg-gray-50 p-3">
            <ServiceIcon iconKey={iconKey} colorKey={colorKey} />
            <div>
              <p className="text-sm font-bold text-gray-900">{activeLang?.name || code || "پیش‌نمایش"}</p>
              <p className="text-[11px] text-gray-500">آیکن رنگی سرویس در مودال ایجاد مرسوله</p>
            </div>
          </div>

          <div>
            <div className="mb-2 flex flex-wrap gap-1" role="tablist">
              {languages.map((lang) => (
                <button
                  key={lang.languageId}
                  type="button"
                  role="tab"
                  aria-selected={activeLanguageId === lang.languageId}
                  className={
                    activeLanguageId === lang.languageId
                      ? "rounded-full bg-blue-600 px-3 py-1 text-[11px] font-bold text-white"
                      : "rounded-full border border-gray-200 px-3 py-1 text-[11px] font-semibold text-gray-600"
                  }
                  onClick={() => setLangTab(lang.languageId ?? "")}
                >
                  {lang.nativeName || lang.code}
                </button>
              ))}
            </div>
            {activeLang ? (
              <label className="block space-y-1">
                <span className="text-xs font-bold text-gray-600">نام نمایشی</span>
                <input
                  value={activeLang.name}
                  onChange={(e) => setTranslations((prev) => prev.map((t) => (
                    t.languageId === activeLang.languageId ? { ...t, name: e.target.value } : t
                  )))}
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
                />
              </label>
            ) : null}
          </div>

          <div className="space-y-2 rounded-xl border border-indigo-100 bg-indigo-50/40 p-3">
            <div className="flex items-center justify-between gap-2">
              <h3 className="text-sm font-black text-indigo-950">انواع سرویس (سطح ۲)</h3>
              <button
                type="button"
                className="inline-flex items-center gap-1 rounded border border-indigo-200 bg-white px-2 py-1 text-[11px] font-bold text-indigo-800"
                onClick={() => setOptions((prev) => [...prev, {
                  code: "",
                  isActive: true,
                  sortOrder: (prev.at(-1)?.sortOrder ?? 0) + 10,
                  translations: emptyOptionTranslations(languages),
                }])}
              >
                <Plus className="size-3.5" aria-hidden />
                افزودن نوع
              </button>
            </div>
            {options.length === 0 ? (
              <p className="text-xs text-indigo-900/70">برای این سرویس هنوز نوعی تعریف نشده (مثل پیک بدون سطح دوم).</p>
            ) : (
              <ul className="space-y-2">
                {options.map((option, index) => (
                  <li key={`${option.shippingServiceOptionId ?? "new"}-${index}`} className="rounded-lg border border-white bg-white p-3 shadow-sm">
                    <div className="grid gap-2 sm:grid-cols-[1fr_1fr_auto_auto]">
                      <label className="block space-y-1">
                        <span className="text-[11px] font-bold text-gray-600">کد</span>
                        <input
                          value={option.code}
                          onChange={(e) => setOptions((prev) => prev.map((o, i) => (
                            i === index ? { ...o, code: e.target.value } : o
                          )))}
                          className="w-full rounded-lg border border-gray-200 px-2 py-1.5 text-sm"
                          dir="ltr"
                          placeholder="express"
                        />
                      </label>
                      <label className="block space-y-1">
                        <span className="text-[11px] font-bold text-gray-600">
                          نام ({languages.find((l) => l.languageId === activeLanguageId)?.nativeName || "زبان"})
                        </span>
                        <input
                          value={option.translations.find((t) => t.languageId === activeLanguageId)?.name ?? ""}
                          onChange={(e) => setOptions((prev) => prev.map((o, i) => {
                            if (i !== index) return o;
                            const next = e.target.value;
                            const hasActiveLanguage = o.translations.some(
                              (t) => t.languageId === activeLanguageId,
                            );
                            return {
                              ...o,
                              translations: hasActiveLanguage
                                ? o.translations.map((t) => (
                                  t.languageId === activeLanguageId ? { ...t, name: next } : t
                                ))
                                : [...o.translations, { languageId: activeLanguageId, name: next }],
                            };
                          }))}
                          className="w-full rounded-lg border border-gray-200 px-2 py-1.5 text-sm"
                          placeholder="پیشتاز"
                        />
                      </label>
                      <label className="flex items-end gap-2 pb-2 text-xs font-semibold text-gray-700">
                        <input
                          type="checkbox"
                          checked={option.isActive}
                          onChange={(e) => setOptions((prev) => prev.map((o, i) => (
                            i === index ? { ...o, isActive: e.target.checked } : o
                          )))}
                        />
                        فعال
                      </label>
                      <button
                        type="button"
                        className="self-end rounded border border-red-200 p-2 text-red-600"
                        onClick={() => setOptions((prev) => prev.filter((_, i) => i !== index))}
                        aria-label="حذف نوع"
                      >
                        <Trash2 className="size-4" />
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {formIssues.length > 0 ? (
            <ul
              className="space-y-1 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-semibold text-red-700"
              data-testid="admin-shipping-service-form-errors"
            >
              {formIssues.map((issue) => (
                <li key={`${issue.field}-${issue.optionIndex ?? "service"}-${issue.message}`}>
                  {issue.message}
                </li>
              ))}
            </ul>
          ) : null}

          <div className="flex justify-end gap-2 border-t border-gray-100 pt-3">
            <button
              type="button"
              className="rounded-lg border border-gray-200 px-3 py-2 text-sm font-bold text-gray-700"
              onClick={() => setDialogOpen(false)}
            >
              انصراف
            </button>
            <button
              type="button"
              disabled={saving}
              className="rounded-lg bg-[#2563EB] px-3 py-2 text-sm font-bold text-white disabled:opacity-50"
              onClick={() => void save()}
            >
              ذخیره
            </button>
          </div>
        </div>
      </Dialog>

      <ShippingRatesCoverageDialog
        open={ratesTarget !== null}
        serviceCode={ratesTarget?.code ?? ""}
        serviceName={ratesTarget?.name ?? ""}
        onClose={() => setRatesTarget(null)}
        onNotice={setNotice}
      />

      <ShippingOperatingScheduleDialog
        open={scheduleTarget !== null}
        serviceCode={scheduleTarget?.code ?? ""}
        serviceName={scheduleTarget?.name ?? ""}
        onClose={() => setScheduleTarget(null)}
        onNotice={setNotice}
      />

      <ShippingPackagingDialog
        open={packagingTarget !== null}
        serviceCode={packagingTarget?.code ?? ""}
        serviceName={packagingTarget?.name ?? ""}
        onClose={() => setPackagingTarget(null)}
        onNotice={setNotice}
      />

      <ShippingOperationalQuoteDialog
        open={quoteTarget !== null}
        serviceCode={quoteTarget?.code ?? ""}
        serviceName={quoteTarget?.name ?? ""}
        onClose={() => setQuoteTarget(null)}
      />
    </main>
  );
}
