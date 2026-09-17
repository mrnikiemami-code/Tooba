"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Eye, FilePenLine, Home, Pencil, RotateCcw, Upload } from "lucide-react";
import {
  AppDataGrid,
  createClientGridQueryAdapter,
  formatJalaliDate,
  useLegacyAdminGridDirectProps,
  DEFAULT_GRID_QUERY,
} from "../../../design-system";
import { DEFAULT_APP_GRID_CAPABILITIES } from "../../../design-system/app-data-grid/app-grid-capabilities";
import {
  AppGridRowActionsCell,
  type AppGridRowAction,
} from "../../../design-system/app-data-grid/app-grid-row-actions";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import { ADMIN_LANDING_PAGES_GRID_VIEW_KEY, createHostSavedViewStore } from "../saved-view-store";
import {
  getAdminLandingHome,
  listAdminLandingPages,
  publicPathForStorePage,
  restoreDefaultAdminHome,
  setAdminLandingHome,
  setAdminLandingPageStatus,
  type AdminLandingPage,
} from "./admin-landing-pages-api.ts";

type LandingGridRow = AdminLandingPage & {
  id: string;
  isHomeLabel: string;
  pageTypeLabel: string;
  indexabilityLabel: string;
  pathLabel: string;
};

const ORDERS_LIKE_CAPABILITIES = {
  ...DEFAULT_APP_GRID_CAPABILITIES,
  csvExport: false,
  excelExport: false,
};

/** خانهٔ پیش‌فرض فروشگاه = مسیر canonical در کد؛ با حذف Store Page از بین نمی‌رود. */
const RESTORE_DEFAULT_HOME_CONFIRM =
  "صفحه اصلی سفارشی لغو شود و خانهٔ پیش‌فرض فروشگاه بازگردد؟\nدادهٔ کالا/دسته/برند و صفحات لندینگ حذف نمی‌شوند — فقط انتخاب خانه پاک می‌شود.";

