"use client";

import { useEffect, useState } from "react";
import { AdminSearchableCombobox } from "../admin-searchable-combobox";
import { loadCategoryTree, type CategoryTreeNodeDto } from "../catalog-category-api";
import { listAdminBrandOptions } from "../host-client";
import { bannerSlotCountForVariant } from "./landing-section-catalog.ts";
import { listAdminMenus } from "../menus/admin-menus-api.ts";
import { MediaLibraryDialog } from "../media-library-dialog.tsx";
import { mediaPreviewUrl, type MediaAssetDto } from "../media-api.ts";
import { AdminResourceSelector, ResourceSelectorTrigger } from "./admin-resource-selector.tsx";
import { bannerSlotCellClass, bannerSlotLayoutClass } from "./layout-aware-previews.tsx";
import { VariantLivePreview } from "../../../lib/storefront-composition/variant-live-preview.tsx";
import {
  sourceCapabilityForVariant,
  strategyLabelFa,
} from "../../../lib/storefront-composition/source-capability.ts";
import type { AdminSelectableDataSource } from "../../../lib/storefront-composition/types.ts";
import { SIZE_PRESETS } from "../../../lib/storefront-composition/types.ts";
import { SIZE_PRESET_CONTRACTS } from "../../../lib/storefront-composition/size-presets.ts";
import { AdminHeroSliderSettings } from "./admin-hero-slider-settings.tsx";
import type { SectionWizardStep } from "./admin-landing-section-forms-types.ts";

export type { SectionWizardStep };
export { validateWizardStep, wizardStepsForHost } from "./admin-section-wizard-logic.ts";

function asStringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.map((item) => String(item)).filter(Boolean);
}

function flattenCategories(nodes: CategoryTreeNodeDto[]): { value: string; label: string }[] {
  return nodes
    .filter((node) => node.status !== "Archived")
    .map((node) => ({ value: node.id, label: node.name }));
}

type BannerItem = { imageUrl?: string; href?: string; title?: string; text?: string; ctaLabel?: string };

function asBannerItems(value: unknown): BannerItem[] {
  if (!Array.isArray(value)) return [];
  return value.map((item) => {
    if (!item || typeof item !== "object") return { imageUrl: "", href: "", title: "" };
    const row = item as Record<string, unknown>;
    return {
      imageUrl: typeof row.imageUrl === "string" ? row.imageUrl : "",
      href: typeof row.href === "string" ? row.href : "",
      title: typeof row.title === "string" ? row.title : "",
      text: typeof row.text === "string" ? row.text : "",
      ctaLabel: typeof row.ctaLabel === "string" ? row.ctaLabel : "",
    };
  });
}

