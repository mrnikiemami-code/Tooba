import type { OfferFulfillmentResponsibility, OfferLocalInventoryDraft } from "../../lib/offer-workspace/types.ts";

export interface OfferCreateAndApplyInput {
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
  inventory: Array<{ locationId: string; onHand: number; reason: string }>;
}

export interface OfferCreateAndApplyDetail {
  offerId: string;
  sellerPartyId: string;
  catalogVariantId: string;
  sellerSku: string | null;
  channel: string;
  amount: number | null;
  currency: string;
  returnPolicyChoice: string;
  customReturnWindowDays: number | null;
  minimumOrderQuantity: number | null;
  maximumOrderQuantity: number | null;
  fulfillmentResponsibility: OfferFulfillmentResponsibility | null;
  fulfillmentShippingTypeId: string | null;
  status: string;
  inventoryLocations: Array<{ locationId: string; onHand: number }>;
}

export interface OfferCreateAndApplyTransport {
  create: () => Promise<{ ok: true; offerId: string } | { ok: false; errorCode: string; denied?: boolean }>;
  load: (offerId: string) => Promise<{ ok: true; detail: OfferCreateAndApplyDetail } | { ok: false; errorCode: string }>;
  savePrice: (offerId: string, amount: number, currency: string) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  saveConditions: (offerId: string, input: OfferCreateAndApplyInput) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  provisionPosition: (offerId: string, locationId: string) => Promise<{ ok: true; stockItemId: string } | { ok: false; errorCode: string }>;
  saveInventory: (offerId: string, stockItemId: string, onHand: number, reason: string) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  saveFulfillment?: (offerId: string, input: OfferCreateAndApplyInput) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  onCommitted?: () => void;
}

export type OfferCreateAndApplyResult =
  | { ok: true; offerId: string }
  | { ok: false; errorCode: string; denied?: boolean; createdOfferId?: string; partial?: boolean };

function sameNumber(left: number | null, right: number | null): boolean {
  return left == null ? right == null : right != null && Number(left) === Number(right);
}

function matches(detail: OfferCreateAndApplyDetail, input: OfferCreateAndApplyInput, offerId: string): boolean {
  return detail.offerId === offerId
    && detail.sellerPartyId !== ""
    && (!input.sellerPartyId || detail.sellerPartyId === input.sellerPartyId)
    && detail.status === "Draft"
    && detail.catalogVariantId === input.catalogVariantId
    && (detail.sellerSku ?? null) === input.sellerSku
    && sameNumber(detail.amount, input.amount)
    && detail.channel === input.channel
    && detail.currency === input.currency
    && detail.returnPolicyChoice === input.returnPolicyChoice
    && sameNumber(detail.customReturnWindowDays, input.customReturnWindowDays)
    && sameNumber(detail.minimumOrderQuantity, input.minimumOrderQuantity)
    && sameNumber(detail.maximumOrderQuantity, input.maximumOrderQuantity)
    && detail.fulfillmentResponsibility === input.responsibility
    && (detail.fulfillmentShippingTypeId ?? null) === (input.shippingTypeId ?? null)
    && input.inventory.every((position) => detail.inventoryLocations.some((row) => row.locationId === position.locationId && row.onHand === position.onHand));
}

/** Shared create-and-apply use case; route pages provide only role-specific transport. */
export async function createAndApplyOffer(
  transport: OfferCreateAndApplyTransport,
  input: OfferCreateAndApplyInput,
): Promise<OfferCreateAndApplyResult> {
  const created = await transport.create();
  if (!created.ok) return created;
  const offerId = created.offerId;
  try {
    if (input.amount != null) {
      const price = await transport.savePrice(offerId, input.amount, input.currency);
      if (!price.ok) return { ok: false, errorCode: price.errorCode, createdOfferId: offerId, partial: true };
    }
    for (const position of input.inventory) {
      const provisioned = await transport.provisionPosition(offerId, position.locationId);
      if (!provisioned.ok) return { ok: false, errorCode: provisioned.errorCode, createdOfferId: offerId, partial: true };
      const saved = await transport.saveInventory(offerId, provisioned.stockItemId, position.onHand, position.reason);
      if (!saved.ok) return { ok: false, errorCode: saved.errorCode, createdOfferId: offerId, partial: true };
    }
    if (transport.saveFulfillment && input.responsibility) {
      const fulfillment = await transport.saveFulfillment(offerId, input);
      if (!fulfillment.ok) return { ok: false, errorCode: fulfillment.errorCode, createdOfferId: offerId, partial: true };
    }
    const reread = await transport.load(offerId);
    if (!reread.ok) return { ok: false, errorCode: reread.errorCode, createdOfferId: offerId, partial: true };
    if (!matches(reread.detail, input, offerId)) return { ok: false, errorCode: "offer.create.response_mismatch", createdOfferId: offerId, partial: true };
    transport.onCommitted?.();
    return { ok: true, offerId };
  } catch {
    return { ok: false, errorCode: "host-unreachable", createdOfferId: offerId, partial: true };
  }
}
