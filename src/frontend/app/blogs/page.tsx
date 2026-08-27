import type { Metadata } from "next";
import { BlogsListingClient } from "./blogs-ui";
import { blogOpenGraphLocale, resolveRequestLocale } from "../../lib/i18n/resolve-request-locale";

type Props = { searchParams: Promise<{ locale?: string }> };

/**
 * فهرست بلاگ: canonical همیشه /blogs (fa).
 * openGraph.locale از کوکی/query؛ hreflang عمداً منتشر نمی‌شود تا locale دوم واقعاً منتشر شود.
 */
export async function generateMetadata({ searchParams }: Props): Promise<Metadata> {
  const params = await searchParams;
  const locale = await resolveRequestLocale(params);
  return {
    title: "مجله توبا | مقالات و راهنماها",
    description: "مقالات منتشرشدهٔ توبا درباره خرید آنلاین، محصول و راهنماها.",
    alternates: { canonical: "/blogs" },
    openGraph: {
      locale: blogOpenGraphLocale(locale),
      // hreflang awaits a second published locale — do not invent empty alternates.
    },
  };
}

/** مسیر فهرست بلاگ عمومی. */
export default function BlogsPage() {
  return <BlogsListingClient />;
}
