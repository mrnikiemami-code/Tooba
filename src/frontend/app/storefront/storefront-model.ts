/**
 * کارت فهرست فروشگاه. مبلغ از Offer/Pricing است نه فیلد قیمت روی Product.
 */
export interface StorefrontProductCard {
  productId: string;
  slug: string;
  title: string;
  categoryName: string;
  categoryId: string | null;
  mediaAssetId: string | null;
  primaryOfferId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  offerAmountExclusiveOfTax: number;
  promotionalAmountExclusiveOfTax: number | null;
  currency: string;
  availableUnits: number;
  inStock: boolean;
  promotionLabel: string | null;
}

/**
 * ردهٔ ناوبری از Catalog.
 */
export interface StorefrontCategoryItem {
  categoryId: string;
  parentCategoryId: string | null;
  name: string;
}

/**
 * برند Catalog برای نوار برند خانه.
 */
export interface StorefrontBrandItem {
  brandId: string;
  name: string;
}

/**
 * خانهٔ زنده.
 */
export interface StorefrontHomePage {
  categories: StorefrontCategoryItem[];
  featuredProducts: StorefrontProductCard[];
  specialOffers: StorefrontProductCard[];
  campaignProducts: StorefrontProductCard[];
  newArrivals: StorefrontProductCard[];
  productRail: StorefrontProductCard[];
  brands: StorefrontBrandItem[];
  heroTitle: string;
  heroSubtitle: string;
}

/**
 * فهرست زنده.
 */
export interface StorefrontListingPage {
  categories: StorefrontCategoryItem[];
  sellers: StorefrontSellerFilterItem[];
  products: StorefrontProductCard[];
  query: string | null;
  categoryId: string | null;
  sellerPartyId: string | null;
  inStock: boolean | null;
  sort: StorefrontListingSort;
  page: number;
  pageSize: number;
  totalCount: number;
}

/**
 * facet فروشنده فقط از Offerهای واقعاً قابل نمایش ساخته می‌شود.
 */
export interface StorefrontSellerFilterItem {
  sellerPartyId: string;
  displayName: string;
}

/** ترتیب‌های واقعی که Host روی مبلغ ترکیب‌شده یا ترتیب Catalog اعمال می‌کند. */
export type StorefrontListingSort = "default" | "newest" | "price-asc" | "price-desc";

/** ورودی عمومی کشف کالا؛ UI فقط این پارامترها را به Host منتقل می‌کند. */
export interface StorefrontListingRequest {
  query?: string;
  categoryId?: string;
  sellerPartyId?: string;
  inStock?: boolean;
  sort?: StorefrontListingSort;
  page?: number;
}

/**
 * Offer نمایشی PDP. مبلغ Exclusive از Pricing است.
 */
export interface StorefrontOfferCandidate {
  offerId: string;
  catalogVariantId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  sellerSku: string | null;
  amountExclusiveOfTax: number;
  currency: string;
  market: string;
  availableUnits: number;
  taxCategoryLabel: string;
}

/**
 * فروشندهٔ دیگر روی همان Variant.
 */
export interface StorefrontAlternateOffer {
  offerId: string;
  sellerDisplayName: string;
  amountExclusiveOfTax: number;
  currency: string;
  availableUnits: number;
  inStock: boolean;
}

/**
 * جزئیات محصول زنده. موجودی از Inventory روی Offer است.
 */
export interface StorefrontProductDetailPage {
  productId: string;
  slug: string;
  title: string;
  description: string | null;
  categoryName: string;
  brandName: string | null;
  mediaAssetIds: string[];
  selectedVariantId: string;
  primaryOffer: StorefrontOfferCandidate;
  otherSellers: StorefrontAlternateOffer[];
  seoTitle: string;
  seoDescription: string;
  cartMutationEnabled: boolean;
}
