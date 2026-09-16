/**
 * Fashion template sample preview — Template Catalog (persisted) is the single source of truth.
 * Origin: fashion-template-catalog-persisted (LOCK-SF-291).
 */

import {
  FASHION_IMAGES,
  fashionDemoMediaUrl,
} from "./fashion-demo-media.ts";
import {
  buildTemplateSectionPayloads,
  getIndustryTemplate,
} from "./industry-templates.ts";
import { loadStorefrontBrands, loadStorefrontCategories, loadStorefrontListing, storefrontHostOrigin } from "../../app/storefront/storefront-api.ts";
import type {
  LandingRenderContext,
  StorefrontLandingPage,
  StorefrontLandingSection,
} from "../../app/storefront/storefront-landing-api.ts";
import type {
  StorefrontArticleItem,
  StorefrontBrandItem,
  StorefrontCategoryItem,
  StorefrontFeaturedReviewItem,
  StorefrontProductCard,
} from "../../app/storefront/storefront-model.ts";

export const FASHION_DEMO_ORIGIN = "fashion-template-catalog-persisted" as const;
export const FASHION_STORE_ORIGIN = "operational-store-catalog" as const;
export type FashionPreviewSource = "sample" | "store";
export const FASHION_DEMO_CATEGORY_TREE_COUNT = 8;
export const FASHION_DEMO_PRODUCT_COUNT = 15;

export type FashionPreviewErrorCode =
  | "store.data.unavailable"
  | "sample.preview.failed"
  | "preview.load.failed";

const FASHION_PREVIEW_ERRORS: Record<FashionPreviewErrorCode, { fa: string; en: string }> = {
  "store.data.unavailable": {
    fa: "دادهٔ فروشگاه در دسترس نیست. اتصال Host یا Catalog عملیاتی را بررسی کنید.",
    en: "Store catalog data is unavailable. Check the Host connection or operational Catalog.",
  },
  "sample.preview.failed": {
    fa: "بارگذاری پیش‌نمایش Template Catalog ناموفق بود.",
    en: "Fashion Template Catalog preview failed to load.",
  },
  "preview.load.failed": {
    fa: "بارگذاری پیش‌نمایش ناموفق بود.",
    en: "Preview failed to load.",
  },
};

export class FashionPreviewLoadError extends Error {
  readonly code: FashionPreviewErrorCode;

  constructor(code: FashionPreviewErrorCode) {
    super(code);
    this.name = "FashionPreviewLoadError";
    this.code = code;
  }
}

/** Localized user-facing message for Fashion template preview failures. */
export function fashionPreviewErrorMessage(
  raw: string | null | undefined,
  locale: "fa" | "en" = "fa",
): string {
  const code = (raw ?? "").trim() as FashionPreviewErrorCode;
  const row = FASHION_PREVIEW_ERRORS[code];
  if (row) return row[locale];
  if (raw && !/^[a-z0-9]+(?:\.[a-z0-9_-]+)+$/i.test(raw) && !/unavailable|failed/i.test(raw)) {
    return raw;
  }
  return FASHION_PREVIEW_ERRORS["preview.load.failed"][locale];
}

export function parseFashionPreviewSource(value: string | null | undefined): FashionPreviewSource {
  return value === "store" ? "store" : "sample";
}

export { fashionDemoMediaUrl };

export const FASHION_SECTION_LABELS_FA = [
  "هیرو",
  "استوری",
  "دسته‌بندی",
  "محصولات",
  "بنر کمپین",
  "برندها",
  "نظرات",
  "مقالات",
] as const;

