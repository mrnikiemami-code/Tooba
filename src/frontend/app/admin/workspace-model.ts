import type { ProductPublishReadiness } from "./product-publishing-panel-model.ts";

/**
 * مدل نمایش Product Workspace. این فایل DTO ترکیب Host است نه موجودیت EF.
 * Product.Price و Product.Stock اینجا وجود ندارند.
 */
export interface ProductWorkspacePermissions {
  canView: boolean;
  canEditCatalog: boolean;
  canEditCommercial: boolean;
  canEditInventory: boolean;
  canPublish: boolean;
}

export interface ProductOfferRow {
  offerId: string;
  catalogVariantId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  status: string;
  channel: string;
  sellerSku: string | null;
}

export interface ProductPriceRow {
  priceId: string;
  offerId: string;
  market: string;
  currency: string;
  amountExclusiveOfTax: number;
  status: string;
}

export interface ProductStockRow {
  offerId: string;
  locationId: string;
  locationCode: string;
  locationName: string;
  onHand: number;
  reserved: number;
  available: number;
}

/** ترجمهٔ محلی محصول — بدون NameFa/NameEn. */
export interface ProductTranslationView {
  locale: string;
  name: string;
  slug: string | null;
  shortDescription: string | null;
  description: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
}

export interface ProductWorkspaceView {
  productId: string;
  title: string;
  status: string;
  kind: string;
  brandName: string | null;
  categoryNames: string[];
  /** شناسهٔ دستهٔ اصلی؛ برای schema ویژگی‌ها لازم است. */
  primaryCategoryId?: string | null;
  /** مسیر انسانی دسته (مثلاً کالای دیجیتال > موبایل > گوشی). */
  categoryPath?: string | null;
  /** آیا دستهٔ اصلی سطح سوم (قابل اختصاص) است؟ */
  isPrimaryCategoryAssignable?: boolean;
  slug?: string | null;
  shortDescription?: string | null;
  translations?: ProductTranslationView[];
  variants: {
    variantId: string;
    fingerprint: string;
    status: string;
    catalogCodeSeam: string | null;
    offerCount: number;
  }[];
  media: { mediaAssetId: string; primary: boolean; displayOrder?: number; altText?: string | null }[];
  offers: ProductOfferRow[];
  prices: ProductPriceRow[];
  taxClassifications: { offerId: string; categoryCode: string; displayName: string }[];
  stock: ProductStockRow[];
  seo: { slugSeam: string | null; seoTitleSeam: string | null; semanticNote: string };
  publication: {
    catalogStatus: string;
    purchasableHint: boolean;
    checks: string[];
    statusUpdatedAt?: string | null;
    aggregateReadiness?: ProductPublishReadiness | null;
  };
  activity: { kind: string; summary: string; at: string }[];
  audit: { kind: string; summary: string; at: string }[];
  permissions: ProductWorkspacePermissions;
  catalogUpdatedAt: string;
  readinessWarnings: string[];
  unsupportedMutations: string[];
}

/**
 * آمادگی انتشار UI است نه حقیقت دامنهٔ قابل‌خرید.
 */
export function mapPublicationReadiness(view: ProductWorkspaceView): string[] {
  return view.publication.checks;
}

/**
 * Commercial باید چند Offer را بدون ادغام در یک ردیف Product نشان دهد.
 */
export function groupOffersBySeller(view: ProductWorkspaceView): Map<string, ProductOfferRow[]> {
  const grouped = new Map<string, ProductOfferRow[]>();
  for (const offer of view.offers) {
    const current = grouped.get(offer.sellerPartyId) ?? [];
    current.push(offer);
    grouped.set(offer.sellerPartyId, current);
  }
  return grouped;
}

/**
 * موجودی هرگز روی شناسهٔ Product جمع نمی‌شود.
 */
export function assertInventoryIsOfferScoped(view: ProductWorkspaceView): boolean {
  return view.stock.every((row) => view.offers.some((offer) => offer.offerId === row.offerId));
}

