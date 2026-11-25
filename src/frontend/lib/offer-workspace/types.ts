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
  inventoryLocations: OfferInventoryLocation[];
}

export interface OfferSellerOption {
  sellerPartyId: string;
  displayName: string;
}

export interface OfferVariantOption {
  catalogVariantId: string;
  productId: string;
  productTitle: string;
  catalogCode: string | null;
  productStatus: string;
}

export interface OfferShippingTypeOption {
  shippingTypeId: string;
  code: string;
  label: string;
  isActive: boolean;
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
  searchVariants: (search: string, page: number) => Promise<OfferPickerPage<OfferVariantOption>>;
  createDraft: (input: {
    sellerPartyId?: string;
    catalogVariantId: string;
    sellerSku: string | null;
  }) => Promise<OfferCreateResult>;
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
