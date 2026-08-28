"use client";

/**
 * صفحهٔ Admin Category: درخت + workspace با VIEW/EDIT صریح (T005-R1).
 * تب‌های آینده فقط پوستهٔ progressive disclosure هستند.
 */

import Link from "next/link";
import { useParams, usePathname, useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import {
  AppCategoryTree,
  buildCategoryPath,
  buildParentMap,
  buildTranslationStatuses,
  categoryStatusLabel,
  collectAncestorIds,
  countDirectChildren,
  resolveCategoryDropPlan,
  translationReadinessLabel,
  useAdminFormMode,
  type AppCategoryTreeNode,
  type CategoryDropRequest,
} from "../../design-system";
import { prepareAdminDevActor } from "./admin-api.ts";
import {
  buildStorefrontCategoryRoute,
  createCategory,
  fetchCategoryTree,
  fetchCategoryWorkspace,
  mapCategoryMutationError,
  moveCategory,
  reorderCategories,
  slugifyCategoryName,
  updateCategoryCore,
  upsertCategoryTranslation,
  type CategoryPublicationStatus,
  type CategoryTreeNodeDto,
  type CategoryWorkspaceSummary,
} from "./catalog-category-api.ts";

const API_LOCALE = "fa-IR";
const UI_LOCALES = ["fa-IR", "en-US", "ar-SA"] as const;

const TABS = [
  { id: "general", label: "عمومی", implemented: true },
  { id: "translations", label: "ترجمه‌ها", implemented: true },
  { id: "attributes", label: "ویژگی‌ها", implemented: false },
  { id: "facets", label: "فیلترها", implemented: false },
  { id: "mega-menu", label: "مگامنو", implemented: false },
  { id: "products", label: "محصولات", implemented: false },
  { id: "seo", label: "SEO", implemented: false },
  { id: "settings", label: "تنظیمات", implemented: false },
  { id: "history", label: "تاریخچه", implemented: false },
] as const;

type TabId = (typeof TABS)[number]["id"];

interface GeneralDraft {
  name: string;
  slug: string;
  status: CategoryPublicationStatus;
  sortOrder: number;
  isVisible: boolean;
  parentId: string | null;
  slugTouched: boolean;
}

function isTabId(value: string | undefined): value is TabId {
  return Boolean(value && TABS.some((t) => t.id === value));
}

function toTreeNodes(rows: CategoryTreeNodeDto[]): AppCategoryTreeNode[] {
  return rows.map((r) => ({
    id: r.id,
    parentId: r.parentId,
    name: r.name,
    slug: r.slug,
    status: r.status,
    sortOrder: r.sortOrder,
    isVisible: r.isVisible,
    hasChildren: r.hasChildren,
    productCount: r.productCount,
  }));
}

function statusBadgeClass(status: CategoryPublicationStatus): string {
  if (status === "Published") return "bg-emerald-50 text-emerald-700 border-emerald-200";
  if (status === "Archived") return "bg-slate-50 text-slate-600 border-slate-200";
  return "bg-amber-50 text-amber-800 border-amber-200";
}

function collectDescendantIds(nodes: AppCategoryTreeNode[], rootId: string): Set<string> {
  const byParent = new Map<string | null, string[]>();
  for (const n of nodes) {
    const list = byParent.get(n.parentId) ?? [];
    list.push(n.id);
    byParent.set(n.parentId, list);
  }
  const out = new Set<string>();
  const stack = [...(byParent.get(rootId) ?? [])];
  while (stack.length) {
    const id = stack.pop()!;
    if (out.has(id)) continue;
    out.add(id);
    for (const child of byParent.get(id) ?? []) stack.push(child);
  }
  return out;
}

function draftFromWorkspace(
  workspace: CategoryWorkspaceSummary,
  localeName: string,
  localeSlug: string,
): GeneralDraft {
  return {
    name: localeName,
    slug: localeSlug,
    status: workspace.status,
    sortOrder: workspace.sortOrder,
    isVisible: workspace.isVisible,
    parentId: workspace.parentCategoryId,
    slugTouched: true,
  };
}

function CreateCategoryDialog({
  open,
  parentId,
  parentName,
  busy,
  onClose,
  onSubmit,
}: {
  open: boolean;
  parentId: string | null;
  parentName: string | null;
  busy: boolean;
  onClose: () => void;
  onSubmit: (input: { name: string; slug: string; parentId: string | null }) => void;
}) {
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [slugTouched, setSlugTouched] = useState(false);

  useEffect(() => {
    if (!open) return;
    setName("");
    setSlug("");
    setSlugTouched(false);
  }, [open, parentId]);

  if (!open) return null;

  const isChild = Boolean(parentId);
  const preview = slug.trim() ? buildStorefrontCategoryRoute("fa", slug.trim()) : null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="create-category-title"
      data-testid="create-category-dialog"
    >
      <div className="w-full max-w-md rounded-2xl border border-gray-200 bg-white p-5 shadow-xl">
        <h3 id="create-category-title" className="text-lg font-bold text-slate-900">
          {isChild ? "افزودن زیرمجموعه" : "دسته‌بندی جدید"}
        </h3>
        <p className="mt-1 text-sm text-slate-500">
          {isChild
            ? `والد: ${parentName || "—"}`
            : "والد: ریشه · وضعیت: پیش‌نویس"}
        </p>

        <label className="mt-4 block text-sm font-medium text-slate-700">
          نام (فارسی)
          <input
            className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={name}
            onChange={(e) => {
              const next = e.target.value;
              setName(next);
              if (!slugTouched) setSlug(slugifyCategoryName(next));
            }}
            autoFocus
            data-testid="create-category-name"
          />
        </label>

        <label className="mt-3 block text-sm font-medium text-slate-700">
          نامک (Slug)
          <input
            className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            dir="ltr"
            value={slug}
            onChange={(e) => {
              setSlugTouched(true);
              setSlug(e.target.value);
            }}
            data-testid="create-category-slug"
          />
        </label>
        {preview ? (
          <p className="mt-2 text-xs text-slate-500" dir="ltr" data-testid="create-category-route-preview">
            پیش‌نمایش: {preview}
          </p>
        ) : null}

        <p className="mt-3 text-xs text-slate-500">
          وضعیت پیش‌فرض: پیش‌نویس · SEO و ویژگی‌ها بعداً تکمیل می‌شوند.
        </p>

        <div className="mt-5 flex items-center justify-end gap-2">
          <button
            type="button"
            className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-gray-50"
            onClick={onClose}
            disabled={busy}
          >
            انصراف
          </button>
          <button
            type="button"
            className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95 disabled:opacity-50"
            disabled={busy || !name.trim() || !slug.trim()}
            onClick={() => onSubmit({ name: name.trim(), slug: slug.trim(), parentId })}
            data-testid="create-category-save"
          >
            {busy ? "در حال ذخیره…" : "ذخیره"}
          </button>
        </div>
      </div>
    </div>
  );
}

