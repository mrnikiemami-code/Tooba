/**
 * Pure Offer workspace model helpers: digit normalization, validation, dirty detection and
 * buyability derivation. No React, no network, no domain writes.
 */
import type { OfferCurrencyOption, OfferPriceView, OfferWorkspaceDetail } from "./types";

export interface OfferWorkspaceDraft {
  sellerSku: string;
  amount: string;
  currency: string;
  inventoryOnHand: Record<string, string>;
  inventoryReason: string;
  offeredQuantity: string;
  returnPolicyChoice: string;
  customReturnWindowDays: string;
  minimumOrderQuantity: string;
  maximumOrderQuantity: string;
  status: string;
}

const PERSIAN_DIGITS = "۰۱۲۳۴۵۶۷۸۹";
const ARABIC_DIGITS = "٠١٢٣٤٥٦٧٨٩";

export function normalizeDigits(input: string): string {
  return input
    .replace(/[۰-۹]/g, (digit) => String(PERSIAN_DIGITS.indexOf(digit)))
    .replace(/[٠-٩]/g, (digit) => String(ARABIC_DIGITS.indexOf(digit)))
    .replace(/[٫،]/g, ".");
}

export function parseDecimal(input: string | null | undefined): number | null {
  if (input == null) return null;
  const text = normalizeDigits(input).replace(/,/g, "").trim();
  if (text.length === 0) return null;
  const value = Number(text);
  return Number.isFinite(value) ? value : null;
}

export function draftFromDetail(detail: OfferWorkspaceDetail): OfferWorkspaceDraft {
  return {
    sellerSku: detail.sellerSku ?? "",
    amount: (() => { const row = priceRowFor(detail, detail.currency); return row && row.status !== "Retired" ? String(row.amount) : ""; })(),
    currency: detail.currency,
    inventoryOnHand: Object.fromEntries(
      detail.inventoryLocations.map((row) => [row.stockItemId, String(row.onHand)]),
    ),
    inventoryReason: "",
    offeredQuantity: detail.offeredQuantity != null ? String(detail.offeredQuantity) : "",
    returnPolicyChoice: detail.returnPolicyChoice || "Default",
    customReturnWindowDays:
      detail.customReturnWindowDays != null ? String(detail.customReturnWindowDays) : "",
    minimumOrderQuantity:
      detail.minimumOrderQuantity != null ? String(detail.minimumOrderQuantity) : "",
    maximumOrderQuantity:
      detail.maximumOrderQuantity != null ? String(detail.maximumOrderQuantity) : "",
    status: detail.status,
  };
}

export function priceDirty(detail: OfferWorkspaceDetail, draft: OfferWorkspaceDraft): boolean {
  if (draft.amount.trim() !== "" && parseDecimal(draft.amount) == null) return true;
  const row = priceRowFor(detail, draft.currency);
  if (row) return row.status === "Retired" ? draft.amount.trim() !== "" : parseDecimal(draft.amount) !== row.amount;
  // A currency with no authored row becomes dirty once a non-empty amount is entered.
  return draft.amount.trim() !== "";
}

/** Current authored row for the selected currency, or the primary detail price as fallback. */
export function priceRowFor(detail: Pick<OfferWorkspaceDetail, "prices" | "currency" | "amount" | "revisions"> & Partial<Pick<OfferWorkspaceDetail, "priceRevision">>, currency: string): OfferPriceView | null {
  const code = (currency || "").toUpperCase();
  const row = (detail.prices ?? []).find((item) => item.currency.toUpperCase() === code);
  if (row) return row;
  if (detail.currency && detail.currency.toUpperCase() === code && detail.amount != null) {
    return {
      priceId: "",
      currency: detail.currency,
      amount: detail.amount,
      revision: detail.priceRevision ?? detail.revisions?.price ?? "",
      status: "",
    };
  }
  return null;
}

/** Revision used as CAS for the selected currency (independent per authored price row). */
export function priceRevisionFor(detail: Pick<OfferWorkspaceDetail, "prices" | "currency" | "amount" | "revisions"> & Partial<Pick<OfferWorkspaceDetail, "priceRevision">>, currency: string): string | null {
  const row = priceRowFor(detail, currency);
  if (row) return row.revision || null;
  if (detail.currency && detail.currency.toUpperCase() === (currency || "").toUpperCase()) {
    const primary = detail.revisions?.price ?? detail.priceRevision;
    if (primary) return primary;
  }
  // Pricing expects the literal CAS sentinel "absent" when the currency has no authored row yet.
  return "absent";
}

