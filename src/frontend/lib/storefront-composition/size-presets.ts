import type { DataSourceKind, DataSourceSupport, SizePreset } from "./types.ts";
import { SIZE_PRESETS } from "./types.ts";

export type SizePresetContract = {
  key: SizePreset;
  nameFa: string;
  desktop: string;
  tablet: string;
  mobile: string;
};

/** Bounded height/size presets. Mobile caps are system-owned. */
export const SIZE_PRESET_CONTRACTS: Record<SizePreset, SizePresetContract> = {
  Compact: {
    key: "Compact",
    nameFa: "جمع‌وجور",
    desktop: "min(28vh, 280px)",
    tablet: "min(26vh, 240px)",
    mobile: "min(22vh, 180px)",
  },
  Medium: {
    key: "Medium",
    nameFa: "متوسط",
    desktop: "min(42vh, 380px)",
    tablet: "min(36vh, 320px)",
    mobile: "min(28vh, 220px)",
  },
  Large: {
    key: "Large",
    nameFa: "بزرگ",
    desktop: "min(56vh, 480px)",
    tablet: "min(44vh, 400px)",
    mobile: "min(32vh, 260px)",
  },
  ExtraLarge: {
    key: "ExtraLarge",
    nameFa: "خیلی بزرگ",
    desktop: "min(68vh, 560px)",
    tablet: "min(52vh, 460px)",
    mobile: "min(36vh, 280px)",
  },
};

export function isSizePreset(value: unknown): value is SizePreset {
  return typeof value === "string" && (SIZE_PRESETS as readonly string[]).includes(value);
}

/** CSS height classes for hero/banner presets (mobile caps system-owned). */
export function heightPresetHeroClass(preset: unknown): string {
  const key = isSizePreset(preset) ? preset : "Medium";
  switch (key) {
    case "Compact":
      return "h-[140px] sm:h-[170px] md:h-[200px] lg:h-[220px]";
    case "Large":
      return "h-[220px] sm:h-[280px] md:h-[340px] lg:h-[420px]";
    case "ExtraLarge":
      return "h-[260px] sm:h-[320px] md:h-[400px] lg:h-[500px]";
    case "Medium":
    default:
      return "h-[190px] sm:h-[230px] md:h-[290px] lg:h-[350px]";
  }
}

export function heightPresetBannerClass(preset: unknown): string {
  const key = isSizePreset(preset) ? preset : "Medium";
  switch (key) {
    case "Compact":
      return "h-28 md:h-32";
    case "Large":
      return "h-48 md:h-64";
    case "ExtraLarge":
      return "h-56 md:h-72";
    case "Medium":
    default:
      return "h-40 md:h-52";
  }
}

export const DATA_SOURCE_SUPPORT: Record<DataSourceKind, DataSourceSupport> = {
  Manual: "Supported",
  Category: "Supported",
  Brand: "Supported",
  Newest: "Supported",
  LatestArticles: "Supported",
  ApprovedReviews: "Supported",
  /** Home currently sorts listing by ReviewCount — not a dedicated API. */
  MostViewed: "HeuristicHomeOnly",
  /** Home SpecialOffers ≈ PromotionLabel heuristic — not a dedicated Discounted query. */
  Discounted: "HeuristicHomeOnly",
  /** Home FeaturedProducts = listing slice — not a Featured ranking API. */
  Featured: "HeuristicHomeOnly",
  /** Landing marks BestSelling unsupported; Home best_sellers = category buckets heuristic. */
  BestSelling: "HeuristicHomeOnly",
  HotTrending: "Deferred",
};

export function isDataSourceSupportedForLanding(kind: DataSourceKind): boolean {
  return DATA_SOURCE_SUPPORT[kind] === "Supported";
}
