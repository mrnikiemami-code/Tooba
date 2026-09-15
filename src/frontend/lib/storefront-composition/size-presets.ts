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
