/**
 * TB-245-R4 — شمارنده‌های مالک خواندن سبد برای اثبات نبودن حلقهٔ اثر/رویداد.
 * فقط تشخیص/اندازه‌گیری؛ هیچ fetchی اینجا انجام نمی‌شود.
 */

export type CartOwnerReadKind =
  | "cart-route-mount-cart"
  | "cart-route-mount-pending"
  | "cart-route-mount-login"
  | "cart-route-event-cart"
  | "cart-route-recommendations"
  | "mini-cart-open-cart"
  | "mini-cart-event-cart"
  | "header-badge-mount"
  | "header-badge-event";

export type CartOwnerReadLedger = Readonly<Record<CartOwnerReadKind, number>>;

const EMPTY: CartOwnerReadLedger = {
  "cart-route-mount-cart": 0,
  "cart-route-mount-pending": 0,
  "cart-route-mount-login": 0,
  "cart-route-event-cart": 0,
  "cart-route-recommendations": 0,
  "mini-cart-open-cart": 0,
  "mini-cart-event-cart": 0,
  "header-badge-mount": 0,
  "header-badge-event": 0,
};

let ledger: Record<CartOwnerReadKind, number> = { ...EMPTY };

export function resetCartOwnerReadLedger(): void {
  ledger = { ...EMPTY };
}

export function noteCartOwnerRead(kind: CartOwnerReadKind): void {
  ledger[kind] += 1;
  if (typeof window !== "undefined") {
    (window as Window & { __tb245r4CartReads?: CartOwnerReadLedger }).__tb245r4CartReads = {
      ...ledger,
    };
  }
}

export function snapshotCartOwnerReadLedger(): CartOwnerReadLedger {
  return { ...ledger };
}

/** پس از یک mount مسیر سبد: حداکثر یک cart / pending / login. */
export function cartRouteMountWithinBounds(snapshot: CartOwnerReadLedger): boolean {
  return (
    snapshot["cart-route-mount-cart"] === 1
    && snapshot["cart-route-mount-pending"] === 1
    && snapshot["cart-route-mount-login"] === 1
  );
}

/** پیشنهادها حداکثر یک‌بار پس از usable؛ نه هم‌زمان با mount مسدودکننده. */
export function recommendationsWithinBounds(snapshot: CartOwnerReadLedger): boolean {
  return snapshot["cart-route-recommendations"] <= 1;
}

/** باز شدن مینی‌سبد: حداکثر یک خواندن اولیه. */
export function miniCartOpenWithinBounds(snapshot: CartOwnerReadLedger, opens: number): boolean {
  return snapshot["mini-cart-open-cart"] === opens;
}

/** هر CART_CHANGED حداکثر یک refresh برای مالک سوارشده. */
export function eventRefreshWithinBounds(
  eventReads: number,
  events: number,
): boolean {
  return eventReads === events;
}

/** نسل نتیجهٔ دیررس نباید نصب شود. */
export function acceptStaleGeneration(mine: number, current: number): boolean {
  return mine === current;
}
