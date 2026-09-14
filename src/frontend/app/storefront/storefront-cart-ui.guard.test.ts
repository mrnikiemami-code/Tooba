import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const cardSource = fs.readFileSync(path.join(root, "app/storefront/storefront-product-card.tsx"), "utf8");
const headerSource = fs.readFileSync(path.join(root, "app/storefront/storefront-header.tsx"), "utf8");
const miniCartSource = fs.readFileSync(path.join(root, "app/storefront/storefront-mini-cart.tsx"), "utf8");
const cartSource = fs.readFileSync(path.join(root, "app/storefront/storefront-cart.tsx"), "utf8");
const cartPageSource = fs.readFileSync(path.join(root, "app/cart/page.tsx"), "utf8");

test("product card ATC is a real button wired to addOfferToCart", () => {
  assert.match(cardSource, /data-testid="product-card-atc"/);
  assert.match(cardSource, /addOfferToCart\(card\.primaryOfferId/);
  assert.match(cardSource, /from "react-toastify"/);
  assert.match(cardSource, /محصول \$\{card\.title\} به سبد خرید اضافه شد/);
  assert.match(cardSource, /اضافه شد/);
  assert.match(cardSource, /bg-primary|text-primary|--color-primary/);
  assert.doesNotMatch(cardSource, /#2563EB/);
  assert.doesNotMatch(cardSource, /#E53935/);
  assert.ok(!cardSource.includes('<span\n            className={`mt-auto inline-flex'));
});

test("header opens mini-cart drawer instead of bare /cart link", () => {
  assert.match(headerSource, /StorefrontMiniCartDrawer/);
  assert.match(headerSource, /data-testid="header-cart-button"/);
  assert.match(headerSource, /data-testid="header-cart-badge"/);
  assert.match(headerSource, /setCartOpen\(true\)/);
  assert.match(headerSource, /loadStorefrontCart/);
  assert.match(headerSource, /AUTH_CHANGED_EVENT/);
  assert.match(headerSource, /StorefrontAccountMenu/);
  assert.doesNotMatch(headerSource, /data-testid="header-login-link"/);
});

test("canonical account menu reuses Shopeiva dropdown and logout", () => {
  const menu = fs.readFileSync(path.join(root, "app/storefront/storefront-account-menu.tsx"), "utf8");
  assert.match(menu, /header-login-link/);
  assert.match(menu, /header-account-menu/);
  assert.match(menu, /\/customer-panel\/orders/);
  assert.match(menu, /\/customer-panel/);
  assert.match(menu, /\/api\/auth\/logout/);
  assert.match(menu, /clearCartSession/);
  assert.match(menu, /notifyAuthChanged/);
  assert.match(menu, /header-account-label/);
  assert.match(menu, /storefrontAccountLabel|session\.label/);
  assert.match(menu, /markStorefrontSessionAnonymous/);
  assert.match(menu, /resetStorefrontMergeTransition/);
  assert.doesNotMatch(menu, /#E53935/);
  assert.doesNotMatch(menu, /setInterval/);
  assert.doesNotMatch(headerSource, /setInterval/);
  assert.doesNotMatch(cartSource, /setInterval/);
});

test("mini-cart drawer preserves Shopeiva structure on Host cart APIs", () => {
  assert.match(miniCartSource, /data-testid="mini-cart-drawer"/);
  assert.match(miniCartSource, /data-testid="mini-cart-overlay"/);
  assert.match(miniCartSource, /changeCartLineQuantity/);
  assert.match(miniCartSource, /removeCartLine/);
  assert.match(miniCartSource, /loadStorefrontCart/);
  assert.match(miniCartSource, /تکمیل خرید/);
  assert.match(miniCartSource, /localizePath\("\/cart"\)/);
  assert.match(miniCartSource, /data-testid="mini-cart-checkout-cta"/);
  assert.match(miniCartSource, /max-w-sm/);
  assert.match(miniCartSource, /bg-primary|text-primary|--color-primary/);
  assert.doesNotMatch(miniCartSource, /#2563EB/);
});

test("cart page recommendations use live feed cards with working ATC reuse", () => {
  assert.match(cartSource, /data-testid="cart-recommendations"/);
  assert.match(cartSource, /StorefrontProductCardView/);
  assert.match(cartPageSource, /pickCartRecommendations/);
  assert.match(cartPageSource, /newArrivals/);
  assert.match(cartPageSource, /featuredProducts/);
  assert.match(cartSource, /cart-shipping-honest/);
  assert.match(cartSource, /ارسال رایگان یا نرخ چندحامل جعلی/);
  assert.match(cartSource, /CART_CHANGED_EVENT/);
  assert.match(cartSource, /AUTH_CHANGED_EVENT/);
  assert.match(cartSource, /addEventListener\(CART_CHANGED_EVENT/);
  assert.match(cartSource, /addEventListener\(AUTH_CHANGED_EVENT/);
  assert.match(cartSource, /StorefrontPendingPayments/);
  assert.match(cartSource, /onRemoved=\{removePendingFromCart\}/);
  assert.match(cartSource, /سبد فعال شما خالی است/);
});
