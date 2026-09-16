/**
 * Dedicated Store Preview-Fake media — code-owned static assets under
 * /images/preview-placeholder/. Independent from Template Catalog media.
 */

export const PREVIEW_FAKE_ASSET_ROOT = "/images/preview-placeholder" as const;
/** Alias used by focused guards. */
export const PREVIEW_FAKE_MEDIA_ROOT = PREVIEW_FAKE_ASSET_ROOT;

export const PREVIEW_FAKE_SOURCE = "PreviewFake" as const;

export type PreviewFakeSourceMarker = typeof PREVIEW_FAKE_SOURCE;

const BANNERS = [
  `${PREVIEW_FAKE_ASSET_ROOT}/banner-1.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/banner-2.svg`,
] as const;

const PRODUCTS = [
  `${PREVIEW_FAKE_ASSET_ROOT}/product-1.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/product-2.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/product-3.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/product-4.svg`,
] as const;

const CATEGORIES = [
  `${PREVIEW_FAKE_ASSET_ROOT}/category-1.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/category-2.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/category-3.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/category-4.svg`,
] as const;

const BRANDS = [
  `${PREVIEW_FAKE_ASSET_ROOT}/brand-1.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/brand-2.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/brand-3.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/brand-4.svg`,
] as const;

const ARTICLES = [
  `${PREVIEW_FAKE_ASSET_ROOT}/article-1.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/article-2.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/article-3.svg`,
] as const;

const STORIES = [
  `${PREVIEW_FAKE_ASSET_ROOT}/story-1.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/story-2.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/story-3.svg`,
] as const;

const AVATARS = [
  `${PREVIEW_FAKE_ASSET_ROOT}/avatar-1.svg`,
  `${PREVIEW_FAKE_ASSET_ROOT}/avatar-2.svg`,
] as const;

export const PREVIEW_FAKE_HERO = `${PREVIEW_FAKE_ASSET_ROOT}/hero.svg` as const;
export const PREVIEW_FAKE_PROMO = `${PREVIEW_FAKE_ASSET_ROOT}/promo.svg` as const;

export function previewFakeBannerUrl(index: number): string {
  return BANNERS[index % BANNERS.length]!;
}

export function previewFakeProductUrl(index: number): string {
  return PRODUCTS[index % PRODUCTS.length]!;
}

export function previewFakeCategoryUrl(index: number): string {
  return CATEGORIES[index % CATEGORIES.length]!;
}

export function previewFakeBrandUrl(index: number): string {
  return BRANDS[index % BRANDS.length]!;
}

export function previewFakeArticleUrl(index: number): string {
  return ARTICLES[index % ARTICLES.length]!;
}

export function previewFakeStoryUrl(index: number): string {
  return STORIES[index % STORIES.length]!;
}

export function previewFakeAvatarUrl(index: number): string {
  return AVATARS[index % AVATARS.length]!;
}

/** True when a media path/id belongs to the Preview-Fake asset family. */
export function isPreviewFakeAssetPath(path: string | null | undefined): boolean {
  return Boolean(path && path.startsWith(`${PREVIEW_FAKE_ASSET_ROOT}/`));
}

/** Flat list for boundary tests (no Template Catalog paths). */
export const PREVIEW_FAKE_ALL_PATHS = [
  ...BANNERS,
  ...PRODUCTS,
  ...CATEGORIES,
  ...BRANDS,
  ...ARTICLES,
  ...STORIES,
  ...AVATARS,
  PREVIEW_FAKE_HERO,
  PREVIEW_FAKE_PROMO,
] as const;
