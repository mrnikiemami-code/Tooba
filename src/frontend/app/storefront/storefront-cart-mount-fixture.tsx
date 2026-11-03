"use client";

/**
 * Mount harness shell for TB-250: same seller sections + goods summary +
 * quantity/remove/coupon mutations as StorefrontShopeivaCart, without Next router
 * / pending-payments (those need the app router).
 */
import { createElement, useEffect, useState, type ReactNode } from "react";
import {
  changeCartLineQuantity,
  loadStorefrontCart,
  removeCartLine,
  toCustomerCartMessage,
  type StorefrontCartPage,
} from "./storefront-cart-api.ts";
import {
  previewStorefrontCheckout,
  toCustomerCheckoutMessage,
} from "./storefront-checkout-api.ts";
import { formatOfferAmount } from "./storefront-money.ts";
import { StorefrontCartSellerSections } from "./storefront-cart-seller-sections.tsx";

export function StorefrontCartMountFixture(): ReactNode {
  const [cart, setCart] = useState<StorefrontCartPage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [couponInput, setCouponInput] = useState("");
  const [couponMessage, setCouponMessage] = useState<string | null>(null);
  const [couponBusy, setCouponBusy] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void loadStorefrontCart()
      .then((page) => {
        if (cancelled) return;
        setCart(page);
        setError(null);
      })
      .catch((cause: unknown) => {
        if (cancelled) return;
        setError(toCustomerCartMessage(cause));
        setCart(null);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  async function mutate(action: () => Promise<StorefrontCartPage>) {
    setBusy(true);
    setError(null);
    try {
      setCart(await action());
    } catch (cause) {
      setError(toCustomerCartMessage(cause));
    } finally {
      setBusy(false);
    }
  }

  async function applyCoupon() {
    if (!cart) return;
    const code = couponInput.trim();
    if (!code) {
      setCouponMessage("کد تخفیف پاک شد.");
      return;
    }
    setCouponBusy(true);
    try {
      const preview = await previewStorefrontCheckout(cart.cartId, code);
      setCouponMessage(
        preview.discountAmount > 0
          ? `تخفیف ${formatOfferAmount(preview.discountAmount, preview.currency)} اعمال شد.`
          : "این کد برای سبد فعلی قابل اعمال نیست.",
      );
    } catch (cause) {
      setCouponMessage(toCustomerCheckoutMessage(cause));
    } finally {
      setCouponBusy(false);
    }
  }

  if (loading) {
    return createElement("div", { "data-testid": "cart-loading" }, "loading");
  }
  if (!cart || cart.lines.length === 0) {
    return createElement("div", { "data-testid": "cart-empty" }, error ?? "empty");
  }

  const goodsAvailable = Boolean(cart.goodsSubtotalAvailable && cart.subtotalExclusiveOfTax != null);
  const goodsLabel = goodsAvailable
    ? formatOfferAmount(cart.subtotalExclusiveOfTax as number, cart.currency)
    : "نامشخص";

  return createElement(
    "div",
    { "data-testid": "cart-page" },
    error ? createElement("p", { role: "alert" }, error) : null,
    createElement(StorefrontCartSellerSections, {
      lines: cart.lines,
      busy,
      locale: "fa" as const,
      onRemove: (lineId: string) => void mutate(() => removeCartLine(lineId)),
      onChangeQuantity: (lineId: string, quantity: number) =>
        void mutate(() => changeCartLineQuantity(lineId, quantity)),
    }),
    createElement(
      "div",
      { "data-testid": "cart-summary" },
      createElement(
        "span",
        {
          "data-testid": "cart-summary-goods-amount",
          "data-goods-available": goodsAvailable ? "true" : "false",
        },
        goodsLabel,
      ),
    ),
    createElement(
      "div",
      { "data-testid": "cart-coupon" },
      createElement("input", {
        id: "cart-coupon",
        value: couponInput,
        onChange: (event: { target: { value: string } }) => setCouponInput(event.target.value),
      }),
      createElement(
        "button",
        {
          type: "button",
          disabled: couponBusy || busy,
          onClick: () => void applyCoupon(),
        },
        couponBusy ? "…" : "اعمال",
      ),
      couponMessage ? createElement("p", { "data-testid": "cart-coupon-message" }, couponMessage) : null,
    ),
  );
}
