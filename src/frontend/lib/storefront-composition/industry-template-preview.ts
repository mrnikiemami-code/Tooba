/**
 * Batch A industry template sample/store preview — Template Catalog only for sample.
 * Shared composition engine (same Fashion query/page-builder pattern).
 */

import {
  BATCH_A_TEMPLATE_KEYS,
  INDUSTRY_STORE_ORIGIN,
  industryDemoMediaUrl,
  industryDemoOrigin,
  industryTemplateImages,
  isBatchATemplateKey,
  type BatchATemplateKey,
} from "./industry-demo-media.ts";
import {
  buildTemplateSectionPayloads,
  getIndustryTemplate,
} from "./industry-templates.ts";
import {
  loadStorefrontBrands,
  loadStorefrontCategories,
  loadStorefrontListing,
  storefrontHostOrigin,
} from "../../app/storefront/storefront-api.ts";
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
import {
  FASHION_STORE_ORIGIN,
  FashionPreviewLoadError,
  fashionPreviewErrorMessage,
  parseFashionPreviewSource,
  parseFashionStorePreviewFixture,
  type FashionPreviewSource,
  type FashionStorePreviewFixture,
} from "./fashion-demo-preview.ts";

export {
  BATCH_A_TEMPLATE_KEYS,
  INDUSTRY_STORE_ORIGIN,
  industryDemoOrigin,
  isBatchATemplateKey,
  type BatchATemplateKey,
  fashionPreviewErrorMessage,
  parseFashionPreviewSource as parseIndustryPreviewSource,
  parseFashionStorePreviewFixture as parseIndustryStorePreviewFixture,
  type FashionPreviewSource as IndustryPreviewSource,
  type FashionStorePreviewFixture as IndustryStorePreviewFixture,
};

