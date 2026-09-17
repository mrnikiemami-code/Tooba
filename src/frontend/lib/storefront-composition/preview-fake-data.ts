/**
 * In-memory deterministic fake items for Store preview fill only.
 * Never persisted; never queried from Template Catalog; never used on published storefront.
 * Media comes exclusively from /images/preview-placeholder/ (dedicated Preview-Fake family).
 */

import {
  PREVIEW_FAKE_HERO,
  PREVIEW_FAKE_PROMO,
  PREVIEW_FAKE_SOURCE,
  previewFakeArticleUrl,
  previewFakeAvatarUrl,
  previewFakeBannerUrl,
  previewFakeBrandUrl,
  previewFakeCategoryUrl,
  previewFakeProductUrl,
  previewFakeStoryUrl,
  type PreviewFakeSourceMarker,
} from "./preview-fake-media.ts";
import {
  previewFakeArticleExcerpt,
  previewFakeArticleTitle,
  previewFakeBannerTitle,
  previewFakeBrandName,
  previewFakeCategoryName,
  previewFakeHeroSubtitle,
  previewFakeHeroTitle,
  previewFakeProductTitle,
  previewFakePromoTitle,
  previewFakeReviewAuthor,
  previewFakeReviewBody,
  previewFakeRichText,
  previewFakeStoryTitle,
} from "./preview-fake-locale.ts";
import type {
  StorefrontArticleItem,
  StorefrontBrandItem,
  StorefrontCategoryItem,
  StorefrontFeaturedReviewItem,
  StorefrontProductCard,
} from "../../app/storefront/storefront-model.ts";
import type { PublicStoryCard } from "../../app/stories/story-api.ts";

export const PREVIEW_FAKE_ID_PREFIX = "preview-fake-" as const;

export { PREVIEW_FAKE_SOURCE };
export type { PreviewFakeSourceMarker };

export function isPreviewFakeId(id: string | null | undefined): boolean {
  return Boolean(id && id.startsWith(PREVIEW_FAKE_ID_PREFIX));
}

type PreviewFakeMarked = {
  previewFake: true;
  /** In-memory only — never persisted / never shown as developer jargon in UI. */
  previewSource: PreviewFakeSourceMarker;
};

export function createFakeProduct(index: number, locale: string): StorefrontProductCard & PreviewFakeMarked {
  const id = `${PREVIEW_FAKE_ID_PREFIX}product-${index + 1}`;
  return {
    productId: id,
    slug: id,
    title: previewFakeProductTitle(index, locale),
    categoryName: previewFakeCategoryName(0, locale),
    categoryId: `${PREVIEW_FAKE_ID_PREFIX}category-1`,
    mediaAssetId: previewFakeProductUrl(index),
    primaryOfferId: "",
    sellerPartyId: "",
    sellerDisplayName: "",
    offerAmountExclusiveOfTax: 1_250_000 + index * 50_000,
    promotionalAmountExclusiveOfTax: null,
    currency: "IRR",
    availableUnits: 0,
    inStock: false,
    promotionLabel: null,
    averageRating: 4.5,
    reviewCount: 12,
    brandId: null,
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export function createFakeCategory(index: number, locale: string): StorefrontCategoryItem & PreviewFakeMarked {
  const id = `${PREVIEW_FAKE_ID_PREFIX}category-${index + 1}`;
  const image = previewFakeCategoryUrl(index);
  return {
    categoryId: id,
    parentCategoryId: null,
    name: previewFakeCategoryName(index, locale),
    imageMediaAssetId: image,
    imageUrl: image,
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export function createFakeBrand(index: number, locale: string): StorefrontBrandItem & PreviewFakeMarked {
  const id = `${PREVIEW_FAKE_ID_PREFIX}brand-${index + 1}`;
  return {
    brandId: id,
    slug: id,
    name: previewFakeBrandName(index, locale),
    productCount: 0,
    logoMediaAssetId: previewFakeBrandUrl(index),
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export function createFakeReview(index: number, locale: string): StorefrontFeaturedReviewItem & PreviewFakeMarked {
  return {
    publicId: `${PREVIEW_FAKE_ID_PREFIX}review-${index + 1}`,
    authorDisplayName: previewFakeReviewAuthor(index, locale),
    rating: 4 + (index % 2),
    title: null,
    body: previewFakeReviewBody(index, locale),
    verifiedPurchase: false,
    createdAt: "2026-01-01T00:00:00.000Z",
    productTitle: previewFakeProductTitle(index, locale),
    productSlug: `${PREVIEW_FAKE_ID_PREFIX}product-${index + 1}`,
    authorAvatarUrl: previewFakeAvatarUrl(index),
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export function createFakeArticle(index: number, locale: string): StorefrontArticleItem & PreviewFakeMarked {
  const id = `${PREVIEW_FAKE_ID_PREFIX}article-${index + 1}`;
  return {
    articleId: id,
    slug: id,
    title: previewFakeArticleTitle(index, locale),
    excerpt: previewFakeArticleExcerpt(index, locale),
    coverMediaAssetId: previewFakeArticleUrl(index),
    publishDate: "2026-01-01T00:00:00.000Z",
    authorDisplayName: previewFakeReviewAuthor(index, locale),
    tags: [],
    isFeatured: index === 0,
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export type PreviewFakeBannerItem = {
  src: string;
  href: string;
  title: string;
  objectPosition: string;
  previewFake: true;
  previewSource: PreviewFakeSourceMarker;
};

export function createFakeBanner(index: number, locale: string): PreviewFakeBannerItem {
  return {
    src: previewFakeBannerUrl(index),
    href: "/products",
    title: previewFakeBannerTitle(index, locale),
    objectPosition: "50.00% 50.00%",
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export function createFakeHeroConfig(locale: string): Record<string, unknown> {
  const title = previewFakeHeroTitle(locale);
  const subtitle = previewFakeHeroSubtitle(locale);
  return {
    title,
    subtitle,
    href: "/products",
    imageUrl: PREVIEW_FAKE_HERO,
    displayHeightPx: 420,
    slideIntervalSec: 5,
    slideCount: 1,
    slides: [
      {
        mediaAssetId: "",
        imageUrl: PREVIEW_FAKE_HERO,
        title,
        alt: title,
        seoTitle: title,
        seoDescription: subtitle,
        href: "/products",
      },
    ],
    autoplay: true,
    focalPointX: 0.5,
    focalPointY: 0.45,
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export function createFakePromoConfig(locale: string): Record<string, unknown> {
  return {
    title: previewFakePromoTitle(locale),
    href: "/offers",
    imageUrl: PREVIEW_FAKE_PROMO,
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export function createFakeRichTextConfig(locale: string): Record<string, unknown> {
  const copy = previewFakeRichText(locale);
  return {
    title: copy.title,
    text: copy.text,
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}

export function createFakeStory(index: number, locale: string): PublicStoryCard & PreviewFakeMarked {
  const id = `${PREVIEW_FAKE_ID_PREFIX}story-${index + 1}`;
  const media = previewFakeStoryUrl(index);
  return {
    storyId: id,
    title: previewFakeStoryTitle(index, locale),
    coverMediaUrl: media,
    isVideo: false,
    displayOrder: index,
    ctaType: "None",
    ctaTarget: null,
    items: [
      {
        storyItemId: `${id}-item-1`,
        mediaType: "Image",
        mediaUrl: media,
        caption: previewFakeStoryTitle(index, locale),
        durationMs: 5000,
        ctaType: "None",
        ctaTarget: null,
      },
    ],
    previewFake: true,
    previewSource: PREVIEW_FAKE_SOURCE,
  };
}
