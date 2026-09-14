import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadPublishedLandingPage } from "../storefront/storefront-landing-api.ts";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";
import { resolveRequestLocale } from "../../lib/i18n/resolve-request-locale.ts";
import { canonicalForLocale, localeToContentApi } from "../../lib/i18n/routing.ts";
import { isReservedLandingSlug } from "../../lib/storefront-landing/reserved-slugs.ts";

type Props = { params: Promise<{ slug: string }> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const locale = await resolveRequestLocale();
  if (isReservedLandingSlug(slug)) {
    return { robots: { index: false, follow: false } };
  }
  const page = await loadPublishedLandingPage(slug, locale);
  if (!page) {
    return { title: locale === "fa" ? "صفحه پیدا نشد | توبا" : "Page not found | Tooba", robots: { index: false, follow: false } };
  }
  return {
    title: page.seoTitle,
    description: page.seoDescription ?? undefined,
    alternates: { canonical: canonicalForLocale(locale, `/${page.slug}`) },
    robots: { index: true, follow: true },
  };
}

/**
 * پوستهٔ موقت Landing از فیلدهای کاننیکال صفحه. Section builder اینجا نیست.
 */
export default async function StoreLandingPageRoute({ params }: Props) {
  const { slug } = await params;
  if (isReservedLandingSlug(slug)) {
    notFound();
  }
  const locale = await resolveRequestLocale();
  const page = await loadPublishedLandingPage(slug, locale);
  if (!page) {
    notFound();
  }
  const home = await loadStorefrontHome(localeToContentApi(locale));
  return (
    <StorefrontShell categories={home?.categories ?? []}>
      <article className="max-w-3xl mx-auto py-12 px-4" data-testid="storefront-landing-page" data-landing-slug={page.slug}>
        <h1 className="text-2xl font-bold text-foreground">{page.title}</h1>
        {page.seoDescription ? <p className="mt-4 text-sm text-foreground/80 leading-7">{page.seoDescription}</p> : null}
      </article>
    </StorefrontShell>
  );
}