/** Settings + source fields for wizard (no language, no story authoring). */
export function LandingSectionForm({
  type,
  value,
  onChange,
  mode = "all",
  heroShowErrors,
  heroFocusRequest,
  heroVariantKey,
}: {
  type: string;
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
  /** Wizard can show source-only or settings-only slices. */
  mode?: "all" | "source" | "settings";
  heroShowErrors?: boolean;
  heroFocusRequest?: {
    slideIndex: number;
    field: import("../../../lib/storefront-composition/hero-slider-config.ts").HeroSlideFieldKey;
    token: number;
  } | null;
  heroVariantKey?: string | null;
}) {
  const title = typeof value.title === "string" ? value.title : "";
  const set = (patch: Record<string, unknown>) => onChange({ ...value, ...patch });
  const variantKey = typeof value.variantKey === "string" ? value.variantKey : undefined;
  const showBannerSlots = type === "BannerShowcase" || (variantKey?.startsWith("banner.") ?? false);
  const showStorySettings = type === "StoryRail" || (variantKey?.startsWith("story.") ?? false);
  const capability = variantKey ? sourceCapabilityForVariant(variantKey) : null;

  if (showBannerSlots && mode !== "source") {
    const slots = Math.max(1, bannerSlotCountForVariant(variantKey || "banner.single"));
    const current = asBannerItems(value.items);
    const items = Array.from({ length: slots }, (_, index) => current[index] ?? { imageUrl: "", href: "", title: "" });
    const heightPreset = typeof value.heightPreset === "string" ? value.heightPreset : "Medium";
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        {mode !== "settings" ? null : (
          <TextField label="عنوان بخش" value={title} onChange={(next) => set({ title: next })} />
        )}
        {mode === "all" || mode === "settings" ? (
          <label className="block text-sm">
            <span className="mb-1 block font-bold">ارتفاع بنر</span>
            <select
              className="w-full rounded-xl border border-border bg-surface px-3 py-2"
              value={heightPreset}
              onChange={(e) => set({ heightPreset: e.target.value })}
              data-testid="height-preset-select"
            >
              {SIZE_PRESETS.map((preset) => (
                <option key={preset} value={preset}>{SIZE_PRESET_CONTRACTS[preset].nameFa}</option>
              ))}
            </select>
          </label>
        ) : null}
        <div
          className={bannerSlotLayoutClass(variantKey)}
          data-testid="banner-slot-editor"
          data-banner-variant={variantKey}
        >
          <p className="col-span-full text-sm font-bold">جایگاه‌های بنر ({slots.toLocaleString("fa-IR")} مورد)</p>
          {items.every((item) => !(item.imageUrl ?? "").trim()) ? (
            <p className="col-span-full rounded-xl border border-dashed px-3 py-2 text-xs text-muted" data-testid="banner-empty-media-hint">
              هنوز تصویری برای بنرها تنظیم نشده. برای هر جایگاه یک آدرس تصویر وارد کنید تا در فروشگاه خالی نماند.
            </p>
          ) : null}
          {items.map((item, index) => (
            <div
              key={index}
              className={`rounded-xl border border-border bg-slate-50 p-3 space-y-2 ${bannerSlotCellClass(variantKey, index)}`}
              data-testid={`banner-slot-${index}`}
            >
              <p className="text-xs font-bold text-muted">جایگاه {(index + 1).toLocaleString("fa-IR")}</p>
              <TextField
                label="آدرس تصویر"
                value={item.imageUrl ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, imageUrl: next };
                  set({ items: nextItems });
                }}
                placeholder="https://…"
              />
              <TextField
                label="پیوند مقصد"
                value={item.href ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, href: next };
                  set({ items: nextItems });
                }}
                placeholder="/offers"
              />
              <TextField
                label="عنوان کوتاه"
                value={item.title ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, title: next };
                  set({ items: nextItems });
                }}
              />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (showStorySettings) {
    const take = typeof value.take === "number" ? value.take : 12;
    return (
      <div className="space-y-3" data-testid="landing-section-form" data-story-display-settings="1">
        <TextField label="عنوان بخش (اختیاری)" value={title} onChange={(next) => set({ title: next })} />
        <TakeField value={take} onChange={(next) => set({ take: next, items: [] })} max={50} label="حداکثر تعداد نمایش" />
        <p className="rounded-xl border border-dashed px-3 py-2 text-xs text-muted" data-testid="story-display-only-hint">
          استوری‌ها از ماژول استوری (پس از تأیید) خوانده می‌شوند. اینجا فقط تنظیمات نمایش است — ساخت استوری در این بخش ممکن نیست.
        </p>
      </div>
    );
  }

  if (type === "Hero" && mode !== "source") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <AdminHeroSliderSettings
          value={value}
          onChange={onChange}
          showErrors={heroShowErrors}
          focusRequest={heroFocusRequest}
          variantKey={heroVariantKey}
        />
      </div>
    );
  }

  if (type === "PromoBanner" && mode !== "source") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} required />
        <TextField
          label="پیوند دکمه"
          value={typeof value.href === "string" ? value.href : ""}
          onChange={(next) => set({ href: next })}
          placeholder="/products"
        />
      </div>
    );
  }

  if (type === "RichText" && mode !== "source") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <label className="block text-sm">
          <span className="mb-1 block font-bold">متن</span>
          <textarea
            className="min-h-32 w-full rounded-xl border border-border bg-surface px-3 py-2"
            value={typeof value.text === "string" ? value.text : ""}
            onChange={(event) => set({ text: event.target.value })}
          />
        </label>
      </div>
    );
  }

  if (type === "Reviews") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        {(mode === "all" || mode === "settings") ? (
          <TextField label="عنوان بخش" value={title} onChange={(next) => set({ title: next })} />
        ) : null}
        <p className="text-xs text-muted">نظرهای تأییدشدهٔ فروشگاه نمایش داده می‌شود.</p>
      </div>
    );
  }

  if (type === "ArticleList") {
    return (
      <ArticleSourceForm
        value={value}
        onChange={onChange}
        mode={mode}
        strategies={(capability?.strategies ?? ["LatestArticles", "Manual"]) as AdminSelectableDataSource[]}
      />
    );
  }

  if (type === "CategoryGrid") {
    const selected = asStringArray(value.categoryIds ?? value.ids);
    if (mode === "settings") {
      return (
        <div className="space-y-3" data-testid="landing-section-form">
          <TextField label="عنوان بخش" value={title} onChange={(next) => set({ title: next })} />
        </div>
      );
    }
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        {mode === "all" ? <TextField label="عنوان بخش" value={title} onChange={(next) => set({ title: next })} /> : null}
        <ManualResourceField
          family="categories"
          selected={selected}
          onChange={(categoryIds) => set({ categoryIds })}
          emptyHint="هنوز دسته‌ای انتخاب نشده. چند دسته اضافه کنید تا بخش در فروشگاه خالی نماند."
          emptyTestId="empty-state-category-source"
        />
      </div>
    );
  }

  if (type === "BrandStrip") {
    const selected = asStringArray(value.brandIds ?? value.ids);
    const source = typeof value.source === "string" ? value.source : "Manual";
    // Host BrandStrip persists Manual brandIds only — no fake Dynamic strategies.
    if (mode === "settings") {
      return (
        <div className="space-y-3" data-testid="landing-section-form" data-brand-settings="1">
          <TextField label="عنوان بخش" value={title} onChange={(next) => set({ title: next })} />
          <TakeField value={typeof value.take === "number" ? value.take : Math.max(selected.length, 6)} onChange={(take) => set({ take })} />
          {variantKey ? <VariantLivePreview variantKey={variantKey} eager testId="brand-settings-preview" /> : null}
        </div>
      );
    }
    return (
      <div className="space-y-3" data-testid="landing-section-form" data-brand-source="1">
        {mode === "all" ? <TextField label="عنوان بخش" value={title} onChange={(next) => set({ title: next })} /> : null}
        {(mode === "all" || mode === "source") ? (
          <>
            <label className="block text-sm">
              <span className="mb-1 block font-bold">منبع برندها</span>
              <select
                className="w-full rounded-xl border border-border bg-surface px-3 py-2"
                value={source === "Manual" ? "Manual" : "Manual"}
                onChange={() => set({ source: "Manual" })}
                data-testid="brand-source-strategy"
              >
                <option value="Manual">انتخاب دستی</option>
              </select>
              <span className="mt-1 block text-xs text-muted">منبع پویای برند روی Host برای این بخش تعریف نشده؛ فقط انتخاب دستی معتبر است.</span>
            </label>
            <ManualResourceField
              family="brands"
              selected={selected}
              onChange={(brandIds) => set({ brandIds, source: "Manual" })}
              emptyHint="هنوز برندی انتخاب نشده. چند برند اضافه کنید تا بخش در فروشگاه خالی نماند."
              emptyTestId="empty-state-brand-source"
            />
          </>
        ) : null}
      </div>
    );
  }

  if (type === "ProductCollection") {
    return (
      <ProductSourceForm
        value={value}
        onChange={onChange}
        mode={mode}
        strategies={(capability?.strategies ?? ["Manual", "Category", "Brand", "Newest"]) as AdminSelectableDataSource[]}
      />
    );
  }

  if (type === "NavigationMenu" && mode !== "source") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <MenuPicker value={typeof value.menuId === "string" ? value.menuId : null} onChange={(menuId) => set({ menuId })} />
      </div>
    );
  }

  if (mode === "source" && !capability?.strategies.length) {
    return <p className="text-sm text-muted">این بخش منبع محتوای جداگانه‌ای ندارد.</p>;
  }

  return <p className="text-sm text-muted">این بخش قابل ویرایش نیست.</p>;
}

