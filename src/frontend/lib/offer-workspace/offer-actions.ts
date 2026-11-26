import type { MutableRefObject } from "react";
import { changedInventoryRows, detailVersionChanged, offeredQuantityDirty, parseDecimal, quantityAligned, validateDraft, type OfferWorkspaceDraft } from "./model.ts";
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
      return { ...current, amount: next.amount != null ? String(next.amount) : "", currency: next.currency };
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
    const transient = result.errorCode === "host-unreachable" || result.errorCode === "concurrency.conflict";
    ctx.setActiveTab(tab);
    ctx.setTabError({ tab, code: result.errorCode || "save" });
    ctx.setSaveNeedsReload(transient);
    ctx.setSaveRetryable(!transient);
    const text = transient ? ctx.t.saveUnknown : result.denied ? ctx.t.denied : offerErrorText(ctx.t, result.errorCode);
    ctx.showBlocking(transient ? "reload" : "error", savedAny ? ctx.t.partialTitle : ctx.t.saveErrorTitle, savedAny ? `${text} ${ctx.t.partialSaved}` : text);
    if (savedAny) {
      ctx.setSaveNotice(ctx.t.partialSaved);
      ctx.setSaveRetryable(true);
    }
  }

  async function reloadAfterUnknown() {
    if (!ctx.adapter.loadDetail || ctx.saveReloading) return;
    ctx.setSaveReloading(true);
    try {
      const result = await ctx.adapter.loadDetail();
      if (!result.ok) {
        ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.saveUnknown);
        return;
      }
      applyServerDetail(result.detail);
      ctx.setSaveNeedsReload(false);
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
    const expectedAmount = parseDecimal(ctx.draft.amount);
    const expectedInventory = new Map<string, number | null>();
    if (ctx.stockChanged) {
      for (const row of ctx.detail.inventoryLocations) expectedInventory.set(row.stockItemId, parseDecimal(ctx.draft.inventoryOnHand[row.stockItemId]));
    }

    try {
      if (ctx.adapter.loadDetail) {
        const latest = await ctx.adapter.loadDetail();
        if (!latest.ok) {
          ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.saveUnknown);
          ctx.setSaveNeedsReload(true);
          ctx.setSaveRetryable(false);
          return;
        }
        if (detailVersionChanged(ctx.detail, latest.detail)) {
          applyServerDetail(latest.detail);
          ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.concurrentChange);
          ctx.setSaveNeedsReload(true);
          ctx.setSaveRetryable(false);
          return;
        }
      }

      if (ctx.commercialChanged) {
        const result = await ctx.adapter.saveCommercial(buildCommercialPatch(ctx.detail, ctx.draft), ctx.detail?.offerRevision ?? ctx.detail?.revisions?.offer ?? undefined);
        if (!result.ok) return finishSaveFailure("conditions", result, savedAny);
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
        const result = await ctx.adapter.savePrice({ amount: expectedAmount, currency: ctx.draft.currency }, ctx.detail?.priceRevision ?? ctx.detail?.revisions?.price ?? "absent");
        if (!result.ok) return finishSaveFailure("pricing", result, savedAny);
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
        const result = await ctx.adapter.saveOfferedQuantity(quantity, ctx.detail?.revisions?.offeredQuantity ?? ctx.detail?.allocationRevision ?? "absent", crypto.randomUUID());
        if (!result.ok) return finishSaveFailure("inventory", result, savedAny);
        ctx.setDetail(result.detail);
        ctx.setDraft((current) => ({ ...current, offeredQuantity: result.detail.offeredQuantity != null ? String(result.detail.offeredQuantity) : "" }));
        savedAny = true;
      }

      if (ctx.stockChanged) {
        const changedRows = changedInventoryRows(ctx.detail, ctx.draft);
        const inventoryRevisions = new Map(ctx.detail.inventoryLocations.map((row) => [row.stockItemId, row.revision]));
        for (const row of changedRows) {
          const result = await ctx.adapter.saveInventory({
            stockItemId: row.stockItemId,
            onHand: parseDecimal(ctx.draft.inventoryOnHand[row.stockItemId]) ?? row.onHand,
            reason: ctx.draft.inventoryReason.trim(),
          }, inventoryRevisions.get(row.stockItemId) ?? row.revision ?? "absent");
          if (!result.ok) return finishSaveFailure("inventory", result, savedAny);
          inventoryRevisions.set(row.stockItemId, result.detail.inventoryLocations.find((item) => item.stockItemId === row.stockItemId)?.revision ?? null);
          applySavedInventory(result.detail, row.stockItemId);
          savedAny = true;
        }
        ctx.setDraft((current) => ({ ...current, inventoryReason: "" }));
      }

      if (ctx.shippingChanged && ctx.adapter.saveFulfillment) {
        ctx.setShippingSaving(true);
        const result = await ctx.adapter.saveFulfillment({ responsibility: ctx.responsibility, shippingTypeId: ctx.shippingTypeId || null }, ctx.detail?.offerRevision ?? ctx.detail?.revisions?.offer ?? undefined);
        ctx.setShippingSaving(false);
        if (!result.ok) return finishSaveFailure("shipping", result, savedAny);
        const verification = ctx.adapter.loadDetail ? await ctx.adapter.loadDetail() : null;
        if (!verification || !verification.ok || verification.detail.fulfillmentResponsibility !== ctx.responsibility || (verification.detail.fulfillmentShippingTypeId ?? null) !== (ctx.shippingTypeId || null)) {
          ctx.setShippingError(ctx.t.perTabError);
          return finishSaveFailure("shipping", { errorCode: "seller.shipping.response_mismatch" }, savedAny);
        }
        ctx.applyLoadedDetail(verification.detail);
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
        const expectedDays = ctx.draft.customReturnWindowDays.trim() ? Number.parseInt(ctx.draft.customReturnWindowDays, 10) : null;
        const commercialMismatch = ctx.commercialChanged && (
          verification.detail.sellerSku !== ctx.draft.sellerSku.trim()
          || verification.detail.status !== ctx.draft.status
          || verification.detail.currency !== ctx.draft.currency
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
          || (ctx.priceChanged && verification.detail.amount !== expectedAmount)
          || (ctx.shippingChanged && (verification.detail.fulfillmentResponsibility !== ctx.responsibility || (verification.detail.fulfillmentShippingTypeId ?? null) !== (ctx.shippingTypeId || null)))
          || (ctx.stockChanged && ctx.detail.inventoryLocations.some((row) => {
            const expected = expectedInventory.get(row.stockItemId);
            if (expected == null) return false;
            const actual = verification.detail.inventoryLocations.find((item) => item.stockItemId === row.stockItemId);
            return !actual || actual.onHand !== expected;
          }));
        if (mismatch) {
          applyServerDetail(verification.detail);
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
    if (ctx.priceChanged && (parsedAmount == null || parsedAmount < 0)) {
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
    if (inventoryInputs.some((row) => row.parsed == null || (row.parsed > 0 && !quantityAligned(row.parsed, dp, step)))) {
      ctx.setActiveTab("inventory"); ctx.setSaveError(ctx.t.validationStep); return;
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
      const localDetail = ctx.detail;
      const createInput = {
        sellerPartyId: ctx.selectedSeller?.sellerPartyId,
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
      ctx.adapter.onCreated(result.offerId);
    } catch {
      ctx.showBlocking("reload", ctx.t.saveErrorTitle, ctx.t.createUnknown);
      ctx.setSaveRetryable(false);
      ctx.setSaveNeedsReload(true);
    } finally {
      ctx.saveRef.current = false;
      ctx.setSaving(false);
    }
  }

  return { runSave, runCreate, reloadAfterUnknown };
}

function buildCommercialPatch(detail: OfferWorkspaceDetail, draft: OfferWorkspaceDraft): OfferCommercialPatch {
  return {
    sellerSku: draft.sellerSku.trim() || null,
    status: draft.status,
    returnPolicyChoice: draft.returnPolicyChoice,
    customReturnWindowDays: draft.returnPolicyChoice === "Custom" ? parseDecimal(draft.customReturnWindowDays) : null,
    minimumOrderQuantity: parseDecimal(draft.minimumOrderQuantity),
    maximumOrderQuantity: parseDecimal(draft.maximumOrderQuantity),
  };
}
