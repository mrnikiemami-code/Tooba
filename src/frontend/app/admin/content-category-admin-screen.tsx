"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import {
  AppCategoryTree,
  buildParentMap,
  canAddCategoryChild,
  collectAncestorIds,
  formatJalaliDate,
  isSelfOrDescendant,
  MAX_CONTENT_CATEGORY_DEPTH,
  Spinner,
  useAdminFormMode,
  type AppCategoryTreeNode,
} from "../../design-system";
import { prepareAdminDevActor } from "./admin-api.ts";
import {
  archiveContentCategory,
  createContentCategory,
  fetchContentCategoryTree,
  fetchContentCategoryWorkspace,
  mapContentCategoryMutationError,
  slugifyContentCategoryName,
  updateContentCategoryCore,
  updateContentCategoryMedia,
  updateContentCategorySeo,
  type ContentCategoryTreeNodeDto,
  type ContentCategoryWorkspaceDto,
} from "./content-category-api.ts";
import { loadAdminLanguages } from "./language-api.ts";
import type { SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";
import { MediaLibraryDialog } from "./media-library-dialog.tsx";
import { mediaPreviewUrl, type MediaAssetDto } from "./media-api.ts";

const TABS = [
  { id: "general", label: "عمومی" },
  { id: "seo", label: "SEO" },
  { id: "media", label: "رسانه" },
  { id: "articles", label: "مقالات" },
  { id: "history", label: "تاریخچه" },
] as const;

type TabId = (typeof TABS)[number]["id"];

function languageTabLabel(lang: SupportedLocaleDefinition): string {
  const native = lang.nativeName?.trim() ?? "";
  if (native && !/^\?+$/.test(native)) return native;
  return lang.displayName?.trim() || lang.code;
}

function toTreeNodes(rows: ContentCategoryTreeNodeDto[]): AppCategoryTreeNode[] {
  return rows.map((row) => ({
    id: row.id,
    parentId: row.parentId,
    name: row.name,
    slug: row.slug,
    status: row.status === "Archived" ? "Archived" : "Published",
    sortOrder: row.sortOrder,
    isVisible: row.status !== "Archived",
    hasChildren: row.hasChildren,
    productCount: row.articleCount,
  }));
}

function parentOptions(
  rows: ContentCategoryTreeNodeDto[],
  selectedId: string | null,
  languageCode: string,
): { id: string; label: string }[] {
  const tree = toTreeNodes(rows);
  const parentMap = buildParentMap(tree);
  return rows
    .filter((row) => row.languageCode === languageCode)
    .filter((row) => !row.parentId) // فقط دسته اصلی می‌تواند والد باشد (عمق ۲)
    .filter((row) => !selectedId || (row.id !== selectedId && !isSelfOrDescendant(parentMap, selectedId, row.id)))
    .map((row) => ({ id: row.id, label: `${row.name} (دسته اصلی)` }));
}

export function ContentCategoryAdminScreen() {
  const params = useParams<{ categoryId?: string }>();
  const router = useRouter();
  const selectedId = typeof params.categoryId === "string" ? params.categoryId : null;
  const [languages, setLanguages] = useState<SupportedLocaleDefinition[]>([]);
  const [languageCode, setLanguageCode] = useState<string>("fa-IR");
  const [search, setSearch] = useState("");
  const [expandedKeys, setExpandedKeys] = useState<string[]>([]);
  const [treeRows, setTreeRows] = useState<ContentCategoryTreeNodeDto[]>([]);
  const [workspace, setWorkspace] = useState<ContentCategoryWorkspaceDto | null>(null);
  const [tab, setTab] = useState<TabId>("general");
  const [loading, setLoading] = useState(true);
  const [workspaceLoading, setWorkspaceLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [showCreate, setShowCreate] = useState(false);
  const [createParentId, setCreateParentId] = useState<string | null>(null);
  const [createName, setCreateName] = useState("");
  const [createSlug, setCreateSlug] = useState("");
  const [createSlugTouched, setCreateSlugTouched] = useState(false);
  const [draftName, setDraftName] = useState("");
  const [draftSlug, setDraftSlug] = useState("");
  const [draftShortDescription, setDraftShortDescription] = useState("");
  const [draftDescription, setDraftDescription] = useState("");
  const [draftSortOrder, setDraftSortOrder] = useState(0);
  const [draftStatus, setDraftStatus] = useState<"Active" | "Archived">("Active");
  const [draftParentId, setDraftParentId] = useState<string | null>(null);
  const [draftSeoTitle, setDraftSeoTitle] = useState("");
  const [draftSeoDescription, setDraftSeoDescription] = useState("");
  const [mediaOpen, setMediaOpen] = useState(false);
  const [mediaAsset, setMediaAsset] = useState<MediaAssetDto | null>(null);

  const form = useAdminFormMode({ canView: true, canEdit: true });

  useEffect(() => {
    let cancelled = false;
    void prepareAdminDevActor().then(() =>
      loadAdminLanguages().then((result) => {
        if (cancelled) return;
        if (result.state !== "ok" || !result.data?.length) {
          setLanguages([]);
          return;
        }
        const active = result.data
          .filter((row) => row.active)
          .slice()
          .sort((a, b) => a.sortOrder - b.sortOrder || a.code.localeCompare(b.code));
        setLanguages(active);
        const defaultLang = active.find((row) => row.default) ?? active[0];
        if (defaultLang) {
          setLanguageCode((prev) => (active.some((row) => row.code === prev) ? prev : defaultLang.code));
        }
      }),
    );
    return () => {
      cancelled = true;
    };
  }, []);

  const refreshTree = useCallback(async () => {
    await prepareAdminDevActor();
    const result = await fetchContentCategoryTree(languageCode, search);
    if (result.state === "ok" && result.data) setTreeRows(result.data);
  }, [languageCode, search]);

  const refreshWorkspace = useCallback(async (categoryId: string) => {
    setWorkspaceLoading(true);
    try {
      const result = await fetchContentCategoryWorkspace(categoryId);
      if (result.state !== "ok" || !result.data) {
        setWorkspace(null);
        return;
      }
      const data = result.data;
      setWorkspace(data);
      setDraftName(data.name);
      setDraftSlug(data.slug);
      setDraftShortDescription(data.shortDescription ?? "");
      setDraftDescription(data.description ?? "");
      setDraftSortOrder(data.sortOrder);
      setDraftStatus(data.status);
      setDraftParentId(data.parentId);
      setDraftSeoTitle(data.seoTitle ?? "");
      setDraftSeoDescription(data.seoDescription ?? "");
      if (data.imageMediaAssetId) {
        setMediaAsset({ mediaAssetId: data.imageMediaAssetId } as MediaAssetDto);
      } else {
        setMediaAsset(null);
      }
    } finally {
      setWorkspaceLoading(false);
    }
  }, []);

  useEffect(() => {
    setLoading(true);
    void refreshTree().finally(() => setLoading(false));
  }, [refreshTree]);

  useEffect(() => {
    if (!selectedId) {
      setWorkspace(null);
      setWorkspaceLoading(false);
      return;
    }
    void refreshWorkspace(selectedId);
  }, [refreshWorkspace, selectedId]);

  const treeNodes = useMemo(() => toTreeNodes(treeRows), [treeRows]);
  const parentMap = useMemo(() => buildParentMap(treeNodes), [treeNodes]);

  useEffect(() => {
    if (!selectedId) return;
    setExpandedKeys(collectAncestorIds(parentMap, selectedId));
  }, [parentMap, selectedId]);
  const parentPicker = useMemo(
    () => parentOptions(treeRows, selectedId, languageCode),
    [languageCode, selectedId, treeRows],
  );

  const selectNode = useCallback((id: string) => {
    router.push(`/admin/content/categories/${id}`);
  }, [router]);

  const openCreate = useCallback((parentId: string | null) => {
    if (parentId && !canAddCategoryChild(treeNodes, parentId, MAX_CONTENT_CATEGORY_DEPTH)) {
      toast.error("دسته‌بندی مقاله حداکثر می‌تواند دو سطح داشته باشد.");
      return;
    }
    setCreateParentId(parentId);
    setCreateName("");
    setCreateSlug("");
    setCreateSlugTouched(false);
    setShowCreate(true);
  }, [treeNodes]);

  const saveCreate = useCallback(async () => {
    setSaving(true);
    const result = await createContentCategory({
      languageCode,
      parentCategoryId: createParentId,
      name: createName,
      slug: createSlug,
    });
    setSaving(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapContentCategoryMutationError(result));
      return;
    }
    setShowCreate(false);
    await refreshTree();
    selectNode(result.data.id);
  }, [createName, createParentId, createSlug, languageCode, refreshTree, selectNode]);

  const saveGeneral = useCallback(async () => {
    if (!workspace) return;
    setSaving(true);
    const result = await updateContentCategoryCore(workspace.id, {
      name: draftName,
      slug: draftSlug,
      shortDescription: draftShortDescription || null,
      description: draftDescription || null,
      sortOrder: draftSortOrder,
      status: draftStatus,
    });
    setSaving(false);
    if (result.state !== "ok" || !result.data) {
      toast.error(mapContentCategoryMutationError(result));
      return;
    }
    toast.success("ذخیره شد");
    form.onSaved();
    await refreshTree();
    await refreshWorkspace(workspace.id);
  }, [draftDescription, draftName, draftShortDescription, draftSlug, draftSortOrder, draftStatus, form, refreshTree, refreshWorkspace, workspace]);

  const saveSeo = useCallback(async () => {
    if (!workspace) return;
    setSaving(true);
    const result = await updateContentCategorySeo(workspace.id, {
      seoTitle: draftSeoTitle || null,
      seoDescription: draftSeoDescription || null,
    });
    setSaving(false);
    if (result.state !== "ok") {
      toast.error(mapContentCategoryMutationError(result));
      return;
    }
    toast.success("SEO ذخیره شد");
    form.onSaved();
    await refreshWorkspace(workspace.id);
  }, [draftSeoDescription, draftSeoTitle, form, refreshWorkspace, workspace]);

  const assignMedia = useCallback(async (asset: MediaAssetDto | null) => {
    if (!workspace) return;
    setSaving(true);
    const result = await updateContentCategoryMedia(workspace.id, asset?.mediaAssetId ?? null);
    setSaving(false);
    if (result.state !== "ok") {
      toast.error(mapContentCategoryMutationError(result));
      return;
    }
    setMediaAsset(asset);
    setMediaOpen(false);
    toast.success(asset ? "تصویر اختصاص یافت" : "تصویر حذف شد");
    await refreshWorkspace(workspace.id);
  }, [refreshWorkspace, workspace]);

  const archiveSelected = useCallback(async () => {
    if (!workspace) return;
    if (!window.confirm(`بایگانی «${workspace.name}»؟`)) return;
    setSaving(true);
    const result = await archiveContentCategory(workspace.id);
    setSaving(false);
    if (result.state !== "ok") {
      toast.error(mapContentCategoryMutationError(result));
      return;
    }
    toast.success("بایگانی شد");
    router.push("/admin/content/categories");
    await refreshTree();
  }, [refreshTree, router, workspace]);

  const isRtl = languageCode.toLowerCase().startsWith("fa");

  return (
    <main className="w-full" data-testid="admin-content-categories">
      <div className="mb-4 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-[length:var(--type-title)] font-semibold tracking-tight">دسته‌بندی مقالات</h1>
          <p className="mt-1 text-sm text-muted">درخت زبان‌محور مقالات — مستقل از دسته‌بندی محصولات</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {languages.map((opt) => (
            <button
              key={opt.code}
              type="button"
              className={`rounded-xl px-3 py-2 text-sm font-semibold ${languageCode === opt.code ? "bg-[#2563EB] text-white" : "border border-border bg-white"}`}
              onClick={() => {
                setLanguageCode(opt.code);
                router.push("/admin/content/categories");
              }}
              data-testid={`content-category-lang-${opt.code}`}
            >
              {languageTabLabel(opt)}
            </button>
          ))}
          <button
            type="button"
            className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white"
            onClick={() => openCreate(null)}
            data-testid="content-category-create-root"
          >
            دسته اصلی
          </button>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-[minmax(320px,48%)_minmax(0,1fr)]">
        <section className="min-w-[320px] max-w-full resize-x overflow-auto rounded-2xl border border-border bg-surface-elevated p-3 shadow-sm">
          <AppCategoryTree
            nodes={treeNodes}
            expandedKeys={expandedKeys}
            selectedKeys={selectedId ? [selectedId] : []}
            onExpandedKeysChange={setExpandedKeys}
            onSelect={selectNode}
            onCreateRoot={() => openCreate(null)}
            onCreateChild={(id) => openCreate(id)}
            searchQuery={search}
            onSearchQueryChange={setSearch}
            loading={loading}
            direction={isRtl ? "rtl" : "ltr"}
            uiLocale={isRtl ? "fa" : "en"}
            maxDepth={MAX_CONTENT_CATEGORY_DEPTH}
            createLabel={isRtl ? "دسته اصلی" : "Main category"}
            title={isRtl ? "دسته‌بندی مقالات" : "Article categories"}
            searchPlaceholder={isRtl ? "جستجو…" : "Search…"}
            allowDrag={false}
          />
        </section>

        <section className="rounded-2xl border border-border bg-surface-elevated p-4 shadow-sm">
          {workspaceLoading ? (
            <div className="flex items-center gap-2 text-sm text-muted" data-testid="content-category-workspace-loading">
              <Spinner />
              <span>در حال بارگذاری…</span>
            </div>
          ) : !workspace ? (
            <p className="text-sm text-muted">یک دسته را از درخت انتخاب کنید یا دستهٔ جدید بسازید.</p>
          ) : (
            <>
              <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
                <div>
                  <h2 className="text-lg font-bold">{workspace.name}</h2>
                  <p className="text-xs text-muted" dir="ltr">{workspace.slug} · {workspace.languageCode}</p>
                </div>
                <div className="flex gap-2">
                  {form.mode === "view" ? (
                    <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={form.onEdit}>ویرایش</button>
                  ) : (
                    <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={form.onCancel}>انصراف</button>
                  )}
                  <button
                    type="button"
                    className="rounded-xl border border-red-200 px-3 py-2 text-sm text-red-700"
                    onClick={() => void archiveSelected()}
                    disabled={saving}
                  >
                    بایگانی
                  </button>
                </div>
              </div>

              <div className="mb-4 flex flex-wrap gap-2 border-b border-border pb-2">
                {TABS.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    className={`rounded-lg px-3 py-1.5 text-sm ${tab === item.id ? "bg-slate-900 text-white" : "text-muted"}`}
                    onClick={() => setTab(item.id)}
                    data-testid={`content-category-tab-${item.id}`}
                  >
                    {item.label}
                  </button>
                ))}
              </div>

              {tab === "general" ? (
                <div className="space-y-3">
                  <label className="block text-sm">
                    <span className="mb-1 block text-muted">نام</span>
                    <input className="w-full rounded-xl border px-3 py-2" value={draftName} disabled={form.mode === "view"} onChange={(e) => setDraftName(e.target.value)} />
                  </label>
                  <label className="block text-sm">
                    <span className="mb-1 block text-muted">نامک</span>
                    <input className="w-full rounded-xl border px-3 py-2" dir="ltr" value={draftSlug} disabled={form.mode === "view"} onChange={(e) => setDraftSlug(e.target.value)} />
                  </label>
                  <label className="block text-sm">
                    <span className="mb-1 block text-muted">والد (همان زبان)</span>
                    <select
                      className="w-full rounded-xl border px-3 py-2"
                      disabled={form.mode === "view"}
                      value={draftParentId ?? ""}
                      onChange={(e) => setDraftParentId(e.target.value || null)}
                    >
                      <option value="">— ریشه —</option>
                      {parentPicker.map((opt) => (
                        <option key={opt.id} value={opt.id}>{opt.label}</option>
                      ))}
                    </select>
                  </label>
                  <label className="block text-sm">
                    <span className="mb-1 block text-muted">ترتیب</span>
                    <input type="number" className="w-full rounded-xl border px-3 py-2" value={draftSortOrder} disabled={form.mode === "view"} onChange={(e) => setDraftSortOrder(Number(e.target.value))} />
                  </label>
                  <label className="block text-sm">
                    <span className="mb-1 block text-muted">توضیح کوتاه</span>
                    <textarea className="w-full rounded-xl border px-3 py-2" rows={2} value={draftShortDescription} disabled={form.mode === "view"} onChange={(e) => setDraftShortDescription(e.target.value)} />
                  </label>
                  <label className="block text-sm">
                    <span className="mb-1 block text-muted">توضیح</span>
                    <textarea className="w-full rounded-xl border px-3 py-2" rows={4} value={draftDescription} disabled={form.mode === "view"} onChange={(e) => setDraftDescription(e.target.value)} />
                  </label>
                  {form.mode !== "view" ? (
                    <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white" disabled={saving} onClick={() => void saveGeneral()}>
                      ذخیره عمومی
                    </button>
                  ) : null}
                </div>
              ) : null}

              {tab === "seo" ? (
                <div className="space-y-3">
                  <label className="block text-sm">
                    <span className="mb-1 block text-muted">عنوان SEO</span>
                    <input className="w-full rounded-xl border px-3 py-2" value={draftSeoTitle} disabled={form.mode === "view"} onChange={(e) => setDraftSeoTitle(e.target.value)} />
                  </label>
                  <label className="block text-sm">
                    <span className="mb-1 block text-muted">توضیح متا</span>
                    <textarea className="w-full rounded-xl border px-3 py-2" rows={3} value={draftSeoDescription} disabled={form.mode === "view"} onChange={(e) => setDraftSeoDescription(e.target.value)} />
                  </label>
                  {form.mode !== "view" ? (
                    <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-semibold text-white" disabled={saving} onClick={() => void saveSeo()}>
                      ذخیره SEO
                    </button>
                  ) : null}
                </div>
              ) : null}

              {tab === "media" ? (
                <div className="space-y-3">
                  {mediaAsset?.mediaAssetId ? (
                    <img src={mediaPreviewUrl(mediaAsset.mediaAssetId)} alt="" className="max-h-40 rounded-xl border object-cover" />
                  ) : (
                    <p className="text-sm text-muted">تصویری اختصاص داده نشده است.</p>
                  )}
                  <div className="flex gap-2">
                    <button type="button" className="rounded-xl border px-3 py-2 text-sm" onClick={() => setMediaOpen(true)} disabled={form.mode === "view"}>
                      انتخاب از کتابخانه
                    </button>
                    {mediaAsset ? (
                      <button type="button" className="rounded-xl border px-3 py-2 text-sm" disabled={form.mode === "view"} onClick={() => void assignMedia(null)}>
                        حذف اختصاص
                      </button>
                    ) : null}
                  </div>
                </div>
              ) : null}

              {tab === "articles" ? (
                <div className="space-y-3 text-sm">
                  <p>تعداد مقالات متصل: <strong>{workspace.articleCount}</strong></p>
                  <Link href="/admin/content" className="text-[#2563EB] underline">رفتن به فهرست مقالات</Link>
                </div>
              ) : null}

              {tab === "history" ? (
                <div className="grid gap-3 sm:grid-cols-2">
                  <div className="rounded-xl border p-3 text-sm">
                    <div className="text-muted">ایجاد</div>
                    <div>{formatJalaliDate(workspace.createdAt, "fa")}</div>
                  </div>
                  <div className="rounded-xl border p-3 text-sm">
                    <div className="text-muted">آخرین به‌روزرسانی</div>
                    <div>{formatJalaliDate(workspace.updatedAt, "fa")}</div>
                  </div>
                </div>
              ) : null}
            </>
          )}
        </section>
      </div>

      {showCreate ? (
        <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-md rounded-2xl bg-white p-5 shadow-xl">
            <h3 className="text-lg font-bold">
              {createParentId ? "زیردستهٔ جدید" : "دستهٔ اصلی جدید"}
            </h3>
            <label className="mt-3 block text-sm">
              نام
              <input
                className="mt-1 w-full rounded-xl border px-3 py-2"
                value={createName}
                onChange={(e) => {
                  setCreateName(e.target.value);
                  if (!createSlugTouched) setCreateSlug(slugifyContentCategoryName(e.target.value));
                }}
              />
            </label>
            <label className="mt-3 block text-sm">
              نامک
              <input className="mt-1 w-full rounded-xl border px-3 py-2" dir="ltr" value={createSlug} onChange={(e) => { setCreateSlugTouched(true); setCreateSlug(e.target.value); }} />
            </label>
            <div className="mt-4 flex justify-end gap-2">
              <button type="button" className="rounded-xl px-4 py-2 text-sm" onClick={() => setShowCreate(false)}>انصراف</button>
              <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" disabled={saving} onClick={() => void saveCreate()}>ذخیره</button>
            </div>
          </div>
        </div>
      ) : null}

      <MediaLibraryDialog
        open={mediaOpen}
        selectionMode="single"
        onClose={() => setMediaOpen(false)}
        onConfirm={(assets) => void assignMedia(assets[0] ?? null)}
      />
    </main>
  );
}
