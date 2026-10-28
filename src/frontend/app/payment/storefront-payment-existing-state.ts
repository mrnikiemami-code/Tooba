import type {
  StorefrontPendingPaymentItem,
  StorefrontPendingPaymentPage,
} from "../storefront/storefront-pending-payment-api.ts";

/**
 * TB240-R5 — وضعیت «آیا برای این سفارش پرداختی آغاز شده است؟» سه‌حالته است و «نامعلوم» هرگز به معنای
 * «آغاز نشده» نیست.
 *
 * <para>
 * خواندن فهرست پرداخت‌های در انتظار ممکن است شکست بخورد (شبکه/۵xx/۴۰۱). در آن حالت هیچ شاهدی نداریم که
 * سفارش تازه است؛ باز کردن فرم پرداخت تازه می‌تواند سفارشِ ازپیش‌آغاز‌شده را دوباره به شروع پرداخت ببرد.
 * بنابراین فقط یک خواندنِ <b>موفق</b> که آن سفارش را در فهرست نداشته باشد اجازهٔ فرم تازه می‌دهد.
 * </para>
 */
export type PendingStatusRead =
  | { readonly status: "verified"; readonly page: StorefrontPendingPaymentPage | null }
  | { readonly status: "failed" };

/** تصمیمِ فقط-خواندنیِ سطح صفحه؛ هیچ paymentId/وضعیتی جعل نمی‌شود. */
export type ExistingPaymentState =
  | { readonly kind: "none" }
  | { readonly kind: "started"; readonly paymentId: string; readonly awaitingManual: boolean }
  | { readonly kind: "unknown" };

/**
 * وضعیت پرداخت موجود را از نتیجهٔ خواندن فهرست در انتظار استنتاج می‌کند.
 * - خواندن ناموفق ⇒ `unknown` (فرم تازه بسته می‌شود و فقط تلاش دوباره/خطا نمایش داده می‌شود)
 * - خواندن موفق و بدون ردیف همین سفارش ⇒ `none` (فرم تازه مجاز است)
 * - خواندن موفق با ردیف همین سفارش ⇒ `started`؛ `awaitingManual` فقط وقتی خودِ ردیف
 *   «کارت‌به‌کارت در انتظار بررسی» باشد.
 */
export function resolveExistingPaymentState(
  read: PendingStatusRead,
  checkoutId: string | null | undefined,
): ExistingPaymentState {
  if (read.status === "failed") {
    return { kind: "unknown" };
  }
  const target = (checkoutId ?? "").trim();
  if (!target) {
    return { kind: "unknown" };
  }
  const item: StorefrontPendingPaymentItem | null =
    read.page?.items.find((row) => row.checkoutId === target) ?? null;
  if (!item) {
    return { kind: "none" };
  }
  const paymentId = (item.paymentId ?? "").trim();
  if (!paymentId) {
    // ردیف بدون شناسهٔ پرداخت به‌معنای پرداختِ آغازشده نیست و شناسه‌ای هم ساخته نمی‌شود.
    return { kind: "unknown" };
  }
  return { kind: "started", paymentId, awaitingManual: item.isManualAwaitingReview === true };
}

/** آیا کاربر می‌تواند دکمهٔ پرداخت تازه را ببیند/بزند؟ فقط `none` یا سفارشِ تازه‌ثبت‌نشده. */
export function allowsFreshPaymentAction(
  state: ExistingPaymentState,
  mode: "cart" | "checkout",
): boolean {
  if (mode === "cart") {
    // سبد هنوز سفارش نساخته است؛ وضعیت پرداخت سفارش دیگری اینجا معنا ندارد.
    return true;
  }
  return state.kind === "none";
}

