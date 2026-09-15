"use client";

import type { VariantPreviewKind } from "../../../lib/storefront-composition/types.ts";
import { getVariant } from "../../../lib/storefront-composition/registry.ts";
import type { IndustryTemplateSeed } from "../../../lib/storefront-composition/industry-templates.ts";
import { templateCompositionMiniature } from "../../../lib/storefront-composition/industry-templates.ts";

/** Layout-aware miniature cells — not generic colored bars. */
export type PreviewCell = {
  className: string;
  kind?: string;
};

const INDUSTRY_ACCENT: Record<string, string> = {
  fashion: "bg-rose-200/80",
  autoparts: "bg-slate-300/90",
  interior: "bg-amber-100/90",
  beauty: "bg-fuchsia-100/80",
  grocery: "bg-lime-100/90",
  electronics: "bg-sky-100/90",
  sports: "bg-orange-100/90",
  kids: "bg-yellow-100/90",
  books: "bg-stone-200/90",
  general: "bg-slate-100/90",
};

function industryTone(industry: string): string {
  const key = industry.toLowerCase();
  if (key.includes("fashion") || key.includes("مدا")) return INDUSTRY_ACCENT.fashion!;
  if (key.includes("auto") || key.includes("قطعه")) return INDUSTRY_ACCENT.autoparts!;
  if (key.includes("interior") || key.includes("دکور") || key.includes("منزل")) return INDUSTRY_ACCENT.interior!;
  if (key.includes("beauty") || key.includes("زیبایی")) return INDUSTRY_ACCENT.beauty!;
  return INDUSTRY_ACCENT.general!;
}

