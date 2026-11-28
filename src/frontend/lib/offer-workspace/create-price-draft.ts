import { parseDecimal, validatePrice, type OfferWorkspaceDraft } from "./model.ts";
import type { OfferCurrencyOption, OfferPriceView } from "./types.ts";

/** Each currency has one pending authored amount; the current field is the latest authority. */
export function createPriceEntries(draft: OfferWorkspaceDraft): Record<string, string> {
  const rows = { ...draft.createPrices };
  if (draft.currency) {
    const code = draft.currency.toUpperCase();
    if (draft.amount.trim()) rows[code] = draft.amount;
    else delete rows[code];
  }
  return rows;
}
export function switchCreateCurrency(draft: OfferWorkspaceDraft, currency: string): Partial<OfferWorkspaceDraft> {
  const rows = createPriceEntries(draft);
  const code = currency.toUpperCase();
  return { createPrices: rows, currency: code, amount: rows[code] ?? (!draft.currency ? draft.amount : "") };
}
export function createPriceRows(draft: OfferWorkspaceDraft): OfferPriceView[] {
  return Object.entries(createPriceEntries(draft)).flatMap(([currency, text]) => {
    const amount = parseDecimal(text);
    return amount == null ? [] : [{ priceId: "", currency, amount, revision: "", status: "Draft" }];
  });
}
export function validateCreatePrices(draft: OfferWorkspaceDraft, currencies: OfferCurrencyOption[]) {
  if (!draft.currency && draft.amount.trim()) return { error: "offer.price.invalid", prices: [] };
  const prices: Array<{ currency: string; amount: number }> = [];
  for (const [currency, text] of Object.entries(createPriceEntries(draft))) {
    const error = validatePrice(text, currency, currencies);
    const amount = parseDecimal(text);
    if (error || amount == null) return { error: error ?? "offer.price.invalid", prices: [] };
    prices.push({ currency, amount });
  }
  return { error: null, prices };
}
