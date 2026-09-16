import { loadStorefrontHome, loadStorefrontListing, storefrontHostOrigin } from "./storefront-api.ts";
import { loadStorefrontMenu, type StorefrontMenuItem } from "./storefront-menu-api.ts";
import type {
  StorefrontArticleItem,
  StorefrontBrandItem,
  StorefrontCategoryItem,
  StorefrontFeaturedReviewItem,
  StorefrontProductCard,
} from "./storefront-model.ts";

export type LandingRenderContext = {
  products: StorefrontProductCard[];
  categories: StorefrontCategoryItem[];
  brands: StorefrontBrandItem[];
  articles: StorefrontArticleItem[];
  reviews: StorefrontFeaturedReviewItem[];
  menus: Record<string, StorefrontMenuItem[]>;
};

export interface StorefrontLandingResolvedItem {
  id: string;
  slug: string | null;
}

export interface StorefrontLandingSection {
  pageSectionId: string;
  sectionType: string;
  sortOrder: number;
  config: string;
  items: StorefrontLandingResolvedItem[];
}

export interface StorefrontLandingPage {
  pageId: string;
  pageType: "Home" | "Landing";
  locale: string;
  slug: string;
  title: string;
  seoTitle: string;
  seoDescription: string | null;
  robotsIndex: boolean;
  robotsFollow: boolean;
  canonicalUrl: string | null;
  ogTitle: string | null;
  ogDescription: string | null;
  ogImageUrl: string | null;
  primaryH1: string;
  templateKey: string;
  sections: StorefrontLandingSection[];
}

export interface StorefrontHomeSelection {
  homePageId: string | null;
  selectedPage: StorefrontLandingPage | null;
  usesCanonicalHome: boolean;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? value as Record<string, unknown> : null;
}

function readString(record: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string") return value;
  }
  return "";
}

function mapSection(raw: unknown): StorefrontLandingSection | null {
  const record = asRecord(raw);
  if (!record) return null;
  const pageSectionId = readString(record, "pageSectionId", "PageSectionId");
  const sectionType = readString(record, "sectionType", "SectionType");
  if (!pageSectionId || !sectionType) return null;
  const itemsRaw = record.items ?? record.Items;
  const items = Array.isArray(itemsRaw)
    ? itemsRaw.map((item) => {
      const row = asRecord(item) ?? {};
      return { id: readString(row, "id", "Id"), slug: readString(row, "slug", "Slug") || null };
    }).filter((item) => item.id)
    : [];
  const configValue = record.config ?? record.Config;
  return {
    pageSectionId,
    sectionType,
    sortOrder: typeof record.sortOrder === "number" ? record.sortOrder : Number(record.SortOrder ?? 0),
    config: typeof configValue === "string" ? configValue : JSON.stringify(configValue ?? {}),
    items,
  };
}

export function mapStorefrontLandingPage(raw: unknown, fallbackLocale = "fa"): StorefrontLandingPage | null {
  const record = asRecord(raw);
  if (!record) return null;
  const slug = readString(record, "slug", "Slug");
  const title = readString(record, "title", "Title");
  if (!slug || !title) return null;
  const sectionsRaw = record.sections ?? record.Sections;
  const pageTypeRaw = readString(record, "pageType", "PageType");
  const robotsIndex = record.robotsIndex ?? record.RobotsIndex;
  const robotsFollow = record.robotsFollow ?? record.RobotsFollow;
  return {
    pageId: readString(record, "pageId", "PageId"),
    pageType: pageTypeRaw === "Home" ? "Home" : "Landing",
    locale: readString(record, "locale", "Locale") || fallbackLocale,
    slug,
    title,
    seoTitle: readString(record, "seoTitle", "SeoTitle") || title,
    seoDescription: readString(record, "seoDescription", "SeoDescription") || null,
    robotsIndex: typeof robotsIndex === "boolean" ? robotsIndex : true,
    robotsFollow: typeof robotsFollow === "boolean" ? robotsFollow : true,
    canonicalUrl: readString(record, "canonicalUrl", "CanonicalUrl") || null,
    ogTitle: readString(record, "ogTitle", "OgTitle") || null,
    ogDescription: readString(record, "ogDescription", "OgDescription") || null,
    ogImageUrl: readString(record, "ogImageUrl", "OgImageUrl") || null,
    primaryH1: readString(record, "primaryH1", "PrimaryH1") || title,
    templateKey: readString(record, "templateKey", "TemplateKey") || "default",
    sections: Array.isArray(sectionsRaw)
      ? sectionsRaw.map(mapSection).filter((row): row is StorefrontLandingSection => row !== null)
      : [],
  };
}

