"use client";

/**
 * TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146 — صفحهٔ «سرویس ارسال» تک‌سطحی.
 *
 * مدل کانونیکال: **یک روش قابل‌انتخاب مشتری = یک ردیف مستقل مدیریتی**.
 * منبع ردیف‌ها کاتالوگ canonical یک‌سطحی سرور است
 * (`GET /v1/admin/shipping-services/shipping-methods/one-level`) که برای هر روش `methodCode`
 * canonical و ترجمه‌های واقعی خودش را برمی‌گرداند.
 *
 * قواعد سخت:
 * - هر ردیف فقط سه کنترل عملیاتی دارد: مشاهده / ویرایش / حذف. آیکن‌های پراکندهٔ نرخ، پوشش، برنامه و
 *   بسته‌بندی حذف شده‌اند؛ آن‌ها داخل چهار زبانهٔ همان روش هستند.
 * - هیچ زبانهٔ «انواع سرویس» و هیچ زبانهٔ سراسری روی کل فهرست وجود ندارد.
 * - «حذف» همان عملیات امن موجود است: از تنها درز مجاز ۰۹۶ (`…/one-level/{methodCode}/activation`)
 *   همان روش غیرفعال می‌شود. هیچ رکورد نرخ/پوشش/برنامه/بسته‌بندی حذف نمی‌شود و هیچ روش خواهر
 *   تغییر نمی‌کند، چون هیچ‌جای این مسیر روی سرویس/گزینهٔ والد نوشته نمی‌شود.
 * - `serviceCode`/`optionCode` هرگز در مرورگر از متن `methodCode` ساخته نمی‌شوند؛ تنها از پیوند
 *   authoritative سرور خوانده می‌شوند.
 * - هیچ قیمت‌گذاری مشتری، رزرو یا تغییر Checkout/Order در این مسیر وجود ندارد.
 */