/** Distinct layout miniature for a Variant — structure communicates the real layout. */
export function layoutAwareVariantPreview(variantKey: string): PreviewCell[] {
  switch (variantKey) {
    case "hero.full-width":
    case "hero.contained":
      return [{ className: "col-span-6 row-span-2 rounded-md bg-slate-700/25 border border-slate-400/40" }];
    case "hero.split":
      return [
        { className: "col-span-3 row-span-2 rounded-md bg-slate-600/30 border border-slate-400/30" },
        { className: "col-span-3 row-span-2 rounded-md bg-white/70 border border-slate-300/50 space-y-1 p-1 flex flex-col justify-center" },
      ];
    case "hero.side-promos":
      return [
        { className: "col-span-4 row-span-2 rounded-md bg-slate-600/30" },
        { className: "col-span-2 rounded-sm bg-slate-400/25" },
        { className: "col-span-2 rounded-sm bg-slate-400/20" },
      ];
    case "hero.editorial":
      return [
        { className: "col-span-6 row-span-2 rounded-md bg-gradient-to-l from-slate-500/20 to-slate-700/35 relative" },
      ];
    case "story.circle":
    case "story.image-circles":
      return Array.from({ length: 6 }, () => ({
        className: "rounded-full aspect-square bg-white border-2 border-rose-300/70 shadow-sm",
        kind: "story-circle",
      }));
    case "story.rounded-cards":
      return Array.from({ length: 4 }, () => ({
        className: "rounded-xl h-10 bg-white border border-rose-200/80",
        kind: "story-card",
      }));
    case "story.icon-shortcuts":
      return Array.from({ length: 5 }, () => ({
        className: "rounded-lg aspect-square bg-white border border-slate-300 flex items-center justify-center",
        kind: "story-icon",
      }));
    case "product.grid":
    case "product.large-cards":
      return Array.from({ length: 4 }, () => ({
        className: "rounded-md h-12 bg-white border border-amber-200/80 flex flex-col p-0.5 gap-0.5",
        kind: "product-card",
      }));
    case "product.compact-rows":
    case "product.minimal-list":
      return Array.from({ length: 4 }, () => ({
        className: "col-span-6 h-3 rounded bg-white border border-amber-100",
        kind: "product-row",
      }));
    case "product.category-columns":
    case "ranked.multi-column":
      return Array.from({ length: 3 }, () => ({
        className: "rounded-md h-14 bg-white border border-amber-200/70 flex flex-col gap-0.5 p-0.5",
        kind: "product-column",
      }));
    case "product.featured-plus-rail":
      return [
        { className: "col-span-2 row-span-2 rounded-md bg-white border-2 border-amber-300/80", kind: "featured" },
        { className: "col-span-4 h-5 rounded bg-white border border-amber-100" },
        { className: "col-span-4 h-5 rounded bg-white border border-amber-100" },
      ];
    case "product.card-carousel":
    case "product.tabbed":
      return Array.from({ length: 4 }, () => ({
        className: "rounded-md h-11 bg-white border border-amber-200/70",
        kind: "product-rail",
      }));
    case "ranked.horizontal":
    case "ranked.ticker":
      return Array.from({ length: 5 }, (_, i) => ({
        className: `rounded-md h-8 bg-white border border-orange-200/70 ${i === 0 ? "ring-1 ring-orange-300" : ""}`,
        kind: "ranked",
      }));
    case "ranked.grid":
      return Array.from({ length: 4 }, () => ({
        className: "rounded-md h-10 bg-white border border-orange-200/70",
        kind: "ranked-grid",
      }));
    case "banner.single":
      return [{ className: "col-span-6 h-12 rounded-xl bg-violet-200/50 border border-violet-300/60" }];
    case "banner.two-equal":
    case "banner.two-asymmetric":
      return [
        { className: "col-span-3 h-12 rounded-lg bg-violet-200/50 border border-violet-300/50" },
        { className: "col-span-3 h-12 rounded-lg bg-violet-200/40 border border-violet-300/40" },
      ];
    case "banner.four-grid":
      return Array.from({ length: 4 }, () => ({
        className: "col-span-3 h-7 rounded-md bg-violet-200/45 border border-violet-300/40",
      }));
    case "banner.one-large-two-small":
      return [
        { className: "col-span-4 row-span-2 rounded-lg bg-violet-300/50 border border-violet-400/40" },
        { className: "col-span-2 h-5 rounded-md bg-violet-200/40" },
        { className: "col-span-2 h-5 rounded-md bg-violet-200/35" },
      ];
    case "banner.eight-compact":
      return Array.from({ length: 8 }, () => ({
        className: "col-span-1.5 h-5 rounded-sm bg-violet-200/40 border border-violet-300/30",
      })).map((cell, i) => ({
        ...cell,
        className: `col-span-3 h-4 rounded-sm bg-violet-200/40 border border-violet-300/30 ${i >= 4 ? "opacity-80" : ""}`,
      }));
    case "banner.mosaic-2x2":
      return [
        { className: "col-span-3 row-span-2 rounded-lg bg-violet-300/55 border border-violet-400/40" },
        { className: "col-span-3 h-5 rounded-md bg-violet-200/45" },
        { className: "col-span-3 h-5 rounded-md bg-violet-200/35" },
      ];
    case "banner.three":
      return Array.from({ length: 3 }, () => ({
        className: "col-span-2 h-10 rounded-md bg-violet-200/45 border border-violet-300/40",
      }));
    case "banner.one-large-four-small":
      return [
        { className: "col-span-3 row-span-2 rounded-lg bg-violet-300/50" },
        ...Array.from({ length: 4 }, () => ({
          className: "col-span-1.5 h-4 rounded-sm bg-violet-200/40",
        })),
      ].map((cell, i) => (i === 0 ? cell : { ...cell, className: "col-span-3 h-4 rounded-sm bg-violet-200/40" }));
    case "brand.logo-rail":
    case "brand.featured":
      return Array.from({ length: 5 }, () => ({
        className: "rounded-full h-8 w-8 bg-white border border-slate-300",
        kind: "brand-logo",
      }));
    case "brand.grid":
      return Array.from({ length: 6 }, () => ({
        className: "rounded-md h-7 bg-white border border-slate-300",
        kind: "brand-tile",
      }));
    case "article.magazine-rail":
      return Array.from({ length: 3 }, () => ({
        className: "rounded-md h-12 bg-white border border-cyan-200/70 flex flex-col p-0.5 gap-0.5",
        kind: "article",
      }));
    case "article.grid":
      return Array.from({ length: 4 }, () => ({
        className: "rounded-md h-10 bg-white border border-cyan-200/70",
        kind: "article-grid",
      }));
    case "article.featured-plus-list":
      return [
        { className: "col-span-3 row-span-2 rounded-lg bg-white border-2 border-cyan-300/70", kind: "article-featured" },
        { className: "col-span-3 h-3 rounded bg-cyan-50 border border-cyan-100" },
        { className: "col-span-3 h-3 rounded bg-cyan-50 border border-cyan-100" },
        { className: "col-span-3 h-3 rounded bg-cyan-50 border border-cyan-100" },
      ];
    case "category.image-cards":
    case "category.editorial-tiles":
      return Array.from({ length: 4 }, () => ({
        className: "rounded-lg h-10 bg-white border border-emerald-200/70",
        kind: "category",
      }));
    case "category.compact-tiles":
      return Array.from({ length: 6 }, () => ({
        className: "rounded-md h-5 bg-white border border-emerald-100",
        kind: "category-tile",
      }));
    case "category.horizontal-rail":
      return Array.from({ length: 5 }, () => ({
        className: "rounded-full h-8 w-8 bg-white border border-emerald-200",
        kind: "category-rail",
      }));
    case "reviews.card-carousel":
    case "reviews.compact-quotes":
      return Array.from({ length: 3 }, () => ({
        className: "rounded-lg h-10 bg-white border border-yellow-200/70",
        kind: "review",
      }));
    case "promo.default":
      return [{ className: "col-span-6 h-10 rounded-xl bg-pink-200/50 border border-pink-300/50" }];
    case "richtext.default":
      return [
        { className: "col-span-6 h-2 rounded bg-stone-300/60" },
        { className: "col-span-5 h-2 rounded bg-stone-200/60" },
        { className: "col-span-4 h-2 rounded bg-stone-200/50" },
      ];
    case "nav.menu":
      return Array.from({ length: 4 }, () => ({
        className: "col-span-1.5 h-3 rounded bg-blue-100 border border-blue-200",
      })).map((c) => ({ ...c, className: "col-span-3 h-3 rounded bg-blue-100 border border-blue-200" }));
    default: {
      const kind = getVariant(variantKey)?.previewKind ?? "promo";
      return previewKindFallback(kind);
    }
  }
}

