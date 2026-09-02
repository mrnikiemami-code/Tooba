import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { loadPublicCategory } from "../../content/content-api";
import { BlogsTaxonomyListingClient } from "../blogs-taxonomy-ui";
import { blogsCategoryPath, blogsCopy } from "../blogs-copy.ts";
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
  const copy = blogsCopy(locale);
  const category = await loadPublicCategory(slug, contentLocale);
  if (!category) {
    return {
      title: locale === "fa" ? `${copy.categoryHeading} پیدا نشد | توبا` : `${copy.categoryHeading} not found | Tooba`,
      robots: { index: false, follow: false },
    };
  }
  const internalPath = blogsCategoryPath(category.slug);
  const ogImages = category.imageMediaAssetId
    ? [{ url: storefrontMediaUrl(category.imageMediaAssetId) ?? "" }].filter((item) => item.url)
    : undefined;
  return {
    title: category.seoTitle || category.name,
    description: category.seoDescription || category.shortDescription || undefined,
    alternates: { canonical: category.canonicalPath ?? canonicalForLocale(locale, internalPath) },
    openGraph: {
      title: category.seoTitle || category.name,
      description: category.seoDescription || category.shortDescription || undefined,
      locale: blogOpenGraphLocale(locale),
      images: ogImages,
    },
    robots: { index: true, follow: true },
  };
}

/** مسیر عمومی دستهٔ مجله — /blogs/category/{slug}. */
export default async function BlogCategoryPage({ params, searchParams }: Props) {
  const { slug } = await params;
  const query = await searchParams;
  const locale = await resolveRequestLocale(query);
  const contentLocale = localeToContentApi(locale);
  const category = await loadPublicCategory(slug, contentLocale);
  if (!category) {
    notFound();
  }
  return (
    <BlogsTaxonomyListingClient
      kind="category"
      slug={category.slug}
      heading={category.name}
      description={category.shortDescription ?? category.description}
    />
  );
}