type HostFashionPreview = {
  origin: string;
  templateId: string;
  templateKey: string;
  templateName: string;
  page: {
    pageId: string;
    locale: string;
    slug: string;
    title: string;
    seoTitle?: string | null;
    seoDescription?: string | null;
    templateKey: string;
    sections: Array<{
      pageSectionId: string;
      sectionType: string;
      sortOrder: number;
      configurationJson: string;
    }>;
  };
  categories: Array<{
    categoryId: string;
    parentCategoryId: string | null;
    name: string;
    imageMediaAssetId?: string | null;
    imageUrl?: string | null;
  }>;
  products: Array<{
    productId: string;
    slug: string;
    title: string;
    categoryName: string;
    categoryId: string;
    mediaAssetId: string;
    mediaUrl?: string | null;
    primaryOfferId: string;
    sellerPartyId: string;
    sellerDisplayName: string;
    offerAmountExclusiveOfTax: number;
    promotionalAmountExclusiveOfTax?: number | null;
    currency: string;
    availableUnits: number;
    inStock: boolean;
    promotionLabel?: string | null;
    averageRating: number;
    reviewCount: number;
    brandId?: string | null;
  }>;
  brands: Array<{
    brandId: string;
    slug: string;
    name: string;
    productCount: number;
    logoMediaAssetId?: string | null;
    logoUrl?: string | null;
  }>;
  compositionFillers: {
    articles: Array<{
      articleId: string;
      slug: string;
      title: string;
      excerpt: string;
      coverMediaAssetId: string;
      coverMediaUrl?: string | null;
      publishDate: string;
      authorDisplayName: string;
      tags: string[];
      isFeatured: boolean;
    }>;
    reviews: Array<{
      publicId: string;
      authorDisplayName: string;
      rating: number;
      title: string;
      body: string;
      verifiedPurchase: boolean;
      createdAt: string;
      productTitle: string;
      productSlug: string;
    }>;
  };
  purity: {
    templateProductCount: number;
    templateTopLevelCategoryCount: number;
    templateBrandCount: number;
    templateBannerItemCount: number;
    operationalProductIdHits: number;
    operationalCategoryIdHits: number;
    operationalBrandIdHits: number;
    isPure: boolean;
  };
};

function mapContext(preview: HostFashionPreview): LandingRenderContext {
  const categories: StorefrontCategoryItem[] = preview.categories.map((c) => ({
    categoryId: c.categoryId,
    parentCategoryId: c.parentCategoryId,
    name: c.name,
    imageMediaAssetId: c.imageMediaAssetId ?? null,
    imageUrl: c.imageUrl ?? null,
  }));
  const products: StorefrontProductCard[] = preview.products.map((p) => ({
    productId: p.productId,
    slug: p.slug,
    title: p.title,
    categoryName: p.categoryName,
    categoryId: p.categoryId,
    mediaAssetId: p.mediaAssetId,
    primaryOfferId: p.primaryOfferId,
    sellerPartyId: p.sellerPartyId,
    sellerDisplayName: p.sellerDisplayName,
    offerAmountExclusiveOfTax: p.offerAmountExclusiveOfTax,
    promotionalAmountExclusiveOfTax: p.promotionalAmountExclusiveOfTax ?? null,
    currency: p.currency,
    availableUnits: p.availableUnits,
    inStock: p.inStock,
    promotionLabel: p.promotionLabel ?? null,
    averageRating: p.averageRating,
    reviewCount: p.reviewCount,
    brandId: p.brandId ?? undefined,
  }));
  const brands: StorefrontBrandItem[] = preview.brands.map((b) => ({
    brandId: b.brandId,
    slug: b.slug,
    name: b.name,
    productCount: b.productCount,
    logoMediaAssetId: b.logoMediaAssetId ?? undefined,
  }));
  const articles: StorefrontArticleItem[] = preview.compositionFillers.articles.map((a) => ({
    articleId: a.articleId,
    slug: a.slug,
    title: a.title,
    excerpt: a.excerpt,
    coverMediaAssetId: a.coverMediaAssetId,
    publishDate: a.publishDate,
    authorDisplayName: a.authorDisplayName,
    tags: a.tags,
    isFeatured: a.isFeatured,
  }));
  const reviews: StorefrontFeaturedReviewItem[] = preview.compositionFillers.reviews.map((r) => ({
    publicId: r.publicId,
    authorDisplayName: r.authorDisplayName,
    rating: r.rating,
    title: r.title,
    body: r.body,
    verifiedPurchase: r.verifiedPurchase,
    createdAt: r.createdAt,
    productTitle: r.productTitle,
    productSlug: r.productSlug,
  }));
  return { products, categories, brands, articles, reviews, menus: {} };
}

