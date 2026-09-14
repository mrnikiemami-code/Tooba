import { storefrontHostOrigin } from "./storefront-api.ts";

export interface StorefrontLandingPage {
  pageId: string;
  locale: string;
  slug: string;
  title: string;
  seoTitle: string;
  seoDescription: string | null;
  templateKey: string;
}

export async function loadPublishedLandingPage(slug: string, locale: string): Promise<StorefrontLandingPage | null> {
  try {
    const url = new URL(`${storefrontHostOrigin()}/v1/storefront/pages/${encodeURIComponent(slug)}`);
    url.searchParams.set("locale", locale);
    const response = await fetch(url, { cache: "no-store", headers: { Accept: "application/json" } });
    if (!response.ok) {
      return null;
    }
    const payload = await response.json() as Partial<StorefrontLandingPage>;
    if (!payload.slug || !payload.title) {
      return null;
    }
    return {
      pageId: String(payload.pageId ?? ""),
      locale: String(payload.locale ?? locale),
      slug: payload.slug,
      title: payload.title,
      seoTitle: payload.seoTitle ?? payload.title,
      seoDescription: payload.seoDescription ?? null,
      templateKey: payload.templateKey ?? "default",
    };
  } catch {
    return null;
  }
}