export function commercialDirty(detail: OfferWorkspaceDetail, draft: OfferWorkspaceDraft): boolean {
  const min = parseDecimal(draft.minimumOrderQuantity);
  const max = parseDecimal(draft.maximumOrderQuantity);
  const customDays =
    draft.returnPolicyChoice === "Custom" ? parseDecimal(draft.customReturnWindowDays) : null;
  return (
    draft.sellerSku.trim() !== (detail.sellerSku ?? "").trim() ||
    draft.status !== detail.status ||
    draft.returnPolicyChoice !== detail.returnPolicyChoice ||
    customDays !== detail.customReturnWindowDays ||
    min !== detail.minimumOrderQuantity ||
    max !== detail.maximumOrderQuantity
  );
}

export function offeredQuantityDirty(detail: OfferWorkspaceDetail, draft: OfferWorkspaceDraft): boolean {
  if (!detail.capabilities?.canEditOfferedQuantity) return false;
  return (draft.offeredQuantity.trim() !== "" && parseDecimal(draft.offeredQuantity) == null) || parseDecimal(draft.offeredQuantity) !== detail.offeredQuantity;
}

export function inventoryDirty(detail: OfferWorkspaceDetail, draft: OfferWorkspaceDraft): boolean {
  return detail.inventoryLocations.some(
    (row) => parseDecimal(draft.inventoryOnHand[row.stockItemId]) !== row.onHand,
  );
}

export interface ValidationError {
  tab: "identity" | "pricing" | "inventory" | "conditions";
  code: string;
}

export function currencyDecimalPlaces(currency: string, currencies: OfferCurrencyOption[] = []): number {
  return currencies.find((row) => row.code.toUpperCase() === currency.toUpperCase())?.decimalPlaces ?? (["IRR", "JPY", "KRW"].includes(currency.toUpperCase()) ? 0 : 2);
}

export function validatePrice(amountText: string, currency: string, currencies: OfferCurrencyOption[] = []): string | null {
  if (amountText.trim().length === 0) return null;
  const amount = parseDecimal(amountText);
  if (amount == null || amount < 0 || (currencies.length > 0 && !currencies.some(row => row.code.toUpperCase() === currency.toUpperCase()))) return "offer.price.invalid";
  const raw = normalizeDigits(amountText).replace(/,/g, "").trim();
  const fraction = /^(?:\+?\d*(?:\.(\d*))?)$/.exec(raw);
  if (!fraction) return "offer.price.invalid";
  if ((fraction[1] ?? "").replace(/0+$/, "").length > currencyDecimalPlaces(currency, currencies)) {
    return "offer.price.decimals_not_allowed";
  }
  return null;
}

export function quantityAligned(value: number, decimalPlaces: number, step: number | null): boolean {
  if (!Number.isFinite(value) || value <= 0) return false;
  const factor = 10 ** Math.max(0, decimalPlaces);
  if (Math.abs(Math.round(value * factor) - value * factor) > 1e-8) return false;
  if (step != null && step > 0) {
    const quotient = value / step;
    if (Math.abs(quotient - Math.round(quotient)) > 1e-9) return false;
  }
  return true;
}

export function validateInventory(
  detail: OfferWorkspaceDetail,
  draft: OfferWorkspaceDraft,
): string | null {
  const changed = detail.inventoryLocations.filter(
    (row) => parseDecimal(draft.inventoryOnHand[row.stockItemId]) !== row.onHand,
  );
  if (changed.length === 0) return null;
  if (detail.capabilities?.canEditPhysicalInventory !== true) return "denied";
  for (const row of changed) {
    const value = parseDecimal(draft.inventoryOnHand[row.stockItemId]);
    if (value == null || value < 0) return "offer.inventory.invalid";
    if (value < row.reserved) return "offer.inventory.below_reserved";
    if (value > 0 && !quantityAligned(value, detail.unitQuantityDecimalPlaces ?? 0, detail.unitQuantityStep ?? null)) return "offer.inventory.step_invalid";
  }
  if (changed.some(row => row.requiresAdjustmentReason !== false) && draft.inventoryReason.trim().length === 0) return "offer.inventory.reason_required";
  return null;
}

