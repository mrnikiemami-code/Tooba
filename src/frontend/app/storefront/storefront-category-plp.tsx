"use client";

import { LocalizedLink as Link } from "../../lib/i18n/LocalizedLink.tsx";
import { useMemo, useState } from "react";
import {
  ChevronLeft,
  ChevronRight,
  Layers,
  Package,
  SlidersHorizontal,
  X,
} from "lucide-react";
import { StorefrontProductCardView } from "./storefront-product-card.tsx";
import type {
  StorefrontAppliedFilterChip,
  StorefrontCategoryBreadcrumbItem,
  StorefrontCategoryChildItem,
  StorefrontCategoryPlpPage,
  StorefrontListingSort,
  StorefrontPlpFacet,
} from "./storefront-model.ts";

function isRangeFacet(facet: StorefrontPlpFacet): boolean {
  return (
    facet.displayType.toLowerCase().includes("range") ||
    facet.valueKind.toLowerCase() === "number"
  );
}

function isBooleanFacet(facet: StorefrontPlpFacet): boolean {
  return (
    facet.displayType.toLowerCase().includes("boolean") ||
    facet.valueKind.toLowerCase() === "boolean"
  );
}

function filterParamKey(facet: StorefrontPlpFacet): string {
  if (isRangeFacet(facet)) return `r_${facet.code}`;
  if (isBooleanFacet(facet)) return `b_${facet.code}`;
  return `f_${facet.code}`;
}

/**
 * PLP ردهٔ canonical: breadcrumb، زیررده‌ها، facet پویا، چیپ فیلتر، مرتب‌سازی و شبکهٔ کارت.
 * مسیر پایه `/category/{slug}` است؛ فیلترها در query با پیشوند f_/r_/b_.
 */
