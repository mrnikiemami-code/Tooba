import assert from "node:assert/strict";
import test from "node:test";
import { mapStorefrontCart } from "./storefront-cart-map.ts";
import {
  groupCartLinesBySeller,
  resolveCartGoodsSubtotal,
} from "./storefront-cart-grouping.ts";
import {
  formatOfferAmount,
  moneyDecimalPlaces,
  sumExactAmounts,
  sumExactDecimalTokens,
} from "./storefront-money.ts";

const ARMAN = "01a030d1-40cb-7000-8abe-6d31739956c5";
const NILVA = "01a0ffff-0001-7000-8000-000000000001";

function line(partial: {
  lineId: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  title: string;
  quantity?: number;
  unit?: number | null;
  lineAmount?: number | null;
  currency?: string;
}) {
  return {
    lineId: partial.lineId,
    offerId: `offer-${partial.lineId}`,
    catalogVariantId: `var-${partial.lineId}`,
    sellerPartyId: partial.sellerPartyId,
    productId: `prod-${partial.lineId}`,
    productSlug: partial.lineId,
    title: partial.title,
    sellerDisplayName: partial.sellerDisplayName,
    mediaAssetId: null,
    quantity: partial.quantity ?? 1,
    unitAmountExclusiveOfTax: partial.unit === undefined ? 1000 : partial.unit,
    lineAmountExclusiveOfTax: partial.lineAmount === undefined ? 1000 : partial.lineAmount,
    currency: partial.currency ?? "IRR",
    quotedTaxExclusive: true,
    unitCode: null,
    unitDisplayName: null,
    quantityDecimalPlaces: 0,
    quantityStep: 1,
    availability: "Available",
  };
}

test("FinancialRounder policy: IRR/JPY/KRW are integer; others keep 2 places", () => {
  assert.equal(moneyDecimalPlaces("IRR"), 0);
  assert.equal(moneyDecimalPlaces("irr"), 0);
  assert.equal(moneyDecimalPlaces("JPY"), 0);
  assert.equal(moneyDecimalPlaces("KRW"), 0);
  assert.equal(moneyDecimalPlaces("USD"), 2);
  assert.equal(moneyDecimalPlaces("EUR"), 2);
});

test("IRR customer format drops fractional suffix; USD keeps two places", () => {
  const irr = formatOfferAmount(15_511_232.16, "IRR");
  assert.match(irr, /ریال/);
  assert.equal(irr.includes("."), false);
  assert.equal(irr.includes("۱۶"), false); // no fractional 16 glyph path via latin "."
  assert.match(irr, /۱۵/);
  const usd = formatOfferAmount(12.5, "USD");
  assert.match(usd, /۱۲٫۵۰|12\.50|۱۲.۵۰/);
  assert.match(usd, /USD/);
});

test("exact decimal sum reconciles fractional intermediate without float drift", () => {
  assert.equal(sumExactDecimalTokens(["5170410.72", "5170410.72", "5170410.72"]), "15511232.16");
  assert.equal(sumExactAmounts([5170410.72, 5170410.72, 5170410.72]), 15511232.16);
  assert.equal(sumExactAmounts([0, 0]), 0);
  assert.equal(sumExactAmounts([1000, null]), null);
});

test("three lines / two sellers → two sections + seller subtotals", () => {
  const sections = groupCartLinesBySeller([
    line({ lineId: "l1", sellerPartyId: ARMAN, sellerDisplayName: "فروشگاه آرمان", title: "Samsung", lineAmount: 100 }),
    line({ lineId: "l2", sellerPartyId: ARMAN, sellerDisplayName: "فروشگاه آرمان", title: "Huawei", lineAmount: 200 }),
    line({ lineId: "l3", sellerPartyId: NILVA, sellerDisplayName: "فروشگاه نیلوا", title: "Xiaomi", lineAmount: 300 }),
  ]);
  assert.equal(sections.length, 2);
  assert.equal(sections[0]!.sellerPartyId, ARMAN);
  assert.equal(sections[0]!.lines.length, 2);
  assert.equal(sections[0]!.sellerSubtotalExclusiveOfTax, 300);
  assert.equal(sections[1]!.sellerPartyId, NILVA);
  assert.equal(sections[1]!.lines.length, 1);
  assert.equal(sections[1]!.sellerSubtotalExclusiveOfTax, 300);
});

test("same display name with distinct seller ids stay separate", () => {
  const sections = groupCartLinesBySeller([
    line({ lineId: "a", sellerPartyId: "seller-a", sellerDisplayName: "هم‌نام", title: "A", lineAmount: 1 }),
    line({ lineId: "b", sellerPartyId: "seller-b", sellerDisplayName: "هم‌نام", title: "B", lineAmount: 2 }),
  ]);
  assert.equal(sections.length, 2);
  assert.equal(sections[0]!.sellerPartyId, "seller-a");
  assert.equal(sections[1]!.sellerPartyId, "seller-b");
});

