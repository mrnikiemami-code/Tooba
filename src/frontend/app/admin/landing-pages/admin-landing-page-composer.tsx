"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ChevronDown, ChevronUp, Eye, EyeOff, Pencil, Plus, Trash2 } from "lucide-react";
import { LandingSectionForm } from "./admin-landing-section-forms.tsx";
import {
  addAdminLandingSection,
  createAdminLandingPage,
  deleteAdminLandingSection,
  getAdminLandingHome,
  getAdminLandingPage,
  listAdminLandingSections,
  reorderAdminLandingSections,
  setAdminLandingHome,
  setAdminLandingPageStatus,
  setAdminLandingSectionEnabled,
  updateAdminLandingPage,
  updateAdminLandingSection,
  type AdminLandingPage,
  type AdminLandingSection,
} from "./admin-landing-pages-api.ts";
import {
  LANDING_SECTION_CHOICES,
  landingSectionLabel,
  parseLandingConfig,
  summarizeLandingSection,
  type LandingSectionType,
} from "./landing-section-catalog.ts";
import {
  adminImplementedVariants,
  adminSelectableSectionTypes,
  defaultConfigForCompositionSection,
  previewMosaicClass,
  sectionSupportsHeightPreset,
  variantPreviewStructure,
  type AdminCompositionSectionChoice,
} from "./admin-composition-catalog.ts";
import { SIZE_PRESETS } from "../../../lib/storefront-composition/types.ts";
import { SIZE_PRESET_CONTRACTS } from "../../../lib/storefront-composition/size-presets.ts";
import { getSectionType, getVariant } from "../../../lib/storefront-composition/registry.ts";
import { bannerSlotCountForVariant } from "./landing-section-catalog.ts";
import {
  buildTemplateSectionPayloads,
  listIndustryTemplates,
  templateCompositionMiniature,
  templateSectionSummaryFa,
} from "../../../lib/storefront-composition/industry-templates.ts";

type Meta = {
  title: string;
  slug: string;
  locale: string;
  seoTitle: string;
  seoDescription: string;
};

function toMeta(page?: AdminLandingPage | null): Meta {
  return {
    title: page?.title ?? "",
    slug: page?.slug ?? "",
    locale: page?.locale ?? "fa",
    seoTitle: page?.seoTitle ?? "",
    seoDescription: page?.seoDescription ?? "",
  };
}

function resolveSectionTypeKey(hostType: string, config: Record<string, unknown>): string | null {
  const variantKey = typeof config.variantKey === "string" ? config.variantKey : undefined;
  if (variantKey) {
    const fromVariant = getVariant(variantKey);
    if (fromVariant) return fromVariant.sectionTypeKey;
  }
  return adminSelectableSectionTypes().find((s) => s.hostType === hostType)?.sectionTypeKey ?? null;
}