function previewKindFallback(kind: VariantPreviewKind): PreviewCell[] {
  if (kind.startsWith("banner")) {
    return Array.from({ length: kind.includes("four") ? 4 : kind.includes("two") ? 2 : 1 }, () => ({
      className: "rounded-md h-8 bg-violet-200/40 border border-violet-300/40",
    }));
  }
  if (kind.startsWith("product") || kind.startsWith("ranked")) {
    return Array.from({ length: 4 }, () => ({
      className: "rounded-md h-9 bg-white border border-amber-200/60",
    }));
  }
  if (kind.startsWith("story")) {
    return Array.from({ length: 5 }, () => ({
      className: "rounded-full aspect-square bg-white border-2 border-rose-300/60",
    }));
  }
  if (kind.startsWith("article")) {
    return Array.from({ length: 3 }, () => ({
      className: "rounded-md h-10 bg-white border border-cyan-200/60",
    }));
  }
  return Array.from({ length: 3 }, () => ({
    className: "rounded-md h-8 bg-white border border-slate-200",
  }));
}

/** Industry template miniature — Fashion/AutoParts/Interior/Beauty visibly differ. */
export function layoutAwareTemplatePreview(template: IndustryTemplateSeed): {
  toneClass: string;
  cells: Array<{ kind: string; className: string }>;
} {
  const miniature = templateCompositionMiniature(template);
  const toneClass = industryTone(template.industry);
  const dense = miniature.length >= 6;
  return {
    toneClass,
    cells: miniature.map((kind, index) => {
      if (kind === "hero") {
        return { kind, className: `col-span-6 ${dense ? "h-5" : "h-7"} rounded-md bg-slate-600/35 border border-slate-500/30` };
      }
      if (kind === "story") {
        return { kind, className: "col-span-1 flex gap-0.5" };
      }
      if (kind === "banner") {
        return { kind, className: `col-span-3 ${dense ? "h-4" : "h-6"} rounded bg-violet-300/45 border border-violet-400/30` };
      }
      if (kind === "product") {
        return { kind, className: `col-span-2 ${dense ? "h-5" : "h-7"} rounded-md bg-white border border-amber-200/70` };
      }
      if (kind === "brand") {
        return { kind, className: "col-span-1 rounded-full aspect-square bg-white border border-slate-300" };
      }
      if (kind === "article") {
        return { kind, className: `col-span-2 ${dense ? "h-5" : "h-7"} rounded-md bg-white border border-cyan-200/70` };
      }
      if (kind === "category") {
        return { kind, className: `col-span-2 ${dense ? "h-4" : "h-6"} rounded-md bg-white border border-emerald-200/70` };
      }
      if (kind === "reviews") {
        return { kind, className: `col-span-2 ${dense ? "h-4" : "h-6"} rounded-md bg-white border border-yellow-200/70` };
      }
      return {
        kind,
        className: `col-span-2 ${dense ? "h-4" : "h-5"} rounded bg-white/80 border border-slate-200 ${index % 2 === 0 ? "" : "opacity-80"}`,
      };
    }),
  };
}