export function validateConditions(
  detail: OfferWorkspaceDetail,
  draft: OfferWorkspaceDraft,
): string | null {
  const min = parseDecimal(draft.minimumOrderQuantity);
  const max = parseDecimal(draft.maximumOrderQuantity);
  if (min == null && draft.minimumOrderQuantity.trim().length > 0) return "offer.qty.min_invalid";
  if (max == null && draft.maximumOrderQuantity.trim().length > 0) return "offer.qty.max_invalid";
  if (min != null && (!(min > 0) || !Number.isFinite(min))) return "offer.qty.min_invalid";
  if (min != null && !quantityAligned(min, detail.unitQuantityDecimalPlaces ?? 0, detail.unitQuantityStep ?? null)) return "offer.qty.step_invalid";
  if (max != null && (!(max > 0) || !Number.isFinite(max))) return "offer.qty.max_invalid";
  if (max != null && !quantityAligned(max, detail.unitQuantityDecimalPlaces ?? 0, detail.unitQuantityStep ?? null)) return "offer.qty.step_invalid";
  if (min != null && max != null && min > max) return "offer.qty.min_above_max";
  if (draft.returnPolicyChoice === "Custom") {
    const days = parseDecimal(draft.customReturnWindowDays);
    if (days == null || !Number.isInteger(days) || days <= 0) return "offer.return.window_invalid";
    if (days < detail.minReturnWindowDays || days > detail.maxReturnWindowDays) {
      return "offer.return.window_out_of_range";
    }
  }
  return null;
}

export function validateDraft(
  detail: OfferWorkspaceDetail,
  draft: OfferWorkspaceDraft,
  currencies: OfferCurrencyOption[] = [],
): ValidationError[] {
  const errors: ValidationError[] = [];
  const priceError = priceDirty(detail, draft) ? validatePrice(draft.amount, draft.currency, currencies) : null;
  if (priceError) errors.push({ tab: "pricing", code: priceError });
  const inventoryError = inventoryDirty(detail, draft) ? validateInventory(detail, draft) : null;
  if (inventoryError) errors.push({ tab: "inventory", code: inventoryError });
  if (offeredQuantityDirty(detail, draft)) {
    const quantity = parseDecimal(draft.offeredQuantity);
    const held = detail.allocationHeld ?? 0;
    if (quantity == null || quantity < 0) errors.push({ tab: "inventory", code: "offer.inventory.invalid" });
    else if (quantity < held) errors.push({ tab: "inventory", code: "inventory.allocation.below_held" });
    else if (quantity > detail.onHand) errors.push({ tab: "inventory", code: "inventory.allocation.exceeds_physical" });
    else if (quantity > 0 && !quantityAligned(quantity, detail.unitQuantityDecimalPlaces ?? 0, detail.unitQuantityStep ?? null)) errors.push({ tab: "inventory", code: "offer.inventory.step_invalid" });
  }
  const conditionsError = commercialDirty(detail, draft) ? validateConditions(detail, draft) : null;
  if (conditionsError) errors.push({ tab: "conditions", code: conditionsError });
  return errors;
}

export interface Buyability {
  buyable: boolean;
  reason: string | null;
}

export function deriveBuyability(detail: Pick<
  OfferWorkspaceDetail,
  "status" | "amount" | "availableUnits"
>): Buyability {
  if (detail.status !== "Active") return { buyable: false, reason: "offer.buyable.inactive" };
  if (detail.amount == null) return { buyable: false, reason: "offer.buyable.no_price" };
  if (detail.availableUnits <= 0) return { buyable: false, reason: "offer.buyable.no_stock" };
  return { buyable: true, reason: null };
}

/** Lifecycle transitions allowed by the real Offer aggregate. Archived is terminal. */
export function allowedStatuses(current: string): string[] {
  switch (current) {
    case "Draft":
      return ["Draft", "Active", "Suspended", "Archived"];
    case "Active":
      return ["Active", "Suspended", "Archived"];
    case "Suspended":
      return ["Suspended", "Active", "Archived"];
    case "Archived":
      return ["Archived"];
    default:
      return [current];
  }
}

