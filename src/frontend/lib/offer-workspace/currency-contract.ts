import type { OfferCurrencyOption } from "./types.ts";

/** Strict shared decoder for the Pricing-owned currency reference; never invent a precision. */
export function parseOfferCurrencies(value: unknown): OfferCurrencyOption[] {
  if (!Array.isArray(value)) throw new Error("offer.currency.invalid_reference");
  const seen = new Set<string>();
  return value.map(raw => {
    if (!raw || typeof raw !== "object") throw new Error("offer.currency.invalid_reference");
    const row = raw as Record<string, unknown>;
    const code = row.code ?? row.Code;
    const englishName = row.englishName ?? row.EnglishName;
    const nativeName = row.nativeName ?? row.NativeName;
    const decimalPlaces = row.decimalPlaces ?? row.DecimalPlaces;
    if (typeof code !== "string" || !/^[A-Z]{3}$/.test(code) || seen.has(code)
        || typeof englishName !== "string" || typeof nativeName !== "string"
        || typeof decimalPlaces !== "number" || !Number.isInteger(decimalPlaces) || decimalPlaces < 0 || decimalPlaces > 4) {
      throw new Error("offer.currency.invalid_reference");
    }
    seen.add(code);
    return { code, englishName, nativeName, decimalPlaces };
  });
}
