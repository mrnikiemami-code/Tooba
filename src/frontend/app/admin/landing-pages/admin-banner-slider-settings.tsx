"use client";

/**
 * تنظیمات بنر — الگوی دقیق اسلایدر هیرو، بدون «تعداد» و «زمان تغییر».
 * تعداد تب‌ها از چیدمان Variant (بنرهای طرح) می‌آید؛ هر تب یک بنر را ویرایش می‌کند.
 */

import { useEffect, useMemo, useRef, useState } from "react";
import { AlertCircle } from "lucide-react";
import { MediaLibraryDialog } from "../media-library-dialog.tsx";
import { mediaPreviewUrl, type MediaAssetDto } from "../media-api.ts";
import { AdminResourceSelector, ResourceSelectorTrigger } from "./admin-resource-selector.tsx";
import { loadProductWorkspace } from "../host-client.ts";
import {
  HERO_DESTINATION_LABELS_FA,
  HERO_DESTINATION_TYPES,
  resolveHeroSlideHref,
  type HeroDestinationType,
} from "../../../lib/storefront-composition/hero-slider-config.ts";

export type BannerItemConfig = {
  mediaAssetId: string;
  imageUrl: string;
  alt: string;
  title: string;
  text: string;
  ctaLabel: string;
  destinationType: HeroDestinationType;
  targetId: string;
  targetSlug: string;
  targetLabel: string;
  customUrl: string;
  href: string;
};

type BannerFieldKey = "image" | "alt" | "title" | "ctaLabel" | "destination" | "customUrl";

const FIELD_TEST_IDS: Record<BannerFieldKey, string> = {
  image: "banner-item-pick-media",
  alt: "banner-item-alt",
  title: "banner-item-title",
  ctaLabel: "banner-item-cta-label",
  destination: "banner-item-destination-type",
  customUrl: "banner-item-custom-url",
};

function emptyBanner(): BannerItemConfig {
  return {
    mediaAssetId: "",
    imageUrl: "",
    alt: "",
    title: "",
    text: "",
    ctaLabel: "",
    destinationType: "none",
    targetId: "",
    targetSlug: "",
    targetLabel: "",
    customUrl: "",
    href: "",
  };
}

function normalizeBanner(value: unknown, index: number): BannerItemConfig {
  const row = value && typeof value === "object" ? (value as Record<string, unknown>) : {};
  const str = (key: string) => (typeof row[key] === "string" ? (row[key] as string) : "");
  const legacyHref = str("href");
  const base: BannerItemConfig = {
    mediaAssetId: str("mediaAssetId"),
    imageUrl: str("imageUrl"),
    alt: str("alt") || str("altText"),
    title: str("title") || `بنر ${(index + 1).toLocaleString("fa-IR")}`,
    text: str("text") || str("description"),
    ctaLabel: str("ctaLabel"),
    destinationType: (HERO_DESTINATION_TYPES as readonly string[]).includes(str("destinationType"))
      ? (str("destinationType") as HeroDestinationType)
      : legacyHref
        ? "custom-url"
        : "none",
    targetId: str("targetId"),
    targetSlug: str("targetSlug"),
    targetLabel: str("targetLabel"),
    customUrl: str("customUrl") || (legacyHref && !/^\/products/.test(legacyHref) ? legacyHref : ""),
    href: "",
  };
  base.href = resolveHeroSlideHref(base) || legacyHref;
  return base;
}

export function readBannerItems(config: Record<string, unknown>, slotCount: number): BannerItemConfig[] {
  const raw = Array.isArray(config.items) ? config.items : [];
  const n = Math.max(1, Math.floor(slotCount) || 1);
  return Array.from({ length: n }, (_, index) => normalizeBanner(raw[index], index));
}

function validateBanner(item: BannerItemConfig): Partial<Record<BannerFieldKey, string>> {
  const errors: Partial<Record<BannerFieldKey, string>> = {};
  if (!(item.mediaAssetId.trim() || item.imageUrl.trim())) {
    errors.image = "تصویر بنر الزامی است.";
  }
  if (!item.title.trim()) {
    errors.title = "عنوان قابل‌نمایش الزامی است.";
  }
  if (item.destinationType === "product" && !(item.targetId.trim() || item.targetSlug.trim())) {
    errors.destination = "یک محصول انتخاب کنید.";
  }
  if (item.destinationType === "category" && !item.targetId.trim()) {
    errors.destination = "یک دسته‌بندی انتخاب کنید.";
  }
  if (item.destinationType === "custom-url" && !item.customUrl.trim()) {
    errors.customUrl = "آدرس سفارشی را وارد کنید.";
  }
  return errors;
}

