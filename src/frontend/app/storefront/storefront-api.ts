import type {
  StorefrontAlternateOffer,
  StorefrontCategoryItem,
  StorefrontHomePage,
  StorefrontListingPage,
  StorefrontOfferCandidate,
  StorefrontProductCard,
  StorefrontProductDetailPage,
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
 * نشانی تصویر نمایشی توسعه برای مرجع مات Media.
 */
export function storefrontMediaUrl(assetId: string | null | undefined): string {
  const id = assetId && assetId !== "00000000-0000-0000-0000-000000000000" ? assetId : "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
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
  return {
    productId,
    slug: asString(readProp(item, "slug", "Slug"), productId),
    title: asString(readProp(item, "title", "Title"), "کالا"),
    categoryName: asString(readProp(item, "categoryName", "CategoryName"), "رده"),
    categoryId: categoryIdRaw == null ? null : asString(categoryIdRaw),
    mediaAssetId: mediaRaw == null ? null : asString(mediaRaw),
    primaryOfferId,
    sellerDisplayName: asString(readProp(item, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
    offerAmountExclusiveOfTax: asNumber(readProp(item, "offerAmountExclusiveOfTax", "OfferAmountExclusiveOfTax")),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    availableUnits: asNumber(readProp(item, "availableUnits", "AvailableUnits")),
    inStock: asBoolean(readProp(item, "inStock", "InStock")),
    promotionLabel: promoRaw == null ? null : asString(promoRaw),
  };
}

function mapCategory(value: unknown): StorefrontCategoryItem | null {
  const item = asRecord(value);
  if (!item) {
    return null;
  }
  const categoryId = asString(readProp(item, "categoryId", "CategoryId"));
  return categoryId ? { categoryId, name: asString(readProp(item, "name", "Name"), "رده") } : null;
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
  };
}

/**
 * JSON خانه را به مدل UI تبدیل می‌کند. فیلد price روی محصول پذیرفته نمی‌شود.
 */
export function mapStorefrontHome(payload: unknown): StorefrontHomePage | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const products = Array.isArray(readProp(item, "featuredProducts", "FeaturedProducts"))
    ? (readProp(item, "featuredProducts", "FeaturedProducts") as unknown[]).map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
    : [];
  const categories = Array.isArray(readProp(item, "categories", "Categories"))
    ? (readProp(item, "categories", "Categories") as unknown[]).map(mapCategory).filter((row): row is StorefrontCategoryItem => row !== null)
    : [];
  return {
    categories,
    featuredProducts: products,
    heroTitle: asString(readProp(item, "heroTitle", "HeroTitle"), "فروشگاه توبا"),
    heroSubtitle: asString(readProp(item, "heroSubtitle", "HeroSubtitle")),
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
  const products = Array.isArray(readProp(item, "products", "Products"))
    ? (readProp(item, "products", "Products") as unknown[]).map(mapCard).filter((row): row is StorefrontProductCard => row !== null)
    : [];
  const categories = Array.isArray(readProp(item, "categories", "Categories"))
    ? (readProp(item, "categories", "Categories") as unknown[]).map(mapCategory).filter((row): row is StorefrontCategoryItem => row !== null)
    : [];
  const queryRaw = readProp(item, "query", "Query");
  const categoryRaw = readProp(item, "categoryId", "CategoryId");
  return {
    categories,
    products,
    query: queryRaw == null ? null : asString(queryRaw),
    categoryId: categoryRaw == null ? null : asString(categoryRaw),
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
  const mediaRaw = readProp(item, "mediaAssetIds", "MediaAssetIds");
  const brandRaw = readProp(item, "brandName", "BrandName");
  const descriptionRaw = readProp(item, "description", "Description");
  return {
    productId,
    slug: asString(readProp(item, "slug", "Slug"), productId),
    title: asString(readProp(item, "title", "Title"), "کالا"),
    description: descriptionRaw == null ? null : asString(descriptionRaw),
    categoryName: asString(readProp(item, "categoryName", "CategoryName"), "رده"),
    brandName: brandRaw == null ? null : asString(brandRaw),
    mediaAssetIds: Array.isArray(mediaRaw) ? mediaRaw.map((id) => asString(id)) : [],
    selectedVariantId: asString(readProp(item, "selectedVariantId", "SelectedVariantId")),
    primaryOffer,
    otherSellers: Array.isArray(othersRaw)
      ? othersRaw.map((row) => {
          const other = asRecord(row) ?? {};
          return {
            offerId: asString(readProp(other, "offerId", "OfferId")),
            sellerDisplayName: asString(readProp(other, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
            amountExclusiveOfTax: asNumber(readProp(other, "amountExclusiveOfTax", "AmountExclusiveOfTax")),
            currency: asString(readProp(other, "currency", "Currency"), "IRR"),
            availableUnits: asNumber(readProp(other, "availableUnits", "AvailableUnits")),
            inStock: asBoolean(readProp(other, "inStock", "InStock")),
          } satisfies StorefrontAlternateOffer;
        })
      : [],
    seoTitle: asString(readProp(item, "seoTitle", "SeoTitle")),
    seoDescription: asString(readProp(item, "seoDescription", "SeoDescription")),
    cartMutationEnabled: asBoolean(readProp(item, "cartMutationEnabled", "CartMutationEnabled")),
  };
}

async function readJson(path: string): Promise<unknown | null> {
  try {
    const response = await fetch(`${storefrontHostOrigin()}${path}`, { cache: "no-store" });
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
export async function loadStorefrontListing(query?: string, categoryId?: string): Promise<StorefrontListingPage | null> {
  const params = new URLSearchParams();
  if (query) {
    params.set("q", query);
  }
  if (categoryId) {
    params.set("categoryId", categoryId);
  }
  const suffix = params.toString() ? `?${params.toString()}` : "";
  return mapStorefrontListing(await readJson(`/v1/storefront/products${suffix}`));
}

/**
 * PDP را از Host می‌خواند.
 */
export async function loadStorefrontDetail(slug: string): Promise<StorefrontProductDetailPage | null> {
  return mapStorefrontDetail(await readJson(`/v1/storefront/products/${encodeURIComponent(slug)}`));
}
