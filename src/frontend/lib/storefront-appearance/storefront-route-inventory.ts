export type StorefrontRouteGroup = "A" | "B" | "C" | "D" | "E" | "F" | "G";

export type StorefrontRouteAuth = "anonymous" | "customer" | "either";

export interface StorefrontRouteInventoryEntry {
  id: string;
  group: StorefrontRouteGroup;
  pattern: string;
  examplePath: string;
  auth: StorefrontRouteAuth;
  shell: "storefront-shell" | "customer-panel-shell";
  majorWrapper: string;
  themeCoverage: "shared-shell" | "shared-shell+page" | "layout-static";
  notes: string;
  dynamic?: boolean;
  crawl: boolean;
  proofShot?: string;
}

/**
 * Canonical Storefront + customer route inventory. Crawler and coverage guards load this file.
 * Example paths for dynamic rows are filled at runtime from seeded Host data when possible.
 */
export const STOREFRONT_ROUTE_INVENTORY: StorefrontRouteInventoryEntry[] = [
  { id: "home", group: "A", pattern: "/", examplePath: "/fa", auth: "anonymous", shell: "storefront-shell", majorWrapper: "StorefrontPageSurface", themeCoverage: "shared-shell+page", notes: "Home composition bands", crawl: true, proofShot: "home-r3.png" },
  { id: "products", group: "A", pattern: "/products", examplePath: "/fa/products", auth: "anonymous", shell: "storefront-shell", majorWrapper: "listing section", themeCoverage: "shared-shell+page", notes: "PLP", crawl: true, proofShot: "plp-r3.png" },
  { id: "category", group: "A", pattern: "/category/[slug]", examplePath: "/fa/category/{slug}", auth: "anonymous", shell: "storefront-shell", majorWrapper: "listing section", themeCoverage: "shared-shell+page", notes: "Category PLP", crawl: true, dynamic: true },
  { id: "brands", group: "A", pattern: "/brands", examplePath: "/fa/brands", auth: "anonymous", shell: "storefront-shell", majorWrapper: "directory section", themeCoverage: "shared-shell+page", notes: "Brand directory", crawl: true },
  { id: "brand", group: "A", pattern: "/brand/[slug]", examplePath: "/fa/brand/{slug}", auth: "anonymous", shell: "storefront-shell", majorWrapper: "listing/merch section", themeCoverage: "shared-shell+page", notes: "Brand landing", crawl: true, dynamic: true },
  { id: "sellers", group: "A", pattern: "/sellers", examplePath: "/fa/sellers", auth: "anonymous", shell: "storefront-shell", majorWrapper: "directory section", themeCoverage: "shared-shell+page", notes: "Seller directory", crawl: true },
  { id: "seller-profile", group: "A", pattern: "/seller-profile/[publicId]", examplePath: "/fa/seller-profile/{publicId}", auth: "anonymous", shell: "storefront-shell", majorWrapper: "listing section", themeCoverage: "shared-shell+page", notes: "Public seller storefront", crawl: true, dynamic: true },
  { id: "offers", group: "A", pattern: "/offers", examplePath: "/fa/offers", auth: "anonymous", shell: "storefront-shell", majorWrapper: "merchandising section", themeCoverage: "shared-shell+page", notes: "Offers merchandising", crawl: true },
  { id: "sale", group: "A", pattern: "/sale", examplePath: "/fa/sale", auth: "anonymous", shell: "storefront-shell", majorWrapper: "merchandising section", themeCoverage: "shared-shell+page", notes: "Sale merchandising", crawl: true },
  { id: "new-products", group: "A", pattern: "/new-products", examplePath: "/fa/new-products", auth: "anonymous", shell: "storefront-shell", majorWrapper: "merchandising section", themeCoverage: "shared-shell+page", notes: "New products", crawl: true },
  { id: "most-viewed", group: "A", pattern: "/most-viewed", examplePath: "/fa/most-viewed", auth: "anonymous", shell: "storefront-shell", majorWrapper: "merchandising section", themeCoverage: "shared-shell+page", notes: "Most viewed", crawl: true },
  { id: "best-seller", group: "A", pattern: "/best-seller", examplePath: "/fa/best-seller", auth: "anonymous", shell: "storefront-shell", majorWrapper: "merchandising section", themeCoverage: "shared-shell+page", notes: "Best sellers", crawl: true },
  { id: "trending", group: "A", pattern: "/trending", examplePath: "/fa/trending", auth: "anonymous", shell: "storefront-shell", majorWrapper: "merchandising section", themeCoverage: "shared-shell+page", notes: "Trending", crawl: true },
  { id: "pdp", group: "F", pattern: "/products/[slug]", examplePath: "/fa/products/{slug}", auth: "anonymous", shell: "storefront-shell", majorWrapper: "PDP section + card", themeCoverage: "shared-shell+page", notes: "Product detail", crawl: true, dynamic: true, proofShot: "pdp-r3.png" },
  { id: "landing", group: "F", pattern: "/[slug]", examplePath: "/fa/landing-campaign", auth: "anonymous", shell: "storefront-shell", majorWrapper: "landing section roles", themeCoverage: "shared-shell+page", notes: "Published landing", crawl: true, dynamic: true, proofShot: "landing-r3.png" },
  { id: "cart", group: "B", pattern: "/cart", examplePath: "/fa/cart", auth: "either", shell: "storefront-shell", majorWrapper: "commerce section", themeCoverage: "shared-shell+page", notes: "Cart", crawl: true, proofShot: "cart-r3.png" },
  { id: "shipping", group: "B", pattern: "/shipping", examplePath: "/fa/shipping", auth: "either", shell: "storefront-shell", majorWrapper: "commerce page + section", themeCoverage: "shared-shell+page", notes: "Shipping / checkout address", crawl: true, proofShot: "checkout-r3.png" },
  { id: "checkout-redirect", group: "B", pattern: "/checkout", examplePath: "/fa/checkout", auth: "either", shell: "storefront-shell", majorWrapper: "redirect to shipping", themeCoverage: "shared-shell", notes: "Legacy /checkout redirects to /shipping", crawl: true },
  { id: "payment", group: "B", pattern: "/payment", examplePath: "/fa/payment", auth: "either", shell: "storefront-shell", majorWrapper: "payment page", themeCoverage: "shared-shell+page", notes: "Payment handoff; empty cart is honest empty", crawl: true },
  { id: "payment-result", group: "B", pattern: "/payment/result", examplePath: "/fa/payment/result", auth: "either", shell: "storefront-shell", majorWrapper: "result card", themeCoverage: "shared-shell+page", notes: "May be empty without paymentId", crawl: true },
  { id: "order-confirmation", group: "B", pattern: "/order/confirmation", examplePath: "/fa/order/confirmation", auth: "either", shell: "storefront-shell", majorWrapper: "confirmation card", themeCoverage: "shared-shell+page", notes: "May be empty without checkout access", crawl: true },
  { id: "login", group: "C", pattern: "/login", examplePath: "/fa/login", auth: "anonymous", shell: "storefront-shell", majorWrapper: "auth card", themeCoverage: "shared-shell+page", notes: "OTP login", crawl: true, proofShot: "login-r3.png" },
  { id: "customer-dashboard", group: "D", pattern: "/customer-panel", examplePath: "/customer-panel", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel page + section main", themeCoverage: "shared-shell+page", notes: "Dashboard", crawl: true, proofShot: "customer-dashboard-r3.png" },
  { id: "customer-orders", group: "D", pattern: "/customer-panel/orders", examplePath: "/customer-panel/orders", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Orders list", crawl: true, proofShot: "customer-orders-r3.png" },
  { id: "customer-order-detail", group: "F", pattern: "/customer-panel/orders/[checkoutId]", examplePath: "/customer-panel/orders/{checkoutId}", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Order detail; sample from live list", crawl: true, dynamic: true, proofShot: "customer-order-detail-r3.png" },
  { id: "customer-wishlist", group: "D", pattern: "/customer-panel/wishlist", examplePath: "/customer-panel/wishlist", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Wishlist", crawl: true },
  { id: "customer-addresses", group: "D", pattern: "/customer-panel/addresses", examplePath: "/customer-panel/addresses", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Addresses", crawl: true },
  { id: "customer-notifications", group: "D", pattern: "/customer-panel/notifications", examplePath: "/customer-panel/notifications", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Notifications", crawl: true },
  { id: "customer-tickets", group: "D", pattern: "/customer-panel/tickets", examplePath: "/customer-panel/tickets", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Tickets", crawl: true },
  { id: "customer-tickets-new", group: "D", pattern: "/customer-panel/tickets/new", examplePath: "/customer-panel/tickets/new", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "New ticket", crawl: true },
  { id: "customer-ticket-detail", group: "F", pattern: "/customer-panel/tickets/[id]", examplePath: "/customer-panel/tickets/{id}", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "layout-static", notes: "Crawled if a ticket id exists; otherwise layout-static", crawl: true, dynamic: true },
  { id: "customer-wallet", group: "D", pattern: "/customer-panel/wallet", examplePath: "/customer-panel/wallet", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Wallet", crawl: true },
  { id: "customer-gift-cards", group: "D", pattern: "/customer-panel/gift-cards", examplePath: "/customer-panel/gift-cards", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Gift cards", crawl: true },
  { id: "customer-profile", group: "D", pattern: "/customer-panel/profile", examplePath: "/customer-panel/profile", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Profile", crawl: true },
  { id: "customer-settings", group: "D", pattern: "/customer-panel/settings", examplePath: "/customer-panel/settings", auth: "customer", shell: "customer-panel-shell", majorWrapper: "panel section + cards", themeCoverage: "shared-shell+page", notes: "Settings", crawl: true },
  { id: "customer-wallet-checkout-dev", group: "G", pattern: "/customer-panel/dev/wallet-checkout", examplePath: "/customer-panel/dev/wallet-checkout", auth: "customer", shell: "customer-panel-shell", majorWrapper: "dev preview page", themeCoverage: "shared-shell+page", notes: "Dev wallet checkout preview; not in live nav", crawl: true },
  { id: "blogs", group: "E", pattern: "/blogs", examplePath: "/fa/blogs", auth: "anonymous", shell: "storefront-shell", majorWrapper: "content section", themeCoverage: "shared-shell+page", notes: "Blog listing", crawl: true, proofShot: "content-r3.png" },
  { id: "blog-detail", group: "F", pattern: "/blogs/[slug]", examplePath: "/fa/blogs/{slug}", auth: "anonymous", shell: "storefront-shell", majorWrapper: "article section + card", themeCoverage: "shared-shell+page", notes: "Article detail", crawl: true, dynamic: true },
  { id: "blog-category", group: "E", pattern: "/blogs/category/[slug]", examplePath: "/fa/blogs/category/{slug}", auth: "anonymous", shell: "storefront-shell", majorWrapper: "content section", themeCoverage: "shared-shell+page", notes: "Blog category", crawl: true, dynamic: true },
  { id: "blog-author", group: "E", pattern: "/blogs/author/[slug]", examplePath: "/fa/blogs/author/{slug}", auth: "anonymous", shell: "storefront-shell", majorWrapper: "content section", themeCoverage: "shared-shell+page", notes: "Blog author", crawl: true, dynamic: true },
];

export const CUSTOMER_PANEL_NAV_HREFS = [
  "/customer-panel",
  "/customer-panel/orders",
  "/customer-panel/wishlist",
  "/customer-panel/addresses",
  "/customer-panel/notifications",
  "/customer-panel/tickets",
  "/customer-panel/wallet",
  "/customer-panel/gift-cards",
  "/customer-panel/profile",
  "/customer-panel/settings",
] as const;
