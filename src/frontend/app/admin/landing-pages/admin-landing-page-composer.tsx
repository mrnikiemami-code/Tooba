"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { ChevronDown, ChevronUp, Eye, EyeOff, Pencil, Plus, Trash2 } from "lucide-react";
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
} from "./landing-section-catalog.ts";
import {
  defaultConfigForCompositionSection,
} from "./admin-composition-catalog.ts";
import { SIZE_PRESET_CONTRACTS } from "../../../lib/storefront-composition/size-presets.ts";
import { getSectionType, getVariant } from "../../../lib/storefront-composition/registry.ts";
import {
  buildTemplateSectionPayloads,
  listIndustryTemplates,
} from "../../../lib/storefront-composition/industry-templates.ts";
import { AdminSectionWizard } from "./admin-section-wizard.tsx";
import { VariantPreviewCanvas } from "./layout-aware-previews.tsx";
import { AdminTemplateSelectionWorkspace } from "./admin-template-selection-workspace.tsx";

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
  return null;
}

function localeLabel(locale: string): string {
  return locale === "en" ? "انگلیسی" : "فارسی";
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
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);
  const [focusedSectionId, setFocusedSectionId] = useState<string | null>(null);
  const [createMode, setCreateMode] = useState<"blank" | "template" | null>(pageId ? "blank" : null);
  const [selectedTemplateKey, setSelectedTemplateKey] = useState<string | null>(null);
  const [templateConfirmed, setTemplateConfirmed] = useState(false);
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

  const saveMeta = async (options?: { openWizardAfter?: boolean }) => {
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
      seoTitle: meta.seoTitle.trim() || undefined,
      seoDescription: meta.seoDescription.trim() || undefined,
    };
    const result = page
      ? await updateAdminLandingPage(page.pageId, payload)
      : await createAdminLandingPage(payload);
    if (!result.ok) {
      setBusy(false);
      setMessage(result.message);
      return null;
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
            return null;
          }
        }
      } catch (error) {
        setBusy(false);
        setPage(result.data);
        setMeta(toMeta(result.data));
        setMessage(error instanceof Error ? error.message : "اعمال قالب ناموفق بود.");
        router.replace(`/admin/landing-pages/${result.data.pageId}`);
        return null;
      }
    }

    setBusy(false);
    setPage(result.data);
    pageRef.current = result.data;
    setMeta(toMeta(result.data));
    if (createdSections.length) setSections(createdSections);
    setSaved(true);
    setMessage(undefined);
    if (!pageId) router.replace(`/admin/landing-pages/${result.data.pageId}`);
    if (options?.openWizardAfter) {
      setWizardMode("create");
      setEditing(null);
      setWizardOpen(true);
    }
    return result.data;
  };

  const openAddSectionWizard = async () => {
    if (page) {
      setWizardMode("create");
      setEditing(null);
      setWizardOpen(true);
      return;
    }
    await saveMeta({ openWizardAfter: true });
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
    if (editing?.pageSectionId === sectionId) {
      setEditing(null);
      setWizardOpen(false);
    }
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

  if (!pageId && !page && createMode === null) {
    return (
      <main data-testid="admin-landing-page-editor" className="space-y-5">
        <div>
          <Link href="/admin/landing-pages" className="text-sm text-muted">بازگشت به فهرست</Link>
          <h1 className="mt-1 text-xl font-black">ایجاد صفحهٔ فرود / خانه</h1>
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

  if (!pageId && !page && createMode === "template" && !templateConfirmed) {
    return (
      <AdminTemplateSelectionWorkspace
        selectedTemplateKey={selectedTemplateKey}
        onBack={() => setCreateMode(null)}
        onStartBlank={() => {
          setCreateMode("blank");
          setSelectedTemplateKey(null);
          setTemplateConfirmed(false);
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
          if (!selectedTemplateKey) {
            const first = industryTemplates[0];
            if (first) {
              setSelectedTemplateKey(first.templateKey);
              setMeta((current) => ({
                ...current,
                title: current.title || first.nameFa,
                slug: current.slug || "",
              }));
            }
          }
          setTemplateConfirmed(true);
        }}
      />
    );
  }

  return (
    <main data-testid="admin-landing-page-editor" data-page-workspace="1">
      <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
        <div>
          <Link href="/admin/landing-pages" className="text-sm text-muted">بازگشت به فهرست</Link>
          <h1 className="mt-1 text-xl font-black">{page ? "فضای کار صفحه" : "ایجاد صفحهٔ فرود"}</h1>
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

      <section className="mb-5 rounded-2xl border border-border bg-surface-elevated p-5" data-testid="page-workspace-meta">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 className="font-black">مشخصات صفحه</h2>
          <div className="flex flex-wrap items-center gap-2">
            {page ? (
              <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${page.status === "Published" ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}>
                {page.status === "Published" ? "منتشرشده" : "پیش‌نویس"}
              </span>
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
            <span className="mb-1 block font-bold">آدرس صفحه</span>
            <input className="w-full rounded-xl border px-3 py-2" dir="ltr" value={meta.slug} onChange={(e) => setMeta({ ...meta, slug: e.target.value })} />
          </label>
          {!page ? (
            <label className="text-sm" data-testid="page-language-create-field">
              <span className="mb-1 block font-bold">زبان صفحه</span>
              <select className="w-full rounded-xl border px-3 py-2" value={meta.locale} onChange={(e) => setMeta({ ...meta, locale: e.target.value })}>
                <option value="fa">فارسی</option>
                <option value="en">انگلیسی</option>
              </select>
              <span className="mt-1 block text-xs text-muted">زبان فقط هنگام ایجاد انتخاب می‌شود و برای همهٔ بخش‌ها ثابت می‌ماند.</span>
            </label>
          ) : (
            <div className="text-sm" data-testid="page-language-edit-fixed">
              <span className="mb-1 block font-bold">زبان صفحه</span>
              <p className="rounded-xl border bg-slate-50 px-3 py-2 font-bold">{localeLabel(page.locale)}</p>
              <span className="mt-1 block text-xs text-muted">ترجمهٔ صفحه جریان جداگانه‌ای است؛ بخش‌ها زبان مستقل ندارند.</span>
            </div>
          )}
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

      <section className="mb-5 rounded-2xl border border-border bg-surface-elevated p-5" data-testid="page-workspace-preview">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          <h2 className="font-black">پیش‌نمایش ترکیب صفحه</h2>
          <span className="text-xs font-bold text-muted">{sections.length.toLocaleString("fa-IR")} بخش</span>
        </div>
        {sections.length === 0 ? (
          <div className="rounded-xl border border-dashed px-4 py-8 text-center" data-testid="blank-page-empty-state">
            <p className="text-sm font-bold">صفحه هنوز بخشی ندارد.</p>
            <p className="mt-2 text-sm text-muted">پس از ذخیرهٔ پیش‌نویس، اولین بخش را با جادوگر اضافه کنید.</p>
            <button
              type="button"
              className="mt-4 inline-flex items-center gap-2 rounded-xl bg-slate-900 px-4 py-2 text-sm font-bold text-white disabled:opacity-40"
              disabled={busy}
              onClick={() => void openAddSectionWizard()}
              data-testid="add-first-section-cta"
            >
              <Plus className="h-4 w-4" />
              افزودن اولین بخش
            </button>
          </div>
        ) : (
          <div className="space-y-2" data-testid="composition-visual-preview" data-workspace-composition="1">
            {sections.map((row) => {
              const config = parseLandingConfig(row.config);
              const variantKey = typeof config.variantKey === "string" ? config.variantKey : undefined;
              const sectionTypeKey = resolveSectionTypeKey(row.sectionType, config);
              const typeLabel = sectionTypeKey ? (getSectionType(sectionTypeKey)?.nameFa ?? landingSectionLabel(row.sectionType)) : landingSectionLabel(row.sectionType);
              const focused = focusedSectionId === row.pageSectionId;
              return (
                <button
                  key={row.pageSectionId}
                  type="button"
                  onClick={() => setFocusedSectionId(row.pageSectionId)}
                  className={`w-full rounded-xl border px-3 py-2 text-start ${
                    focused ? "border-[#2563EB] ring-2 ring-[#2563EB]/20" : ""
                  } ${row.isEnabled ? "bg-white" : "bg-slate-50 opacity-60"}`}
                  data-testid={`workspace-mini-${row.pageSectionId}`}
                  data-section-focused={focused ? "true" : undefined}
                >
                  <div className="flex items-center gap-3">
                    <div className="w-28 shrink-0">
                      {variantKey ? <VariantPreviewCanvas variantKey={variantKey} testId={`workspace-preview-${row.pageSectionId}`} /> : (
                        <div className="mb-0 h-10 rounded-lg bg-slate-100" />
                      )}
                    </div>
                    <div className="min-w-0">
                      <p className="text-sm font-bold">{typeLabel}</p>
                      <p className="truncate text-xs text-muted">{summarizeLandingSection(row.sectionType, config)}</p>
                    </div>
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </section>

      <section className="rounded-2xl border border-border bg-surface-elevated p-5" data-testid="landing-section-composer">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 className="font-black">بخش‌های صفحه</h2>
          <button
            type="button"
            className="inline-flex items-center gap-2 rounded-xl bg-slate-900 px-4 py-2 text-sm font-bold text-white disabled:opacity-40"
            disabled={busy}
            onClick={() => void openAddSectionWizard()}
            data-testid="landing-add-section"
          >
            <Plus className="h-4 w-4" />
            افزودن بخش
          </button>
        </div>
        {sections.length === 0 ? (
          <p className="rounded-xl border border-dashed px-4 py-8 text-center text-sm text-muted">هنوز بخشی اضافه نشده است. «افزودن بخش» پیش‌نویس را در صورت نیاز می‌سازد و جادوگر را باز می‌کند.</p>
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
              const focused = focusedSectionId === row.pageSectionId;
              return (
                <li
                  key={row.pageSectionId}
                  id={`composer-section-${row.pageSectionId}`}
                  className={`rounded-xl border px-4 py-3 ${focused ? "border-[#2563EB] ring-2 ring-[#2563EB]/15" : ""} ${row.isEnabled ? "bg-white" : "bg-slate-50 opacity-80"}`}
                  data-testid="composer-section-card"
                >
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
                          setWizardMode("edit");
                          setWizardOpen(true);
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

      {wizardOpen ? (
        <AdminSectionWizard
          key={editing?.pageSectionId ?? "create"}
          open={wizardOpen}
          mode={wizardMode}
          initialHostType={editing?.sectionType}
          initialConfig={editing ? parseLandingConfig(editing.config) : undefined}
          busy={busy}
          onClose={() => {
            setWizardOpen(false);
            setEditing(null);
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
              const result = await addAdminLandingSection(activePage.pageId, payload.hostType, config);
              setBusy(false);
              if (!result.ok) {
                setMessage(result.message);
                return;
              }
              setSections((rows) => [...rows, result.data]);
              setFocusedSectionId(result.data.pageSectionId);
            }
            setWizardOpen(false);
            setEditing(null);
            setMessage(undefined);
          }}
        />
      ) : null}
    </main>
  );
}
