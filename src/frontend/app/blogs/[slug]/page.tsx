import type { Metadata } from "next";
import { BlogDetailClient } from "./blog-detail-ui";
import { loadPublishedArticleBySlug } from "../../content/content-api";

type Props = { params: Promise<{ slug: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const article = await loadPublishedArticleBySlug(slug);
  if (!article) {
    return { title: "مقاله پیدا نشد | توبا", robots: { index: false, follow: false } };
  }
  return {
    title: article.seoTitle || article.title,
    description: article.seoDescription || article.excerpt,
    alternates: { canonical: `/blogs/${article.slug}` },
    openGraph: {
      title: article.seoTitle || article.title,
      description: article.seoDescription || article.excerpt,
      type: "article",
      locale: "fa_IR",
    },
  };
}

/** مسیر جزئیات مقالهٔ عمومی با slug پایدار. */
export default async function BlogDetailPage({ params }: Props) {
  const { slug } = await params;
  return <BlogDetailClient slug={slug} />;
}
