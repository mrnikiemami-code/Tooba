"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Ban,
  Bike,
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
import { resolveAdminChromeLocale } from "./admin-chrome-messages.ts";
import { loadAdminLanguages } from "./language-api.ts";
import {
  deactivateAdminShippingService,
  loadAdminShippingService,
  loadAdminShippingServices,
  writeAdminShippingService,
  type ShippingServiceListItem,
  type ShippingServiceOptionWrite,
  type ShippingServiceTranslationWrite,
} from "./shipping-services-api.ts";
import { createHostSavedViewStore } from "./saved-view-store";
import type { SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";

const GRID_VIEW_KEY = "admin-shipping-services";

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
  const [rows, setRows] = useState<ShippingServiceListItem[]>([]);
  const [languages, setLanguages] = useState<SupportedLocaleDefinition[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
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

  const refresh = useCallback(async () => {
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
    setRows(listResult.data);
    setLanguages(langsResult.data.filter((row) => row.active && row.languageId));
    setState("ok");
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
    setDialogOpen(true);
  }

  async function openEdit(serviceId: string) {
    const result = await loadAdminShippingService(serviceId);
    if (result.state !== "ok" || !result.data) {
      setMessage(result.message ?? "سرویس خوانده نشد");
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
    setDialogOpen(true);
  }

  async function save() {
    setSaving(true);
    const result = await writeAdminShippingService(editingId, {
      code,
      providerKind,
      iconKey,
      colorKey,
      isActive,
      sortOrder: Number(sortOrder) || 0,
      translations: translations.filter((t) => t.name.trim()),
      options: options.filter((o) => o.code.trim()),
    });
    setSaving(false);
    if (result.state !== "ok") {
      setMessage(result.message ?? "ذخیره ناموفق بود");
      return;
    }
    setDialogOpen(false);
    await refresh();
  }

  async function deactivate(serviceId: string) {
    const result = await deactivateAdminShippingService(serviceId);
    if (result.state !== "ok") {
      setMessage(result.message ?? "غیرفعال‌سازی ناموفق بود");
      return;
    }
    await refresh();
  }

  const columns = useMemo<GridColumnDef<ShippingServiceListItem>[]>(() => [
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
      accessor: (row) => row.shippingServiceId,
      cell: (row) => (
        <div className="flex flex-wrap gap-1">
          <button
            type="button"
            className="inline-flex items-center gap-1 rounded border border-gray-200 px-2 py-1 text-[11px] font-bold text-gray-700"
            onClick={() => void openEdit(row.shippingServiceId)}
          >
            <Pencil className="size-3.5 text-blue-600" aria-hidden />
            ویرایش
          </button>
          {row.isActive ? (
            <button
              type="button"
              className="inline-flex items-center gap-1 rounded border border-red-200 px-2 py-1 text-[11px] font-bold text-red-700"
              onClick={() => void deactivate(row.shippingServiceId)}
            >
              <Ban className="size-3.5" aria-hidden />
              غیرفعال
            </button>
          ) : null}
        </div>
      ),
      width: 160,
      minWidth: 140,
    },
  ], [languages]);

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

  const activeLang = translations.find((t) => t.languageId === langTab) ?? translations[0];

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
            <AppDataGrid<ShippingServiceListItem> {...gridProps} />
          </div>
        </section>
      )}

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
                  aria-selected={langTab === lang.languageId}
                  className={
                    langTab === lang.languageId
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
                          نام ({languages.find((l) => l.languageId === langTab)?.nativeName || "زبان"})
                        </span>
                        <input
                          value={option.translations.find((t) => t.languageId === langTab)?.name ?? ""}
                          onChange={(e) => setOptions((prev) => prev.map((o, i) => {
                            if (i !== index) return o;
                            return {
                              ...o,
                              translations: o.translations.map((t) => (
                                t.languageId === langTab ? { ...t, name: e.target.value } : t
                              )),
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

          {message ? <p className="text-xs font-semibold text-red-600">{message}</p> : null}

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
              disabled={saving || !code.trim()}
              className="rounded-lg bg-[#2563EB] px-3 py-2 text-sm font-bold text-white disabled:opacity-50"
              onClick={() => void save()}
            >
              ذخیره
            </button>
          </div>
        </div>
      </Dialog>
    </main>
  );
}
