/**
 * Immediate App Router pending shell for cart navigations.
 */
export default function CartLoading() {
  return (
    <div
      className="mx-auto max-w-[1800px] px-4 sm:px-6 py-10 space-y-4"
      data-testid="storefront-route-loading"
      aria-busy="true"
      aria-live="polite"
    >
      <p className="sr-only">در حال بارگذاری سبد خرید…</p>
      <div className="h-8 w-40 rounded-lg bg-gray-200 animate-pulse" />
      <div className="h-40 w-full rounded-2xl bg-gray-100 animate-pulse" />
      <div className="h-40 w-full rounded-2xl bg-gray-100 animate-pulse" />
    </div>
  );
}
