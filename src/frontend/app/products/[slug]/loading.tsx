/**
 * Immediate App Router pending shell for PDP segment navigations.
 */
export default function ProductDetailLoading() {
  return (
    <div
      className="mx-auto max-w-[1800px] px-4 sm:px-6 py-10 space-y-4"
      data-testid="storefront-route-loading"
      aria-busy="true"
      aria-live="polite"
    >
      <p className="sr-only">در حال بارگذاری صفحهٔ کالا…</p>
      <div className="h-8 w-1/3 rounded-lg bg-gray-200 animate-pulse" />
      <div className="grid gap-6 md:grid-cols-2">
        <div className="aspect-[4/5] rounded-2xl bg-gray-100 animate-pulse" />
        <div className="space-y-3">
          <div className="h-6 w-2/3 rounded bg-gray-200 animate-pulse" />
          <div className="h-6 w-1/2 rounded bg-gray-200 animate-pulse" />
          <div className="h-24 w-full rounded-xl bg-gray-100 animate-pulse" />
        </div>
      </div>
    </div>
  );
}
