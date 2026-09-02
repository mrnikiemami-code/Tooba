"use client";

import { useCallback, useMemo, useState } from "react";
import Link from "next/link";
import { toast } from "react-toastify";
import { Archive, Eye, Pencil, Trash2, Upload, Undo2 } from "lucide-react";
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
  formatArticleLocaleLabel,
  canArchiveArticle,
  canHardDeleteArticle,
  archiveAdminArticle,
  deleteAdminArticle,
  isArticleArchived,
  isArticlePublished,
  publishAdminArticle,
  queryAdminContentArticlesGrid,
  unpublishAdminArticle,
  type AdminContentArticle,
} from "../content/content-api";
import {
  ContentArticleDestructiveDialog,
  type ArticleDestructiveKind,
  type ArticleDestructiveTarget,
} from "./content-article-destructive-dialog.tsx";
import { ADMIN_CONTENT_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";

const CONTENT_GRID_FILTER_MATRIX: Record<string, AppGridFilterSpec> = {
  title: { field: "title", kind: "text" },
  slug: { field: "slug", kind: "text" },
  status: { field: "status", kind: "status" },
  category: { field: "category", kind: "text" },
  locale: { field: "locale", kind: "text" },
  authorDisplayName: { field: "authorDisplayName", kind: "text" },
  updatedAt: { field: "updatedAt", kind: "jalali-date" },
};

const CONTENT_EXTERNAL_FILTER_FIELDS = appGridExternalFilterFields(CONTENT_GRID_FILTER_MATRIX);

function applyContentGridFilterHeader<T>(colDef: ColDef<T>): ColDef<T> {
  const field = String(colDef.field ?? colDef.colId ?? "");
  return applyAppGridFilterHeader(colDef, CONTENT_GRID_FILTER_MATRIX[field] ?? { field, kind: "none" });
}

function isPublished(status: string): boolean {
  return isArticlePublished(status);
}

function contentStatusClass(status: string): string {
  if (isArticleArchived(status)) {
    return "inline-flex rounded-full bg-muted/20 px-2.5 py-1 text-xs font-medium text-muted";
  }
  return isPublished(status)
    ? "inline-flex rounded-full bg-success/15 px-2.5 py-1 text-xs font-medium text-success"
    : "inline-flex rounded-full bg-warning/15 px-2.5 py-1 text-xs font-medium text-warning";
}

function contentStatusLabel(status: string): string {
  if (isArticleArchived(status)) return "بایگانی";
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

function LocaleCell(params: ICellRendererParams<AdminContentArticle>) {
  const row = params.data;
  if (!row) return null;
  return <span className="text-sm">{formatArticleLocaleLabel(row.locale)}</span>;
}

function AuthorCell(params: ICellRendererParams<AdminContentArticle>) {
  const row = params.data;
  if (!row?.authorDisplayName?.trim()) return <span className="text-muted">—</span>;
  return <AppGridTruncatedCell params={params} text={row.authorDisplayName} />;
}

function buildContentRowActions(
  onRequestAction: (kind: ArticleDestructiveKind, row: AdminContentArticle) => void,
): AppGridRowAction<AdminContentArticle>[] {
  return [
    {
      id: "view",
      label: "مشاهده",
      icon: Eye,
      href: (row) => `/admin/content/articles/${encodeURIComponent(row.articleId)}`,
      testId: (row) => `admin-content-view-${row.articleId}`,
    },
    {
      id: "edit",
      label: "ویرایش",
      icon: Pencil,
      href: (row) => `/admin/content/articles/${encodeURIComponent(row.articleId)}?mode=edit`,
      testId: (row) => `admin-content-edit-${row.articleId}`,
      visible: (row) => !isArticleArchived(row.status),
    },
    {
      id: "delete",
      label: "حذف",
      icon: Trash2,
      variant: "destructive",
      onClick: (row) => onRequestAction("delete", row),
      testId: (row) => `admin-content-delete-${row.articleId}`,
      visible: (row) => canHardDeleteArticle(row.status),
    },
    {
      id: "archive",
      label: "بایگانی",
      icon: Archive,
      variant: "destructive",
      onClick: (row) => onRequestAction("archive", row),
      testId: (row) => `admin-content-archive-${row.articleId}`,
      visible: (row) => canArchiveArticle(row.status),
    },
    {
      id: "publish",
      label: "انتشار",
      icon: Upload,
      onClick: (row) => onRequestAction("publish", row),
      testId: (row) => `admin-content-publish-${row.articleId}`,
      visible: (row) => !isPublished(row.status) && !isArticleArchived(row.status),
    },
    {
      id: "unpublish",
      label: "لغو انتشار",
      icon: Undo2,
      variant: "destructive",
      onClick: (row) => onRequestAction("unpublish", row),
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
      field: "locale",
      headerName: "زبان",
      width: 100,
      minWidth: 90,
      cellRenderer: LocaleCell,
    }),
    applyContentGridFilterHeader({
      field: "authorDisplayName",
      headerName: "نویسنده",
      width: 140,
      minWidth: 110,
      maxWidth: 180,
      cellRenderer: AuthorCell,
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
      actionSlots: 5,
      width: 200,
      minWidth: 180,
      maxWidth: 200,
      cellRenderer: (params: ICellRendererParams<AdminContentArticle>) =>
        params.data ? <AppGridRowActionsCell row={params.data} actions={rowActions} /> : null,
    }),
  ];
}

const CONTENT_STATUS_FILTER_OPTIONS = [
  { value: "Published", label: "منتشر" },
  { value: "Draft", label: "پیش‌نویس" },
  { value: "Archived", label: "بایگانی" },
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
      { value: "Archived", label: "بایگانی" },
    ],
  },
  { id: "locale", header: "زبان", filterKind: "text" },
  { id: "authorDisplayName", header: "نویسنده", filterKind: "text" },
  { id: "category", header: "دسته", filterKind: "text" },
  { id: "updatedAt", header: "به‌روزرسانی", filterKind: "date" },
];

