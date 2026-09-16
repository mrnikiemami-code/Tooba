import { permanentRedirect, notFound } from "next/navigation";
import { loadPublishedLandingPage } from "../storefront/storefront-landing-api.ts";
import { resolveRequestLocale } from "../../lib/i18n/resolve-request-locale.ts";
import { isReservedLandingSlug } from "../../lib/storefront-landing/reserved-slugs.ts";

type Props = { params: Promise<{ slug: string }> };

/**
 * سازگاری: /{slug} قدیمی → /landing/{slug} کاننیکال.
 * مسیرهای رزروشده و صفحات ناموجود ۴۰۴ می‌مانند.
 */
export default async function LegacyLandingSlugRedirect({ params }: Props) {
  const { slug } = await params;
  if (isReservedLandingSlug(slug)) {
    notFound();
  }
  const locale = await resolveRequestLocale();
  const page = await loadPublishedLandingPage(slug, locale);
  if (!page) {
    notFound();
  }
  permanentRedirect(`/landing/${page.slug}`);
}
