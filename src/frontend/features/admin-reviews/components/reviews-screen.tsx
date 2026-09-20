"use client";

import { useCallback, useMemo, useState } from "react";
import { CheckCircle, Star } from "lucide-react";
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
import { createHostSavedViewStore, ADMIN_REVIEW_GRID_VIEW_KEY } from "../../../app/admin/saved-view-store.ts";
import {
  moderateAdminReview,
  queryAdminReviewsGrid,
  type AdminReviewRow,
} from "../api/reviews-api.ts";

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
  reloadToken = 0,
}: {
  title: string;
  description: string;
  gridId: string;
  columns: GridColumnDef<T>[];
  queryFn: (query: GridServerQuery) => Promise<AdminGridQueryResult<T>>;
  savedViewStore?: SavedViewStore;
  testId?: string;
  reloadToken?: number;
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
      void reloadToken;
      void localReloadToken;
      return adminGridQueryAdapter(queryFn, () => setDenied(true), (message) => setGridError(message))(query);
    },
    [queryFn, reloadToken, localReloadToken],
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

const reviewColumns = (moderate: (id: string, action: "publish" | "reject") => void): GridColumnDef<AdminReviewRow>[] => [
  { id: "reviewer", header: "نویسنده", accessor: (row) => row.reviewerDisplayName, cell: (row) => <strong>{row.reviewerDisplayName}</strong>, width: 150, minWidth: 110, maxWidth: 210, sticky: "start" },
  { id: "product", header: "محصول", accessor: (row) => row.productTitle, width: 180, minWidth: 130, maxWidth: 260 },
  { id: "rating", header: "امتیاز", accessor: (row) => row.rating, cell: (row) => <span className="inline-flex items-center gap-1"><Star className="size-4 fill-amber-400 text-amber-400" />{row.rating.toLocaleString("fa-IR")}</span>, width: 90, minWidth: 75, maxWidth: 110 },
  { id: "excerpt", header: "نظر", accessor: (row) => row.excerpt, width: 260, minWidth: 180, maxWidth: 360 },
  { id: "verified", header: "خرید تأییدشده", accessor: (row) => row.verifiedPurchase ? "بله" : "خیر", cell: (row) => row.verifiedPurchase ? <CheckCircle className="size-4 text-emerald-600" aria-label="بله" /> : "—", width: 120, minWidth: 100, maxWidth: 150 },
  { id: "status", header: "وضعیت", accessor: (row) => row.status, cell: (row) => <Status value={row.status} />, width: 110, minWidth: 90, maxWidth: 150 },
  { id: "created", header: "تاریخ", accessor: (row) => row.createdAt, cell: (row) => formatAdminDate(row.createdAt), width: 110, minWidth: 95, maxWidth: 150 },
  { id: "actions", header: "عملیات", accessor: () => "", cell: (row) => <span className="flex gap-2"><button onClick={() => moderate(row.id, "publish")} className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs text-white" data-testid={`admin-review-publish-${row.id}`}>انتشار</button><button onClick={() => moderate(row.id, "reject")} className="rounded-lg bg-red-600 px-3 py-1.5 text-xs text-white" data-testid={`admin-review-reject-${row.id}`}>رد</button></span>, width: 160, minWidth: 145, maxWidth: 190 },
];

/** حداقل سطح تعدیل نظر با AppDataGrid canonical و server GridQuery. */
export function AdminReviewsScreen() {
  const [reloadToken, setReloadToken] = useState(0);
  const moderate = useCallback(
    (id: string, action: "publish" | "reject") =>
      void moderateAdminReview(id, action).then((result) => {
        if (result.state === "ok") setReloadToken((value) => value + 1);
      }),
    [],
  );
  const columns = useMemo(() => reviewColumns(moderate), [moderate]);
  const queryFn = useCallback(
    (query: GridServerQuery) => {
      void reloadToken;
      return queryAdminReviewsGrid(query);
    },
    [reloadToken],
  );
  return (
    <ServerGridPage
      title="مدیریت نظرات"
      description="بررسی نظرهای در انتظار انتشار"
      queryFn={queryFn}
      columns={columns}
      gridId={ADMIN_REVIEW_GRID_VIEW_KEY}
      testId="admin-reviews"
      reloadToken={reloadToken}
    />
  );
}