function ArticleSourceForm({
  value,
  onChange,
  mode,
  strategies,
}: {
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
  mode: "all" | "source" | "settings";
  strategies: AdminSelectableDataSource[];
}) {
  const title = typeof value.title === "string" ? value.title : "";
  const set = (patch: Record<string, unknown>) => onChange({ ...value, ...patch });
  const hostSource = typeof value.source === "string" ? value.source : "Latest";
  const dynamic = hostSource === "Manual" ? "Manual" : "LatestArticles";
  const articleIds = asStringArray(value.articleIds);

  return (
    <div className="space-y-3" data-testid="landing-section-form" data-article-source="1">
      {(mode === "all" || mode === "settings") ? (
        <TextField label="عنوان بخش" value={title} onChange={(next) => set({ title: next })} />
      ) : null}
      {(mode === "all" || mode === "source") ? (
        <>
          <label className="block text-sm">
            <span className="mb-1 block font-bold">منبع محتوا</span>
            <select
              className="w-full rounded-xl border border-border bg-surface px-3 py-2"
              value={dynamic}
              onChange={(event) => {
                const next = event.target.value;
                if (next === "Manual") set({ source: "Manual", articleIds: articleIds });
                else set({ source: "Latest", articleIds: [] });
              }}
              data-testid="article-source-strategy"
            >
              {strategies.includes("LatestArticles") ? (
                <option value="LatestArticles">{strategyLabelFa("LatestArticles")}</option>
              ) : null}
              {strategies.includes("Manual") ? (
                <option value="Manual">{strategyLabelFa("Manual")}</option>
              ) : null}
            </select>
          </label>
          {dynamic === "LatestArticles" ? (
            <TakeField value={typeof value.take === "number" ? value.take : 6} onChange={(take) => set({ take, source: "Latest" })} />
          ) : (
            <ManualResourceField
              family="articles"
              selected={articleIds}
              onChange={(ids) => set({ articleIds: ids, source: "Manual" })}
              emptyHint="هنوز مطلبی انتخاب نشده. از فهرست مطالب انتخاب کنید."
              emptyTestId="empty-state-article-source"
            />
          )}
        </>
      ) : null}
    </div>
  );
}