/** فهرست مقالات Admin — الگوی canonical AppDataGrid مثل محصولات. */
export function AdminContentScreen() {
  const [reloadToken, setReloadToken] = useState(0);
  const [gridError, setGridError] = useState<string>();
  const [destructiveKind, setDestructiveKind] = useState<ArticleDestructiveKind | null>(null);
  const [destructiveTarget, setDestructiveTarget] = useState<ArticleDestructiveTarget | null>(null);
  const [destructivePending, setDestructivePending] = useState(false);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_CONTENT_GRID_VIEW_KEY), []);

  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);

  const onRequestAction = useCallback((kind: ArticleDestructiveKind, row: AdminContentArticle) => {
    setDestructiveKind(kind);
    setDestructiveTarget({ articleId: row.articleId, title: row.title, locale: row.locale });
  }, []);

  const onConfirmDestructive = useCallback(async () => {
    if (!destructiveTarget || !destructiveKind) return;
    setDestructivePending(true);
    try {
      if (destructiveKind === "delete") {
        const result = await deleteAdminArticle(destructiveTarget.articleId);
        if (!result.ok) {
          toast.error(result.message ?? "حذف ناموفق بود");
          return;
        }
      } else if (destructiveKind === "archive") {
        const result = await archiveAdminArticle(destructiveTarget.articleId);
        if (!result.ok) {
          toast.error(result.message ?? "بایگانی ناموفق بود");
          return;
        }
      } else if (destructiveKind === "publish") {
        const ok = await publishAdminArticle(destructiveTarget.articleId);
        if (!ok) {
          toast.error("انتشار ناموفق بود");
          return;
        }
      } else if (destructiveKind === "unpublish") {
        const ok = await unpublishAdminArticle(destructiveTarget.articleId);
        if (!ok) {
          toast.error("لغو انتشار ناموفق بود");
          return;
        }
      }
      setDestructiveKind(null);
      setDestructiveTarget(null);
      refresh();
    } finally {
      setDestructivePending(false);
    }
  }, [destructiveKind, destructiveTarget, refresh]);

  const rowActions = useMemo(() => buildContentRowActions(onRequestAction), [onRequestAction]);
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
        <Link
          href="/admin/content/articles/new"
          className="inline-flex min-h-11 items-center gap-1 rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95"
          data-testid="admin-content-new-article"
        >
          <span aria-hidden>+</span>
          مقاله جدید
        </Link>
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
            exportHeaders={["عنوان", "نشانی صفحه", "وضعیت", "زبان", "نویسنده", "دسته", "به‌روزرسانی"]}
            getExportRow={(row) => [
              row.title,
              row.slug,
              contentStatusLabel(row.status),
              formatArticleLocaleLabel(row.locale),
              row.authorDisplayName ?? "",
              row.category ?? "",
              formatJalaliDate(row.updatedAt, "fa"),
            ]}
          />
        )}
      </section>

      <ContentArticleDestructiveDialog
        kind={destructiveKind}
        target={destructiveTarget}
        open={destructiveKind !== null && destructiveTarget !== null}
        pending={destructivePending}
        onClose={() => {
          if (!destructivePending) {
            setDestructiveKind(null);
            setDestructiveTarget(null);
          }
        }}
        onConfirm={onConfirmDestructive}
      />
    </main>
  );
}
