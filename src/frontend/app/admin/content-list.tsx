"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Eye, Upload, Undo2 } from "lucide-react";
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
import {
  createAdminArticle,
  publishAdminArticle,
  queryAdminContentArticlesGrid,
  unpublishAdminArticle,
  type AdminContentArticle,
} from "../content/content-api";
import { fetchContentCategoryTree, type ContentCategoryTreeNodeDto } from "./content-category-api.ts";
import { ADMIN_CONTENT_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";

const CONTENT_GRID_FILTER_MATRIX: Record<string, AppGridFilterSpec> = {
  title: { field: "title", kind: "text" },
  slug: { field: "slug", kind: "text" },
  status: { field: "status", kind: "status" },
  category: { field: "category", kind: "text" },
  updatedAt: { field: "updatedAt", kind: "jalali-date" },
};

const CONTENT_EXTERNAL_FILTER_FIELDS = appGridExternalFilterFields(CONTENT_GRID_FILTER_MATRIX);

function applyContentGridFilterHeader<T>(colDef: ColDef<T>): ColDef<T> {
  const field = String(colDef.field ?? colDef.colId ?? "");
  return applyAppGridFilterHeader(colDef, CONTENT_GRID_FILTER_MATRIX[field] ?? { field, kind: "none" });
}

function isPublished(status: string): boolean {
  return status === "Published" || status === "1";
}

function contentStatusClass(status: string): string {
  return isPublished(status)
    ? "inline-flex rounded-full bg-success/15 px-2.5 py-1 text-xs font-medium text-success"
    : "inline-flex rounded-full bg-warning/15 px-2.5 py-1 text-xs font-medium text-warning";
}

function contentStatusLabel(status: string): string {
  return isPublished(status) ? "منتشر" : "پیش‌نویس";
}

function TitleCell(params: ICellRendererParams<AdminContentArticle>) {
  const row = params.data;
  if (!row) return null;
  return <AppGridTruncatedCell params={params} text={row.title} className="font-semibold" />;
}

function SlugCell(params: ICellRendererParams<AdminContentArticle>) {
  const row = params.data;
  if (!row) return null;
  return <span dir="ltr" className="font-mono text-xs text-muted">{row.slug}</span>;
}

function StatusCell(params: ICellRendererParams<AdminContentArticle>) {
  const status = String(params.value ?? "");
  return <AppGridBadgeCell params={params} label={contentStatusLabel(status)} className={contentStatusClass(status)} />;
}

function CategoryCell(params: ICellRendererParams<AdminContentArticle>) {
  const row = params.data;
  if (!row?.category?.trim()) return <span className="text-muted">—</span>;
  return <AppGridTruncatedCell params={params} text={row.category} />;
}

function buildContentRowActions(
  onPublishToggle: (articleId: string, published: boolean) => Promise<void>,
): AppGridRowAction<AdminContentArticle>[] {
  return [
    {
      id: "view",
      label: "مشاهده",
      icon: Eye,
      href: (row) => `/blogs/${encodeURIComponent(row.slug)}`,
      testId: (row) => `admin-content-view-${row.articleId}`,
      visible: (row) => isPublished(row.status),
    },
    {
      id: "publish",
      label: "انتشار",
      icon: Upload,
      confirm: (row) => `انتشار «${row.title}»؟`,
      onClick: (row) => onPublishToggle(row.articleId, false),
      testId: (row) => `admin-content-publish-${row.articleId}`,
      visible: (row) => !isPublished(row.status),
    },
    {
      id: "unpublish",
      label: "لغو انتشار",
      icon: Undo2,
      variant: "destructive",
      confirm: (row) => `لغو انتشار «${row.title}»؟`,
      onClick: (row) => onPublishToggle(row.articleId, true),
      testId: (row) => `admin-content-unpublish-${row.articleId}`,
      visible: (row) => isPublished(row.status),
    },
  ];
}

function buildColumnDefs(
  rowActions: AppGridRowAction<AdminContentArticle>[],
): ColDef<AdminContentArticle>[] {
  return [
    applyContentGridFilterHeader({
      field: "title",
      headerName: "عنوان",
      minWidth: 280,
      flex: 2,
      cellRenderer: TitleCell,
    }),
    applyContentGridFilterHeader({
      field: "slug",
      headerName: "نشانی صفحه",
      width: 180,
      minWidth: 140,
      maxWidth: 260,
      cellRenderer: SlugCell,
    }),
    applyContentGridFilterHeader({
      field: "status",
      headerName: "وضعیت",
      width: 120,
      cellRenderer: StatusCell,
    }),
    applyContentGridFilterHeader({
      field: "category",
      headerName: "دسته",
      width: 140,
      minWidth: 110,
      maxWidth: 200,
      cellRenderer: CategoryCell,
    }),
    applyContentGridFilterHeader({
      field: "updatedAt",
      headerName: "به‌روزرسانی",
      width: 130,
      minWidth: 110,
      valueFormatter: (p) => formatJalaliDate(String(p.value ?? ""), "fa"),
    }),
    buildPinnedActionsColumnDef<AdminContentArticle>({
      direction: "rtl",
      actionSlots: 3,
      width: 132,
      minWidth: 120,
      maxWidth: 168,
      cellRenderer: (params: ICellRendererParams<AdminContentArticle>) =>
        params.data ? <AppGridRowActionsCell row={params.data} actions={rowActions} /> : null,
    }),
  ];
}

const CONTENT_STATUS_FILTER_OPTIONS = [
  { value: "Published", label: "منتشر" },
  { value: "Draft", label: "پیش‌نویس" },
] as const;

const CONTENT_ADVANCED_FILTERS: AppGridFilterColumnDef[] = [
  { id: "title", header: "عنوان", filterKind: "text" },
  { id: "slug", header: "نشانی صفحه", filterKind: "text" },
  {
    id: "status",
    header: "وضعیت",
    filterKind: "status",
    enumOptions: [
      { value: "Published", label: "منتشر" },
      { value: "Draft", label: "پیش‌نویس" },
    ],
  },
  { id: "category", header: "دسته", filterKind: "text" },
  { id: "updatedAt", header: "به‌روزرسانی", filterKind: "date" },
];

/** فهرست مقالات Admin — الگوی canonical AppDataGrid مثل محصولات. */
export function AdminContentScreen() {
  const [reloadToken, setReloadToken] = useState(0);
  const [gridError, setGridError] = useState<string>();
  const [showCreate, setShowCreate] = useState(false);
  const [categoryOptions, setCategoryOptions] = useState<ContentCategoryTreeNodeDto[]>([]);
  const [draft, setDraft] = useState({
    slug: "",
    title: "",
    excerpt: "",
    body: "",
    authorDisplayName: "تحریریه توبا",
    category: "",
    categoryId: "" as string,
    seoTitle: "",
    seoDescription: "",
  });
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_CONTENT_GRID_VIEW_KEY), []);

  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);

  useEffect(() => {
    void fetchContentCategoryTree("fa-IR").then((result) => {
      if (result.state === "ok" && result.data) setCategoryOptions(result.data);
    });
  }, []);

  const onPublishToggle = useCallback(async (articleId: string, published: boolean) => {
    if (published) {
      await unpublishAdminArticle(articleId);
    } else {
      await publishAdminArticle(articleId);
    }
    refresh();
  }, [refresh]);

  const rowActions = useMemo(() => buildContentRowActions(onPublishToggle), [onPublishToggle]);
  const columnDefs = useMemo(() => buildColumnDefs(rowActions), [rowActions]);

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      const result = await queryAdminContentArticlesGrid(query);
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

  return (
    <main className="w-full" data-testid="admin-content">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">محتوا / بلاگ</h1>
          <p className="mt-1 text-[length:var(--type-body)] text-muted">ایجاد، انتشار و بهینه‌سازی جستجوی مقالات</p>
        </div>
        <button
          type="button"
          className="inline-flex min-h-11 items-center gap-1 rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95"
          onClick={() => setShowCreate(true)}
        >
          <span aria-hidden>+</span>
          مقاله جدید
        </button>
      </div>

      <section className="rounded-2xl border border-border bg-surface-elevated p-2 shadow-sm md:p-4">
        {gridError ? (
          <ErrorState
            title="مقالات خوانده نشد"
            detail={gridError}
            onRetry={refresh}
            retryLabel={faWorkspaceMessages.retry}
          />
        ) : (
          <AppDataGrid<AdminContentArticle>
            gridId={ADMIN_CONTENT_GRID_VIEW_KEY}
            columnDefs={columnDefs}
            queryAdapter={queryAdapter}
            advancedFilterColumns={CONTENT_ADVANCED_FILTERS}
            externalFilterFields={CONTENT_EXTERNAL_FILTER_FIELDS}
            statusFilterOptions={[...CONTENT_STATUS_FILTER_OPTIONS]}
            locale="fa"
            direction="rtl"
            rowCountNoun={{ fa: "مقاله", en: "rows" }}
            messageOverrides={{
              advancedFilterTitle: "فیلتر پیشرفته مقالات",
              advancedFilterSubtitle: "جستجوی دقیق میان مقالات",
            }}
            savedViewStore={savedViewStore}
            exportFilenameBase="admin-content"
            exportHeaders={["عنوان", "نشانی صفحه", "وضعیت", "دسته", "به‌روزرسانی"]}
            getExportRow={(row) => [
              row.title,
              row.slug,
              contentStatusLabel(row.status),
              row.category ?? "",
              formatJalaliDate(row.updatedAt, "fa"),
            ]}
          />
        )}
      </section>

      {showCreate ? (
        <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/50 p-4">
          <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white p-5 shadow-xl">
            <h2 className="mb-4 text-lg font-bold">ایجاد پیش‌نویس</h2>
            <div className="space-y-3">
              {([
                ["slug", "نشانی صفحه"],
                ["title", "عنوان"],
                ["excerpt", "چکیده"],
                ["body", "بدنه"],
                ["authorDisplayName", "نویسنده"],
                ["categoryId", "دسته"],
                ["seoTitle", "عنوان جستجو"],
                ["seoDescription", "توضیح جستجو"],
              ] as const).map(([key, label]) => (
                <label key={key} className="block text-sm">
                  <span className="mb-1 block text-gray-600">{label}</span>
                  {key === "body" || key === "excerpt" || key === "seoDescription" ? (
                    <textarea
                      className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                      rows={key === "body" ? 5 : 2}
                      value={draft[key === "categoryId" ? "categoryId" : key]}
                      onChange={(e) => setDraft((current) => ({ ...current, [key === "categoryId" ? "categoryId" : key]: e.target.value }))}
                    />
                  ) : key === "categoryId" ? (
                    <select
                      className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                      value={draft.categoryId}
                      onChange={(e) => {
                        const selected = categoryOptions.find((row) => row.id === e.target.value);
                        setDraft((current) => ({
                          ...current,
                          categoryId: e.target.value,
                          category: selected?.name ?? "",
                        }));
                      }}
                    >
                      <option value="">— بدون دسته —</option>
                      {categoryOptions.map((row) => (
                        <option key={row.id} value={row.id}>{row.name}</option>
                      ))}
                    </select>
                  ) : (
                    <input
                      className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                      value={draft[key]}
                      onChange={(e) => setDraft((current) => ({ ...current, [key]: e.target.value }))}
                    />
                  )}
                </label>
              ))}
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <button type="button" className="rounded-xl px-4 py-2 text-sm" onClick={() => setShowCreate(false)}>انصراف</button>
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                onClick={() => void createAdminArticle({
                  ...draft,
                  categoryId: draft.categoryId || null,
                }).then((result) => {
                  if (result.ok) { setShowCreate(false); refresh(); }
                  else setGridError(result.message);
                })}
              >
                ذخیره پیش‌نویس
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </main>
  );
}
