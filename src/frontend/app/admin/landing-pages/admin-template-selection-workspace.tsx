"use client";

import { useMemo, useState } from "react";
import { Monitor, Smartphone, Tablet } from "lucide-react";
import {
  listIndustryTemplates,
  templateSectionSummaryFa,
  type IndustryTemplateSeed,
} from "../../../lib/storefront-composition/industry-templates.ts";
import { layoutAwareTemplatePreview } from "./layout-aware-previews.tsx";

export type TemplatePreviewDevice = "desktop" | "tablet" | "mobile";

const DEVICE_FRAME: Record<TemplatePreviewDevice, { widthClass: string; heightClass: string; label: string }> = {
  desktop: { widthClass: "w-full max-w-[920px]", heightClass: "min-h-[420px]", label: "دسکتاپ" },
  tablet: { widthClass: "w-full max-w-[640px]", heightClass: "min-h-[480px]", label: "تبلت" },
  mobile: { widthClass: "w-full max-w-[360px]", heightClass: "min-h-[560px]", label: "موبایل" },
};

/** Wireframe block recipes — structurally distinct per industry (not identical skeletons). */
function industryWireframeBlocks(template: IndustryTemplateSeed, device: TemplatePreviewDevice): Array<{ kind: string; className: string }> {
  const key = template.templateKey;
  const dense = device === "mobile";
  const mid = device === "tablet";

  if (key === "fashion" || key === "shoes" || key === "beauty") {
    return [
      { kind: "hero", className: `col-span-12 ${dense ? "h-24" : mid ? "h-28" : "h-32"} rounded-xl bg-rose-200/70 border border-rose-300/50` },
      ...Array.from({ length: dense ? 4 : 6 }, (_, i) => ({
        kind: `story-${i}`,
        className: "col-span-2 aspect-square rounded-full bg-white border-2 border-rose-300/80",
      })),
      { kind: "category", className: `col-span-12 ${dense ? "h-14" : "h-16"} rounded-lg bg-white border border-amber-200/70` },
      { kind: "product-a", className: `col-span-4 ${dense ? "h-16" : "h-20"} rounded-lg bg-white border border-amber-200` },
      { kind: "product-b", className: `col-span-4 ${dense ? "h-16" : "h-20"} rounded-lg bg-white border border-amber-200` },
      { kind: "product-c", className: `col-span-4 ${dense ? "h-16" : "h-20"} rounded-lg bg-white border border-amber-200` },
      { kind: "banner-a", className: `col-span-6 ${dense ? "h-12" : "h-14"} rounded-lg bg-violet-200/60 border border-violet-300/50` },
      { kind: "banner-b", className: `col-span-6 ${dense ? "h-12" : "h-14"} rounded-lg bg-violet-200/45 border border-violet-300/40` },
      ...(key === "beauty"
        ? [{ kind: "brand", className: "col-span-12 h-12 rounded-full bg-white border border-fuchsia-200" }]
        : [{ kind: "brand", className: "col-span-12 flex gap-2 h-10" }]),
    ];
  }

  if (key === "auto-parts" || key === "tools-hardware" || key === "building-supplies") {
    return [
      { kind: "search-bar", className: `col-span-12 ${dense ? "h-9" : "h-10"} rounded-md bg-slate-200 border border-slate-300` },
      { kind: "cat-a", className: `col-span-3 ${dense ? "h-12" : "h-16"} rounded-md bg-white border border-slate-300` },
      { kind: "cat-b", className: `col-span-3 ${dense ? "h-12" : "h-16"} rounded-md bg-white border border-slate-300` },
      { kind: "cat-c", className: `col-span-3 ${dense ? "h-12" : "h-16"} rounded-md bg-white border border-slate-300` },
      { kind: "cat-d", className: `col-span-3 ${dense ? "h-12" : "h-16"} rounded-md bg-white border border-slate-300` },
      { kind: "brand-grid", className: `col-span-12 ${dense ? "h-12" : "h-14"} rounded-md bg-white border border-slate-300` },
      { kind: "product-row-a", className: `col-span-12 ${dense ? "h-8" : "h-9"} rounded-md border border-slate-200 bg-slate-50` },
      { kind: "product-row-b", className: `col-span-12 ${dense ? "h-8" : "h-9"} rounded-md border border-slate-200 bg-slate-50` },
      { kind: "product-row-c", className: `col-span-12 ${dense ? "h-8" : "h-9"} rounded-md border border-slate-200 bg-slate-50` },
      { kind: "ranked", className: `col-span-12 ${dense ? "h-10" : "h-12"} rounded-md bg-orange-100 border border-orange-200` },
      { kind: "banner", className: `col-span-12 ${dense ? "h-10" : "h-12"} rounded-md bg-violet-200/50` },
    ];
  }

  if (key === "tile-ceramic" || key === "interior-decor") {
    return [
      { kind: "hero-large", className: `col-span-12 ${dense ? "h-28" : mid ? "h-36" : "h-40"} rounded-2xl bg-amber-100/90 border border-amber-200/70` },
      { kind: "editorial", className: `${dense ? "col-span-12" : "col-span-6"} ${dense ? "h-16" : "h-24"} rounded-xl bg-white border border-stone-200` },
      { kind: "editorial-b", className: `${dense ? "col-span-12" : "col-span-6"} ${dense ? "h-16" : "h-24"} rounded-xl bg-stone-50 border border-stone-200` },
      { kind: "collection", className: `col-span-12 ${dense ? "h-20" : "h-28"} rounded-xl bg-teal-50 border border-teal-200/60` },
      { kind: "calm-product", className: `col-span-12 ${dense ? "h-14" : "h-16"} rounded-lg bg-white border border-amber-100` },
    ];
  }

  if (key === "home-appliance") {
    return [
      { kind: "hero", className: `col-span-12 ${dense ? "h-20" : "h-24"} rounded-xl bg-sky-100 border border-sky-200` },
      { kind: "brand-feature", className: `col-span-12 ${dense ? "h-16" : "h-20"} rounded-xl bg-white border-2 border-slate-300` },
      { kind: "tabbed", className: `col-span-12 ${dense ? "h-24" : "h-28"} rounded-lg bg-slate-50 border border-slate-200` },
      { kind: "promo", className: `col-span-6 ${dense ? "h-12" : "h-14"} rounded-lg bg-violet-200/50` },
      { kind: "promo-b", className: `col-span-6 ${dense ? "h-12" : "h-14"} rounded-lg bg-violet-200/40` },
    ];
  }

  if (key === "plants") {
    return [
      { kind: "calm-hero", className: `col-span-12 ${dense ? "h-24" : "h-28"} rounded-2xl bg-lime-100/80 border border-lime-200` },
      { kind: "category", className: `col-span-12 ${dense ? "h-12" : "h-14"} rounded-full bg-white border border-emerald-200` },
      { kind: "product", className: `col-span-4 ${dense ? "h-16" : "h-20"} rounded-2xl bg-white border border-lime-200` },
      { kind: "product-b", className: `col-span-4 ${dense ? "h-16" : "h-20"} rounded-2xl bg-white border border-lime-200` },
      { kind: "product-c", className: `col-span-4 ${dense ? "h-16" : "h-20"} rounded-2xl bg-white border border-lime-200` },
    ];
  }

  const preview = layoutAwareTemplatePreview(template);
  return preview.cells.map((cell, i) => ({
    kind: cell.kind || `block-${i}`,
    className: cell.className.replace(/col-span-\d+/g, "col-span-6") + (dense ? " scale-y-110" : ""),
  }));
}

