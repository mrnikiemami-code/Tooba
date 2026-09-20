"use client";

import { useCallback, useMemo, useState } from "react";
import {
  AppDataGrid,
  ErrorState,
  faWorkspaceMessages,
  adminGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../../design-system";
import type { GridColumnDef, GridServerQuery, SavedViewStore } from "../../../design-system/data-grid";
import type { AdminGridQueryResult } from "../../../design-system/app-data-grid/admin-grid-query-client.ts";
import { formatAdminDate, formatAdminStatus } from "../../../app/admin/admin-api.ts";
import { createHostSavedViewStore, ADMIN_CUSTOMER_GRID_VIEW_KEY } from "../../../app/admin/saved-view-store.ts";
import { queryAdminCustomersGrid, type AdminCustomerRow } from "../api/customers-api.ts";

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

function Status({ value }: { value: string }) {
  return <span className="inline-flex rounded-full bg-secondary px-2.5 py-1 text-xs font-medium">{formatAdminStatus(value)}</span>;
}

function ServerGridPage<T extends { id: string }>({
  title,
  description,
  gridId,
  columns,
  queryFn,
  savedViewStore: savedViewStoreInput,
  testId,
}: {
  title: string;
  description: string;
  gridId: string;
  columns: GridColumnDef<T>[];
  queryFn: (query: GridServerQuery) => Promise<AdminGridQueryResult<T>>;
  savedViewStore?: SavedViewStore;
  testId?: string;
}) {
  const savedViewStore = useMemo(
    () => savedViewStoreInput ?? createHostSavedViewStore(gridId),
    [gridId, savedViewStoreInput],
  );
  const [denied, setDenied] = useState(false);
  const [gridError, setGridError] = useState<string>();
  const [localReloadToken, setLocalReloadToken] = useState(0);
  const refresh = () => setLocalReloadToken((value) => value + 1);
  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void localReloadToken;
      return adminGridQueryAdapter(queryFn, () => setDenied(true), (message) => setGridError(message))(query);
    },
    [queryFn, localReloadToken],
  );
  const gridProps = useLegacyAdminGridDirectProps({ gridId, columns, queryAdapter, savedViewStore });
  if (denied) return <Denied retry={refresh} />;
  return (
    <main data-testid={testId}>
      <PageHeading title={title} description={description} />
      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">دادهٔ زندهٔ فروشگاه — server GridQuery</span>
        </div>
        <div className="p-2 md:p-4">
          {gridError ? (
            <ErrorState title="فروشگاه در دسترس نیست" detail={gridError} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : (
            <AppDataGrid<T> {...gridProps} />
          )}
        </div>
      </section>
    </main>
  );
}

const customerColumns: GridColumnDef<AdminCustomerRow>[] = [
  { id: "name", header: "مشتری", accessor: (row) => row.displayName, cell: (row) => <strong>{row.displayName}</strong>, width: 220, minWidth: 150, maxWidth: 300, sticky: "start", filterKind: "text", sortable: true },
  { id: "contact", header: "راه ارتباطی", accessor: (row) => row.contact, width: 160, minWidth: 120, maxWidth: 220, filterKind: "text" },
  { id: "orders", header: "تعداد سفارش", accessor: (row) => row.orderCount, cell: (row) => row.orderCount.toLocaleString("fa-IR"), width: 120, minWidth: 95, maxWidth: 150, sortable: true },
  { id: "activity", header: "آخرین فعالیت", accessor: (row) => row.lastActivityAt ?? "", cell: (row) => formatAdminDate(row.lastActivityAt), width: 130, minWidth: 105, maxWidth: 170, sortable: true },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <Status value={row.status} />, width: 110, minWidth: 90, maxWidth: 150, filterKind: "status" },
];

/** فهرست صادقانهٔ خریداران شناخته‌شده؛ نه CRM. */
export function AdminCustomersScreen() {
  return (
    <ServerGridPage
      title="مشتریان"
      description="خریداران شناخته‌شده از سفارش‌های زنده"
      queryFn={queryAdminCustomersGrid}
      columns={customerColumns}
      gridId={ADMIN_CUSTOMER_GRID_VIEW_KEY}
      testId="admin-customers"
    />
  );
}