type HostIndustryPreview = {
  origin: string;
  templateId: string;
  templateKey: string;
  templateName: string;
  page: {
    pageId: string;
    locale: string;
    slug: string;
    title: string;
    seoTitle: string | null;
    seoDescription: string | null;
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
    imageMediaAssetId: string | null;
    imageUrl: string | null;
  }>;
  products: Array<{
    productId: string;
    slug: string;
    title: string;
    categoryName: string;
    categoryId: string;
    mediaAssetId: string;
    mediaUrl: string | null;
    primaryOfferId: string;
    sellerPartyId: string;
    sellerDisplayName: string;
    offerAmountExclusiveOfTax: number;
    promotionalAmountExclusiveOfTax: number | null;
    currency: string;
    availableUnits: number;
    inStock: boolean;
    promotionLabel: string | null;
    averageRating: number;
    reviewCount: number;
    brandId: string | null;
  }>;
  brands: Array<{
    brandId: string;
    slug: string;
    name: string;
    productCount: number;
    logoMediaAssetId: string | null;
    logoUrl: string | null;
  }>;
  compositionFillers: {
    articles: Array<{
      articleId: string;
      slug: string;
      title: string;
      excerpt: string;
      coverMediaAssetId: string;
      coverMediaUrl: string | null;
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

function pick(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function asString(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

function asNumber(value: unknown, fallback = 0): number {
  return typeof value === "number" && Number.isFinite(value) ? value : fallback;
}

function asBool(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function normalizePreview(raw: Record<string, unknown>): HostIndustryPreview {
  const pageRaw = asRecord(pick(raw, "page", "Page")) ?? {};
  const sectionsRaw = (pick(pageRaw, "sections", "Sections") as unknown[]) ?? [];
  const categoriesRaw = (pick(raw, "categories", "Categories") as unknown[]) ?? [];
  const productsRaw = (pick(raw, "products", "Products") as unknown[]) ?? [];
  const brandsRaw = (pick(raw, "brands", "Brands") as unknown[]) ?? [];
  const fillersRaw = asRecord(pick(raw, "compositionFillers", "CompositionFillers")) ?? {};
  const purityRaw = asRecord(pick(raw, "purity", "Purity")) ?? {};

  return {
    origin: asString(pick(raw, "origin", "Origin")),
    templateId: asString(pick(raw, "templateId", "TemplateId")),
    templateKey: asString(pick(raw, "templateKey", "TemplateKey")),
    templateName: asString(pick(raw, "templateName", "TemplateName")),
    page: {
      pageId: asString(pick(pageRaw, "pageId", "PageId")),
      locale: asString(pick(pageRaw, "locale", "Locale"), "fa"),
      slug: asString(pick(pageRaw, "slug", "Slug")),
      title: asString(pick(pageRaw, "title", "Title")),
      seoTitle: (() => {
        const v = pick(pageRaw, "seoTitle", "SeoTitle");
        return v == null ? null : asString(v);
      })(),
      seoDescription: (() => {
        const v = pick(pageRaw, "seoDescription", "SeoDescription");
        return v == null ? null : asString(v);
      })(),
      templateKey: asString(pick(pageRaw, "templateKey", "TemplateKey")),
      sections: sectionsRaw.map((row) => {
        const s = asRecord(row) ?? {};
        return {
          pageSectionId: asString(pick(s, "pageSectionId", "PageSectionId")),
          sectionType: asString(pick(s, "sectionType", "SectionType")),
          sortOrder: asNumber(pick(s, "sortOrder", "SortOrder")),
          configurationJson: asString(pick(s, "configurationJson", "ConfigurationJson"), "{}"),
        };
      }),
    },
    categories: categoriesRaw.map((row) => {
      const c = asRecord(row) ?? {};
      return {
        categoryId: asString(pick(c, "categoryId", "CategoryId")),
        parentCategoryId: (() => {
          const v = pick(c, "parentCategoryId", "ParentCategoryId");
          return v == null || v === "" ? null : asString(v);
        })(),
        name: asString(pick(c, "name", "Name")),
        imageMediaAssetId: (() => {
          const v = pick(c, "imageMediaAssetId", "ImageMediaAssetId");
          return v == null ? null : asString(v);
        })(),
        imageUrl: (() => {
          const v = pick(c, "imageUrl", "ImageUrl");
          return v == null ? null : asString(v);
        })(),
      };
    }),
    products: productsRaw.map((row) => {
      const p = asRecord(row) ?? {};
      return {
        productId: asString(pick(p, "productId", "ProductId")),
        slug: asString(pick(p, "slug", "Slug")),
        title: asString(pick(p, "title", "Title")),
        categoryName: asString(pick(p, "categoryName", "CategoryName")),
        categoryId: asString(pick(p, "categoryId", "CategoryId")),
        mediaAssetId: asString(pick(p, "mediaAssetId", "MediaAssetId")),
        mediaUrl: (() => {
          const v = pick(p, "mediaUrl", "MediaUrl");
          return v == null ? null : asString(v);
        })(),
        primaryOfferId: asString(pick(p, "primaryOfferId", "PrimaryOfferId")),
        sellerPartyId: asString(pick(p, "sellerPartyId", "SellerPartyId")),
        sellerDisplayName: asString(pick(p, "sellerDisplayName", "SellerDisplayName")),
        offerAmountExclusiveOfTax: asNumber(pick(p, "offerAmountExclusiveOfTax", "OfferAmountExclusiveOfTax")),
        promotionalAmountExclusiveOfTax: (() => {
          const v = pick(p, "promotionalAmountExclusiveOfTax", "PromotionalAmountExclusiveOfTax");
          return v == null ? null : asNumber(v);
        })(),
        currency: asString(pick(p, "currency", "Currency"), "IRR"),
        availableUnits: asNumber(pick(p, "availableUnits", "AvailableUnits")),
        inStock: asBool(pick(p, "inStock", "InStock"), true),
        promotionLabel: (() => {
          const v = pick(p, "promotionLabel", "PromotionLabel");
          return v == null ? null : asString(v);
        })(),
        averageRating: asNumber(pick(p, "averageRating", "AverageRating")),
        reviewCount: asNumber(pick(p, "reviewCount", "ReviewCount")),
        brandId: (() => {
          const v = pick(p, "brandId", "BrandId");
          return v == null || v === "" ? null : asString(v);
        })(),
      };
    }),
    brands: brandsRaw.map((row) => {
      const b = asRecord(row) ?? {};
      return {
        brandId: asString(pick(b, "brandId", "BrandId")),
        slug: asString(pick(b, "slug", "Slug")),
        name: asString(pick(b, "name", "Name")),
        productCount: asNumber(pick(b, "productCount", "ProductCount")),
        logoMediaAssetId: (() => {
          const v = pick(b, "logoMediaAssetId", "LogoMediaAssetId");
          return v == null ? null : asString(v);
        })(),
        logoUrl: (() => {
          const v = pick(b, "logoUrl", "LogoUrl");
          return v == null ? null : asString(v);
        })(),
      };
    }),
    compositionFillers: {
      articles: ((pick(fillersRaw, "articles", "Articles") as unknown[]) ?? []).map((row) => {
        const a = asRecord(row) ?? {};
        return {
          articleId: asString(pick(a, "articleId", "ArticleId")),
          slug: asString(pick(a, "slug", "Slug")),
          title: asString(pick(a, "title", "Title")),
          excerpt: asString(pick(a, "excerpt", "Excerpt")),
          coverMediaAssetId: asString(pick(a, "coverMediaAssetId", "CoverMediaAssetId")),
          coverMediaUrl: (() => {
            const v = pick(a, "coverMediaUrl", "CoverMediaUrl");
            return v == null ? null : asString(v);
          })(),
          publishDate: asString(pick(a, "publishDate", "PublishDate")),
          authorDisplayName: asString(pick(a, "authorDisplayName", "AuthorDisplayName")),
          tags: ((pick(a, "tags", "Tags") as unknown[]) ?? []).map((t) => asString(t)),
          isFeatured: asBool(pick(a, "isFeatured", "IsFeatured")),
        };
      }),
      reviews: ((pick(fillersRaw, "reviews", "Reviews") as unknown[]) ?? []).map((row) => {
        const r = asRecord(row) ?? {};
        return {
          publicId: asString(pick(r, "publicId", "PublicId")),
          authorDisplayName: asString(pick(r, "authorDisplayName", "AuthorDisplayName")),
          rating: asNumber(pick(r, "rating", "Rating")),
          title: asString(pick(r, "title", "Title")),
          body: asString(pick(r, "body", "Body")),
          verifiedPurchase: asBool(pick(r, "verifiedPurchase", "VerifiedPurchase")),
          createdAt: asString(pick(r, "createdAt", "CreatedAt")),
          productTitle: asString(pick(r, "productTitle", "ProductTitle")),
          productSlug: asString(pick(r, "productSlug", "ProductSlug")),
        };
      }),
    },
    purity: {
      templateProductCount: asNumber(pick(purityRaw, "templateProductCount", "TemplateProductCount")),
      templateTopLevelCategoryCount: asNumber(
        pick(purityRaw, "templateTopLevelCategoryCount", "TemplateTopLevelCategoryCount"),
      ),
      templateBrandCount: asNumber(pick(purityRaw, "templateBrandCount", "TemplateBrandCount")),
      templateBannerItemCount: asNumber(pick(purityRaw, "templateBannerItemCount", "TemplateBannerItemCount")),
      operationalProductIdHits: asNumber(pick(purityRaw, "operationalProductIdHits", "OperationalProductIdHits")),
      operationalCategoryIdHits: asNumber(pick(purityRaw, "operationalCategoryIdHits", "OperationalCategoryIdHits")),
      operationalBrandIdHits: asNumber(pick(purityRaw, "operationalBrandIdHits", "OperationalBrandIdHits")),
      isPure: asBool(pick(purityRaw, "isPure", "IsPure"), true),
    },
  };
}

function mapContext(preview: HostIndustryPreview, templateKey: BatchATemplateKey): LandingRenderContext {
  const products: StorefrontProductCard[] = preview.products.map((p) => ({
    productId: p.productId,
    slug: p.slug,
    title: p.title,
    categoryName: p.categoryName,
    categoryId: p.categoryId,
    mediaAssetId: p.mediaUrl ?? industryDemoMediaUrl(templateKey, p.mediaAssetId) ?? p.mediaAssetId,
    primaryOfferId: p.primaryOfferId,
    sellerPartyId: p.sellerPartyId,
    sellerDisplayName: p.sellerDisplayName,
    offerAmountExclusiveOfTax: p.offerAmountExclusiveOfTax,
    promotionalAmountExclusiveOfTax: p.promotionalAmountExclusiveOfTax,
    currency: p.currency,
    availableUnits: p.availableUnits,
    inStock: p.inStock,
    promotionLabel: p.promotionLabel,
    averageRating: p.averageRating,
    reviewCount: p.reviewCount,
    brandId: p.brandId,
  }));

  const categories: StorefrontCategoryItem[] = preview.categories.map((c) => ({
    categoryId: c.categoryId,
    parentCategoryId: c.parentCategoryId,
    name: c.name,
    slug: c.categoryId,
    imageMediaAssetId: c.imageUrl ?? industryDemoMediaUrl(templateKey, c.imageMediaAssetId) ?? c.imageMediaAssetId,
  }));

  const brands: StorefrontBrandItem[] = preview.brands.map((b) => ({
    brandId: b.brandId,
    slug: b.slug,
    name: b.name,
    productCount: b.productCount,
    logoMediaAssetId: b.logoUrl ?? industryDemoMediaUrl(templateKey, b.logoMediaAssetId) ?? b.logoMediaAssetId,
  }));

  const articles: StorefrontArticleItem[] = preview.compositionFillers.articles.map((a) => ({
    articleId: a.articleId,
    slug: a.slug,
    title: a.title,
    excerpt: a.excerpt,
    coverMediaAssetId: a.coverMediaUrl ?? a.coverMediaAssetId,
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

function parseBannerItems(configurationJson: string): Array<Record<string, unknown>> | undefined {
  try {
    const parsed = JSON.parse(configurationJson) as Record<string, unknown>;
    const items = parsed.items;
    if (!Array.isArray(items) || items.length === 0) return undefined;
    return items as Array<Record<string, unknown>>;
  } catch {
    return undefined;
  }
}

type IndustryTemplatePageOptions = {
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

const HERO_COPY: Record<BatchATemplateKey, { title: string; subtitle: string }> = {
  "auto-parts": {
    title: "قطعات مطمئن خودرو",
    subtitle: "موتور، ترمز و مصرفی‌های استاندارد برای ویترین یدکی",
  },
  "building-materials": {
    title: "مصالح ساختمانی پروژه",
    subtitle: "سیمان، عایق، لوله و تجهیزات کارگاهی در یک ویترین",
  },
  "tools-hardware": {
    title: "ابزار و یراق حرفه‌ای",
    subtitle: "برقی، دستی، ایمنی و پیچ و مهره برای کارگاه",
  },
};

/** Shared composition page builder for Batch A industry templates. */
export function buildIndustryTemplatePage(
  templateKey: BatchATemplateKey,
  context: LandingRenderContext,
  options: IndustryTemplatePageOptions,
): StorefrontLandingPage {
  const template = getIndustryTemplate(templateKey);
  if (!template) throw new Error(`${templateKey} template missing`);
  const payloads = buildTemplateSectionPayloads(templateKey);
  const images = industryTemplateImages(templateKey);
  const isStore = options.origin === INDUSTRY_STORE_ORIGIN || options.origin === FASHION_STORE_ORIGIN;
  const rootCategoryIds = context.categories.filter((c) => !c.parentCategoryId).map((c) => c.categoryId);
  const categoryIdsForGrid = rootCategoryIds.length > 0
    ? rootCategoryIds
    : context.categories.map((c) => c.categoryId);
  const productIds = context.products.map((p) => p.productId);
  const brandIds = context.brands.map((b) => b.brandId);
  const firstProductMedia = context.products
    .map((p) => p.mediaAssetId)
    .filter((id): id is string => Boolean(id))
    .slice(0, 3);

  let bannerItems: Array<Record<string, unknown>>;
  if (options.bannerItems && options.bannerItems.length > 0) {
    bannerItems = options.bannerItems;
  } else if (isStore) {
    bannerItems = [];
  } else {
    bannerItems = firstProductMedia.length > 0
      ? firstProductMedia.map((mediaAssetId, index) => ({
          mediaAssetId,
          href: "/products",
          title: index === 0 ? "پیشنهاد نمونه" : "منتخب نمونه",
          focalPointX: 0.5,
          focalPointY: 0.42,
        }))
      : [
          { imageUrl: images[0], href: "/products", title: "کمپین نمونه", focalPointX: 0.55, focalPointY: 0.4 },
          { imageUrl: images[1], href: "/products", title: "پیشنهاد ویژه", focalPointX: 0.45, focalPointY: 0.4 },
        ];
  }

  const hasArticleSection = payloads.some((p) => p.hostType === "ArticleList");
  const sections: StorefrontLandingSection[] = payloads.map((payload, index) => {
    const config: Record<string, unknown> = { ...payload.config, demoOrigin: options.origin };
    if (payload.hostType === "Hero") {
      if (isStore) {
        Object.assign(config, {
          title: "مجموعه فروشگاه",
          subtitle: "محصولات واقعی Catalog عملیاتی",
          href: "/products",
          ...(firstProductMedia[0]
            ? { mediaAssetId: firstProductMedia[0] }
            : { previewPlaceholder: true }),
        });
        delete config.imageUrl;
      } else {
        const copy = HERO_COPY[templateKey];
        Object.assign(config, {
          title: copy.title,
          subtitle: copy.subtitle,
          href: "/products",
          imageUrl: images[0],
          ...(firstProductMedia[0] ? { mediaAssetId: firstProductMedia[0] } : {}),
          focalPointX: 0.58,
          focalPointY: 0.4,
        });
      }
    }
    if (payload.hostType === "CategoryGrid") {
      config.categoryIds = categoryIdsForGrid.slice(0, 8);
      config.title = isStore ? "دسته‌بندی فروشگاه" : `دسته‌بندی ${template.nameFa}`;
      if (isStore && categoryIdsForGrid.length === 0) config.previewPlaceholder = true;
    }
    if (payload.hostType === "ProductCollection") {
      config.source = "Manual";
      config.productIds = productIds.slice(0, 15);
      config.take = 15;
      config.title = isStore ? "منتخب فروشگاه" : `منتخب ${template.nameFa}`;
      if (isStore && productIds.length === 0) config.previewPlaceholder = true;
    }
    if (payload.hostType === "BannerShowcase") {
      config.items = isStore
        ? bannerItems
        : bannerItems.map((item, i) => ({
            ...item,
            focalPointX: item.focalPointX ?? (i === 0 ? 0.55 : 0.45),
            focalPointY: item.focalPointY ?? 0.4,
          }));
      config.title = isStore ? "بنرهای فروشگاه" : `بنرهای ${template.nameFa}`;
      if (isStore && bannerItems.length === 0) {
        config.previewPlaceholder = true;
        config.previewPlaceholderSlots = 2;
      }
    }
    if (payload.hostType === "BrandStrip") {
      config.brandIds = brandIds.slice(0, 8);
      config.source = "Manual";
      config.title = isStore ? "برندهای فروشگاه" : `برندهای ${template.nameFa}`;
      if (isStore && brandIds.length === 0) config.previewPlaceholder = true;
    }
    if (payload.hostType === "Reviews") {
      config.title = "نظر خریداران";
      if (isStore && context.reviews.length === 0) config.previewPlaceholder = true;
    }
    if (payload.hostType === "StoryRail") {
      config.title = isStore ? "استوری فروشگاه" : "استوری‌های نمایشی";
      config.take = 8;
      if (isStore) config.previewPlaceholder = true;
    }
    if (payload.hostType === "ArticleList") {
      config.title = isStore ? "مقالات فروشگاه" : `مجله ${template.nameFa}`;
      if (isStore && context.articles.length === 0) config.previewPlaceholder = true;
    }

    const items =
      payload.hostType === "ProductCollection"
        ? productIds.slice(0, 15).map((id) => ({ id, slug: id }))
        : [];

    return {
      pageSectionId: options.sectionIds?.[index] ?? `${templateKey}-section-${index + 1}`,
      sectionType: payload.hostType,
      sortOrder: index,
      config: JSON.stringify(config),
      items,
    };
  });

  if (!hasArticleSection && !isStore) {
    sections.push({
      pageSectionId: `${templateKey}-section-articles`,
      sectionType: "ArticleList",
      sortOrder: sections.length,
      config: JSON.stringify({
        title: `مجله ${template.nameFa}`,
        source: "Latest",
        take: 3,
        variantKey: "article.magazine-rail",
        demoOrigin: options.origin,
      }),
      items: [],
    });
  }

  return {
    pageId: options.pageId ?? `${templateKey}-template-preview`,
    pageType: "Landing",
    locale: options.locale ?? "fa",
    slug: options.slug ?? `${templateKey}-template-sample`,
    title: options.title ?? `پیش‌نمایش قالب ${template.nameFa}`,
    seoTitle: options.seoTitle ?? options.title ?? `پیش‌نمایش قالب ${template.nameFa}`,
    seoDescription: options.seoDescription ?? `پیش‌نمایش قالب ${template.nameFa}`,
    robotsIndex: false,
    robotsFollow: false,
    canonicalUrl: null,
    ogTitle: null,
    ogDescription: null,
    ogImageUrl: null,
    primaryH1: options.title ?? `پیش‌نمایش قالب ${template.nameFa}`,
    templateKey,
    sections,
    products: context.products,
    categories: context.categories,
    brands: context.brands,
    articles: context.articles,
    reviews: context.reviews,
  };
}

export async function loadIndustryTemplatePreview(templateKey: BatchATemplateKey): Promise<{
  kind: "sample";
  context: LandingRenderContext;
  page: StorefrontLandingPage;
  purity: HostIndustryPreview["purity"];
  origin: string;
}> {
  const host = storefrontHostOrigin();
  let response: Response;
  try {
    response = await fetch(`${host}/v1/storefront/template-catalog/${templateKey}/preview`, {
      cache: "no-store",
    });
  } catch {
    throw new FashionPreviewLoadError("sample.preview.failed");
  }
  if (!response.ok) {
    throw new FashionPreviewLoadError("sample.preview.failed");
  }
  const preview = normalizePreview((await response.json()) as Record<string, unknown>);
  const context = mapContext(preview, templateKey);
  const bannerSection = preview.page.sections.find((s) => s.sectionType === "BannerShowcase");
  const bannerItems = bannerSection ? parseBannerItems(bannerSection.configurationJson) : undefined;
  const page = buildIndustryTemplatePage(templateKey, context, {
    origin: industryDemoOrigin(templateKey),
    pageId: preview.page.pageId,
    locale: preview.page.locale,
    slug: preview.page.slug,
    title: preview.page.title,
    seoTitle: preview.page.seoTitle ?? preview.page.title,
    seoDescription: preview.page.seoDescription ?? `پیش‌نمایش قالب از Template Catalog`,
    sectionIds: preview.page.sections.map((s) => s.pageSectionId),
    bannerItems,
  });
  return {
    kind: "sample",
    context,
    page,
    purity: preview.purity,
    origin: preview.origin || industryDemoOrigin(templateKey),
  };
}

export async function loadIndustryStorePreview(
  templateKey: BatchATemplateKey,
  uiLocale: "fa" | "en" = "fa",
  options?: { fixture?: FashionStorePreviewFixture },
): Promise<{
  kind: "store";
  context: LandingRenderContext;
  page: StorefrontLandingPage;
  origin: typeof INDUSTRY_STORE_ORIGIN;
}> {
  void options;
  const [listing, categoriesRaw, brandsRaw] = await Promise.all([
    loadStorefrontListing({ sort: "newest" }),
    loadStorefrontCategories(),
    loadStorefrontBrands(),
  ]);

  const seen = new Set<string>();
  const products: StorefrontProductCard[] = [];
  for (const card of listing?.products ?? []) {
    if (!card.productId || seen.has(card.productId)) continue;
    seen.add(card.productId);
    products.push(card);
  }
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
  const template = getIndustryTemplate(templateKey)!;
  const page = buildIndustryTemplatePage(templateKey, context, {
    origin: INDUSTRY_STORE_ORIGIN,
    pageId: `${templateKey}-template-store-preview`,
    locale: uiLocale,
    slug: `${templateKey}-template-store`,
    title: uiLocale === "en"
      ? `${template.nameFa} template preview with store data`
      : `پیش‌نمایش قالب ${template.nameFa} با داده فروشگاه`,
  });
  return {
    kind: "store",
    context,
    page,
    origin: INDUSTRY_STORE_ORIGIN,
  };
}
