import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BlogDetailClient } from "./blog-detail-ui";
import { loadPublishedArticleBySlug } from "../../content/content-api";
import { blogOpenGraphLocale, resolveRequestLocale } from "../../../lib/i18n/resolve-request-locale";
import { canonicalForLocale, localeToContentApi } from "../../../lib/i18n/routing.ts";
import { storefrontMediaUrl } from "../../storefront/storefront-api";

type Props = {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ locale?: string }>;
};

export async function generateMetadata({ params, searchParams }: Props): Promise<Metadata> {
  const { slug } = await params;
  const query = await searchParams;
  const locale = await resolveRequestLocale(query);
  const contentLocale = localeToContentApi(locale);
  const article = await loadPublishedArticleBySlug(slug, contentLocale);
  if (!article) {
    return { title: locale === "fa" ? "مقاله پیدا نشد | توبا" : "Article not found | Tooba", robots: { index: false, follow: false } };
  }
  const internalPath = `/blogs/${article.slug}`;
  const seoImageId = article.seoImageMediaAssetId ?? article.coverMediaAssetId;
  const ogImages = seoImageId ? [{ url: storefrontMediaUrl(seoImageId) ?? "" }].filter((item) => item.url) : undefined;
  return {
    title: article.seoTitle || article.title,
    description: article.seoDescription || article.excerpt,
    alternates: { canonical: canonicalForLocale(locale, internalPath) },
    openGraph: {
      title: article.seoTitle || article.title,
      description: article.seoDescription || article.excerpt,
      type: "article",
      locale: blogOpenGraphLocale(locale),
      images: ogImages,
    },
    robots: { index: true, follow: true },
  };
}

/** مسیر جزئیات مقالهٔ عمومی — lookup locale+slug بدون fallback. */
export default async function BlogDetailPage({ params, searchParams }: Props) {
  const { slug } = await params;
  const query = await searchParams;
  const locale = await resolveRequestLocale(query);
  const contentLocale = localeToContentApi(locale);
  const article = await loadPublishedArticleBySlug(slug, contentLocale);
  if (!article) {
    notFound();
  }
  return <BlogDetailClient slug={slug} contentLocale={contentLocale} />;
}