export function AdminLandingPagesScreen() {
  const [rows, setRows] = useState<LandingGridRow[]>([]);
  const [homePageId, setHomePageId] = useState<string | null>(null);
  const [usesCanonicalHome, setUsesCanonicalHome] = useState(true);
  const [message, setMessage] = useState<string>();
  const [messageTone, setMessageTone] = useState<"error" | "success">("error");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [denied, setDenied] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_LANDING_PAGES_GRID_VIEW_KEY), []);

  const refresh = useCallback((opts?: { notice?: string; tone?: "error" | "success" }) => {
    setLoading(true);
    void Promise.all([listAdminLandingPages(), getAdminLandingHome()]).then(([pages, home]) => {
      setLoading(false);
      if (!pages.ok) {
        setDenied(Boolean(pages.denied));
        setMessageTone("error");
        setMessage(pages.message);
        return;
      }
      setDenied(false);
      const nextHome = home.ok ? home.data.homePageId : null;
      const nextCanonical = home.ok ? home.data.usesCanonicalHome : nextHome == null;
      if (home.ok) {
        setHomePageId(nextHome);
        setUsesCanonicalHome(nextCanonical);
      }
      setRows(
        pages.data.map((page) => ({
          ...page,
          id: page.pageId,
          isHomeLabel: nextHome === page.pageId ? "بله" : "خیر",
          pageTypeLabel: page.pageType === "Home" ? "خانه" : "فرود",
          indexabilityLabel: page.robotsIndex ? "ایندکس" : "بدون ایندکس",
          pathLabel: publicPathForStorePage(page),
        })),
      );
      if (opts?.notice) {
        setMessageTone(opts.tone ?? "success");
        setMessage(opts.notice);
      } else {
        setMessage(undefined);
      }
      setReloadToken((value) => value + 1);
    });
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const publish = useCallback(async (row: LandingGridRow) => {
    setBusy(true);
    const next = row.status === "Published" ? "Draft" : "Published";
    const result = await setAdminLandingPageStatus(row.pageId, next);
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    refresh({ notice: next === "Published" ? "صفحه منتشر شد." : "صفحه به پیش‌نویس برگشت.", tone: "success" });
  }, [refresh]);

  const setHome = useCallback(async (row: LandingGridRow) => {
    if (row.status !== "Published") {
      setMessageTone("error");
      setMessage("برای «تنظیم به عنوان صفحه اصلی» ابتدا صفحه را منتشر کنید.");
      return;
    }
    const clearing = homePageId === row.pageId;
    setBusy(true);
    const result = await setAdminLandingHome(clearing ? null : row.pageId);
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    setHomePageId(result.data.homePageId);
    setUsesCanonicalHome(result.data.usesCanonicalHome);
    refresh({
      notice: result.data.usesCanonicalHome
        ? "خانهٔ پیش‌فرض فروشگاه بازگردانده شد. صفحه حذف نشد."
        : `«${row.title}» صفحه اصلی شد؛ آدرس در فهرست خالی است. صفحه حذف نشد.`,
      tone: "success",
    });
  }, [homePageId, refresh]);

  const restoreDefault = useCallback(async () => {
    if (!homePageId || usesCanonicalHome) {
      return;
    }
    if (!window.confirm(RESTORE_DEFAULT_HOME_CONFIRM)) {
      return;
    }
    setBusy(true);
    const result = await restoreDefaultAdminHome();
    setBusy(false);
    if (!result.ok) {
      setMessageTone("error");
      setMessage(result.message);
      return;
    }
    setHomePageId(result.data.homePageId);
    setUsesCanonicalHome(result.data.usesCanonicalHome);
    refresh({
      notice: "خانهٔ پیش‌فرض فروشگاه بازگردانده شد. صفحهٔ سفارشی حذف نشد و در فهرست باقی است.",
      tone: "success",
    });
  }, [homePageId, refresh, usesCanonicalHome]);

  const showRestoreDefaultHome = Boolean(homePageId) && !usesCanonicalHome;

  const rowActions = useMemo<AppGridRowAction<LandingGridRow>[]>(
    () => [
      {
        id: "edit",
        label: "ویرایش",
        icon: Pencil,
        href: (row) => `/admin/landing-pages/${row.pageId}`,
        testId: (row) => `landing-edit-${row.slug}`,
      },
      {
        id: "preview",
        label: "پیش‌نمایش",
        icon: Eye,
        href: (row) => `/admin/landing-pages/${row.pageId}/preview`,
        testId: (row) => `landing-preview-${row.slug}`,
      },
      {
        id: "publish",
        label: "انتشار",
        icon: Upload,
        visible: (row) => row.status === "Draft",
        disabled: () => busy,
        onClick: (row) => publish(row),
        testId: (row) => `landing-publish-${row.slug}`,
      },
      {
        id: "unpublish",
        label: "پیش‌نویس",
        icon: FilePenLine,
        visible: (row) => row.status === "Published",
        disabled: () => busy,
        onClick: (row) => publish(row),
        testId: (row) => `landing-unpublish-${row.slug}`,
      },
      {
        id: "home",
        label: "تنظیم به عنوان صفحه اصلی",
        icon: Home,
        disabled: () => busy,
        confirm: (row) =>
          homePageId === row.pageId
            ? "این صفحه از حالت خانه خارج شود و خانهٔ پیش‌فرض بازگردد؟ صفحه حذف نمی‌شود."
            : homePageId
              ? "صفحهٔ اصلی فعلی جایگزین شود؟ صفحهٔ قبلی حذف نمی‌شود و آدرس این صفحه در فهرست خالی می‌شود."
              : "این صفحه به‌عنوان صفحه اصلی فروشگاه تنظیم شود؟ آدرس صفحه در فهرست خالی می‌شود؛ صفحه حذف نمی‌شود.",
        onClick: (row) => setHome(row),
        testId: (row) => `landing-home-${row.slug}`,
      },
    ],
    [busy, homePageId, publish, setHome],
  );

  const columns = useMemo<GridColumnDef<LandingGridRow>[]>(
    () => [
      {
        id: "title",
        header: "عنوان",
        accessor: (row) => row.title,
        filterKind: "text",
        sortable: true,
        // عرض‌ها از نمای مرجع «اصلی» (سپس حذف‌شده) به‌عنوان پیش‌فرض کد.
        width: 233,
        minWidth: 140,
      },
      {
        id: "pageType",
        header: "نوع صفحه",
        accessor: (row) => row.pageTypeLabel,
        filterKind: "status",
        enumOptions: [
          { value: "خانه", label: "خانه" },
          { value: "فرود", label: "فرود" },
        ],
        width: 161,
        minWidth: 90,
      },
      {
        id: "slug",
        header: "آدرس صفحه",
        accessor: (row) => row.pathLabel,
        cell: (row) => <span dir="ltr">{row.pathLabel}</span>,
        filterKind: "text",
        sortable: true,
        width: 180,
        minWidth: 120,
      },
      {
        id: "locale",
        header: "زبان",
        accessor: (row) => row.locale,
        cell: (row) => (row.locale === "en" ? "انگلیسی" : "فارسی"),
        filterKind: "status",
        enumOptions: [
          { value: "fa", label: "فارسی" },
          { value: "en", label: "انگلیسی" },
        ],
        width: 100,
        minWidth: 80,
      },
      {
        id: "status",
        header: "وضعیت",
        accessor: (row) => row.status,
        cell: (row) => (
          <span
            className={`rounded-full px-2 py-0.5 text-xs font-bold ${
              row.status === "Published" ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"
            }`}
          >
            {row.status === "Published" ? "منتشرشده" : "پیش‌نویس"}
          </span>
        ),
        filterKind: "status",
        enumOptions: [
          { value: "Published", label: "منتشرشده" },
          { value: "Draft", label: "پیش‌نویس" },
        ],
        width: 130,
        minWidth: 90,
      },
      {
        id: "indexability",
        header: "ایندکس‌پذیری",
        accessor: (row) => row.indexabilityLabel,
        filterKind: "status",
        enumOptions: [
          { value: "ایندکس", label: "ایندکس" },
          { value: "بدون ایندکس", label: "بدون ایندکس" },
        ],
        width: 176,
        minWidth: 100,
      },
      {
        id: "isHome",
        header: "صفحه اصلی؟",
        accessor: (row) => row.isHomeLabel,
        cell: (row) =>
          row.isHomeLabel === "بله" ? (
            <span className="rounded-full bg-blue-50 px-2 py-0.5 text-xs font-bold text-blue-700" data-testid="home-current-indicator">
              خانه فعلی
            </span>
          ) : (
            "خیر"
          ),
        filterKind: "status",
        enumOptions: [
          { value: "بله", label: "بله" },
          { value: "خیر", label: "خیر" },
        ],
        width: 162,
        minWidth: 90,
      },
      {
        id: "updatedAt",
        header: "آخرین ویرایش",
        accessor: (row) => row.updatedAt,
        cell: (row) => formatJalaliDate(row.updatedAt, "fa"),
        filterKind: "date",
        sortable: true,
        width: 181,
        minWidth: 110,
      },
      {
        id: "actions",
        header: "عملیات",
        accessor: () => "",
        exportable: false,
        sortable: false,
        width: 168,
        minWidth: 148,
        cell: (row) => <AppGridRowActionsCell row={row} actions={rowActions} compact />,
      },
    ],
    [rowActions],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      return createClientGridQueryAdapter(rows, columns)(query);
    },
    [columns, reloadToken, rows],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: ADMIN_LANDING_PAGES_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  /** پیش‌فرض کد از نمای مرجع «اصلی»: sort=updatedAt desc، pageSize=20. */
  const landingPagesDefaultQuery = useMemo(() => ({ ...DEFAULT_GRID_QUERY }), []);

  if (denied) {
    return (
      <main className="rounded-2xl border border-border bg-surface-elevated p-8" data-testid="admin-landing-pages">
        <p>دسترسی به صفحات فروشگاه مجاز نیست.</p>
        <button type="button" className="mt-3 rounded-xl border px-4 py-2" onClick={refresh}>تلاش دوباره</button>
      </main>
    );
  }

  return (
    <main data-testid="admin-landing-pages" data-grid-profile="orders-canonical">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-black">صفحات فروشگاه</h1>
          <p className="mt-1 text-sm text-muted">
            مدیریت خانه و صفحات فرود با جدول استاندارد سفارش‌ها؛ ایجاد، سئو، انتشار و خانه.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {showRestoreDefaultHome ? (
            <button
              type="button"
              className="inline-flex items-center gap-2 rounded-xl border px-4 py-2 text-sm font-bold"
              disabled={busy}
              onClick={() => void restoreDefault()}
              data-testid="restore-default-home"
            >
              <RotateCcw className="h-4 w-4" />
              بازگردانی صفحه اصلی پیش‌فرض
            </button>
          ) : null}
          <Link
            href="/admin/landing-pages/new"
            className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
            data-testid="landing-create"
          >
            ایجاد صفحه
          </Link>
        </div>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        {loading ? (
          <p className="p-6 text-sm text-muted">در حال بارگذاری…</p>
        ) : rows.length === 0 ? (
          <div className="p-10 text-center" data-testid="landing-empty">
            <p className="font-bold">هنوز صفحه‌ای ساخته نشده است.</p>
            <p className="mt-2 text-sm text-muted">با «ایجاد صفحه» یک پیش‌نویس خانه یا فرود بسازید.</p>
          </div>
        ) : (
          <div className="overflow-x-auto p-2 md:p-4" data-testid="landing-pages-app-data-grid">
            <AppDataGrid<LandingGridRow>
              {...gridProps}
              defaultQuery={landingPagesDefaultQuery}
              capabilities={ORDERS_LIKE_CAPABILITIES}
              rowCountNoun={{ fa: "صفحه", en: "pages" }}
              messageOverrides={{
                advancedFilterTitle: "فیلتر پیشرفته صفحات فروشگاه",
                advancedFilterSubtitle: "جستجوی دقیق مانند فهرست سفارش‌ها",
              }}
            />
          </div>
        )}
        {message ? (
          <p
            className={`px-4 py-3 text-sm ${messageTone === "success" ? "text-emerald-700" : "text-red-600"}`}
            data-testid="landing-pages-notice"
          >
            {message}
          </p>
        ) : null}
      </section>
    </main>
  );
}