function ProductSourceForm({
  value,
  onChange,
  mode,
  strategies,
}: {
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
  mode: "all" | "source" | "settings";
  strategies: AdminSelectableDataSource[];
}) {
  const title = typeof value.title === "string" ? value.title : "";
  const set = (patch: Record<string, unknown>) => onChange({ ...value, ...patch });
  const source = typeof value.source === "string" ? value.source : "Newest";
  const variantKey = typeof value.variantKey === "string" ? value.variantKey : "";
  const isExplorer = variantKey === "product.explorer" || variantKey === "explorer";
  const bannerImageUrl = typeof value.bannerImageUrl === "string" ? value.bannerImageUrl : "";
  const bannerHref = typeof value.bannerHref === "string" ? value.bannerHref : "";
  const bannerMediaAssetId = typeof value.bannerMediaAssetId === "string" ? value.bannerMediaAssetId : "";
  const [bannerMediaOpen, setBannerMediaOpen] = useState(false);
  const manualEmpty = source === "Manual" && asStringArray(value.productIds).length === 0;
  const categoryMissing = source === "Category" && !(typeof value.categoryId === "string" && value.categoryId);
  const brandMissing = source === "Brand" && !(typeof value.brandId === "string" && value.brandId);
  const allowed = strategies.filter((s) => ["Manual", "Category", "Brand", "Newest"].includes(s));
  const bannerPreview =
    bannerImageUrl.trim()
    || (bannerMediaAssetId.trim() ? mediaPreviewUrl(bannerMediaAssetId) ?? "" : "");

  return (
    <div className="space-y-3" data-testid="product-section-editor">
      {(mode === "all" || mode === "settings") ? (
        <TextField label="عنوان بخش" value={title} onChange={(next) => set({ title: next })} />
      ) : null}
      {(mode === "all" || mode === "settings") && isExplorer ? (
        <div className="space-y-3 rounded-2xl border border-border bg-surface p-3" data-testid="explorer-banner-settings">
          <p className="text-sm font-bold text-foreground">بنر ثابت کاشف</p>
          <p className="text-xs text-muted">
            بنر هم‌اندازه کارت سمت راست است؛ با کشیدن (گراب) جمع می‌شود و نوار باریک می‌ماند تا دوباره باز شود. فلش راهنما فقط نشانهٔ ورق‌زدن است.
          </p>
          <div className="flex flex-wrap items-center gap-3">
            <button
              type="button"
              className="inline-flex min-h-11 items-center rounded-xl border border-border bg-background px-3 text-sm font-bold"
              onClick={() => setBannerMediaOpen(true)}
              data-testid="explorer-banner-pick-media"
            >
              {bannerPreview ? "تغییر تصویر بنر" : "انتخاب تصویر بنر"}
            </button>
            {bannerPreview ? (
              <button
                type="button"
                className="text-xs font-bold text-red-600"
                onClick={() => set({ bannerImageUrl: "", bannerMediaAssetId: null })}
                data-testid="explorer-banner-clear-media"
              >
                حذف تصویر
              </button>
            ) : null}
          </div>
          {bannerPreview ? (
            <div className="overflow-hidden rounded-xl border border-border bg-background">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img src={bannerPreview} alt="" className="h-36 w-full object-cover" data-testid="explorer-banner-preview" />
            </div>
          ) : (
            <p className="rounded-xl border border-dashed border-border px-3 py-6 text-center text-xs text-muted">
              هنوز بنری انتخاب نشده.
            </p>
          )}
          <TextField
            label="لینک بنر"
            value={bannerHref}
            onChange={(next) => set({ bannerHref: next })}
            placeholder="/products یا آدرس کامل"
          />
          <MediaLibraryDialog
            open={bannerMediaOpen}
            title="انتخاب تصویر بنر کاشف"
            selectionMode="single"
            assetKind="image"
            onClose={() => setBannerMediaOpen(false)}
            onConfirm={(assets: MediaAssetDto[]) => {
              const asset = assets[0];
              if (!asset) return;
              set({
                bannerMediaAssetId: asset.mediaAssetId,
                bannerImageUrl: mediaPreviewUrl(asset.mediaAssetId) ?? "",
              });
              setBannerMediaOpen(false);
            }}
          />
        </div>
      ) : null}
      {(mode === "all" || mode === "source") ? (
        <>
          <label className="block text-sm">
            <span className="mb-1 block font-bold">منبع کالا</span>
            <select
              className="w-full rounded-xl border border-border bg-surface px-3 py-2"
              value={source}
              onChange={(event) => set({ source: event.target.value })}
              data-testid="product-source-strategy"
            >
              {allowed.map((item) => (
                <option key={item} value={item}>{strategyLabelFa(item)}</option>
              ))}
            </select>
          </label>
          {manualEmpty || categoryMissing || brandMissing ? (
            <p className="rounded-xl border border-dashed px-3 py-2 text-xs text-muted" data-testid="empty-state-product-source">
              {manualEmpty
                ? "هنوز کالایی انتخاب نشده. چند کالا اضافه کنید تا بخش در فروشگاه خالی نماند."
                : categoryMissing
                  ? "یک دسته انتخاب کنید؛ اگر دسته حذف شده باشد منبع را عوض کنید."
                  : "یک برند انتخاب کنید؛ اگر برند حذف شده باشد منبع را عوض کنید."}
            </p>
          ) : null}
          <TakeField value={typeof value.take === "number" ? value.take : 8} onChange={(take) => set({ take })} />
          {source === "Category" ? (
            <EntitySinglePicker
              kind="category"
              value={typeof value.categoryId === "string" ? value.categoryId : null}
              onChange={(categoryId) => set({ categoryId })}
            />
          ) : null}
          {source === "Brand" ? (
            <EntitySinglePicker
              kind="brand"
              value={typeof value.brandId === "string" ? value.brandId : null}
              onChange={(brandId) => set({ brandId })}
            />
          ) : null}
          {source === "Manual" ? (
            <ManualResourceField
              family="products"
              selected={asStringArray(value.productIds)}
              onChange={(productIds) => set({ productIds })}
              emptyHint="هنوز کالایی انتخاب نشده. از فهرست کالا انتخاب کنید."
              emptyTestId="empty-state-product-manual"
            />
          ) : null}
        </>
      ) : null}
    </div>
  );
}

