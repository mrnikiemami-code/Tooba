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
  sellerDisplayName: string;
  offerAmountExclusiveOfTax: number;
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
  name: string;
}

/**
 * خانهٔ زنده.
 */
export interface StorefrontHomePage {
  categories: StorefrontCategoryItem[];
  featuredProducts: StorefrontProductCard[];
  heroTitle: string;
  heroSubtitle: string;
}

/**
 * فهرست زنده.
 */
export interface StorefrontListingPage {
  categories: StorefrontCategoryItem[];
  products: StorefrontProductCard[];
  query: string | null;
  categoryId: string | null;
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