export const demoProductWorkspace: ProductWorkspaceView = {
  productId: "11111111-1111-7111-8111-111111111111",
  title: "پیراهن لینن اداری",
  status: "Published",
  kind: "Physical",
  brandName: "Tooba Studio",
  categoryNames: ["پوشاک"],
  primaryCategoryId: "66111111-1111-7111-8111-111111111111",
  categoryPath: "پوشاک > مردانه > پیراهن",
  isPrimaryCategoryAssignable: true,
  slug: "linen-admin-shirt",
  shortDescription: "پیراهن لینن سبک اداری",
  translations: [
    {
      locale: "fa-IR",
      name: "پیراهن لینن اداری",
      slug: "linen-admin-shirt",
      shortDescription: "پیراهن لینن سبک اداری",
      description: null,
      seoTitle: "پیراهن لینن",
      seoDescription: null,
    },
    {
      locale: "en",
      name: "Linen office shirt",
      slug: "linen-admin-shirt",
      shortDescription: "Light linen office shirt",
      description: null,
      seoTitle: "Linen shirt",
      seoDescription: null,
    },
  ],
  variants: [
    {
      variantId: "22111111-1111-7111-8111-111111111111",
      fingerprint: "color=sand|size=m",
      status: "Published",
      catalogCodeSeam: "LINEN-SAND-M",
      offerCount: 2,
    },
  ],
  media: [
    {
      mediaAssetId: "33111111-1111-7111-8111-111111111111",
      primary: true,
      displayOrder: 0,
      altText: "پیراهن لینن",
    },
  ],
  offers: [
    {
      offerId: "44111111-1111-7111-8111-111111111111",
      catalogVariantId: "22111111-1111-7111-8111-111111111111",
      sellerPartyId: "seller-north",
      sellerDisplayName: "بازار اطلس",
      status: "Active",
      channel: "Web",
      sellerSku: "NORTH-LINEN-M",
    },
    {
      offerId: "44111111-1111-7111-8111-111111111112",
      catalogVariantId: "22111111-1111-7111-8111-111111111111",
      sellerPartyId: "seller-south",
      sellerDisplayName: "خانهٔ پارچهٔ شمال",
      status: "Active",
      channel: "Web",
      sellerSku: "SOUTH-LINEN-M",
    },
  ],
  prices: [
    {
      priceId: "55111111-1111-7111-8111-111111111111",
      offerId: "44111111-1111-7111-8111-111111111111",
      market: "IR",
      currency: "IRR",
      amountExclusiveOfTax: 1200000,
      status: "Active",
    },
    {
      priceId: "55111111-1111-7111-8111-111111111112",
      offerId: "44111111-1111-7111-8111-111111111112",
      market: "IR",
      currency: "IRR",
      amountExclusiveOfTax: 1180000,
      status: "Active",
    },
  ],
  taxClassifications: [
    { offerId: "44111111-1111-7111-8111-111111111111", categoryCode: "standard", displayName: "استاندارد" },
  ],
  stock: [
    {
      offerId: "44111111-1111-7111-8111-111111111111",
      locationId: "loc-tehran",
      locationCode: "THR-1",
      locationName: "انبار تهران",
      onHand: 12,
      reserved: 2,
      available: 10,
    },
    {
      offerId: "44111111-1111-7111-8111-111111111112",
      locationId: "loc-shiraz",
      locationCode: "SHZ-1",
      locationName: "انبار شیراز",
      onHand: 4,
      reserved: 0,
      available: 4,
    },
  ],
  seo: {
    slugSeam: "linen-admin-shirt",
    seoTitleSeam: "پیراهن لینن",
    semanticNote: "Semantic Content != Page Composition",
  },
  publication: {
    catalogStatus: "Published",
    purchasableHint: true,
    checks: [],
    statusUpdatedAt: "2026-08-24T00:00:00Z",
    aggregateReadiness: null,
  },
  activity: [{ kind: "activity", summary: "محصول از فهرست باز شد", at: "2026-08-24T00:00:00Z" }],
  audit: [{ kind: "audit", summary: "Catalog loaded separately from Offer/Price/Stock", at: "2026-08-24T00:00:00Z" }],
  permissions: {
    canView: true,
    canEditCatalog: true,
    canEditCommercial: true,
    canEditInventory: true,
    canPublish: true,
  },
  catalogUpdatedAt: "2026-08-24T00:00:00Z",
  readinessWarnings: [],
  unsupportedMutations: ["media-binary-upload", "promotion-write", "full-content-studio"],
};
