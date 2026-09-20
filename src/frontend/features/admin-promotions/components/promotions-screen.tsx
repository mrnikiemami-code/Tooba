"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  AppDataGrid,
  ErrorState,
  faWorkspaceMessages,
  createClientGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../../design-system";
import type { GridColumnDef, GridServerQuery } from "../../../design-system/data-grid";
import type { AdminLoadState, AdminResult } from "../../../lib/admin/admin-result.ts";
import { createHostSavedViewStore, ADMIN_PROMOTION_GRID_VIEW_KEY } from "../../../app/admin/saved-view-store.ts";
import { formatAdminDate, formatAdminMoney, formatAdminStatus } from "../../../app/admin/admin-api.ts";
import {
  deactivateAdminPromotion,
  loadAdminPromotions,
  type AdminPromotionRow,
} from "../api/promotions-api.ts";

function Status({ value }: { value: string }) {
  return <span className="inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium">{formatAdminStatus(value)}</span>;
}

function Denied({ retry }: { retry: () => void }) {
  return (
    <div data-testid="admin-auth-denied">
      <ErrorState
        title="دسترسی مجاز نیست"
        detail="سامانه هویت فعلی را مدیر تشخیص نداد. تغییر مسیر یا هدر مرورگر مجوز ایجاد نمی‌کند."
        onRetry={retry}
        retryLabel={faWorkspaceMessages.retry}
      />
    </div>
  );
}

function PageHeading({ title, description }: { title: string; description: string }) {
  return (
    <div className="mb-5">
      <p className="text-sm text-muted">خانه / {title}</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
      <p className="mt-1 text-base text-muted">{description}</p>
    </div>
  );
}

function ClientGridPage<T extends { id: string }>({
  title,
  description,
  loader,
  columns,
  gridId,
  boundedReason,
}: {
  title: string;
  description: string;
  loader: () => Promise<AdminResult<T[]>>;
  columns: GridColumnDef<T>[];
  gridId: string;
  boundedReason?: string;
}) {
  const savedViewStore = useMemo(() => createHostSavedViewStore(gridId), [gridId]);
  const [state, setState] = useState<AdminLoadState | "loading">("loading");
  const [rows, setRows] = useState<T[]>([]);
  const [message, setMessage] = useState<string>();
  const refresh = () =>
    void loader().then((result) => {
      setState(result.state);
      setRows(result.data ?? []);
      setMessage(result.message);
    });
  useEffect(refresh, [loader]);
  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(rows, columns)(query),
    [rows, columns],
  );
  const gridProps = useLegacyAdminGridDirectProps({ gridId, columns, queryAdapter, savedViewStore });
  if (state === "denied") return <Denied retry={refresh} />;
  return (
    <main data-testid="admin-promotions">
      <PageHeading title={title} description={description} />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">
            {boundedReason ??
              (state === "ok"
                ? "فهرست bounded — client GridQuery"
                : state === "loading"
                  ? "در حال بارگذاری"
                  : "اتصال برقرار نیست")}
          </span>
          <span className="rounded-full bg-secondary px-3 py-1 text-xs">{rows.length.toLocaleString("fa-IR")} مورد</span>
        </div>
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : state === "loading" ? (
            <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <AppDataGrid<T> {...gridProps} />
          )}
        </div>
      </section>
    </main>
  );
}

const promotionColumns = (deactivate: (id: string) => void): GridColumnDef<AdminPromotionRow>[] => [
  {
    id: "code",
    header: "کد",
    accessor: (row) => row.couponCode ?? "",
    cell: (row) => <strong className="font-mono" dir="ltr">{row.couponCode ?? "—"}</strong>,
    width: 140,
    minWidth: 110,
    maxWidth: 200,
    sticky: "start",
  },
  { id: "name", header: "نام", accessor: (row) => row.name, width: 180, minWidth: 130, maxWidth: 260 },
  {
    id: "discount",
    header: "تخفیف",
    accessor: (row) =>
      row.discountKind === "FixedAmountOff" ? row.fixedAmount : Math.round(row.percentageRate * 100),
    cell: (row) =>
      row.discountKind === "FixedAmountOff"
        ? formatAdminMoney(row.fixedAmount, "IRR")
        : `${Math.round(row.percentageRate * 100).toLocaleString("fa-IR")}٪`,
    width: 120,
    minWidth: 100,
    maxWidth: 160,
  },
  {
    id: "seller",
    header: "فروشنده",
    accessor: (row) => row.sellerPartyId ?? "",
    cell: (row) => (
      <span className="font-mono text-xs" dir="ltr">
        {row.sellerPartyId ? `${row.sellerPartyId.slice(0, 8)}…` : "—"}
      </span>
    ),
    width: 130,
    minWidth: 110,
    maxWidth: 180,
  },
  {
    id: "status",
    header: "وضعیت",
    accessor: (row) => row.status,
    cell: (row) => <Status value={row.status} />,
    width: 110,
    minWidth: 90,
    maxWidth: 150,
  },
  {
    id: "expires",
    header: "انقضا",
    accessor: (row) => row.effectiveTo ?? "",
    cell: (row) => (row.effectiveTo ? formatAdminDate(row.effectiveTo) : "باز"),
    width: 120,
    minWidth: 100,
    maxWidth: 150,
  },
  {
    id: "actions",
    header: "عملیات",
    accessor: () => "",
    cell: (row) =>
      row.status === "Active" ? (
        <button
          type="button"
          onClick={() => deactivate(row.promotionId)}
          className="rounded-lg bg-red-600 px-3 py-1.5 text-xs text-white"
          data-testid={`admin-promotion-deactivate-${row.promotionId}`}
        >
          غیرفعال
        </button>
      ) : (
        "—"
      ),
    width: 120,
    minWidth: 100,
    maxWidth: 150,
  },
];

/** نظارت ادمین بر پروموشن/کوپن فروشندگان — bounded client grid. */
export function AdminPromotionsScreen() {
  const [reloadToken, setReloadToken] = useState(0);
  const loader = useCallback(async () => {
    void reloadToken;
    return loadAdminPromotions();
  }, [reloadToken]);
  const deactivate = useCallback(
    (id: string) =>
      void deactivateAdminPromotion(id).then((result) => {
        if (result.state === "ok") setReloadToken((value) => value + 1);
      }),
    [],
  );
  const columns = useMemo(() => promotionColumns(deactivate), [deactivate]);
  return (
    <ClientGridPage
      title="نظارت پروموشن‌ها"
      description="فهرست و غیرفعال‌سازی نظارتی کدهای تخفیف فروشندگان"
      loader={loader}
      columns={columns}
      gridId={ADMIN_PROMOTION_GRID_VIEW_KEY}
      boundedReason="SMALL_BOUNDED_CLIENT_SAFE — bounded by active seller promotions"
    />
  );
}
