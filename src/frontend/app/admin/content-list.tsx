"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
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
  capabilityPermissionIds,
  createAdminAccessApi,
  hasCapability,
} from "../access-control/access-control-api";
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
import type { SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";
import { prepareAdminDevActor } from "./admin-api";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import { loadAdminLanguages } from "./language-api.ts";
import {
  ContentArticleDestructiveDialog,
  type ArticleDestructiveKind,
  type ArticleDestructiveTarget,
} from "./content-article-destructive-dialog.tsx";
import { ADMIN_CONTENT_GRID_VIEW_KEY, createHostSavedViewStore } from "./saved-view-store";

/** نگاشت کلید خطای گرید به جزئیات فارسی قابل‌نمایش. */
export function contentListGridErrorDetail(raw: string | undefined): string | undefined {
  if (!raw) return undefined;
  return mapAdminErrorMessage(raw, "fa");
}

const CONTENT_GRID_FILTER_MATRIX: Record<string, AppGridFilterSpec> = {
  title: { field: "title", kind: "text" },
  status: { field: "status", kind: "status" },
  category: { field: "category", kind: "text" },
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

/** caps === null (بارگذاری ناموفق) → همه مجاز؛ مثل AdminShell. */
function allowedCapability(caps: Set<string> | null, permissionId: string): boolean {
  if (caps === null) return true;
  return hasCapability(caps, permissionId);
}

function languageTabLabel(lang: SupportedLocaleDefinition): string {
  return lang.nativeName?.trim() || lang.displayName?.trim() || lang.code;
}

function TitleCell(params: ICellRendererParams<AdminContentArticle>) {
  const row = params.data;
  if (!row) return null;
  return <AppGridTruncatedCell params={params} text={row.title} className="font-semibold" />;
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

function AuthorCell(params: ICellRendererParams<AdminContentArticle>) {
  const row = params.data;
  if (!row?.authorDisplayName?.trim()) return <span className="text-muted">—</span>;
  return <AppGridTruncatedCell params={params} text={row.authorDisplayName} />;
}

function buildContentRowActions(
  caps: Set<string> | null,
  onRequestAction: (kind: ArticleDestructiveKind, row: AdminContentArticle) => void,
): AppGridRowAction<AdminContentArticle>[] {
  const canEdit = allowedCapability(caps, "content.edit");
  const canPublish = allowedCapability(caps, "content.publish");

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
      visible: (row) => canEdit && !isArticleArchived(row.status),
    },
    {
      id: "delete",
      label: "حذف",
      icon: Trash2,
      variant: "destructive",
      onClick: (row) => onRequestAction("delete", row),
      testId: (row) => `admin-content-delete-${row.articleId}`,
      visible: (row) => canEdit && canHardDeleteArticle(row.status),
    },
    {
      id: "archive",
      label: "بایگانی",
      icon: Archive,
      variant: "destructive",
      onClick: (row) => onRequestAction("archive", row),
      testId: (row) => `admin-content-archive-${row.articleId}`,
      visible: (row) => canEdit && canArchiveArticle(row.status),
    },
    {
      id: "publish",
      label: "انتشار",
      icon: Upload,
      onClick: (row) => onRequestAction("publish", row),
      testId: (row) => `admin-content-publish-${row.articleId}`,
      visible: (row) => canPublish && !isPublished(row.status) && !isArticleArchived(row.status),
    },
    {
      id: "unpublish",
      label: "لغو انتشار",
      icon: Undo2,
      variant: "destructive",
      onClick: (row) => onRequestAction("unpublish", row),
      testId: (row) => `admin-content-unpublish-${row.articleId}`,
      visible: (row) => canPublish && isPublished(row.status),
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
      field: "status",
      headerName: "وضعیت",
      width: 120,
      cellRenderer: StatusCell,
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
  { id: "authorDisplayName", header: "نویسنده", filterKind: "text" },
  { id: "category", header: "دسته", filterKind: "text" },
  { id: "updatedAt", header: "به‌روزرسانی", filterKind: "date" },
];

/** فهرست مقالات Admin — تب زبان + الگوی canonical AppDataGrid. */
export function AdminContentScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [reloadToken, setReloadToken] = useState(0);
  const [gridError, setGridError] = useState<string>();
  const [languages, setLanguages] = useState<SupportedLocaleDefinition[]>([]);
  const [selectedLanguage, setSelectedLanguage] = useState<string | null>(null);
  const [destructiveKind, setDestructiveKind] = useState<ArticleDestructiveKind | null>(null);
  const [destructiveTarget, setDestructiveTarget] = useState<ArticleDestructiveTarget | null>(null);
  const [destructivePending, setDestructivePending] = useState(false);
  const [caps, setCaps] = useState<Set<string> | null>(null);
  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_CONTENT_GRID_VIEW_KEY), []);

  useEffect(() => {
    void prepareAdminDevActor()
      .then(async () => {
        try {
          const effective = await createAdminAccessApi().getMyCapabilities();
          setCaps(capabilityPermissionIds(effective));
        } catch {
          setCaps(null);
        }
      })
      .catch(() => {
        setCaps(null);
      });
  }, []);

  useEffect(() => {
    void prepareAdminDevActor().then(() =>
      loadAdminLanguages().then((result) => {
        if (result.state !== "ok" || !result.data?.length) {
          setLanguages([]);
          setSelectedLanguage(null);
          return;
        }
        const active = result.data
          .filter((row) => row.active)
          .slice()
          .sort((a, b) => a.sortOrder - b.sortOrder || a.code.localeCompare(b.code));
        setLanguages(active);
        const param = searchParams.get("language")?.trim() ?? "";
        const defaultLang = active.find((row) => row.default) ?? active[0]!;
        const matched = active.find((row) => row.code === param);
        const resolved = matched?.code ?? defaultLang.code;
        setSelectedLanguage(resolved);
        if (param !== resolved) {
          const next = new URLSearchParams(searchParams.toString());
          next.set("language", resolved);
          router.replace(`/admin/content?${next.toString()}`);
        }
      }),
    );
  }, [router, searchParams]);

  const refresh = useCallback(() => setReloadToken((value) => value + 1), []);

  const onSelectLanguage = useCallback(
    (code: string) => {
      if (code === selectedLanguage) return;
      setSelectedLanguage(code);
      const next = new URLSearchParams(searchParams.toString());
      next.set("language", code);
      router.replace(`/admin/content?${next.toString()}`);
      setReloadToken((value) => value + 1);
    },
    [router, searchParams, selectedLanguage],
  );

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

  const canCreate = allowedCapability(caps, "content.create");
  const rowActions = useMemo(() => buildContentRowActions(caps, onRequestAction), [caps, onRequestAction]);
  const columnDefs = useMemo(() => buildColumnDefs(rowActions), [rowActions]);

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => {
      void reloadToken;
      if (!selectedLanguage) {
        return { rows: [], total: 0 };
      }
      const result = await queryAdminContentArticlesGrid({
        ...query,
        filters: {
          ...query.filters,
          locale: { kind: "text", operator: "equals", query: selectedLanguage },
        },
      });
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
    [reloadToken, selectedLanguage],
  );

  const createHref = selectedLanguage
    ? `/admin/content/articles/new?language=${encodeURIComponent(selectedLanguage)}`
    : "/admin/content/articles/new";

  return (
    <main className="w-full" data-testid="admin-content">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">مقالات</h1>
          <p className="mt-1 text-[length:var(--type-body)] text-muted">ایجاد، انتشار و بهینه‌سازی جستجوی مقالات</p>
        </div>
        {canCreate ? (
          <Link
            href={createHref}
            className="inline-flex min-h-11 items-center gap-1 rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95"
            data-testid="admin-content-new-article"
          >
            <span aria-hidden>+</span>
            مقاله جدید
          </Link>
        ) : null}
      </div>

      {languages.length > 0 ? (
        <div className="mb-4 flex flex-wrap gap-2" role="tablist" aria-label="زبان مقالات" data-testid="admin-content-language-tabs">
          {languages.map((lang) => {
            const active = lang.code === selectedLanguage;
            return (
              <button
                key={lang.code}
                type="button"
                role="tab"
                aria-selected={active}
                data-testid={`admin-content-lang-${lang.code}`}
                className={
                  active
                    ? "rounded-lg bg-[#2563EB] px-3 py-1.5 text-sm font-semibold text-white"
                    : "rounded-lg border border-border px-3 py-1.5 text-sm"
                }
                onClick={() => onSelectLanguage(lang.code)}
              >
                {languageTabLabel(lang)}
              </button>
            );
          })}
        </div>
      ) : null}

      <section className="rounded-2xl border border-border bg-surface-elevated p-2 shadow-sm md:p-4">
        {gridError ? (
          <ErrorState
            title="مقالات خوانده نشد"
            detail={contentListGridErrorDetail(gridError)}
            onRetry={refresh}
            retryLabel={faWorkspaceMessages.retry}
          />
        ) : selectedLanguage ? (
          <AppDataGrid<AdminContentArticle>
            key={`admin-content-grid-${selectedLanguage}-${reloadToken}`}
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
            exportHeaders={["عنوان", "زبان", "نویسنده", "دسته", "وضعیت", "به‌روزرسانی"]}
            getExportRow={(row) => [
              row.title,
              formatArticleLocaleLabel(row.locale),
              row.authorDisplayName ?? "",
              row.category ?? "",
              contentStatusLabel(row.status),
              formatJalaliDate(row.updatedAt, "fa"),
            ]}
          />
        ) : (
          <p className="p-4 text-sm text-muted">در حال بارگذاری زبان‌ها…</p>
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