import { useCallback, useEffect, useMemo, useState } from "react";
import { Eye, Pencil, Plus, Trash2 } from "lucide-react";
import {
  AppDataGrid,
  Dialog,
  ErrorState,
  faWorkspaceMessages,
  createClientGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../../design-system";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import { createHostSavedViewStore } from "../saved-view-store";
import { buildFlatMethodRows, boolFa, NO_LABEL_FA, type FlatMethodRow } from "./flat-method-rows.ts";
import { loadOneLevelCatalog } from "./flat-method-catalog-api.ts";
import {
  FlatMethodDetailDialog,
  type FlatDetailTabId,
} from "./flat-method-detail-dialog.tsx";
import { loadOneLevelMethodBinding, setOneLevelMethodActivation } from "./shipping-one-level-activation-model.ts";
import {
  ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA,
  activationErrorFa,
  activationUnavailableFa,
} from "./shipping-one-level-activation-model.ts";
import type { OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";
import { loadAdminLanguages } from "../../../features/admin-languages";
import type { SupportedLocaleDefinition } from "../../../lib/i18n/supported-locales.ts";
import { validateCreateMethodInput } from "./flat-method-create-model.ts";
import { createFailureMessage, createOneLevelMethod } from "./flat-method-create-api.ts";

const GRID_VIEW_KEY = "admin-shipping-services";

export function ShippingServicesFlatScreen({
  initialMethods,
  activeLanguageId: initialActiveLanguageId,
}: {
  initialMethods: readonly OneLevelMethodRow[];
  activeLanguageId: string;
}) {
  const [state, setState] = useState<"loading" | "ok" | "denied" | "error">("ok");
  const [methods, setMethods] = useState<readonly OneLevelMethodRow[]>(initialMethods);
  const [activeLanguageId, setActiveLanguageId] = useState(initialActiveLanguageId);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [notice, setNotice] = useState<{ tone: "success" | "error"; text: string } | undefined>(undefined);
  const [detail, setDetail] = useState<{ row: FlatMethodRow; mode: "view" | "edit" } | null>(null);
  const [activeTab, setActiveTab] = useState<FlatDetailTabId>("general");
  const [createOpen, setCreateOpen] = useState(false);
  const [createCode, setCreateCode] = useState("");
  const [createProvider, setCreateProvider] = useState("");
  const [createNames, setCreateNames] = useState<Record<string, string>>({});
  const [createLanguages, setCreateLanguages] = useState<readonly SupportedLocaleDefinition[]>([]);
  const [createLanguagesState, setCreateLanguagesState] = useState<"idle" | "loading" | "ok" | "denied" | "error">("idle");
  const [createSubmitting, setCreateSubmitting] = useState(false);
  const [createError, setCreateError] = useState<string | undefined>(undefined);

  const refresh = useCallback(async (opts?: { notice?: string; tone?: "success" | "error" }) => {
    setState("loading");
    const result = await loadOneLevelCatalog();
    if (result.state === "denied") {
      setState("denied");
      return;
    }
    if (result.state !== "ok" || !result.data) {
      setState("error");
      setMessage(result.message);
      return;
    }
    setMethods(result.data.methods);
    setActiveLanguageId(result.data.activeLanguageId);
    setState("ok");
    setNotice(opts?.notice ? { tone: opts.tone ?? "success", text: opts.notice } : undefined);
  }, []);

  useEffect(() => {
    // ردیف‌های سرور مقدار اولیه‌اند؛ نبودشان یعنی هنوز چیزی خوانده نشده است.
    if (initialMethods.length === 0) void refresh();
  }, [initialMethods.length, refresh]);

  /** زبان‌های فرم فقط از رجیستری واقعی سرور می‌آیند؛ هیچ زبان یا نامی در کلاینت ساخته نمی‌شود. */
  const openCreate = useCallback(async () => {
    setCreateCode("");
    setCreateProvider("");
    setCreateNames({});
    setCreateError(undefined);
    setCreateLanguagesState("loading");
    setCreateOpen(true);
    const result = await loadAdminLanguages();
    if (result.state === "ok" && result.data) {
      const withIds = result.data.filter((x) => (x.languageId ?? "").length > 0);
      setCreateLanguages(withIds);
      setCreateLanguagesState(withIds.length > 0 ? "ok" : "error");
      return;
    }
    setCreateLanguages([]);
    setCreateLanguagesState(result.state === "denied" ? "denied" : "error");
  }, []);

  /**
   * ایجاد واقعی روش canonical از مسیر مجاز سرور. ناموفق‌بودن هرگز «موفقیت» نمایش نمی‌دهد؛
   * فقط پاسخ تأییدشدهٔ سرور فهرست را تازه می‌کند.
   */
  const submitCreate = useCallback(async () => {
    if (createSubmitting) return;
    const validation = validateCreateMethodInput({
      methodCode: createCode,
      providerKind: createProvider,
      names: createLanguages.map((language) => ({
        languageId: language.languageId ?? "",
        name: createNames[language.languageId ?? ""] ?? "",
      })),
    });
    if (!validation.ok) {
      setCreateError(validation.message);
      return;
    }

    setCreateSubmitting(true);
    setCreateError(undefined);
    const result = await createOneLevelMethod(validation.request);
    setCreateSubmitting(false);

    if (result.state !== "ok" || !result.data) {
      setCreateError(createFailureMessage(result));
      return;
    }

    setCreateOpen(false);
    await refresh({
      notice:
        `روش «${result.data.methodCode}» ساخته و دوباره از سرور خوانده شد. این روش غیرفعال است و تا ثبت `
        + "سیاست نرخ اختصاصی همین روش و فعال‌سازی، برای مشتری قابل‌انتخاب/قیمت‌گذاری نیست.",
      tone: "success",
    });
  }, [createCode, createLanguages, createNames, createProvider, createSubmitting, refresh]);

  const rows = useMemo(
    () => buildFlatMethodRows(methods, activeLanguageId),
    [methods, activeLanguageId],
  );

  /** حذف امن: فقط همان روش، از تنها درز مجاز ۰۹۶. هیچ سیاست ثبت‌شده‌ای حذف نمی‌شود. */
  const removeMethod = useCallback(async (row: FlatMethodRow) => {
    const binding = await loadOneLevelMethodBinding(row.methodCode);
    if (binding.state !== "resolved") {
      setNotice({
        tone: "error",
        text: activationUnavailableFa(binding.state === "untrusted" ? "untrusted" : "unresolved"),
      });
      return;
    }
    if (binding.binding.optionCode === null) {
      setNotice({ tone: "error", text: ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA });
      return;
    }
    if (!row.isActive) {
      setNotice({ tone: "error", text: "این روش از قبل غیرفعال است؛ تغییری اعمال نشد." });
      return;
    }
    const result = await setOneLevelMethodActivation(row.methodCode, false);
    if (result.state !== "ok" || !result.data) {
      setNotice({ tone: "error", text: activationErrorFa(result.message ?? "") });
      return;
    }
    await refresh({
      notice: `روش «${row.label || row.methodCode}» غیرفعال شد و از فهرست روش‌های قابل انتخاب فروشگاه خارج شد. سیاست‌های ثبت‌شده‌اش حذف نشد.`,
      tone: "success",
    });
  }, [refresh]);

  const rowActions = useMemo<AppGridRowAction<FlatMethodRow>[]>(
    () => [
      {
        id: "view",
        label: "مشاهده",
        icon: Eye,
        testId: (row) => `shipping-method-view-${row.id}`,
        onClick: (row) => {
          setNotice(undefined);
          setActiveTab("general");
          setDetail({ row, mode: "view" });
        },
      },
      {
        id: "edit",
        label: "ویرایش",
        icon: Pencil,
        testId: (row) => `shipping-method-edit-${row.id}`,
        onClick: (row) => {
          setNotice(undefined);
          setActiveTab("general");
          setDetail({ row, mode: "edit" });
        },
      },
      {
        id: "delete",
        label: "حذف",
        icon: Trash2,
        variant: "destructive",
        confirm: (row) =>
          `روش «${row.label || row.methodCode}» حذف (غیرفعال) شود؟ پس از آن در فروشگاه نمایش داده نمی‌شود و سیاست‌های ثبت‌شده‌اش پاک نمی‌شود.`,
        testId: (row) => `shipping-method-delete-${row.id}`,
        onClick: (row) => void removeMethod(row),
      },
    ],
    [removeMethod],
  );

  const columns = useMemo<GridColumnDef<FlatMethodRow>[]>(() => [
    {
      id: "name",
      header: "نام روش",
      accessor: (row) => (row.hasLabel ? row.label : row.methodCode),
      cell: (row) => (
        <span className="flex flex-col">
          <span className="font-bold text-gray-900">{row.hasLabel ? row.label : row.methodCode}</span>
          {row.hasLabel ? null : (
            <span className="mt-0.5 text-[10px] font-semibold text-amber-700" data-testid={`shipping-method-nolabel-${row.id}`}>
              {NO_LABEL_FA} — نام نمایشی این روش در زبان فعال ثبت نشده است
            </span>
          )}
        </span>
      ),
      width: 260,
      minWidth: 180,
      flex: 1.4,
    },
    {
      id: "code",
      header: "کد روش",
      accessor: (row) => row.methodCode,
      cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.methodCode}</span>,
      width: 180,
      minWidth: 130,
    },
    {
      id: "active",
      header: "وضعیت",
      accessor: (row) => row.isActive,
      cell: (row) => (
        <span
          className={
            row.isActive
              ? "rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-bold text-emerald-700"
              : "rounded-full bg-gray-100 px-2 py-0.5 text-xs font-bold text-gray-600"
          }
        >
          {boolFa(row.isActive)}
        </span>
      ),
      width: 100,
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
    return (
      <ErrorState
        title="خطا"
        detail={message ?? faWorkspaceMessages.retry}
        onRetry={() => void refresh()}
        retryLabel={faWorkspaceMessages.retry}
      />
    );
  }

  return (
    <main className="space-y-4" data-testid="admin-shipping-services-screen">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-black text-gray-900">سرویس ارسال</h1>
          <p className="mt-1 text-sm text-gray-500">
            هر روش ارسال یک ردیف مستقل است. برای هر روش فقط سه عملیات «مشاهده / ویرایش / حذف» وجود دارد و
            تعرفه، پوشش، برنامه کاری و بسته‌بندی داخل زبانه‌های همان روش مدیریت می‌شود.
          </p>
        </div>
        <button
          type="button"
          className="inline-flex min-h-10 items-center gap-2 rounded-xl bg-[#2563EB] px-4 text-sm font-bold text-white"
          data-testid="admin-shipping-method-create"
          onClick={() => void openCreate()}
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
            <AppDataGrid<FlatMethodRow> {...gridProps} />
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

      {createOpen ? (
        <Dialog open onClose={() => (createSubmitting ? undefined : setCreateOpen(false))} title="سرویس جدید" showCloseButton size="md">
          <form
            className="space-y-4 text-sm text-gray-700"
            data-testid="admin-shipping-method-create-form"
            onSubmit={(event) => {
              event.preventDefault();
              void submitCreate();
            }}
          >
            <p className="text-xs text-gray-500">
              هر روش ارسال یک هویت مستقل دارد. کد روش تغییرناپذیر است و پیوند قدیمی آن در سرور از
              همین کد مشتق و اعتبارسنجی می‌شود؛ روش تازه غیرفعال ساخته می‌شود.
            </p>

            <label className="block space-y-1">
              <span className="font-bold text-gray-900">کد روش</span>
              <input
                dir="ltr"
                className="w-full rounded-xl border border-gray-200 px-3 py-2 font-mono text-xs"
                value={createCode}
                placeholder="tipax:sameday"
                maxLength={32}
                disabled={createSubmitting}
                data-testid="admin-shipping-method-create-code"
                onChange={(event) => setCreateCode(event.target.value)}
              />
              <span className="block text-[11px] text-gray-500">
                حروف کوچک/رقم/زیرخط و حداکثر یک «:»؛ حداکثر ۳۲ نویسه. کد نمی‌تواند بعداً تغییر کند.
              </span>
            </label>

            <label className="block space-y-1">
              <span className="font-bold text-gray-900">شناسهٔ ارائه‌دهنده (اختیاری)</span>
              <input
                dir="ltr"
                className="w-full rounded-xl border border-gray-200 px-3 py-2 font-mono text-xs"
                value={createProvider}
                placeholder="tipax"
                maxLength={64}
                disabled={createSubmitting}
                data-testid="admin-shipping-method-create-provider"
                onChange={(event) => setCreateProvider(event.target.value)}
              />
              <span className="block text-[11px] text-gray-500">
                فقط metadata ارائه‌دهنده است و هیچ نرخ/پوشش/برنامه‌ای را با روش‌های خواهر به اشتراک
                نمی‌گذارد. خالی‌بودن یعنی از بخش سرویس همان کد گرفته شود.
              </span>
            </label>

            <div className="space-y-2" data-testid="admin-shipping-method-create-names">
              <span className="block font-bold text-gray-900">نام نمایشی به هر زبان</span>
              {createLanguagesState === "loading" ? (
                <p className="text-xs text-gray-500">در حال خواندن زبان‌های ثبت‌شده…</p>
              ) : createLanguagesState === "denied" ? (
                <p className="text-xs font-semibold text-red-700">
                  فهرست زبان‌ها قابل خواندن نیست؛ بدون زبان واقعی روشی ساخته نمی‌شود.
                </p>
              ) : createLanguagesState === "error" ? (
                <p className="text-xs font-semibold text-red-700">
                  زبان ثبت‌شده‌ای از سرور دریافت نشد؛ ساخت روش ممکن نیست.
                </p>
              ) : (
                createLanguages.map((language) => {
                  const languageId = language.languageId ?? "";
                  return (
                    <label key={languageId} className="flex items-center gap-2">
                      <span className="w-28 shrink-0 text-xs text-gray-600" dir="auto">
                        {language.nativeName || language.displayName || language.code}
                      </span>
                      <input
                        className="w-full rounded-xl border border-gray-200 px-3 py-2 text-xs"
                        value={createNames[languageId] ?? ""}
                        maxLength={128}
                        disabled={createSubmitting}
                        data-testid={`admin-shipping-method-create-name-${languageId}`}
                        onChange={(event) =>
                          setCreateNames((current) => ({ ...current, [languageId]: event.target.value }))}
                      />
                    </label>
                  );
                })
              )}
              <span className="block text-[11px] text-gray-500">
                حداقل یک زبان باید نام داشته باشد. زبان‌ها فقط از رجیستری واقعی سرور می‌آیند.
              </span>
            </div>

            {createError ? (
              <p
                role="alert"
                className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-xs font-semibold text-red-700"
                data-testid="admin-shipping-method-create-error"
              >
                {createError}
              </p>
            ) : null}

            <div className="flex flex-wrap items-center justify-end gap-2">
              <button
                type="button"
                className="inline-flex min-h-10 items-center rounded-xl border border-gray-200 px-4 text-sm font-bold text-gray-700"
                disabled={createSubmitting}
                onClick={() => setCreateOpen(false)}
              >
                انصراف
              </button>
              <button
                type="submit"
                className="inline-flex min-h-10 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-bold text-white disabled:opacity-60"
                disabled={createSubmitting || createLanguagesState !== "ok"}
                data-testid="admin-shipping-method-create-submit"
              >
                {createSubmitting ? "در حال ساخت…" : "ساخت روش"}
              </button>
            </div>
          </form>
        </Dialog>
      ) : null}

      {detail ? (
        <FlatMethodDetailDialog
          row={detail.row}
          method={methods.find((m) => m.methodCode === detail.row.methodCode)}
          mode={detail.mode}
          activeTab={activeTab}
          onTabChange={setActiveTab}
          activeLanguageId={activeLanguageId}
          onClose={() => setDetail(null)}
          onNotice={setNotice}
          onSaved={async (text) => {
            setDetail(null);
            await refresh({ notice: text, tone: "success" });
          }}
          onRefreshed={(text) => refresh({ notice: text, tone: "success" })}
        />
      ) : null}
    </main>
  );
}
