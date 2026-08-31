"use client";

/**
 * صفحهٔ Admin Category: درخت + workspace با VIEW/EDIT صریح (T005-R1 / T006).
 * تب‌های عمومی، ترجمه‌ها، ویژگی‌ها، فیلترها، مگامنو و محصولات واقعی‌اند.
 * SEO/تنظیمات/تاریخچه فعلاً از ناوبری تب پنهان‌اند (SEO در ترجمه‌ها موجود است).
 */

import Link from "next/link";
import { PanelLeftOpen } from "lucide-react";
import { useParams, usePathname, useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { toast } from "react-toastify";
import {
  AppCategoryTree,
  buildCategoryPath,
  buildParentMap,
  buildTranslationStatuses,
  canAddCategoryChild,
  collectAncestorIds,
  countDirectChildren,
  MAX_CATEGORY_DEPTH_MESSAGE_FA,
  resolveCategoryDropPlan,
  translationReadinessLabel,
  useAdminFormMode,
  type AppCategoryTreeNode,
  type CategoryDropRequest,
  type LocaleTranslationStatus,
  type TranslationReadiness,
} from "../../design-system";
import { prepareAdminDevActor } from "./admin-api.ts";
import { CatalogTagsCard } from "./catalog-tags-card.tsx";
import {
  buildStorefrontCategoryRoute,
  createCategory,
  fetchCategoryTree,
  fetchCategoryWorkspace,
  mapCategoryMutationError,
  moveCategory,
  parseMetaKeywords,
  reorderCategories,
  serializeMetaKeywords,
  slugifyCategoryName,
  updateCategoryCore,
  upsertCategoryTranslation,
  type CategoryPublicationStatus,
  type CategoryTranslationDto,
  type CategoryTreeNodeDto,
  type CategoryWorkspaceSummary,
} from "./catalog-category-api.ts";
import { CategoryAttributesPanel } from "./category-attributes-panel.tsx";
import { CategoryFacetsPanel } from "./category-facets-panel.tsx";
import { CategoryMegaMenuPanel } from "./category-mega-menu-panel.tsx";
import { CategoryProductsPanel } from "./category-products-panel.tsx";
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mediaPreviewUrl, type MediaAssetDto } from "./media-api.ts";
import { mapAdminErrorMessage } from "./admin-error-map.ts";

const API_LOCALE = "fa-IR";

/** زبان‌های پشتیبانی‌شدهٔ Admin Catalog — ویترین i18n فقط fa/en دارد؛ Catalog سه locale دارد. */
const UI_LOCALES = ["fa-IR", "en-US", "ar-SA"] as const;

const LOCALE_DISPLAY: Record<string, string> = {
  "fa-IR": "فارسی",
  "en-US": "English",
  "ar-SA": "العربية",
};

const TABS = [
  { id: "general", label: "عمومی", implemented: true },
  { id: "translations", label: "ترجمه‌ها", implemented: true },
  { id: "attributes", label: "ویژگی‌ها", implemented: true },
  { id: "facets", label: "فیلترهای صفحه محصولات", implemented: true },
  { id: "mega-menu", label: "مگامنو", implemented: true },
  { id: "products", label: "محصولات", implemented: true },
] as const;

type TabId = (typeof TABS)[number]["id"];
type EditSurface = "general" | "translations" | "attributes" | "facets" | "mega-menu";

interface GeneralDraft {
  name: string;
  slug: string;
  status: CategoryPublicationStatus;
  sortOrder: number;
  isVisible: boolean;
  parentId: string | null;
  slugTouched: boolean;
}