function ComingSoonPanel() {
  return (
    <div
      className="rounded-2xl border border-dashed border-gray-200 bg-slate-50 p-8 text-center text-sm text-slate-500"
      data-testid="category-tab-coming-soon"
    >
      این بخش در تسک بعدی تکمیل می‌شود
      <div className="mt-2 text-xs text-slate-400">به‌زودی</div>
    </div>
  );
}

function SummaryCard({ label, value, ltr }: { label: string; value: string; ltr?: boolean }) {
  return (
    <div className="rounded-2xl border border-gray-200 bg-white p-4 shadow-sm">
      <div className="text-xs font-medium text-slate-500">{label}</div>
      <div className="mt-1 text-sm font-semibold text-slate-900" dir={ltr ? "ltr" : undefined}>
        {value}
      </div>
    </div>
  );
}

function GeneralViewSummary({
  workspace,
  parentName,
  childrenCount,
  productCount,
  storefrontRoute,
  activeLocaleName,
  activeLocaleSlug,
}: {
  workspace: CategoryWorkspaceSummary;
  parentName: string;
  childrenCount: number;
  productCount: number | null;
  storefrontRoute: string | null;
  activeLocaleName: string;
  activeLocaleSlug: string;
}) {
  const coverage = buildTranslationStatuses(workspace.translations, UI_LOCALES);
  const completeCount = coverage.filter((c) => c.readiness === "complete").length;

  return (
    <div className="space-y-4" data-testid="category-general-summary" data-form-mode="view">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SummaryCard label="والد" value={parentName} />
        <SummaryCard label="وضعیت" value={categoryStatusLabel(workspace.status)} />
        <SummaryCard label="ترتیب" value={String(workspace.sortOrder)} />
        <SummaryCard label="نمایش در ویترین" value={workspace.isVisible ? "بله" : "خیر"} />
        <SummaryCard label="نام" value={activeLocaleName || "—"} />
        <SummaryCard label="نامک" value={activeLocaleSlug || "—"} ltr />
        <SummaryCard label="پوشش ترجمه" value={`${completeCount} از ${coverage.length}`} />
        <SummaryCard label="زیرمجموعه‌ها" value={String(childrenCount)} />
        {productCount != null ? (
          <SummaryCard label="محصولات" value={String(productCount)} />
        ) : null}
        {storefrontRoute ? (
          <SummaryCard label="مسیر ویترین" value={storefrontRoute} ltr />
        ) : null}
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div className="rounded-2xl border border-gray-200 bg-white p-4">
          <div className="text-xs font-medium text-slate-500">تصویر</div>
          <div className="mt-3 flex h-28 items-center justify-center rounded-xl border border-dashed border-gray-200 bg-slate-50 text-sm text-slate-400">
            {workspace.imageMediaAssetId ? "تصویر متصل است" : "هنوز تصویری تنظیم نشده"}
          </div>
        </div>
        <div className="rounded-2xl border border-gray-200 bg-white p-4">
          <div className="text-xs font-medium text-slate-500">آیکون</div>
          <div className="mt-3 flex h-28 items-center justify-center rounded-xl border border-dashed border-gray-200 bg-slate-50 text-sm text-slate-400">
            {workspace.iconMediaAssetId ? "آیکون متصل است" : "هنوز آیکونی تنظیم نشده"}
          </div>
        </div>
      </div>
    </div>
  );
}