export function StorefrontCategoryPlpView({
  page,
  activeFilters,
}: {
  page: StorefrontCategoryPlpPage;
  activeFilters: Record<string, string>;
}) {
  const [mobileFiltersOpen, setMobileFiltersOpen] = useState(false);
  const pageCount = Math.max(1, Math.ceil(page.totalCount / page.pageSize));
  const basePath = `/category/${page.slug}`;

  const hrefFor = (overrides: Record<string, string | undefined>) => {
    const params = new URLSearchParams();
    const merged: Record<string, string | undefined> = {
      ...activeFilters,
      sort: page.sort === "default" ? undefined : page.sort,
      page: page.page > 1 ? String(page.page) : undefined,
      ...overrides,
    };
    for (const [key, value] of Object.entries(merged)) {
      if (value) params.set(key, value);
    }
    const suffix = params.toString();
    return suffix ? `${basePath}?${suffix}` : basePath;
  };

  const toggleEnumValue = (facet: StorefrontPlpFacet, value: string) => {
    const key = filterParamKey(facet);
    const current = (activeFilters[key] ?? "")
      .split(",")
      .map((v) => v.trim())
      .filter(Boolean);
    const next = current.includes(value)
      ? current.filter((v) => v !== value)
      : [...current, value];
    return hrefFor({
      [key]: next.length ? next.join(",") : undefined,
      page: undefined,
    });
  };

  const removeChip = (chip: StorefrontAppliedFilterChip) => {
    const facet = page.facets.find((f) => f.code === chip.code);
    if (!facet) return hrefFor({ page: undefined });
    const key = filterParamKey(facet);
    if (isRangeFacet(facet) || isBooleanFacet(facet)) {
      return hrefFor({ [key]: undefined, page: undefined });
    }
    const current = (activeFilters[key] ?? "")
      .split(",")
      .map((v) => v.trim())
      .filter(Boolean)
      .filter((v) => v.toLowerCase() !== chip.value.toLowerCase());
    return hrefFor({ [key]: current.length ? current.join(",") : undefined, page: undefined });
  };

  const selectedEnum = (facet: StorefrontPlpFacet) =>
    new Set(
      (activeFilters[filterParamKey(facet)] ?? "")
        .split(",")
        .map((v) => v.trim().toLowerCase())
        .filter(Boolean),
    );

  const filterPanel = (
    <div className="space-y-5" data-testid="category-plp-filters">
      <h2 className="font-bold text-sm flex items-center gap-2 text-gray-900">
        <Layers className="w-4 h-4 text-[#2563EB]" />
        فیلترها
      </h2>

      {page.subcategories.length > 0 ? (
        <div className="space-y-1" data-testid="category-plp-subcategories">
          <div className="text-xs font-semibold text-gray-500 px-1 mb-1">زیررده‌ها</div>
          {page.subcategories.map((child: StorefrontCategoryChildItem) => (
            <Link
              key={child.categoryId}
              href={child.path}
              unprefixed
              className="flex items-center justify-between px-3 py-2.5 rounded-xl text-sm text-gray-700 hover:bg-gray-50"
              onClick={() => setMobileFiltersOpen(false)}
            >
              <span>{child.name}</span>
              <ChevronLeft className="w-3 h-3 opacity-50" />
            </Link>
          ))}
        </div>
      ) : null}

      {page.facets.map((facet) => {
        if (isRangeFacet(facet)) {
          const key = filterParamKey(facet);
          const current = activeFilters[key] ?? "";
          const [minPart, maxPart] = current.split(":");
          return (
            <div key={facet.definitionId} className="space-y-2" data-testid={`plp-facet-${facet.code}`}>
              <div className="text-xs font-semibold text-gray-700">{facet.localizedName}</div>
              <form
                className="flex items-center gap-2"
                onSubmit={(event) => {
                  event.preventDefault();
                  const form = event.currentTarget;
                  const min = (form.elements.namedItem("min") as HTMLInputElement).value.trim();
                  const max = (form.elements.namedItem("max") as HTMLInputElement).value.trim();
                  const next = min || max ? `${min}:${max}` : undefined;
                  window.location.href = hrefFor({ [key]: next, page: undefined });
                }}
              >
                <input
                  name="min"
                  defaultValue={minPart ?? ""}
                  placeholder={facet.rangeMin != null ? String(facet.rangeMin) : "از"}
                  className="w-full rounded-lg border border-gray-200 px-2 py-1.5 text-xs"
                  dir="ltr"
                />
                <span className="text-gray-400 text-xs">–</span>
                <input
                  name="max"
                  defaultValue={maxPart ?? ""}
                  placeholder={facet.rangeMax != null ? String(facet.rangeMax) : "تا"}
                  className="w-full rounded-lg border border-gray-200 px-2 py-1.5 text-xs"
                  dir="ltr"
                />
                <button type="submit" className="shrink-0 rounded-lg bg-[#2563EB] text-white px-2 py-1.5 text-[10px] font-bold">
                  اعمال
                </button>
              </form>
            </div>
          );
        }

        return (
          <div key={facet.definitionId} className="space-y-1" data-testid={`plp-facet-${facet.code}`}>
            <div className="text-xs font-semibold text-gray-700 px-1">{facet.localizedName}</div>
            {facet.options.map((option) => {
              const selected = selectedEnum(facet).has(option.value.toLowerCase());
              return (
                <Link
                  key={`${facet.code}-${option.value}`}
                  href={toggleEnumValue(facet, option.value)}
                  className={`flex items-center justify-between px-3 py-2 rounded-xl text-sm transition-colors ${
                    selected ? "bg-[#2563EB] text-white" : "text-gray-700 hover:bg-gray-50"
                  }`}
                  onClick={() => setMobileFiltersOpen(false)}
                >
                  <span>{option.label}</span>
                  {facet.showCounts && option.count != null ? (
                    <span className="text-[10px] opacity-70">({option.count.toLocaleString("fa-IR")})</span>
                  ) : null}
                </Link>
              );
            })}
          </div>
        );
      })}
    </div>
  );

  const sortOptions: { value: StorefrontListingSort; label: string }[] = [
    { value: "default", label: "پیش‌فرض" },
    { value: "newest", label: "جدیدترین" },
    { value: "price-asc", label: "ارزان‌ترین" },
    { value: "price-desc", label: "گران‌ترین" },
  ];

  const breadcrumb = useMemo(() => page.breadcrumb, [page.breadcrumb]);

  return (
    <div className="mt-6" data-testid="category-plp-page">
      <nav className="flex flex-wrap items-center gap-1 text-xs text-gray-500 mb-4" data-testid="category-plp-breadcrumb">
        <Link href="/" className="hover:text-[#2563EB]">
          خانه
        </Link>
        {breadcrumb.map((crumb: StorefrontCategoryBreadcrumbItem) => (
          <span key={crumb.categoryId} className="inline-flex items-center gap-1">
            <ChevronLeft className="w-3 h-3 opacity-40" />
            <Link href={crumb.path} unprefixed className="hover:text-[#2563EB]">
              {crumb.name}
            </Link>
          </span>
        ))}
      </nav>

      <div className="bg-white rounded-2xl border border-gray-100 p-5 mb-4">
        <h1 className="text-xl sm:text-2xl font-black text-gray-900" data-testid="category-plp-title">
          {page.name}
        </h1>
        {page.shortDescription ? (
          <p className="mt-2 text-sm text-gray-600 leading-relaxed">{page.shortDescription}</p>
        ) : null}
        <p className="mt-2 text-xs text-gray-400">
          {page.totalCount.toLocaleString("fa-IR")} کالا در این رده و زیررده‌ها
        </p>
      </div>

      {page.appliedFilters.length > 0 ? (
        <div className="flex flex-wrap gap-2 mb-4" data-testid="category-plp-applied-chips">
          {page.appliedFilters.map((chip) => (
            <Link
              key={`${chip.code}-${chip.value}`}
              href={removeChip(chip)}
              className="inline-flex items-center gap-1 rounded-full bg-blue-50 text-[#2563EB] px-3 py-1 text-xs font-semibold"
            >
              {chip.label}: {chip.displayValue}
              <X className="w-3 h-3" />
            </Link>
          ))}
          <Link href={basePath} className="text-xs text-gray-500 underline underline-offset-2 self-center">
            پاک کردن فیلترها
          </Link>
        </div>
      ) : null}

      <div className="lg:hidden mb-3">
        <button
          type="button"
          className="inline-flex items-center gap-2 rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm font-semibold"
          onClick={() => setMobileFiltersOpen(true)}
          data-testid="category-plp-open-filters"
        >
          <SlidersHorizontal className="w-4 h-4" />
          فیلترها
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[260px_minmax(0,1fr)] gap-4">
        <aside className="hidden lg:block bg-white rounded-2xl border border-gray-100 p-4 h-fit sticky top-24">
          {filterPanel}
        </aside>

        <section className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3 bg-white rounded-2xl border border-gray-100 px-4 py-3">
            <div className="text-xs text-gray-500">مرتب‌سازی</div>
            <div className="flex flex-wrap gap-2">
              {sortOptions.map((option) => (
                <Link
                  key={option.value}
                  href={hrefFor({
                    sort: option.value === "default" ? undefined : option.value,
                    page: undefined,
                  })}
                  className={`rounded-lg px-3 py-1.5 text-xs font-semibold ${
                    page.sort === option.value
                      ? "bg-[#2563EB] text-white"
                      : "bg-gray-50 text-gray-700 hover:bg-gray-100"
                  }`}
                >
                  {option.label}
                </Link>
              ))}
            </div>
          </div>

          {page.products.length === 0 ? (
            <div className="bg-white rounded-2xl border border-dashed border-gray-200 py-16 text-center text-sm text-gray-500">
              <Package className="w-8 h-8 mx-auto mb-2 opacity-40" />
              کالایی با این فیلترها یافت نشد.
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-3" data-testid="category-plp-grid">
              {page.products.map((card) => (
                <StorefrontProductCardView key={card.productId} card={card} />
              ))}
            </div>
          )}

          {pageCount > 1 ? (
            <div className="flex items-center justify-center gap-2 pt-2" data-testid="category-plp-pagination">
              {page.page > 1 ? (
                <Link
                  href={hrefFor({ page: String(page.page - 1) })}
                  className="inline-flex items-center gap-1 rounded-xl border border-gray-200 bg-white px-3 py-2 text-xs font-semibold"
                >
                  <ChevronRight className="w-3 h-3" />
                  قبلی
                </Link>
              ) : null}
              <span className="text-xs text-gray-500">
                صفحه {page.page.toLocaleString("fa-IR")} از {pageCount.toLocaleString("fa-IR")}
              </span>
              {page.page < pageCount ? (
                <Link
                  href={hrefFor({ page: String(page.page + 1) })}
                  className="inline-flex items-center gap-1 rounded-xl border border-gray-200 bg-white px-3 py-2 text-xs font-semibold"
                >
                  بعدی
                  <ChevronLeft className="w-3 h-3" />
                </Link>
              ) : null}
            </div>
          ) : null}
        </section>
      </div>

      {mobileFiltersOpen ? (
        <div className="fixed inset-0 z-50 lg:hidden" data-testid="category-plp-mobile-filters">
          <button
            type="button"
            className="absolute inset-0 bg-black/40"
            aria-label="بستن فیلترها"
            onClick={() => setMobileFiltersOpen(false)}
          />
          <div className="absolute inset-y-0 right-0 w-[min(100%,320px)] bg-white shadow-xl p-4 overflow-y-auto">
            <div className="flex items-center justify-between mb-4">
              <span className="font-bold text-sm">فیلترها</span>
              <button type="button" onClick={() => setMobileFiltersOpen(false)} aria-label="بستن">
                <X className="w-5 h-5" />
              </button>
            </div>
            {filterPanel}
          </div>
        </div>
      ) : null}
    </div>
  );
}
