"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState, type DragEvent } from "react";
import {
  AlertTriangle,
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
  deleteAdminLandingPage,
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
import { buildLandingPublishReadiness } from "./admin-landing-page-readiness.ts";
import {
  LandingChecklistModal,
  LandingPublishIssuesModal,
  LandingPublishReadinessCard,
} from "./admin-landing-page-readiness-ui.tsx";
import { Dialog } from "../../../design-system/index.ts";

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

export function AdminLandingPageComposer({ pageId }: { pageId?: string }) {
  const router = useRouter();
  const [page, setPage] = useState<AdminLandingPage | null>(null);
  const pageRef = useRef<AdminLandingPage | null>(null);
  const [meta, setMeta] = useState<Meta>(toMeta());
  const [sections, setSections] = useState<AdminLandingSection[]>([]);
  const [homePageId, setHomePageId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [saved, setSaved] = useState(true);
  const [wizardOpen, setWizardOpen] = useState(false);
  const [wizardMode, setWizardMode] = useState<"create" | "edit">("create");
  const [editing, setEditing] = useState<AdminLandingSection | null>(null);
  const [insertAt, setInsertAt] = useState<number | null>(null);
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);
  const [deletePageConfirmOpen, setDeletePageConfirmOpen] = useState(false);
  const [focusedSectionId, setFocusedSectionId] = useState<string | null>(null);
  const [createMode, setCreateMode] = useState<"blank" | "template" | null>(pageId ? "blank" : null);
  const [selectedTemplateKey, setSelectedTemplateKey] = useState<string | null>(null);
  const [templatePickerOpen, setTemplatePickerOpen] = useState(false);
  const [metaTab, setMetaTab] = useState<"page" | "seo">("page");
  const [leaveConfirmOpen, setLeaveConfirmOpen] = useState(false);
  const [checklistOpen, setChecklistOpen] = useState(false);
  const [issuesModal, setIssuesModal] = useState<{
    mode: "publish-block" | "save-as-draft";
    issues: ReturnType<typeof buildLandingPublishReadiness>["missing"];
  } | null>(null);
  const [errorModal, setErrorModal] = useState<{ title: string; description: string } | null>(null);
  const [dragId, setDragId] = useState<string | null>(null);
  const [dragOverId, setDragOverId] = useState<string | null>(null);
  const [pendingReplaceTemplateKey, setPendingReplaceTemplateKey] = useState<string | null>(null);
  const industryTemplates = useMemo(() => listIndustryTemplates(), []);
  void LANDING_SECTION_CHOICES;

  useEffect(() => {
    pageRef.current = page;
  }, [page]);

  const dirty = useMemo(() => {
    if (!page) return Boolean(meta.title);
    const homeLocked = page.pageType === "Home" || homePageId === page.pageId;
    const slugHidden = page.pageType === "Landing" || homeLocked;
    return meta.title !== page.title
      || (!slugHidden && meta.slug !== page.slug)
      || meta.seoTitle !== (page.seoTitle ?? "")
      || meta.seoDescription !== (page.seoDescription ?? "")
      || meta.robotsIndex !== page.robotsIndex
      || meta.robotsFollow !== page.robotsFollow
      || meta.canonicalUrl !== (page.canonicalUrl ?? "")
      || meta.ogTitle !== (page.ogTitle ?? "")
      || meta.ogDescription !== (page.ogDescription ?? "")
      || meta.ogImageUrl !== (page.ogImageUrl ?? "")
      || meta.primaryH1 !== (page.primaryH1 ?? "");
  }, [meta, page, homePageId]);

  const readiness = useMemo(
    () =>
      buildLandingPublishReadiness({
        title: meta.title,
        slug: meta.slug,
        pageType: page?.pageType ?? meta.pageType,
        isHome: (page?.pageType ?? meta.pageType) === "Home" || (page != null && homePageId === page.pageId),
        seoTitle: meta.seoTitle,
        seoDescription: meta.seoDescription,
        sections,
      }),
    [meta, page, homePageId, sections],
  );

  const uiLocale = page?.locale ?? meta.locale ?? "fa";

  useEffect(() => {
    if (!dirty) return;
    const onBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", onBeforeUnload);
    return () => window.removeEventListener("beforeunload", onBeforeUnload);
  }, [dirty]);

  const showError = useCallback((title: string, description: string) => {
    setErrorModal({ title, description });
  }, []);

  const load = useCallback(async (id: string) => {
    const [pageResult, sectionResult, home] = await Promise.all([
      getAdminLandingPage(id),
      listAdminLandingSections(id),
      getAdminLandingHome(),
    ]);
    if (!pageResult.ok) {
      showError("بارگذاری ناموفق بود", pageResult.message);
      return;
    }
    setPage(pageResult.data);
    setMeta(toMeta(pageResult.data));
    setSaved(true);
    if (sectionResult.ok) setSections(sectionResult.data);
    if (home.ok) setHomePageId(home.data.homePageId);
  }, [showError]);

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
      showError("ذخیرهٔ ترتیب ناموفق بود", result.message);
      void load(activePage.pageId);
      return false;
    }
    setSections(result.data);
    return true;
  }, [load, sections, showError]);

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
      showError("عملیات ناموفق بود", result.message);
      return null;
    }
    return result.data;
  };

  const applyTemplateToDraft = async (templateKey: string, options?: { skipReplaceConfirm?: boolean }) => {
    const template = getIndustryTemplate(templateKey);
    if (!template) {
      showError("قالب یافت نشد", "قالب انتخاب‌شده یافت نشد.");
      return;
    }

    if (page && sections.length > 0 && !options?.skipReplaceConfirm) {
      setTemplatePickerOpen(false);
      setPendingReplaceTemplateKey(templateKey);
      return;
    }

    setBusy(true);
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
        showError("ایجاد صفحه ناموفق بود", createResult.message);
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

  const saveMeta = async (options?: {
    openWizardAfter?: boolean;
    insertAtAfter?: number | null;
    forceDraft?: boolean;
    skipIncompletePrompt?: boolean;
  }) => {
    const treatingAsHome =
      (page?.pageType ?? meta.pageType) === "Home" || (page != null && homePageId === page.pageId);
    const pageType = page?.pageType ?? meta.pageType;
    // صفحهٔ فرود در UI اسلاگ ندارد؛ برای یکتایی API در پس‌زمینه ساخته می‌شود.
    const slugForSave = treatingAsHome || pageType === "Landing"
      ? (page?.slug?.trim() || meta.slug.trim() || slugifyTemplate(treatingAsHome ? "home" : meta.title.trim() || "landing"))
      : meta.slug.trim();
    if (!meta.title.trim()) {
      showError(
        uiLocale.startsWith("en") ? "Cannot save" : "امکان ذخیره نیست",
        uiLocale.startsWith("en") ? "Page title is required." : "عنوان صفحه لازم است.",
      );
      return null;
    }
    if (!page && !meta.locale) {
      showError(
        uiLocale.startsWith("en") ? "Cannot save" : "امکان ذخیره نیست",
        uiLocale.startsWith("en") ? "Select a page language." : "زبان صفحه را انتخاب کنید.",
      );
      return null;
    }

    const currentReadiness = buildLandingPublishReadiness({
      title: meta.title,
      slug: "",
      pageType,
      isHome: treatingAsHome,
      seoTitle: meta.seoTitle,
      seoDescription: meta.seoDescription,
      sections,
    });

    if (!options?.skipIncompletePrompt && !currentReadiness.canPublish && !options?.forceDraft) {
      setIssuesModal({ mode: "save-as-draft", issues: currentReadiness.missing });
      return null;
    }

    setBusy(true);
    const payload = {
      title: meta.title.trim(),
      slug: slugForSave,
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
      showError(uiLocale.startsWith("en") ? "Save failed" : "ذخیره ناموفق بود", result.message);
      return null;
    }

    let savedPage = result.data;
    const shouldForceDraft =
      Boolean(options?.forceDraft) || (!currentReadiness.canPublish && savedPage.status === "Published");
    if (shouldForceDraft && savedPage.status === "Published") {
      const draftResult = await setAdminLandingPageStatus(savedPage.pageId, "Draft");
      if (!draftResult.ok) {
        setBusy(false);
        showError(uiLocale.startsWith("en") ? "Draft switch failed" : "بازگشت به پیش‌نویس ناموفق بود", draftResult.message);
        return null;
      }
      savedPage = draftResult.data;
    }

    setBusy(false);
    setPage(savedPage);
    pageRef.current = savedPage;
    setMeta(toMeta(savedPage));
    setSaved(true);
    if (!pageId) router.replace(`/admin/landing-pages/${savedPage.pageId}`);
    if (options?.openWizardAfter) {
      setWizardMode("create");
      setEditing(null);
      setInsertAt(options.insertAtAfter ?? null);
      setWizardOpen(true);
    }
    return savedPage;
  };

  const openAddSectionWizard = async (atIndex: number | null = null) => {
    setInsertAt(atIndex);
    if (page) {
      setWizardMode("create");
      setEditing(null);
      setWizardOpen(true);
      return;
    }
    await saveMeta({ openWizardAfter: true, insertAtAfter: atIndex, skipIncompletePrompt: true, forceDraft: true });
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
      showError(uiLocale.startsWith("en") ? "Update failed" : "به‌روزرسانی ناموفق بود", result.message);
      return;
    }
    setSections((rows) => rows.map((item) => item.pageSectionId === row.pageSectionId ? result.data : item));
  };

  const remove = async (sectionId: string) => {
    if (!page) return;
    setBusy(true);
    const result = await deleteAdminLandingSection(page.pageId, sectionId);
    setBusy(false);
    setDeleteTargetId(null);
    if (!result.ok) {
      showError(uiLocale.startsWith("en") ? "Delete failed" : "حذف ناموفق بود", result.message);
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
    if (page.status === "Published") {
      setBusy(true);
      const result = await setAdminLandingPageStatus(page.pageId, "Draft");
      setBusy(false);
      if (!result.ok) {
        showError(uiLocale.startsWith("en") ? "Unpublish failed" : "بازگشت به پیش‌نویس ناموفق بود", result.message);
        return;
      }
      setPage(result.data);
      return;
    }

    if (dirty) {
      const saved = await saveMeta({ skipIncompletePrompt: true, forceDraft: true });
      if (!saved) return;
    }

    const current = buildLandingPublishReadiness({
      title: meta.title,
      slug: meta.slug,
      pageType: page.pageType,
      isHome: page.pageType === "Home" || homePageId === page.pageId,
      seoTitle: meta.seoTitle,
      seoDescription: meta.seoDescription,
      sections,
    });
    if (!current.canPublish) {
      setIssuesModal({ mode: "publish-block", issues: current.missing });
      return;
    }

    setBusy(true);
    const result = await setAdminLandingPageStatus(page.pageId, "Published");
    setBusy(false);
    if (!result.ok) {
      showError(uiLocale.startsWith("en") ? "Publish failed" : "انتشار ناموفق بود", result.message);
      return;
    }
    setPage(result.data);
  };

  const removePage = async () => {
    if (!page) return;
    setBusy(true);
    const result = await deleteAdminLandingPage(page.pageId);
    setBusy(false);
    setDeletePageConfirmOpen(false);
    if (!result.ok) {
      showError(uiLocale.startsWith("en") ? "Delete failed" : "حذف ناموفق بود", result.message);
      return;
    }
    router.push("/admin/landing-pages");
  };

  const setHome = async () => {
    if (!page) return;
    if (page.pageType !== "Landing") {
      showError(
        uiLocale.startsWith("en") ? "Home selection blocked" : "تنظیم خانه ممکن نیست",
        uiLocale.startsWith("en")
          ? "Only landing pages can be set as the store home."
          : "فقط صفحات فرود را می‌توان به‌عنوان صفحه اصلی تنظیم کرد.",
      );
      return;
    }
    if (page.status !== "Published") {
      showError(
        uiLocale.startsWith("en") ? "Publish required" : "انتشار لازم است",
        uiLocale.startsWith("en")
          ? "Publish the page before setting it as home."
          : "برای «تنظیم به عنوان صفحه اصلی» ابتدا صفحه را منتشر کنید.",
      );
      return;
    }
    const ok = window.confirm(
      homePageId
        ? "صفحهٔ اصلی فعلی جایگزین شود؟ خانهٔ پیش‌فرض canonical در کد باقی می‌ماند و بعداً قابل بازگردانی است."
        : "این صفحه به‌عنوان صفحه اصلی سفارشی فروشگاه تنظیم شود؟",
    );
    if (!ok) return;
    setBusy(true);
    const result = await setAdminLandingHome(page.pageId);
    setBusy(false);
    if (!result.ok) {
      showError(uiLocale.startsWith("en") ? "Home update failed" : "تنظیم خانه ناموفق بود", result.message);
      return;
    }
    setHomePageId(result.data.homePageId);
    setPage((current) => (current ? { ...current, pageType: "Home" } : current));
    setMeta((current) => ({ ...current, pageType: "Home", slug: "" }));
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

  const isHomeManaged =
    (page?.pageType ?? meta.pageType) === "Home" || (page != null && homePageId === page.pageId);
  const effectivePageType = page?.pageType ?? meta.pageType;
  // صفحهٔ فرود در UI اسلاگ ندارد؛ خانه هم مسیر عمومی خالی دارد.
  const hideSlugField = effectivePageType === "Landing" || isHomeManaged;
  const canSetAsHome = Boolean(page && page.pageType === "Landing");
  const faUi = !uiLocale.trim().toLowerCase().startsWith("en");

  return (
    <main
      data-testid="admin-landing-page-editor"
      data-page-workspace="1"
      data-page-type={page?.pageType ?? meta.pageType}
      data-unified-section-workspace="1"
    >
      <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
        <div className="flex min-w-0 flex-wrap items-start gap-4">
          <div>
            <h1 className="text-xl font-black">{page ? "فضای کار صفحه" : "ایجاد صفحه فروشگاه"}</h1>
            <p className="mt-1 text-sm text-muted">
              {dirty ? "تغییرات ذخیره نشده است." : saved ? "همهٔ تغییرات ذخیره شده‌اند." : "آمادهٔ ویرایش"}
            </p>
          </div>
          <LandingPublishReadinessCard
            readiness={readiness}
            locale={uiLocale}
            onOpenChecklist={() => setChecklistOpen(true)}
          />
        </div>
        <div className="flex flex-wrap gap-2" data-testid="page-workspace-actions">
          {page ? (
            <>
              <button type="button" className="rounded-xl border px-4 py-2 text-sm font-bold" disabled={busy} onClick={() => void publish()}>
                {page.status === "Published" ? "پیش‌نویس" : "انتشار"}
              </button>
              {canSetAsHome ? (
                <button
                  type="button"
                  className="rounded-xl border px-4 py-2 text-sm font-bold"
                  disabled={busy}
                  onClick={() => void setHome()}
                  data-testid="set-as-home"
                >
                  تنظیم به عنوان صفحه اصلی
                </button>
              ) : null}
              <button
                type="button"
                className="rounded-xl border border-red-200 px-4 py-2 text-sm font-bold text-red-700"
                disabled={busy}
                data-testid="delete-page"
                onClick={() => setDeletePageConfirmOpen(true)}
              >
                حذف
              </button>
            </>
          ) : null}
          <button
            type="button"
            className="rounded-xl border px-4 py-2 text-sm font-bold"
            data-testid="back-to-list"
            onClick={() => {
              if (dirty) {
                setLeaveConfirmOpen(true);
                return;
              }
              router.push("/admin/landing-pages");
            }}
          >
            بازگشت به فهرست
          </button>
          <button
            type="button"
            className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
            disabled={busy}
            data-testid="save-page"
            onClick={() => void saveMeta()}
          >
            ذخیره
          </button>
        </div>
      </div>

      <section className="mb-5 rounded-2xl border border-border bg-surface-elevated p-5" data-testid="page-workspace-meta">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap gap-2" role="tablist" aria-label="اطلاعات صفحه و سئو" data-testid="page-meta-tabs">
            <button
              type="button"
              role="tab"
              aria-selected={metaTab === "page"}
              data-testid="tab-page-info"
              className={`inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-bold ${metaTab === "page" ? "bg-slate-900 text-white" : "border bg-white"}`}
              onClick={() => setMetaTab("page")}
            >
              اطلاعات صفحه
              {readiness.pageTabIncomplete ? (
                <AlertTriangle className={`h-4 w-4 ${metaTab === "page" ? "text-amber-300" : "text-amber-600"}`} data-testid="tab-page-warning" aria-label="ناقص" />
              ) : null}
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={metaTab === "seo"}
              data-testid="tab-seo-info"
              className={`inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-bold ${metaTab === "seo" ? "bg-slate-900 text-white" : "border bg-white"}`}
              onClick={() => setMetaTab("seo")}
            >
              اطلاعات سئو
              {readiness.seoTabIncomplete ? (
                <AlertTriangle className={`h-4 w-4 ${metaTab === "seo" ? "text-amber-300" : "text-amber-600"}`} data-testid="tab-seo-warning" aria-label="ناقص" />
              ) : null}
            </button>
          </div>
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

        {metaTab === "page" ? (
          <div role="tabpanel" data-testid="tab-panel-page-info">
            <div className="grid gap-3 md:grid-cols-2">
              <label className="text-sm">
                <span className="mb-1 block font-bold">عنوان</span>
                <input className="w-full rounded-xl border px-3 py-2" value={meta.title} onChange={(e) => setMeta({ ...meta, title: e.target.value })} />
              </label>
              {!hideSlugField ? (
                <label className="text-sm">
                  <span className="mb-1 block font-bold">آدرس صفحه (slug)</span>
                  <input
                    className="w-full rounded-xl border px-3 py-2"
                    dir="ltr"
                    value={meta.slug}
                    onChange={(e) => setMeta({ ...meta, slug: e.target.value })}
                    data-testid="page-slug-input"
                  />
                  <span className="mt-1 block text-xs text-muted" dir="ltr">
                    مسیر عمومی:{" "}
                    {publicPathForStorePage({ pageType: effectivePageType, slug: meta.slug || "slug" }) || "/"}
                  </span>
                </label>
              ) : null}
              {!page ? (
                <>
                  <label className="text-sm" data-testid="create-page-type">
                    <span className="mb-1 block font-bold">نوع صفحه</span>
                    <select
                      className="w-full rounded-xl border px-3 py-2"
                      value={meta.pageType}
                      onChange={(e) => {
                        const nextType = e.target.value === "Home" ? "Home" : "Landing";
                        setMeta({
                          ...meta,
                          pageType: nextType,
                          slug: "",
                        });
                      }}
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
          </div>
        ) : (
          <div role="tabpanel" data-testid="page-seo-panel">
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
                    || `https://store.example${
                      isHomeManaged
                        ? "/"
                        : page?.slug
                          ? `/landing/${page.slug}`
                          : hideSlugField
                            ? "/"
                            : publicPathForStorePage({ pageType: effectivePageType, slug: meta.slug || "slug" }) || "/"
                    }`}
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
          </div>
        )}
      </section>

      <section id="landing-section-composer" className="rounded-2xl border border-border bg-surface-elevated p-5" data-testid="landing-section-composer" data-canonical-order="1">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex flex-wrap items-center gap-3">
            <h2 className="font-black">بخش‌های صفحه</h2>
            <span
              className="inline-flex items-center rounded-xl bg-violet-50 px-3 py-1.5 text-sm font-bold text-violet-800 ring-1 ring-violet-100"
              data-testid="section-count-badge"
            >
              {sections.length.toLocaleString("fa-IR")} بخش
            </span>
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
                          <button
                            type="button"
                            className="inline-flex items-center gap-1 rounded-lg border border-dashed border-violet-300 bg-violet-50 px-3 py-2 text-xs font-bold text-violet-800 min-h-11"
                            data-testid={`section-add-below-${index}`}
                            disabled={busy}
                            title="افزودن بخش زیر این بخش"
                            onClick={(event) => {
                              event.stopPropagation();
                              void openAddSectionWizard(index + 1);
                            }}
                          >
                            <Plus className="h-3.5 w-3.5" />
                            افزودن بخش
                          </button>
                          <button
                            type="button"
                            className="rounded-lg border px-3 py-2 text-xs min-h-11"
                            data-testid="section-delete"
                            onClick={(event) => {
                              event.stopPropagation();
                              setDeleteTargetId(row.pageSectionId);
                            }}
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </button>
                        </div>
                      </div>
                    </div>
                  </li>
                );
              })}
            </ol>
          </div>
        )}
      </section>

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
              showError("ذخیره لازم است", "ابتدا مشخصات صفحه را ذخیره کنید.");
              return;
            }
            setBusy(true);
            if (wizardMode === "edit" && editing) {
              const result = await updateAdminLandingSection(activePage.pageId, editing.pageSectionId, payload.config);
              setBusy(false);
              if (!result.ok) {
                showError("عملیات ناموفق بود", result.message);
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
                showError("عملیات ناموفق بود", result.message);
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
          }}
        />
      ) : null}

      <Dialog
        title={faUi ? "اطلاعات ذخیره نشده است" : "Unsaved changes"}
        open={leaveConfirmOpen}
        onClose={() => setLeaveConfirmOpen(false)}
        showCloseButton={false}
      >
        <div data-testid="landing-unsaved-leave-dialog">
          <p className="text-sm">
            {faUi
              ? "اطلاعات ذخیره نشده است. آیا می‌خواهید این صفحه را ترک کنید؟"
              : "You have unsaved changes. Do you want to leave this page?"}
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <button
              type="button"
              className="rounded-xl border px-4 py-2 text-sm font-bold"
              data-testid="landing-unsaved-stay"
              onClick={() => setLeaveConfirmOpen(false)}
            >
              {faUi ? "ماندن در صفحه" : "Stay"}
            </button>
            <button
              type="button"
              className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
              data-testid="landing-unsaved-leave"
              onClick={() => {
                setLeaveConfirmOpen(false);
                router.push("/admin/landing-pages");
              }}
            >
              {faUi ? "ترک صفحه" : "Leave"}
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog
        title={faUi ? "حذف صفحه" : "Delete page"}
        open={deletePageConfirmOpen}
        onClose={() => setDeletePageConfirmOpen(false)}
        showCloseButton={false}
      >
        <div data-testid="delete-page-confirm">
          <p className="text-sm">
            {faUi
              ? "این صفحه و بخش‌هایش حذف شود؟ این عمل قابل بازگشت نیست."
              : "Delete this page and its sections? This cannot be undone."}
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <button
              type="button"
              className="rounded-xl border px-4 py-2 text-sm font-bold"
              onClick={() => setDeletePageConfirmOpen(false)}
            >
              {faUi ? "انصراف" : "Cancel"}
            </button>
            <button
              type="button"
              className="rounded-xl bg-red-600 px-4 py-2 text-sm font-bold text-white"
              disabled={busy}
              data-testid="delete-page-confirm-yes"
              onClick={() => void removePage()}
            >
              {faUi ? "حذف" : "Delete"}
            </button>
          </div>
        </div>
      </Dialog>

      <Dialog
        title={faUi ? "حذف بخش" : "Delete section"}
        open={deleteTargetId != null}
        onClose={() => setDeleteTargetId(null)}
        showCloseButton={false}
      >
        <div data-testid="delete-section-confirm">
          <p className="text-sm">
            {faUi
              ? "این بخش از صفحه حذف شود؟ این عمل قابل بازگشت نیست."
              : "Delete this section from the page? This cannot be undone."}
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            <button
              type="button"
              className="rounded-xl border px-4 py-2 text-sm font-bold"
              onClick={() => setDeleteTargetId(null)}
            >
              {faUi ? "انصراف" : "Cancel"}
            </button>
            <button
              type="button"
              className="rounded-xl bg-red-600 px-4 py-2 text-sm font-bold text-white"
              disabled={busy}
              onClick={() => {
                if (deleteTargetId) void remove(deleteTargetId);
              }}
            >
              {faUi ? "حذف" : "Delete"}
            </button>
          </div>
        </div>
      </Dialog>

      <LandingChecklistModal
        open={checklistOpen}
        locale={uiLocale}
        readiness={readiness}
        onClose={() => setChecklistOpen(false)}
        onNavigate={(group) => {
          if (group === "page" || group === "seo") setMetaTab(group);
          if (group === "sections") {
            document.getElementById("landing-section-composer")?.scrollIntoView({ behavior: "smooth" });
          }
        }}
      />

      <LandingPublishIssuesModal
        open={issuesModal != null}
        locale={uiLocale}
        title={
          issuesModal?.mode === "save-as-draft"
            ? faUi
              ? "امکان انتشار وجود ندارد"
              : "Cannot publish yet"
            : faUi
              ? "انتشار ممکن نیست"
              : "Publishing blocked"
        }
        description={
          issuesModal?.mode === "save-as-draft"
            ? faUi
              ? "برخی موارد مهم ناقص‌اند. در صورت تأیید، صفحه فقط به‌صورت پیش‌نویس ذخیره می‌شود."
              : "Some required items are incomplete. If you continue, the page will be saved as a draft only."
            : faUi
              ? "برای انتشار باید همهٔ موارد الزامی تکمیل شوند."
              : "All required items must be completed before publishing."
        }
        issues={issuesModal?.issues ?? []}
        onClose={() => setIssuesModal(null)}
        secondaryLabel={faUi ? "بستن" : "Close"}
        primaryLabel={
          issuesModal?.mode === "save-as-draft"
            ? faUi
              ? "ذخیره به‌عنوان پیش‌نویس"
              : "Save as draft"
            : undefined
        }
        onPrimary={
          issuesModal?.mode === "save-as-draft"
            ? () => {
                setIssuesModal(null);
                void saveMeta({ forceDraft: true, skipIncompletePrompt: true });
              }
            : undefined
        }
      />

      <Dialog
        title={errorModal?.title ?? ""}
        open={errorModal != null}
        onClose={() => setErrorModal(null)}
        showCloseButton={false}
      >
        <div data-testid="landing-error-modal">
          <p className="text-sm text-muted">{errorModal?.description}</p>
          <div className="mt-3 flex justify-end">
            <button
              type="button"
              className="rounded-xl border px-4 py-2 text-sm font-bold"
              onClick={() => setErrorModal(null)}
            >
              {faUi ? "بستن" : "Close"}
            </button>
          </div>
        </div>
      </Dialog>
    </main>
  );
}
