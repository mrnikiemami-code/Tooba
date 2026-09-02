"use client";

import { useCallback, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, Pencil, UserX } from "lucide-react";
import type { ColDef, ICellRendererParams } from "ag-grid-community";
import {
  AppDataGrid,
  ErrorState,
  faWorkspaceMessages,
  formatJalaliDate,
} from "../../design-system";
import {
  applyAppGridFilterHeader,
  appGridExternalFilterFields,
  type AppGridFilterSpec,
} from "../../design-system/app-data-grid/app-grid-filter-header.ts";
import type { AppGridFilterColumnDef } from "../../design-system/app-data-grid/filter-column-def";
import { AppGridBadgeCell, AppGridTruncatedCell } from "../../design-system/app-data-grid/app-grid-cells";
import { buildPinnedActionsColumnDef } from "../../design-system/app-data-grid/app-grid-pinned-actions";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../design-system/app-data-grid/app-grid-row-actions";
import type { GridServerQuery } from "../../design-system/data-grid";
import { toast } from "react-toastify";
import {
  createContentAuthor,
  deactivateContentAuthor,
  mapContentAuthorMutationError,
  queryAdminContentAuthorsGrid,
  slugifyContentAuthorName,
  type ContentAuthorGridRow,
} from "./content-author-api.ts";
import { mediaPreviewUrl } from "./media-api.ts";
import { ADMIN_CONTENT_AUTHORS_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";

const AUTHORS_GRID_FILTER_MATRIX: Record<string, AppGridFilterSpec> = {
  displayName: { field: "displayName", kind: "text" },
  slug: { field: "slug", kind: "text" },
  isActive: { field: "isActive", kind: "status" },
  articleCount: { field: "articleCount", kind: "number" },
  updatedAt: { field: "updatedAt", kind: "jalali-date" },
};

const AUTHORS_EXTERNAL_FILTER_FIELDS = appGridExternalFilterFields(AUTHORS_GRID_FILTER_MATRIX);

function applyAuthorsGridFilterHeader<T>(colDef: ColDef<T>): ColDef<T> {
  const field = String(colDef.field ?? colDef.colId ?? "");
  return applyAppGridFilterHeader(colDef, AUTHORS_GRID_FILTER_MATRIX[field] ?? { field, kind: "none" });
}

function authorStatusLabel(isActive: boolean): string {
  return isActive ? "فعال" : "غیرفعال";
}

function authorStatusClass(isActive: boolean): string {
  return isActive
    ? "inline-flex rounded-full bg-success/15 px-2.5 py-1 text-xs font-medium text-success"
    : "inline-flex rounded-full bg-muted/20 px-2.5 py-1 text-xs font-medium text-muted";
}

function AvatarCell(params: ICellRendererParams<ContentAuthorGridRow>) {
  const row = params.data;
  if (!row) return null;
  const url = mediaPreviewUrl(row.profileImageMediaAssetId);
  return (
    <div className="flex items-center gap-2">
      {url ? (
        <img src={url} alt="" className="h-8 w-8 rounded-full border object-cover" />
      ) : (
        <span className="flex h-8 w-8 items-center justify-center rounded-full bg-slate-100 text-xs font-bold text-slate-500">
          {row.displayName.trim().charAt(0) || "?"}
        </span>
      )}
      <AppGridTruncatedCell params={params} text={row.displayName} className="font-semibold" />
    </div>
  );
}

function SlugCell(params: ICellRendererParams<ContentAuthorGridRow>) {
  const row = params.data;
  if (!row) return null;
  return <span dir="ltr" className="font-mono text-xs text-muted">{row.slug}</span>;
}

function ActiveCell(params: ICellRendererParams<ContentAuthorGridRow>) {
  const active = Boolean(params.data?.isActive);
  return <AppGridBadgeCell params={params} label={authorStatusLabel(active)} className={authorStatusClass(active)} />;
}

function buildAuthorRowActions(
  onDeactivate: (authorId: string, displayName: string) => Promise<void>,
): AppGridRowAction<ContentAuthorGridRow>[] {
  return [
    {
      id: "view",
      label: "مشاهده",
      icon: Eye,
      href: (row) => `/admin/content/authors/${row.authorId}`,
      testId: (row) => `admin-content-author-view-${row.authorId}`,
    },
    {
      id: "edit",
      label: "ویرایش",
      icon: Pencil,
      href: (row) => `/admin/content/authors/${row.authorId}`,
      testId: (row) => `admin-content-author-edit-${row.authorId}`,
    },
    {
      id: "deactivate",
      label: "غیرفعال‌سازی",
      icon: UserX,
      variant: "destructive",
      confirm: (row) => `غیرفعال‌سازی «${row.displayName}»؟`,
      onClick: (row) => onDeactivate(row.authorId, row.displayName),
      testId: (row) => `admin-content-author-deactivate-${row.authorId}`,
      visible: (row) => row.isActive,
    },
  ];
}

function buildColumnDefs(
  rowActions: AppGridRowAction<ContentAuthorGridRow>[],
): ColDef<ContentAuthorGridRow>[] {
  return [
    applyAuthorsGridFilterHeader({
      field: "displayName",
      headerName: "نویسنده",
      minWidth: 240,
      flex: 2,
      cellRenderer: AvatarCell,
    }),
    applyAuthorsGridFilterHeader({
      field: "slug",
      headerName: "نامک",
      width: 180,
      minWidth: 140,
      maxWidth: 240,
      cellRenderer: SlugCell,
    }),
    applyAuthorsGridFilterHeader({
      field: "isActive",
      headerName: "وضعیت",
      width: 110,
      cellRenderer: ActiveCell,
    }),
    applyAuthorsGridFilterHeader({
      field: "articleCount",
      headerName: "مقالات",
      width: 100,
      minWidth: 80,
      valueFormatter: (p) => String(p.value ?? 0),
    }),
    applyAuthorsGridFilterHeader({
      field: "updatedAt",
      headerName: "به‌روزرسانی",
      width: 130,
      minWidth: 110,
      valueFormatter: (p) => formatJalaliDate(String(p.value ?? ""), "fa"),
    }),
    buildPinnedActionsColumnDef<ContentAuthorGridRow>({
      direction: "rtl",
      actionSlots: 3,
      width: 132,
      minWidth: 120,
      maxWidth: 168,
      cellRenderer: (params: ICellRendererParams<ContentAuthorGridRow>) =>
        params.data ? <AppGridRowActionsCell row={params.data} actions={rowActions} /> : null,
    }),
  ];
}

const AUTHORS_STATUS_FILTER_OPTIONS = [
  { value: "true", label: "فعال" },
  { value: "false", label: "غیرفعال" },
] as const;

const AUTHORS_ADVANCED_FILTERS: AppGridFilterColumnDef[] = [
  { id: "displayName", header: "نام نمایشی", filterKind: "text" },
  { id: "slug", header: "نامک", filterKind: "text" },
  {
    id: "isActive",
    header: "وضعیت",
    filterKind: "status",
    enumOptions: [
      { value: "true", label: "فعال" },
      { value: "false", label: "غیرفعال" },
    ],
  },
  { id: "articleCount", header: "تعداد مقالات", filterKind: "number" },
  { id: "updatedAt", header: "به‌روزرسانی", filterKind: "date" },
];

/** فهرست نویسندگان Admin — الگوی canonical AppDataGrid. */
export function AdminContentAuthorsScreen() {
  const router = useRouter();
  const [reloadToken, setReloadToken] = useState(0);
  const [gridError, setGridError] = useState<string>();
  const [showCreate, setShowCreate] = useState(false);
  const [createName, setCreateName] = useState("");
  const [createSlug, setCreateSlug] = useState("");
  const [createSlugTouched, setCreateSlugTouched] = useState(false);
  const [saving, setSaving] = useState(false);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_CONTENT_AUTHORS_GRID_VIEW_KEY), []);

  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);

  const onDeactivate = useCallback(async (authorId: string, displayName: string) => {
    const result = await deactivateContentAuthor(authorId);
    if (result.state !== "ok") {
      toast.error(mapContentAuthorMutationError(result));
      return;
    }
    toast.success(`«${displayName}» غیرفعال شد`);
    refresh();
  }, [refresh]);

  const rowActions = useMemo(() => buildAuthorRowActions(onDeactivate), [onDeactivate]);
  const columnDefs = useMemo(() => buildColumnDefs(rowActions), [rowActions]);

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      const result = await queryAdminContentAuthorsGrid(query);
      if (result.denied) {
        setGridError("admin.authorization.denied");
        throw new Error(result.message);
      }
      if (result.source === "error") {
        setGridError(result.message);
        throw new Error(result.message ?? "host-unreachable");
      }
      setGridError(undefined);
      return result.page;
    },
    [reloadToken],
  );

  const saveCreate = useCallback(async () => {
    setSaving(true);
    const result = await createContentAuthor({ displayName: createName, slug: createSlug });
    setSaving(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapContentAuthorMutationError(result));
      return;
    }
    setShowCreate(false);
    router.push(`/admin/content/authors/${result.data.authorId}`);
  }, [createName, createSlug, router]);

  return (
    <main className="w-full" data-testid="admin-content-authors">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">نویسندگان</h1>
          <p className="mt-1 text-[length:var(--type-body)] text-muted">مدیریت پروفایل و انتساب نویسنده به مقالات</p>
        </div>
        <button
          type="button"
          className="inline-flex min-h-11 items-center gap-1 rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95"
          onClick={() => setShowCreate(true)}
          data-testid="content-author-create"
        >
          <span aria-hidden>+</span>
          نویسنده جدید
        </button>
      </div>

      <section className="rounded-2xl border border-border bg-surface-elevated p-2 shadow-sm md:p-4">
        {gridError ? (
          <ErrorState
            title="نویسندگان خوانده نشد"
            detail={gridError}
            onRetry={refresh}
            retryLabel={faWorkspaceMessages.retry}
          />
        ) : (
          <AppDataGrid<ContentAuthorGridRow>
            gridId={ADMIN_CONTENT_AUTHORS_GRID_VIEW_KEY}
            columnDefs={columnDefs}
            queryAdapter={queryAdapter}
            advancedFilterColumns={AUTHORS_ADVANCED_FILTERS}
            externalFilterFields={AUTHORS_EXTERNAL_FILTER_FIELDS}
            statusFilterOptions={[...AUTHORS_STATUS_FILTER_OPTIONS]}
            locale="fa"
            direction="rtl"
            rowCountNoun={{ fa: "نویسنده", en: "authors" }}
            messageOverrides={{
              advancedFilterTitle: "فیلتر پیشرفته نویسندگان",
              advancedFilterSubtitle: "جستجوی دقیق میان نویسندگان",
            }}
            savedViewStore={savedViewStore}
            exportFilenameBase="admin-content-authors"
            exportHeaders={["نام نمایشی", "نامک", "وضعیت", "مقالات", "به‌روزرسانی"]}
            getExportRow={(row) => [
              row.displayName,
              row.slug,
              authorStatusLabel(row.isActive),
              String(row.articleCount),
              formatJalaliDate(row.updatedAt, "fa"),
            ]}
          />
        )}
      </section>

      {showCreate ? (
        <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-md rounded-2xl bg-white p-5 shadow-xl">
            <h2 className="mb-4 text-lg font-bold">نویسندهٔ جدید</h2>
            <label className="block text-sm">
              <span className="mb-1 block text-gray-600">نام نمایشی</span>
              <input
                className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                value={createName}
                onChange={(e) => {
                  setCreateName(e.target.value);
                  if (!createSlugTouched) setCreateSlug(slugifyContentAuthorName(e.target.value));
                }}
              />
            </label>
            <label className="mt-3 block text-sm">
              <span className="mb-1 block text-gray-600">نامک</span>
              <input
                className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                dir="ltr"
                value={createSlug}
                onChange={(e) => {
                  setCreateSlugTouched(true);
                  setCreateSlug(e.target.value);
                }}
              />
            </label>
            <div className="mt-4 flex justify-end gap-2">
              <button type="button" className="rounded-xl px-4 py-2 text-sm" onClick={() => setShowCreate(false)}>انصراف</button>
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                disabled={saving}
                onClick={() => void saveCreate()}
              >
                ایجاد
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </main>
  );
}