test("one seller stays a single section", () => {
  const sections = groupCartLinesBySeller([
    line({ lineId: "a", sellerPartyId: ARMAN, sellerDisplayName: "آرمان", title: "A", lineAmount: 10 }),
    line({ lineId: "b", sellerPartyId: ARMAN, sellerDisplayName: "آرمان", title: "B", lineAmount: 20 }),
  ]);
  assert.equal(sections.length, 1);
  assert.equal(sections[0]!.sellerSubtotalExclusiveOfTax, 30);
});

test("unknown seller identities never merge", () => {
  const sections = groupCartLinesBySeller([
    line({ lineId: "u1", sellerPartyId: "", sellerDisplayName: "", title: "U1", lineAmount: 1 }),
    line({ lineId: "u2", sellerPartyId: "   ", sellerDisplayName: "X", title: "U2", lineAmount: 2 }),
  ]);
  assert.equal(sections.length, 2);
  assert.equal(sections[0]!.sellerPartyId, null);
  assert.equal(sections[1]!.sellerPartyId, null);
  assert.notEqual(sections[0]!.sectionKey, sections[1]!.sectionKey);
});

test("Host totalsByCurrency maps to nonzero goods total; missing never silent 0", () => {
  const mapped = mapStorefrontCart({
    cartId: "cart-tb250",
    version: 2,
    market: "IR",
    defaultCurrency: "IRR",
    channel: "Marketplace",
    itemCount: 3,
    totalsByCurrency: [{ currency: "IRR", subtotalExclusiveOfTax: 74689416.16 }],
    lines: [
      {
        lineId: "l1",
        offerId: "o1",
        catalogVariantId: "v1",
        sellerPartyId: ARMAN,
        title: "Samsung",
        sellerDisplayName: "فروشگاه آرمان",
        quantity: 1,
        unitAmountExclusiveOfTax: 3783477,
        lineAmountExclusiveOfTax: 3783477,
        currency: "IRR",
      },
      {
        lineId: "l2",
        offerId: "o2",
        catalogVariantId: "v2",
        sellerPartyId: ARMAN,
        title: "Huawei",
        sellerDisplayName: "فروشگاه آرمان",
        quantity: 1,
        unitAmountExclusiveOfTax: 4870632,
        lineAmountExclusiveOfTax: 4870632,
        currency: "IRR",
      },
      {
        lineId: "l3",
        offerId: "o3",
        catalogVariantId: "v3",
        sellerPartyId: NILVA,
        title: "Xiaomi",
        sellerDisplayName: "فروشگاه نیلوا",
        quantity: 3,
        unitAmountExclusiveOfTax: 5170410.72,
        lineAmountExclusiveOfTax: 15511232.16,
        currency: "IRR",
      },
    ],
    status: "Active",
  });
  assert.ok(mapped);
  assert.equal(mapped!.currency, "IRR");
  assert.equal(mapped!.goodsSubtotalAvailable, true);
  assert.equal(mapped!.subtotalExclusiveOfTax, 74689416.16);
  assert.equal(mapped!.totalsByCurrency.length, 1);

  const missing = mapStorefrontCart({
    cartId: "cart-missing-total",
    version: 1,
    defaultCurrency: "IRR",
    itemCount: 1,
    lines: [
      {
        lineId: "l1",
        offerId: "o1",
        catalogVariantId: "v1",
        sellerPartyId: ARMAN,
        title: "X",
        sellerDisplayName: "آرمان",
        quantity: 1,
        unitAmountExclusiveOfTax: 1000,
        lineAmountExclusiveOfTax: 1000,
        currency: "IRR",
      },
    ],
  });
  assert.ok(missing);
  assert.equal(missing!.goodsSubtotalAvailable, false);
  assert.equal(missing!.subtotalExclusiveOfTax, null);

  const empty = mapStorefrontCart({
    cartId: "cart-empty",
    version: 1,
    defaultCurrency: "IRR",
    totalsByCurrency: [],
    lines: [],
  });
  assert.ok(empty);
  assert.equal(empty!.goodsSubtotalAvailable, true);
  assert.equal(empty!.subtotalExclusiveOfTax, 0);

  const lawfulZero = resolveCartGoodsSubtotal({
    currency: "IRR",
    lines: [{ lineAmountExclusiveOfTax: 0 }],
    totalsByCurrency: [{ currency: "IRR", subtotalExclusiveOfTax: 0 }],
  });
  assert.deepEqual(lawfulZero, { state: "known", amount: 0, currency: "IRR" });
});

test("legacy scalar subtotal still maps when totalsByCurrency absent", () => {
  const mapped = mapStorefrontCart({
    cartId: "legacy",
    version: 1,
    currency: "IRR",
    subtotalExclusiveOfTax: 5000,
    lines: [
      {
        lineId: "l1",
        offerId: "o1",
        catalogVariantId: "v1",
        sellerPartyId: ARMAN,
        title: "X",
        sellerDisplayName: "آرمان",
        quantity: 1,
        lineAmountExclusiveOfTax: 5000,
        currency: "IRR",
      },
    ],
  });
  assert.equal(mapped?.goodsSubtotalAvailable, true);
  assert.equal(mapped?.subtotalExclusiveOfTax, 5000);
});