/** Banner slot geometry for editor — matches selected layout. */
export function bannerSlotLayoutClass(variantKey: string | undefined): string {
  switch (variantKey) {
    case "banner.two-equal":
      return "grid grid-cols-2 gap-3";
    case "banner.two-asymmetric":
      return "grid grid-cols-[2fr_1fr] gap-3";
    case "banner.four-grid":
    case "banner.mosaic-2x2":
      return "grid grid-cols-2 gap-3";
    case "banner.one-large-two-small":
      return "grid grid-cols-[2fr_1fr] grid-rows-2 gap-3";
    case "banner.eight-compact":
      return "grid grid-cols-4 gap-2";
    case "banner.three":
      return "grid grid-cols-3 gap-3";
    case "banner.one-large-four-small":
      return "grid grid-cols-2 gap-3";
    default:
      return "grid grid-cols-1 gap-3";
  }
}

export function bannerSlotCellClass(variantKey: string | undefined, index: number): string {
  if (variantKey === "banner.one-large-two-small" && index === 0) {
    return "row-span-2 min-h-[140px]";
  }
  if (variantKey === "banner.mosaic-2x2" && index === 0) {
    return "min-h-[100px]";
  }
  if (variantKey === "banner.eight-compact") {
    return "min-h-[72px]";
  }
  return "min-h-[96px]";
}

export function VariantPreviewCanvas({
  variantKey,
  testId = "variant-preview-canvas",
}: {
  variantKey: string;
  testId?: string;
}) {
  const cells = layoutAwareVariantPreview(variantKey);
  const isStory = variantKey.startsWith("story.");
  const isBrand = variantKey.startsWith("brand.");
  return (
    <div
      className={`mb-3 grid h-16 gap-1 rounded-xl border border-slate-200 bg-slate-50 p-2 ${
        isStory || isBrand ? "grid-flow-col auto-cols-fr items-center" : "grid-cols-6 grid-rows-2"
      }`}
      aria-hidden
      data-testid={testId}
      data-layout-aware="1"
      data-preview-fingerprint={variantKey}
    >
      {cells.map((cell, index) => (
        <span key={`${variantKey}-${index}`} className={cell.className} data-preview-kind={cell.kind} />
      ))}
    </div>
  );
}