export function AdminBannerSliderSettings({
  value,
  onChange,
  slotCount,
  showErrors = false,
}: {
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
  /** تعداد بنرها بر اساس چیدمان طرح (Variant). */
  slotCount: number;
  showErrors?: boolean;
}) {
  const slots = Math.max(1, Math.floor(slotCount) || 1);
  const items = useMemo(() => readBannerItems(value, slots), [value, slots]);
  const [active, setActive] = useState(0);
  const [mediaOpen, setMediaOpen] = useState(false);
  const [productOpen, setProductOpen] = useState(false);
  const [categoryOpen, setCategoryOpen] = useState(false);
  const [productBusy, setProductBusy] = useState(false);
  const panelRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (active > slots - 1) setActive(slots - 1);
  }, [active, slots]);

  const set = (patch: Record<string, unknown>) => onChange({ ...value, ...patch });

  const patchItem = (index: number, patch: Partial<BannerItemConfig>) => {
    const nextItems = items.map((row, i) => {
      if (i !== index) return row;
      const next = { ...row, ...patch };
      next.href = resolveHeroSlideHref(next);
      return next;
    });
    set({ items: nextItems });
  };

  const onPickMedia = (assets: MediaAssetDto[]) => {
    const asset = assets[0];
    if (!asset) return;
    patchItem(active, {
      mediaAssetId: asset.mediaAssetId,
      imageUrl: mediaPreviewUrl(asset.mediaAssetId) ?? "",
      alt: items[active]?.alt || asset.originalFileName || "",
    });
    setMediaOpen(false);
  };

  const onPickProduct = async (ids: string[], labels?: Record<string, string>) => {
    const id = ids[0] ?? "";
    if (!id) {
      patchItem(active, { destinationType: "product", targetId: "", targetSlug: "", targetLabel: "", href: "" });
      return;
    }
    setProductBusy(true);
    try {
      const result = await loadProductWorkspace(id, true);
      const slug = result.view?.slug?.trim() || "";
      const label = labels?.[id] || result.view?.title || "محصول انتخاب‌شده";
      patchItem(active, {
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
    patchItem(active, {
      destinationType: "category",
      targetId: id,
      targetSlug: "",
      targetLabel: label,
      customUrl: "",
      href: id ? `/products?categoryId=${encodeURIComponent(id)}` : "",
    });
    setCategoryOpen(false);
  };

  const item = items[active] ?? emptyBanner();
  const errors = validateBanner(item);
  const errorFor = (field: BannerFieldKey): string | null =>
    showErrors ? errors[field] ?? null : null;
  const previewSrc =
    item.imageUrl.trim() || (item.mediaAssetId.trim() ? mediaPreviewUrl(item.mediaAssetId) ?? "" : "");

  const fieldClass = (field: BannerFieldKey) =>
    `w-full rounded-xl border px-3 py-2 ${
      errorFor(field) ? "border-red-500 ring-1 ring-red-400" : "border-border"
    }`;

  return (
    <div className="space-y-4" data-testid="banner-slider-settings" ref={panelRef}>
      <div className="rounded-2xl border border-border p-3" data-testid="banner-item-tabs">
        <div className="mb-3 flex flex-wrap gap-2" role="tablist" aria-label="بنرها">
          {items.map((_, index) => {
            const invalid = showErrors && Object.keys(validateBanner(items[index]!)).length > 0;
            const selected = active === index;
            return (
              <button
                key={`banner-tab-${index}`}
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
                onClick={() => setActive(index)}
                data-testid={`banner-item-tab-${index}`}
                data-has-error={invalid ? "true" : undefined}
              >
                {invalid ? <AlertCircle className="h-3.5 w-3.5" aria-hidden /> : null}
                بنر {(index + 1).toLocaleString("fa-IR")}
                {invalid ? <span className="sr-only">دارای خطا</span> : null}
              </button>
            );
          })}
        </div>

        <div className="space-y-3" role="tabpanel" data-testid={`banner-item-panel-${active}`}>
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
              data-testid={FIELD_TEST_IDS.image}
              aria-invalid={Boolean(errorFor("image"))}
            >
              انتخاب تصویر از مدیا
            </button>
          </div>
          {errorFor("image") ? <p className="text-xs text-red-600" role="alert">{errorFor("image")}</p> : null}

          <label className="block text-sm">
            <span className={`mb-1 block font-bold ${errorFor("title") ? "text-red-600" : ""}`}>عنوان</span>
            <input
              className={fieldClass("title")}
              value={item.title}
              onChange={(e) => patchItem(active, { title: e.target.value })}
              required
              aria-invalid={Boolean(errorFor("title"))}
              data-testid={FIELD_TEST_IDS.title}
            />
            {errorFor("title") ? <span className="mt-1 block text-xs text-red-600">{errorFor("title")}</span> : null}
          </label>

          <label className="block text-sm">
            <span className="mb-1 block font-bold">Alt تصویر</span>
            <input
              className="w-full rounded-xl border border-border px-3 py-2"
              value={item.alt}
              onChange={(e) => patchItem(active, { alt: e.target.value })}
              data-testid={FIELD_TEST_IDS.alt}
            />
          </label>

          <label className="block text-sm">
            <span className="mb-1 block font-bold">توضیح</span>
            <textarea
              className="min-h-20 w-full rounded-xl border border-border px-3 py-2"
              value={item.text}
              onChange={(e) => patchItem(active, { text: e.target.value })}
              placeholder="اختیاری"
              data-testid="banner-item-text"
            />
          </label>

          <label className="block text-sm">
            <span className="mb-1 block font-bold">برچسب دکمه (اختیاری)</span>
            <input
              className="w-full rounded-xl border border-border px-3 py-2"
              value={item.ctaLabel}
              onChange={(e) => patchItem(active, { ctaLabel: e.target.value })}
              placeholder="مثلاً مشاهده"
              data-testid={FIELD_TEST_IDS.ctaLabel}
            />
          </label>

          <label className="block text-sm">
            <span className={`mb-1 block font-bold ${errorFor("destination") ? "text-red-600" : ""}`}>مقصد پیوند</span>
            <select
              className={fieldClass("destination")}
              value={item.destinationType}
              onChange={(e) => {
                const nextType = e.target.value as HeroDestinationType;
                patchItem(active, {
                  destinationType: nextType,
                  targetId: nextType === "product" || nextType === "category" ? item.targetId : "",
                  targetSlug: nextType === "product" ? item.targetSlug : "",
                  targetLabel: nextType === "product" || nextType === "category" ? item.targetLabel : "",
                  customUrl: nextType === "custom-url" ? item.customUrl : "",
                  href: nextType === "all-products" ? "/products" : "",
                });
              }}
              aria-invalid={Boolean(errorFor("destination"))}
              data-testid={FIELD_TEST_IDS.destination}
            >
              {HERO_DESTINATION_TYPES.map((type) => (
                <option key={type} value={type}>{HERO_DESTINATION_LABELS_FA[type]}</option>
              ))}
            </select>
            {errorFor("destination") ? (
              <span className="mt-1 block text-xs text-red-600">{errorFor("destination")}</span>
            ) : null}
          </label>

          {item.destinationType === "all-products" ? (
            <p className="rounded-xl border border-dashed px-3 py-2 text-xs text-muted" data-testid="banner-dest-all-products">
              مقصد: فهرست همه محصولات (`/products`)
            </p>
          ) : null}

          {item.destinationType === "product" ? (
            <div data-testid="banner-dest-product" className="space-y-2">
              {item.targetLabel ? (
                <p className="rounded-xl bg-slate-50 px-3 py-2 text-sm font-bold" data-testid="banner-product-label">
                  {item.targetLabel}
                </p>
              ) : (
                <p className="text-xs text-muted">هنوز محصولی انتخاب نشده.</p>
              )}
              {productBusy ? <p className="text-xs text-muted">در حال بارگذاری…</p> : null}
              <ResourceSelectorTrigger
                count={item.targetId ? 1 : 0}
                onOpen={() => setProductOpen(true)}
                emptyHint="یک محصول از فهرست انتخاب کنید"
              />
              <AdminResourceSelector
                family="products"
                selectedIds={item.targetId ? [item.targetId] : []}
                onChange={(ids, labels) => void onPickProduct(ids, labels)}
                multiSelect={false}
                maxCount={1}
                open={productOpen}
                onClose={() => setProductOpen(false)}
                title="انتخاب محصول مقصد"
              />
            </div>
          ) : null}

          {item.destinationType === "category" ? (
            <div data-testid="banner-dest-category" className="space-y-2">
              {item.targetLabel ? (
                <p className="rounded-xl bg-slate-50 px-3 py-2 text-sm font-bold" data-testid="banner-category-label">
                  {item.targetLabel}
                </p>
              ) : (
                <p className="text-xs text-muted">هنوز دسته‌ای انتخاب نشده.</p>
              )}
              <ResourceSelectorTrigger
                count={item.targetId ? 1 : 0}
                onOpen={() => setCategoryOpen(true)}
                emptyHint="یک دسته‌بندی از فهرست انتخاب کنید"
              />
              <AdminResourceSelector
                family="categories"
                selectedIds={item.targetId ? [item.targetId] : []}
                onChange={onPickCategory}
                multiSelect={false}
                maxCount={1}
                open={categoryOpen}
                onClose={() => setCategoryOpen(false)}
                title="انتخاب دسته‌بندی مقصد"
              />
            </div>
          ) : null}

          {item.destinationType === "custom-url" ? (
            <label className="block text-sm">
              <span className={`mb-1 block font-bold ${errorFor("customUrl") ? "text-red-600" : ""}`}>آدرس سفارشی</span>
              <input
                className={fieldClass("customUrl")}
                dir="ltr"
                value={item.customUrl}
                onChange={(e) => patchItem(active, { customUrl: e.target.value })}
                placeholder="/offers یا https://…"
                aria-invalid={Boolean(errorFor("customUrl"))}
                data-testid={FIELD_TEST_IDS.customUrl}
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
        title="انتخاب تصویر بنر"
        selectionMode="single"
        assetKind="image"
        onClose={() => setMediaOpen(false)}
        onConfirm={onPickMedia}
      />
    </div>
  );
}
