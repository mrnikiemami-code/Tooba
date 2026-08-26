import type {
  StorefrontAlternateOffer,
  StorefrontBrandPage,
  StorefrontBrandItem,
  StorefrontBestSellerColumn,
  StorefrontCategoryItem,
  StorefrontHomePage,
  StorefrontListingPage,
  StorefrontListingRequest,
  StorefrontListingSort,
  StorefrontOfferCandidate,
  StorefrontMerchandisingPage,
  StorefrontProductCard,
  StorefrontProductDetailPage,
  StorefrontProductSpecification,
  StorefrontProductVariant,
  StorefrontPublicSellerItem,
  StorefrontPublicSellerPage,
  StorefrontPublicReview,
  StorefrontReviewsPage,
  StorefrontReviewSubmission,
} from "./storefront-model.ts";

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function asString(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function asBoolean(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

/**
 * مبدأ Host برای خواندن RSC. دمو JSON جایگزین این مبدأ نمی‌شود.
 */
export function storefrontHostOrigin(): string {
  return process.env.TOOBA_HOST_ORIGIN ?? "http://127.0.0.1:5088";
}

/**
 * نشانی تصویر نمایشی توسعه برای مرجع مات Media. حقیقت Catalog نیست.
 */
export function storefrontMediaUrl(assetId: string | null | undefined): string {
  const id =
    assetId && assetId !== "00000000-0000-0000-0000-000000000000"
      ? assetId
      : "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
  return `${storefrontHostOrigin()}/v1/storefront/media/${id}`;
}

/**
 * مبلغ Offer را برای نمایش فارسی قالب می‌کند.
 */
export function formatOfferAmount(amount: number, currency: string): string {
  const formatted = new Intl.NumberFormat("fa-IR").format(amount);
  return currency === "IRR" ? `${formatted} ریال` : `${formatted} ${currency}`;
}

function mapCard(value: unknown): StorefrontProductCard | null {
  const item = asRecord(value);
  if (!item) {
    return null;
  }
  const productId = asString(readProp(item, "productId", "ProductId"));
  const primaryOfferId = asString(readProp(item, "primaryOfferId", "PrimaryOfferId"));
  if (!productId || !primaryOfferId) {
    return null;
  }
  const categoryIdRaw = readProp(item, "categoryId", "CategoryId");
  const mediaRaw = readProp(item, "mediaAssetId", "MediaAssetId");
  const promoRaw = readProp(item, "promotionLabel", "PromotionLabel");
  const promotionalAmountRaw = readProp(item, "promotionalAmountExclusiveOfTax", "PromotionalAmountExclusiveOfTax");
  const averageRatingRaw = readProp(item, "averageRating", "AverageRating");
  return {
    productId,
    slug: asString(readProp(item, "slug", "Slug"), productId),
    title: asString(readProp(item, "title", "Title"), "کالا"),
    categoryName: asString(readProp(item, "categoryName", "CategoryName"), "رده"),
    categoryId: categoryIdRaw == null ? null : asString(categoryIdRaw),
    mediaAssetId: mediaRaw == null ? null : asString(mediaRaw),
    primaryOfferId,
    sellerPartyId: asString(readProp(item, "sellerPartyId", "SellerPartyId")),
    sellerDisplayName: asString(readProp(item, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
    offerAmountExclusiveOfTax: asNumber(
      readProp(item, "offerAmountExclusiveOfTax", "OfferAmountExclusiveOfTax") ??
        readProp(item, "offerAmountExclusiveOfTax", "OfferAmountExclusiveOfTax"),
    ),
    promotionalAmountExclusiveOfTax:
      promotionalAmountRaw == null ? null : asNumber(promotionalAmountRaw),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    availableUnits: asNumber(readProp(item, "availableUnits", "AvailableUnits")),
    inStock: asBoolean(readProp(item, "inStock", "InStock") ?? readProp(item, "inStock", "InStock")),
    promotionLabel:
      promoRaw == null
        ? (() => {
            const hostPromo = readProp(item, "promotionLabel", "PromotionLabel");
            return hostPromo == null ? null : asString(hostPromo);
          })()
        : asString(promoRaw),
    averageRating: averageRatingRaw == null ? null : asNumber(averageRatingRaw),
    reviewCount: Math.max(0, asNumber(readProp(item, "reviewCount", "ReviewCount"))),
  };
}

function mapCategory(value: unknown): StorefrontCategoryItem | null {
  const item = asRecord(value);
  if (!item) {
    return null;
  }
  const categoryId = asString(readProp(item, "categoryId", "CategoryId"));
  const parentRaw = readProp(item, "parentCategoryId", "ParentCategoryId");
  return categoryId
    ? {
        categoryId,
        parentCategoryId: parentRaw == null ? null : asString(parentRaw),
        name: asString(readProp(item, "name", "Name"), "رده"),
      }
    : null;
}

function mapOffer(value: unknown): StorefrontOfferCandidate | null {
  const offer = asRecord(value);
  if (!offer) {
    return null;
  }
  const offerId = asString(readProp(offer, "offerId", "OfferId"));
  if (!offerId) {
    return null;
  }
  const skuRaw = readProp(offer, "sellerSku", "SellerSku");
  const promotionalAmountRaw = readProp(offer, "promotionalAmountExclusiveOfTax", "PromotionalAmountExclusiveOfTax");
  const promotionLabelRaw = readProp(offer, "promotionLabel", "PromotionLabel");
  return {
    offerId,
    catalogVariantId: asString(readProp(offer, "catalogVariantId", "CatalogVariantId")),
    sellerPartyId: asString(readProp(offer, "sellerPartyId", "SellerPartyId")),
    sellerDisplayName: asString(readProp(offer, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
    sellerSku: skuRaw == null ? null : asString(skuRaw),
    amountExclusiveOfTax: asNumber(readProp(offer, "amountExclusiveOfTax", "AmountExclusiveOfTax")),
    currency: asString(readProp(offer, "currency", "Currency"), "IRR"),
    market: asString(readProp(offer, "market", "Market"), "IR"),
    availableUnits: asNumber(readProp(offer, "availableUnits", "AvailableUnits")),
    taxCategoryLabel: asString(readProp(offer, "taxCategoryLabel", "TaxCategoryLabel"), "مالیات"),
    promotionalAmountExclusiveOfTax: promotionalAmountRaw == null ? null : asNumber(promotionalAmountRaw),
    promotionLabel: promotionLabelRaw == null ? null : asString(promotionLabelRaw),
  };
}

/** پاسخ عمومی نظرها را بدون پذیرش داده‌های هویتی یا یادداشت تعدیل نگاشت می‌کند. */
export function mapStorefrontReviews(payload: unknown): StorefrontReviewsPage | null {
  const item = asRecord(payload);
  if (!item) return null;
  const averageRaw = readProp(item, "averageRating", "AverageRating");
  const reviewsRaw = readProp(item, "reviews", "Reviews");
  const distributionRaw = readProp(item, "ratingDistribution", "RatingDistribution");
  const reviews: StorefrontPublicReview[] = Array.isArray(reviewsRaw) ? reviewsRaw.flatMap((value) => {
    const review = asRecord(value);
    if (!review) return [];
    const publicId = asString(readProp(review, "publicId", "PublicId") ?? readProp(review, "reviewId", "ReviewId"));
    const body = asString(readProp(review, "body", "Body")).trim();
    const rating = asNumber(readProp(review, "rating", "Rating"));
    if (!publicId || !body || rating < 1 || rating > 5) return [];
    const titleRaw = readProp(review, "title", "Title");
    return [{
      publicId,
      authorDisplayName: asString(readProp(review, "authorDisplayName", "AuthorDisplayName"), "مشتری توبا"),
      rating: Math.trunc(rating),
      title: titleRaw == null || !asString(titleRaw).trim() ? null : asString(titleRaw).trim(),
      body,
      createdAt: asString(readProp(review, "createdAt", "CreatedAt")),
      verifiedPurchase: asBoolean(readProp(review, "verifiedPurchase", "VerifiedPurchase")),
    }];
  }) : [];
  const distribution = new Map<number, number>();
  if (Array.isArray(distributionRaw)) {
    distributionRaw.forEach((value) => {
      const row = asRecord(value);
      const rating = row ? asNumber(readProp(row, "rating", "Rating")) : 0;
      if (rating >= 1 && rating <= 5) distribution.set(rating, Math.max(0, asNumber(readProp(row!, "count", "Count"))));
    });
  } else {
    const keyed = asRecord(distributionRaw);
    if (keyed) Object.entries(keyed).forEach(([key, value]) => {
      const rating = Number(key);
      if (rating >= 1 && rating <= 5) distribution.set(rating, Math.max(0, asNumber(value)));
    });
  }
  return {
    averageRating: averageRaw == null ? null : asNumber(averageRaw),
    reviewCount: Math.max(0, asNumber(readProp(item, "reviewCount", "ReviewCount"))),
    ratingDistribution: [5, 4, 3, 2, 1].map((rating) => ({ rating, count: distribution.get(rating) ?? 0 })),
    reviews,
    page: Math.max(1, asNumber(readProp(item, "page", "Page"), 1)),
    pageSize: Math.max(1, asNumber(readProp(item, "pageSize", "PageSize"), 10)),
    totalCount: Math.max(0, asNumber(readProp(item, "totalCount", "TotalCount"))),
  };
}

/** خطای قابل‌تفکیک ثبت نظر، شامل وضعیت واقعی احراز هویت یا تعارض Host. */
export class StorefrontReviewApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

/** نظرهای منتشرشدهٔ یک slug را فقط از endpoint عمومی می‌خواند. */
export async function loadStorefrontReviews(slug: string, page = 1, pageSize = 10): Promise<StorefrontReviewsPage | null> {
  const payload = await readJson(`/v1/storefront/products/${encodeURIComponent(slug)}/reviews?page=${page}&pageSize=${pageSize}`);
  return mapStorefrontReviews(payload);
}

/** نظر مشتری را از مرز cookie/session موجود ارسال می‌کند و انتشار فوری را فرض نمی‌کند. */
export async function submitStorefrontReview(command: StorefrontReviewSubmission): Promise<void> {
  const response = await fetch("/v1/customer/reviews", {
    method: "POST",
    cache: "no-store",
    credentials: "include",
    headers: { "content-type": "application/json", Accept: "application/json" },
    body: JSON.stringify(command),
  });
  if (!response.ok) {
    const messages: Record<number, string> = {
      401: "برای ثبت نظر باید وارد حساب کاربری شوید.",
      403: "اجازهٔ ثبت نظر برای این حساب یا خرید وجود ندارد.",
      409: "برای این کالا قبلاً نظر ثبت کرده‌اید.",
    };
    throw new StorefrontReviewApiError(response.status, messages[response.status] ?? "ثبت نظر انجام نشد. لطفاً دوباره تلاش کنید.");
  }
}

export interface StorefrontQaItem {
  questionId: string;
  authorDisplayName: string;
  body: string;
  createdAt: string;
  answerBody: string | null;
  answerAuthorDisplayName: string | null;
  answerCreatedAt: string | null;
}

export interface StorefrontQaPage {
  items: StorefrontQaItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** پرسش‌های منتشرشدهٔ محصول را از Host می‌خواند. */
export async function loadStorefrontQuestions(slug: string, page = 1, pageSize = 20): Promise<StorefrontQaPage | null> {
  const payload = await readJson(`/v1/storefront/products/${encodeURIComponent(slug)}/questions?page=${page}&pageSize=${pageSize}`);
  const record = asRecord(payload);
  if (!record) return null;
  const rawItems = readProp(record, "items", "Items") ?? readProp(record, "questions", "Questions");
  const items = Array.isArray(rawItems)
    ? (rawItems as unknown[]).map((row) => {
        const item = asRecord(row) ?? {};
        return {
          questionId: asString(readProp(item, "questionId", "QuestionId")),
          authorDisplayName: asString(readProp(item, "authorDisplayName", "AuthorDisplayName"), "مشتری"),
          body: asString(readProp(item, "body", "Body")),
          createdAt: asString(readProp(item, "createdAt", "CreatedAt")),
          answerBody: readProp(item, "answerBody", "AnswerBody") == null ? null : asString(readProp(item, "answerBody", "AnswerBody")),
          answerAuthorDisplayName:
            readProp(item, "answerAuthorDisplayName", "AnswerAuthorDisplayName") == null
              ? null
              : asString(readProp(item, "answerAuthorDisplayName", "AnswerAuthorDisplayName")),
          answerCreatedAt:
            readProp(item, "answerCreatedAt", "AnswerCreatedAt") == null
              ? null
              : asString(readProp(item, "answerCreatedAt", "AnswerCreatedAt")),
        } satisfies StorefrontQaItem;
      })
    : [];
  return {
    items,
    page: asNumber(readProp(record, "page", "Page"), page),
    pageSize: asNumber(readProp(record, "pageSize", "PageSize"), pageSize),
    totalCount: asNumber(readProp(record, "totalCount", "TotalCount"), items.length),
  };
}

/** پرسش مشتری را ثبت می‌کند (Pending تا تعدیل). */
export async function submitStorefrontQuestion(productId: string, body: string): Promise<void> {
  const headers: Record<string, string> = {
    "content-type": "application/json",
    Accept: "application/json",
  };
  if (typeof window !== "undefined") {
    const actor = window.localStorage.getItem("tooba.customerActorUserId");
    if (actor) headers["X-Tooba-Dev-Actor-User-Id"] = actor;
  }
  const response = await fetch("/v1/customer/product-questions", {
    method: "POST",
    cache: "no-store",
    credentials: "include",
    headers,
    body: JSON.stringify({ productId, body }),
  });
  if (!response.ok) {
    const messages: Record<number, string> = {
      401: "برای ثبت پرسش باید وارد حساب کاربری شوید.",
      400: "متن پرسش معتبر نیست.",
    };
    throw new Error(messages[response.status] ?? "ثبت پرسش انجام نشد.");
  }
}

export interface StorefrontBulkInquiryInput {
  fullName: string;
  phone: string;
  email?: string;
  companyName?: string;
  address: string;
  quantity: number;
  notes?: string;
}

/** درخواست خرید عمده را بدون قیمت‌گذاری جعلی به Host می‌فرستد. */
export async function submitStorefrontBulkInquiry(slug: string, input: StorefrontBulkInquiryInput): Promise<string> {
  const response = await fetch(`/v1/storefront/products/${encodeURIComponent(slug)}/bulk-inquiries`, {
    method: "POST",
    cache: "no-store",
    headers: { "content-type": "application/json", Accept: "application/json" },
    body: JSON.stringify(input),
  });
  if (!response.ok) {
    throw new Error(response.status === 400 ? "اطلاعات درخواست عمده معتبر نیست." : "ثبت درخواست عمده انجام نشد.");
  }
  const payload = (await response.json().catch(() => null)) as Record<string, unknown> | null;
  return asString(payload ? readProp(payload, "inquiryId", "InquiryId") : null, "");
}

function mapAlternateOffers(value: unknown): StorefrontAlternateOffer[] {
  return Array.isArray(value)
    ? value.map((row) => {
        const other = asRecord(row) ?? {};
        return {
          offerId: asString(readProp(other, "offerId", "OfferId")),
          sellerDisplayName: asString(readProp(other, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
          amountExclusiveOfTax: asNumber(readProp(other, "amountExclusiveOfTax", "AmountExclusiveOfTax")),
          currency: asString(readProp(other, "currency", "Currency"), "IRR"),
          availableUnits: asNumber(readProp(other, "availableUnits", "AvailableUnits")),
          inStock: asBoolean(readProp(other, "inStock", "InStock")),
        };
      })
    : [];
}

function mapBrand(value: unknown): StorefrontBrandItem | null {
  const item = asRecord(value);
  if (!item) {
    return null;
  }
  const brandId = asString(readProp(item, "brandId", "BrandId"));
  return brandId ? {
    brandId,
    slug: asString(readProp(item, "slug", "Slug"), brandId),
    name: asString(readProp(item, "name", "Name"), "برند"),
    productCount: asNumber(readProp(item, "productCount", "ProductCount")),
  } : null;
}

function mapPublicSeller(value: unknown): StorefrontPublicSellerItem | null {
  const item = asRecord(value);
  const publicId = item ? asString(readProp(item, "publicId", "PublicId")) : "";
  return item && publicId ? {
    publicId,
    displayName: asString(readProp(item, "displayName", "DisplayName"), "فروشنده"),
    activeOfferCount: asNumber(readProp(item, "activeOfferCount", "ActiveOfferCount")),
    productCount: asNumber(readProp(item, "productCount", "ProductCount")),
  } : null;
}

/** پاسخ merchandising را طوری نگاشت می‌کند که Supported و فهرست خالیِ unsupported حفظ شوند. */
export function mapStorefrontMerchandising(payload: unknown, fallbackKind = ""): StorefrontMerchandisingPage | null {
  const item = asRecord(payload);
  if (!item) return null;
  const products = readProp(item, "products", "Products");
  const reason = readProp(item, "unavailableReason", "UnavailableReason");
  return {
    kind: asString(readProp(item, "kind", "Kind"), fallbackKind),
    title: asString(readProp(item, "title", "Title"), "کالاها"),
    supported: asBoolean(readProp(item, "supported", "Supported")),
    unavailableReason: reason == null ? null : asString(reason),
    products: Array.isArray(products) ? products.map(mapCard).filter((row): row is StorefrontProductCard => row !== null) : [],
  };
}

/** DTO عمومی فروشنده را بدون پذیرش PartyId نگاشت می‌کند. */
export function mapStorefrontPublicSeller(payload: unknown): StorefrontPublicSellerItem | null {
  return mapPublicSeller(payload);
}

/**
 * JSON خانه را به مدل UI تبدیل می‌کند. فیلد price روی محصول پذیرفته نمی‌شود.
 */
export function mapStorefrontHome(payload: unknown): StorefrontHomePage | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const productsRaw = readProp(item, "featuredProducts", "FeaturedProducts");
  const specialOffersRaw = readProp(item, "specialOffers", "SpecialOffers");
  const campaignProductsRaw = readProp(item, "campaignProducts", "CampaignProducts");
  const newArrivalsRaw = readProp(item, "newArrivals", "NewArrivals");
  const productRailRaw = readProp(item, "productRail", "ProductRail");
  const categoriesRaw = readProp(item, "categories", "Categories");
  const brandsRaw = readProp(item, "brands", "Brands");
  const homeCategoriesRaw = readProp(item, "homeCategories", "HomeCategories");
  const bestSellerColumnsRaw = readProp(item, "bestSellerColumns", "BestSellerColumns");
  const mostViewedRaw = readProp(item, "mostViewedProducts", "MostViewedProducts");
  const categories = Array.isArray(categoriesRaw)
    ? categoriesRaw.map(mapCategory).filter((row): row is StorefrontCategoryItem => row !== null)
    : [];
  const homeCategories = Array.isArray(homeCategoriesRaw)
    ? homeCategoriesRaw.map(mapCategory).filter((row): row is StorefrontCategoryItem => row !== null)
    : categories.filter((row) => row.parentCategoryId == null).slice(0, 20);
  return {
    categories,
    featuredProducts: Array.isArray(productsRaw)
      ? productsRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
    specialOffers: Array.isArray(specialOffersRaw)
      ? specialOffersRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
    campaignProducts: Array.isArray(campaignProductsRaw)
      ? campaignProductsRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
    newArrivals: Array.isArray(newArrivalsRaw)
      ? newArrivalsRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
    productRail: Array.isArray(productRailRaw)
      ? productRailRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
    brands: Array.isArray(brandsRaw)
      ? brandsRaw.map(mapBrand).filter((row): row is StorefrontBrandItem => row !== null)
      : [],
    heroTitle: asString(readProp(item, "heroTitle", "HeroTitle"), "فروشگاه توبا"),
    heroSubtitle: asString(readProp(item, "heroSubtitle", "HeroSubtitle")),
    homeCategories,
    bestSellerColumns: Array.isArray(bestSellerColumnsRaw)
      ? bestSellerColumnsRaw.map(mapBestSellerColumn).filter((row): row is StorefrontBestSellerColumn => row !== null)
      : [],
    mostViewedProducts: Array.isArray(mostViewedRaw)
      ? mostViewedRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
  };
}

function mapBestSellerColumn(payload: unknown): StorefrontBestSellerColumn | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const productsRaw = readProp(item, "products", "Products");
  return {
    categoryId: asString(readProp(item, "categoryId", "CategoryId")),
    categoryName: asString(readProp(item, "categoryName", "CategoryName"), "رده"),
    products: Array.isArray(productsRaw)
      ? productsRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
  };
}

/**
 * JSON فهرست را نگاشت می‌کند.
 */
export function mapStorefrontListing(payload: unknown): StorefrontListingPage | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const productsRaw = readProp(item, "products", "Products");
  const categoriesRaw = readProp(item, "categories", "Categories");
  const sellersRaw = readProp(item, "sellers", "Sellers");
  const queryRaw = readProp(item, "query", "Query");
  const categoryRaw = readProp(item, "categoryId", "CategoryId");
  const sellerRaw = readProp(item, "sellerPartyId", "SellerPartyId");
  const availabilityRaw = readProp(item, "inStock", "InStock");
  const sortRaw = asString(readProp(item, "sort", "Sort"), "default");
  const allowedSorts: StorefrontListingSort[] = ["default", "newest", "price-asc", "price-desc"];
  return {
    categories: Array.isArray(categoriesRaw)
      ? categoriesRaw.map(mapCategory).filter((row): row is StorefrontCategoryItem => row !== null)
      : [],
    sellers: Array.isArray(sellersRaw)
      ? sellersRaw.flatMap((value) => {
          const seller = asRecord(value);
          const sellerPartyId = seller ? asString(readProp(seller, "sellerPartyId", "SellerPartyId")) : "";
          return sellerPartyId
            ? [{ sellerPartyId, displayName: asString(readProp(seller!, "displayName", "DisplayName"), "فروشنده") }]
            : [];
        })
      : [],
    products: Array.isArray(productsRaw)
      ? productsRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
    query: queryRaw == null ? null : asString(queryRaw),
    categoryId: categoryRaw == null ? null : asString(categoryRaw),
    sellerPartyId: sellerRaw == null ? null : asString(sellerRaw),
    inStock: typeof availabilityRaw === "boolean" ? availabilityRaw : null,
    sort: allowedSorts.includes(sortRaw as StorefrontListingSort) ? (sortRaw as StorefrontListingSort) : "default",
    page: Math.max(1, asNumber(readProp(item, "page", "Page"), 1)),
    pageSize: Math.max(1, asNumber(readProp(item, "pageSize", "PageSize"), 24)),
    totalCount: Math.max(0, asNumber(readProp(item, "totalCount", "TotalCount"))),
  };
}

/**
 * JSON PDP را نگاشت می‌کند. مبلغ فقط از primaryOffer خوانده می‌شود.
 */
export function mapStorefrontDetail(payload: unknown): StorefrontProductDetailPage | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const primaryOffer = mapOffer(readProp(item, "primaryOffer", "PrimaryOffer"));
  const productId = asString(readProp(item, "productId", "ProductId"));
  if (!primaryOffer || !productId) {
    return null;
  }
  const othersRaw = readProp(item, "otherSellers", "OtherSellers");
  const relatedRaw = readProp(item, "relatedProducts", "RelatedProducts");
  const mediaRaw = readProp(item, "mediaAssetIds", "MediaAssetIds");
  const brandRaw = readProp(item, "brandName", "BrandName");
  const descriptionRaw = readProp(item, "description", "Description");
  const shortDescriptionRaw = readProp(item, "shortDescription", "ShortDescription");
  const fullDescriptionRaw = readProp(item, "fullDescription", "FullDescription");
  const specificationsRaw = readProp(item, "specifications", "Specifications");
  const variantsRaw = readProp(item, "variants", "Variants");
  const promotionalAmountRaw = readProp(item, "promotionalAmountExclusiveOfTax", "PromotionalAmountExclusiveOfTax");
  const promotionLabelRaw = readProp(item, "promotionLabel", "PromotionLabel");
  return {
    productId,
    slug: asString(readProp(item, "slug", "Slug"), productId),
    title: asString(readProp(item, "title", "Title"), "کالا"),
    description: descriptionRaw == null ? null : asString(descriptionRaw),
    shortDescription: shortDescriptionRaw == null ? null : asString(shortDescriptionRaw),
    fullDescription: fullDescriptionRaw == null ? null : asString(fullDescriptionRaw),
    specifications: Array.isArray(specificationsRaw)
      ? specificationsRaw.flatMap((value) => {
          const specification = asRecord(value);
          const label = specification ? asString(readProp(specification, "label", "Label")).trim() : "";
          const specificationValue = specification ? asString(readProp(specification, "value", "Value")).trim() : "";
          return label && specificationValue ? [{ label, value: specificationValue } satisfies StorefrontProductSpecification] : [];
        })
      : [],
    variants: Array.isArray(variantsRaw)
      ? variantsRaw.flatMap((value) => {
          const variant = asRecord(value);
          const variantId = variant ? asString(readProp(variant, "variantId", "VariantId")) : "";
          const optionsRaw = variant
            ? readProp(variant, "options", "Options") ?? readProp(variant, "axes", "Axes")
            : null;
          if (!variantId) return [];
          return [{
            variantId,
            options: Array.isArray(optionsRaw) ? optionsRaw.flatMap((optionValue) => {
              const option = asRecord(optionValue);
              const label = option ? asString(readProp(option, "label", "Label")).trim() : "";
              const value = option ? asString(readProp(option, "value", "Value")).trim() : "";
              return label && value ? [{ label, value }] : [];
            }) : [],
            primaryOffer: mapOffer(readProp(variant!, "primaryOffer", "PrimaryOffer")),
            otherSellers: mapAlternateOffers(readProp(variant!, "otherSellers", "OtherSellers")),
          } satisfies StorefrontProductVariant];
        })
      : [],
    categoryName: asString(readProp(item, "categoryName", "CategoryName"), "رده"),
    brandName: brandRaw == null ? null : asString(brandRaw),
    mediaAssetIds: Array.isArray(mediaRaw) ? mediaRaw.map((id) => asString(id)) : [],
    selectedVariantId: asString(readProp(item, "selectedVariantId", "SelectedVariantId")),
    primaryOffer,
    otherSellers: mapAlternateOffers(othersRaw),
    relatedProducts: Array.isArray(relatedRaw)
      ? relatedRaw.map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
      : [],
    seoTitle: asString(readProp(item, "seoTitle", "SeoTitle")),
    seoDescription: asString(readProp(item, "seoDescription", "SeoDescription")),
    cartMutationEnabled: asBoolean(readProp(item, "cartMutationEnabled", "CartMutationEnabled")),
    promotionalAmountExclusiveOfTax: promotionalAmountRaw == null ? null : asNumber(promotionalAmountRaw),
    promotionLabel: promotionLabelRaw == null ? null : asString(promotionLabelRaw),
    averageRating: (() => {
      const value = readProp(item, "averageRating", "AverageRating");
      return value == null ? null : asNumber(value);
    })(),
    reviewCount: Math.max(0, asNumber(readProp(item, "reviewCount", "ReviewCount"))),
  };
}

async function readJson(path: string): Promise<unknown | null> {
  try {
    // درخواست مرورگر از rewrite هم‌مبدأ Next عبور می‌کند؛ RSC مستقیماً Host را می‌خواند.
    const url = typeof window === "undefined" ? `${storefrontHostOrigin()}${path}` : path;
    const response = await fetch(url, { cache: "no-store" });
    if (!response.ok) {
      return null;
    }
    return await response.json();
  } catch {
    return null;
  }
}

/**
 * خانه را از Host می‌خواند. در خطا null برمی‌گردد تا UI فیکسچر نسازد.
 */
export async function loadStorefrontHome(): Promise<StorefrontHomePage | null> {
  return mapStorefrontHome(await readJson("/v1/storefront/home"));
}

/**
 * فهرست را از Host می‌خواند.
 */
export async function loadStorefrontListing(request: StorefrontListingRequest = {}): Promise<StorefrontListingPage | null> {
  const params = new URLSearchParams();
  if (request.query) {
    params.set("q", request.query);
  }
  if (request.categoryId) {
    params.set("categoryId", request.categoryId);
  }
  if (request.sellerPartyId) {
    params.set("sellerPartyId", request.sellerPartyId);
  }
  if (request.inStock !== undefined) {
    params.set("inStock", String(request.inStock));
  }
  if (request.sort) {
    params.set("sort", request.sort);
  }
  if (request.page && request.page > 1) {
    params.set("page", String(request.page));
  }
  const suffix = params.toString() ? `?${params.toString()}` : "";
  return mapStorefrontListing(await readJson(`/v1/storefront/products${suffix}`));
}

/**
 * PDP را از Host می‌خواند و در صورت انتخاب ترکیب، VariantId را فقط به درخواست
 * مقتدر Host می‌افزاید؛ انتخاب Offer در مرورگر انجام نمی‌شود.
 */
export async function loadStorefrontDetail(slug: string, variantId?: string): Promise<StorefrontProductDetailPage | null> {
  const suffix = variantId ? `?variantId=${encodeURIComponent(variantId)}` : "";
  return mapStorefrontDetail(await readJson(`/v1/storefront/products/${encodeURIComponent(slug)}${suffix}`));
}

/** صفحهٔ merchandising را با وضعیت پشتیبانی صریح از Host می‌خواند. */
export async function loadStorefrontMerchandising(kind: string): Promise<StorefrontMerchandisingPage | null> {
  return mapStorefrontMerchandising(await readJson(`/v1/storefront/merchandising/${encodeURIComponent(kind)}`), kind);
}

/** فهرست برندهای منتشرشده را از Catalog می‌خواند. */
export async function loadStorefrontBrands(): Promise<StorefrontBrandItem[] | null> {
  const payload = await readJson("/v1/storefront/brands");
  return Array.isArray(payload) ? payload.map(mapBrand).filter((row): row is StorefrontBrandItem => row !== null) : null;
}

/** landing برند را بدون کپی بازاریابی ساختگی نگاشت می‌کند. */
export async function loadStorefrontBrand(slug: string): Promise<StorefrontBrandPage | null> {
  const item = asRecord(await readJson(`/v1/storefront/brands/${encodeURIComponent(slug)}`));
  const brand = item ? mapBrand(readProp(item, "brand", "Brand")) : null;
  const products = item ? readProp(item, "products", "Products") : null;
  return brand && Array.isArray(products)
    ? { brand, products: products.map(mapCard).filter((row): row is StorefrontProductCard => row !== null) }
    : null;
}

/** فهرست فروشندگان عمومی را بدون پذیرش PartyId می‌خواند. */
export async function loadStorefrontSellers(): Promise<StorefrontPublicSellerItem[] | null> {
  const payload = await readJson("/v1/storefront/sellers");
  return Array.isArray(payload) ? payload.map(mapPublicSeller).filter((row): row is StorefrontPublicSellerItem => row !== null) : null;
}

/** پروفایل عمومی فروشنده را با PublicId مات می‌خواند. */
export async function loadStorefrontSeller(publicId: string): Promise<StorefrontPublicSellerPage | null> {
  const item = asRecord(await readJson(`/v1/storefront/sellers/${encodeURIComponent(publicId)}`));
  const seller = item ? mapPublicSeller(readProp(item, "seller", "Seller")) : null;
  const products = item ? readProp(item, "products", "Products") : null;
  return seller && Array.isArray(products)
    ? { seller, products: products.map(mapCard).filter((row): row is StorefrontProductCard => row !== null) }
    : null;
}