interface TranslationDraft {
  locale: string;
  name: string;
  slug: string;
  shortDescription: string;
  description: string;
  seoTitle: string;
  seoDescription: string;
  metaKeywords: string;
  slugTouched: boolean;
  /** true وقتی کاربر «ایجاد ترجمه» زده و هنوز ردیف ذخیره نشده. */
  isCreate: boolean;
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

/** برچسب وضعیت فقط برای workspace — درخت را تغییر نمی‌دهد. */
function workspaceStatusLabel(status: CategoryPublicationStatus): string {
  if (status === "Draft") return "پیش‌نویس";
  if (status === "Published") return "منتشرشده";
  return "بایگانی‌شده";
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

function localeUiSegment(locale: string): string {
  if (locale === "en-US" || locale === "en") return "en";
  if (locale === "ar-SA" || locale === "ar") return "ar";
  return "fa";
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

function emptyTranslationDraft(locale: string, isCreate = true): TranslationDraft {
  return {
    locale,
    name: "",
    slug: "",
    shortDescription: "",
    description: "",
    seoTitle: "",
    seoDescription: "",
    metaKeywords: "",
    slugTouched: false,
    isCreate,
  };
}

function translationDraftFromRow(row: CategoryTranslationDto): TranslationDraft {
  return {
    locale: row.locale,
    name: row.name ?? "",
    slug: row.slug ?? "",
    shortDescription: row.shortDescription ?? "",
    description: row.description ?? "",
    seoTitle: row.seoTitle ?? "",
    seoDescription: row.seoDescription ?? "",
    metaKeywords: row.metaKeywords ?? "",
    slugTouched: true,
    isCreate: false,
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
            className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
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

function readinessChipClass(readiness: TranslationReadiness): string {
  if (readiness === "complete") return "rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700";
  if (readiness === "partial") return "rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold text-amber-800";
  return "rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600";
}

/** نمایش فقط‌خواندنی کلمات کلیدی به‌صورت تگ. */
function MetaKeywordsViewChips({ value }: { value: string | null | undefined }) {
  const tags = parseMetaKeywords(value);
  if (tags.length === 0) {
    return <span className="text-sm text-slate-500">—</span>;
  }
  return (
    <ul className="flex flex-wrap gap-2" data-testid="translation-view-meta-keywords">
      {tags.map((tag) => (
        <li
          key={tag}
          className="inline-flex items-center rounded-full bg-slate-100 px-3 py-1 text-xs font-medium text-slate-800"
          data-testid={`translation-view-meta-keyword-${tag}`}
        >
          {tag}
        </li>
      ))}
    </ul>
  );
}

/**
 * ورودی کلمات کلیدی SEO — تگ‌های قابل حذف؛ Enter / ویرگول اضافه می‌کند.
 * ذخیره همچنان رشتهٔ metaKeywords سمت Host است.
 */
function MetaKeywordsTagInput({
  value,
  onChange,
  disabled,
}: {
  value: string;
  onChange: (next: string) => void;
  disabled?: boolean;
}) {
  const [draft, setDraft] = useState("");
  const tags = useMemo(() => parseMetaKeywords(value), [value]);

  function commitDraft(raw: string) {
    const nextTags = parseMetaKeywords([...tags, ...parseMetaKeywords(raw)].join(","));
    if (nextTags.length === tags.length && raw.trim() === "") return;
    onChange(serializeMetaKeywords(nextTags));
    setDraft("");
  }

  function removeTag(tag: string) {
    onChange(serializeMetaKeywords(tags.filter((t) => t !== tag)));
  }

  return (
    <div
      className="mt-1 rounded-xl border border-gray-200 bg-white px-2 py-2 focus-within:ring-2 focus-within:ring-blue-500"
      data-testid="translation-edit-meta-keywords"
    >
      <ul className="flex flex-wrap gap-2" data-testid="translation-edit-meta-keyword-chips">
        {tags.map((tag) => (
          <li
            key={tag}
            className="inline-flex items-center gap-1.5 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-800"
            data-testid={`translation-edit-meta-keyword-${tag}`}
          >
            <span>{tag}</span>
            <button
              type="button"
              disabled={disabled}
              className="rounded-full px-1 text-red-600 hover:bg-red-50 disabled:opacity-50"
              aria-label={`حذف ${tag}`}
              data-testid={`translation-edit-meta-keyword-remove-${tag}`}
              onClick={() => removeTag(tag)}
            >
              ×
            </button>
          </li>
        ))}
      </ul>
      <input
        className="mt-1 min-h-9 w-full border-0 bg-transparent px-1 text-sm focus:outline-none"
        value={draft}
        disabled={disabled}
        placeholder={tags.length === 0 ? "کلمه را بنویسید و Enter بزنید" : "افزودن کلمه…"}
        data-testid="translation-edit-meta-keywords-input"
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === "," || e.key === "،") {
            e.preventDefault();
            commitDraft(draft);
            return;
          }
          if (e.key === "Backspace" && draft === "" && tags.length > 0) {
            e.preventDefault();
            removeTag(tags[tags.length - 1]!);
          }
        }}
        onBlur={() => {
          if (draft.trim()) commitDraft(draft);
        }}
      />
      <p className="mt-1 px-1 text-[11px] text-slate-500">با Enter یا ویرگول اضافه کنید؛ روی × برای حذف.</p>
    </div>
  );
}

/** اسلات رسانهٔ دسته — انتخاب/آپلود/حذف ارجاع بدون نمایش Guid خام. */
function CategoryMediaField({
  label,
  role,
  mediaAssetId,
  editable,
  busy,
  onSelect,
  onClear,
}: {
  label: string;
  role: "image" | "icon" | "banner";
  mediaAssetId: string | null;
  editable: boolean;
  busy: boolean;
  onSelect: () => void;
  onClear: () => void;
}) {
  const preview = mediaPreviewUrl(mediaAssetId);
  return (
    <div
      className="rounded-2xl border border-gray-200 bg-white p-4"
      data-testid={`category-media-${role}`}
    >
      <div className="text-xs font-medium text-slate-500">{label}</div>
      <div
        className="mt-3 flex min-h-28 flex-col items-center justify-center gap-2 rounded-xl border border-gray-100 bg-slate-50 p-3 text-sm text-slate-600"
        data-testid={`category-media-status-${role}`}
      >
        {preview ? (
          <>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={preview}
              alt={label}
              className="max-h-24 w-full object-contain"
              data-testid={`category-media-preview-${role}`}
            />
            <span>{label} متصل است</span>
          </>
        ) : (
          <span>{`هنوز ${label}ی تنظیم نشده`}</span>
        )}
      </div>
      {editable ? (
        <div className="mt-3 flex flex-wrap gap-2">
          <button
            type="button"
            disabled={busy}
            className="min-h-10 rounded-xl border border-gray-200 bg-white px-3 text-sm font-medium hover:bg-slate-50 disabled:opacity-50"
            onClick={onSelect}
            data-testid={`category-media-select-${role}`}
          >
            {mediaAssetId ? "تغییر" : "انتخاب / آپلود"}
          </button>
          {mediaAssetId ? (
            <button
              type="button"
              disabled={busy}
              className="min-h-10 rounded-xl border border-red-200 px-3 text-sm text-red-700 hover:bg-red-50 disabled:opacity-50"
              onClick={onClear}
              data-testid={`category-media-clear-${role}`}
            >
              حذف ارجاع
            </button>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

type CategoryMediaRole = "image" | "icon" | "banner";

function CategoryMediaSection({
  workspace,
  editable,
  onWorkspaceChange,
}: {
  workspace: CategoryWorkspaceSummary;
  editable: boolean;
  onWorkspaceChange: (next: CategoryWorkspaceSummary) => void;
}) {
  const [pickerRole, setPickerRole] = useState<CategoryMediaRole | null>(null);
  const [busy, setBusy] = useState(false);

  async function assign(role: CategoryMediaRole, asset: MediaAssetDto) {
    setBusy(true);
    const patch =
      role === "image"
        ? { imageMediaAssetId: asset.mediaAssetId }
        : role === "icon"
          ? { iconMediaAssetId: asset.mediaAssetId }
          : { bannerMediaAssetId: asset.mediaAssetId };
    const result = await updateCategoryCore(workspace.categoryId, {
      ...patch,
      expectedUpdatedAt: workspace.updatedAt,
    });
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapCategoryMutationError(result));
      return;
    }
    onWorkspaceChange(result.data);
    setPickerRole(null);
    toast.success("رسانهٔ دسته به‌روز شد");
  }

  async function clear(role: CategoryMediaRole) {
    setBusy(true);
    const patch =
      role === "image"
        ? { clearImage: true }
        : role === "icon"
          ? { clearIcon: true }
          : { clearBanner: true };
    const result = await updateCategoryCore(workspace.categoryId, {
      ...patch,
      expectedUpdatedAt: workspace.updatedAt,
    });
    setBusy(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapAdminErrorMessage(result.message ?? "host-unreachable", "fa"));
      return;
    }
    onWorkspaceChange(result.data);
    toast.success("ارجاع رسانه برداشته شد");
  }

  return (
    <>
      <div className="grid gap-3 sm:grid-cols-3" data-testid="category-media-section">
        <CategoryMediaField
          label="تصویر"
          role="image"
          mediaAssetId={workspace.imageMediaAssetId}
          editable={editable}
          busy={busy}
          onSelect={() => setPickerRole("image")}
          onClear={() => void clear("image")}
        />
        <CategoryMediaField
          label="آیکن"
          role="icon"
          mediaAssetId={workspace.iconMediaAssetId}
          editable={editable}
          busy={busy}
          onSelect={() => setPickerRole("icon")}
          onClear={() => void clear("icon")}
        />
        <CategoryMediaField
          label="بنر"
          role="banner"
          mediaAssetId={workspace.bannerMediaAssetId}
          editable={editable}
          busy={busy}
          onSelect={() => setPickerRole("banner")}
          onClear={() => void clear("banner")}
        />
      </div>
      <MediaLibraryDialog
        open={pickerRole != null}
        title={
          pickerRole === "icon"
            ? "انتخاب آیکن دسته"
            : pickerRole === "banner"
              ? "انتخاب بنر دسته"
              : "انتخاب تصویر دسته"
        }
        selectionMode="single"
        onClose={() => {
          if (!busy) setPickerRole(null);
        }}
        onConfirm={async (assets) => {
          if (!pickerRole || !assets[0]) return;
          await assign(pickerRole, assets[0]);
        }}
      />
    </>
  );
}

/** انتخابگر والد با جستجو روی مسیر/نام — بدون نمایش UUID. */
function ParentCategorySelector({
  options,
  value,
  onChange,
}: {
  options: { id: string | null; label: string }[];
  value: string | null;
  onChange: (next: string | null) => void;
}) {
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const selected = options.find((o) => o.id === value) ?? options[0];
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return options;
    return options.filter((o) => o.label.toLowerCase().includes(q));
  }, [options, query]);

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  return (
    <div className="relative" ref={rootRef} data-testid="category-edit-parent">
      <span className="block text-sm font-medium text-slate-700">والد</span>
      <button
        type="button"
        className="mt-1 flex min-h-11 w-full items-center justify-between rounded-xl border border-gray-200 bg-white px-3 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500"
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => {
          setOpen((v) => !v);
          setQuery("");
        }}
        data-testid="category-edit-parent-trigger"
      >
        <span className="truncate text-start">{selected?.label ?? "ریشه"}</span>
        <span className="ms-2 text-slate-400" aria-hidden>
          ▾
        </span>
      </button>
      {open ? (
        <div
          className="absolute z-20 mt-1 max-h-64 w-full overflow-hidden rounded-xl border border-gray-200 bg-white shadow-lg"
          role="listbox"
          data-testid="category-edit-parent-list"
        >
          <div className="border-b border-gray-100 p-2">
            <input
              className="min-h-10 w-full rounded-lg border border-gray-200 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="جستجوی نام یا مسیر…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              autoFocus
              data-testid="category-edit-parent-search"
              aria-label="جستجوی دسته والد"
            />
          </div>
          <ul className="max-h-48 overflow-y-auto py-1">
            {filtered.length === 0 ? (
              <li className="px-3 py-2 text-sm text-slate-400">موردی یافت نشد</li>
            ) : (
              filtered.map((opt) => {
                const active = opt.id === value;
                return (
                  <li key={opt.id ?? "root"}>
                    <button
                      type="button"
                      role="option"
                      aria-selected={active}
                      className={
                        active
                          ? "flex min-h-10 w-full px-3 py-2 text-start text-sm font-semibold text-[#2563EB] bg-blue-50"
                          : "flex min-h-10 w-full px-3 py-2 text-start text-sm text-slate-700 hover:bg-slate-50"
                      }
                      onClick={() => {
                        onChange(opt.id);
                        setOpen(false);
                        setQuery("");
                      }}
                      data-testid={
                        opt.id == null
                          ? "category-parent-option-root"
                          : `category-parent-option-${opt.id}`
                      }
                    >
                      {opt.label}
                    </button>
                  </li>
                );
              })
            )}
          </ul>
        </div>
      ) : null}
      {/* مقدار انتخاب‌شده برای تست‌ها؛ UUID در UI برچسب نیست */}
      <input type="hidden" value={value ?? ""} data-testid="category-edit-parent-value" readOnly />
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
  translationStatuses,
  canEditMedia,
  onWorkspaceChange,
}: {
  workspace: CategoryWorkspaceSummary;
  parentName: string;
  childrenCount: number;
  productCount: number | null;
  storefrontRoute: string | null;
  activeLocaleName: string;
  activeLocaleSlug: string;
  translationStatuses: LocaleTranslationStatus[];
  canEditMedia: boolean;
  onWorkspaceChange: (next: CategoryWorkspaceSummary) => void;
}) {
  const completeCount = translationStatuses.filter((c) => c.readiness === "complete").length;

  return (
    <div className="space-y-4" data-testid="category-general-summary" data-form-mode="view">
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <SummaryCard label="نام دسته در زبان فعلی" value={activeLocaleName || "—"} />
        <SummaryCard label="دسته والد" value={parentName} />
        <SummaryCard label="وضعیت" value={workspaceStatusLabel(workspace.status)} />
        <SummaryCard label="ترتیب نمایش" value={String(workspace.sortOrder)} />
        <SummaryCard label="قابل نمایش بودن" value={workspace.isVisible ? "بله" : "خیر"} />
        <SummaryCard label="نامک" value={activeLocaleSlug || "—"} ltr />
        <SummaryCard
          label="وضعیت ترجمه‌ها"
          value={`${completeCount} از ${translationStatuses.length} کامل`}
        />
        <SummaryCard label="تعداد زیرمجموعه‌ها" value={String(childrenCount)} />
        {productCount != null ? (
          <SummaryCard label="تعداد محصولات" value={String(productCount)} />
        ) : null}
        {storefrontRoute ? (
          <SummaryCard label="آدرس عمومی دسته" value={storefrontRoute} ltr />
        ) : null}
      </div>

      <div className="rounded-2xl border border-gray-200 bg-white p-4" data-testid="category-translation-status-summary">
        <div className="text-xs font-medium text-slate-500">خلاصه ترجمه‌ها</div>
        <ul className="mt-3 flex flex-wrap gap-2">
          {translationStatuses.map((row) => (
            <li
              key={row.locale}
              className="inline-flex items-center gap-2 rounded-full border border-gray-100 bg-slate-50 px-3 py-1.5 text-xs"
              data-testid={`general-translation-chip-${row.locale}`}
            >
              <span className="font-medium text-slate-800">{row.label}</span>
              <span className={readinessChipClass(row.readiness)}>
                {translationReadinessLabel(row.readiness)}
              </span>
            </li>
          ))}
        </ul>
      </div>

      <CategoryMediaSection
        workspace={workspace}
        editable={canEditMedia}
        onWorkspaceChange={onWorkspaceChange}
      />

      <CatalogTagsCard ownerKind="category" ownerId={workspace.categoryId} canEdit={canEditMedia} />
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
  workspace,
  onWorkspaceChange,
}: {
  draft: GeneralDraft;
  fieldError: string | null;
  parentOptions: { id: string | null; label: string }[];
  busy: boolean;
  onChange: (next: GeneralDraft) => void;
  onSave: () => void;
  onCancel: () => void;
  workspace: CategoryWorkspaceSummary;
  onWorkspaceChange: (next: CategoryWorkspaceSummary) => void;
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
            aria-describedby={fieldError ? "category-slug-error" : undefined}
          />
          {fieldError ? (
            <span
              id="category-slug-error"
              className="mt-1 block text-xs text-red-600"
              data-testid="category-slug-error"
              role="alert"
            >
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
            <option value="Archived">بایگانی‌شده</option>
          </select>
        </label>

        <label className="block text-sm font-medium text-slate-700">
          ترتیب نمایش
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
          قابل نمایش بودن در ویترین
        </label>

        <ParentCategorySelector
          options={parentOptions}
          value={draft.parentId}
          onChange={(parentId) => onChange({ ...draft, parentId })}
        />
      </div>

      <CategoryMediaSection
        workspace={workspace}
        editable
        onWorkspaceChange={onWorkspaceChange}
      />

      <CatalogTagsCard ownerKind="category" ownerId={workspace.categoryId} canEdit={!busy} />

      <div className="sticky bottom-0 flex flex-wrap items-center justify-end gap-2 border-t border-gray-100 bg-white/95 py-4 backdrop-blur">
        <button
          type="button"
          className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
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

function TranslationsPanel({
  workspace,
  selectedLocale,
  draft,
  isEdit,
  canEdit,
  fieldError,
  busy,
  onSelectLocale,
  onCreateTranslation,
  onChange,
  onSave,
  onCancel,
}: {
  workspace: CategoryWorkspaceSummary;
  selectedLocale: string;
  draft: TranslationDraft | null;
  isEdit: boolean;
  canEdit: boolean;
  fieldError: string | null;
  busy: boolean;
  onSelectLocale: (locale: string) => void;
  onCreateTranslation: () => void;
  onChange: (next: TranslationDraft) => void;
  onSave: () => void;
  onCancel: () => void;
}) {
  const statuses = buildTranslationStatuses(workspace.translations, UI_LOCALES);
  const existing = workspace.translations.find((t) => t.locale === selectedLocale);
  const readiness =
    statuses.find((s) => s.locale === selectedLocale)?.readiness
    ?? ("missing" as TranslationReadiness);
  const activeDraft = draft && draft.locale === selectedLocale ? draft : null;
  const previewSlug = isEdit && activeDraft ? activeDraft.slug.trim() : (existing?.slug ?? "").trim();
  const preview = previewSlug
    ? buildStorefrontCategoryRoute(localeUiSegment(selectedLocale), previewSlug)
    : null;

  return (
    <div className="space-y-4" data-testid="category-translations-panel">
      <p className="text-sm text-slate-500">
        این یک دسته‌بندی است با چند نسخهٔ زبانی — نه چند رکورد جدا.
      </p>

      <div
        className="flex flex-wrap gap-2"
        role="tablist"
        aria-label="زبان‌های ترجمه"
        data-testid="category-locale-switcher"
      >
        {statuses.map((row) => {
          const selected = row.locale === selectedLocale;
          return (
            <button
              key={row.locale}
              type="button"
              role="tab"
              aria-selected={selected}
              className={
                selected
                  ? "inline-flex min-h-11 flex-col items-start gap-1 rounded-2xl border border-[#2563EB] bg-blue-50 px-3 py-2 text-start"
                  : "inline-flex min-h-11 flex-col items-start gap-1 rounded-2xl border border-gray-200 bg-white px-3 py-2 text-start hover:bg-slate-50"
              }
              onClick={() => onSelectLocale(row.locale)}
              data-testid={`translation-locale-${row.locale}`}
            >
              <span className="text-sm font-semibold text-slate-900">{row.label}</span>
              <span className={readinessChipClass(row.readiness)} data-testid={`translation-status-${row.locale}`}>
                {translationReadinessLabel(row.readiness)}
              </span>
            </button>
          );
        })}
      </div>

      {readiness === "missing" && !isEdit ? (
        <div
          className="rounded-2xl border border-dashed border-gray-200 bg-slate-50 p-6 text-center"
          data-testid="category-translation-missing"
          data-form-mode="view"
        >
          <p className="text-sm text-slate-600">
            ترجمهٔ {LOCALE_DISPLAY[selectedLocale] ?? selectedLocale} هنوز ایجاد نشده است.
          </p>
          {canEdit ? (
            <button
              type="button"
              className="mt-4 inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95"
              onClick={onCreateTranslation}
              data-testid="category-translation-create"
            >
              ایجاد ترجمه
            </button>
          ) : null}
        </div>
      ) : null}

      {readiness !== "missing" && !isEdit ? (
        <div className="space-y-4" data-testid="category-translation-view" data-form-mode="view">
          <div className="text-sm text-slate-500">
            وضعیت:{" "}
            <span className={readinessChipClass(readiness)}>
              {translationReadinessLabel(readiness)}
            </span>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <SummaryCard label="نام" value={existing?.name || "—"} />
            <SummaryCard label="نامک" value={existing?.slug || "—"} ltr />
            <SummaryCard label="توضیح کوتاه" value={existing?.shortDescription || "—"} />
            <SummaryCard label="توضیح" value={existing?.description || "—"} />
            <SummaryCard label="عنوان SEO" value={existing?.seoTitle || "—"} />
            <SummaryCard label="توضیح SEO" value={existing?.seoDescription || "—"} />
            <div className="rounded-2xl border border-gray-200 bg-white p-4 sm:col-span-2">
              <div className="text-xs font-medium text-slate-500">کلمات کلیدی</div>
              <div className="mt-2">
                <MetaKeywordsViewChips value={existing?.metaKeywords} />
              </div>
            </div>
            {preview ? <SummaryCard label="آدرس عمومی" value={preview} ltr /> : null}
          </div>
        </div>
      ) : null}

      {isEdit && activeDraft ? (
        <div className="space-y-4" data-testid="category-translation-edit" data-form-mode="edit">
          <div className="grid gap-4 sm:grid-cols-2">
            <label className="block text-sm font-medium text-slate-700">
              نام
              <input
                className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={activeDraft.name}
                onChange={(e) => {
                  const name = e.target.value;
                  onChange({
                    ...activeDraft,
                    name,
                    slug: activeDraft.slugTouched ? activeDraft.slug : slugifyCategoryName(name),
                  });
                }}
                data-testid="translation-edit-name"
                autoFocus={activeDraft.isCreate}
              />
            </label>

            <label className="block text-sm font-medium text-slate-700">
              نامک (Slug)
              <input
                className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                dir="ltr"
                value={activeDraft.slug}
                onChange={(e) =>
                  onChange({ ...activeDraft, slug: e.target.value, slugTouched: true })
                }
                data-testid="translation-edit-slug"
                aria-invalid={Boolean(fieldError)}
                aria-describedby={fieldError ? "translation-slug-error" : undefined}
              />
              {fieldError ? (
                <span
                  id="translation-slug-error"
                  className="mt-1 block text-xs text-red-600"
                  role="alert"
                  data-testid="translation-slug-error"
                >
                  {fieldError}
                </span>
              ) : null}
              {preview ? (
                <span
                  className="mt-1 block text-xs text-slate-500"
                  dir="ltr"
                  data-testid="translation-route-preview"
                >
                  پیش‌نمایش: {preview}
                </span>
              ) : null}
            </label>

            <label className="block text-sm font-medium text-slate-700 sm:col-span-2">
              توضیح کوتاه
              <textarea
                className="mt-1 min-h-20 w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={activeDraft.shortDescription}
                onChange={(e) => onChange({ ...activeDraft, shortDescription: e.target.value })}
                data-testid="translation-edit-short-description"
              />
            </label>

            <label className="block text-sm font-medium text-slate-700 sm:col-span-2">
              توضیح
              <textarea
                className="mt-1 min-h-28 w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={activeDraft.description}
                onChange={(e) => onChange({ ...activeDraft, description: e.target.value })}
                data-testid="translation-edit-description"
              />
            </label>

            <label className="block text-sm font-medium text-slate-700">
              عنوان SEO
              <input
                className="mt-1 min-h-11 w-full rounded-xl border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={activeDraft.seoTitle}
                onChange={(e) => onChange({ ...activeDraft, seoTitle: e.target.value })}
                data-testid="translation-edit-seo-title"
              />
            </label>

            <label className="block text-sm font-medium text-slate-700 sm:col-span-2">
              کلمات کلیدی
              <MetaKeywordsTagInput
                value={activeDraft.metaKeywords}
                onChange={(metaKeywords) => onChange({ ...activeDraft, metaKeywords })}
                disabled={busy}
              />
            </label>

            <label className="block text-sm font-medium text-slate-700 sm:col-span-2">
              توضیح SEO
              <textarea
                className="mt-1 min-h-20 w-full rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={activeDraft.seoDescription}
                onChange={(e) => onChange({ ...activeDraft, seoDescription: e.target.value })}
                data-testid="translation-edit-seo-description"
              />
            </label>
          </div>

          <div className="sticky bottom-0 flex flex-wrap items-center justify-end gap-2 border-t border-gray-100 bg-white/95 py-4 backdrop-blur">
            <button
              type="button"
              className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
              onClick={onCancel}
              disabled={busy}
              data-testid="translation-edit-cancel"
            >
              انصراف
            </button>
            <button
              type="button"
              className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95 disabled:opacity-50"
              onClick={onSave}
              disabled={busy || !activeDraft.name.trim() || !activeDraft.slug.trim()}
              data-testid="translation-edit-save"
            >
              {busy ? "در حال ذخیره…" : "ذخیره"}
            </button>
          </div>
        </div>
      ) : null}
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
  const [treePaneCollapsed, setTreePaneCollapsed] = useState(false);

  const [draft, setDraft] = useState<GeneralDraft | null>(null);
  const [translationDraft, setTranslationDraft] = useState<TranslationDraft | null>(null);
  const [selectedLocale, setSelectedLocale] = useState<string>(API_LOCALE);
  const [editSurface, setEditSurface] = useState<EditSurface>("general");
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
      setTranslationDraft(null);
      setSelectedLocale(API_LOCALE);
      setEditSurface("general");
      formMode.resetToView();
      setSlugFieldError(null);
      return;
    }
    let cancelled = false;
    setWorkspaceLoading(true);
    setWorkspaceError(null);
    formMode.resetToView();
    setSlugFieldError(null);
    setEditSurface("general");
    setSelectedLocale(API_LOCALE);
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
        setWorkspaceError(
          result.status === 404
            ? "این دسته‌بندی پیدا نشد یا حذف شده است."
            : (result.message ?? "بارگذاری ناموفق بود"),
        );
        return;
      }
      setWorkspace(result.data);
      const tr =
        result.data.translations.find((t) => t.locale === API_LOCALE) ?? result.data.translations[0];
      setDraft(draftFromWorkspace(result.data, tr?.name ?? "", tr?.slug ?? ""));
      const selTr = result.data.translations.find((t) => t.locale === API_LOCALE);
      setTranslationDraft(selTr ? translationDraftFromRow(selTr) : emptyTranslationDraft(API_LOCALE));
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

  const softRefreshTreeLabel = useCallback(
    (ws: CategoryWorkspaceSummary, locale: string) => {
      if (!categoryId) return;
      const tr = ws.translations.find((t) => t.locale === locale);
      // برچسب درخت فقط وقتی locale فعلی UI درخت (فارسی) عوض شود
      if (locale !== API_LOCALE || !tr) {
        setFlatNodes((prev) =>
          prev.map((n) =>
            n.id === categoryId
              ? {
                  ...n,
                  status: ws.status,
                  sortOrder: ws.sortOrder,
                  isVisible: ws.isVisible,
                  parentId: ws.parentCategoryId,
                }
              : n,
          ),
        );
        return;
      }
      setFlatNodes((prev) =>
        prev.map((n) =>
          n.id === categoryId
            ? {
                ...n,
                name: tr.name,
                slug: tr.slug,
                status: ws.status,
                sortOrder: ws.sortOrder,
                isVisible: ws.isVisible,
                parentId: ws.parentCategoryId,
              }
            : n,
        ),
      );
    },
    [categoryId],
  );

  const navigateToCategory = useCallback(
    (id: string, tab: TabId = "general") => {
      if (formMode.isDirty && !formMode.confirmDiscardIfDirty()) return;
      formMode.resetToView();
      setSlugFieldError(null);
      setEditSurface("general");
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
    if (!canAddCategoryChild(flatNodes, parentId)) {
      toast.error(MAX_CATEGORY_DEPTH_MESSAGE_FA);
      return;
    }
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
      setExpandedKeys((prev) =>
        prev.includes(input.parentId!) ? prev : [...prev, input.parentId!],
      );
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

  const handleEnterGeneralEdit = () => {
    if (!workspace || !draft) return;
    const tr =
      workspace.translations.find((t) => t.locale === API_LOCALE) ?? workspace.translations[0];
    setDraft(draftFromWorkspace(workspace, tr?.name ?? draft.name, tr?.slug ?? draft.slug));
    setSlugFieldError(null);
    setEditSurface("general");
    formMode.onEdit();
  };

  const handleCancelGeneralEdit = () => {
    if (!formMode.confirmDiscardIfDirty()) return;
    if (workspace) {
      const tr =
        workspace.translations.find((t) => t.locale === API_LOCALE) ?? workspace.translations[0];
      setDraft(draftFromWorkspace(workspace, tr?.name ?? "", tr?.slug ?? ""));
    }
    setSlugFieldError(null);
    formMode.onCancel();
  };

  const handleSaveGeneral = async () => {
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
      softRefreshTreeLabel(refreshed.data, API_LOCALE);
    }

    formMode.clearDirty();
    toast.success("تغییرات دسته‌بندی ذخیره شد.");
  };

  const loadTranslationDraftForLocale = useCallback(
    (locale: string, ws: CategoryWorkspaceSummary) => {
      const row = ws.translations.find((t) => t.locale === locale);
      setTranslationDraft(row ? translationDraftFromRow(row) : emptyTranslationDraft(locale));
    },
    [],
  );

  const handleSelectLocale = (locale: string) => {
    if (locale === selectedLocale) return;
    if (formMode.isDirty && !formMode.confirmDiscardIfDirty()) return;
    if (formMode.isDirty) formMode.clearDirty();
    setSlugFieldError(null);
    setSelectedLocale(locale);
    if (workspace) loadTranslationDraftForLocale(locale, workspace);
  };

  const handleCreateTranslation = () => {
    if (!canEdit) return;
    setTranslationDraft(emptyTranslationDraft(selectedLocale, true));
    setSlugFieldError(null);
    setEditSurface("translations");
    formMode.onEdit();
  };

  const handleEnterTranslationEdit = () => {
    if (!workspace || !canEdit) return;
    loadTranslationDraftForLocale(selectedLocale, workspace);
    setSlugFieldError(null);
    setEditSurface("translations");
    formMode.onEdit();
  };

  const handleCancelTranslationEdit = () => {
    if (!formMode.confirmDiscardIfDirty()) return;
    if (workspace) loadTranslationDraftForLocale(selectedLocale, workspace);
    setSlugFieldError(null);
    formMode.onCancel();
  };

  const handleEnterAttributesEdit = () => {
    if (!canEdit) return;
    setEditSurface("attributes");
    formMode.onEdit();
  };

  const handleCancelAttributesEdit = () => {
    formMode.onCancel();
  };

  const handleEnterFacetsEdit = () => {
    if (!canEdit) return;
    setEditSurface("facets");
    formMode.onEdit();
  };

  const handleCancelFacetsEdit = () => {
    formMode.onCancel();
  };

  const handleEnterMegaMenuEdit = () => {
    if (!canEdit) return;
    setEditSurface("mega-menu");
    formMode.onEdit();
  };

  const handleCancelMegaMenuEdit = () => {
    formMode.onCancel();
  };

  const handleSaveTranslation = async () => {
    if (!categoryId || !workspace || !translationDraft) return;
    if (translationDraft.locale !== selectedLocale) return;
    setSaveBusy(true);
    setSlugFieldError(null);

    const result = await upsertCategoryTranslation(categoryId, {
      locale: translationDraft.locale,
      name: translationDraft.name.trim(),
      slug: translationDraft.slug.trim(),
      shortDescription: translationDraft.shortDescription.trim() || null,
      description: translationDraft.description.trim() || null,
      seoTitle: translationDraft.seoTitle.trim() || null,
      seoDescription: translationDraft.seoDescription.trim() || null,
      metaKeywords: serializeMetaKeywords(parseMetaKeywords(translationDraft.metaKeywords)) || null,
    });

    if (result.state !== "ok" || !result.data) {
      setSaveBusy(false);
      const mapped = mapCategoryMutationError(result);
      setSlugFieldError(mapped);
      toast.error(mapped);
      return;
    }

    const refreshed = await fetchCategoryWorkspace(categoryId);
    setSaveBusy(false);
    if (refreshed.state === "ok" && refreshed.data) {
      setWorkspace(refreshed.data);
      loadTranslationDraftForLocale(selectedLocale, refreshed.data);
      softRefreshTreeLabel(refreshed.data, translationDraft.locale);
      // همگام‌سازی پیش‌نویس عمومی اگر همان locale فارسی باشد
      if (translationDraft.locale === API_LOCALE) {
        const tr = refreshed.data.translations.find((t) => t.locale === API_LOCALE);
        setDraft(draftFromWorkspace(refreshed.data, tr?.name ?? "", tr?.slug ?? ""));
      }
    }

    formMode.clearDirty();
    toast.success("ترجمه ذخیره شد");
  };

  const revertSurfaceDraft = useCallback(
    (surface: EditSurface) => {
      if (!workspace) return;
      if (surface === "general") {
        const tr =
          workspace.translations.find((t) => t.locale === API_LOCALE) ?? workspace.translations[0];
        setDraft(draftFromWorkspace(workspace, tr?.name ?? "", tr?.slug ?? ""));
      }
      if (surface === "translations") {
        loadTranslationDraftForLocale(selectedLocale, workspace);
      }
      setSlugFieldError(null);
    },
    [workspace, selectedLocale, loadTranslationDraftForLocale],
  );

  const prepareSurfaceDraft = useCallback(
    (surface: EditSurface) => {
      if (!workspace) return;
      if (surface === "general") {
        const tr =
          workspace.translations.find((t) => t.locale === API_LOCALE) ?? workspace.translations[0];
        setDraft(draftFromWorkspace(workspace, tr?.name ?? "", tr?.slug ?? ""));
      }
      if (surface === "translations") {
        loadTranslationDraftForLocale(selectedLocale, workspace);
      }
      setSlugFieldError(null);
    },
    [workspace, selectedLocale, loadTranslationDraftForLocale],
  );

  const handleHeaderEdit = () => {
    if (!canEdit) return;
    if (activeTab === "products") {
      handleEnterGeneralEdit();
      if (categoryId) router.push(`${basePath}/${categoryId}`);
      return;
    }
    if (activeTab === "general") handleEnterGeneralEdit();
    else if (activeTab === "translations") handleEnterTranslationEdit();
    else if (activeTab === "attributes") handleEnterAttributesEdit();
    else if (activeTab === "facets") handleEnterFacetsEdit();
    else if (activeTab === "mega-menu") handleEnterMegaMenuEdit();
  };

  const handleHeaderSave = () => {
    if (editSurface === "general" && activeTab === "general") {
      void handleSaveGeneral();
      return;
    }
    if (editSurface === "translations" && activeTab === "translations") {
      void handleSaveTranslation();
    }
  };

  const handleHeaderDiscard = () => {
    if (editSurface === "general") {
      handleCancelGeneralEdit();
      return;
    }
    if (editSurface === "translations") {
      handleCancelTranslationEdit();
      return;
    }
    if (editSurface === "attributes") {
      handleCancelAttributesEdit();
      return;
    }
    if (editSurface === "facets") {
      handleCancelFacetsEdit();
      return;
    }
    if (editSurface === "mega-menu") {
      handleCancelMegaMenuEdit();
    }
  };

  const handleEndEdit = () => {
    if (!formMode.confirmDiscardIfDirty()) return;
    revertSurfaceDraft(editSurface);
    formMode.onCancel();
  };

  const tabToEditSurface = (tab: TabId): EditSurface | null => {
    if (tab === "products") return null;
    return tab;
  };

  const selectedNode = categoryId ? flatNodes.find((n) => n.id === categoryId) : undefined;
  const pathNames = categoryId ? buildCategoryPath(flatNodes, categoryId) : [];
  const parentName = workspace?.parentCategoryId
    ? buildCategoryPath(flatNodes, workspace.parentCategoryId).join(" / ") || "—"
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
      const path = buildCategoryPath(flatNodes, n.id).join(" / ");
      opts.push({ id: n.id, label: path || n.name });
    }
    return opts;
  }, [categoryId, flatNodes]);

  const translationStatuses = useMemo(
    () => (workspace ? buildTranslationStatuses(workspace.translations, UI_LOCALES) : []),
    [workspace],
  );

  const showTreePane = isNarrow
    ? !mobileWorkspace || !categoryId
    : !treePaneCollapsed;
  const showTreeExpandRail = !isNarrow && treePaneCollapsed;
  const showWorkspacePane = !isNarrow || (mobileWorkspace && Boolean(categoryId));
  const isEdit = formMode.mode === "edit";
  const isGeneralEdit = isEdit && editSurface === "general" && activeTab === "general";
  const isTranslationEdit = isEdit && editSurface === "translations" && activeTab === "translations";
  const isAttributesEdit = isEdit && editSurface === "attributes" && activeTab === "attributes";
  const isFacetsEdit = isEdit && editSurface === "facets" && activeTab === "facets";
  const isMegaMenuEdit = isEdit && editSurface === "mega-menu" && activeTab === "mega-menu";

  const headerEditVisible = !isEdit && formMode.canEdit;
  const headerSaveVisible =
    isEdit
    && ((editSurface === "general" && activeTab === "general")
      || (editSurface === "translations" && activeTab === "translations"));

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
      {showTreeExpandRail ? (
        <div className="flex shrink-0 items-start pt-2" data-testid="category-tree-expand-rail">
          <button
            type="button"
            className="inline-flex min-h-11 flex-col items-center gap-1 rounded-xl border border-gray-200 bg-white px-2 py-3 text-[11px] font-medium text-slate-600 shadow-sm transition-colors hover:bg-slate-50 hover:text-slate-900"
            onClick={() => setTreePaneCollapsed(false)}
            aria-label="نمایش درخت دسته‌بندی‌ها"
            title="نمایش درخت دسته‌بندی‌ها"
            data-testid="category-tree-expand-pane"
          >
            <PanelLeftOpen size={18} aria-hidden />
            <span className="[writing-mode:vertical-rl] rotate-180">دسته‌بندی‌ها</span>
          </button>
        </div>
      ) : null}

      {showTreePane ? (
        <aside className="w-full shrink-0 lg:w-[360px] xl:w-[400px]" data-testid="category-tree-pane">
          <AppCategoryTree
            nodes={flatNodes}
            expandedKeys={expandedKeys}
            selectedKeys={
              categoryId && flatNodes.some((n) => n.id === categoryId) ? [categoryId] : []
            }
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
            onCollapsePane={!isNarrow ? () => setTreePaneCollapsed(true) : undefined}
            collapsePaneLabel="بستن درخت دسته‌بندی‌ها"
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
                      {isGeneralEdit && draft
                        ? draft.name || activeTranslation?.name || selectedNode?.name || "دسته‌بندی"
                        : activeTranslation?.name || selectedNode?.name || "دسته‌بندی"}
                    </h1>
                    <span
                      className={`rounded-full border px-2.5 py-0.5 text-xs font-semibold ${statusBadgeClass(isGeneralEdit && draft ? draft.status : workspace.status)}`}
                    >
                      {workspaceStatusLabel(isGeneralEdit && draft ? draft.status : workspace.status)}
                    </span>
                    <span
                      className="rounded-full bg-slate-100 px-2.5 py-0.5 text-[11px] font-medium text-slate-600"
                      data-testid="category-form-mode-badge"
                    >
                      {isEdit ? "ویرایش" : "مشاهده"}
                    </span>
                  </div>
                  <div className="flex flex-wrap items-center gap-2">
                    {isEdit ? (
                      <>
                        {headerSaveVisible ? (
                          <button
                            type="button"
                            className="inline-flex min-h-11 items-center rounded-xl bg-[#2563EB] px-4 text-sm font-semibold text-white hover:brightness-95 disabled:opacity-50"
                            onClick={handleHeaderSave}
                            disabled={saveBusy}
                            data-testid="category-header-save"
                          >
                            {saveBusy ? "در حال ذخیره…" : "ذخیره"}
                          </button>
                        ) : null}
                        <button
                          type="button"
                          className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
                          onClick={handleHeaderDiscard}
                          disabled={saveBusy}
                          data-testid="category-header-discard"
                        >
                          انصراف
                        </button>
                        <button
                          type="button"
                          className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
                          onClick={handleEndEdit}
                          disabled={saveBusy}
                          data-testid="category-header-end-edit"
                        >
                          پایان ویرایش
                        </button>
                      </>
                    ) : null}
                    {headerEditVisible ? (
                      <button
                        type="button"
                        className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-semibold text-slate-800 hover:bg-slate-50"
                        onClick={handleHeaderEdit}
                        data-testid="category-edit-action"
                      >
                        ویرایش
                      </button>
                    ) : null}
                    {!isEdit && activeTab === "general" && storefrontRoute ? (
                      <a
                        href={`http://localhost:3000${storefrontRoute}`}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 bg-white px-4 text-sm font-medium text-slate-700 hover:bg-slate-50"
                        data-testid="category-storefront-preview"
                      >
                        پیش‌نمایش ویترین
                      </a>
                    ) : null}
                  </div>
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
                        if (formMode.isDirty) {
                          revertSurfaceDraft(editSurface);
                          formMode.clearDirty();
                        }
                        setSlugFieldError(null);
                        const nextSurface = tabToEditSurface(tab.id);
                        if (formMode.mode === "edit") {
                          if (nextSurface) {
                            setEditSurface(nextSurface);
                            prepareSurfaceDraft(nextSurface);
                          }
                        } else if (nextSurface) {
                          setEditSurface(nextSurface);
                        } else {
                          setEditSurface("general");
                        }
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
                    </Link>
                  );
                })}
              </nav>

              <div className="flex-1 p-4 lg:p-6">
                {activeTab === "general" ? (
                  isGeneralEdit && draft ? (
                    <GeneralEditForm
                      draft={draft}
                      fieldError={slugFieldError}
                      parentOptions={parentOptions}
                      busy={saveBusy}
                      workspace={workspace}
                      onWorkspaceChange={setWorkspace}
                      onChange={(next) => {
                        setDraft(next);
                        formMode.markDirty();
                        if (slugFieldError) setSlugFieldError(null);
                      }}
                      onSave={() => void handleSaveGeneral()}
                      onCancel={handleCancelGeneralEdit}
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
                      translationStatuses={translationStatuses}
                      canEditMedia={formMode.canEdit}
                      onWorkspaceChange={setWorkspace}
                    />
                  )
                ) : null}
                {activeTab === "translations" ? (
                  <TranslationsPanel
                    workspace={workspace}
                    selectedLocale={selectedLocale}
                    draft={translationDraft}
                    isEdit={isTranslationEdit}
                    canEdit={formMode.canEdit}
                    fieldError={slugFieldError}
                    busy={saveBusy}
                    onSelectLocale={handleSelectLocale}
                    onCreateTranslation={handleCreateTranslation}
                    onChange={(next) => {
                      setTranslationDraft(next);
                      formMode.markDirty();
                      if (slugFieldError) setSlugFieldError(null);
                    }}
                    onSave={() => void handleSaveTranslation()}
                    onCancel={handleCancelTranslationEdit}
                  />
                ) : null}
                {activeTab === "attributes" && categoryId ? (
                  <CategoryAttributesPanel
                    categoryId={categoryId}
                    treeNodes={flatNodes}
                    isEdit={isAttributesEdit}
                    canEdit={formMode.canEdit}
                    busy={saveBusy}
                  />
                ) : null}
                {activeTab === "facets" && categoryId ? (
                  <CategoryFacetsPanel
                    categoryId={categoryId}
                    treeNodes={flatNodes}
                    isEdit={isFacetsEdit}
                    canEdit={formMode.canEdit}
                    busy={saveBusy}
                  />
                ) : null}
                {activeTab === "mega-menu" && categoryId ? (
                  <CategoryMegaMenuPanel
                    categoryId={categoryId}
                    isEdit={isMegaMenuEdit}
                    canEdit={formMode.canEdit}
                    busy={saveBusy}
                  />
                ) : null}
                {activeTab === "products" && categoryId ? (
                  <CategoryProductsPanel
                    categoryId={categoryId}
                    categoryName={
                      activeTranslation?.name
                      || selectedNode?.name
                      || "دسته"
                    }
                    treeNodes={flatNodes}
                    canEdit={formMode.canEdit}
                  />
                ) : null}
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
