"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { AlertCircle } from "lucide-react";
import { MediaLibraryDialog } from "../media-library-dialog.tsx";
import { mediaPreviewUrl, type MediaAssetDto } from "../media-api.ts";
import { AdminResourceSelector, ResourceSelectorTrigger } from "./admin-resource-selector.tsx";
import { loadProductWorkspace } from "../host-client.ts";
import {
  HERO_DESTINATION_LABELS_FA,
  HERO_DESTINATION_TYPES,
  HERO_HEIGHT_PRESETS,
  HERO_HEIGHT_PRESET_LABELS_FA,
  HERO_SLIDER_MAX_SLIDES,
  emptyHeroSlide,
  heroImageGuidance,
  heroVariantIdFromKey,
  normalizeHeroSlides,
  readHeroSliderFields,
  resolveHeroSlideHref,
  validateHeroSliderDetailed,
  type HeroDestinationType,
  type HeroHeightPreset,
  type HeroSlideConfig,
  type HeroSlideFieldKey,
} from "../../../lib/storefront-composition/hero-slider-config.ts";

export type HeroSliderSettingsProps = {
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
  /** When true, inline/tab errors are visible and clear live as fields become valid. */
  showErrors?: boolean;
  /** Request focus/scroll to a specific slide+field (from wizard validation). */
  focusRequest?: { slideIndex: number; field: HeroSlideFieldKey; token: number } | null;
  variantKey?: string | null;
};

const FIELD_TEST_IDS: Record<HeroSlideFieldKey, string> = {
  image: "hero-slide-pick-media",
  alt: "hero-slide-alt",
  title: "hero-slide-title",
  description: "hero-slide-description",
  ctaLabel: "hero-slide-cta-label",
  destination: "hero-slide-destination-type",
  customUrl: "hero-slide-custom-url",
};

