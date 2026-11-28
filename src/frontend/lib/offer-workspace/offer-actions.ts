import type { MutableRefObject } from "react";
import { offerIntentScope, ownerWriteIntent } from "./owner-write-intent.ts";
import { draftFromDetail, changedInventoryRows, detailVersionChanged, parseDecimal, priceRevisionFor, priceRowFor, quantityAligned, validateDraft, validatePrice, type OfferWorkspaceDraft } from "./model.ts";
import { offerErrorText, type OfferWorkspaceMessages } from "./messages.ts";
import type {
  OfferCommercialPatch,
  OfferFulfillmentResponsibility,
  OfferProductOption,
  OfferSellerOption,
  OfferTabId,
  OfferVariantOption,
  OfferWorkspaceAdapter,
  OfferWorkspaceDetail,
} from "./types.ts";

export interface OfferActionsContext {
  adapter: OfferWorkspaceAdapter;
  t: OfferWorkspaceMessages;
  isEdit: boolean;
  detail: OfferWorkspaceDetail | null;
  draft: OfferWorkspaceDraft;
  selectedSeller: OfferSellerOption | null;
  selectedProduct: OfferProductOption | null;
  selectedVariant: OfferVariantOption | null;
  selectedChannel: string;
  localInventory: import("./types.ts").OfferLocalInventoryDraft[];
  createReady: boolean;
  identityReady: boolean;
  activeConflict: { offerId: string } | null;
  saving: boolean;
  saveRef: MutableRefObject<boolean>;
  saveNeedsReload: boolean;
  saveReloading: boolean;
  priceChanged: boolean;
  commercialChanged: boolean;
  stockChanged: boolean;
  offeredChanged: boolean;
  shippingChanged: boolean;
  responsibility: OfferFulfillmentResponsibility | null;
  shippingTypeId: string;
  setDraft: (updater: (current: OfferWorkspaceDraft) => OfferWorkspaceDraft) => void;
  setDetail: (detail: OfferWorkspaceDetail) => void;
  setSaving: (value: boolean) => void;
  setSaveError: (value: string | null) => void;
  setSaveNotice: (value: string | null) => void;
  setSaveNeedsReload: (value: boolean) => void;
  setSaveRetryable: (value: boolean) => void;
  setSaveReloading: (value: boolean) => void;
  setTabError: (value: { tab: OfferTabId; code: string } | null) => void;
  setActiveTab: (tab: OfferTabId) => void;
  setActiveConflict: (value: { offerId: string } | null) => void;
  setShippingSaving: (value: boolean) => void;
  setShippingError: (value: string | null) => void;
  setShippingNotice: (value: string | null) => void;
  setShowCreateErrors: (value: boolean) => void;
  showBlocking: (kind: "duplicate" | "reload" | "error" | "confirm", title: string, text: string, offerId?: string) => void;
  applyLoadedDetail: (detail: OfferWorkspaceDetail) => void;
  completeCreate: (offerId: string) => void;
}

