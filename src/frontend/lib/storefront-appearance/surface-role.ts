export const STOREFRONT_SURFACE_ROLES = ["page", "section", "alternate", "accent"] as const;
export type StorefrontSurfaceRole = (typeof STOREFRONT_SURFACE_ROLES)[number];

export const STOREFRONT_ALLOWED_SURFACES = [
  ...STOREFRONT_SURFACE_ROLES,
  "inherit",
  "card",
  "elevated",
  "input",
  "interactive",
  "media",
  "header",
  "footer",
  "overlay",
] as const;
export type StorefrontAllowedSurface = (typeof STOREFRONT_ALLOWED_SURFACES)[number];

export const INTENTIONAL_FIXED_SURFACE_ROLES = [
  "card",
  "elevated",
  "input",
  "interactive",
  "media",
  "header",
  "footer",
  "overlay",
] as const;

const ROLE_CLASS: Record<StorefrontAllowedSurface, string> = {
  page: "bg-page",
  section: "bg-section-surface",
  alternate: "bg-section-alternate",
  accent: "bg-section-accent",
  inherit: "bg-transparent",
  card: "bg-surface",
  elevated: "bg-surface-elevated",
  input: "bg-surface",
  interactive: "bg-secondary",
  media: "bg-background",
  header: "bg-surface",
  footer: "bg-surface",
  overlay: "bg-surface-elevated",
};

const LANDING_SECTION_ROLE: Record<string, StorefrontSurfaceRole> = {
  Hero: "accent",
  PromoBanner: "accent",
  ProductCollection: "alternate",
  ArticleList: "alternate",
  CategoryGrid: "section",
  BrandStrip: "section",
  Reviews: "section",
  RichText: "section",
  NavigationMenu: "section",
  StoryRail: "section",
  BannerShowcase: "alternate",
};

export function surfaceRoleClass(role: StorefrontAllowedSurface): string {
  return ROLE_CLASS[role];
}

export function isStorefrontAllowedSurface(value: string): value is StorefrontAllowedSurface {
  return (STOREFRONT_ALLOWED_SURFACES as readonly string[]).includes(value);
}

export function landingSectionSurfaceRole(sectionType: string): StorefrontSurfaceRole {
  return LANDING_SECTION_ROLE[sectionType] ?? "section";
}
