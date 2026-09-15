/**
 * Fashion template pilot demo pack — in-memory only.
 * Origin marker: fashion-template-preview-pilot (LOCK-SF-283).
 * Does not write Host/DB; never contaminates user-owned Catalog.
 */

import {
  buildTemplateSectionPayloads,
  getIndustryTemplate,
} from "./industry-templates.ts";
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

export const FASHION_DEMO_ORIGIN = "fashion-template-preview-pilot" as const;
export const FASHION_DEMO_CATEGORY_TREE_COUNT = 8;
export const FASHION_DEMO_PRODUCT_COUNT = 15;

const FASHION_IMAGES = [
  "https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1469334031218-e382a71b716b?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1445205170230-053b83016050?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1558769132-cb1aea458c5e?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?auto=format&fit=crop&w=800&q=80",
  "https://images.unsplash.com/photo-1509631179647-0177331693ae?auto=format&fit=crop&w=800&q=80",
] as const;

export function fashionDemoMediaUrl(assetId: string | null | undefined): string | null {
  if (!assetId?.startsWith("demo-fashion-media-")) return null;
  const n = Number(assetId.replace("demo-fashion-media-", "")) || 1;
  return FASHION_IMAGES[(n - 1) % FASHION_IMAGES.length]!;
}

const TREE_ROOTS = [
  "زنانه",
  "مردانه",
  "بچگانه",
  "کفش",
  "کیف و اکسسوری",
  "ورزشی",
  "لباس رسمی",
  "فصل جدید",
] as const;

function buildCategoryTrees(): StorefrontCategoryItem[] {
  const rows: StorefrontCategoryItem[] = [];
  TREE_ROOTS.forEach((rootName, rootIndex) => {
    const rootId = `demo-fashion-cat-root-${rootIndex + 1}`;
    rows.push({ categoryId: rootId, parentCategoryId: null, name: rootName });
    for (let mid = 1; mid <= 2; mid += 1) {
      const midId = `demo-fashion-cat-mid-${rootIndex + 1}-${mid}`;
      rows.push({
        categoryId: midId,
        parentCategoryId: rootId,
        name: `${rootName} · سطح ${(mid + 1).toLocaleString("fa-IR")}`,
      });
      for (let leaf = 1; leaf <= 2; leaf += 1) {
        rows.push({
          categoryId: `demo-fashion-cat-leaf-${rootIndex + 1}-${mid}-${leaf}`,
          parentCategoryId: midId,
          name: `${rootName} · زیر ${(mid * 2 + leaf).toLocaleString("fa-IR")}`,
        });
      }
    }
  });
  return rows;
}

const PRODUCT_TITLES = [
  "مانتو کتان بهاره",
  "شومیز ابریشمی گل‌دار",
  "شلوار جین اسلیم",
  "کت بلیزر کلاسیک",
  "پیراهن نخی مردانه",
  "هودی پنبه‌ای اورسایز",
  "دامن پلیسه میدی",
  "تی‌شرت بیسیک رنگی",
  "کفش اسنیکر شهری",
  "بوت چرمی کوتاه",
  "کیف دوشی مینیمال",
  "شال نخی تابستانی",
  "ست ورزشی سبک",
  "لباس مجلسی ساده",
  "کاپشن سبک پاییزه",
] as const;

function buildProducts(categories: StorefrontCategoryItem[]): StorefrontProductCard[] {
  const leaves = categories.filter((c) => c.categoryId.includes("-leaf-"));
  return PRODUCT_TITLES.map((title, index) => {
    const category = leaves[index % leaves.length]!;
    const n = index + 1;
    return {
      productId: `demo-fashion-prod-${n}`,
      slug: `demo-fashion-prod-${n}`,
      title,
      categoryName: category.name,
      categoryId: category.categoryId,
      mediaAssetId: `demo-fashion-media-${(n % FASHION_IMAGES.length) + 1}`,
      primaryOfferId: `demo-fashion-offer-${n}`,
      sellerPartyId: "demo-fashion-seller",
      sellerDisplayName: "نمایشگاه پوشاک آزمایشی",
      offerAmountExclusiveOfTax: 890_000 + n * 35_000,
      promotionalAmountExclusiveOfTax: n % 3 === 0 ? 790_000 + n * 20_000 : null,
      currency: "IRR",
      availableUnits: 12,
      inStock: true,
      promotionLabel: n % 3 === 0 ? "پیشنهاد ویژه" : null,
      averageRating: 4.2 + (n % 5) * 0.1,
      reviewCount: 8 + n,
      brandId: `demo-fashion-brand-${(n % 6) + 1}`,
    };
  });
}

function buildBrands(): StorefrontBrandItem[] {
  const names = ["نوآ پوشاک", "ریتم استایل", "سادهٔ شهری", "گلبرگ", "خط فرم", "پنبه خانه"];
  return names.map((name, index) => ({
    brandId: `demo-fashion-brand-${index + 1}`,
    slug: `demo-fashion-brand-${index + 1}`,
    name,
    productCount: 3 + index,
    logoMediaAssetId: `demo-fashion-media-${index + 1}`,
  }));
}

