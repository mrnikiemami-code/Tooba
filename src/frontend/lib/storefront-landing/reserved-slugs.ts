export const RESERVED_LANDING_SLUGS = [
  "account", "admin", "api", "auth", "best-seller", "blog", "blogs", "brand", "brands",
  "cart", "categories", "category", "checkout", "customer-panel", "design-system",
  "en", "fa", "favicon", "home", "icon", "login", "most-viewed", "new-products",
  "not-found", "offers", "order", "page", "pages", "payment", "product", "products",
  "sale", "search", "seller-profile", "sellers", "settings", "shipping", "trending",
  "v1", "vendor-panel",
] as const;

export function isReservedLandingSlug(slug: string): boolean {
  return (RESERVED_LANDING_SLUGS as readonly string[]).includes(slug);
}
