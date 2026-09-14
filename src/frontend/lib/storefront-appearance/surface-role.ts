export const STOREFRONT_SURFACE_ROLES = ["page", "section", "alternate", "accent"] as const;
export type StorefrontSurfaceRole = (typeof STOREFRONT_SURFACE_ROLES)[number];

const ROLE_CLASS: Record<StorefrontSurfaceRole, string> = {
  page: "bg-page",
  section: "bg-section-surface",
  alternate: "bg-section-alternate",
  accent: "bg-section-accent",
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
};

export function surfaceRoleClass(role: StorefrontSurfaceRole): string {
  return ROLE_CLASS[role];
}

export function landingSectionSurfaceRole(sectionType: string): StorefrontSurfaceRole {
  return LANDING_SECTION_ROLE[sectionType] ?? "section";
}