function buildArticles(): StorefrontArticleItem[] {
  return [
    {
      articleId: "demo-shared-article-1",
      slug: "demo-shared-style-guide",
      title: "راهنمای استایل فصل",
      excerpt: "چطور چند قطعه پایه را با هم ترکیب کنید.",
      coverMediaAssetId: "demo-fashion-media-2",
      publishDate: "2026-03-01T00:00:00Z",
      authorDisplayName: "تحریریه نمایشی",
      tags: ["استایل"],
      isFeatured: true,
    },
    {
      articleId: "demo-shared-article-2",
      slug: "demo-shared-fabric-care",
      title: "مراقبت از پارچه‌های ظریف",
      excerpt: "نکات ساده برای ماندگاری لباس‌های روزمره.",
      coverMediaAssetId: "demo-fashion-media-4",
      publishDate: "2026-02-12T00:00:00Z",
      authorDisplayName: "تحریریه نمایشی",
      tags: ["مراقبت"],
      isFeatured: false,
    },
    {
      articleId: "demo-shared-article-3",
      slug: "demo-shared-trends",
      title: "روندهای ملایم بهار",
      excerpt: "رنگ‌ها و برش‌هایی که در ویترین دیده می‌شوند.",
      coverMediaAssetId: "demo-fashion-media-6",
      publishDate: "2026-01-20T00:00:00Z",
      authorDisplayName: "تحریریه نمایشی",
      tags: ["ترند"],
      isFeatured: false,
    },
  ];
}

function buildReviews(): StorefrontFeaturedReviewItem[] {
  return [
    {
      publicId: "demo-shared-review-1",
      authorDisplayName: "سارا",
      rating: 5,
      title: "کیفیت خوب",
      body: "پارچه نرم بود و اندازه دقیق بود.",
      verifiedPurchase: true,
      createdAt: "2026-03-10T00:00:00Z",
      productTitle: "مانتو کتان بهاره",
      productSlug: "demo-fashion-prod-1",
    },
    {
      publicId: "demo-shared-review-2",
      authorDisplayName: "نیما",
      rating: 4,
      title: "ارسال سریع",
      body: "برای استفاده روزمره مناسب است.",
      verifiedPurchase: true,
      createdAt: "2026-03-08T00:00:00Z",
      productTitle: "شلوار جین اسلیم",
      productSlug: "demo-fashion-prod-3",
    },
    {
      publicId: "demo-shared-review-3",
      authorDisplayName: "مینا",
      rating: 5,
      title: "رنگ عالی",
      body: "با بقیه لباس‌هایم خوب ست شد.",
      verifiedPurchase: false,
      createdAt: "2026-03-05T00:00:00Z",
      productTitle: "شال نخی تابستانی",
      productSlug: "demo-fashion-prod-12",
    },
  ];
}

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

export function buildFashionDemoContext(): LandingRenderContext {
  const categories = buildCategoryTrees();
  return {
    products: buildProducts(categories),
    categories,
    brands: buildBrands(),
    articles: buildArticles(),
    reviews: buildReviews(),
    menus: {},
  };
}

export function buildFashionDemoPage(context: LandingRenderContext): StorefrontLandingPage {
  const template = getIndustryTemplate("fashion");
  if (!template) throw new Error("Fashion template missing");
  const payloads = buildTemplateSectionPayloads("fashion");
  const rootCategoryIds = context.categories
    .filter((c) => c.parentCategoryId === null)
    .map((c) => c.categoryId);
  const productIds = context.products.map((p) => p.productId);
  const brandIds = context.brands.map((b) => b.brandId);

  const sections: StorefrontLandingSection[] = payloads.map((payload, index) => {
    const config = { ...payload.config, demoOrigin: FASHION_DEMO_ORIGIN };
    if (payload.hostType === "Hero") {
      Object.assign(config, {
        title: "مجموعه بهاره پوشاک",
        subtitle: "استایل روزمره با قطعات ساده و قابل ترکیب",
        href: "/products",
        imageUrl: FASHION_IMAGES[0],
      });
    }
    if (payload.hostType === "CategoryGrid") {
      config.categoryIds = rootCategoryIds;
      config.title = "دسته‌بندی پوشاک";
    }
    if (payload.hostType === "ProductCollection") {
      config.source = "Manual";
      config.productIds = productIds;
      config.take = 15;
      config.title = "منتخب پوشاک";
    }
    if (payload.hostType === "BannerShowcase") {
      config.items = [
        {
          imageUrl: FASHION_IMAGES[1],
          href: "/products",
          title: "کمپین فصل جدید",
        },
        {
          imageUrl: FASHION_IMAGES[2],
          href: "/products",
          title: "تخفیف اکسسوری",
        },
      ];
      config.title = "بنرهای پوشاک";
    }
    if (payload.hostType === "BrandStrip") {
      config.brandIds = brandIds;
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
        ? productIds.map((id) => ({ id, slug: id }))
        : [];

    return {
      pageSectionId: `demo-fashion-section-${index + 1}`,
      sectionType: payload.hostType,
      sortOrder: index,
      config: JSON.stringify(config),
      items,
    };
  });

  // Pilot adds magazine articles for Fashion visual completeness (task §3).
  sections.push({
    pageSectionId: "demo-fashion-section-articles",
    sectionType: "ArticleList",
    sortOrder: sections.length,
    config: JSON.stringify({
      title: "مجله پوشاک",
      source: "Latest",
      take: 3,
      variantKey: "article.magazine-rail",
      demoOrigin: FASHION_DEMO_ORIGIN,
    }),
    items: [],
  });

  return {
    pageId: "demo-fashion-preview-page",
    locale: "fa",
    slug: "template-preview-fashion",
    title: "پیش‌نمایش قالب پوشاک",
    seoTitle: "پیش‌نمایش قالب پوشاک",
    seoDescription: "پیش‌نمایش آزمایشی قالب پوشاک — داده نمونه ایزوله",
    templateKey: "fashion",
    sections,
  };
}

export function fashionDemoSeedSummaryFa(): string[] {
  return [
    "۸ درخت دسته‌بندی سه‌سطحی",
    "۱۵ محصول پوشاک",
    "تصاویر محصول مرتبط",
    "بنرهای پوشاک",
    "برندهای مرتبط",
    "استوری عمومی نمایشی",
    "نظرات عمومی نمایشی",
    "مقالات عمومی نمایشی",
  ];
}
