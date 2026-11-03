/**
 * Host cart JSON → storefront cart page.
 * Host contract: totalsByCurrency + defaultCurrency (no scalar subtotalExclusiveOfTax).
 */

export interface StorefrontCartLine {
  lineId: string;
  offerId: string;
  catalogVariantId: string;
  sellerPartyId: string;
  productId: string | null;
  productSlug: string | null;
  title: string;
  sellerDisplayName: string;
  mediaAssetId: string | null;
  quantity: number;
  unitAmountExclusiveOfTax: number | null;
  lineAmountExclusiveOfTax: number | null;
  currency: string;
  quotedTaxExclusive: boolean;
  unitCode: string | null;
  unitDisplayName: string | null;
  quantityDecimalPlaces: number;
  quantityStep: number | null;
  availability: string;
}

export interface StorefrontCartCurrencyTotal {
  currency: string;
  subtotalExclusiveOfTax: number;
}

export interface StorefrontCartPage {
  cartId: string;
  version: number;
  market: string;
  currency: string;
  channel: string;
  itemCount: number;
  /** Resolved goods subtotal, or null when unavailable (never silent 0 for priced carts). */
  subtotalExclusiveOfTax: number | null;
  goodsSubtotalAvailable: boolean;
  totalsByCurrency: StorefrontCartCurrencyTotal[];
  lines: StorefrontCartLine[];
  guestSecret: string | null;
  status: string;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function asString(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function asFiniteNumberOrNull(value: unknown): number | null {
  if (value == null || value === "") return null;
  if (typeof value === "number" && Number.isFinite(value)) return value;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function asBoolean(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function mapTotalsByCurrency(value: unknown): StorefrontCartCurrencyTotal[] {
  if (!Array.isArray(value)) return [];
  const rows: StorefrontCartCurrencyTotal[] = [];
  for (const entry of value) {
    const row = asRecord(entry);
    if (!row) continue;
    const currency = asString(readProp(row, "currency", "Currency")).trim();
    const amount = asFiniteNumberOrNull(readProp(row, "subtotalExclusiveOfTax", "SubtotalExclusiveOfTax"));
    if (!currency || amount == null) continue;
    rows.push({ currency, subtotalExclusiveOfTax: amount });
  }
  return rows;
}

function mapLine(row: unknown): StorefrontCartLine {
  const line = asRecord(row) ?? {};
  const productRaw = readProp(line, "productId", "ProductId");
  const slugRaw = readProp(line, "productSlug", "ProductSlug");
  const mediaRaw = readProp(line, "mediaAssetId", "MediaAssetId");
  const unitRaw = readProp(line, "unitAmountExclusiveOfTax", "UnitAmountExclusiveOfTax");
  const lineRaw = readProp(line, "lineAmountExclusiveOfTax", "LineAmountExclusiveOfTax");
  const unitCodeRaw = readProp(line, "unitCode", "UnitCode");
  const unitNameRaw = readProp(line, "unitDisplayName", "UnitDisplayName");
  const unitCode = asString(unitCodeRaw);
  const unitName = asString(unitNameRaw);
  const stepRaw = readProp(line, "quantityStep", "QuantityStep");
  return {
    lineId: asString(readProp(line, "lineId", "LineId")),
    offerId: asString(readProp(line, "offerId", "OfferId")),
    catalogVariantId: asString(readProp(line, "catalogVariantId", "CatalogVariantId")),
    sellerPartyId: asString(readProp(line, "sellerPartyId", "SellerPartyId")),
    productId: productRaw == null ? null : asString(productRaw),
    productSlug: slugRaw == null ? null : asString(slugRaw),
    title: asString(readProp(line, "title", "Title"), "کالا"),
    sellerDisplayName: asString(readProp(line, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
    mediaAssetId: mediaRaw == null ? null : asString(mediaRaw),
    quantity: asNumber(readProp(line, "quantity", "Quantity"), 1),
    unitAmountExclusiveOfTax: unitRaw == null ? null : asFiniteNumberOrNull(unitRaw),
    lineAmountExclusiveOfTax: lineRaw == null ? null : asFiniteNumberOrNull(lineRaw),
    currency: asString(readProp(line, "currency", "Currency"), "IRR"),
    quotedTaxExclusive: asBoolean(readProp(line, "quotedTaxExclusive", "QuotedTaxExclusive"), true),
    unitCode: unitCode.length > 0 ? unitCode : null,
    unitDisplayName: unitName.length > 0 ? unitName : null,
    quantityDecimalPlaces: asNumber(readProp(line, "quantityDecimalPlaces", "QuantityDecimalPlaces")),
    quantityStep: stepRaw == null || stepRaw === "" ? null : asNumber(stepRaw),
    availability: asString(readProp(line, "availability", "Availability"), "Available"),
  };
}

/** Maps Host cart JSON. Product `price` fields are ignored. */
export function mapStorefrontCart(payload: unknown): StorefrontCartPage | null {
  const item = asRecord(payload);
  if (!item) return null;
  const cartId = asString(readProp(item, "cartId", "CartId"));
  if (!cartId) return null;

  const linesRaw = readProp(item, "lines", "Lines");
  const secretRaw = readProp(item, "guestSecret", "GuestSecret");
  const currency = asString(
    readProp(item, "defaultCurrency", "DefaultCurrency") ?? readProp(item, "currency", "Currency"),
    "IRR",
  );
  const totalsByCurrency = mapTotalsByCurrency(readProp(item, "totalsByCurrency", "TotalsByCurrency"));
  const legacySubtotal = asFiniteNumberOrNull(
    readProp(item, "subtotalExclusiveOfTax", "SubtotalExclusiveOfTax"),
  );
  const lines = Array.isArray(linesRaw) ? linesRaw.map(mapLine) : [];

  let subtotalExclusiveOfTax: number | null = null;
  let goodsSubtotalAvailable = false;
  if (lines.length === 0) {
    subtotalExclusiveOfTax = 0;
    goodsSubtotalAvailable = true;
  } else if (totalsByCurrency.length > 0) {
    const match =
      totalsByCurrency.find((row) => row.currency.toUpperCase() === currency.toUpperCase()) ??
      (totalsByCurrency.length === 1 ? totalsByCurrency[0] : undefined);
    if (match) {
      subtotalExclusiveOfTax = match.subtotalExclusiveOfTax;
      goodsSubtotalAvailable = true;
    }
  } else if (legacySubtotal != null) {
    subtotalExclusiveOfTax = legacySubtotal;
    goodsSubtotalAvailable = true;
  }

  return {
    cartId,
    version: asNumber(readProp(item, "version", "Version")),
    market: asString(readProp(item, "market", "Market"), "IR"),
    currency,
    channel: asString(readProp(item, "channel", "Channel")),
    itemCount: asNumber(readProp(item, "itemCount", "ItemCount")),
    subtotalExclusiveOfTax,
    goodsSubtotalAvailable,
    totalsByCurrency,
    guestSecret: secretRaw == null ? null : asString(secretRaw),
    status: asString(readProp(item, "status", "Status"), "Active"),
    lines,
  };
}
