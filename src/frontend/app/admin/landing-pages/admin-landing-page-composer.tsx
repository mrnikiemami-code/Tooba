"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState, type DragEvent } from "react";
import {
  ChevronDown,
  ChevronUp,
  Eye,
  EyeOff,
  GripVertical,
  Pencil,
  Plus,
  Trash2,
} from "lucide-react";
import {
  addAdminLandingSection,
  createAdminLandingPage,
  deleteAdminLandingSection,
  getAdminLandingHome,
  getAdminLandingPage,
  listAdminLandingSections,
  reorderAdminLandingSections,
  replaceAdminLandingComposition,
  setAdminLandingHome,
  setAdminLandingPageStatus,
  setAdminLandingSectionEnabled,
  updateAdminLandingPage,
  updateAdminLandingSection,
  publicPathForStorePage,
  type AdminLandingPage,
  type AdminLandingSection,
  type StorePageType,
} from "./admin-landing-pages-api.ts";
import {
  LANDING_SECTION_CHOICES,
  incompleteSourceWarning,
  landingSectionLabel,
  parseLandingConfig,
  summarizeLandingSection,
} from "./landing-section-catalog.ts";
import {
  defaultConfigForCompositionSection,
} from "./admin-composition-catalog.ts";
import { SIZE_PRESET_CONTRACTS } from "../../../lib/storefront-composition/size-presets.ts";
import { getSectionType, getVariant } from "../../../lib/storefront-composition/registry.ts";
import {
  buildTemplateSectionPayloads,
  getIndustryTemplate,
  listIndustryTemplates,
} from "../../../lib/storefront-composition/industry-templates.ts";
import { AdminSectionWizard } from "./admin-section-wizard.tsx";
import { AdminTemplateSelectionWorkspace } from "./admin-template-selection-workspace.tsx";

type Meta = {
  title: string;
  slug: string;
  locale: string;
  pageType: StorePageType;
  seoTitle: string;
  seoDescription: string;
  robotsIndex: boolean;
  robotsFollow: boolean;
  canonicalUrl: string;
  ogTitle: string;
  ogDescription: string;
  ogImageUrl: string;
  primaryH1: string;
};

function toMeta(page?: AdminLandingPage | null): Meta {
  return {
    title: page?.title ?? "",
    slug: page?.slug ?? "",
    locale: page?.locale ?? "fa",
    pageType: page?.pageType ?? "Landing",
    seoTitle: page?.seoTitle ?? "",
    seoDescription: page?.seoDescription ?? "",
    robotsIndex: page?.robotsIndex ?? true,
    robotsFollow: page?.robotsFollow ?? true,
    canonicalUrl: page?.canonicalUrl ?? "",
    ogTitle: page?.ogTitle ?? "",
    ogDescription: page?.ogDescription ?? "",
    ogImageUrl: page?.ogImageUrl ?? "",
    primaryH1: page?.primaryH1 ?? "",
  };
}

function resolveSectionTypeKey(hostType: string, config: Record<string, unknown>): string | null {
  const variantKey = typeof config.variantKey === "string" ? config.variantKey : undefined;
  if (variantKey) {
    const fromVariant = getVariant(variantKey);
    if (fromVariant) return fromVariant.sectionTypeKey;
  }
  return null;
}

function localeLabel(locale: string): string {
  return locale === "en" ? "انگلیسی" : "فارسی";
}

function slugifyTemplate(templateKey: string): string {
  const base = templateKey.replace(/[^a-zA-Z0-9_-]+/g, "-").replace(/^-+|-+$/g, "").toLowerCase() || "page";
  const suffix = Date.now().toString(36).slice(-5);
  return `${base}-${suffix}`;
}

function InsertBetweenAffordance({
  disabled,
  onClick,
  testId,
}: {
  disabled: boolean;
  onClick: () => void;
  testId: string;
}) {
  return (
    <div className="flex justify-center py-1" data-testid={testId}>
      <button
        type="button"
        disabled={disabled}
        onClick={onClick}
        className="inline-flex items-center gap-1 rounded-full border border-dashed border-slate-300 bg-white px-3 py-1 text-xs font-bold text-slate-600 hover:border-[#2563EB] hover:text-[#2563EB] disabled:opacity-40"
      >
        <Plus className="h-3.5 w-3.5" />
        افزودن بخش
      </button>
    </div>
  );
}

