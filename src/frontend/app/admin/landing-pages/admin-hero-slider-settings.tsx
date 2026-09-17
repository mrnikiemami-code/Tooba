"use client";

import { useState } from "react";
import { MediaLibraryDialog } from "../media-library-dialog.tsx";
import { mediaPreviewUrl, type MediaAssetDto } from "../media-api.ts";
import {
  HERO_SLIDER_MAX_SLIDES,
  emptyHeroSlide,
  normalizeHeroSlides,
  readHeroSliderFields,
  type HeroSlideConfig,
} from "../../../lib/storefront-composition/hero-slider-config.ts";

export function AdminHeroSliderSettings({
  value,
  onChange,
}: {
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
}) {
  const fields = readHeroSliderFields(value);
  const [activeSlide, setActiveSlide] = useState(0);
  const [mediaOpen, setMediaOpen] = useState(false);

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
    const slides = fields.slides.map((row, i) => (i === index ? { ...row, ...patch } : row));
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

  const slide = fields.slides[activeSlide] ?? emptyHeroSlide();
  const previewSrc =
    slide.imageUrl.trim()
    || (slide.mediaAssetId.trim() ? mediaPreviewUrl(slide.mediaAssetId) ?? "" : "");

  return (
    <div className="space-y-4" data-testid="hero-slider-settings">
      <label className="block text-sm">
        <span className="mb-1 block font-bold">اندازه نمایش (پیکسل)</span>
        <input
          type="number"
          min={120}
          max={1200}
          className="w-full rounded-xl border border-border bg-surface px-3 py-2"
          value={fields.displayHeightPx}
          onChange={(e) => set({ displayHeightPx: Number(e.target.value) || 0 })}
          data-testid="hero-display-height"
        />
      </label>

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
        />
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
          {fields.slides.map((_, index) => (
            <button
              key={`slide-tab-${index}`}
              type="button"
              role="tab"
              aria-selected={activeSlide === index}
              className={`rounded-xl px-3 py-1.5 text-xs font-bold ${
                activeSlide === index ? "bg-slate-900 text-white" : "border bg-white"
              }`}
              onClick={() => setActiveSlide(index)}
              data-testid={`hero-slide-tab-${index}`}
            >
              اسلاید {(index + 1).toLocaleString("fa-IR")}
            </button>
          ))}
        </div>

        <div className="space-y-3" role="tabpanel" data-testid={`hero-slide-panel-${activeSlide}`}>
          <div className="flex flex-wrap items-start gap-3">
            <div className="h-28 w-40 overflow-hidden rounded-xl border bg-slate-100">
              {previewSrc ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={previewSrc} alt="" className="h-full w-full object-cover" />
              ) : (
                <div className="flex h-full items-center justify-center text-xs text-muted">بدون تصویر</div>
              )}
            </div>
            <button
              type="button"
              className="rounded-xl border px-3 py-2 text-sm font-bold"
              onClick={() => setMediaOpen(true)}
              data-testid="hero-slide-pick-media"
            >
              انتخاب تصویر از مدیا
            </button>
          </div>

          <label className="block text-sm">
            <span className="mb-1 block font-bold">عنوان</span>
            <input
              className="w-full rounded-xl border px-3 py-2"
              value={slide.title}
              onChange={(e) => patchSlide(activeSlide, { title: e.target.value })}
              required
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block font-bold">Alt تصویر</span>
            <input
              className="w-full rounded-xl border px-3 py-2"
              value={slide.alt}
              onChange={(e) => patchSlide(activeSlide, { alt: e.target.value })}
              required
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block font-bold">عنوان سئو اسلاید</span>
            <input
              className="w-full rounded-xl border px-3 py-2"
              value={slide.seoTitle}
              onChange={(e) => patchSlide(activeSlide, { seoTitle: e.target.value })}
              required
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block font-bold">توضیح سئو اسلاید</span>
            <textarea
              className="min-h-20 w-full rounded-xl border px-3 py-2"
              value={slide.seoDescription}
              onChange={(e) => patchSlide(activeSlide, { seoDescription: e.target.value })}
              required
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block font-bold">پیوند دکمه (اختیاری)</span>
            <input
              className="w-full rounded-xl border px-3 py-2"
              dir="ltr"
              value={slide.href}
              onChange={(e) => patchSlide(activeSlide, { href: e.target.value })}
              placeholder="/products"
            />
          </label>
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