export function createOfferActions(ctx: OfferActionsContext) {
  function applyServerDetail(next: OfferWorkspaceDetail) {
    ctx.setDetail(next);
  }

  function applySavedDetail(next: OfferWorkspaceDetail, domain: "commercial" | "price") {
    ctx.setDetail(next);
    ctx.setDraft((current) => {
      if (domain === "commercial") {
        return {
          ...current,
          sellerSku: next.sellerSku ?? "",
          status: next.status,
          returnPolicyChoice: next.returnPolicyChoice,
          customReturnWindowDays: next.customReturnWindowDays != null ? String(next.customReturnWindowDays) : "",
          minimumOrderQuantity: next.minimumOrderQuantity != null ? String(next.minimumOrderQuantity) : "",
          maximumOrderQuantity: next.maximumOrderQuantity != null ? String(next.maximumOrderQuantity) : "",
        };
      }
      const saved = priceRowFor(next, current.currency);
      return { ...current, amount: saved ? String(saved.amount) : "", currency: current.currency || next.currency };
    });
  }

  function applySavedInventory(next: OfferWorkspaceDetail, stockItemId: string) {
    ctx.setDetail(next);
    ctx.setDraft((current) => {
      const saved = next.inventoryLocations.find((row) => row.stockItemId === stockItemId);
      return { ...current, inventoryOnHand: { ...current.inventoryOnHand, [stockItemId]: saved ? String(saved.onHand) : current.inventoryOnHand[stockItemId] ?? "" } };
    });
  }

  function finishSaveFailure(tab: OfferTabId, result: { errorCode: string; denied?: boolean }, savedAny: boolean) {
    const transient = result.errorCode === "host-unreachable" || result.errorCode === "concurrency.conflict" || result.errorCode.includes("revision_conflict") || result.errorCode.includes("response_mismatch");
    ctx.setActiveTab(tab);
    ctx.setTabError({ tab, code: result.errorCode || "save" });
    ctx.setSaveNeedsReload(transient);
    ctx.setSaveRetryable(!transient);
    const text = transient ? ctx.t.saveUnknown : result.denied ? ctx.t.denied : offerErrorText(ctx.t, result.errorCode);
    ctx.showBlocking(transient ? "reload" : "error", savedAny ? ctx.t.partialTitle : ctx.t.saveErrorTitle, savedAny ? `${text} ${ctx.t.partialSaved}` : text);
    if (savedAny) {
      ctx.setSaveNotice(ctx.t.partialSaved);
      ctx.setSaveRetryable(!transient);
    }
  }

  async function reloadAfterUnknown() {
    if (ctx.saveReloading) return;
    if (!ctx.isEdit) { await runCreate(); return; }
    if (!ctx.adapter.loadDetail || !ctx.detail) return;
    ctx.setSaveReloading(true);
    try {
      const result = await ctx.adapter.loadDetail();
      if (!result.ok) {
        ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.saveUnknown);
        return;
      }
      // Only merge fields whose server value is unchanged or already matches our intent.
      const merged = reconcileDraft(ctx.detail, result.detail, ctx.draft);
      const oldShipping = [ctx.detail.fulfillmentResponsibility, ctx.detail.fulfillmentShippingTypeId ?? ""];
      const nextShipping = [result.detail.fulfillmentResponsibility, result.detail.fulfillmentShippingTypeId ?? ""];
      const wantedShipping = [ctx.responsibility, ctx.shippingTypeId];
      const shippingSafe = JSON.stringify(oldShipping) === JSON.stringify(nextShipping) || JSON.stringify(nextShipping) === JSON.stringify(wantedShipping);
      if (!merged || !shippingSafe) {
        ctx.showBlocking("error", ctx.t.saveErrorTitle, ctx.t.concurrentChange);
        return;
      }
      applyServerDetail(result.detail);
      ctx.setDraft(() => merged);
      ctx.setSaveNeedsReload(false);
      ctx.setSaveRetryable(true);
      ctx.setSaveError(null);
      ctx.setSaveNotice(ctx.t.reloaded);
    } catch {
      ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.saveUnknown);
    } finally {
      ctx.setSaveReloading(false);
    }
  }

  async function runSave() {
    if (!ctx.detail || ctx.saving || ctx.saveRef.current) return;
    if ((ctx.shippingChanged && ctx.detail.capabilities?.canAssignFulfillment !== true) || (ctx.stockChanged && ctx.detail.capabilities?.canEditPhysicalInventory !== true) || (ctx.offeredChanged && (ctx.detail.capabilities?.canEditOfferedQuantity !== true || !ctx.adapter.saveOfferedQuantity))) {
      ctx.showBlocking("error", ctx.t.saveErrorTitle, ctx.t.denied); return;
    }
    if (ctx.saveNeedsReload) {
      ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.concurrentChange);
      return;
    }
    const errors = validateDraft(ctx.detail, ctx.draft);
    if (errors.length > 0) {
      const first = errors[0]!;
      ctx.setTabError({ tab: first.tab, code: first.code });
      ctx.setActiveTab(first.tab);
      ctx.setSaveError(offerErrorText(ctx.t, first.code));
      ctx.setSaveRetryable(true);
      return;
    }
    if (ctx.priceChanged && ctx.draft.amount.trim().length === 0) {
      ctx.setTabError({ tab: "pricing", code: "offer.price.required" });
      ctx.setActiveTab("pricing");
      ctx.setSaveError(ctx.t.priceRequired);
      ctx.setSaveRetryable(true);
      return;
    }
    if (!ctx.commercialChanged && !ctx.priceChanged && !ctx.stockChanged && !ctx.offeredChanged && !ctx.shippingChanged) {
      ctx.setSaveNotice(ctx.t.noChanges);
      ctx.setSaveError(null);
      ctx.setTabError(null);
      return;
    }

    ctx.saveRef.current = true;
    ctx.setSaving(true);
    ctx.setSaveError(null);
    ctx.setSaveNotice(null);
    ctx.setTabError(null);
    ctx.setShippingError(null);
    ctx.setSaveNeedsReload(false);
    ctx.setSaveRetryable(true);
    let savedAny = false;
    let current = ctx.detail;
    const expectedAmount = parseDecimal(ctx.draft.amount);
    const expectedInventory = new Map<string, number | null>();
    if (ctx.stockChanged) {
      for (const row of changedInventoryRows(ctx.detail, ctx.draft)) expectedInventory.set(row.stockItemId, parseDecimal(ctx.draft.inventoryOnHand[row.stockItemId]));
    }

    try {
      const scope = offerIntentScope(ctx.adapter.role, current.sellerPartyId, current.offerId, current.channel);
      if (ctx.adapter.loadDetail) {
        const latest = await ctx.adapter.loadDetail();
        if (!latest.ok) {
          ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.saveUnknown);
          ctx.setSaveNeedsReload(true);
          ctx.setSaveRetryable(false);
          return;
        }
        if (detailVersionChanged(ctx.detail, latest.detail)) {
          ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.concurrentChange);
          ctx.setSaveNeedsReload(true);
          ctx.setSaveRetryable(false);
          return;
        }
      }

      if (ctx.commercialChanged) {
        const revision = current.revisions?.offer ?? current.offerRevision;
        if (!revision) return finishSaveFailure("conditions", { errorCode: "concurrency.conflict" }, savedAny);
        const result = await ctx.adapter.saveCommercial(buildCommercialPatch(current, ctx.draft), revision);
        if (!result.ok) return finishSaveFailure("conditions", result, savedAny);
        current = result.detail;
        applySavedDetail(result.detail, "commercial");
        savedAny = true;
      }

      if (ctx.priceChanged) {
        if (expectedAmount == null) {
          ctx.setActiveTab("pricing");
          ctx.setSaveError(ctx.t.validationAmount);
          ctx.setSaveRetryable(true);
          return;
        }
        const revision = priceRevisionFor(current, ctx.draft.currency);
        if (!revision) return finishSaveFailure("pricing", { errorCode: "concurrency.conflict" }, savedAny);
        const intent = ownerWriteIntent(scope + ":price", revision, [expectedAmount, ctx.draft.currency]);
        const result = await ctx.adapter.savePrice({ amount: expectedAmount, currency: ctx.draft.currency, operationId: intent.operationId }, revision);
        if (!result.ok) return finishSaveFailure("pricing", result, savedAny);
        if ((priceRowFor(result.detail, ctx.draft.currency)?.amount ?? null) !== expectedAmount) return finishSaveFailure("pricing", { errorCode: "offer.price.response_mismatch" }, savedAny);
        intent.clear(); current = result.detail;
        applySavedDetail(result.detail, "price");
        savedAny = true;
      }

      if (ctx.offeredChanged) {
        const quantity = parseDecimal(ctx.draft.offeredQuantity);
        if (quantity == null || quantity < 0 || !ctx.adapter.saveOfferedQuantity) {
          ctx.setActiveTab("inventory");
          ctx.setSaveError(ctx.t.validationQty);
          ctx.setSaveRetryable(true);
          return;
        }
        const revision = current.revisions?.offeredQuantity ?? current.allocationRevision;
        if (!revision) return finishSaveFailure("inventory", { errorCode: "concurrency.conflict" }, savedAny);
        const intent = ownerWriteIntent(scope + ":allocation", revision, quantity);
        const result = await ctx.adapter.saveOfferedQuantity(quantity, revision, intent.operationId);
        if (!result.ok) return finishSaveFailure("inventory", result, savedAny);
        if (result.detail.offeredQuantity !== quantity) return finishSaveFailure("inventory", { errorCode: "offer.allocation.response_mismatch" }, savedAny);
        intent.clear(); current = result.detail;
        ctx.setDetail(result.detail);
        ctx.setDraft((current) => ({ ...current, offeredQuantity: result.detail.offeredQuantity != null ? String(result.detail.offeredQuantity) : "" }));
        savedAny = true;
      }

      if (ctx.stockChanged) {
        const changedRows = changedInventoryRows(ctx.detail, ctx.draft);
        for (const row of changedRows) {
          const revision = current.inventoryLocations.find((item) => item.stockItemId === row.stockItemId)?.revision;
          if (!revision) return finishSaveFailure("inventory", { errorCode: "concurrency.conflict" }, savedAny);
          const onHand = parseDecimal(ctx.draft.inventoryOnHand[row.stockItemId])!;
          const reason = ctx.draft.inventoryReason.trim();
          const intent = ownerWriteIntent(scope + ":physical:" + row.stockItemId, revision, [onHand, reason]);
          const result = await ctx.adapter.saveInventory({ stockItemId: row.stockItemId, onHand, reason, operationId: intent.operationId }, revision);
          if (!result.ok) return finishSaveFailure("inventory", result, savedAny);
          if (result.detail.inventoryLocations.find((item) => item.stockItemId === row.stockItemId)?.onHand !== onHand) return finishSaveFailure("inventory", { errorCode: "offer.inventory.response_mismatch" }, savedAny);
          intent.clear(); current = result.detail;
          applySavedInventory(result.detail, row.stockItemId);
          savedAny = true;
        }
        ctx.setDraft((current) => ({ ...current, inventoryReason: "" }));
      }

      if (ctx.shippingChanged && ctx.adapter.saveFulfillment) {
        ctx.setShippingSaving(true);
        const revision = current.revisions?.offer ?? current.offerRevision;
        if (!revision) { ctx.setShippingSaving(false); return finishSaveFailure("shipping", { errorCode: "concurrency.conflict" }, savedAny); }
        const result = await ctx.adapter.saveFulfillment({ responsibility: ctx.responsibility, shippingTypeId: ctx.shippingTypeId || null }, revision);
        ctx.setShippingSaving(false);
        if (!result.ok) return finishSaveFailure("shipping", result, savedAny);
        const verification = ctx.adapter.loadDetail ? await ctx.adapter.loadDetail() : null;
        if (!verification || !verification.ok || verification.detail.fulfillmentResponsibility !== ctx.responsibility || (verification.detail.fulfillmentShippingTypeId ?? null) !== (ctx.shippingTypeId || null)) {
          ctx.setShippingError(ctx.t.perTabError);
          return finishSaveFailure("shipping", { errorCode: "seller.shipping.response_mismatch" }, savedAny);
        }
        ctx.setDetail(verification.detail);
        ctx.setShippingNotice(ctx.t.shippingSaved);
        savedAny = true;
      }

      if (ctx.adapter.loadDetail) {
        const verification = await ctx.adapter.loadDetail();
        if (!verification.ok) {
          ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.saveUnknown);
          ctx.setSaveNeedsReload(true);
          ctx.setSaveRetryable(false);
          return;
        }
        const expectedMin = ctx.draft.minimumOrderQuantity.trim() ? parseDecimal(ctx.draft.minimumOrderQuantity) : null;
        const expectedMax = ctx.draft.maximumOrderQuantity.trim() ? parseDecimal(ctx.draft.maximumOrderQuantity) : null;
        const expectedDays = ctx.draft.returnPolicyChoice === "Custom" ? parseDecimal(ctx.draft.customReturnWindowDays) : null;
        const commercialMismatch = ctx.commercialChanged && (
          (verification.detail.sellerSku ?? null) !== (ctx.draft.sellerSku.trim() || null)
          || verification.detail.status !== ctx.draft.status
          || verification.detail.returnPolicyChoice !== ctx.draft.returnPolicyChoice
          || (verification.detail.customReturnWindowDays ?? null) !== expectedDays
          || (verification.detail.minimumOrderQuantity ?? null) !== expectedMin
          || (verification.detail.maximumOrderQuantity ?? null) !== expectedMax);
        const mismatch =
          verification.detail.offerId !== ctx.detail.offerId
          || verification.detail.sellerPartyId !== ctx.detail.sellerPartyId
          || verification.detail.catalogVariantId !== ctx.detail.catalogVariantId
          || verification.detail.channel !== ctx.detail.channel
          || commercialMismatch
          || (ctx.offeredChanged && verification.detail.offeredQuantity !== parseDecimal(ctx.draft.offeredQuantity))
          || (ctx.priceChanged && (priceRowFor(verification.detail, ctx.draft.currency)?.amount ?? null) !== expectedAmount)
          || (ctx.shippingChanged && (verification.detail.fulfillmentResponsibility !== ctx.responsibility || (verification.detail.fulfillmentShippingTypeId ?? null) !== (ctx.shippingTypeId || null)))
          || (ctx.stockChanged && ctx.detail.inventoryLocations.some((row) => {
            const expected = expectedInventory.get(row.stockItemId);
            if (expected == null) return false;
            const actual = verification.detail.inventoryLocations.find((item) => item.stockItemId === row.stockItemId);
            return !actual || actual.onHand !== expected;
          }));
        if (mismatch) {
          ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.saveUnknown);
          ctx.setSaveNeedsReload(true);
          ctx.setSaveRetryable(false);
          return;
        }
        ctx.applyLoadedDetail(verification.detail);
      }

      if (ctx.shippingChanged) ctx.setShippingNotice(ctx.t.shippingSaved);
      ctx.setSaveNotice(ctx.t.saved);
      ctx.setSaveError(null);
      ctx.setTabError(null);
    } catch {
      ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.saveUnknown);
      ctx.setSaveNeedsReload(true);
      ctx.setSaveRetryable(false);
    } finally {
      ctx.saveRef.current = false;
      ctx.setSaving(false);
      ctx.setShippingSaving(false);
    }
  }

  async function runCreate() {
    if (ctx.saving || ctx.saveRef.current) return;
    if (ctx.activeConflict) {
      ctx.showBlocking("duplicate", ctx.t.duplicateActiveTitle, ctx.t.duplicateActive);
      return;
    }
    if (!ctx.createReady || (ctx.adapter.role === "admin" && !ctx.selectedSeller) || !ctx.selectedVariant) {
      ctx.setShowCreateErrors(true);
      ctx.setSaveRetryable(true);
      return;
    }
    const parsedAmount = ctx.draft.amount.trim() ? parseDecimal(ctx.draft.amount) : null;
    if (ctx.priceChanged && (parsedAmount == null || validatePrice(ctx.draft.amount, ctx.draft.currency))) {
      ctx.setActiveTab("pricing"); ctx.setSaveError(ctx.t.validationAmount); return;
    }
    const parsedMin = ctx.draft.minimumOrderQuantity.trim() ? parseDecimal(ctx.draft.minimumOrderQuantity) : null;
    const parsedMax = ctx.draft.maximumOrderQuantity.trim() ? parseDecimal(ctx.draft.maximumOrderQuantity) : null;
    if ((ctx.draft.minimumOrderQuantity.trim() && parsedMin == null) || (ctx.draft.maximumOrderQuantity.trim() && parsedMax == null)) {
      ctx.setActiveTab("conditions"); ctx.setSaveError(ctx.t.validationQty); return;
    }
    const dp = ctx.selectedProduct?.quantityDecimalPlaces ?? 0;
    const step = ctx.selectedProduct?.quantityStep ?? null;
    if ((parsedMin != null && !quantityAligned(parsedMin, dp, step)) || (parsedMax != null && !quantityAligned(parsedMax, dp, step))) {
      ctx.setActiveTab("conditions"); ctx.setSaveError(ctx.t.validationStep); return;
    }
    const inventoryInputs = ctx.localInventory.filter((row) => row.onHand.trim().length > 0).map((row) => ({ ...row, parsed: parseDecimal(row.onHand) }));
    if (inventoryInputs.some((row) => row.parsed == null || row.parsed < 0 || (row.parsed > 0 && !quantityAligned(row.parsed, dp, step)))) {
      ctx.setActiveTab("inventory"); ctx.setSaveError(ctx.t.validationStep); return;
    }
    if ((parsedMin != null && parsedMax != null && parsedMin > parsedMax) || (ctx.draft.returnPolicyChoice === "Custom" && (!Number.isInteger(parseDecimal(ctx.draft.customReturnWindowDays)) || (parseDecimal(ctx.draft.customReturnWindowDays) ?? 0) <= 0))) {
      ctx.setActiveTab("conditions"); ctx.setSaveError(ctx.t.validationReturnWindow); return;
    }
    if (inventoryInputs.length > 0 && !ctx.draft.inventoryReason.trim()) {
      ctx.setActiveTab("inventory"); ctx.setSaveError(ctx.t.validationReason); return;
    }
    ctx.saveRef.current = true;
    ctx.setSaving(true);
    ctx.setSaveError(null);
    ctx.setSaveNotice(null);
    ctx.setSaveNeedsReload(false);
    ctx.setSaveRetryable(true);
    try {
      const createInput = {
        sellerPartyId: ctx.selectedSeller?.sellerPartyId ?? ctx.adapter.fixedSeller?.sellerPartyId,
        catalogVariantId: ctx.selectedVariant.catalogVariantId,
        sellerSku: ctx.draft.sellerSku.trim() || null,
        channel: ctx.selectedChannel,
        inventory: inventoryInputs.map((row) => ({ locationId: row.locationId, onHand: row.parsed!, reason: ctx.draft.inventoryReason.trim() })),
        amount: parsedAmount,
        currency: ctx.draft.currency,
        returnPolicyChoice: ctx.draft.returnPolicyChoice,
        customReturnWindowDays: ctx.draft.returnPolicyChoice === "Custom" ? parseDecimal(ctx.draft.customReturnWindowDays) : null,
        minimumOrderQuantity: parseDecimal(ctx.draft.minimumOrderQuantity),
        maximumOrderQuantity: parseDecimal(ctx.draft.maximumOrderQuantity),
        responsibility: ctx.adapter.role === "admin" ? ctx.responsibility : null,
        shippingTypeId: ctx.adapter.role === "admin" ? ctx.shippingTypeId || null : null,
      };
      const result = ctx.adapter.createAndSaveDraft
        ? await ctx.adapter.createAndSaveDraft(createInput)
        : await ctx.adapter.createDraft({ sellerPartyId: createInput.sellerPartyId, catalogVariantId: createInput.catalogVariantId, sellerSku: createInput.sellerSku, channel: createInput.channel });
      if (!result.ok) {
        if ("createdOfferId" in result && result.createdOfferId) {
          ctx.showBlocking("error", ctx.t.partialTitle, `${offerErrorText(ctx.t, result.errorCode ?? "save")} ${ctx.t.partialSaved}`, String(result.createdOfferId));
          return;
        }
        if (result.errorCode === "offer.listing.duplicate_active" && ctx.adapter.findActiveOffer) {
          const sellerId = ctx.adapter.role === "seller" ? ctx.adapter.fixedSeller?.sellerPartyId : ctx.selectedSeller?.sellerPartyId;
          const existing = sellerId ? await ctx.adapter.findActiveOffer(sellerId, ctx.selectedVariant.catalogVariantId, ctx.selectedChannel) : null;
          if (existing) ctx.setActiveConflict(existing);
          else ctx.showBlocking("error", ctx.t.duplicateActiveTitle, ctx.t.duplicateActive);
          ctx.setSaveRetryable(false);
          return;
        }
        if (result.errorCode === "host-unreachable") {
          ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.createUnknown);
          ctx.setSaveRetryable(false);
          ctx.setSaveNeedsReload(true);
          return;
        }
        ctx.showBlocking("error", ctx.t.saveErrorTitle, result.denied ? ctx.t.denied : offerErrorText(ctx.t, result.errorCode));
        ctx.setSaveRetryable(!result.denied);
        return;
      }
      // The create-and-apply contract has verified the durable read-back. Clear only this local draft.
      ctx.completeCreate(result.offerId);
    } catch {
      ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.createUnknown);
      ctx.setSaveRetryable(false);
      ctx.setSaveNeedsReload(true);
    } finally {
      ctx.saveRef.current = false;
      ctx.setSaving(false);
    }
  }

  async function runProvisionPosition(locationId: string) {
    const previous = ctx.detail;
    if (!ctx.isEdit || !previous || !ctx.adapter.provisionPosition || !ctx.adapter.loadDetail || !locationId
        || ctx.saving || ctx.saveRef.current || ctx.saveNeedsReload || previous.status === "Archived"
        || previous.capabilities?.canEditPhysicalInventory !== true
        || previous.inventoryLocations.some(row => row.locationId === locationId)) return;
    ctx.saveRef.current = true;
    ctx.setSaving(true);
    ctx.setSaveNotice(null);
    ctx.setSaveError(null);
    ctx.setTabError(null);
    try {
      const latest = await ctx.adapter.loadDetail();
      if (!latest.ok || detailVersionChanged(previous, latest.detail))
        return finishSaveFailure("inventory", { errorCode: "concurrency.conflict" }, false);
      const position = await ctx.adapter.provisionPosition(locationId);
      const reread = await ctx.adapter.loadDetail();
      if (!position || !reread.ok)
        return finishSaveFailure("inventory", { errorCode: "offer.inventory.response_mismatch" }, false);
      const next = reread.detail;
      const row = next.inventoryLocations.find(item => item.locationId === locationId && item.stockItemId === position.stockItemId);
      if (!row || !row.revision || row.onHand !== 0 || row.reserved !== 0
          || detailVersionChanged(previous, { ...next, inventoryLocations: next.inventoryLocations.filter(item => item.stockItemId !== row.stockItemId) }))
        return finishSaveFailure("inventory", { errorCode: "offer.inventory.response_mismatch" }, false);
      ctx.setDetail(next);
      ctx.setDraft(current => ({ ...current, inventoryOnHand: { ...current.inventoryOnHand, [row.stockItemId]: String(row.onHand) } }));
      ctx.setSaveNotice(ctx.t.saved);
    } catch {
      finishSaveFailure("inventory", { errorCode: "host-unreachable" }, false);
    } finally {
      ctx.saveRef.current = false;
      ctx.setSaving(false);
    }
  }

  return { runSave, runCreate, reloadAfterUnknown, runProvisionPosition };
}

