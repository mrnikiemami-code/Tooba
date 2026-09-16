/**
 * In-memory deterministic fake items for Store preview fill only.
 * Never persisted; never queried from Template Catalog; never used on published storefront.
 */

import { FASHION_IMAGES } from "./fashion-demo-media.ts";
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

export function isPreviewFakeId(id: string | null | undefined): boolean {
  return Boolean(id && id.startsWith(PREVIEW_FAKE_ID_PREFIX));
}

function demoImage(index: number): string {
  return FASHION_IMAGES[index % FASHION_IMAGES.length]!;
}

export function createFakeProduct(index: number, locale: string): StorefrontProductCard {
  const id = `${PREVIEW_FAKE_ID_PREFIX}product-${index + 1}`;
  return {
    productId: id,
    slug: id,
    title: previewFakeProductTitle(index, locale),
    categoryName: previewFakeCategoryName(0, locale),
    categoryId: `${PREVIEW_FAKE_ID_PREFIX}category-1`,
    mediaAssetId: demoImage(index),
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
  };
}

export function createFakeCategory(index: number, locale: string): StorefrontCategoryItem {
  const id = `${PREVIEW_FAKE_ID_PREFIX}category-${index + 1}`;
  return {
    categoryId: id,
    parentCategoryId: null,
    name: previewFakeCategoryName(index, locale),
    imageMediaAssetId: demoImage(index + 2),
    imageUrl: demoImage(index + 2),
    previewFake: true,
  };
}

export function createFakeBrand(index: number, locale: string): StorefrontBrandItem {
  const id = `${PREVIEW_FAKE_ID_PREFIX}brand-${index + 1}`;
  return {
    brandId: id,
    slug: id,
    name: previewFakeBrandName(index, locale),
    productCount: 0,
    logoMediaAssetId: demoImage(index + 3),
    previewFake: true,
  };
}

export function createFakeReview(index: number, locale: string): StorefrontFeaturedReviewItem {
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
    previewFake: true,
  };
}

export function createFakeArticle(index: number, locale: string): StorefrontArticleItem {
  const id = `${PREVIEW_FAKE_ID_PREFIX}article-${index + 1}`;
  return {
    articleId: id,
    slug: id,
    title: previewFakeArticleTitle(index, locale),
    excerpt: previewFakeArticleExcerpt(index, locale),
    coverMediaAssetId: demoImage(index + 1),
    publishDate: "2026-01-01T00:00:00.000Z",
    authorDisplayName: previewFakeReviewAuthor(index, locale),
    tags: [],
    isFeatured: index === 0,
    previewFake: true,
  };
}

export type PreviewFakeBannerItem = {
  src: string;
  href: string;
  title: string;
  objectPosition: string;
  previewFake: true;
};

export function createFakeBanner(index: number, locale: string): PreviewFakeBannerItem {
  return {
    src: demoImage(index + 1),
    href: "/products",
    title: previewFakeBannerTitle(index, locale),
    objectPosition: "50.00% 50.00%",
    previewFake: true,
  };
}

export function createFakeHeroConfig(locale: string): Record<string, unknown> {
  return {
    title: previewFakeHeroTitle(locale),
    subtitle: previewFakeHeroSubtitle(locale),
    href: "/products",
    imageUrl: demoImage(0),
    focalPointX: 0.5,
    focalPointY: 0.45,
    previewFake: true,
  };
}

export function createFakePromoConfig(locale: string): Record<string, unknown> {
  return {
    title: previewFakePromoTitle(locale),
    href: "/offers",
    imageUrl: demoImage(4),
    previewFake: true,
  };
}

export function createFakeRichTextConfig(locale: string): Record<string, unknown> {
  const copy = previewFakeRichText(locale);
  return {
    title: copy.title,
    text: copy.text,
    previewFake: true,
  };
}

export function createFakeStory(index: number, locale: string): PublicStoryCard {
  const id = `${PREVIEW_FAKE_ID_PREFIX}story-${index + 1}`;
  const media = demoImage(index);
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
  };
}
