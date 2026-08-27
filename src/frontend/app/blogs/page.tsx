import type { Metadata } from "next";
import { BlogsListingClient } from "./blogs-ui";
import { blogOpenGraphLocale, resolveRequestLocale } from "../../lib/i18n/resolve-request-locale";
import { buildLocaleAlternates, canonicalForLocale } from "../../lib/i18n/routing.ts";

type Props = { searchParams: Promise<{ locale?: string }> };

/**
 * فهرست بلاگ با canonical locale-prefixed و hreflang واقعی fa/en.
 */
export async function generateMetadata({ searchParams }: Props): Promise<Metadata> {
  await searchParams;
  const locale = await resolveRequestLocale();
  const alternates = buildLocaleAlternates("/blogs", { includeXDefault: true });
  return {
    title: locale === "fa" ? "مجله توبا | مقالات و راهنماها" : "Tooba Magazine | Articles",
    description:
      locale === "fa"
        ? "مقالات منتشرشدهٔ توبا درباره خرید آنلاین، محصول و راهنماها."
        : "Published Tooba articles about shopping, products, and guides.",
    alternates: {
      canonical: canonicalForLocale(locale, "/blogs"),
      languages: alternates.languages,
    },
    openGraph: {
      locale: blogOpenGraphLocale(locale),
    },
  };
}

/** مسیر فهرست بلاگ عمومی. */
export default function BlogsPage() {
  return <BlogsListingClient />;
}
