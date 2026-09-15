"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Eye, Home, Pencil, Upload } from "lucide-react";
import {
  AppDataGrid,
  createClientGridQueryAdapter,
  formatJalaliDate,
  useLegacyAdminGridDirectProps,
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
  setAdminLandingHome,
  setAdminLandingPageStatus,
  type AdminLandingPage,
} from "./admin-landing-pages-api.ts";

type LandingGridRow = AdminLandingPage & { id: string; isHomeLabel: string };

const ORDERS_LIKE_CAPABILITIES = {
  ...DEFAULT_APP_GRID_CAPABILITIES,
  csvExport: false,
  excelExport: false,
};

export function AdminLandingPagesScreen() {
  const [rows, setRows] = useState<LandingGridRow[]>([]);
  const [homePageId, setHomePageId] = useState<string | null>(null);
  const [message, setMessage] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [denied, setDenied] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_LANDING_PAGES_GRID_VIEW_KEY), []);

  const refresh = useCallback(() => {
    setLoading(true);
    void Promise.all([listAdminLandingPages(), getAdminLandingHome()]).then(([pages, home]) => {
      setLoading(false);
      if (!pages.ok) {
        setDenied(Boolean(pages.denied));
        setMessage(pages.message);
        return;
      }
      setDenied(false);
      const nextHome = home.ok ? home.data.homePageId : null;
      if (home.ok) setHomePageId(nextHome);
      setRows(
        pages.data.map((page) => ({
          ...page,
          id: page.pageId,
          isHomeLabel: nextHome === page.pageId ? "بله" : "خیر",
        })),
      );
      setMessage(undefined);
      setReloadToken((value) => value + 1);
    });
  }, []);

  useEffect(refresh, [refresh]);

  const publish = useCallback(async (row: LandingGridRow) => {
    setBusy(true);
    const next = row.status === "Published" ? "Draft" : "Published";
    const result = await setAdminLandingPageStatus(row.pageId, next);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    refresh();
  }, [refresh]);

  const setHome = useCallback(async (row: LandingGridRow) => {
    if (row.status !== "Published") {
      setMessage("برای انتخاب به‌عنوان صفحهٔ اصلی ابتدا صفحه را منتشر کنید.");
      return;
    }
    setBusy(true);
    const result = await setAdminLandingHome(homePageId === row.pageId ? null : row.pageId);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setHomePageId(result.data.homePageId);
    refresh();
  }, [homePageId, refresh]);

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
        label: "انتشار / پیش‌نویس",
        icon: Upload,
        disabled: () => busy,
        onClick: (row) => publish(row),
        testId: (row) => `landing-publish-${row.slug}`,
      },
      {
        id: "home",
        label: "صفحه اصلی",
        icon: Home,
        disabled: () => busy,
        confirm: (row) =>
          homePageId && homePageId !== row.pageId ? "صفحهٔ اصلی فعلی جایگزین شود؟" : false,
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
        width: 220,
        minWidth: 140,
      },
      {
        id: "slug",
        header: "آدرس صفحه",
        accessor: (row) => row.slug,
        cell: (row) => <span dir="ltr">/{row.slug}</span>,
        filterKind: "text",
        sortable: true,
        width: 160,
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
        width: 110,
        minWidth: 90,
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
        width: 120,
        minWidth: 100,
      },
      {
        id: "isHome",
        header: "صفحه اصلی؟",
        accessor: (row) => row.isHomeLabel,
        filterKind: "status",
        enumOptions: [
          { value: "بله", label: "بله" },
          { value: "خیر", label: "خیر" },
        ],
        width: 110,
        minWidth: 90,
      },
      {
        id: "updatedAt",
        header: "آخرین ویرایش",
        accessor: (row) => row.updatedAt,
        cell: (row) => formatJalaliDate(row.updatedAt, "fa"),
        filterKind: "date",
        sortable: true,
        width: 140,
        minWidth: 120,
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

  if (denied) {
    return (
      <main className="rounded-2xl border border-border bg-surface-elevated p-8" data-testid="admin-landing-pages">
        <p>دسترسی به صفحات فرود مجاز نیست.</p>
        <button type="button" className="mt-3 rounded-xl border px-4 py-2" onClick={refresh}>تلاش دوباره</button>
      </main>
    );
  }

  return (
    <main data-testid="admin-landing-pages" data-grid-profile="orders-canonical">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-black">صفحات فرود</h1>
          <p className="mt-1 text-sm text-muted">
            فهرست صفحات با همان استاندارد جدول سفارش‌ها؛ ایجاد، پیش‌نمایش، انتشار و خانه.
          </p>
        </div>
        <Link
          href="/admin/landing-pages/new"
          className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
          data-testid="landing-create"
        >
          ایجاد صفحه
        </Link>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        {loading ? (
          <p className="p-6 text-sm text-muted">در حال بارگذاری…</p>
        ) : rows.length === 0 ? (
          <div className="p-10 text-center" data-testid="landing-empty">
            <p className="font-bold">هنوز صفحه‌ای ساخته نشده است.</p>
            <p className="mt-2 text-sm text-muted">با «ایجاد صفحه» یک پیش‌نویس بسازید و بخش‌ها را اضافه کنید.</p>
          </div>
        ) : (
          <div className="overflow-x-auto p-2 md:p-4" data-testid="landing-pages-app-data-grid">
            <AppDataGrid<LandingGridRow>
              {...gridProps}
              capabilities={ORDERS_LIKE_CAPABILITIES}
              rowCountNoun={{ fa: "صفحه", en: "pages" }}
              messageOverrides={{
                advancedFilterTitle: "فیلتر پیشرفته صفحات فرود",
                advancedFilterSubtitle: "جستجوی دقیق مانند فهرست سفارش‌ها",
              }}
            />
          </div>
        )}
        {message ? <p className="px-4 py-3 text-sm text-red-600">{message}</p> : null}
      </section>
    </main>
  );
}
