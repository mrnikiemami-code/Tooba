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

  const rows = useMemo(
    () => buildFlatMethodRows(methods, activeLanguageId),
    [methods, activeLanguageId],
  );

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
          onClick={() => setCreateOpen(true)}
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
        <Dialog open onClose={() => setCreateOpen(false)} title="سرویس جدید" showCloseButton size="md">
          <div className="space-y-3 text-sm text-gray-700" data-testid="admin-shipping-method-create-blocked">
            <p className="font-bold text-gray-900">
              ایجاد روش ارسال تازه در حال حاضر از سمت سرور پشتیبانی نمی‌شود.
            </p>
            <p>
              کاتالوگ یک‌سطحی تنها مسیر خواندن، «نوشتن نام نمایشی» و «فعال/غیرفعال‌سازی روش گزینه‌دار» را
              برای ادمین منتشر کرده است؛ هیچ endpoint ایجاد روش canonical وجود ندارد. مسیر قدیمی ایجاد
              «سرویس ارائه‌دهنده + گزینه» یک موجودیت دوسطحی می‌سازد و به‌عنوان روش مستقل canonical
              ساخته نمی‌شود، پس عمداً استفاده نمی‌شود.
            </p>
            <p className="text-xs text-gray-500">
              نتیجه: دکمهٔ «سرویس جدید» حفظ شده است، اما تا افزودن endpoint ایجاد روش در سرور هیچ سرویسی
              ساخته نمی‌شود؛ این مورد به‌عنوان BLOCKED گزارش می‌شود.
            </p>
          </div>
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
