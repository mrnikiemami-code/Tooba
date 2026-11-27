import type { OfferFulfillmentResponsibility } from "../../lib/offer-workspace/types.ts";
import { ownerWriteIntent } from "../../lib/offer-workspace/owner-write-intent.ts";

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
  inventoryLocations: Array<{ locationId: string; stockItemId: string; onHand: number; revision: string | null }>;
  revisions: { offer: string; price: string; offeredQuantity: string } | null;
  capabilities: { canEditPhysicalInventory: boolean; canAssignFulfillment: boolean } | null;
}

export interface OfferCreateAndApplyTransport {
  scope: string;
  create: () => Promise<{ ok: true; offerId: string } | { ok: false; errorCode: string; denied?: boolean }>;
  load: (offerId: string) => Promise<{ ok: true; detail: OfferCreateAndApplyDetail } | { ok: false; errorCode: string }>;
  savePrice: (offerId: string, amount: number, currency: string, version: string, operationId: string) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  saveConditions: (offerId: string, input: OfferCreateAndApplyInput, version: string) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  provisionPosition: (offerId: string, locationId: string) => Promise<{ ok: true; stockItemId: string } | { ok: false; errorCode: string }>;
  saveInventory: (offerId: string, stockItemId: string, onHand: number, reason: string, version: string, operationId: string) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
  saveFulfillment?: (offerId: string, input: OfferCreateAndApplyInput, version: string) => Promise<{ ok: true } | { ok: false; errorCode: string }>;
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
    && (!input.currency || detail.currency === input.currency)
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
  const partial = (errorCode: string): OfferCreateAndApplyResult => ({ ok: false, errorCode, createdOfferId: offerId, partial: true });
  const read = async () => {
    const result = await transport.load(offerId);
    if (!result.ok) throw new Error(result.errorCode);
    const detail = result.detail;
    if (detail.offerId !== offerId || detail.sellerPartyId !== input.sellerPartyId || detail.catalogVariantId !== input.catalogVariantId || detail.channel !== input.channel || detail.status !== "Draft")
      throw new Error("offer.create.response_mismatch");
    return detail;
  };
  try {
    let current = await read();
    // Initial commercial fields belong to the idempotent create. Do not silently rewrite
    // someone else's modified Draft after an unknown creation response.
    if ((current.sellerSku ?? null) !== input.sellerSku || current.returnPolicyChoice !== input.returnPolicyChoice
        || !sameNumber(current.customReturnWindowDays, input.customReturnWindowDays)
        || !sameNumber(current.minimumOrderQuantity, input.minimumOrderQuantity)
        || !sameNumber(current.maximumOrderQuantity, input.maximumOrderQuantity))
      return partial("offer.create.response_mismatch");
    if (transport.saveFulfillment && (current.fulfillmentResponsibility !== input.responsibility || (current.fulfillmentShippingTypeId ?? null) !== input.shippingTypeId)) {
      if (!current.capabilities?.canAssignFulfillment || !current.revisions?.offer) return partial("denied");
      const result = await transport.saveFulfillment(offerId, input, current.revisions.offer);
      if (!result.ok) return partial(result.errorCode);
      current = await read();
      if (current.fulfillmentResponsibility !== input.responsibility || (current.fulfillmentShippingTypeId ?? null) !== input.shippingTypeId)
        return partial("offer.create.response_mismatch");
    }
    const currency = input.currency || current.currency;
    if (input.amount != null && !currency) return partial("offer.price.invalid");
    if (input.amount != null && currency === "IRR" && !Number.isInteger(input.amount)) return partial("offer.price.decimals_not_allowed");
    if (input.amount != null && (current.amount !== input.amount || current.currency !== currency)) {
      const revision = current.revisions?.price;
      if (!revision) return partial("concurrency.conflict");
      const intent = ownerWriteIntent(transport.scope + ":price:" + offerId, revision, [input.amount, currency]);
      const price = await transport.savePrice(offerId, input.amount, currency, revision, intent.operationId);
      if (!price.ok) return partial(price.errorCode);
      current = await read();
      if (current.amount !== input.amount || current.currency !== currency) return partial("offer.create.response_mismatch");
      intent.clear();
    }
    for (const position of input.inventory) {
      if (current.capabilities?.canEditPhysicalInventory !== true) return partial("denied");
      let target = current.inventoryLocations.find((row) => row.locationId === position.locationId);
      if (!target) {
        const provisioned = await transport.provisionPosition(offerId, position.locationId);
        if (!provisioned.ok) return partial(provisioned.errorCode);
        current = await read();
        target = current.inventoryLocations.find((row) => row.locationId === position.locationId && row.stockItemId === provisioned.stockItemId);
        if (!target) return partial("offer.create.response_mismatch");
      }
      if (target.onHand === position.onHand) continue;
      if (!target.revision) return partial("concurrency.conflict");
      const intent = ownerWriteIntent(transport.scope + ":physical:" + target.stockItemId, target.revision, [position.onHand, position.reason]);
      const saved = await transport.saveInventory(offerId, target.stockItemId, position.onHand, position.reason, target.revision, intent.operationId);
      if (!saved.ok) return partial(saved.errorCode);
      current = await read();
      if (!current.inventoryLocations.some((row) => row.stockItemId === target!.stockItemId && row.locationId === position.locationId && row.onHand === position.onHand))
        return partial("offer.create.response_mismatch");
      intent.clear();
    }
    const final = await read();
    if (!matches(final, { ...input, currency }, offerId)) return partial("offer.create.response_mismatch");
    transport.onCommitted?.();
    return { ok: true, offerId };
  } catch (error) {
    return partial(error instanceof Error && (error.message.includes(".") || error.message === "host-unreachable" || error.message === "denied") ? error.message : "host-unreachable");
  }
}
