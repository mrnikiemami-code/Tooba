import type { OfferFulfillmentResponsibility, OfferPriceView, OfferShippingSelectionMode } from "../../lib/offer-workspace/types.ts";
import { ownerWriteIntent } from "../../lib/offer-workspace/owner-write-intent.ts";
import { priceRowFor, priceRevisionFor } from "../../lib/offer-workspace/model.ts";

export interface OfferCreateAndApplyInput {
  sellerPartyId?: string;
  catalogVariantId: string;
  sellerSku: string | null;
  channel: string;
  amount: number | null;
  currency: string;
  prices?: Array<{ currency: string; amount: number }>;
  returnPolicyChoice: string;
  customReturnWindowDays: number | null;
  minimumOrderQuantity: number | null;
  maximumOrderQuantity: number | null;
  responsibility: OfferFulfillmentResponsibility | null;
  shippingTypeId: string | null;
  shippingMode: OfferShippingSelectionMode;
  shippingMethodIds: string[];
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
  prices?: OfferPriceView[];
  priceRevision?: string | null;
  returnPolicyChoice: string;
  customReturnWindowDays: number | null;
  minimumOrderQuantity: number | null;
  maximumOrderQuantity: number | null;
  fulfillmentResponsibility: OfferFulfillmentResponsibility | null;
  fulfillmentShippingTypeId: string | null;
  fulfillmentShippingMode?: OfferShippingSelectionMode;
  fulfillmentShippingMethodIds?: string[];
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

function sameShipping(detail: OfferCreateAndApplyDetail, input: OfferCreateAndApplyInput): boolean {
  return detail.fulfillmentResponsibility === input.responsibility
    && (detail.fulfillmentShippingTypeId ?? null) === (input.shippingTypeId ?? null)
    && (detail.fulfillmentShippingMode ?? "Automatic") === input.shippingMode
    && JSON.stringify([...(detail.fulfillmentShippingMethodIds ?? [])].sort())
      === JSON.stringify([...input.shippingMethodIds].sort());
}

function matches(detail: OfferCreateAndApplyDetail, input: OfferCreateAndApplyInput, offerId: string): boolean {
  return detail.offerId === offerId
    && detail.sellerPartyId !== ""
    && (!input.sellerPartyId || detail.sellerPartyId === input.sellerPartyId)
    && detail.status === "Draft"
    && detail.catalogVariantId === input.catalogVariantId
    && (detail.sellerSku ?? null) === input.sellerSku
    && (input.prices ? input.prices.every(({ currency, amount }) => priceMatches(detail, currency, amount)) : sameNumber(priceRowFor(detail, input.currency || detail.currency)?.amount ?? null, input.amount))
    && detail.channel === input.channel
    && detail.returnPolicyChoice === input.returnPolicyChoice
    && sameNumber(detail.customReturnWindowDays, input.customReturnWindowDays)
    && sameNumber(detail.minimumOrderQuantity, input.minimumOrderQuantity)
    && sameNumber(detail.maximumOrderQuantity, input.maximumOrderQuantity)
    && sameShipping(detail, input)
    && input.inventory.every((position) => detail.inventoryLocations.some((row) => row.locationId === position.locationId && row.onHand === position.onHand));
}

function priceMatches(detail: OfferCreateAndApplyDetail, currency: string, amount: number) {
  const row = priceRowFor(detail, currency);
  return row != null && row.status !== "Retired" && row.amount === amount;
}

/** Shared create-and-apply use case; route pages provide only role-specific transport. */
export async function createAndApplyOffer(
  transport: OfferCreateAndApplyTransport,
  input: OfferCreateAndApplyInput,
): Promise<OfferCreateAndApplyResult> {
  if (input.prices && (new Set(input.prices.map(row => row.currency.toUpperCase())).size !== input.prices.length
    || input.prices.some(row => !row.currency.trim() || !Number.isFinite(row.amount) || row.amount < 0 || (row.currency.toUpperCase() === "IRR" && !Number.isInteger(row.amount)))))
    return { ok: false, errorCode: "offer.price.invalid" };
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
    if (transport.saveFulfillment && !sameShipping(current, input)) {
      if (!current.capabilities?.canAssignFulfillment || !current.revisions?.offer) return partial("denied");
      const result = await transport.saveFulfillment(offerId, input, current.revisions.offer);
      if (!result.ok) return partial(result.errorCode);
      current = await read();
      if (!sameShipping(current, input)) return partial("offer.create.response_mismatch");
    }
    const currency = input.currency || current.currency;
    const intendedPrices = input.prices ?? (input.amount == null ? [] : [{ currency, amount: input.amount }]);
    for (const entry of intendedPrices) {
      const code = entry.currency.toUpperCase();
      if (!code) return partial("offer.price.invalid");
      if (code === "IRR" && !Number.isInteger(entry.amount)) return partial("offer.price.decimals_not_allowed");
      if (priceMatches(current, code, entry.amount)) continue;
      const revision = priceRevisionFor(current, code);
      if (!revision) return partial("concurrency.conflict");
      const intent = ownerWriteIntent(transport.scope + ":price:" + offerId + ":" + code, revision, [entry.amount, code]);
      const price = await transport.savePrice(offerId, entry.amount, code, revision, intent.operationId);
      if (!price.ok) return partial(price.errorCode);
      current = await read();
      if (!priceMatches(current, code, entry.amount)) return partial("offer.create.response_mismatch");
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
