import { StorefrontShell } from "./storefront/storefront-shell.tsx";
import { StorefrontProductCardView } from "./storefront/storefront-product-card.tsx";
import { loadStorefrontHome } from "./storefront/storefront-api.ts";

/**
 * خانهٔ فروشگاه زنده. داده از Host می‌آید نه از JSON دمو.
 */
export default async function HomePage() {
  const home = await loadStorefrontHome();
  if (!home) {
    return (
      <StorefrontShell categories={[]}>
        <div className="sf-error">فروشگاه زنده در دسترس نیست. Host باید روی درگاه ۵۰۸۸ پاسخ بدهد.</div>
      </StorefrontShell>
    );
  }

  return (
    <StorefrontShell categories={home.categories}>
      <section className="sf-hero">
        <h1>{home.heroTitle}</h1>
        <p>{home.heroSubtitle}</p>
      </section>
      <section className="sf-section">
        <h2>کالاهای فروشگاه</h2>
        {home.featuredProducts.length === 0 ? (
          <div className="sf-empty">کالای قابل‌فروش منتشرشده‌ای برای نمایش نیست.</div>
        ) : (
          <div className="sf-grid">
            {home.featuredProducts.map((card) => (
              <StorefrontProductCardView key={card.productId} card={card} />
            ))}
          </div>
        )}
      </section>
    </StorefrontShell>
  );
}
