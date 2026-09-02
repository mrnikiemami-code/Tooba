import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { loadPublicAuthor } from "../../content/content-api";
import { BlogsTaxonomyListingClient } from "../blogs-taxonomy-ui";
import { blogsAuthorPath, blogsCopy } from "../blogs-copy.ts";
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
  const author = await loadPublicAuthor(slug, contentLocale);
  if (!author) {
    return {
      title: locale === "fa" ? `${copy.authorHeading} پیدا نشد | توبا` : `${copy.authorHeading} not found | Tooba`,
      robots: { index: false, follow: false },
    };
  }
  const internalPath = blogsAuthorPath(author.slug);
  const ogImages = author.profileImageMediaAssetId
    ? [{ url: storefrontMediaUrl(author.profileImageMediaAssetId) ?? "" }].filter((item) => item.url)
    : undefined;
  return {
    title: author.displayName,
    description: author.shortBio || undefined,
    alternates: { canonical: author.canonicalPath ?? canonicalForLocale(locale, internalPath) },
    openGraph: {
      title: author.displayName,
      description: author.shortBio || undefined,
      locale: blogOpenGraphLocale(locale),
      images: ogImages,
    },
    robots: { index: true, follow: true },
  };
}

/** مسیر عمومی نویسندهٔ مجله — /blogs/author/{slug}. */
export default async function BlogAuthorPage({ params, searchParams }: Props) {
  const { slug } = await params;
  const query = await searchParams;
  const locale = await resolveRequestLocale(query);
  const contentLocale = localeToContentApi(locale);
  const author = await loadPublicAuthor(slug, contentLocale);
  if (!author) {
    notFound();
  }
  return (
    <BlogsTaxonomyListingClient
      kind="author"
      slug={author.slug}
      heading={author.displayName}
      description={author.shortBio ?? author.fullBio}
    />
  );
}
