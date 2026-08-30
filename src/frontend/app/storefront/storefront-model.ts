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
  /** میانگین مقتدر امتیازهای منتشرشده؛ در نبود امتیاز null است. */
  averageRating: number | null;
  /** تعداد مقتدر نظرهای منتشرشده. */
  reviewCount: number;
  /** برند اختیاری؛ فیلتر سراسری PLP با f_brand. */
  brandId?: string | null;
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
  slug: string;
  name: string;
  productCount: number;
  logoMediaAssetId: string | null;
}

/** فروشندهٔ عمومی بدون PartyId، رابطهٔ مجوز، اطلاعات تماس یا دادهٔ تسویه. */
export interface StorefrontPublicSellerItem {
  publicId: string;
  displayName: string;
  activeOfferCount: number;
  productCount: number;
}

/** landing عمومی برند با کارت‌های زندهٔ همان برند. */
export interface StorefrontBrandPage {
  brand: StorefrontBrandItem;
  products: StorefrontProductCard[];
}

/** پروفایل عمومی فروشنده و کالاهای Offer فعال او. */
export interface StorefrontPublicSellerPage {
  seller: StorefrontPublicSellerItem;
  products: StorefrontProductCard[];
}

/** پاسخ صریح merchandising؛ Supported مانع ادعای ساختگی برای سیگنال‌های غایب است. */
export interface StorefrontMerchandisingPage {
  kind: string;
  title: string;
  supported: boolean;
  unavailableReason: string | null;
  products: StorefrontProductCard[];
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
  /** ریل خانه؛ سقف Shopeiva (~20) نه dump کامل Catalog. */
  homeCategories: StorefrontCategoryItem[];
  bestSellerColumns: StorefrontBestSellerColumn[];
  mostViewedProducts: StorefrontProductCard[];
  featuredReviews: StorefrontFeaturedReviewItem[];
  latestArticles: StorefrontArticleItem[];
}

/** ستون پرفروش خانه مطابق Shopeiva. */
export interface StorefrontBestSellerColumn {
  categoryId: string;
  categoryName: string;
  products: StorefrontProductCard[];
}

/** نظر منتشرشدهٔ اخیر برای ریل خانه. */
export interface StorefrontFeaturedReviewItem {
  publicId: string;
  authorDisplayName: string;
  rating: number;
  title: string | null;
  body: string;
  verifiedPurchase: boolean;
  createdAt: string;
  productTitle: string;
  productSlug: string;
}

/** مقالهٔ منتشرشدهٔ اخیر برای ریل خانه. */
export interface StorefrontArticleItem {
  articleId: string;
  slug: string;
  title: string;
  excerpt: string;
  coverMediaAssetId: string | null;
  publishDate: string;
  authorDisplayName: string;
  tags: string[];
  isFeatured: boolean;
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

/** گزینهٔ facet در PLP رده. */
export interface StorefrontPlpFacetOption {
  value: string;
  label: string;
  count: number | null;
}

/** facet پویا از پیکربندی رده (T008) + شمارش runtime. */
export interface StorefrontPlpFacet {
  definitionId: string;
  code: string;
  localizedName: string;
  valueKind: string;
  displayType: string;
  isSearchable: boolean;
  isCollapsedByDefault: boolean;
  showCounts: boolean;
  rangeMin: number | null;
  rangeMax: number | null;
  options: StorefrontPlpFacetOption[];
}

/** چیپ فیلتر اعمال‌شده. */
export interface StorefrontAppliedFilterChip {
  code: string;
  label: string;
  value: string;
  displayValue: string;
}

export interface StorefrontCategoryBreadcrumbItem {
  categoryId: string;
  name: string;
  slug: string;
  path: string;
}

export interface StorefrontCategoryChildItem {
  categoryId: string;
  name: string;
  slug: string;
  path: string;
}

/** صفحهٔ PLP ردهٔ canonical. */
export interface StorefrontCategoryPlpPage {
  categoryId: string;
  locale: string;
  slug: string;
  name: string;
  shortDescription: string | null;
  description: string | null;
  canonicalPath: string;
  isRedirect: boolean;
  redirectToPath: string | null;
  totalCount: number;
  page: number;
  pageSize: number;
  sort: StorefrontListingSort;
  breadcrumb: StorefrontCategoryBreadcrumbItem[];
  subcategories: StorefrontCategoryChildItem[];
  facets: StorefrontPlpFacet[];
  appliedFilters: StorefrontAppliedFilterChip[];
  products: StorefrontProductCard[];
  supportedSorts: StorefrontListingSort[];
}

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
  promotionalAmountExclusiveOfTax?: number | null;
  promotionLabel?: string | null;
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

/** یک گزینهٔ قابل فهم برای مشتری؛ برچسب و مقدار هر دو از Catalog می‌آیند. */
export interface StorefrontVariantOption {
  label: string;
  value: string;
}

/** ترکیب قابل انتخاب و Offerهای مقتدر همان ترکیب که Host برگردانده است. */
export interface StorefrontProductVariant {
  variantId: string;
  options: StorefrontVariantOption[];
  primaryOffer: StorefrontOfferCandidate | null;
  otherSellers: StorefrontAlternateOffer[];
}

/** مشخصهٔ عمومی محصول با برچسب و مقدار قابل نمایش، بدون شناسهٔ داخلی. */
export interface StorefrontProductSpecification {
  label: string;
  value: string;
}

/**
 * جزئیات محصول زنده. موجودی از Inventory روی Offer است.
 */
export interface StorefrontProductDetailPage {
  productId: string;
  slug: string;
  title: string;
  description: string | null;
  shortDescription: string | null;
  fullDescription: string | null;
  specifications: StorefrontProductSpecification[];
  variants: StorefrontProductVariant[];
  categoryName: string;
  brandName: string | null;
  mediaAssetIds: string[];
  selectedVariantId: string;
  primaryOffer: StorefrontOfferCandidate;
  otherSellers: StorefrontAlternateOffer[];
  relatedProducts: StorefrontProductCard[];
  seoTitle: string;
  seoDescription: string;
  cartMutationEnabled: boolean;
  promotionalAmountExclusiveOfTax?: number | null;
  promotionLabel?: string | null;
  /** میانگین مقتدر امتیازهای منتشرشده از Host. */
  averageRating: number | null;
  /** تعداد مقتدر نظرهای منتشرشده از Host. */
  reviewCount: number;
}

/** یک سطر توزیع امتیاز منتشرشده که شمارش آن از Host می‌آید. */
export interface StorefrontRatingDistribution {
  rating: number;
  count: number;
}

/** نظر عمومی منتشرشده؛ عمداً هیچ شناسهٔ Actor، User یا Party ندارد. */
export interface StorefrontPublicReview {
  publicId: string;
  authorDisplayName: string;
  rating: number;
  title: string | null;
  body: string;
  createdAt: string;
  verifiedPurchase: boolean;
}

/** صفحهٔ عمومی نظرها با آمار مقتدر و صفحه‌بندی Host. */
export interface StorefrontReviewsPage {
  averageRating: number | null;
  reviewCount: number;
  ratingDistribution: StorefrontRatingDistribution[];
  reviews: StorefrontPublicReview[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** فرمان ثبت نظر مشتری؛ نام نویسنده از نشست احراز‌شده می‌آید. */
export interface StorefrontReviewSubmission {
  productId: string;
  rating: number;
  title?: string;
  body: string;
}
