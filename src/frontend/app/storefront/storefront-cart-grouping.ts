import type { StorefrontCartLine } from "./storefront-cart-map.ts";
import { sumExactAmounts } from "./storefront-money.ts";

/**
 * One seller section on the cart page. Keyed by stable sellerPartyId.
 * Unknown / blank seller ids never merge with each other.
 */
export type StorefrontCartSellerSection = {
  sectionKey: string;
  sellerPartyId: string | null;
  sellerDisplayName: string;
  lines: StorefrontCartLine[];
  /** Exact sum of known line amounts; null when any line amount is missing. */
  sellerSubtotalExclusiveOfTax: number | null;
  currency: string;
};

/**
 * Groups cart lines by sellerPartyId in first-seen order.
 * Blank/whitespace seller ids stay isolated (one section per line).
 */
export function groupCartLinesBySeller(lines: readonly StorefrontCartLine[]): StorefrontCartSellerSection[] {
  const sections: StorefrontCartSellerSection[] = [];
  const indexByKey = new Map<string, number>();

  for (const line of lines) {
    const rawId = line.sellerPartyId?.trim() ?? "";
    const known = rawId.length > 0;
    const sectionKey = known ? `seller:${rawId}` : `unknown:${line.lineId}`;
    const existing = indexByKey.get(sectionKey);
    if (existing == null) {
      indexByKey.set(sectionKey, sections.length);
      sections.push({
        sectionKey,
        sellerPartyId: known ? rawId : null,
        sellerDisplayName: line.sellerDisplayName?.trim() || (known ? "فروشنده" : "فروشنده نامشخص"),
        lines: [line],
        sellerSubtotalExclusiveOfTax: null,
        currency: line.currency || "IRR",
      });
      continue;
    }
    sections[existing]!.lines.push(line);
  }

  for (const section of sections) {
    section.sellerSubtotalExclusiveOfTax = sumExactAmounts(
      section.lines.map((line) => line.lineAmountExclusiveOfTax),
    );
    section.currency = section.lines.find((line) => line.currency)?.currency ?? section.currency;
  }

  return sections;
}

/** Goods-subtotal resolution for the cart summary (pre-shipping). */
export type CartGoodsSubtotal =
  | { state: "known"; amount: number; currency: string }
  | { state: "unavailable"; currency: string };

export type CartCurrencyTotalView = {
  currency: string;
  subtotalExclusiveOfTax: number;
};

/**
 * Resolves the cart goods total from Host totalsByCurrency.
 * Missing/malformed totals with priced lines → unavailable (never silent 0).
 * Empty cart → lawful zero. Explicit 0 with lines → lawful free/zero.
 */
export function resolveCartGoodsSubtotal(input: {
  currency: string;
  lines: readonly { lineAmountExclusiveOfTax: number | null }[];
  totalsByCurrency: readonly CartCurrencyTotalView[];
  /** Legacy scalar — only trusted when totalsByCurrency is absent AND value is present. */
  legacySubtotalExclusiveOfTax?: number | null;
}): CartGoodsSubtotal {
  const currency = (input.currency || "IRR").trim() || "IRR";
  const hasLines = input.lines.length > 0;

  if (!hasLines) {
    return { state: "known", amount: 0, currency };
  }

  const totals = input.totalsByCurrency;
  if (totals.length > 0) {
    const match =
      totals.find((row) => row.currency.trim().toUpperCase() === currency.toUpperCase()) ??
      (totals.length === 1 ? totals[0] : undefined);
    if (
      match &&
      Number.isFinite(match.subtotalExclusiveOfTax) &&
      typeof match.subtotalExclusiveOfTax === "number"
    ) {
      return { state: "known", amount: match.subtotalExclusiveOfTax, currency: match.currency || currency };
    }
    return { state: "unavailable", currency };
  }

  const legacy = input.legacySubtotalExclusiveOfTax;
  if (legacy != null && Number.isFinite(legacy)) {
    return { state: "known", amount: legacy, currency };
  }

  return { state: "unavailable", currency };
}