function parseBannerItems(configurationJson: string): Array<Record<string, unknown>> {
  try {
    const parsed = JSON.parse(configurationJson) as { items?: Array<Record<string, unknown>> };
    return Array.isArray(parsed.items) ? parsed.items : [];
  } catch {
    return [];
  }
}

export function buildFashionDemoPageFromPreview(
  preview: HostFashionPreview,
  context: LandingRenderContext,
): StorefrontLandingPage {
  const bannerSection = preview.page.sections.find((s) => s.sectionType === "BannerShowcase");
  const bannerItems = bannerSection
    ? parseBannerItems(bannerSection.configurationJson)
    : undefined;
  return buildFashionTemplatePage(context, {
    origin: FASHION_DEMO_ORIGIN,
    pageId: preview.page.pageId,
    locale: preview.page.locale,
    slug: preview.page.slug,
    title: preview.page.title,
    seoTitle: preview.page.seoTitle ?? preview.page.title,
    seoDescription: preview.page.seoDescription ?? "پیش‌نمایش قالب پوشاک از Template Catalog",
    sectionIds: preview.page.sections.map((s) => s.pageSectionId),
    bannerItems,
  });
}

type FashionTemplatePageOptions = {
  origin: string;
  pageId?: string;
  locale?: string;
  slug?: string;
  title?: string;
  seoTitle?: string;
  seoDescription?: string;
  sectionIds?: string[];
  bannerItems?: Array<Record<string, unknown>>;
};

