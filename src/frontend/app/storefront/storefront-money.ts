/**
 * Customer-facing storefront money display + exact decimal helpers.
 *
 * Rounding authority mirrors backend `FinancialRounder.MoneyPlaces`:
 * IRR / JPY / KRW → 0 fractional places (integer minor units);
 * all other registered currencies → 2 places.
 *
 * Display formatting never redefines stored order/payment amounts. Internal
 * values keep full precision; IRR customer glyphs drop the fractional suffix.
 */

/** Places for a currency code — same table as FinancialRounder.MoneyPlaces. */
export function moneyDecimalPlaces(currency: string | null | undefined): number {
  const code = (currency ?? "").trim().toUpperCase();
  return code === "IRR" || code === "JPY" || code === "KRW" ? 0 : 2;
}

/**
 * Formats a quoted amount for customer UI (fa-IR digits + separators).
 * IRR renders whole rials with no fractional suffix.
 */
export function formatOfferAmount(amount: number, currency: string): string {
  if (!Number.isFinite(amount)) {
    return "—";
  }
  const places = moneyDecimalPlaces(currency);
  const formatted = new Intl.NumberFormat("fa-IR", {
    // IRR: no fractional suffix. Other currencies: fixed registered scale (FinancialRounder).
    minimumFractionDigits: places === 0 ? 0 : places,
    maximumFractionDigits: places,
  }).format(amount);
  return currency === "IRR" ? `${formatted} ریال` : `${formatted} ${currency}`;
}

type Scaled = { readonly units: bigint; readonly scale: number };

function parseDecimalToken(text: string): Scaled | null {
  const match = /^(-?)(\d+)(?:\.(\d+))?$/.exec(text.trim());
  if (!match) return null;
  const negative = match[1] === "-";
  const whole = match[2] ?? "0";
  const frac = match[3] ?? "";
  const units = BigInt(whole + frac);
  return { units: negative ? -units : units, scale: frac.length };
}

function scaleUp(value: Scaled, targetScale: number): bigint {
  const delta = targetScale - value.scale;
  if (delta <= 0) return value.units;
  return value.units * BigInt(10) ** BigInt(delta);
}

function formatScaled(units: bigint, scale: number): string {
  const zero = BigInt(0);
  const negative = units < zero;
  const abs = negative ? -units : units;
  if (scale === 0) return `${negative ? "-" : ""}${abs.toString()}`;
  const raw = abs.toString().padStart(scale + 1, "0");
  const whole = raw.slice(0, -scale);
  const frac = raw.slice(-scale);
  return `${negative ? "-" : ""}${whole}.${frac}`;
}

/** Exact decimal sum of canonical tokens; null when any token is malformed. */
export function sumExactDecimalTokens(tokens: readonly string[]): string | null {
  let scale = 0;
  let units = BigInt(0);
  for (const token of tokens) {
    const parsed = parseDecimalToken(token);
    if (!parsed) return null;
    scale = Math.max(scale, parsed.scale);
  }
  for (const token of tokens) {
    const parsed = parseDecimalToken(token)!;
    units += scaleUp(parsed, scale);
  }
  return formatScaled(units, scale);
}

/**
 * Exact sum of finite JS amounts via invariant decimal tokens (no float accumulate).
 * Returns null when any amount is missing/non-finite.
 */
export function sumExactAmounts(amounts: readonly (number | null | undefined)[]): number | null {
  const tokens: string[] = [];
  for (const amount of amounts) {
    if (amount == null || !Number.isFinite(amount)) return null;
    tokens.push(String(amount));
  }
  if (tokens.length === 0) return 0;
  const summed = sumExactDecimalTokens(tokens);
  if (summed == null) return null;
  const value = Number(summed);
  return Number.isFinite(value) ? value : null;
}
