import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { StorefrontShell } from "../../storefront/storefront-shell.tsx";
import { StorefrontCategoryPlpView } from "../../storefront/storefront-category-plp.tsx";
import { loadStorefrontCategoryPlp, loadStorefrontHome } from "../../storefront/storefront-api.ts";
import { resolveRequestLocale } from "../../../lib/i18n/resolve-request-locale.ts";
import { canonicalForLocale, localeToContentApi } from "../../../lib/i18n/routing.ts";
import { openGraphLocaleFor } from "../../../lib/i18n/locale.ts";
import type { StorefrontListingSort } from "../../storefront/storefront-model.ts";

type CategorySearchParams = Record<string, string | string[] | undefined>;

function readSort(value?: string): StorefrontListingSort {
  return value === "newest" || value === "price-asc" || value === "price-desc" ? value : "default";
}

function firstParam(value: string | string[] | undefined): string | undefined {
  return Array.isArray(value) ? value[0] : value;
}

function extractFilterQuery(params: CategorySearchParams): Record<string, string> {
  const filters: Record<string, string> = {};
  for (const [key, raw] of Object.entries(params)) {
    if (!key.startsWith("f_") && !key.startsWith("r_") && !key.startsWith("b_")) continue;
    const value = firstParam(raw);
    if (value) filters[key] = value;
  }
  return filters;
}

function hasActiveFilters(params: CategorySearchParams): boolean {
  if (Object.keys(extractFilterQuery(params)).length > 0) return true;
  const sort = firstParam(params.sort);
  return Boolean(sort && sort !== "default");
}

/**
 * SEO: صفحهٔ رده بدون فیلتر index می‌شود؛ فیلتر/مرتب‌سازی غیرپیش‌فرض noindex.
 * canonical همیشه مسیر تمیز رده است (بدون query فیلتر).
 */
export async function generateMetadata({
  params,
  searchParams,
}: {
  params: Promise<{ slug: string }>;
  searchParams: Promise<CategorySearchParams>;
}): Promise<Metadata> {
  const { slug } = await params;
  const query = await searchParams;
  const locale = await resolveRequestLocale();
  const filterQuery = extractFilterQuery(query);
  const page = await loadStorefrontCategoryPlp(slug, {
    locale: localeToContentApi(locale),
    filterQuery,
    sort: readSort(firstParam(query.sort)),
    page: Math.max(1, Number.parseInt(firstParam(query.page) ?? "1", 10) || 1),
  });
  if (!page) {
    return { title: "رده پیدا نشد" };
  }
  const internalPath = `/category/${page.slug}`;
  const filtered = hasActiveFilters(query);
  return {
    title: `${page.name} | توبا`,
    description: page.shortDescription ?? `کالاهای رده ${page.name} در فروشگاه توبا`,
    alternates: { canonical: canonicalForLocale(locale, internalPath) },
    openGraph: { locale: openGraphLocaleFor(locale) },
    robots: filtered ? { index: false, follow: true } : { index: true, follow: true },
  };
}

/**
 * PLP رده در مسیر canonical `/{locale}/category/{localizedSlug}`.
 */
export default async function CategoryPlpPage({
  params,
  searchParams,
}: {
  params: Promise<{ slug: string }>;
  searchParams: Promise<CategorySearchParams>;
}) {
  const { slug } = await params;
  const query = await searchParams;
  const locale = await resolveRequestLocale();
  const filterQuery = extractFilterQuery(query);
  const pageNumber = Math.max(1, Number.parseInt(firstParam(query.page) ?? "1", 10) || 1);

  const [plp, home] = await Promise.all([
    loadStorefrontCategoryPlp(slug, {
      locale: localeToContentApi(locale),
      filterQuery,
      sort: readSort(firstParam(query.sort)),
      page: pageNumber,
    }),
    loadStorefrontHome(),
  ]);

  if (!plp) {
    notFound();
  }

  if (plp.isRedirect && plp.redirectToPath) {
    const suffix = new URLSearchParams();
    for (const [key, value] of Object.entries(filterQuery)) suffix.set(key, value);
    const sort = firstParam(query.sort);
    if (sort && sort !== "default") suffix.set("sort", sort);
    if (pageNumber > 1) suffix.set("page", String(pageNumber));
    const q = suffix.toString();
    redirect(q ? `${plp.redirectToPath}?${q}` : plp.redirectToPath);
  }

  return (
    <StorefrontShell categories={home?.categories ?? []} searchCatalog={plp.products}>
      <StorefrontCategoryPlpView page={plp} activeFilters={filterQuery} />
    </StorefrontShell>
  );
}