export function AdminHeroSliderSettings({
  value,
  onChange,
  showErrors = false,
  focusRequest = null,
  variantKey,
}: HeroSliderSettingsProps) {
  const fields = readHeroSliderFields(value);
  const [activeSlide, setActiveSlide] = useState(0);
  const [mediaOpen, setMediaOpen] = useState(false);
  const [productOpen, setProductOpen] = useState(false);
  const [categoryOpen, setCategoryOpen] = useState(false);
  const [productBusy, setProductBusy] = useState(false);
  const panelRef = useRef<HTMLDivElement | null>(null);
  const lastFocusToken = useRef<number | null>(null);

  const variantId = heroVariantIdFromKey(
    typeof variantKey === "string" && variantKey
      ? variantKey
      : typeof value.variantKey === "string"
        ? value.variantKey
        : "hero.fullscreen",
  );
  const guidance = heroImageGuidance(variantId, fields.heightPreset);
  const validation = useMemo(() => validateHeroSliderDetailed(value), [value]);

  const set = (patch: Record<string, unknown>) => onChange({ ...value, ...patch, autoplay: true });

  const updateSlideCount = (nextCountRaw: number) => {
    const nextCount = Math.max(1, Math.min(HERO_SLIDER_MAX_SLIDES, Math.floor(nextCountRaw) || 1));
    if (nextCount < fields.slideCount) {
      const ok = window.confirm(
        "با کم کردن تعداد اسلایدر، اسلایدهای انتهایی و اطلاعات ثبت‌شده‌شان حذف می‌شوند. ادامه می‌دهید؟",
      );
      if (!ok) return;
    }
    const slides = normalizeHeroSlides(fields.slides, nextCount);
    set({ slideCount: nextCount, slides });
    setActiveSlide((current) => Math.min(current, nextCount - 1));
  };

  const patchSlide = (index: number, patch: Partial<HeroSlideConfig>) => {
    const slides = fields.slides.map((row, i) => {
      if (i !== index) return row;
      const next = { ...row, ...patch };
      next.href = resolveHeroSlideHref(next);
      return next;
    });
    set({ slides, slideCount: slides.length });
  };

  const onPickMedia = (assets: MediaAssetDto[]) => {
    const asset = assets[0];
    if (!asset) return;
    patchSlide(activeSlide, {
      mediaAssetId: asset.mediaAssetId,
      imageUrl: mediaPreviewUrl(asset.mediaAssetId) ?? "",
      alt: fields.slides[activeSlide]?.alt || asset.originalFileName || "",
    });
    setMediaOpen(false);
  };

  const onPickProduct = async (ids: string[], labels?: Record<string, string>) => {
    const id = ids[0] ?? "";
    if (!id) {
      patchSlide(activeSlide, {
        destinationType: "product",
        targetId: "",
        targetSlug: "",
        targetLabel: "",
        href: "",
      });
      return;
    }
    setProductBusy(true);
    try {
      const result = await loadProductWorkspace(id, true);
      const slug = result.view?.slug?.trim() || "";
      const label = labels?.[id] || result.view?.title || "محصول انتخاب‌شده";
      patchSlide(activeSlide, {
        destinationType: "product",
        targetId: id,
        targetSlug: slug,
        targetLabel: label,
        customUrl: "",
        href: slug ? `/products/${encodeURIComponent(slug)}` : "",
      });
    } finally {
      setProductBusy(false);
      setProductOpen(false);
    }
  };

  const onPickCategory = (ids: string[], labels?: Record<string, string>) => {
    const id = ids[0] ?? "";
    const label = id ? labels?.[id] || "دسته انتخاب‌شده" : "";
    patchSlide(activeSlide, {
      destinationType: "category",
      targetId: id,
      targetSlug: "",
      targetLabel: label,
      customUrl: "",
      href: id ? `/products?categoryId=${encodeURIComponent(id)}` : "",
    });
    setCategoryOpen(false);
  };

  useEffect(() => {
    if (!focusRequest) return;
    if (lastFocusToken.current === focusRequest.token) return;
    lastFocusToken.current = focusRequest.token;
    setActiveSlide(focusRequest.slideIndex);
    const timer = window.setTimeout(() => {
      const testId = FIELD_TEST_IDS[focusRequest.field];
      const root = panelRef.current ?? document;
      const el = root.querySelector(`[data-testid="${testId}"]`) as HTMLElement | null;
      if (el) {
        el.scrollIntoView({ block: "center", behavior: "smooth" });
        if (typeof el.focus === "function") el.focus();
      }
    }, 60);
    return () => window.clearTimeout(timer);
  }, [focusRequest]);

  const slide = fields.slides[activeSlide] ?? emptyHeroSlide();
  const slideErrors = validation.slides[activeSlide]?.errors ?? [];
  const errorFor = (field: HeroSlideFieldKey): string | null => {
    if (!showErrors) return null;
    return slideErrors.find((row) => row.field === field)?.messageFa ?? null;
  };
  const previewSrc =
    slide.imageUrl.trim()
    || (slide.mediaAssetId.trim() ? mediaPreviewUrl(slide.mediaAssetId) ?? "" : "");

  const fieldClass = (field: HeroSlideFieldKey) =>
    `w-full rounded-xl border px-3 py-2 ${
      errorFor(field) ? "border-red-500 ring-1 ring-red-400" : "border-border"
    }`;

  return (
    <div className="space-y-4" data-testid="hero-slider-settings" ref={panelRef}>
      <label className="block text-sm">
        <span className="mb-1 block font-bold">اندازه نمایش</span>
        <select
          className={`w-full rounded-xl border bg-surface px-3 py-2 ${
            showErrors && validation.heightErrorFa ? "border-red-500" : "border-border"
          }`}
          value={fields.heightPreset}
          onChange={(e) => set({ heightPreset: e.target.value as HeroHeightPreset, displayHeightPx: undefined })}
          data-testid="hero-height-preset"
          aria-invalid={Boolean(showErrors && validation.heightErrorFa)}
        >
          {HERO_HEIGHT_PRESETS.map((preset) => (
            <option key={preset} value={preset}>{HERO_HEIGHT_PRESET_LABELS_FA[preset]}</option>
          ))}
        </select>
        {showErrors && validation.heightErrorFa ? (
          <span className="mt-1 block text-xs text-red-600">{validation.heightErrorFa}</span>
        ) : null}
      </label>

      <p
        className="rounded-xl border border-dashed border-sky-200 bg-sky-50 px-3 py-2 text-xs text-sky-950"
        data-testid="hero-image-guidance"
        data-variant={variantId}
        data-height={fields.heightPreset}
      >
        {guidance.summaryFa}
      </p>

      <label className="block text-sm">
        <span className="mb-1 block font-bold">زمان تغییر اسلایدر (ثانیه)</span>
        <input
          type="number"
          min={1}
          max={60}
          step={0.5}
          className="w-full rounded-xl border border-border bg-surface px-3 py-2"
          value={fields.slideIntervalSec}
          onChange={(e) => set({ slideIntervalSec: Number(e.target.value) || 0 })}
          data-testid="hero-slide-interval"
          aria-invalid={Boolean(showErrors && validation.intervalErrorFa)}
        />
        {showErrors && validation.intervalErrorFa ? (
          <span className="mt-1 block text-xs text-red-600">{validation.intervalErrorFa}</span>
        ) : null}
      </label>

      <label className="block text-sm">
        <span className="mb-1 block font-bold">تعداد اسلایدر (حداکثر {HERO_SLIDER_MAX_SLIDES.toLocaleString("fa-IR")})</span>
        <input
          type="number"
          min={1}
          max={HERO_SLIDER_MAX_SLIDES}
          className="w-full rounded-xl border border-border bg-surface px-3 py-2"
          value={fields.slideCount}
          onChange={(e) => updateSlideCount(Number(e.target.value))}
          data-testid="hero-slide-count"
        />
      </label>

      <div className="rounded-2xl border border-border p-3" data-testid="hero-slide-tabs">
        <div className="mb-3 flex flex-wrap gap-2" role="tablist" aria-label="اسلایدها">
          {fields.slides.map((_, index) => {
            const invalid = showErrors && (validation.slides[index]?.errors.length ?? 0) > 0;
            const selected = activeSlide === index;
            return (
              <button
                key={`slide-tab-${index}`}
                type="button"
                role="tab"
                aria-selected={selected}
                aria-invalid={invalid || undefined}
                className={`inline-flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-bold ${
                  invalid
                    ? selected
                      ? "bg-red-600 text-white"
                      : "border border-red-500 bg-red-50 text-red-700"
                    : selected
                      ? "bg-slate-900 text-white"
                      : "border bg-white"
                }`}
                onClick={() => setActiveSlide(index)}
                data-testid={`hero-slide-tab-${index}`}
                data-has-error={invalid ? "true" : undefined}
              >
                {invalid ? <AlertCircle className="h-3.5 w-3.5" aria-hidden /> : null}
                اسلاید {(index + 1).toLocaleString("fa-IR")}
                {invalid ? <span className="sr-only">دارای خطا</span> : null}
              </button>
            );
          })}
        </div>

        <div className="space-y-3" role="tabpanel" data-testid={`hero-slide-panel-${activeSlide}`}>
          <div className="flex flex-wrap items-start gap-3">
            <div
              className={`h-28 w-40 overflow-hidden rounded-xl border bg-slate-100 ${
                errorFor("image") ? "border-red-500 ring-1 ring-red-400" : ""
              }`}
            >
              {previewSrc ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={previewSrc} alt="" className="h-full w-full object-cover" />
              ) : (
                <div className="flex h-full items-center justify-center text-xs text-muted">بدون تصویر</div>
              )}
            </div>
            <button
              type="button"
              className={`rounded-xl border px-3 py-2 text-sm font-bold ${
                errorFor("image") ? "border-red-500 text-red-700" : ""
              }`}
              onClick={() => setMediaOpen(true)}
              data-testid="hero-slide-pick-media"
              aria-invalid={Boolean(errorFor("image"))}
            >
              انتخاب تصویر از مدیا
            </button>
          </div>
          {errorFor("image") ? (
            <p className="text-xs text-red-600" role="alert">{errorFor("image")}</p>
          ) : null}

          <label className="block text-sm">
            <span className={`mb-1 block font-bold ${errorFor("title") ? "text-red-600" : ""}`}>عنوان</span>
            <input
              className={fieldClass("title")}
              value={slide.title}
              onChange={(e) => patchSlide(activeSlide, { title: e.target.value })}
              required
              aria-invalid={Boolean(errorFor("title"))}
              data-testid="hero-slide-title"
            />
            {errorFor("title") ? <span className="mt-1 block text-xs text-red-600">{errorFor("title")}</span> : null}
          </label>

          <label className="block text-sm">
            <span className={`mb-1 block font-bold ${errorFor("alt") ? "text-red-600" : ""}`}>Alt تصویر</span>
            <input
              className={fieldClass("alt")}
              value={slide.alt}
              onChange={(e) => patchSlide(activeSlide, { alt: e.target.value })}
              required
              aria-invalid={Boolean(errorFor("alt"))}
              data-testid="hero-slide-alt"
            />
            {errorFor("alt") ? <span className="mt-1 block text-xs text-red-600">{errorFor("alt")}</span> : null}
          </label>

          <label className="block text-sm">
            <span className="mb-1 block font-bold">توضیح</span>
            <textarea
              className="min-h-20 w-full rounded-xl border border-border px-3 py-2"
              value={slide.description}
              onChange={(e) => patchSlide(activeSlide, { description: e.target.value })}
              data-testid="hero-slide-description"
              placeholder="اختیاری"
            />
          </label>

          <label className="block text-sm">
            <span className={`mb-1 block font-bold ${errorFor("ctaLabel") ? "text-red-600" : ""}`}>
              برچسب دکمه (اختیاری)
            </span>
            <input
              className={fieldClass("ctaLabel")}
              value={slide.ctaLabel}
              onChange={(e) => patchSlide(activeSlide, { ctaLabel: e.target.value })}
              aria-invalid={Boolean(errorFor("ctaLabel"))}
              data-testid="hero-slide-cta-label"
              placeholder="مثلاً مشاهده"
            />
            {errorFor("ctaLabel") ? (
              <span className="mt-1 block text-xs text-red-600">{errorFor("ctaLabel")}</span>
            ) : null}
          </label>

          <label className="block text-sm">
            <span className={`mb-1 block font-bold ${errorFor("destination") ? "text-red-600" : ""}`}>
              مقصد پیوند
            </span>
            <select
              className={fieldClass("destination")}
              value={slide.destinationType}
              onChange={(e) => {
                const destinationType = e.target.value as HeroDestinationType;
                patchSlide(activeSlide, {
                  destinationType,
                  targetId: destinationType === "product" || destinationType === "category" ? slide.targetId : "",
                  targetSlug: destinationType === "product" ? slide.targetSlug : "",
                  targetLabel:
                    destinationType === "product" || destinationType === "category" ? slide.targetLabel : "",
                  customUrl: destinationType === "custom-url" ? slide.customUrl : "",
                });
              }}
              aria-invalid={Boolean(errorFor("destination"))}
              data-testid="hero-slide-destination-type"
            >
              {HERO_DESTINATION_TYPES.map((type) => (
                <option key={type} value={type}>{HERO_DESTINATION_LABELS_FA[type]}</option>
              ))}
            </select>
            {errorFor("destination") ? (
              <span className="mt-1 block text-xs text-red-600">{errorFor("destination")}</span>
            ) : null}
          </label>

          {slide.destinationType === "all-products" ? (
            <p className="rounded-xl border border-dashed px-3 py-2 text-xs text-muted" data-testid="hero-dest-all-products">
              مقصد: فهرست همه محصولات (`/products`)
            </p>
          ) : null}

          {slide.destinationType === "product" ? (
            <div data-testid="hero-dest-product" className="space-y-2">
              {slide.targetLabel ? (
                <p className="rounded-xl bg-slate-50 px-3 py-2 text-sm font-bold" data-testid="hero-product-label">
                  {slide.targetLabel}
                </p>
              ) : (
                <p className="text-xs text-muted">هنوز محصولی انتخاب نشده.</p>
              )}
              {productBusy ? <p className="text-xs text-muted">در حال بارگذاری…</p> : null}
              <ResourceSelectorTrigger
                count={slide.targetId ? 1 : 0}
                onOpen={() => setProductOpen(true)}
                emptyHint="یک محصول از فهرست انتخاب کنید"
              />
              <AdminResourceSelector
                family="products"
                selectedIds={slide.targetId ? [slide.targetId] : []}
                onChange={(ids, labels) => void onPickProduct(ids, labels)}
                multiSelect={false}
                maxCount={1}
                open={productOpen}
                onClose={() => setProductOpen(false)}
                title="انتخاب محصول مقصد"
              />
            </div>
          ) : null}

          {slide.destinationType === "category" ? (
            <div data-testid="hero-dest-category" className="space-y-2">
              {slide.targetLabel ? (
                <p className="rounded-xl bg-slate-50 px-3 py-2 text-sm font-bold" data-testid="hero-category-label">
                  {slide.targetLabel}
                </p>
              ) : (
                <p className="text-xs text-muted">هنوز دسته‌ای انتخاب نشده.</p>
              )}
              <ResourceSelectorTrigger
                count={slide.targetId ? 1 : 0}
                onOpen={() => setCategoryOpen(true)}
                emptyHint="یک دسته‌بندی از فهرست انتخاب کنید"
              />
              <AdminResourceSelector
                family="categories"
                selectedIds={slide.targetId ? [slide.targetId] : []}
                onChange={onPickCategory}
                multiSelect={false}
                maxCount={1}
                open={categoryOpen}
                onClose={() => setCategoryOpen(false)}
                title="انتخاب دسته‌بندی مقصد"
              />
            </div>
          ) : null}

          {slide.destinationType === "custom-url" ? (
            <label className="block text-sm">
              <span className={`mb-1 block font-bold ${errorFor("customUrl") ? "text-red-600" : ""}`}>
                آدرس سفارشی
              </span>
              <input
                className={fieldClass("customUrl")}
                dir="ltr"
                value={slide.customUrl}
                onChange={(e) => patchSlide(activeSlide, { customUrl: e.target.value })}
                placeholder="/offers یا https://…"
                aria-invalid={Boolean(errorFor("customUrl"))}
                data-testid="hero-slide-custom-url"
              />
              {errorFor("customUrl") ? (
                <span className="mt-1 block text-xs text-red-600">{errorFor("customUrl")}</span>
              ) : null}
            </label>
          ) : null}
        </div>
      </div>

      <MediaLibraryDialog
        open={mediaOpen}
        title="انتخاب تصویر اسلاید"
        selectionMode="single"
        assetKind="image"
        onClose={() => setMediaOpen(false)}
        onConfirm={onPickMedia}
      />
    </div>
  );
}
