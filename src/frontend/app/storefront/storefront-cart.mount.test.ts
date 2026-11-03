/**
 * TB-250 — mounted cart seller sections + money + mutations (jsdom harness).
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/storefront/storefront-cart.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import {
  click,
  flush,
  mount,
  q,
  setInputValue,
  waitFor,
} from "../../features/order-carrier-execution/test-dom-harness.ts";
import { LocaleProvider } from "../../lib/i18n/locale-context.tsx";
import { StorefrontCartMountFixture } from "./storefront-cart-mount-fixture.tsx";
import { clearCartSession, writeCartSession } from "./storefront-cart-api.ts";
import { resetStorefrontSessionCacheForTests } from "./storefront-identity-api.ts";

const ARMAN = "seller-arman-250";
const NILVA = "seller-nilva-250";
const CART_ID = "cart-tb250-mount";

type CartDto = {
  cartId: string;
  version: number;
  market: string;
  defaultCurrency: string;
  channel: string;
  itemCount: number;
  totalsByCurrency: Array<{ currency: string; subtotalExclusiveOfTax: number }>;
  lines: Array<Record<string, unknown>>;
  guestSecret: string | null;
  status: string;
};

function baseCart(version: number, overrides: Partial<CartDto> = {}): CartDto {
  return {
    cartId: CART_ID,
    version,
    market: "IR",
    defaultCurrency: "IRR",
    channel: "Marketplace",
    itemCount: 3,
    totalsByCurrency: [{ currency: "IRR", subtotalExclusiveOfTax: 600_000 }],
    guestSecret: "secret-tb250",
    status: "Active",
    lines: [
      {
        lineId: "line-samsung",
        offerId: "offer-samsung",
        catalogVariantId: "var-samsung",
        sellerPartyId: ARMAN,
        productId: "prod-samsung",
        productSlug: "samsung",
        title: "Samsung",
        sellerDisplayName: "فروشگاه آرمان",
        quantity: 1,
        unitAmountExclusiveOfTax: 100_000,
        lineAmountExclusiveOfTax: 100_000,
        currency: "IRR",
        quotedTaxExclusive: true,
        availability: "Available",
      },
      {
        lineId: "line-huawei",
        offerId: "offer-huawei",
        catalogVariantId: "var-huawei",
        sellerPartyId: ARMAN,
        productId: "prod-huawei",
        productSlug: "huawei",
        title: "Huawei",
        sellerDisplayName: "فروشگاه آرمان",
        quantity: 2,
        unitAmountExclusiveOfTax: 100_000,
        lineAmountExclusiveOfTax: 200_000,
        currency: "IRR",
        quotedTaxExclusive: true,
        availability: "Available",
      },
      {
        lineId: "line-xiaomi",
        offerId: "offer-xiaomi",
        catalogVariantId: "var-xiaomi",
        sellerPartyId: NILVA,
        productId: "prod-xiaomi",
        productSlug: "xiaomi",
        title: "Xiaomi",
        sellerDisplayName: "فروشگاه نیلوا",
        quantity: 1,
        unitAmountExclusiveOfTax: 300_000.72,
        lineAmountExclusiveOfTax: 300_000.72,
        currency: "IRR",
        quotedTaxExclusive: true,
        availability: "Available",
      },
    ],
    ...overrides,
  };
}

type Ledger = {
  cartGets: number;
  linePatches: number;
  lineDeletes: number;
  couponPosts: number;
};

function installCartFetch(state: { cart: CartDto }): Ledger & { restore: () => void } {
  const ledger: Ledger = { cartGets: 0, linePatches: 0, lineDeletes: 0, couponPosts: 0 };
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(typeof input === "string" || input instanceof URL ? input : input.url);
    const method = (init?.method ?? "GET").toUpperCase();

    if (url.includes("/api/auth/me")) {
      return new Response(JSON.stringify({ authenticated: false }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/storefront/cart/") && url.includes("/lines/") && method === "PATCH") {
      ledger.linePatches += 1;
      const body = JSON.parse(String(init?.body ?? "{}")) as { quantity?: number };
      const lineId = url.split("/lines/")[1]?.split("?")[0] ?? "";
      const nextLines = state.cart.lines.map((line) => {
        if (line.lineId !== lineId) return line;
        const qty = body.quantity ?? Number(line.quantity);
        const unit = Number(line.unitAmountExclusiveOfTax ?? 0);
        return { ...line, quantity: qty, lineAmountExclusiveOfTax: unit * qty };
      });
      const subtotal = nextLines.reduce((sum, line) => sum + Number(line.lineAmountExclusiveOfTax ?? 0), 0);
      state.cart = {
        ...state.cart,
        version: state.cart.version + 1,
        lines: nextLines,
        itemCount: nextLines.reduce((s, l) => s + Number(l.quantity ?? 0), 0),
        totalsByCurrency: [{ currency: "IRR", subtotalExclusiveOfTax: subtotal }],
      };
      return new Response(JSON.stringify(state.cart), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/storefront/cart/") && url.includes("/lines/") && method === "DELETE") {
      ledger.lineDeletes += 1;
      const lineId = url.split("/lines/")[1]?.split("?")[0] ?? "";
      const remaining = state.cart.lines.filter((line) => line.lineId !== lineId);
      const subtotal = remaining.reduce((sum, line) => sum + Number(line.lineAmountExclusiveOfTax ?? 0), 0);
      state.cart = {
        ...state.cart,
        version: state.cart.version + 1,
        lines: remaining,
        itemCount: remaining.reduce((s, l) => s + Number(l.quantity ?? 0), 0),
        totalsByCurrency: [{ currency: "IRR", subtotalExclusiveOfTax: subtotal }],
      };
      return new Response(JSON.stringify(state.cart), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes(`/v1/storefront/cart/${CART_ID}`) && method === "GET") {
      ledger.cartGets += 1;
      return new Response(JSON.stringify(state.cart), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/v1/storefront/checkout/preview")) {
      ledger.couponPosts += 1;
      return new Response(
        JSON.stringify({
          cartId: CART_ID,
          discountAmount: 1000,
          currency: "IRR",
          subtotalExclusiveOfTax: 599_000,
          shippingAmount: 0,
          taxAmount: 0,
          payableAmount: 599_000,
          sellerOrders: [],
          shippingGroups: [],
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    return new Response(JSON.stringify({ errorCode: "not_found", detail: url }), { status: 404 });
  }) as typeof fetch;

  return {
    get cartGets() {
      return ledger.cartGets;
    },
    get linePatches() {
      return ledger.linePatches;
    },
    get lineDeletes() {
      return ledger.lineDeletes;
    },
    get couponPosts() {
      return ledger.couponPosts;
    },
    restore() {
      globalThis.fetch = original;
    },
  };
}

test("mount cart: two seller sections, integer IRR amounts, update/remove/coupon, bounded network", async () => {
  resetStorefrontSessionCacheForTests();
  clearCartSession();
  writeCartSession(CART_ID, "secret-tb250");
  const state = { cart: baseCart(4) };
  const fx = installCartFetch(state);

  const tree = await mount(
    createElement(LocaleProvider, { locale: "fa" }, createElement(StorefrontCartMountFixture)),
  );
  try {
    await waitFor(() => !!tree.container.querySelector('[data-testid="cart-page"]'));
    await waitFor(() => tree.container.querySelectorAll('[data-testid="cart-seller-section"]').length === 2);

    const sections = tree.container.querySelectorAll('[data-testid="cart-seller-section"]');
    assert.equal(sections.length, 2);
    assert.equal(sections[0]?.getAttribute("data-seller-party-id"), ARMAN);
    assert.equal(sections[1]?.getAttribute("data-seller-party-id"), NILVA);
    assert.equal(tree.container.querySelectorAll('[data-testid="cart-line"]').length, 3);

    const goods = q(tree.container, '[data-testid="cart-summary-goods-amount"]');
    assert.equal(goods.getAttribute("data-goods-available"), "true");
    assert.match(goods.textContent ?? "", /ریال/);
    assert.equal((goods.textContent ?? "").includes("."), false);
    assert.equal((goods.textContent ?? "").includes("۷۲"), false);

    for (const label of [...tree.container.querySelectorAll('[data-testid="cart-line-amount"]')].map(
      (node) => node.textContent ?? "",
    )) {
      assert.match(label, /ریال/);
      assert.equal(label.includes("."), false);
    }

    const qtyPlus = tree.container.querySelector(
      '[data-line-id="line-huawei"] [data-testid="cart-line-qty"] button[aria-label="افزایش"]',
    ) as HTMLButtonElement;
    await click(qtyPlus);
    await waitFor(() => fx.linePatches >= 1);
    await flush();

    const removeBtn = tree.container.querySelector(
      '[data-line-id="line-xiaomi"] [data-testid="cart-line-remove"]',
    ) as HTMLButtonElement;
    await click(removeBtn);
    await waitFor(() => fx.lineDeletes >= 1);
    await waitFor(() => tree.container.querySelectorAll('[data-testid="cart-line"]').length === 2);

    await setInputValue(q(tree.container, "#cart-coupon"), "SAVE");
    const apply = [...tree.container.querySelectorAll("button")].find((b) =>
      (b.textContent ?? "").includes("اعمال"),
    );
    assert.ok(apply);
    await click(apply!);
    await waitFor(() => fx.couponPosts >= 1);
    await waitFor(() => !!tree.container.querySelector('[data-testid="cart-coupon-message"]'));

    assert.ok(fx.cartGets <= 4, `unexpected cart GET storm: ${fx.cartGets}`);
    assert.equal(fx.linePatches, 1);
    assert.equal(fx.lineDeletes, 1);
    assert.equal(fx.couponPosts, 1);
  } finally {
    await tree.unmount();
    fx.restore();
    clearCartSession();
  }
});