/** Same Fashion industry template composition; data IDs come from `context`. */
export function buildFashionTemplatePage(
  context: LandingRenderContext,
  options: FashionTemplatePageOptions,
): StorefrontLandingPage {
  const template = getIndustryTemplate("fashion");
  if (!template) throw new Error("Fashion template missing");
  const payloads = buildTemplateSectionPayloads("fashion");
  const rootCategoryIds = context.categories
    .filter((c) => !c.parentCategoryId)
    .map((c) => c.categoryId);
  const categoryIdsForGrid = rootCategoryIds.length > 0
    ? rootCategoryIds
    : context.categories.map((c) => c.categoryId);
  const productIds = context.products.map((p) => p.productId);
  const brandIds = context.brands.map((b) => b.brandId);
  const heroImage =
    context.products.find((p) => p.mediaAssetId)?.mediaAssetId
      ? undefined
      : FASHION_IMAGES[0];
  const firstProductMedia = context.products
    .map((p) => p.mediaAssetId)
    .filter(Boolean)
    .slice(0, 2);
  const bannerItems =
    options.bannerItems
    ?? (firstProductMedia.length > 0
      ? firstProductMedia.map((mediaAssetId, index) => ({
          mediaAssetId,
          href: "/products",
          title: index === 0 ? "پیشنهاد فروشگاه" : "منتخب فروشگاه",
        }))
      : [
          { imageUrl: FASHION_IMAGES[1], href: "/products", title: "کمپین فصل جدید" },
          { imageUrl: FASHION_IMAGES[2], href: "/products", title: "تخفیف اکسسوری" },
        ]);

  const sections: StorefrontLandingSection[] = payloads.map((payload, index) => {
    const config = { ...payload.config, demoOrigin: options.origin };
    if (payload.hostType === "Hero") {
      Object.assign(config, {
        title: "مجموعه بهاره پوشاک",
        subtitle: "استایل روزمره با قطعات ساده و قابل ترکیب",
        href: "/products",
        imageUrl: heroImage ?? FASHION_IMAGES[0],
        ...(firstProductMedia[0] ? { mediaAssetId: firstProductMedia[0] } : {}),
      });
    }
    if (payload.hostType === "CategoryGrid") {
      config.categoryIds = categoryIdsForGrid.slice(0, 8);
      config.title = "دسته‌بندی پوشاک";
    }
    if (payload.hostType === "ProductCollection") {
      config.source = "Manual";
      config.productIds = productIds.slice(0, 15);
      config.take = 15;
      config.title = "منتخب پوشاک";
    }
    if (payload.hostType === "BannerShowcase") {
      config.items = bannerItems;
      config.title = "بنرهای پوشاک";
    }
    if (payload.hostType === "BrandStrip") {
      config.brandIds = brandIds.slice(0, 8);
      config.source = "Manual";
      config.title = "برندهای نمایشی پوشاک";
    }
    if (payload.hostType === "Reviews") {
      config.title = "نظر خریداران";
    }
    if (payload.hostType === "StoryRail") {
      config.title = "استوری‌های نمایشی";
      config.take = 8;
    }

    const items =
      payload.hostType === "ProductCollection"
        ? productIds.slice(0, 15).map((id) => ({ id, slug: id }))
        : [];

    return {
      pageSectionId: options.sectionIds?.[index] ?? `fashion-section-${index + 1}`,
      sectionType: payload.hostType,
      sortOrder: index,
      config: JSON.stringify(config),
      items,
    };
  });

  sections.push({
    pageSectionId: "fashion-section-articles",
    sectionType: "ArticleList",
    sortOrder: sections.length,
    config: JSON.stringify({
      title: "مجله پوشاک",
      source: "Latest",
      take: 3,
      variantKey: "article.magazine-rail",
      demoOrigin: options.origin,
    }),
    items: [],
  });

  return {
    pageId: options.pageId ?? "fashion-template-preview",
    locale: options.locale ?? "fa",
    slug: options.slug ?? "fashion-template-sample",
    title: options.title ?? "پیش‌نمایش قالب پوشاک",
    seoTitle: options.seoTitle ?? options.title ?? "پیش‌نمایش قالب پوشاک",
    seoDescription: options.seoDescription ?? "پیش‌نمایش قالب پوشاک",
    templateKey: "fashion",
    sections,
  };
}