function buildCommercialPatch(detail: OfferWorkspaceDetail, draft: OfferWorkspaceDraft): OfferCommercialPatch {
  return {
    sellerSku: draft.sellerSku.trim() || null,
    status: draft.status,
    returnPolicyChoice: draft.returnPolicyChoice,
    customReturnWindowDays: draft.returnPolicyChoice === "Custom" ? parseDecimal(draft.customReturnWindowDays) : null,
    minimumOrderQuantity: parseDecimal(draft.minimumOrderQuantity),
    maximumOrderQuantity: parseDecimal(draft.maximumOrderQuantity),
    clearOrderQuantityLimits: draft.minimumOrderQuantity.trim() === "" && draft.maximumOrderQuantity.trim() === "",
  };
}

/** Three-way merge for unknown outcomes. A conflicting owner edit is never retried automatically. */
export function reconcileDraft(previous: OfferWorkspaceDetail, latest: OfferWorkspaceDetail, desired: OfferWorkspaceDraft): OfferWorkspaceDraft | null {
  if (previous.offerId !== latest.offerId || previous.sellerPartyId !== latest.sellerPartyId || previous.catalogVariantId !== latest.catalogVariantId || previous.channel !== latest.channel) return null;
  const before = draftFromDetail(previous);
  const after = draftFromDetail(latest);
  const keys = ["sellerSku", "amount", "currency", "offeredQuantity", "returnPolicyChoice", "customReturnWindowDays", "minimumOrderQuantity", "maximumOrderQuantity", "status"] as const;
  const merged = { ...after, inventoryOnHand: { ...after.inventoryOnHand } };
  for (const key of keys) {
    if (desired[key] === before[key]) continue;
    if (after[key] !== before[key] && after[key] !== desired[key]) return null;
    merged[key] = desired[key];
  }
  for (const [id, value] of Object.entries(desired.inventoryOnHand)) {
    if (value === before.inventoryOnHand[id]) continue;
    if (!(id in after.inventoryOnHand) || (after.inventoryOnHand[id] !== before.inventoryOnHand[id] && after.inventoryOnHand[id] !== value)) return null;
    merged.inventoryOnHand[id] = value;
  }
  merged.inventoryReason = desired.inventoryReason;
  return merged;
}
