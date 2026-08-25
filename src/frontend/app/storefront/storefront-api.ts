import type {
  StorefrontAlternateOffer,
  StorefrontBrandItem,
  StorefrontCategoryItem,
  StorefrontHomePage,
  StorefrontListingPage,
  StorefrontListingRequest,
  StorefrontListingSort,
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

function mapBrand(value: unknown): StorefrontBrandItem | null {
  const item = asRecord(value);
  if (!item) {
    return null;
  }
  const brandId = asString(readProp(item, "brandId", "BrandId"));
  return brandId ? { brandId, name: asString(readProp(item, "name", "Name"), "برند") } : null;
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
  return {
    categories: Array.isArray(categoriesRaw)
      ? categoriesRaw.map(mapCategory).filter((row): row is StorefrontCategoryItem => row !== null)
      : [],
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
 * PDP را از Host می‌خواند.
 */
export async function loadStorefrontDetail(slug: string): Promise<StorefrontProductDetailPage | null> {
  return mapStorefrontDetail(await readJson(`/v1/storefront/products/${encodeURIComponent(slug)}`));
}
