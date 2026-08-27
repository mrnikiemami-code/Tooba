import type { Metadata } from "next";
import { BlogDetailClient } from "./blog-detail-ui";
import { loadPublishedArticleBySlug } from "../../content/content-api";
import { blogOpenGraphLocale, resolveRequestLocale } from "../../../lib/i18n/resolve-request-locale";

type Props = {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ locale?: string }>;
};

export async function generateMetadata({ params, searchParams }: Props): Promise<Metadata> {
  const { slug } = await params;
  const query = await searchParams;
  const locale = await resolveRequestLocale(query);
  const article = await loadPublishedArticleBySlug(slug);
  if (!article) {
    return { title: "مقاله پیدا نشد | توبا", robots: { index: false, follow: false } };
  }
  return {
    title: article.seoTitle || article.title,
    description: article.seoDescription || article.excerpt,
    // Canonical stays fa path; no fabricated hreflang until a second locale is published.
    alternates: { canonical: `/blogs/${article.slug}` },
    openGraph: {
      title: article.seoTitle || article.title,
      description: article.seoDescription || article.excerpt,
      type: "article",
      locale: blogOpenGraphLocale(locale),
    },
  };
}

/** مسیر جزئیات مقالهٔ عمومی با slug پایدار. */
export default async function BlogDetailPage({ params }: Props) {
  const { slug } = await params;
  return <BlogDetailClient slug={slug} />;
}