function ManualResourceField({
  family,
  selected,
  onChange,
  emptyHint,
  emptyTestId,
}: {
  family: "products" | "articles" | "brands" | "categories";
  selected: string[];
  onChange: (ids: string[]) => void;
  emptyHint: string;
  emptyTestId: string;
}) {
  const [open, setOpen] = useState(false);
  return (
    <div data-testid={`landing-${family === "products" ? "product" : family}-multi-picker`}>
      <ResourceSelectorTrigger
        count={selected.length}
        onOpen={() => setOpen(true)}
        emptyHint={emptyHint}
        emptyTestId={emptyTestId}
      />
      <AdminResourceSelector
        family={family}
        selectedIds={selected}
        onChange={(ids) => onChange(ids)}
        open={open}
        onClose={() => setOpen(false)}
      />
    </div>
  );
}

function MenuPicker({ value, onChange }: { value: string | null; onChange: (next: string | null) => void }) {
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let cancelled = false;
    void listAdminMenus().then((result) => {
      if (cancelled) return;
      setLoading(false);
      if (!result.ok) return;
      setOptions(result.data.filter((row) => row.isEnabled).map((row) => ({ value: row.menuId, label: row.title })));
    });
    return () => {
      cancelled = true;
    };
  }, []);
  return (
    <div data-testid="landing-menu-picker">
      <p className="mb-1 text-sm font-bold">منو</p>
      {loading ? <p className="text-xs text-muted">در حال بارگذاری…</p> : null}
      {!loading && options.length === 0 ? <p className="text-xs text-muted">منوی فعالی یافت نشد.</p> : null}
      <AdminSearchableCombobox
        value={value}
        options={options}
        onChange={onChange}
        placeholder="جستجوی منو…"
        testId="landing-menu-combobox"
      />
    </div>
  );
}

