/**
 * Shared Offer workspace contracts (TB-296).
 *
 * These types describe the real HTTP-visible Offer read model plus a small adapter surface.
 * They carry no network code and no domain policy; the page owns role-specific adapters and
 * the product APIs stay in their existing owning clients.
 */

export type OfferRole = "admin" | "seller";
export type OfferMode = "create" | "edit";

export type OfferTabId =
  | "identity"
  | "pricing"
  | "discounts"
  | "inventory"
  | "shipping"
  | "conditions"
  | "history";

export interface OfferInventoryLocation {
  stockItemId: string;
  locationId: string;
  locationCode: string;
  locationName: string;
  onHand: number;
  reserved: number;
  available: number;
}

/** Normalized detail used by both admin and seller workspace routes. */
export interface OfferWorkspaceDetail {
  offerId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  catalogVariantId: string;
  productId: string | null;
  productTitle: string;
  brandName: string | null;
  sellerSku: string | null;
  status: string;
  channel: string;
  amount: number | null;
  currency: string;
  onHand: number;
  reserved: number;
  availableUnits: number;
  catalogReadOnly: boolean;
  returnPolicyChoice: string;
  customReturnWindowDays: number | null;
  defaultReturnWindowDays: number;
  sellerCanOverrideReturnPolicy: boolean;
  minReturnWindowDays: number;
  maxReturnWindowDays: number;
  allowNonReturnableOffers: boolean;
  minimumOrderQuantity: number | null;
  maximumOrderQuantity: number | null;
  productUnitCode: string | null;
  productUnitName: string | null;
  productUnitShortName: string | null;
  unitQuantityDecimalPlaces: number;
  unitQuantityStep: number | null;
  unitRoundingMode: string | null;
  inventoryLocations: OfferInventoryLocation[];
  variantCode: string | null;
  variantLabel: string | null;
  primaryMediaAssetId: string | null;
  fulfillmentResponsibility: OfferFulfillmentResponsibility | null;
  fulfillmentShippingTypeId: string | null;
  lastUpdatedAt: string | null;
}

export interface OfferSellerOption {
  sellerPartyId: string;
  displayName: string;
}

export interface OfferProductOption {
  productId: string;
  title: string;
  brandName: string | null;
  primaryMediaAssetId: string | null;
  publishedVariantCount: number;
  unitCode?: string | null;
  unitName?: string | null;
  quantityDecimalPlaces?: number;
  quantityStep?: number | null;
  roundingMode?: string | null;
}

export interface OfferVariantOption {
  catalogVariantId: string;
  productId: string;
  productTitle: string;
  displayLabel: string;
  axisLabels: Array<{ name: string; value: string }>;
  catalogCode: string | null;
  status: string;
  primaryMediaAssetId?: string | null;
  brandName?: string | null;
}

export interface OfferShippingTypeOption {
  shippingTypeId: string;
  code: string;
  label: string;
  isActive: boolean;
}

export interface OfferInventoryLocationOption {
  locationId: string;
  code: string;
  name: string;
}

export interface OfferLocalInventoryDraft {
  locationId: string;
  onHand: string;
  reason: string;
}

export interface OfferHistoryItem {
  offerId: string;
  occurredAt: string;
  kind: string;
  actorUserId: string | null;
  summary: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface OfferHistoryPage {
  items: OfferHistoryItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OfferPickerPage<T> {
  rows: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface OfferCommercialPatch {
  sellerSku: string | null;
  status?: string | null;
  returnPolicyChoice?: string | null;
  customReturnWindowDays?: number | null;
  minimumOrderQuantity?: number | null;
  maximumOrderQuantity?: number | null;
  clearOrderQuantityLimits?: boolean;
}

export type OfferMutationResult =
  | { ok: true; detail: OfferWorkspaceDetail }
  | { ok: false; errorCode: string; denied?: boolean };

export type OfferCreateResult =
  | { ok: true; offerId: string }
  | { ok: false; errorCode: string; denied?: boolean };

export type OfferSimpleResult =
  | { ok: true }
  | { ok: false; errorCode: string; denied?: boolean };

export type OfferDetailResult =
  | { ok: true; detail: OfferWorkspaceDetail }
  | { ok: false; errorCode: string; denied?: boolean };

export type OfferFulfillmentResponsibility = "Seller" | "Platform";

/**
 * Role-specific adapter supplied by the route page. The shared workspace never calls HTTP itself.
 */
export interface OfferWorkspaceAdapter {
  role: OfferRole;
  mode: OfferMode;
  offerId?: string;
  fixedSeller?: OfferSellerOption;
  loadDetail?: () => Promise<OfferDetailResult>;
  searchSellers?: (search: string, page: number) => Promise<OfferPickerPage<OfferSellerOption>>;
  searchProducts: (search: string, page: number) => Promise<OfferPickerPage<OfferProductOption>>;
  loadProductVariants: (productId: string) => Promise<OfferVariantOption[]>;
  loadChannels?: () => Promise<string[]>;
  loadInventoryLocations?: () => Promise<OfferInventoryLocationOption[]>;
  loadHistory?: (page: number, pageSize: number) => Promise<OfferHistoryPage>;
  findActiveOffer?: (sellerPartyId: string, catalogVariantId: string, channel: string) => Promise<{ offerId: string } | null>;
  createDraft: (input: {
    sellerPartyId?: string;
    catalogVariantId: string;
    sellerSku: string | null;
    channel?: string;
  }) => Promise<OfferCreateResult>;
  createAndSaveDraft?: (input: {
    sellerPartyId?: string;
    catalogVariantId: string;
    sellerSku: string | null;
    channel: string;
    amount: number | null;
    currency: string;
    returnPolicyChoice: string;
    customReturnWindowDays: number | null;
    minimumOrderQuantity: number | null;
    maximumOrderQuantity: number | null;
    responsibility: OfferFulfillmentResponsibility | null;
    shippingTypeId: string | null;
    inventory: OfferLocalInventoryDraft[];
  }) => Promise<OfferCreateResult & { createdOfferId?: string; partial?: boolean }>;
  saveCommercial: (patch: OfferCommercialPatch) => Promise<OfferMutationResult>;
  savePrice: (input: { amount: number; currency: string }) => Promise<OfferMutationResult>;
  saveInventory: (input: {
    stockItemId: string;
    onHand: number;
    reason: string;
  }) => Promise<OfferMutationResult>;
  saveFulfillment?: (input: {
    responsibility: OfferFulfillmentResponsibility | null;
    shippingTypeId: string | null;
  }) => Promise<OfferSimpleResult>;
  loadShippingTypes?: () => Promise<OfferShippingTypeOption[]>;
  onCreated: (offerId: string) => void;
  onCancel: () => void;
}