export function statusLabelKey(status: string): string {
  switch (status) {
    case "Draft":
      return "lifecycleDraft";
    case "Active":
      return "lifecycleActive";
    case "Suspended":
      return "lifecycleSuspended";
    case "Archived":
      return "lifecycleArchived";
    default:
      return "status";
  }
}

export function faNumber(value: number | null | undefined, locale: "fa" | "en"): string {
  if (value == null || !Number.isFinite(value)) return "—";
  return value.toLocaleString(locale === "fa" ? "fa-IR" : "en-US", { maximumFractionDigits: 3 });
}

export function digitsFa(value: string | number): string {
  return String(value).replace(/\d/g, (digit) => PERSIAN_DIGITS[Number(digit)]!);
}

export function currencyLabel(currency: string, locale: "fa" | "en"): string {
  const code = (currency || "IRR").toUpperCase();
  const labels: Record<string, { fa: string; en: string }> = {
    IRR: { fa: "ریال", en: "IRR" },
    IRT: { fa: "تومان", en: "Toman" },
    USD: { fa: "دلار", en: "USD" },
    EUR: { fa: "یورو", en: "EUR" },
    AED: { fa: "درهم", en: "AED" },
  };
  return labels[code]?.[locale] ?? code;
}

export function marketLabel(market: string, locale: "fa" | "en"): string {
  const key = (market || "IR").toUpperCase();
  const labels: Record<string, { fa: string; en: string }> = {
    IR: { fa: "ایران", en: "Iran" },
    AE: { fa: "امارات", en: "UAE" },
    TR: { fa: "ترکیه", en: "Türkiye" },
    DE: { fa: "آلمان", en: "Germany" },
  };
  return labels[key]?.[locale] ?? market;
}

export function formatAmountDisplay(input: string): string {
  const normalized = normalizeDigits(input).replace(/,/g, "").trim();
  if (normalized.length === 0) return "";
  const match = /^(-?)(\d+)(?:\.(\d+))?$/.exec(normalized);
  if (!match) return input;
  const sign = match[1] ?? "";
  const integerPart = Number(match[2]);
  if (!Number.isFinite(integerPart)) return input;
  const grouped = integerPart.toLocaleString("fa-IR");
  const fraction = match[3];
  return `${sign}${grouped}${fraction ? `٫${digitsFa(fraction)}` : ""}`;
}

export function faMoney(amount: number | null | undefined, currency: string, locale: "fa" | "en"): string {
  if (amount == null) return "—";
  const formatted = amount.toLocaleString(locale === "fa" ? "fa-IR" : "en-US", { maximumFractionDigits: 4 });
  return `${formatted} ${currencyLabel(currency, locale)}`;
}

export function statusLabel(
  t: { lifecycleDraft: string; lifecycleActive: string; lifecycleSuspended: string; lifecycleArchived: string },
  status: string,
): string {
  switch (status) {
    case "Draft":
      return t.lifecycleDraft;
    case "Active":
      return t.lifecycleActive;
    case "Suspended":
      return t.lifecycleSuspended;
    case "Archived":
      return t.lifecycleArchived;
    default:
      return status;
  }
}

