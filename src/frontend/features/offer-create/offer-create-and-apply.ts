import type { OfferFulfillmentResponsibility } from "../../lib/offer-workspace/types.ts";

export interface OfferCreateAndApplyInput {
  sellerPartyId?: string;
  catalogVariantId: string;
  sellerSku: string | null;
  amount: number | null;
  currency: string;
  returnPolicyChoice: string;
  customReturnWindowDays: number | null;
  minimumOrderQuantity: number | null;
  maximumOrderQuantity: number | null;
  responsibility: OfferFulfillmentResponsibility | null;
  shippingTypeId: string | null;
}

export interface OfferCreateAndApplyDetail {
  offerId: string;
  sellerPartyId: string;
  catalogVariantId: string;
  sellerSku: string | null;
  amount: number | null;
  currency: string;
  returnPolicyChoice: string;
  customReturnWindowDays: number | null;
  minimumOrderQuantity: number | null;
  maximumOrderQuantity: number | null;
  fulfillmentResponsibility: OfferFulfillmentResponsibility | null;
  fulfillmentShippingTypeId: string | null;
}

export interface OfferCreateAndApplyTransport {
  create: () => Promise<{ ok: true; offerId: string } | { ok: false; errorCode: string; denied?: boolean }>;
  load: (offerId: string) => Promise<{ ok: true; detail: OfferCreateAndApplyDetail } | { ok: false; errorCode: string }>;
  savePrice: (offerId: string, amount: number, currency: string) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  saveConditions: (offerId: string, input: OfferCreateAndApplyInput) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  saveFulfillment?: (offerId: string, input: OfferCreateAndApplyInput) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  onCommitted?: () => void;
}

export type OfferCreateAndApplyResult =
  | { ok: true; offerId: string }
  | { ok: false; errorCode: string; denied?: boolean; createdOfferId?: string; partial?: boolean };

function sameNumber(left: number | null, right: number | null): boolean {
  return left == null ? right == null : right != null && Number(left) === Number(right);
}

function matches(detail: OfferCreateAndApplyDetail, input: OfferCreateAndApplyInput): boolean {
  return detail.sellerPartyId !== ""
    && detail.catalogVariantId === input.catalogVariantId
    && (detail.sellerSku ?? null) === input.sellerSku
    && sameNumber(detail.amount, input.amount)
    && detail.currency === input.currency
    && detail.returnPolicyChoice === input.returnPolicyChoice
    && sameNumber(detail.customReturnWindowDays, input.customReturnWindowDays)
    && sameNumber(detail.minimumOrderQuantity, input.minimumOrderQuantity)
    && sameNumber(detail.maximumOrderQuantity, input.maximumOrderQuantity)
    && detail.fulfillmentResponsibility === input.responsibility
    && (detail.fulfillmentShippingTypeId ?? null) === (input.shippingTypeId ?? null);
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
    const conditionsChanged = input.returnPolicyChoice !== "Default"
      || input.customReturnWindowDays != null
      || input.minimumOrderQuantity != null
      || input.maximumOrderQuantity != null;
    if (conditionsChanged) {
      const conditions = await transport.saveConditions(offerId, input);
      if (!conditions.ok) return { ok: false, errorCode: conditions.errorCode, createdOfferId: offerId, partial: true };
    }
    if (transport.saveFulfillment && input.responsibility) {
      const fulfillment = await transport.saveFulfillment(offerId, input);
      if (!fulfillment.ok) return { ok: false, errorCode: fulfillment.errorCode, createdOfferId: offerId, partial: true };
    }
    const reread = await transport.load(offerId);
    if (!reread.ok) return { ok: false, errorCode: reread.errorCode, createdOfferId: offerId, partial: true };
    if (!matches(reread.detail, input)) return { ok: false, errorCode: "offer.create.response_mismatch", createdOfferId: offerId, partial: true };
    transport.onCommitted?.();
    return { ok: true, offerId };
  } catch {
    return { ok: false, errorCode: "host-unreachable", createdOfferId: offerId, partial: true };
  }
}