type Props = {
  selectedTemplateKey: string | null;
  onSelect: (templateKey: string, template: IndustryTemplateSeed) => void;
  onConfirmTemplate: () => void;
  onStartBlank: () => void;
  onBack: () => void;
};

export function AdminTemplateSelectionWorkspace({
  selectedTemplateKey,
  onSelect,
  onConfirmTemplate,
  onStartBlank,
  onBack,
}: Props) {
  const templates = useMemo(() => listIndustryTemplates(), []);
  const [device, setDevice] = useState<TemplatePreviewDevice>("desktop");
  const selected = templates.find((t) => t.templateKey === selectedTemplateKey) ?? templates[0] ?? null;
  const frame = DEVICE_FRAME[device];
  const blocks = selected ? industryWireframeBlocks(selected, device) : [];
  const compactPreview = selected ? layoutAwareTemplatePreview(selected) : null;

  return (
    <main
      data-testid="admin-landing-page-editor"
      data-template-selection-workspace="1"
      data-template-preview-v2="1"
      className="space-y-5"
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <button type="button" className="text-sm text-muted" onClick={onBack} data-testid="template-workspace-back">
            بازگشت
          </button>
          <h1 className="mt-1 text-xl font-black">فضای انتخاب قالب</h1>
          <p className="mt-1 text-sm text-muted">
            پیش‌نمایش سیمی ترکیب صفحه در دستگاه‌های مختلف — بدون ایجاد داده نمونه واقعی در این مرحله.
          </p>
        </div>
        <button
          type="button"
          className="rounded-xl border border-border px-4 py-2 text-sm font-bold text-slate-700"
          data-testid="start-blank-from-templates"
          onClick={onStartBlank}
        >
          شروع از صفحه خالی
        </button>
      </div>

      <div className="grid gap-5 xl:grid-cols-[minmax(0,22rem)_minmax(0,1fr)]" data-testid="template-picker">
        <section className="space-y-3" data-testid="template-browser">
          <h2 className="text-sm font-black text-slate-800">قالب‌های صنعتی</h2>
          <div className="grid max-h-[70vh] gap-3 overflow-y-auto pe-1 sm:grid-cols-2 xl:grid-cols-1">
            {templates.map((template) => {
              const preview = layoutAwareTemplatePreview(template);
              const isSelected = selected?.templateKey === template.templateKey;
              return (
                <button
                  key={template.templateKey}
                  type="button"
                  className={`rounded-2xl border bg-surface-elevated p-3 text-start transition-colors ${
                    isSelected ? "border-[#2563EB] ring-2 ring-[#2563EB]/25" : "border-border hover:border-[#2563EB]/50"
                  }`}
                  data-testid={`template-card-${template.templateKey}`}
                  data-template-selected={isSelected ? "true" : undefined}
                  onClick={() => onSelect(template.templateKey, template)}
                >
                  <div
                    className={`mb-2 grid h-20 grid-cols-6 gap-1 rounded-xl p-1.5 ${preview.toneClass}`}
                    aria-hidden
                    data-testid="template-composition-miniature"
                    data-industry={template.industry}
                  >
                    {preview.cells.slice(0, 10).map((cell, index) => (
                      <span key={`${template.templateKey}-${index}`} className={cell.className} data-kind={cell.kind} />
                    ))}
                  </div>
                  <p className="font-black">{template.nameFa}</p>
                  <p className="mt-1 line-clamp-2 text-xs text-muted">{template.descriptionFa}</p>
                  <p className="mt-2 text-[11px] font-bold text-slate-600">
                    {template.sectionPresetList.length.toLocaleString("fa-IR")} بخش · {templateSectionSummaryFa(template)}
                  </p>
                </button>
              );
            })}
          </div>
        </section>

        <section className="space-y-4 rounded-2xl border border-border bg-surface-elevated p-4" data-testid="template-detail-panel">
          {selected && compactPreview ? (
            <>
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="text-lg font-black" data-testid="template-detail-name">{selected.nameFa}</h2>
                  <p className="mt-1 text-sm text-muted">{selected.descriptionFa}</p>
                  <p className="mt-2 text-xs font-bold text-slate-600" data-testid="template-detail-sections">
                    {selected.sectionPresetList.length.toLocaleString("fa-IR")} بخش · {templateSectionSummaryFa(selected)}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <button
                    type="button"
                    className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
                    data-testid="use-selected-template"
                    onClick={onConfirmTemplate}
                  >
                    استفاده از این قالب
                  </button>
                  <button
                    type="button"
                    className="rounded-xl border px-4 py-2 text-sm font-bold"
                    data-testid="template-detail-start-blank"
                    onClick={onStartBlank}
                  >
                    شروع از صفحه خالی
                  </button>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2" data-testid="template-device-toolbar" role="toolbar" aria-label="حالت پیش‌نمایش دستگاه">
                {(
                  [
                    { id: "desktop" as const, icon: Monitor, tip: "پیش‌نمایش دسکتاپ" },
                    { id: "tablet" as const, icon: Tablet, tip: "پیش‌نمایش تبلت" },
                    { id: "mobile" as const, icon: Smartphone, tip: "پیش‌نمایش موبایل" },
                  ] as const
                ).map((item) => {
                  const Icon = item.icon;
                  const active = device === item.id;
                  return (
                    <button
                      key={item.id}
                      type="button"
                      title={item.tip}
                      aria-label={item.tip}
                      aria-pressed={active}
                      data-testid={`template-device-${item.id}`}
                      data-device-active={active ? "true" : undefined}
                      className={`inline-flex items-center gap-2 rounded-xl border px-3 py-2 text-xs font-bold ${
                        active ? "border-[#2563EB] bg-blue-50 text-[#2563EB]" : "border-border text-slate-600"
                      }`}
                      onClick={() => setDevice(item.id)}
                    >
                      <Icon className="h-4 w-4" aria-hidden />
                      {DEVICE_FRAME[item.id].label}
                    </button>
                  );
                })}
              </div>

              <div className="flex justify-center overflow-x-auto rounded-2xl border border-dashed border-slate-300 bg-slate-50 p-4" data-testid="template-preview-stage">
                <div
                  className={`${frame.widthClass} ${frame.heightClass} rounded-2xl border border-slate-300 bg-white p-3 shadow-sm`}
                  data-testid="template-preview-frame"
                  data-preview-device={device}
                  data-preview-width={device}
                >
                  <div className="mb-2 flex items-center justify-between text-[11px] font-bold text-slate-500">
                    <span>پیش‌نمایش سیمی · {selected.nameFa}</span>
                    <span>{frame.label}</span>
                  </div>
                  <div
                    className={`grid grid-cols-12 gap-2 ${compactPreview.toneClass} rounded-xl p-2`}
                    data-testid="template-wireframe-canvas"
                    data-industry={selected.industry}
                    data-template-key={selected.templateKey}
                  >
                    {blocks.map((block, index) => (
                      <span key={`${selected.templateKey}-${device}-${index}`} className={block.className} data-kind={block.kind} />
                    ))}
                  </div>
                </div>
              </div>

              <div className="rounded-2xl border border-border bg-slate-50 p-4" data-testid="template-seed-pack-summary">
                <h3 className="font-black">داده نمونه این قالب</h3>
                <p className="mt-1 text-xs text-muted">خلاصهٔ قرارداد آینده — در این مرحله داده‌ای ساخته نمی‌شود.</p>
                <ul className="mt-3 space-y-1 text-sm font-bold text-slate-800" data-testid="template-seed-counts">
                  <li>۸ درخت دسته‌بندی سه‌سطحی</li>
                  <li>۱۵ محصول نمونه</li>
                  <li>تصاویر محصول مرتبط</li>
                  <li>بنرهای مرتبط</li>
                  <li>برندهای مرتبط</li>
                </ul>
                <div className="mt-3 rounded-xl border border-dashed border-slate-300 bg-white px-3 py-2 text-xs text-muted" data-testid="template-shared-demo-note">
                  <p className="font-bold text-slate-700">محتوای نمایشی مشترک (غیرقالب‌محور):</p>
                  <p className="mt-1">استوری‌های نمونه عمومی · نظرات نمونه عمومی · مقالات عمومی نمایشی</p>
                  <p className="mt-1">مقالات مخصوص قالب نیستند؛ استوری و نظر از دادهٔ نمایشی مشترک استفاده می‌کنند.</p>
                </div>
                <p className="mt-3 text-xs text-slate-600" data-testid="template-seed-tracking-note">
                  داده‌های نمونه با برچسب سیستمی ثبت می‌شوند تا بعداً بدون حذف اطلاعات واقعی فروشگاه پاک‌سازی شوند.
                </p>
              </div>

              <div className="flex flex-wrap gap-2" data-testid="template-cleanup-prepare-actions">
                <button
                  type="button"
                  disabled
                  className="rounded-xl border border-border bg-white px-4 py-2 text-sm font-bold text-slate-500 opacity-70"
                  data-testid="cleanup-sample-data-action"
                  title="در مرحله بعد فعال می‌شود"
                >
                  پاک‌سازی داده‌های نمونه
                  <span className="ms-2 text-[11px] font-normal">(در مرحله بعد فعال می‌شود)</span>
                </button>
                <button
                  type="button"
                  disabled
                  className="rounded-xl border border-border bg-white px-4 py-2 text-sm font-bold text-slate-500 opacity-70"
                  data-testid="prepare-store-action"
                  title="در مرحله بعد فعال می‌شود"
                >
                  آماده‌سازی فروشگاه برای ورود اطلاعات
                  <span className="ms-2 text-[11px] font-normal">(در مرحله بعد فعال می‌شود)</span>
                </button>
              </div>
            </>
          ) : (
            <p className="text-sm text-muted">یک قالب را از فهرست انتخاب کنید.</p>
          )}
        </section>
      </div>
    </main>
  );
}
