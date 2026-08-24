import Link from "next/link";
import "./storefront.css";
import type { ReactNode } from "react";
import type { StorefrontCategoryItem } from "./storefront-model.ts";

/**
 * پوستهٔ فروشگاهی با ساختار هدر/جستجو/رده/فوتر شبیه Shopeiva و توکن آبی Tooba.
 */
export function StorefrontShell({
  categories,
  children,
  activeCategoryId,
}: {
  categories: StorefrontCategoryItem[];
  children: ReactNode;
  activeCategoryId?: string;
}) {
  return (
    <div className="sf-shell">
      <div className="sf-topbar">ارسال سریع سفارش‌های فروشگاهی · پشتیبانی خرید</div>
      <header className="sf-header">
        <div className="sf-header-row">
          <Link className="sf-logo" href="/">
            توبا
          </Link>
          <form className="sf-search" action="/products" method="get">
            <input name="q" placeholder="جستجو در کالاهای فروشگاه" aria-label="جستجوی کالا" />
            <button type="submit">جستجو</button>
          </form>
          <div className="sf-actions">
            <span>سبد خرید</span>
            <Link href="/admin/products">میزکار</Link>
          </div>
        </div>
        <nav className="sf-cats" aria-label="رده‌ها">
          <Link href="/products">همه کالاها</Link>
          {categories.map((category) => (
            <Link
              key={category.categoryId}
              href={`/products?categoryId=${category.categoryId}`}
              className={activeCategoryId === category.categoryId ? "is-active" : undefined}
            >
              {category.name}
            </Link>
          ))}
        </nav>
      </header>
      {children}
      <footer className="sf-footer">
        <div className="sf-footer-inner">
          <div>
            <strong>فروشگاه توبا</strong>
            <p>تجربهٔ ویترین از الگوی Shopeiva با دادهٔ زندهٔ Catalog، Offer، قیمت و موجودی.</p>
          </div>
          <div>
            <p>درباره فروشگاه</p>
            <p>راهنمای خرید</p>
          </div>
          <div>
            <p>تماس با پشتیبانی</p>
            <p>قوانین انتشار کالا</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
