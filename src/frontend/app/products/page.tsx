import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { StorefrontProductCardView } from "../storefront/storefront-product-card.tsx";
import { loadStorefrontListing } from "../storefront/storefront-api.ts";

/**
 * فهرست زندهٔ کالا. فیلتر رده و جستجو فقط روی دادهٔ Host است.
 */
export default async function ProductsPage({
  searchParams,
}: {
  searchParams: Promise<{ q?: string; categoryId?: string }>;
}) {
  const params = await searchParams;
  const listing = await loadStorefrontListing(params.q, params.categoryId);
  if (!listing) {
    return (
      <StorefrontShell categories={[]}>
        <div className="sf-error">فهرست زنده در دسترس نیست.</div>
      </StorefrontShell>
    );
  }

  return (
    <StorefrontShell categories={listing.categories} activeCategoryId={params.categoryId}>
      <section className="sf-section">
        <h2>{params.q ? `نتیجه جستجو برای «${params.q}»` : "همه کالاها"}</h2>
        {listing.products.length === 0 ? (
          <div className="sf-empty">کالایی با این فیلتر پیدا نشد.</div>
        ) : (
          <div className="sf-grid">
            {listing.products.map((card) => (
              <StorefrontProductCardView key={card.productId} card={card} />
            ))}
          </div>
        )}
      </section>
    </StorefrontShell>
  );
}