function GeneralEditForm({
  draft,
  fieldError,
  parentOptions,
  busy,
  onChange,
  onSave,
  onCancel,
}: {
  draft: GeneralDraft;
  fieldError: string | null;
  parentOptions: { id: string | null; label: string }[];
  busy: boolean;
  onChange: (next: GeneralDraft) => void;
  onSave: () => void;
  onCancel: () => void;
}) {
  const preview = draft.slug.trim()
    ? buildStorefrontCategoryRoute("fa", draft.slug.trim())
    : null;

  return (
    <div className="space-y-4" data-testid="category-general-edit" data-form-mode="edit">
      <div className="grid gap-4 sm:grid-cols-2">
        <label className="block text-sm font-medium text-slate-700">
          نام (فارسی)
          <input
            className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={draft.name}
            onChange={(e) => {
              const name = e.target.value;
              onChange({
                ...draft,
                name,
                slug: draft.slugTouched ? draft.slug : slugifyCategoryName(name),
              });
            }}
            data-testid="category-edit-name"
          />
        </label>

        <label className="block text-sm font-medium text-slate-700">
          نامک (Slug)
          <input
            className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            dir="ltr"
            value={draft.slug}
            onChange={(e) => onChange({ ...draft, slug: e.target.value, slugTouched: true })}
            data-testid="category-edit-slug"
            aria-invalid={Boolean(fieldError)}
          />
          {fieldError ? (
            <span className="mt-1 block text-xs text-red-600" data-testid="category-slug-error">
              {fieldError}
            </span>
          ) : null}
          {preview ? (
            <span className="mt-1 block text-xs text-slate-500" dir="ltr" data-testid="category-route-preview">
              پیش‌نمایش: {preview}
            </span>
          ) : null}
        </label>

        <label className="block text-sm font-medium text-slate-700">
          وضعیت
          <select
            className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={draft.status}
            onChange={(e) =>
              onChange({ ...draft, status: e.target.value as CategoryPublicationStatus })
            }
            data-testid="category-edit-status"
          >
            <option value="Draft">پیش‌نویس</option>
            <option value="Published">منتشرشده</option>
            <option value="Archived">بایگانی</option>
          </select>
        </label>

        <label className="block text-sm font-medium text-slate-700">
          ترتیب
          <input
            type="number"
            className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={draft.sortOrder}
            onChange={(e) =>
              onChange({ ...draft, sortOrder: Number.parseInt(e.target.value || "0", 10) || 0 })
            }
            data-testid="category-edit-sort-order"
          />
        </label>

        <label className="flex min-h-11 items-center gap-2 text-sm font-medium text-slate-700">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border-gray-300"
            checked={draft.isVisible}
            onChange={(e) => onChange({ ...draft, isVisible: e.target.checked })}
            data-testid="category-edit-visible"
          />
          نمایش در ویترین
        </label>

        <label className="block text-sm font-medium text-slate-700">
          والد
          <select
            className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={draft.parentId ?? ""}
            onChange={(e) =>
              onChange({ ...draft, parentId: e.target.value ? e.target.value : null })
            }
            data-testid="category-edit-parent"
          >
            {parentOptions.map((opt) => (
              <option key={opt.id ?? "root"} value={opt.id ?? ""}>
                {opt.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="flex flex-wrap items-center justify-end gap-2 border-t border-gray-100 pt-4">
        <button
          type="button"
          className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-gray-50"
          onClick={onCancel}
          disabled={busy}
          data-testid="category-edit-cancel"
        >
          انصراف
        </button>
        <button
          type="button"
          className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95 disabled:opacity-50"
          onClick={onSave}
          disabled={busy || !draft.name.trim() || !draft.slug.trim()}
          data-testid="category-edit-save"
        >
          {busy ? "در حال ذخیره…" : "ذخیره"}
        </button>
      </div>
    </div>
  );
}

function TranslationsPanel({ workspace }: { workspace: CategoryWorkspaceSummary }) {
  const statuses = buildTranslationStatuses(workspace.translations, UI_LOCALES);
  return (
    <div className="space-y-3" data-testid="category-translations-panel">
      <p className="text-sm text-slate-500">
        هر دسته‌بندی یک هویت دارد؛ ترجمه‌ها وضعیت آمادگی زبان را نشان می‌دهند.
      </p>
      <ul className="space-y-2">
        {statuses.map((row) => (
          <li
            key={row.locale}
            className="flex items-center justify-between rounded-2xl border border-gray-200 bg-white px-4 py-3"
            data-testid={`translation-status-${row.locale}`}
          >
            <span className="font-medium text-slate-800">{row.label}</span>
            <span
              className={
                row.readiness === "complete"
                  ? "rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700"
                  : row.readiness === "partial"
                    ? "rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold text-amber-800"
                    : "rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600"
              }
            >
              {translationReadinessLabel(row.readiness)}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}

/**
 * صفحهٔ مشترک لیست/جزئیات/تب رده.
 */
export function CategoryAdminScreen() {
  const router = useRouter();
  const pathname = usePathname();
  const params = useParams<{ categoryId?: string; tab?: string }>();
  const categoryId = typeof params.categoryId === "string" ? params.categoryId : undefined;
  const tabParam = typeof params.tab === "string" ? params.tab : undefined;
  const activeTab: TabId = isTabId(tabParam) ? tabParam : "general";

  const [ready, setReady] = useState(false);
  const [loadingTree, setLoadingTree] = useState(true);
  const [treeError, setTreeError] = useState<string | null>(null);
  const [accessDenied, setAccessDenied] = useState(false);
  const [flatNodes, setFlatNodes] = useState<AppCategoryTreeNode[]>([]);
  const [expandedKeys, setExpandedKeys] = useState<string[]>([]);
  const [searchQuery, setSearchQuery] = useState("");

  const [workspace, setWorkspace] = useState<CategoryWorkspaceSummary | null>(null);
  const [workspaceLoading, setWorkspaceLoading] = useState(false);
  const [workspaceError, setWorkspaceError] = useState<string | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  const [createParentId, setCreateParentId] = useState<string | null>(null);
  const [createBusy, setCreateBusy] = useState(false);
  const [mobileWorkspace, setMobileWorkspace] = useState(false);
  const [isNarrow, setIsNarrow] = useState(false);

  const [draft, setDraft] = useState<GeneralDraft | null>(null);
  const [saveBusy, setSaveBusy] = useState(false);
  const [slugFieldError, setSlugFieldError] = useState<string | null>(null);

  const canView = ready && !accessDenied;
  // همان دروازهٔ AdminPanelAccess که mutationهای catalog را هم باز می‌کند؛ FE مرجع امنیت نیست.
  const canEdit = canView;

  const formMode = useAdminFormMode({ canView, canEdit });

  const localePrefix = useMemo(() => {
    const m = pathname.match(/^\/(fa|en|ar)(?=\/|$)/);
    return m ? `/${m[1]}` : "/fa";
  }, [pathname]);

  const basePath = `${localePrefix}/admin/catalog/categories`;

  useEffect(() => {
    const mq = window.matchMedia("(max-width: 1023px)");
    const apply = () => setIsNarrow(mq.matches);
    apply();
    mq.addEventListener("change", apply);
    return () => mq.removeEventListener("change", apply);
  }, []);

  useEffect(() => {
    void prepareAdminDevActor().finally(() => setReady(true));
  }, []);

  const reloadTree = useCallback(async () => {
    setLoadingTree(true);
    setTreeError(null);
    const result = await fetchCategoryTree(API_LOCALE);
    setLoadingTree(false);
    if (result.state === "denied") {
      setAccessDenied(true);
      setTreeError("دسترسی مجاز نیست");
      setFlatNodes([]);
      return [];
    }
    if (result.state !== "ok" || !result.data) {
      setTreeError(result.message ?? "بارگذاری درخت ناموفق بود");
      setFlatNodes([]);
      return [];
    }
    setAccessDenied(false);
    const mapped = toTreeNodes(result.data);
    setFlatNodes(mapped);
    return mapped;
  }, []);

  useEffect(() => {
    if (!ready) return;
    void reloadTree();
  }, [ready, reloadTree]);

  useEffect(() => {
    if (!categoryId || !ready) {
      setWorkspace(null);
      setWorkspaceError(null);
      setDraft(null);
      formMode.resetToView();
      setSlugFieldError(null);
      return;
    }
    let cancelled = false;
    setWorkspaceLoading(true);
    setWorkspaceError(null);
    formMode.resetToView();
    setSlugFieldError(null);
    void fetchCategoryWorkspace(categoryId).then((result) => {
      if (cancelled) return;
      setWorkspaceLoading(false);
      if (result.state === "denied") {
        setWorkspace(null);
        setWorkspaceError("دسترسی مجاز نیست");
        return;
      }
      if (result.status === 404 || result.state !== "ok" || !result.data) {
        setWorkspace(null);
        setWorkspaceError(result.status === 404 ? "این دسته‌بندی پیدا نشد یا حذف شده است." : (result.message ?? "بارگذاری ناموفق بود"));
        return;
      }
      setWorkspace(result.data);
      const tr =
        result.data.translations.find((t) => t.locale === API_LOCALE) ?? result.data.translations[0];
      setDraft(draftFromWorkspace(result.data, tr?.name ?? "", tr?.slug ?? ""));
      setMobileWorkspace(true);
    });
    return () => {
      cancelled = true;
    };
    // resetToView is stable; omit formMode object to avoid reload loops
    // eslint-disable-next-line react-hooks/exhaustive-deps -- category open defaults to VIEW
  }, [categoryId, ready]);

  useEffect(() => {
    if (!categoryId || flatNodes.length === 0) return;
    const parentMap = buildParentMap(flatNodes);
    if (!parentMap.has(categoryId)) return;
    const ancestors = collectAncestorIds(parentMap, categoryId);
    setExpandedKeys((prev) => {
      const merged = new Set([...prev, ...ancestors]);
      return [...merged];
    });
  }, [categoryId, flatNodes]);

  const navigateToCategory = useCallback(
    (id: string, tab: TabId = "general") => {
      if (formMode.isDirty && !formMode.confirmDiscardIfDirty()) return;
      formMode.resetToView();
      setSlugFieldError(null);
      const href = tab === "general" ? `${basePath}/${id}` : `${basePath}/${id}/${tab}`;
      router.push(href);
      if (isNarrow) setMobileWorkspace(true);
    },
    [basePath, formMode, isNarrow, router],
  );

  const openCreateRoot = () => {
    setCreateParentId(null);
    setCreateOpen(true);
  };

  const openCreateChild = (parentId: string) => {
    setCreateParentId(parentId);
    setCreateOpen(true);
  };

  const handleCreate = async (input: { name: string; slug: string; parentId: string | null }) => {
    setCreateBusy(true);
    const result = await createCategory({
      parentCategoryId: input.parentId,
      name: input.name,
      slug: input.slug,
      locale: API_LOCALE,
      isVisible: true,
      sortOrder: 0,
    });
    setCreateBusy(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapCategoryMutationError(result));
      return;
    }
    toast.success("دسته‌بندی ایجاد شد");
    setCreateOpen(false);
    const next = await reloadTree();
    if (input.parentId) {
      setExpandedKeys((prev) => (prev.includes(input.parentId!) ? prev : [...prev, input.parentId!]));
    }
    navigateToCategory(result.data.categoryId);
    void next;
  };

  const handleDrop = async (request: CategoryDropRequest) => {
    const snapshot = flatNodes;
    const plan = resolveCategoryDropPlan(snapshot, request);
    if (!plan) {
      toast.error("جابه‌جایی نامعتبر است");
      return;
    }

    const optimistic = snapshot.map((n) => {
      if (n.id !== request.dragId) return n;
      return { ...n, parentId: plan.newParentId };
    });
    const withOrder = optimistic.map((n) => {
      const idx = plan.orderedSiblingIds.indexOf(n.id);
      if (idx >= 0 && n.parentId === plan.newParentId) {
        return { ...n, sortOrder: idx };
      }
      return n;
    });
    setFlatNodes(withOrder);

    if (plan.needsMove) {
      const moveResult = await moveCategory(request.dragId, { newParentId: plan.newParentId });
      if (moveResult.state !== "ok") {
        setFlatNodes(snapshot);
        toast.error(moveResult.message ?? "جابه‌جایی ناموفق بود");
        return;
      }
    }

    const reorderResult = await reorderCategories({
      parentId: plan.newParentId,
      orderedCategoryIds: plan.orderedSiblingIds,
    });
    if (reorderResult.state !== "ok") {
      setFlatNodes(snapshot);
      toast.error(reorderResult.message ?? "مرتب‌سازی ناموفق بود");
      await reloadTree();
      return;
    }

    toast.success("ترتیب به‌روز شد");
    await reloadTree();
  };

  const handleEnterEdit = () => {
    if (!workspace || !draft) return;
    const tr =
      workspace.translations.find((t) => t.locale === API_LOCALE) ?? workspace.translations[0];
    setDraft(draftFromWorkspace(workspace, tr?.name ?? draft.name, tr?.slug ?? draft.slug));
    setSlugFieldError(null);
    formMode.onEdit();
  };

  const handleCancelEdit = () => {
    if (!formMode.confirmDiscardIfDirty()) return;
    if (workspace) {
      const tr =
        workspace.translations.find((t) => t.locale === API_LOCALE) ?? workspace.translations[0];
      setDraft(draftFromWorkspace(workspace, tr?.name ?? "", tr?.slug ?? ""));
    }
    setSlugFieldError(null);
    formMode.onCancel();
  };

  const handleSave = async () => {
    if (!categoryId || !workspace || !draft) return;
    setSaveBusy(true);
    setSlugFieldError(null);

    const nameChanged =
      draft.name.trim()
      !== (workspace.translations.find((t) => t.locale === API_LOCALE)?.name
        ?? workspace.translations[0]?.name
        ?? "");
    const slugChanged =
      draft.slug.trim()
      !== (workspace.translations.find((t) => t.locale === API_LOCALE)?.slug
        ?? workspace.translations[0]?.slug
        ?? "");
    const coreChanged =
      draft.status !== workspace.status
      || draft.sortOrder !== workspace.sortOrder
      || draft.isVisible !== workspace.isVisible;
    const parentChanged = draft.parentId !== workspace.parentCategoryId;

    if (parentChanged) {
      const moveResult = await moveCategory(categoryId, {
        newParentId: draft.parentId,
        expectedUpdatedAt: workspace.updatedAt,
      });
      if (moveResult.state !== "ok") {
        setSaveBusy(false);
        toast.error(mapCategoryMutationError(moveResult));
        return;
      }
      if (moveResult.data) setWorkspace(moveResult.data);
    }

    if (coreChanged) {
      const coreResult = await updateCategoryCore(categoryId, {
        status: draft.status,
        sortOrder: draft.sortOrder,
        isVisible: draft.isVisible,
        expectedUpdatedAt: undefined,
      });
      if (coreResult.state !== "ok" || !coreResult.data) {
        setSaveBusy(false);
        toast.error(mapCategoryMutationError(coreResult));
        return;
      }
      setWorkspace(coreResult.data);
    }

    if (nameChanged || slugChanged) {
      const trResult = await upsertCategoryTranslation(categoryId, {
        locale: API_LOCALE,
        name: draft.name.trim(),
        slug: draft.slug.trim(),
      });
      if (trResult.state !== "ok" || !trResult.data) {
        setSaveBusy(false);
        const mapped = mapCategoryMutationError(trResult);
        setSlugFieldError(mapped);
        toast.error(mapped);
        return;
      }
    }

    const refreshed = await fetchCategoryWorkspace(categoryId);
    setSaveBusy(false);
    if (refreshed.state === "ok" && refreshed.data) {
      setWorkspace(refreshed.data);
      const tr =
        refreshed.data.translations.find((t) => t.locale === API_LOCALE)
        ?? refreshed.data.translations[0];
      setDraft(draftFromWorkspace(refreshed.data, tr?.name ?? "", tr?.slug ?? ""));

      // برچسب درخت را بدون ریست expanded/selected به‌روز کن
      if (tr) {
        setFlatNodes((prev) =>
          prev.map((n) =>
            n.id === categoryId
              ? { ...n, name: tr.name, slug: tr.slug, status: refreshed.data!.status, sortOrder: refreshed.data!.sortOrder, isVisible: refreshed.data!.isVisible, parentId: refreshed.data!.parentCategoryId }
              : n,
          ),
        );
      }
    }

    formMode.onSaved();
    toast.success("دسته‌بندی ذخیره شد");
  };

  const selectedNode = categoryId ? flatNodes.find((n) => n.id === categoryId) : undefined;
  const pathNames = categoryId ? buildCategoryPath(flatNodes, categoryId) : [];
  const parentName = workspace?.parentCategoryId
    ? flatNodes.find((n) => n.id === workspace.parentCategoryId)?.name || "—"
    : "ریشه";
  const childrenCount = categoryId ? countDirectChildren(flatNodes, categoryId) : 0;
  const activeTranslation =
    workspace?.translations.find((t) => t.locale === API_LOCALE) ?? workspace?.translations[0];
  const storefrontRoute = activeTranslation?.slug
    ? buildStorefrontCategoryRoute("fa", activeTranslation.slug)
    : selectedNode?.slug
      ? buildStorefrontCategoryRoute("fa", selectedNode.slug)
      : null;

  const createParentName = createParentId
    ? flatNodes.find((n) => n.id === createParentId)?.name || null
    : null;

  const parentOptions = useMemo(() => {
    if (!categoryId) return [{ id: null as string | null, label: "ریشه" }];
    const blocked = collectDescendantIds(flatNodes, categoryId);
    blocked.add(categoryId);
    const opts: { id: string | null; label: string }[] = [{ id: null, label: "ریشه" }];
    for (const n of flatNodes) {
      if (blocked.has(n.id)) continue;
      opts.push({ id: n.id, label: n.name });
    }
    return opts;
  }, [categoryId, flatNodes]);

  const showTreePane = !isNarrow || !mobileWorkspace || !categoryId;
  const showWorkspacePane = !isNarrow || (mobileWorkspace && Boolean(categoryId));
  const isEdit = formMode.mode === "edit";

  if (!ready) {
    return <div className="p-6 text-sm text-slate-500">در حال آماده‌سازی…</div>;
  }

  return (
    <div
      className="flex min-h-[calc(100vh-7rem)] flex-col gap-4 lg:flex-row-reverse"
      data-testid="category-admin-screen"
      data-layout={isNarrow ? "mobile" : "desktop"}
      data-form-mode={categoryId ? formMode.mode : undefined}
    >
      {showTreePane ? (
        <aside className="w-full shrink-0 lg:w-[360px] xl:w-[400px]" data-testid="category-tree-pane">
          <AppCategoryTree
            nodes={flatNodes}
            expandedKeys={expandedKeys}
            selectedKeys={categoryId ? [categoryId] : []}
            onExpandedKeysChange={setExpandedKeys}
            onSelect={(id) => navigateToCategory(id, activeTab === "general" ? "general" : activeTab)}
            onDropRequest={handleDrop}
            searchQuery={searchQuery}
            onSearchQueryChange={setSearchQuery}
            loading={loadingTree}
            error={treeError}
            onRetry={() => void reloadTree()}
            onCreateRoot={openCreateRoot}
            onCreateChild={openCreateChild}
            direction="rtl"
            virtualHeight={isNarrow ? 360 : 520}
          />
        </aside>
      ) : null}

      {showWorkspacePane ? (
        <section
          className="min-w-0 flex-1 rounded-2xl border border-gray-200 bg-white shadow-sm"
          data-testid="category-workspace-pane"
        >
          {!categoryId ? (
            <div className="flex h-full min-h-[320px] flex-col items-center justify-center gap-3 p-8 text-center text-slate-500">
              <strong className="text-base text-slate-800">یک دسته‌بندی را از درخت انتخاب کنید</strong>
              <p className="max-w-md text-sm">
                برای مشاهده خلاصه، ترجمه‌ها و سایر بخش‌ها، روی نام دسته‌بندی کلیک کنید.
              </p>
            </div>
          ) : workspaceLoading ? (
            <div className="p-8 text-sm text-slate-500" data-testid="category-workspace-loading">
              در حال بارگذاری workspace…
            </div>
          ) : workspaceError ? (
            <div className="space-y-3 p-8" data-testid="category-workspace-error">
              {isNarrow ? (
                <button
                  type="button"
                  className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm"
                  onClick={() => {
                    setMobileWorkspace(false);
                    router.push(basePath);
                  }}
                >
                  بازگشت به دسته‌بندی‌ها
                </button>
              ) : null}
              <p className="text-sm text-red-600">{workspaceError}</p>
              <Link href={basePath} className="text-sm font-medium text-[#2563EB]">
                بازگشت به فهرست
              </Link>
            </div>
          ) : workspace ? (
            <div className="flex h-full flex-col">
              <header className="sticky top-0 z-10 border-b border-gray-200 bg-white/95 px-4 py-4 backdrop-blur lg:px-6">
                {isNarrow ? (
                  <button
                    type="button"
                    className="mb-3 inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-3 text-sm"
                    onClick={() => {
                      if (formMode.isDirty && !formMode.confirmDiscardIfDirty()) return;
                      setMobileWorkspace(false);
                      formMode.resetToView();
                      router.push(basePath);
                    }}
                    data-testid="category-workspace-back"
                  >
                    بازگشت به دسته‌بندی‌ها
                  </button>
                ) : null}
                <div className="text-xs text-slate-500" data-testid="category-breadcrumb">
                  {["دسته‌بندی‌ها", ...pathNames].join(" / ")}
                </div>
                <div className="mt-2 flex flex-wrap items-center justify-between gap-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <h1 className="text-xl font-bold text-slate-900" data-testid="category-workspace-title">
                      {isEdit && draft
                        ? draft.name || activeTranslation?.name || selectedNode?.name || "دسته‌بندی"
                        : activeTranslation?.name || selectedNode?.name || "دسته‌بندی"}
                    </h1>
                    <span
                      className={`rounded-full border px-2.5 py-0.5 text-xs font-semibold ${statusBadgeClass(isEdit && draft ? draft.status : workspace.status)}`}
                    >
                      {categoryStatusLabel(isEdit && draft ? draft.status : workspace.status)}
                    </span>
                    <span
                      className="rounded-full bg-slate-100 px-2.5 py-0.5 text-[11px] font-medium text-slate-600"
                      data-testid="category-form-mode-badge"
                    >
                      {isEdit ? "ویرایش" : "مشاهده"}
                    </span>
                  </div>
                  {!isEdit && formMode.canEdit ? (
                    <button
                      type="button"
                      className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-semibold text-slate-800 hover:bg-slate-50"
                      onClick={handleEnterEdit}
                      data-testid="category-edit-action"
                    >
                      ویرایش
                    </button>
                  ) : null}
                </div>
                {storefrontRoute && !isEdit ? (
                  <p className="mt-1 text-xs text-slate-500" dir="ltr" data-testid="category-storefront-route">
                    مسیر ویترین: {storefrontRoute}
                  </p>
                ) : null}
              </header>

              <nav
                className="flex gap-1 overflow-x-auto border-b border-gray-100 px-3 py-2 lg:px-5"
                aria-label="بخش‌های دسته‌بندی"
                data-testid="category-workspace-tabs"
              >
                {TABS.map((tab) => {
                  const href =
                    tab.id === "general"
                      ? `${basePath}/${categoryId}`
                      : `${basePath}/${categoryId}/${tab.id}`;
                  const active = activeTab === tab.id;
                  return (
                    <Link
                      key={tab.id}
                      href={href}
                      onClick={(e) => {
                        if (formMode.isDirty && !formMode.confirmDiscardIfDirty()) {
                          e.preventDefault();
                          return;
                        }
                        if (formMode.isDirty) formMode.onCancel();
                      }}
                      className={
                        active
                          ? "inline-flex min-h-10 shrink-0 items-center rounded-xl bg-[#2563EB] px-3 text-sm font-semibold text-white"
                          : "inline-flex min-h-10 shrink-0 items-center rounded-xl px-3 text-sm font-medium text-slate-600 hover:bg-slate-50"
                      }
                      aria-current={active ? "page" : undefined}
                      data-testid={`category-tab-${tab.id}`}
                    >
                      {tab.label}
                      {!tab.implemented ? (
                        <span className="ms-1 text-[10px] opacity-70">به‌زودی</span>
                      ) : null}
                    </Link>
                  );
                })}
              </nav>

              <div className="flex-1 p-4 lg:p-6">
                {activeTab === "general" ? (
                  isEdit && draft ? (
                    <GeneralEditForm
                      draft={draft}
                      fieldError={slugFieldError}
                      parentOptions={parentOptions}
                      busy={saveBusy}
                      onChange={(next) => {
                        setDraft(next);
                        formMode.markDirty();
                        if (slugFieldError) setSlugFieldError(null);
                      }}
                      onSave={() => void handleSave()}
                      onCancel={handleCancelEdit}
                    />
                  ) : (
                    <GeneralViewSummary
                      workspace={workspace}
                      parentName={parentName}
                      childrenCount={childrenCount}
                      productCount={selectedNode?.productCount ?? null}
                      storefrontRoute={storefrontRoute}
                      activeLocaleName={activeTranslation?.name || selectedNode?.name || ""}
                      activeLocaleSlug={activeTranslation?.slug || selectedNode?.slug || ""}
                    />
                  )
                ) : null}
                {activeTab === "translations" ? <TranslationsPanel workspace={workspace} /> : null}
                {activeTab !== "general" && activeTab !== "translations" ? <ComingSoonPanel /> : null}
              </div>
            </div>
          ) : null}
        </section>
      ) : null}

      <CreateCategoryDialog
        open={createOpen}
        parentId={createParentId}
        parentName={createParentName}
        busy={createBusy}
        onClose={() => setCreateOpen(false)}
        onSubmit={(input) => void handleCreate(input)}
      />
    </div>
  );
}