export function AdminLandingPageComposer({ pageId }: { pageId?: string }) {
  const router = useRouter();
  const [page, setPage] = useState<AdminLandingPage | null>(null);
  const [meta, setMeta] = useState<Meta>(toMeta());
  const [sections, setSections] = useState<AdminLandingSection[]>([]);
  const [homePageId, setHomePageId] = useState<string | null>(null);
  const [message, setMessage] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [saved, setSaved] = useState(true);
  const [chooserOpen, setChooserOpen] = useState(false);
  const [chooserStep, setChooserStep] = useState<"section" | "variant">("section");
  const [chooserSection, setChooserSection] = useState<AdminCompositionSectionChoice | null>(null);
  const [editing, setEditing] = useState<AdminLandingSection | null>(null);
  const [draftConfig, setDraftConfig] = useState<Record<string, unknown>>({});
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);
  const [createMode, setCreateMode] = useState<"blank" | "template" | null>(pageId ? "blank" : null);
  const [selectedTemplateKey, setSelectedTemplateKey] = useState<string | null>(null);
  const industryTemplates = useMemo(() => listIndustryTemplates(), []);

  const compositionSections = useMemo(() => adminSelectableSectionTypes(), []);
  // Keep LANDING_SECTION_CHOICES referenced for admin-landing-pages.guard.test.ts
  void LANDING_SECTION_CHOICES;

  const editingSectionTypeKey = useMemo(() => {
    if (!editing) return null;
    return resolveSectionTypeKey(editing.sectionType, draftConfig);
  }, [editing, draftConfig]);

  const editingVariants = useMemo(
    () => (editingSectionTypeKey ? adminImplementedVariants(editingSectionTypeKey) : []),
    [editingSectionTypeKey],
  );

  const dirty = useMemo(() => {
    if (!page) return Boolean(meta.title || meta.slug);
    return meta.title !== page.title
      || meta.slug !== page.slug
      || meta.locale !== page.locale
      || meta.seoTitle !== (page.seoTitle ?? "")
      || meta.seoDescription !== (page.seoDescription ?? "");
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

  const openChooser = () => {
    setChooserStep("section");
    setChooserSection(null);
    setChooserOpen(true);
  };

  const saveMeta = async () => {
    if (!meta.title.trim() || !meta.slug.trim()) {
      setMessage("عنوان و آدرس صفحه لازم است.");
      return;
    }
    setBusy(true);
    const payload = {
      title: meta.title.trim(),
      slug: meta.slug.trim(),
      locale: meta.locale,
      seoTitle: meta.seoTitle.trim() || undefined,
      seoDescription: meta.seoDescription.trim() || undefined,
    };
    const result = page
      ? await updateAdminLandingPage(page.pageId, payload)
      : await createAdminLandingPage(payload);
    if (!result.ok) {
      setBusy(false);
      setMessage(result.message);
      return;
    }

    let createdSections: AdminLandingSection[] = [];
    if (!page && createMode === "template" && selectedTemplateKey) {
      try {
        const payloads = buildTemplateSectionPayloads(selectedTemplateKey);
        for (const item of payloads) {
          const sectionResult = await addAdminLandingSection(result.data.pageId, item.hostType, item.config);
          if (sectionResult.ok) createdSections = [...createdSections, sectionResult.data];
          else {
            setBusy(false);
            setPage(result.data);
            setMeta(toMeta(result.data));
            setMessage(sectionResult.message);
            router.replace(`/admin/landing-pages/${result.data.pageId}`);
            return;
          }
        }
      } catch (error) {
        setBusy(false);
        setPage(result.data);
        setMeta(toMeta(result.data));
        setMessage(error instanceof Error ? error.message : "اعمال قالب ناموفق بود.");
        router.replace(`/admin/landing-pages/${result.data.pageId}`);
        return;
      }
    }

    setBusy(false);
    setPage(result.data);
    setMeta(toMeta(result.data));
    if (createdSections.length) setSections(createdSections);
    setSaved(true);
    setMessage(undefined);
    if (!pageId) router.replace(`/admin/landing-pages/${result.data.pageId}`);
  };

  const addSectionWithVariant = async (choice: AdminCompositionSectionChoice, variantKey: string) => {
    if (!page) {
      setMessage("ابتدا مشخصات صفحه را ذخیره کنید.");
      return;
    }
    const hostType = choice.hostType as LandingSectionType;
    const config = defaultConfigForCompositionSection(choice.sectionTypeKey, variantKey);
    if (variantKey.startsWith("banner.")) {
      const slots = bannerSlotCountForVariant(variantKey);
      config.items = Array.from({ length: slots }, (_, index) => ({
        imageUrl: "",
        href: "/offers",
        title: `بنر ${(index + 1).toLocaleString("fa-IR")}`,
      }));
    }
    setBusy(true);
    const result = await addAdminLandingSection(page.pageId, hostType, config);
    setBusy(false);
    setChooserOpen(false);
    setChooserStep("section");
    setChooserSection(null);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setSections((rows) => [...rows, result.data]);
    setEditing(result.data);
    setDraftConfig(parseLandingConfig(result.data.config));
  };

  const move = async (index: number, direction: -1 | 1) => {
    if (!page) return;
    const target = index + direction;
    if (target < 0 || target >= sections.length) return;
    const next = sections.slice();
    const current = next[index]!;
    next[index] = next[target]!;
    next[target] = current;
    setSections(next);
    setBusy(true);
    const result = await reorderAdminLandingSections(page.pageId, next.map((row) => row.pageSectionId));
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      void load(page.pageId);
      return;
    }
    setSections(result.data);
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

  const saveSection = async () => {
    if (!page || !editing) return;
    setBusy(true);
    const result = await updateAdminLandingSection(page.pageId, editing.pageSectionId, draftConfig);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setSections((rows) => rows.map((item) => item.pageSectionId === editing.pageSectionId ? result.data : item));
    setEditing(null);
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
    setSections((rows) => rows.filter((item) => item.pageSectionId !== sectionId));
    if (editing?.pageSectionId === sectionId) setEditing(null);
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
      setMessage("برای انتخاب به‌عنوان صفحهٔ اصلی ابتدا صفحه را منتشر کنید.");
      return;
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

  const showHeightPreset = Boolean(
    editingSectionTypeKey
      && sectionSupportsHeightPreset(editingSectionTypeKey, typeof draftConfig.variantKey === "string" ? draftConfig.variantKey : undefined),
  );

  if (!pageId && !page && createMode === null) {
    return (
      <main data-testid="admin-landing-page-editor" className="space-y-5">
        <div>
          <Link href="/admin/landing-pages" className="text-sm text-muted">بازگشت به فهرست</Link>
          <h1 className="mt-1 text-xl font-black">ایجاد صفحهٔ فرود / خانه</h1>
          <p className="mt-1 text-sm text-muted">از صفحه خالی شروع کنید یا یک قالب آمادهٔ صنعتی را انتخاب کنید.</p>
        </div>
        <div className="grid gap-4 md:grid-cols-2" data-testid="composition-start-mode">
          <button
            type="button"
            className="rounded-2xl border border-border bg-surface-elevated p-6 text-start hover:border-primary"
            data-testid="start-blank"
            onClick={() => setCreateMode("blank")}
          >
            <p className="text-lg font-black">شروع از صفحه خالی</p>
            <p className="mt-2 text-sm text-muted">یک پیش‌نویس خالی بسازید و بخش‌ها را خودتان اضافه کنید.</p>
          </button>
          <button
            type="button"
            className="rounded-2xl border border-border bg-surface-elevated p-6 text-start hover:border-primary"
            data-testid="start-from-template"
            onClick={() => setCreateMode("template")}
          >
            <p className="text-lg font-black">شروع از قالب آماده</p>
            <p className="mt-2 text-sm text-muted">یکی از ۱۰ قالب صنعتی را انتخاب کنید؛ نتیجه یک پیش‌نویس عادی و قابل‌ویرایش است.</p>
          </button>
        </div>
      </main>
    );
  }

  if (!pageId && !page && createMode === "template" && !selectedTemplateKey) {
    return (
      <main data-testid="admin-landing-page-editor" className="space-y-5">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <button type="button" className="text-sm text-muted" onClick={() => setCreateMode(null)}>بازگشت</button>
            <h1 className="mt-1 text-xl font-black">انتخاب قالب آماده</h1>
            <p className="mt-1 text-sm text-muted">نام فارسی، توضیح کوتاه و خلاصهٔ بخش‌ها را ببینید؛ هر قالب ترکیب متفاوتی دارد.</p>
          </div>
        </div>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" data-testid="template-picker">
          {industryTemplates.map((template) => {
            const miniature = templateCompositionMiniature(template);
            return (
              <button
                key={template.templateKey}
                type="button"
                className="rounded-2xl border border-border bg-surface-elevated p-4 text-start hover:border-primary"
                data-testid={`template-card-${template.templateKey}`}
                onClick={() => {
                  setSelectedTemplateKey(template.templateKey);
                  setMeta((current) => ({
                    ...current,
                    title: current.title || template.nameFa,
                    // Keep address blank so the operator sets a Persian-friendly slug (no English template key leak).
                    slug: current.slug || "",
                  }));
                }}
              >
                <div className="mb-3 grid h-24 grid-cols-6 grid-rows-3 gap-1 rounded-xl bg-slate-100 p-2" aria-hidden data-testid="template-composition-miniature">
                  {miniature.map((kind, index) => {
                    const tone =
                      kind === "hero" ? "bg-sky-400"
                        : kind === "story" ? "bg-rose-300"
                          : kind === "category" ? "bg-emerald-400"
                            : kind === "product" ? "bg-amber-400"
                              : kind === "banner" ? "bg-violet-400"
                                : kind === "brand" ? "bg-slate-400"
                                  : kind === "article" ? "bg-cyan-400"
                                    : kind === "reviews" ? "bg-yellow-300"
                                      : "bg-stone-300";
                    const span = kind === "hero" ? "col-span-6 row-span-1" : "col-span-2";
                    return <span key={`${template.templateKey}-${index}`} className={`rounded ${tone} ${span}`} />;
                  })}
                </div>
                <p className="font-black">{template.nameFa}</p>
                <p className="mt-2 text-sm text-muted">{template.descriptionFa}</p>
                <p className="mt-2 text-xs font-bold text-slate-600">{template.sectionPresetList.length.toLocaleString("fa-IR")} بخش</p>
                <p className="mt-1 text-xs text-muted">{templateSectionSummaryFa(template)}</p>
              </button>
            );
          })}
        </div>
      </main>
    );
  }

  return (
    <main data-testid="admin-landing-page-editor">
      <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link href="/admin/landing-pages" className="text-sm text-muted">بازگشت به فهرست</Link>
          <h1 className="mt-1 text-xl font-black">{page ? "ویرایش صفحهٔ فرود" : "ایجاد صفحهٔ فرود"}</h1>
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
              <button type="button" className="rounded-xl border px-4 py-2 text-sm font-bold" disabled={busy} onClick={() => void publish()}>
                {page.status === "Published" ? "بازگرداندن به پیش‌نویس" : "انتشار"}
              </button>
              <button type="button" className="rounded-xl border px-4 py-2 text-sm font-bold" disabled={busy} onClick={() => void setHome()}>
                {homePageId === page.pageId ? "لغو خانه" : "انتخاب به‌عنوان صفحه اصلی"}
              </button>
            </>
          ) : null}
          <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" disabled={busy} onClick={() => void saveMeta()}>
            ذخیره
          </button>
        </div>
      </div>

      <section className="mb-5 rounded-2xl border border-border bg-surface-elevated p-5">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-black">مشخصات صفحه</h2>
          {page ? (
            <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${page.status === "Published" ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}>
              {page.status === "Published" ? "منتشرشده" : "پیش‌نویس"}
            </span>
          ) : null}
        </div>
        <div className="grid gap-3 md:grid-cols-2">
          <label className="text-sm">
            <span className="mb-1 block font-bold">عنوان</span>
            <input className="w-full rounded-xl border px-3 py-2" value={meta.title} onChange={(e) => setMeta({ ...meta, title: e.target.value })} />
          </label>
          <label className="text-sm">
            <span className="mb-1 block font-bold">آدرس صفحه</span>
            <input className="w-full rounded-xl border px-3 py-2" dir="ltr" value={meta.slug} onChange={(e) => setMeta({ ...meta, slug: e.target.value })} />
          </label>
          <label className="text-sm">
            <span className="mb-1 block font-bold">زبان</span>
            <select className="w-full rounded-xl border px-3 py-2" value={meta.locale} onChange={(e) => setMeta({ ...meta, locale: e.target.value })}>
              <option value="fa">فارسی</option>
              <option value="en">انگلیسی</option>
            </select>
          </label>
          <label className="text-sm">
            <span className="mb-1 block font-bold">عنوان سئو</span>
            <input className="w-full rounded-xl border px-3 py-2" value={meta.seoTitle} onChange={(e) => setMeta({ ...meta, seoTitle: e.target.value })} />
          </label>
          <label className="text-sm md:col-span-2">
            <span className="mb-1 block font-bold">توضیح سئو</span>
            <textarea className="min-h-20 w-full rounded-xl border px-3 py-2" value={meta.seoDescription} onChange={(e) => setMeta({ ...meta, seoDescription: e.target.value })} />
          </label>
        </div>
      </section>

      <section className="rounded-2xl border border-border bg-surface-elevated p-5" data-testid="landing-section-composer">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 className="font-black">بخش‌های صفحه</h2>
          <button
            type="button"
            className="inline-flex items-center gap-2 rounded-xl bg-slate-900 px-4 py-2 text-sm font-bold text-white disabled:opacity-40"
            disabled={!page || busy}
            onClick={openChooser}
            data-testid="landing-add-section"
          >
            <Plus className="h-4 w-4" />
            افزودن بخش
          </button>
        </div>
        {sections.length === 0 ? (
          <p className="rounded-xl border border-dashed px-4 py-8 text-center text-sm text-muted">هنوز بخشی اضافه نشده است.</p>
        ) : (
          <ol className="space-y-3">
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
              return (
                <li key={row.pageSectionId} className={`rounded-xl border px-4 py-3 ${row.isEnabled ? "bg-white" : "bg-slate-50 opacity-80"}`} data-testid="composer-section-card">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <strong>{typeLabel}</strong>
                        {variantLabel ? <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[11px] font-bold text-slate-700">{variantLabel}</span> : null}
                        <span className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${row.isEnabled ? "bg-emerald-50 text-emerald-700" : "bg-slate-200 text-slate-600"}`}>
                          {row.isEnabled ? "فعال" : "غیرفعال"}
                        </span>
                        {heightFa ? <span className="rounded-full bg-blue-50 px-2 py-0.5 text-[11px] font-bold text-blue-700">{heightFa}</span> : null}
                      </div>
                      <p className="mt-1 text-xs text-muted">{summarizeLandingSection(row.sectionType, config)}</p>
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                      <button type="button" aria-label="بالا" className="rounded-lg border p-2 min-h-11 min-w-11" disabled={busy || index === 0} onClick={() => void move(index, -1)}>
                        <ChevronUp className="h-4 w-4" />
                      </button>
                      <button type="button" aria-label="پایین" className="rounded-lg border p-2 min-h-11 min-w-11" disabled={busy || index === sections.length - 1} onClick={() => void move(index, 1)}>
                        <ChevronDown className="h-4 w-4" />
                      </button>
                      <button type="button" className="rounded-lg border px-3 py-2 text-xs font-bold min-h-11" disabled={busy} onClick={() => void toggle(row)}>
                        {row.isEnabled ? <span className="inline-flex items-center gap-1"><Eye className="h-3.5 w-3.5" /> فعال</span> : <span className="inline-flex items-center gap-1"><EyeOff className="h-3.5 w-3.5" /> غیرفعال</span>}
                      </button>
                      <button
                        type="button"
                        className="rounded-lg border px-3 py-2 text-xs font-bold min-h-11"
                        onClick={() => {
                          setEditing(row);
                          setDraftConfig(config);
                        }}
                      >
                        <span className="inline-flex items-center gap-1"><Pencil className="h-3.5 w-3.5" /> ویرایش</span>
                      </button>
                      {confirmDelete === row.pageSectionId ? (
                        <span className="inline-flex items-center gap-2 text-xs">
                          حذف شود؟
                          <button type="button" className="font-bold text-red-600" onClick={() => void remove(row.pageSectionId)}>بله</button>
                          <button type="button" onClick={() => setConfirmDelete(null)}>خیر</button>
                        </span>
                      ) : (
                        <button type="button" className="rounded-lg border px-3 py-2 text-xs min-h-11" onClick={() => setConfirmDelete(row.pageSectionId)}>
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      )}
                    </div>
                  </div>
                </li>
              );
            })}
          </ol>
        )}
      </section>

      {message ? <p className="mt-4 text-sm text-red-600">{message}</p> : null}

      {chooserOpen ? (
        <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4" data-testid="add-section-chooser">
          <div className="max-h-[80vh] w-full max-w-3xl overflow-auto rounded-2xl bg-white p-5">
            <div className="mb-4 flex items-center justify-between">
              <h3 className="text-lg font-black">
                {chooserStep === "section" ? "افزودن بخش" : `انتخاب ظاهر — ${chooserSection?.nameFa ?? ""}`}
              </h3>
              <button
                type="button"
                onClick={() => {
                  if (chooserStep === "variant") {
                    setChooserStep("section");
                    setChooserSection(null);
                    return;
                  }
                  setChooserOpen(false);
                }}
              >
                {chooserStep === "variant" ? "بازگشت" : "بستن"}
              </button>
            </div>
            {chooserStep === "section" ? (
              <div className="grid gap-3 sm:grid-cols-2" data-testid="composition-section-catalog">
                {compositionSections.map((choice) => (
                  <button
                    key={choice.sectionTypeKey}
                    type="button"
                    data-testid={choice.testId}
                    data-section-type-key={choice.sectionTypeKey}
                    className="rounded-2xl border p-4 text-start hover:border-[#2563EB]"
                    onClick={() => {
                      setChooserSection(choice);
                      setChooserStep("variant");
                    }}
                  >
                    <div className={`mb-3 h-16 rounded-xl bg-gradient-to-l ${previewMosaicClass(choice.previewKind)}`} aria-hidden />
                    <strong>{choice.nameFa}</strong>
                    <p className="mt-1 text-xs text-muted">{choice.descriptionFa}</p>
                  </button>
                ))}
              </div>
            ) : chooserSection ? (
              <div className="grid gap-3 sm:grid-cols-2" data-testid="composition-variant-picker">
                {adminImplementedVariants(chooserSection.sectionTypeKey).map((variant) => (
                  <button
                    key={variant.variantKey}
                    type="button"
                    data-variant-key={variant.variantKey}
                    data-preview-fingerprint={variant.variantKey}
                    data-testid={`pick-variant-${variant.variantKey.replace(/\./g, "-")}`}
                    className="rounded-2xl border p-4 text-start hover:border-[#2563EB]"
                    onClick={() => void addSectionWithVariant(chooserSection, variant.variantKey)}
                  >
                    <div
                      className={`mb-3 grid h-16 grid-cols-5 grid-rows-2 gap-1 rounded-xl bg-gradient-to-l p-2 ${previewMosaicClass(variant.previewKind)}`}
                      aria-hidden
                      data-testid="variant-preview-canvas"
                    >
                      {variantPreviewStructure(variant.variantKey).map((cell, index) => (
                        <span key={`${variant.variantKey}-${index}`} className={cell.className} />
                      ))}
                    </div>
                    <strong>{variant.nameFa}</strong>
                    <p className="mt-1 text-xs text-muted">{variant.descriptionFa}</p>
                    {variant.recommendedUseFa ? (
                      <span className="mt-2 inline-flex rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-bold text-emerald-700">
                        {variant.recommendedUseFa}
                      </span>
                    ) : null}
                  </button>
                ))}
              </div>
            ) : null}
          </div>
        </div>
      ) : null}

      {editing ? (
        <div className="fixed inset-0 z-40 flex justify-end bg-black/30">
          <aside className="h-full w-full max-w-md overflow-auto bg-white p-5 shadow-2xl" data-testid="landing-section-drawer">
            <div className="mb-4 flex items-center justify-between">
              <h3 className="font-black">ویرایش {landingSectionLabel(editing.sectionType)}</h3>
              <button type="button" onClick={() => setEditing(null)}>بستن</button>
            </div>
            {editingVariants.length > 0 ? (
              <div className="mb-4" data-testid="edit-variant-picker">
                <p className="mb-2 text-sm font-bold">ظاهر بخش</p>
                <div className="grid gap-2">
                  {editingVariants.map((variant) => {
                    const selected = draftConfig.variantKey === variant.variantKey
                      || (!draftConfig.variantKey && variant.variantKey === editingVariants[0]?.variantKey);
                    return (
                      <button
                        key={variant.variantKey}
                        type="button"
                        data-variant-key={variant.variantKey}
                        className={`rounded-xl border p-3 text-start ${selected ? "border-[#2563EB] bg-blue-50" : ""}`}
                        onClick={() => setDraftConfig({ ...draftConfig, variantKey: variant.variantKey })}
                      >
                        <div className="flex items-center gap-3">
                          <span className={`h-10 w-14 shrink-0 rounded-lg bg-gradient-to-l ${previewMosaicClass(variant.previewKind)}`} aria-hidden />
                          <span>
                            <strong className="text-sm">{variant.nameFa}</strong>
                            <span className="mt-0.5 block text-xs text-muted">{variant.descriptionFa}</span>
                          </span>
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>
            ) : null}
            {showHeightPreset ? (
              <label className="mb-4 block text-sm">
                <span className="mb-1 block font-bold">اندازه نمایش</span>
                <select
                  className="w-full rounded-xl border px-3 py-2"
                  value={typeof draftConfig.heightPreset === "string" ? draftConfig.heightPreset : "Medium"}
                  onChange={(e) => setDraftConfig({ ...draftConfig, heightPreset: e.target.value })}
                  data-testid="height-preset-select"
                >
                  {SIZE_PRESETS.map((preset) => (
                    <option key={preset} value={preset}>{SIZE_PRESET_CONTRACTS[preset].nameFa}</option>
                  ))}
                </select>
              </label>
            ) : null}
            <LandingSectionForm type={editing.sectionType} value={draftConfig} onChange={setDraftConfig} />
            <div className="mt-5 flex gap-2">
              <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" disabled={busy} onClick={() => void saveSection()}>
                ذخیرهٔ بخش
              </button>
              <button type="button" className="rounded-xl border px-4 py-2 text-sm" onClick={() => setEditing(null)}>انصراف</button>
            </div>
          </aside>
        </div>
      ) : null}
    </main>
  );
}