export async function listIndexableLandingPages(): Promise<{ locale: string; slug: string }[]> {
  try {
    const response = await fetch(`${storefrontHostOrigin()}/v1/storefront/pages`, {
      cache: "no-store",
      headers: { Accept: "application/json" },
    });
    if (!response.ok) return [];
    const body = await response.json();
    if (!Array.isArray(body)) return [];
    return body.map((row) => {
      const record = asRecord(row) ?? {};
      return {
        locale: readString(record, "locale", "Locale") || "fa",
        slug: readString(record, "slug", "Slug"),
      };
    }).filter((row) => row.slug);
  } catch {
    return [];
  }
}

export async function loadPublishedLandingPage(slug: string, locale: string): Promise<StorefrontLandingPage | null> {
  try {
    const url = new URL(`${storefrontHostOrigin()}/v1/storefront/pages/${encodeURIComponent(slug)}`);
    url.searchParams.set("locale", locale);
    const response = await fetch(url, { cache: "no-store", headers: { Accept: "application/json" } });
    if (!response.ok) return null;
    return mapStorefrontLandingPage(await response.json(), locale);
  } catch {
    return null;
  }
}

export async function loadStorefrontHomeSelection(): Promise<StorefrontHomeSelection | null> {
  try {
    const response = await fetch(`${storefrontHostOrigin()}/v1/storefront/home-selection`, {
      cache: "no-store",
      headers: { Accept: "application/json" },
    });
    if (!response.ok) return null;
    const payload = asRecord(await response.json()) ?? {};
    const selected = mapStorefrontLandingPage(payload.selectedPage ?? payload.SelectedPage);
    return {
      homePageId: readString(payload, "homePageId", "HomePageId") || null,
      selectedPage: selected,
      usesCanonicalHome: Boolean(payload.usesCanonicalHome ?? payload.UsesCanonicalHome ?? !selected),
    };
  } catch {
    return null;
  }
}

export async function loadLandingRenderContext(
  locale: string,
  page: StorefrontLandingPage,
): Promise<LandingRenderContext> {
  const [home, listing] = await Promise.all([
    loadStorefrontHome(locale),
    loadStorefrontListing({ sort: "newest" }),
  ]);
  const products = uniqueProducts([
    ...(home?.featuredProducts ?? []),
    ...(home?.specialOffers ?? []),
    ...(home?.newArrivals ?? []),
    ...(home?.productRail ?? []),
    ...(listing?.products ?? []),
  ]);
  const menuIds = [...new Set(page.sections.flatMap((section) => {
    if (section.sectionType !== "NavigationMenu") return [];
    try {
      const config = JSON.parse(section.config) as { menuId?: string };
      return config.menuId ? [config.menuId] : [];
    } catch {
      return [];
    }
  }))];
  const menuEntries = await Promise.all(menuIds.map(async (menuId) => [menuId, await loadStorefrontMenu(menuId)] as const));
  return {
    products,
    categories: home?.categories ?? listing?.categories ?? [],
    brands: home?.brands ?? [],
    articles: home?.latestArticles ?? [],
    reviews: home?.featuredReviews ?? [],
    menus: Object.fromEntries(menuEntries),
  };
}

function uniqueProducts(cards: StorefrontProductCard[]): StorefrontProductCard[] {
  const seen = new Set<string>();
  const rows: StorefrontProductCard[] = [];
  for (const card of cards) {
    if (seen.has(card.productId)) continue;
    seen.add(card.productId);
    rows.push(card);
  }
  return rows;
}
