"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Archive, Pencil, Upload } from "lucide-react";
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
import { ADMIN_CAMPAIGNS_GRID_VIEW_KEY, createHostSavedViewStore } from "../saved-view-store";
import {
  archiveAdminCampaign,
  campaignLifecycleFa,
  campaignRuntimeFa,
  listAdminCampaignTypes,
  listAdminCampaigns,
  publishAdminCampaign,
  type AdminCampaignListItem,
  type AdminCampaignTypeOption,
} from "./admin-campaigns-api.ts";

type CampaignGridRow = AdminCampaignListItem & {
  id: string;
  runtimeFa: string;
  lifecycleFa: string;
};

const ORDERS_LIKE_CAPABILITIES = {
  ...DEFAULT_APP_GRID_CAPABILITIES,
  csvExport: false,
  excelExport: false,
};

function runtimeBadgeClass(key: string): string {
  switch (key) {
    case "active":
      return "bg-emerald-50 text-emerald-700";
    case "scheduled":
      return "bg-blue-50 text-blue-700";
    case "expired":
      return "bg-slate-100 text-slate-600";
    case "archived":
      return "bg-amber-50 text-amber-800";
    default:
      return "bg-amber-50 text-amber-700";
  }
}

export function AdminCampaignsScreen() {
  const [rows, setRows] = useState<CampaignGridRow[]>([]);
  const [types, setTypes] = useState<AdminCampaignTypeOption[]>([]);
  const [message, setMessage] = useState<string>();
  const [messageTone, setMessageTone] = useState<"error" | "success">("error");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [denied, setDenied] = useState(false);
  const [reloadToken, setReloadToken] = useState(0);
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [lifecycle, setLifecycle] = useState("");
  const [promotionTypeId, setPromotionTypeId] = useState("");
  const [runtimeWindow, setRuntimeWindow] = useState("");
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_CAMPAIGNS_GRID_VIEW_KEY), []);

  useEffect(() => {
    const timer = window.setTimeout(() => setAppliedSearch(search.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  const refresh = useCallback((opts?: { notice?: string; tone?: "error" | "success" }) => {
    setLoading(true);
    void Promise.all([
      listAdminCampaigns({
        search: appliedSearch || undefined,
        lifecycle: lifecycle || undefined,
        promotionTypeId: promotionTypeId || undefined,
        runtimeWindow: runtimeWindow || undefined,
        skip: 0,
        take: 100,
        locale: "fa-IR",
      }),
      listAdminCampaignTypes("fa-IR"),
    ]).then(([list, typeResult]) => {
      setLoading(false);
      if (!list.ok) {
        setDenied(Boolean(list.denied));
        setMessageTone("error");
        setMessage(list.message);
        return;
      }
      setDenied(false);
      if (typeResult.ok) setTypes(typeResult.data);
      setRows(
        list.data.items.map((item) => ({
          ...item,
          id: item.campaignId,
          runtimeFa: campaignRuntimeFa(item.runtimeLabel),
          lifecycleFa: campaignLifecycleFa(item.lifecycleStatus),
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
  }, [appliedSearch, lifecycle, promotionTypeId, runtimeWindow]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const publish = useCallback(
    async (row: CampaignGridRow) => {
      setBusy(true);
      const result = await publishAdminCampaign(row.campaignId);
      setBusy(false);
      if (!result.ok) {
        setMessageTone("error");
        setMessage(result.message);
        return;
      }
      refresh({ notice: `«${row.title}» منتشر شد.`, tone: "success" });
    },
    [refresh],
  );

  const archive = useCallback(
    async (row: CampaignGridRow) => {
      setBusy(true);
      const result = await archiveAdminCampaign(row.campaignId);
      setBusy(false);
      if (!result.ok) {
        setMessageTone("error");
        setMessage(result.message);
        return;
      }
      refresh({ notice: `«${row.title}» بایگانی شد.`, tone: "success" });
    },
    [refresh],
  );

  const rowActions = useMemo<AppGridRowAction<CampaignGridRow>[]>(
    () => [
      {
        id: "edit",
        label: "ویرایش",
        icon: Pencil,
        href: (row) => `/admin/campaigns/${row.campaignId}`,
        testId: (row) => `campaign-edit-${row.campaignId}`,
      },
      {
        id: "publish",
        label: "انتشار",
        icon: Upload,
        visible: (row) => row.lifecycleStatus === "Draft",
        disabled: () => busy,
        onClick: (row) => void publish(row),
        testId: (row) => `campaign-publish-${row.campaignId}`,
      },
      {
        id: "archive",
        label: "بایگانی",
        icon: Archive,
        visible: (row) => row.lifecycleStatus !== "Archived",
        disabled: () => busy,
        confirm: (row) => `کمپین «${row.title}» بایگانی شود؟`,
        onClick: (row) => void archive(row),
        testId: (row) => `campaign-archive-${row.campaignId}`,
      },
    ],
    [archive, busy, publish],
  );

  const columns = useMemo<GridColumnDef<CampaignGridRow>[]>(
    () => [
      {
        id: "title",
        header: "عنوان",
        accessor: (row) => row.title,
        filterKind: "text",
        sortable: true,
        width: 220,
        minWidth: 140,
        maxWidth: 360,
      },
      {
        id: "promotionType",
        header: "نوع کمپین",
        accessor: (row) => row.promotionTypeDisplayName,
        filterKind: "text",
        width: 170,
        minWidth: 120,
        maxWidth: 260,
      },
      {
        id: "runtime",
        header: "وضعیت",
        accessor: (row) => row.runtimeFa,
        cell: (row) => (
          <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${runtimeBadgeClass(row.runtimeLabel)}`}>
            {row.runtimeFa}
          </span>
        ),
        filterKind: "status",
        enumOptions: [
          { value: "پیش‌نویس", label: "پیش‌نویس" },
          { value: "زمان‌بندی‌شده", label: "زمان‌بندی‌شده" },
          { value: "فعال", label: "فعال" },
          { value: "منقضی", label: "منقضی" },
          { value: "بایگانی‌شده", label: "بایگانی‌شده" },
        ],
        width: 140,
        minWidth: 100,
        maxWidth: 180,
      },
      {
        id: "startAt",
        header: "شروع",
        accessor: (row) => row.startAt,
        cell: (row) => formatJalaliDate(row.startAt, "fa"),
        filterKind: "date",
        sortable: true,
        width: 130,
        minWidth: 100,
        maxWidth: 180,
      },
      {
        id: "endAt",
        header: "پایان",
        accessor: (row) => row.endAt ?? "",
        cell: (row) => (row.endAt ? formatJalaliDate(row.endAt, "fa") : "—"),
        filterKind: "date",
        sortable: true,
        width: 130,
        minWidth: 100,
        maxWidth: 180,
      },
      {
        id: "priority",
        header: "اولویت",
        accessor: (row) => row.priority,
        cell: (row) => row.priority.toLocaleString("fa-IR"),
        filterKind: "number",
        sortable: true,
        width: 100,
        minWidth: 80,
        maxWidth: 140,
      },
      {
        id: "memberCount",
        header: "تعداد کالا",
        accessor: (row) => row.memberCount,
        cell: (row) => row.memberCount.toLocaleString("fa-IR"),
        filterKind: "number",
        sortable: true,
        width: 110,
        minWidth: 90,
        maxWidth: 160,
      },
      {
        id: "updatedAt",
        header: "آخرین تغییر",
        accessor: (row) => row.updatedAt,
        cell: (row) => formatJalaliDate(row.updatedAt, "fa"),
        filterKind: "date",
        sortable: true,
        width: 150,
        minWidth: 110,
        maxWidth: 200,
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
    gridId: ADMIN_CAMPAIGNS_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  const defaultQuery = useMemo(() => ({ ...DEFAULT_GRID_QUERY }), []);

  if (denied) {
    return (
      <main className="rounded-2xl border border-border bg-surface-elevated p-8" data-testid="admin-campaigns">
        <p>دسترسی به کمپین‌های فروش مجاز نیست.</p>
        <button type="button" className="mt-3 rounded-xl border px-4 py-2" onClick={() => refresh()}>
          تلاش دوباره
        </button>
      </main>
    );
  }

  return (
    <main data-testid="admin-campaigns" data-grid-profile="orders-canonical">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-black">کمپین‌های فروش</h1>
          <p className="mt-1 text-sm text-muted">
            مدیریت پیشنهاد شگفت‌انگیز و کمپین‌های مرچندایزینگ؛ ایجاد، انتشار و بایگانی.
          </p>
        </div>
        <Link
          href="/admin/campaigns/new"
          className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
          data-testid="campaign-create"
        >
          کمپین جدید
        </Link>
      </div>

      <section className="mb-4 grid gap-3 rounded-2xl border border-border bg-surface-elevated p-4 md:grid-cols-4">
        <label className="block text-sm">
          <span className="mb-1 block font-bold text-muted">جستجو</span>
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full rounded-xl border border-border bg-white px-3 py-2"
            placeholder="عنوان کمپین…"
            data-testid="campaign-filter-search"
          />
        </label>
        <label className="block text-sm">
          <span className="mb-1 block font-bold text-muted">چرخه عمر</span>
          <select
            value={lifecycle}
            onChange={(e) => setLifecycle(e.target.value)}
            className="w-full rounded-xl border border-border bg-white px-3 py-2"
            data-testid="campaign-filter-lifecycle"
          >
            <option value="">همه</option>
            <option value="Draft">پیش‌نویس</option>
            <option value="Published">منتشرشده</option>
            <option value="Archived">بایگانی‌شده</option>
          </select>
        </label>
        <label className="block text-sm">
          <span className="mb-1 block font-bold text-muted">نوع کمپین</span>
          <select
            value={promotionTypeId}
            onChange={(e) => setPromotionTypeId(e.target.value)}
            className="w-full rounded-xl border border-border bg-white px-3 py-2"
            data-testid="campaign-filter-type"
          >
            <option value="">همه</option>
            {types.map((type) => (
              <option key={type.promotionTypeId} value={type.promotionTypeId}>
                {type.displayName}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm">
          <span className="mb-1 block font-bold text-muted">پنجره زمانی</span>
          <select
            value={runtimeWindow}
            onChange={(e) => setRuntimeWindow(e.target.value)}
            className="w-full rounded-xl border border-border bg-white px-3 py-2"
            data-testid="campaign-filter-runtime"
          >
            <option value="">همه</option>
            <option value="active">فعال الان</option>
            <option value="future">آینده</option>
            <option value="expired">منقضی</option>
          </select>
        </label>
      </section>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        {loading ? (
          <p className="p-6 text-sm text-muted">در حال بارگذاری…</p>
        ) : rows.length === 0 ? (
          <div className="p-10 text-center" data-testid="campaigns-empty">
            <p className="font-bold">هنوز کمپینی ساخته نشده است.</p>
            <p className="mt-2 text-sm text-muted">با «کمپین جدید» یک پیشنهاد شگفت‌انگیز بسازید.</p>
          </div>
        ) : (
          <div className="overflow-x-auto p-2 md:p-4" data-testid="campaigns-app-data-grid">
            <AppDataGrid<CampaignGridRow>
              {...gridProps}
              defaultQuery={defaultQuery}
              capabilities={ORDERS_LIKE_CAPABILITIES}
              rowCountNoun={{ fa: "کمپین", en: "campaigns" }}
              messageOverrides={{
                advancedFilterTitle: "فیلتر پیشرفته کمپین‌ها",
                advancedFilterSubtitle: "جستجوی دقیق مانند فهرست سفارش‌ها",
              }}
            />
          </div>
        )}
        {message ? (
          <p
            className={`px-4 py-3 text-sm ${messageTone === "success" ? "text-emerald-700" : "text-red-600"}`}
            data-testid="campaigns-notice"
          >
            {message}
          </p>
        ) : null}
      </section>
    </main>
  );
}
