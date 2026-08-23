"use client";

/**
 * مرز خطای سطح-مسیر. جزئیات استثنا و stack به کاربر نشان داده نمی‌شود؛ فقط digest اختیاری برای همبستگی.
 * ظاهر نهایی خطا پس از استخراج Design System و ACCEPT بصری Architect خواهد آمد.
 */
export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <main>
      <h1>Something went wrong</h1>
      <p>A safe fallback. Commercial error UX is not this bootstrap.</p>
      {error.digest ? <p>Reference: {error.digest}</p> : null}
      <button type="button" onClick={() => reset()}>
        Try again
      </button>
    </main>
  );
}