export function AdminLandingPageComposer({ pageId }: { pageId?: string }) {
  const router = useRouter();
  const [page, setPage] = useState<AdminLandingPage | null>(null);
  const pageRef = useRef<AdminLandingPage | null>(null);
  const [meta, setMeta] = useState<Meta>(toMeta());
  const [sections, setSections] = useState<AdminLandingSection[]>([]);
  const [homePageId, setHomePageId] = useState<string | null>(null);
  const [message, setMessage] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [saved, setSaved] = useState(true);
  const [wizardOpen, setWizardOpen] = useState(false);
  const [wizardMode, setWizardMode] = useState<"create" | "edit">("create");
  const [editing, setEditing] = useState<AdminLandingSection | null>(null);
  const [insertAt, setInsertAt] = useState<number | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);
  const [focusedSectionId, setFocusedSectionId] = useState<string | null>(null);
  const [createMode, setCreateMode] = useState<"blank" | "template" | null>(pageId ? "blank" : null);
  const [selectedTemplateKey, setSelectedTemplateKey] = useState<string | null>(null);
  const [templatePickerOpen, setTemplatePickerOpen] = useState(false);
  const [seoOpen, setSeoOpen] = useState(true);
  const [dragId, setDragId] = useState<string | null>(null);
  const [dragOverId, setDragOverId] = useState<string | null>(null);
  const [pendingReplaceTemplateKey, setPendingReplaceTemplateKey] = useState<string | null>(null);
  const industryTemplates = useMemo(() => listIndustryTemplates(), []);
  void LANDING_SECTION_CHOICES;

  useEffect(() => {
    pageRef.current = page;
  }, [page]);

  const dirty = useMemo(() => {
    if (!page) return Boolean(meta.title || meta.slug);
    return meta.title !== page.title
      || meta.slug !== page.slug
      || meta.seoTitle !== (page.seoTitle ?? "")
      || meta.seoDescription !== (page.seoDescription ?? "")
      || meta.robotsIndex !== page.robotsIndex
      || meta.robotsFollow !== page.robotsFollow
      || meta.canonicalUrl !== (page.canonicalUrl ?? "")
      || meta.ogTitle !== (page.ogTitle ?? "")
      || meta.ogDescription !== (page.ogDescription ?? "")
      || meta.ogImageUrl !== (page.ogImageUrl ?? "")
      || meta.primaryH1 !== (page.primaryH1 ?? "");
  }, [meta, page]);

  const load = useCallback(async (id: string) => {
    const [pageResult, sectionResult, home] = await Promise.all([
      getAdminLandingPage(id),
      listAdminLandingSections(id),
      getAdminLandingHome(),
    ]);
    if (!pageResult.ok) {
      setMessage(pageResult.message);
      return;
    }
    setPage(pageResult.data);
    setMeta(toMeta(pageResult.data));
    setSaved(true);
    if (sectionResult.ok) setSections(sectionResult.data);
    if (home.ok) setHomePageId(home.data.homePageId);
  }, []);

  useEffect(() => {
    if (pageId) void load(pageId);
  }, [load, pageId]);

  const persistOrder = useCallback(async (next: AdminLandingSection[]) => {
    const activePage = pageRef.current;
    if (!activePage) return false;
    const previous = sections;
    setSections(next);
    setBusy(true);
    const result = await reorderAdminLandingSections(activePage.pageId, next.map((row) => row.pageSectionId));
    setBusy(false);
    if (!result.ok) {
      setSections(previous);
      setMessage(result.message);
      void load(activePage.pageId);
      return false;
    }
    setSections(result.data);
    setMessage(undefined);
    return true;
  }, [load, sections]);

  const materializeTemplate = async (
    activePage: AdminLandingPage,
    templateKey: string,
  ): Promise<AdminLandingSection[] | null> => {
    const payloads = buildTemplateSectionPayloads(templateKey);
    const result = await replaceAdminLandingComposition(
      activePage.pageId,
      payloads.map((item) => ({ sectionType: item.hostType, config: item.config, isEnabled: true })),
    );
    if (!result.ok) {
      setMessage(result.message);
      return null;
    }
    return result.data;
  };

  const applyTemplateToDraft = async (templateKey: string, options?: { skipReplaceConfirm?: boolean }) => {
    const template = getIndustryTemplate(templateKey);
    if (!template) {
      setMessage("قالب انتخاب‌شده یافت نشد.");
      return;
    }

    if (page && sections.length > 0 && !options?.skipReplaceConfirm) {
      setTemplatePickerOpen(false);
      setPendingReplaceTemplateKey(templateKey);
      return;
    }

    setBusy(true);
    setMessage(undefined);
    setPendingReplaceTemplateKey(null);

    let activePage = page;
    if (!activePage) {
      const title = meta.title.trim() || template.nameFa;
      const slug = meta.slug.trim() || slugifyTemplate(template.templateKey);
      const createResult = await createAdminLandingPage({
        title,
        slug,
        locale: meta.locale || "fa",
        pageType: meta.pageType,
        seoTitle: meta.seoTitle.trim() || undefined,
        seoDescription: meta.seoDescription.trim() || undefined,
        robotsIndex: meta.robotsIndex,
        robotsFollow: meta.robotsFollow,
        canonicalUrl: meta.canonicalUrl.trim() || undefined,
        ogTitle: meta.ogTitle.trim() || undefined,
        ogDescription: meta.ogDescription.trim() || undefined,
        ogImageUrl: meta.ogImageUrl.trim() || undefined,
        primaryH1: meta.primaryH1.trim() || undefined,
      });
      if (!createResult.ok) {
        setBusy(false);
        setMessage(createResult.message);
        return;
      }
      activePage = createResult.data;
      setPage(activePage);
      pageRef.current = activePage;
      setMeta(toMeta(activePage));
    }

    const created = await materializeTemplate(activePage, templateKey);
    setBusy(false);
    if (!created) {
      if (!pageId && activePage) router.replace(`/admin/landing-pages/${activePage.pageId}`);
      return;
    }

    setSections(created);
    setSelectedTemplateKey(templateKey);
    setTemplatePickerOpen(false);
    setCreateMode("template");
    setSaved(true);
    setFocusedSectionId(created[0]?.pageSectionId ?? null);
    if (!pageId) router.replace(`/admin/landing-pages/${activePage.pageId}`);
  };

  const saveMeta = async (options?: { openWizardAfter?: boolean; insertAtAfter?: number | null }) => {
    if (!meta.title.trim() || !meta.slug.trim()) {
      setMessage("عنوان و آدرس صفحه لازم است.");
      return null;
    }
    if (!page && !meta.locale) {
      setMessage("زبان صفحه را انتخاب کنید.");
      return null;
    }
    setBusy(true);
    const payload = {
      title: meta.title.trim(),
      slug: meta.slug.trim(),
      locale: page?.locale ?? meta.locale,
      pageType: page ? undefined : meta.pageType,
      seoTitle: meta.seoTitle.trim() || undefined,
      seoDescription: meta.seoDescription.trim() || undefined,
      robotsIndex: meta.robotsIndex,
      robotsFollow: meta.robotsFollow,
      canonicalUrl: meta.canonicalUrl.trim() || undefined,
      ogTitle: meta.ogTitle.trim() || undefined,
      ogDescription: meta.ogDescription.trim() || undefined,
      ogImageUrl: meta.ogImageUrl.trim() || undefined,
      primaryH1: meta.primaryH1.trim() || undefined,
    };
    const result = page
      ? await updateAdminLandingPage(page.pageId, payload)
      : await createAdminLandingPage(payload);
    if (!result.ok) {
      setBusy(false);
      setMessage(result.message);
      return null;
    }

    setBusy(false);
    setPage(result.data);
    pageRef.current = result.data;
    setMeta(toMeta(result.data));
    setSaved(true);
    setMessage(undefined);
    if (!pageId) router.replace(`/admin/landing-pages/${result.data.pageId}`);
    if (options?.openWizardAfter) {
      setWizardMode("create");
      setEditing(null);
      setInsertAt(options.insertAtAfter ?? null);
      setWizardOpen(true);
    }
    return result.data;
  };

  const openAddSectionWizard = async (atIndex: number | null = null) => {
    setInsertAt(atIndex);
    if (page) {
      setWizardMode("create");
      setEditing(null);
      setWizardOpen(true);
      return;
    }
    await saveMeta({ openWizardAfter: true, insertAtAfter: atIndex });
  };

  const move = async (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= sections.length) return;
    const next = sections.slice();
    const current = next[index]!;
    next[index] = next[target]!;
    next[target] = current;
    await persistOrder(next);
  };

  const onSectionDragStart = (event: DragEvent<HTMLButtonElement>, sectionId: string) => {
    setDragId(sectionId);
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", sectionId);
  };

  const onSectionDragOver = (event: DragEvent<HTMLLIElement>, sectionId: string) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    if (dragOverId !== sectionId) setDragOverId(sectionId);
  };

  const onSectionDrop = async (event: DragEvent<HTMLLIElement>, targetId: string) => {
    event.preventDefault();
    const sourceId = dragId || event.dataTransfer.getData("text/plain");
    setDragId(null);
    setDragOverId(null);
    if (!sourceId || sourceId === targetId) return;
    const from = sections.findIndex((row) => row.pageSectionId === sourceId);
    const to = sections.findIndex((row) => row.pageSectionId === targetId);
    if (from < 0 || to < 0 || from === to) return;
    const next = sections.slice();
    const [moved] = next.splice(from, 1);
    if (!moved) return;
    next.splice(to, 0, moved);
    await persistOrder(next);
  };

  const toggle = async (row: AdminLandingSection) => {
    if (!page) return;
    setBusy(true);
    const result = await setAdminLandingSectionEnabled(page.pageId, row.pageSectionId, !row.isEnabled);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setSections((rows) => rows.map((item) => item.pageSectionId === row.pageSectionId ? result.data : item));
  };

  const remove = async (sectionId: string) => {
    if (!page) return;
    setBusy(true);
    const result = await deleteAdminLandingSection(page.pageId, sectionId);
    setBusy(false);
    setConfirmDelete(null);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    const remaining = sections.filter((item) => item.pageSectionId !== sectionId);
    setSections(remaining);
    if (editing?.pageSectionId === sectionId) {
      setEditing(null);
      setWizardOpen(false);
    }
    void load(page.pageId);
  };

  const publish = async () => {
    if (!page) return;
    if (dirty) await saveMeta();
    setBusy(true);
    const result = await setAdminLandingPageStatus(page.pageId, page.status === "Published" ? "Draft" : "Published");
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setPage(result.data);
  };

  const setHome = async () => {
    if (!page) return;
    if (page.status !== "Published") {
      setMessage("برای «تنظیم به عنوان صفحه اصلی» ابتدا صفحه را منتشر کنید.");
      return;
    }
    if (homePageId !== page.pageId) {
      const ok = window.confirm(
        homePageId
          ? "صفحهٔ اصلی فعلی جایگزین شود؟ دادهٔ کالا و قالب‌ها حذف نمی‌شود."
          : "این صفحه به‌عنوان صفحه اصلی فروشگاه تنظیم شود؟",
      );
      if (!ok) return;
    }
    setBusy(true);
    const result = await setAdminLandingHome(homePageId === page.pageId ? null : page.pageId);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setHomePageId(result.data.homePageId);
  };

  if (!pageId && !page && createMode === null) {
    return (
      <main data-testid="admin-landing-page-editor" className="space-y-5">
        <div>
          <Link href="/admin/landing-pages" className="text-sm text-muted">بازگشت به فهرست</Link>
          <h1 className="mt-1 text-xl font-black">ایجاد صفحه فروشگاه</h1>
          <p className="mt-1 text-sm text-muted">از صفحه خالی شروع کنید یا یک قالب آمادهٔ صنعتی را انتخاب کنید.</p>
        </div>
        <div className="grid gap-4 md:grid-cols-2" data-testid="composition-start-mode">
          <button type="button" className="rounded-2xl border border-border bg-surface-elevated p-6 text-start hover:border-primary" data-testid="start-blank" onClick={() => setCreateMode("blank")}>
            <p className="text-lg font-black">شروع از صفحه خالی</p>
            <p className="mt-2 text-sm text-muted">یک پیش‌نویس خالی بسازید و بخش‌ها را خودتان اضافه کنید.</p>
          </button>
          <button type="button" className="rounded-2xl border border-border bg-surface-elevated p-6 text-start hover:border-primary" data-testid="start-from-template" onClick={() => {
            const first = industryTemplates[0];
            setCreateMode("template");
            setTemplatePickerOpen(true);
            if (first) {
              setSelectedTemplateKey(first.templateKey);
              setMeta((current) => ({
                ...current,
                title: current.title || first.nameFa,
              }));
            }
          }}>
            <p className="text-lg font-black">شروع از قالب آماده</p>
            <p className="mt-2 text-sm text-muted">یکی از ۱۰ قالب صنعتی را انتخاب کنید؛ نتیجه یک پیش‌نویس عادی و قابل‌ویرایش است.</p>
          </button>
        </div>
      </main>
    );
  }

  if ((!pageId && !page && createMode === "template" && templatePickerOpen) || (page && templatePickerOpen)) {
    return (
      <AdminTemplateSelectionWorkspace
        selectedTemplateKey={selectedTemplateKey}
        onBack={() => {
          if (!page) {
            setCreateMode(null);
            setTemplatePickerOpen(false);
          } else {
            setTemplatePickerOpen(false);
          }
        }}
        onStartBlank={() => {
          if (!page) {
            setCreateMode("blank");
            setSelectedTemplateKey(null);
            setTemplatePickerOpen(false);
          } else {
            setTemplatePickerOpen(false);
          }
        }}
        onSelect={(templateKey, template) => {
          setSelectedTemplateKey(templateKey);
          setMeta((current) => ({
            ...current,
            title: current.title || template.nameFa,
            slug: current.slug || "",
          }));
        }}
        onConfirmTemplate={() => {
          const key = selectedTemplateKey ?? industryTemplates[0]?.templateKey;
          if (!key) return;
          void applyTemplateToDraft(key);
        }}
      />
    );
  }

  return (
    <main
      data-testid="admin-landing-page-editor"
      data-page-workspace="1"
      data-page-type={page?.pageType ?? meta.pageType}
      data-unified-section-workspace="1"
    >
      <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link href="/admin/landing-pages" className="text-sm text-muted">بازگشت به فهرست</Link>
          <h1 className="mt-1 text-xl font-black">{page ? "فضای کار صفحه" : "ایجاد صفحه فروشگاه"}</h1>
          <p className="mt-1 text-sm text-muted">
            {dirty ? "تغییرات ذخیره نشده است." : saved ? "همهٔ تغییرات ذخیره شده‌اند." : "آمادهٔ ویرایش"}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {page ? (
            <>
              <Link className="rounded-xl border px-4 py-2 text-sm font-bold" href={`/admin/landing-pages/${page.pageId}/preview`}>
                پیش‌نمایش
              </Link>
              <button
                type="button"
                className="rounded-xl border px-4 py-2 text-sm font-bold"
                disabled={busy}
                data-testid="apply-template-action"
                onClick={() => {
                  setTemplatePickerOpen(true);
                  if (!selectedTemplateKey && industryTemplates[0]) setSelectedTemplateKey(industryTemplates[0].templateKey);
                }}
              >
                اعمال قالب
              </button>
              <button type="button" className="rounded-xl border px-4 py-2 text-sm font-bold" disabled={busy} onClick={() => void publish()}>
                {page.status === "Published" ? "بازگرداندن به پیش‌نویس" : "انتشار"}
              </button>
              <button
                type="button"
                className="rounded-xl border px-4 py-2 text-sm font-bold"
                disabled={busy}
                onClick={() => void setHome()}
                data-testid={homePageId === page.pageId ? "restore-default-home" : "set-as-home"}
              >
                {homePageId === page.pageId ? "بازگردانی صفحه اصلی پیش‌فرض" : "تنظیم به عنوان صفحه اصلی"}
              </button>
            </>
          ) : null}
          <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" disabled={busy} onClick={() => void saveMeta()}>
            ذخیره
          </button>
        </div>
      </div>

      <section className="mb-5 rounded-2xl border border-border bg-surface-elevated p-5" data-testid="page-workspace-meta">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 className="font-black">اطلاعات صفحه</h2>
          <div className="flex flex-wrap items-center gap-2">
            {page ? (
              <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${page.status === "Published" ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}>
                {page.status === "Published" ? "منتشرشده" : "پیش‌نویس"}
              </span>
            ) : null}
            {homePageId && page && homePageId === page.pageId ? (
              <span className="rounded-full bg-blue-50 px-2 py-0.5 text-xs font-bold text-blue-700" data-testid="home-current-indicator">خانه فعلی</span>
            ) : null}
            <span
              className="rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-700"
              data-testid={page ? "page-language-fixed" : "page-language-create"}
            >
              زبان صفحه: {localeLabel(page?.locale ?? meta.locale)}
            </span>
          </div>
        </div>
        <div className="grid gap-3 md:grid-cols-2">
          <label className="text-sm">
            <span className="mb-1 block font-bold">عنوان</span>
            <input className="w-full rounded-xl border px-3 py-2" value={meta.title} onChange={(e) => setMeta({ ...meta, title: e.target.value })} />
          </label>
          <label className="text-sm">
            <span className="mb-1 block font-bold">آدرس صفحه (slug)</span>
            <input className="w-full rounded-xl border px-3 py-2" dir="ltr" value={meta.slug} onChange={(e) => setMeta({ ...meta, slug: e.target.value })} />
            <span className="mt-1 block text-xs text-muted" dir="ltr">
              مسیر عمومی: {publicPathForStorePage({ pageType: page?.pageType ?? meta.pageType, slug: meta.slug || "slug" })}
            </span>
          </label>
          {!page ? (
            <>
              <label className="text-sm" data-testid="create-page-type">
                <span className="mb-1 block font-bold">نوع صفحه</span>
                <select
                  className="w-full rounded-xl border px-3 py-2"
                  value={meta.pageType}
                  onChange={(e) => setMeta({ ...meta, pageType: e.target.value === "Home" ? "Home" : "Landing" })}
                >
                  <option value="Landing">فرود (Landing)</option>
                  <option value="Home">خانه (Home)</option>
                </select>
              </label>
              <label className="text-sm" data-testid="page-language-create-field">
                <span className="mb-1 block font-bold">زبان صفحه</span>
                <select className="w-full rounded-xl border px-3 py-2" value={meta.locale} onChange={(e) => setMeta({ ...meta, locale: e.target.value })}>
                  <option value="fa">فارسی</option>
                  <option value="en">انگلیسی</option>
                </select>
                <span className="mt-1 block text-xs text-muted">زبان فقط هنگام ایجاد انتخاب می‌شود و برای همهٔ بخش‌ها ثابت می‌ماند.</span>
              </label>
            </>
          ) : (
            <>
              <div className="text-sm">
                <span className="mb-1 block font-bold">نوع صفحه</span>
                <p className="rounded-xl border bg-slate-50 px-3 py-2 font-bold">{page.pageType === "Home" ? "خانه" : "فرود"}</p>
              </div>
              <div className="text-sm" data-testid="page-language-edit-fixed">
                <span className="mb-1 block font-bold">زبان صفحه</span>
                <p className="rounded-xl border bg-slate-50 px-3 py-2 font-bold">{localeLabel(page.locale)}</p>
                <span className="mt-1 block text-xs text-muted">ترجمهٔ صفحه جریان جداگانه‌ای است؛ بخش‌ها زبان مستقل ندارند.</span>
              </div>
            </>
          )}
        </div>
      </section>

      <section className="mb-5 rounded-2xl border border-border bg-surface-elevated p-5" data-testid="page-seo-panel">
        <button
          type="button"
          className="mb-4 flex w-full items-center justify-between gap-3 text-start"
          onClick={() => setSeoOpen((open) => !open)}
          aria-expanded={seoOpen}
          data-testid="page-seo-toggle"
        >
          <h2 className="font-black">سئو و اشتراک‌گذاری</h2>
          <ChevronDown className={`h-4 w-4 transition ${seoOpen ? "rotate-180" : ""}`} />
        </button>
        {seoOpen ? (
          <>
            <div className="grid gap-3 md:grid-cols-2">
              <label className="text-sm">
                <span className="mb-1 block font-bold">عنوان سئو</span>
                <input className="w-full rounded-xl border px-3 py-2" value={meta.seoTitle} onChange={(e) => setMeta({ ...meta, seoTitle: e.target.value })} />
              </label>
              <label className="text-sm">
                <span className="mb-1 block font-bold">H1 اصلی (اختیاری)</span>
                <input className="w-full rounded-xl border px-3 py-2" value={meta.primaryH1} onChange={(e) => setMeta({ ...meta, primaryH1: e.target.value })} placeholder="پیش‌فرض: عنوان صفحه" />
              </label>
              <label className="text-sm md:col-span-2">
                <span className="mb-1 block font-bold">توضیح سئو</span>
                <textarea className="min-h-20 w-full rounded-xl border px-3 py-2" value={meta.seoDescription} onChange={(e) => setMeta({ ...meta, seoDescription: e.target.value })} />
              </label>
              <label className="text-sm">
                <span className="mb-1 block font-bold">Canonical URL (اختیاری)</span>
                <input className="w-full rounded-xl border px-3 py-2" dir="ltr" value={meta.canonicalUrl} onChange={(e) => setMeta({ ...meta, canonicalUrl: e.target.value })} />
              </label>
              <div className="flex flex-wrap items-center gap-4 text-sm">
                <label className="inline-flex items-center gap-2 font-bold">
                  <input type="checkbox" checked={meta.robotsIndex} onChange={(e) => setMeta({ ...meta, robotsIndex: e.target.checked })} />
                  Index
                </label>
                <label className="inline-flex items-center gap-2 font-bold">
                  <input type="checkbox" checked={meta.robotsFollow} onChange={(e) => setMeta({ ...meta, robotsFollow: e.target.checked })} />
                  Follow
                </label>
              </div>
              <label className="text-sm">
                <span className="mb-1 block font-bold">OpenGraph Title</span>
                <input className="w-full rounded-xl border px-3 py-2" value={meta.ogTitle} onChange={(e) => setMeta({ ...meta, ogTitle: e.target.value })} />
              </label>
              <label className="text-sm">
                <span className="mb-1 block font-bold">OpenGraph Image URL</span>
                <input className="w-full rounded-xl border px-3 py-2" dir="ltr" value={meta.ogImageUrl} onChange={(e) => setMeta({ ...meta, ogImageUrl: e.target.value })} />
              </label>
              <label className="text-sm md:col-span-2">
                <span className="mb-1 block font-bold">OpenGraph Description</span>
                <textarea className="min-h-16 w-full rounded-xl border px-3 py-2" value={meta.ogDescription} onChange={(e) => setMeta({ ...meta, ogDescription: e.target.value })} />
              </label>
            </div>
            <div className="mt-4 grid gap-3 md:grid-cols-2" data-testid="seo-snippet-preview">
              <div className="rounded-xl border bg-white p-4">
                <p className="text-xs text-muted">پیش‌نمایش نتیجهٔ جستجو</p>
                <p className="mt-2 text-lg text-[#1a0dab]" dir="auto">{meta.seoTitle || meta.title || "عنوان سئو"}</p>
                <p className="text-sm text-[#006621]" dir="ltr">
                  {meta.canonicalUrl
                    || `https://store.example${publicPathForStorePage({ pageType: page?.pageType ?? meta.pageType, slug: meta.slug || "slug" })}`}
                </p>
                <p className="mt-1 text-sm text-[#4d5156]" dir="auto">{meta.seoDescription || "توضیح سئو اینجا نمایش داده می‌شود."}</p>
                <p className="mt-2 text-xs font-bold text-muted">
                  ایندکس: {meta.robotsIndex ? "بله" : "خیر"} · Follow: {meta.robotsFollow ? "بله" : "خیر"}
                </p>
              </div>
              <div className="rounded-xl border bg-slate-50 p-4">
                <p className="text-xs text-muted">پیش‌نمایش OpenGraph</p>
                {meta.ogImageUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={meta.ogImageUrl} alt="" className="mt-2 h-28 w-full rounded-lg object-cover" />
                ) : (
                  <div className="mt-2 flex h-28 items-center justify-center rounded-lg bg-slate-200 text-xs text-muted">بدون تصویر</div>
                )}
                <p className="mt-2 font-bold" dir="auto">{meta.ogTitle || meta.seoTitle || meta.title || "عنوان اشتراک"}</p>
                <p className="text-sm text-muted" dir="auto">{meta.ogDescription || meta.seoDescription || "توضیح اشتراک"}</p>
              </div>
            </div>
          </>
        ) : null}
      </section>

      <section className="rounded-2xl border border-border bg-surface-elevated p-5" data-testid="landing-section-composer" data-canonical-order="1">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-black">بخش‌های صفحه</h2>
            <p className="mt-1 text-xs text-muted">ترتیب با فلش یا کشیدن دستگیره ذخیره می‌شود · پیش‌نمایش هندسی جدا حذف شده است.</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-xs font-bold text-muted">{sections.length.toLocaleString("fa-IR")} بخش</span>
            <button
              type="button"
              className="inline-flex items-center gap-2 rounded-xl bg-slate-900 px-4 py-2 text-sm font-bold text-white disabled:opacity-40"
              disabled={busy}
              onClick={() => void openAddSectionWizard(sections.length)}
              data-testid="landing-add-section"
            >
              <Plus className="h-4 w-4" />
              افزودن بخش
            </button>
          </div>
        </div>

        {sections.length === 0 ? (
          <div className="rounded-xl border border-dashed px-4 py-8 text-center" data-testid="blank-page-empty-state">
            <p className="text-sm font-bold">صفحه هنوز بخشی ندارد.</p>
            <p className="mt-2 text-sm text-muted">پس از ذخیرهٔ پیش‌نویس، اولین بخش را با جادوگر اضافه کنید یا یک قالب اعمال کنید.</p>
            <button
              type="button"
              className="mt-4 inline-flex items-center gap-2 rounded-xl bg-slate-900 px-4 py-2 text-sm font-bold text-white disabled:opacity-40"
              disabled={busy}
              onClick={() => void openAddSectionWizard(0)}
              data-testid="add-first-section-cta"
            >
              <Plus className="h-4 w-4" />
              افزودن اولین بخش
            </button>
          </div>
        ) : (
          <div className="space-y-1" data-testid="unified-section-workspace" data-workspace-composition="1">
            <InsertBetweenAffordance
              disabled={busy}
              testId="insert-before-first"
              onClick={() => void openAddSectionWizard(0)}
            />
            <ol className="space-y-1">
              {sections.map((row, index) => {
                const config = parseLandingConfig(row.config);
                const variantKey = typeof config.variantKey === "string" ? config.variantKey : undefined;
                const sectionTypeKey = resolveSectionTypeKey(row.sectionType, config);
                const typeLabel = sectionTypeKey ? (getSectionType(sectionTypeKey)?.nameFa ?? landingSectionLabel(row.sectionType)) : landingSectionLabel(row.sectionType);
                const variantLabel = variantKey ? getVariant(variantKey)?.nameFa : null;
                const heightPreset = typeof config.heightPreset === "string" ? config.heightPreset : null;
                const heightFa = heightPreset && heightPreset in SIZE_PRESET_CONTRACTS
                  ? SIZE_PRESET_CONTRACTS[heightPreset as keyof typeof SIZE_PRESET_CONTRACTS].nameFa
                  : null;
                const focused = focusedSectionId === row.pageSectionId;
                const sourceWarning = incompleteSourceWarning(row.sectionType, config);
                const sourceSummary = summarizeLandingSection(row.sectionType, config);
                return (
                  <li key={row.pageSectionId}>
                    <div
                      className={`rounded-xl border px-3 py-3 ${focused ? "border-[#2563EB] ring-2 ring-[#2563EB]/15" : ""} ${
                        row.isEnabled ? "bg-white" : "bg-slate-100 opacity-80"
                      } ${dragOverId === row.pageSectionId ? "border-dashed border-[#2563EB]" : ""}`}
                      data-testid="composer-section-card"
                      data-section-enabled={row.isEnabled ? "true" : "false"}
                      data-section-position={index + 1}
                      id={`composer-section-${row.pageSectionId}`}
                      onDragOver={(event) => onSectionDragOver(event, row.pageSectionId)}
                      onDrop={(event) => void onSectionDrop(event, row.pageSectionId)}
                      onDragLeave={() => {
                        if (dragOverId === row.pageSectionId) setDragOverId(null);
                      }}
                      onClick={() => setFocusedSectionId(row.pageSectionId)}
                    >
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div className="flex min-w-0 flex-1 items-start gap-3">
                          <button
                            type="button"
                            className="mt-0.5 cursor-grab rounded-lg border bg-slate-50 p-2 text-slate-500 active:cursor-grabbing"
                            draggable
                            aria-label="جابجایی با کشیدن"
                            title="کشیدن برای تغییر ترتیب"
                            data-testid="section-drag-handle"
                            disabled={busy}
                            onDragStart={(event) => onSectionDragStart(event, row.pageSectionId)}
                            onDragEnd={() => {
                              setDragId(null);
                              setDragOverId(null);
                            }}
                            onClick={(event) => event.stopPropagation()}
                          >
                            <GripVertical className="h-4 w-4" />
                          </button>
                          <div className="min-w-0">
                            <div className="flex flex-wrap items-center gap-2">
                              <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-bold text-slate-600" data-testid="section-position-badge">
                                {(index + 1).toLocaleString("fa-IR")}
                              </span>
                              <strong>{typeLabel}</strong>
                              {variantLabel ? <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-bold text-slate-700">{variantLabel}</span> : null}
                              <span
                                className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${row.isEnabled ? "bg-emerald-50 text-emerald-700" : "bg-slate-300 text-slate-700"}`}
                                data-testid={row.isEnabled ? "section-enabled-badge" : "section-disabled-badge"}
                              >
                                {row.isEnabled ? "فعال" : "غیرفعال"}
                              </span>
                              {heightFa ? <span className="rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-bold text-blue-700">{heightFa}</span> : null}
                            </div>
                            <p className="mt-1 text-xs text-muted" data-testid="section-source-summary">{sourceSummary}</p>
                            {sourceWarning ? (
                              <p className="mt-1 text-xs font-bold text-amber-700" data-testid="section-incomplete-source">{sourceWarning}</p>
                            ) : null}
                          </div>
                        </div>
                        <div className="flex flex-wrap items-center gap-2">
                          <button
                            type="button"
                            aria-label="بالا"
                            title="انتقال به بالا"
                            className="rounded-lg border p-2 min-h-11 min-w-11"
                            data-testid="section-move-up"
                            disabled={busy || index === 0}
                            onClick={(event) => {
                              event.stopPropagation();
                              void move(index, -1);
                            }}
                          >
                            <ChevronUp className="h-4 w-4" />
                          </button>
                          <button
                            type="button"
                            aria-label="پایین"
                            title="انتقال به پایین"
                            className="rounded-lg border p-2 min-h-11 min-w-11"
                            data-testid="section-move-down"
                            disabled={busy || index === sections.length - 1}
                            onClick={(event) => {
                              event.stopPropagation();
                              void move(index, 1);
                            }}
                          >
                            <ChevronDown className="h-4 w-4" />
                          </button>
                          <button
                            type="button"
                            className="rounded-lg border px-3 py-2 text-xs font-bold min-h-11"
                            disabled={busy}
                            data-testid="section-toggle-enabled"
                            onClick={(event) => {
                              event.stopPropagation();
                              void toggle(row);
                            }}
                          >
                            {row.isEnabled ? <span className="inline-flex items-center gap-1"><Eye className="h-3.5 w-3.5" /> فعال</span> : <span className="inline-flex items-center gap-1"><EyeOff className="h-3.5 w-3.5" /> غیرفعال</span>}
                          </button>
                          <button
                            type="button"
                            className="rounded-lg border px-3 py-2 text-xs font-bold min-h-11"
                            data-testid="section-edit"
                            onClick={(event) => {
                              event.stopPropagation();
                              setEditing(row);
                              setWizardMode("edit");
                              setInsertAt(null);
                              setWizardOpen(true);
                            }}
                          >
                            <span className="inline-flex items-center gap-1"><Pencil className="h-3.5 w-3.5" /> ویرایش</span>
                          </button>
                          {confirmDelete === row.pageSectionId ? (
                            <span className="inline-flex items-center gap-2 text-xs" data-testid="delete-section-confirm">
                              حذف شود؟
                              <button type="button" className="font-bold text-red-600" onClick={() => void remove(row.pageSectionId)}>بله</button>
                              <button type="button" onClick={() => setConfirmDelete(null)}>خیر</button>
                            </span>
                          ) : (
                            <button
                              type="button"
                              className="rounded-lg border px-3 py-2 text-xs min-h-11"
                              data-testid="section-delete"
                              onClick={(event) => {
                                event.stopPropagation();
                                setConfirmDelete(row.pageSectionId);
                              }}
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
                          )}
                        </div>
                      </div>
                    </div>
                    <InsertBetweenAffordance
                      disabled={busy}
                      testId={`insert-after-${index}`}
                      onClick={() => void openAddSectionWizard(index + 1)}
                    />
                  </li>
                );
              })}
            </ol>
          </div>
        )}
      </section>

      {message ? <p className="mt-4 text-sm text-red-600">{message}</p> : null}

      {pendingReplaceTemplateKey ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          data-testid="template-replace-confirm"
          role="dialog"
          aria-modal="true"
          aria-labelledby="template-replace-title"
        >
          <div className="w-full max-w-md rounded-2xl bg-white p-5 shadow-xl">
            <h3 id="template-replace-title" className="text-lg font-black">جایگزینی ترکیب صفحه</h3>
            <p className="mt-2 text-sm text-muted">
              ترکیب فعلی صفحه با بخش‌های این قالب جایگزین شود؟ دادهٔ کالا، دسته، برند، بنر، مطلب، استوری و نظرات حذف نمی‌شود؛ فقط ترکیب صفحه عوض می‌شود.
            </p>
            <div className="mt-4 flex flex-wrap justify-end gap-2">
              <button
                type="button"
                className="rounded-xl border px-4 py-2 text-sm font-bold"
                data-testid="template-replace-cancel"
                onClick={() => setPendingReplaceTemplateKey(null)}
              >
                انصراف
              </button>
              <button
                type="button"
                className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                data-testid="template-replace-confirm-yes"
                disabled={busy}
                onClick={() => {
                  const key = pendingReplaceTemplateKey;
                  setPendingReplaceTemplateKey(null);
                  if (key) void applyTemplateToDraft(key, { skipReplaceConfirm: true });
                }}
              >
                جایگزینی ترکیب
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {wizardOpen ? (
        <AdminSectionWizard
          key={editing?.pageSectionId ?? `create-${insertAt ?? "end"}`}
          open={wizardOpen}
          mode={wizardMode}
          initialHostType={editing?.sectionType}
          initialConfig={editing ? parseLandingConfig(editing.config) : undefined}
          busy={busy}
          onClose={() => {
            setWizardOpen(false);
            setEditing(null);
            setInsertAt(null);
          }}
          onSave={async (payload) => {
            const activePage = pageRef.current;
            if (!activePage) {
              setMessage("ابتدا مشخصات صفحه را ذخیره کنید.");
              return;
            }
            setBusy(true);
            if (wizardMode === "edit" && editing) {
              const result = await updateAdminLandingSection(activePage.pageId, editing.pageSectionId, payload.config);
              setBusy(false);
              if (!result.ok) {
                setMessage(result.message);
                return;
              }
              setSections((rows) => rows.map((item) => item.pageSectionId === editing.pageSectionId ? result.data : item));
            } else {
              const config = payload.config.variantKey
                ? payload.config
                : defaultConfigForCompositionSection(payload.sectionTypeKey, payload.variantKey);
              const result = await addAdminLandingSection(
                activePage.pageId,
                payload.hostType,
                config,
                insertAt ?? undefined,
              );
              setBusy(false);
              if (!result.ok) {
                setMessage(result.message);
                return;
              }
              if (typeof insertAt === "number") {
                const listed = await listAdminLandingSections(activePage.pageId);
                if (listed.ok) setSections(listed.data);
                else void load(activePage.pageId);
              } else {
                setSections((rows) => [...rows, result.data]);
              }
              setFocusedSectionId(result.data.pageSectionId);
            }
            setWizardOpen(false);
            setEditing(null);
            setInsertAt(null);
            setMessage(undefined);
          }}
        />
      ) : null}
    </main>
  );
}