export function errorLabel(code: string, locale: "fa" | "en"): string {
  const table: Record<string, { fa: string; en: string }> = {
    "offer.price.invalid": { fa: "مبلغ نامعتبر است.", en: "Invalid amount." },
    "offer.price.decimals_not_allowed": { fa: "تعداد اعشار مبلغ با ارز انتخابی سازگار نیست.", en: "The amount exceeds the selected currency precision." },
    "offer.inventory.invalid": { fa: "موجودی نامعتبر است.", en: "Invalid stock value." },
    "offer.inventory.below_reserved": {
      fa: "موجودی فیزیکی نباید کمتر از رزرو باشد.",
      en: "On-hand must not be below reserved.",
    },
    "offer.inventory.reason_required": { fa: "دلیل اصلاح الزامی است.", en: "Adjustment reason is required." },
    "offer.qty.min_invalid": { fa: "حداقل مقدار خرید نامعتبر است.", en: "Invalid minimum quantity." },
    "offer.qty.max_invalid": { fa: "حداکثر مقدار خرید نامعتبر است.", en: "Invalid maximum quantity." },
    "offer.qty.min_above_max": {
      fa: "حداقل مقدار خرید نباید از حداکثر بیشتر باشد.",
      en: "Minimum quantity must not exceed maximum.",
    },
    "offer.return.window_invalid": { fa: "مهلت اختصاصی نامعتبر است.", en: "Invalid custom window." },
    "offer.return.window_out_of_range": {
      fa: "مهلت اختصاصی خارج از محدودهٔ مجاز است.",
      en: "Custom window is out of the allowed range.",
    },
    "offer.lifecycle.archived_locked": {
      fa: "آفر بایگانی‌شده قابل فعال‌سازی دوباره نیست.",
      en: "An archived offer cannot be reactivated.",
    },
    "seller.identity.missing": { fa: "هویت فروشنده مشخص نیست.", en: "Seller identity is missing." },
    "host-unreachable": { fa: "ارتباط با سرور برقرار نیست.", en: "Host is unreachable." },
  };
  const row = table[code];
  if (row) return row[locale];
  return code;
}

/** Applies one saved inventory location without resetting other unsaved positions or the reason. */
export function applySavedInventoryDraft(
  draft: OfferWorkspaceDraft,
  next: OfferWorkspaceDetail,
  stockItemId: string,
): OfferWorkspaceDraft {
  const saved = next.inventoryLocations.find((row) => row.stockItemId === stockItemId);
  if (!saved) return draft;
  return {
    ...draft,
    inventoryOnHand: {
      ...draft.inventoryOnHand,
      [stockItemId]: String(saved.onHand),
    },
  };
}

/** Rows whose draft on-hand differs from the last confirmed detail. */
export function changedInventoryRows(
  detail: OfferWorkspaceDetail,
  draft: OfferWorkspaceDraft,
): OfferWorkspaceDetail["inventoryLocations"] {
  return detail.inventoryLocations.filter(
    (row) => parseDecimal(draft.inventoryOnHand[row.stockItemId]) !== row.onHand,
  );
}

/** Compares the persisted offer snapshot so a stale session cannot overwrite another session. */
export function detailVersionChanged(current: OfferWorkspaceDetail, latest: OfferWorkspaceDetail): boolean {
  return current.offerId !== latest.offerId || current.sellerPartyId !== latest.sellerPartyId || current.catalogVariantId !== latest.catalogVariantId || current.channel !== latest.channel
    || current.lastUpdatedAt !== latest.lastUpdatedAt
    || current.revisions?.offer !== latest.revisions?.offer
    || current.revisions?.price !== latest.revisions?.price
    || current.revisions?.offeredQuantity !== latest.revisions?.offeredQuantity
    || current.offeredQuantity !== latest.offeredQuantity || current.allocationHeld !== latest.allocationHeld
    || JSON.stringify(current.capabilities) !== JSON.stringify(latest.capabilities)
    || current.status !== latest.status
    || current.sellerSku !== latest.sellerSku
    || current.returnPolicyChoice !== latest.returnPolicyChoice
    || current.customReturnWindowDays !== latest.customReturnWindowDays
    || current.minimumOrderQuantity !== latest.minimumOrderQuantity
    || current.maximumOrderQuantity !== latest.maximumOrderQuantity
    || current.amount !== latest.amount
    || current.currency !== latest.currency
    || JSON.stringify(current.prices ?? []) !== JSON.stringify(latest.prices ?? [])
    || current.onHand !== latest.onHand
    || current.reserved !== latest.reserved
    || current.fulfillmentResponsibility !== latest.fulfillmentResponsibility
    || current.fulfillmentShippingTypeId !== latest.fulfillmentShippingTypeId
    || current.inventoryLocations.length !== latest.inventoryLocations.length
    || current.inventoryLocations.some((row) => {
      const next = latest.inventoryLocations.find((item) => item.stockItemId === row.stockItemId);
      return !next || next.locationId !== row.locationId || next.revision !== row.revision || next.onHand !== row.onHand || next.reserved !== row.reserved;
    });
}