function pick(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function normalizePreview(raw: Record<string, unknown>): HostFashionPreview {
  const pageRaw = (pick(raw, "page", "Page") ?? {}) as Record<string, unknown>;
  const sectionsRaw = (pick(pageRaw, "sections", "Sections") as unknown[]) ?? [];
  const categoriesRaw = (pick(raw, "categories", "Categories") as unknown[]) ?? [];
  const productsRaw = (pick(raw, "products", "Products") as unknown[]) ?? [];
  const brandsRaw = (pick(raw, "brands", "Brands") as unknown[]) ?? [];
  const fillersRaw = (pick(raw, "compositionFillers", "CompositionFillers") ?? {}) as Record<string, unknown>;
  const purityRaw = (pick(raw, "purity", "Purity") ?? {}) as Record<string, unknown>;

  return {
    origin: String(pick(raw, "origin", "Origin") ?? FASHION_DEMO_ORIGIN),
    templateId: String(pick(raw, "templateId", "TemplateId") ?? ""),
    templateKey: String(pick(raw, "templateKey", "TemplateKey") ?? "fashion"),
    templateName: String(pick(raw, "templateName", "TemplateName") ?? "پوشاک"),
    page: {
      pageId: String(pick(pageRaw, "pageId", "PageId") ?? ""),
      locale: String(pick(pageRaw, "locale", "Locale") ?? "fa"),
      slug: String(pick(pageRaw, "slug", "Slug") ?? "fashion-template-sample"),
      title: String(pick(pageRaw, "title", "Title") ?? "پیش‌نمایش قالب پوشاک"),
      seoTitle: (pick(pageRaw, "seoTitle", "SeoTitle") as string | null | undefined) ?? null,
      seoDescription: (pick(pageRaw, "seoDescription", "SeoDescription") as string | null | undefined) ?? null,
      templateKey: String(pick(pageRaw, "templateKey", "TemplateKey") ?? "fashion"),
      sections: sectionsRaw.map((s) => {
        const row = s as Record<string, unknown>;
        return {
          pageSectionId: String(pick(row, "pageSectionId", "PageSectionId") ?? ""),
          sectionType: String(pick(row, "sectionType", "SectionType") ?? ""),
          sortOrder: Number(pick(row, "sortOrder", "SortOrder") ?? 0),
          configurationJson: String(pick(row, "configurationJson", "ConfigurationJson") ?? "{}"),
        };
      }),
    },
    categories: categoriesRaw.map((c) => {
      const row = c as Record<string, unknown>;
      return {
        categoryId: String(pick(row, "categoryId", "CategoryId") ?? ""),
        parentCategoryId: (pick(row, "parentCategoryId", "ParentCategoryId") as string | null) ?? null,
        name: String(pick(row, "name", "Name") ?? ""),
        imageMediaAssetId: (pick(row, "imageMediaAssetId", "ImageMediaAssetId") as string | null) ?? null,
        imageUrl: (pick(row, "imageUrl", "ImageUrl") as string | null) ?? null,
      };
    }),
    products: productsRaw.map((p) => {
      const row = p as Record<string, unknown>;
      return {
        productId: String(pick(row, "productId", "ProductId") ?? ""),
        slug: String(pick(row, "slug", "Slug") ?? ""),
        title: String(pick(row, "title", "Title") ?? ""),
        categoryName: String(pick(row, "categoryName", "CategoryName") ?? ""),
        categoryId: String(pick(row, "categoryId", "CategoryId") ?? ""),
        mediaAssetId: String(pick(row, "mediaAssetId", "MediaAssetId") ?? ""),
        mediaUrl: (pick(row, "mediaUrl", "MediaUrl") as string | null | undefined) ?? null,
        primaryOfferId: String(pick(row, "primaryOfferId", "PrimaryOfferId") ?? ""),
        sellerPartyId: String(pick(row, "sellerPartyId", "SellerPartyId") ?? ""),
        sellerDisplayName: String(pick(row, "sellerDisplayName", "SellerDisplayName") ?? ""),
        offerAmountExclusiveOfTax: Number(pick(row, "offerAmountExclusiveOfTax", "OfferAmountExclusiveOfTax") ?? 0),
        promotionalAmountExclusiveOfTax:
          (pick(row, "promotionalAmountExclusiveOfTax", "PromotionalAmountExclusiveOfTax") as number | null) ?? null,
        currency: String(pick(row, "currency", "Currency") ?? "IRR"),
        availableUnits: Number(pick(row, "availableUnits", "AvailableUnits") ?? 0),
        inStock: Boolean(pick(row, "inStock", "InStock") ?? true),
        promotionLabel: (pick(row, "promotionLabel", "PromotionLabel") as string | null) ?? null,
        averageRating: Number(pick(row, "averageRating", "AverageRating") ?? 0),
        reviewCount: Number(pick(row, "reviewCount", "ReviewCount") ?? 0),
        brandId: (pick(row, "brandId", "BrandId") as string | null) ?? null,
      };
    }),
    brands: brandsRaw.map((b) => {
      const row = b as Record<string, unknown>;
      return {
        brandId: String(pick(row, "brandId", "BrandId") ?? ""),
        slug: String(pick(row, "slug", "Slug") ?? ""),
        name: String(pick(row, "name", "Name") ?? ""),
        productCount: Number(pick(row, "productCount", "ProductCount") ?? 0),
        logoMediaAssetId: (pick(row, "logoMediaAssetId", "LogoMediaAssetId") as string | null) ?? null,
        logoUrl: (pick(row, "logoUrl", "LogoUrl") as string | null) ?? null,
      };
    }),
    compositionFillers: {
      articles: ((pick(fillersRaw, "articles", "Articles") as unknown[]) ?? []).map((a) => {
        const row = a as Record<string, unknown>;
        return {
          articleId: String(pick(row, "articleId", "ArticleId") ?? ""),
          slug: String(pick(row, "slug", "Slug") ?? ""),
          title: String(pick(row, "title", "Title") ?? ""),
          excerpt: String(pick(row, "excerpt", "Excerpt") ?? ""),
          coverMediaAssetId: String(pick(row, "coverMediaAssetId", "CoverMediaAssetId") ?? ""),
          coverMediaUrl: (pick(row, "coverMediaUrl", "CoverMediaUrl") as string | null) ?? null,
          publishDate: String(pick(row, "publishDate", "PublishDate") ?? ""),
          authorDisplayName: String(pick(row, "authorDisplayName", "AuthorDisplayName") ?? ""),
          tags: ((pick(row, "tags", "Tags") as string[]) ?? []),
          isFeatured: Boolean(pick(row, "isFeatured", "IsFeatured") ?? false),
        };
      }),
      reviews: ((pick(fillersRaw, "reviews", "Reviews") as unknown[]) ?? []).map((r) => {
        const row = r as Record<string, unknown>;
        return {
          publicId: String(pick(row, "publicId", "PublicId") ?? ""),
          authorDisplayName: String(pick(row, "authorDisplayName", "AuthorDisplayName") ?? ""),
          rating: Number(pick(row, "rating", "Rating") ?? 0),
          title: String(pick(row, "title", "Title") ?? ""),
          body: String(pick(row, "body", "Body") ?? ""),
          verifiedPurchase: Boolean(pick(row, "verifiedPurchase", "VerifiedPurchase") ?? false),
          createdAt: String(pick(row, "createdAt", "CreatedAt") ?? ""),
          productTitle: String(pick(row, "productTitle", "ProductTitle") ?? ""),
          productSlug: String(pick(row, "productSlug", "ProductSlug") ?? ""),
        };
      }),
    },
    purity: {
      templateProductCount: Number(pick(purityRaw, "templateProductCount", "TemplateProductCount") ?? 0),
      templateTopLevelCategoryCount: Number(
        pick(purityRaw, "templateTopLevelCategoryCount", "TemplateTopLevelCategoryCount") ?? 0,
      ),
      templateBrandCount: Number(pick(purityRaw, "templateBrandCount", "TemplateBrandCount") ?? 0),
      templateBannerItemCount: Number(pick(purityRaw, "templateBannerItemCount", "TemplateBannerItemCount") ?? 0),
      operationalProductIdHits: Number(pick(purityRaw, "operationalProductIdHits", "OperationalProductIdHits") ?? 0),
      operationalCategoryIdHits: Number(pick(purityRaw, "operationalCategoryIdHits", "OperationalCategoryIdHits") ?? 0),
      operationalBrandIdHits: Number(pick(purityRaw, "operationalBrandIdHits", "OperationalBrandIdHits") ?? 0),
      isPure: Boolean(pick(purityRaw, "isPure", "IsPure") ?? false),
    },
  };
}

export async function loadFashionTemplatePreview(): Promise<{
  kind: "sample";
  context: LandingRenderContext;
  page: StorefrontLandingPage;
  purity: HostFashionPreview["purity"];
  origin: string;
}> {
  const host = storefrontHostOrigin();
  let response: Response;
  try {
    response = await fetch(`${host}/v1/storefront/template-catalog/fashion/preview`, {
      cache: "no-store",
    });
  } catch {
    throw new FashionPreviewLoadError("sample.preview.failed");
  }
  if (!response.ok) {
    throw new FashionPreviewLoadError("sample.preview.failed");
  }
  const preview = normalizePreview((await response.json()) as Record<string, unknown>);
  const context = mapContext(preview);
  const page = buildFashionDemoPageFromPreview(preview, context);
  return {
    kind: "sample",
    context,
    page,
    purity: preview.purity,
    origin: preview.origin || FASHION_DEMO_ORIGIN,
  };
}

/** Operational store Catalog data into the same Fashion template composition.
 * Uses light endpoints (/products, /categories, /brands) — avoids heavy /home which can 500 under load.
 */
export async function loadFashionStorePreview(uiLocale: "fa" | "en" = "fa"): Promise<{
  kind: "store";
  context: LandingRenderContext;
  page: StorefrontLandingPage;
  origin: typeof FASHION_STORE_ORIGIN;
}> {
  const [listing, categoriesRaw, brandsRaw] = await Promise.all([
    loadStorefrontListing({ sort: "newest" }),
    loadStorefrontCategories(),
    loadStorefrontBrands(),
  ]);

  const products = uniqueStoreProducts(listing?.products ?? []);
  const categories =
    (categoriesRaw && categoriesRaw.length > 0 ? categoriesRaw : null)
    ?? listing?.categories
    ?? [];
  const brands = brandsRaw ?? [];
  const hasStoreData = products.length > 0 || categories.length > 0 || brands.length > 0;
  if (!hasStoreData) {
    throw new FashionPreviewLoadError("store.data.unavailable");
  }

  const context: LandingRenderContext = {
    products,
    categories,
    brands,
    articles: [],
    reviews: [],
    menus: {},
  };
  const page = buildFashionTemplatePage(context, {
    origin: FASHION_STORE_ORIGIN,
    pageId: "fashion-template-store-preview",
    locale: uiLocale,
    slug: "fashion-template-store",
    title: uiLocale === "en"
      ? "Fashion template preview with store data"
      : "پیش‌نمایش قالب پوشاک با داده فروشگاه",
    seoTitle: uiLocale === "en"
      ? "Fashion template preview with store data"
      : "پیش‌نمایش قالب پوشاک با داده فروشگاه",
    seoDescription: uiLocale === "en"
      ? "Same Fashion template composition filled from the operational store Catalog"
      : "همان قالب پوشاک با داده Catalog عملیاتی فروشگاه",
  });
  return {
    kind: "store",
    context,
    page,
    origin: FASHION_STORE_ORIGIN,
  };
}

function uniqueStoreProducts(cards: StorefrontProductCard[]): StorefrontProductCard[] {
  const seen = new Set<string>();
  const rows: StorefrontProductCard[] = [];
  for (const card of cards) {
    if (!card.productId || seen.has(card.productId)) continue;
    seen.add(card.productId);
    rows.push(card);
  }
  return rows;
}

/** @deprecated R4 in-memory builders removed; use loadFashionTemplatePreview. */
export function buildFashionDemoContext(): LandingRenderContext {
  throw new Error("In-memory Fashion demo removed; use loadFashionTemplatePreview().");
}

/** @deprecated R4 in-memory builders removed; use loadFashionTemplatePreview. */
export function buildFashionDemoPage(_context: LandingRenderContext): StorefrontLandingPage {
  throw new Error("In-memory Fashion demo removed; use loadFashionTemplatePreview().");
}

export function fashionDemoSeedSummaryFa(): string[] {
  return [
    "۸ درخت دسته‌بندی سه‌سطحی (Template Catalog)",
    "۱۵ محصول پوشاک پایدار",
    "تصاویر محلی fashion-template",
    "بنرهای BannerShowcase پایدار",
    "برندهای مرتبط قالب",
    "استوری عمومی نمایشی",
    "نظرات/مقالات filler ترکیب",
    "بدون مخلوط Catalog عملیاتی",
  ];
}