function TextField({
  label,
  value,
  onChange,
  required,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (next: string) => void;
  required?: boolean;
  placeholder?: string;
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-bold">{label}</span>
      <input
        required={required}
        className="w-full rounded-xl border border-border bg-surface px-3 py-2"
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}

function TakeField({
  value,
  onChange,
  max = 24,
  label = "تعداد نمایش",
}: {
  value: number;
  onChange: (next: number) => void;
  max?: number;
  label?: string;
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-bold">{label}</span>
      <input
        type="number"
        min={1}
        max={max}
        className="w-full rounded-xl border border-border bg-surface px-3 py-2"
        value={value}
        onChange={(event) => onChange(Math.min(max, Math.max(1, Number(event.target.value) || 1)))}
      />
    </label>
  );
}

function EntitySinglePicker({
  kind,
  value,
  onChange,
}: {
  kind: "category" | "brand";
  value: string | null;
  onChange: (next: string | null) => void;
}) {
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    const load = kind === "category"
      ? loadCategoryTree("fa-IR").then((result) => {
        if (result.state !== "ok" || !result.data) return [];
        return flattenCategories(result.data);
      })
      : listAdminBrandOptions().then((result) => result.ok ? result.items.map((item) => ({ value: item.brandId, label: item.name })) : []);
    void load.then((rows) => {
      if (cancelled) return;
      setOptions(rows);
      setLoading(false);
    });
    return () => {
      cancelled = true;
    };
  }, [kind]);

  return (
    <div>
      <p className="mb-1 text-sm font-bold">{kind === "category" ? "دسته" : "برند"}</p>
      {loading ? <p className="text-xs text-muted">در حال بارگذاری…</p> : null}
      <AdminSearchableCombobox
        value={value}
        options={options}
        onChange={onChange}
        placeholder={kind === "category" ? "جستجوی دسته…" : "جستجوی برند…"}
        testId={`landing-${kind}-picker`}
      />
    </div>
  );
}
