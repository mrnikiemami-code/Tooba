/**
 * نشست انتقال هویت مهمان سبد. حقیقت مبلغ/تعداد در sessionStorage نیست.
 */
const CART_ID_KEY = "tooba.storefront.cartId";
const GUEST_SECRET_KEY = "tooba.storefront.guestSecret";

export const CART_CHANGED_EVENT = "tooba-cart-changed";

/**
 * شناسه و راز مهمان را برای حمل درخواست می‌خواند. Totals را نگه نمی‌دارد.
 */
export function readCartSession(): { cartId: string | null; guestSecret: string | null } {
  if (typeof window === "undefined") {
    return { cartId: null, guestSecret: null };
  }
  return {
    cartId: window.sessionStorage.getItem(CART_ID_KEY),
    guestSecret: window.sessionStorage.getItem(GUEST_SECRET_KEY),
  };
}

/**
 * پس از ساخت سبد، فقط شناسه و راز را ذخیره می‌کند.
 */
export function writeCartSession(cartId: string, guestSecret: string | null | undefined): void {
  window.sessionStorage.setItem(CART_ID_KEY, cartId);
  if (guestSecret) {
    window.sessionStorage.setItem(GUEST_SECRET_KEY, guestSecret);
  }
  window.dispatchEvent(new Event(CART_CHANGED_EVENT));
}

/**
 * نشست سبد را پاک می‌کند. برای خالی شدن پس از حذف همهٔ خطوط لازم نیست مگر سبد منقضی شود.
 */
export function clearCartSession(): void {
  window.sessionStorage.removeItem(CART_ID_KEY);
  window.sessionStorage.removeItem(GUEST_SECRET_KEY);
  window.dispatchEvent(new Event(CART_CHANGED_EVENT));
}

/**
 * خط سبد نمایشی. مبلغ از نقل‌قول Host است.
 */
export interface StorefrontCartLine {
  lineId: string;
  offerId: string;
  catalogVariantId: string;
  sellerPartyId: string;
  productId: string | null;
  productSlug: string | null;
  title: string;
  sellerDisplayName: string;
  mediaAssetId: string | null;
  quantity: number;
  unitAmountExclusiveOfTax: number | null;
  lineAmountExclusiveOfTax: number | null;
  currency: string;
  quotedTaxExclusive: boolean;
}

/**
 * صفحهٔ سبد زنده.
 */
export interface StorefrontCartPage {
  cartId: string;
  version: number;
  market: string;
  currency: string;
  channel: string;
  itemCount: number;
  subtotalExclusiveOfTax: number;
  lines: StorefrontCartLine[];
  guestSecret: string | null;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function asString(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function asBoolean(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

/**
 * JSON سبد Host را نگاشت می‌کند. فیلد price روی Product پذیرفته نمی‌شود.
 */
export function mapStorefrontCart(payload: unknown): StorefrontCartPage | null {
  const item = asRecord(payload);
  if (!item) {
    return null;
  }
  const cartId = asString(readProp(item, "cartId", "CartId"));
  if (!cartId) {
    return null;
  }
  const linesRaw = readProp(item, "lines", "Lines");
  const secretRaw = readProp(item, "guestSecret", "GuestSecret");
  return {
    cartId,
    version: asNumber(readProp(item, "version", "Version")),
    market: asString(readProp(item, "market", "Market"), "IR"),
    currency: asString(readProp(item, "currency", "Currency"), "IRR"),
    channel: asString(readProp(item, "channel", "Channel")),
    itemCount: asNumber(readProp(item, "itemCount", "ItemCount")),
    subtotalExclusiveOfTax: asNumber(readProp(item, "subtotalExclusiveOfTax", "SubtotalExclusiveOfTax")),
    guestSecret: secretRaw == null ? null : asString(secretRaw),
    lines: Array.isArray(linesRaw)
      ? linesRaw.map((row) => {
          const line = asRecord(row) ?? {};
          const productRaw = readProp(line, "productId", "ProductId");
          const slugRaw = readProp(line, "productSlug", "ProductSlug");
          const mediaRaw = readProp(line, "mediaAssetId", "MediaAssetId");
          const unitRaw = readProp(line, "unitAmountExclusiveOfTax", "UnitAmountExclusiveOfTax");
          const lineRaw = readProp(line, "lineAmountExclusiveOfTax", "LineAmountExclusiveOfTax");
          return {
            lineId: asString(readProp(line, "lineId", "LineId")),
            offerId: asString(readProp(line, "offerId", "OfferId")),
            catalogVariantId: asString(readProp(line, "catalogVariantId", "CatalogVariantId")),
            sellerPartyId: asString(readProp(line, "sellerPartyId", "SellerPartyId")),
            productId: productRaw == null ? null : asString(productRaw),
            productSlug: slugRaw == null ? null : asString(slugRaw),
            title: asString(readProp(line, "title", "Title"), "کالا"),
            sellerDisplayName: asString(readProp(line, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
            mediaAssetId: mediaRaw == null ? null : asString(mediaRaw),
            quantity: asNumber(readProp(line, "quantity", "Quantity"), 1),
            unitAmountExclusiveOfTax: unitRaw == null ? null : asNumber(unitRaw),
            lineAmountExclusiveOfTax: lineRaw == null ? null : asNumber(lineRaw),
            currency: asString(readProp(line, "currency", "Currency"), "IRR"),
            quotedTaxExclusive: asBoolean(readProp(line, "quotedTaxExclusive", "QuotedTaxExclusive"), true),
          } satisfies StorefrontCartLine;
        })
      : [],
  };
}

export class StorefrontCartApiError extends Error {
  readonly status: number;
  readonly errorCode: string | null;
  readonly detail: string | null;

  constructor(status: number, errorCode: string | null, detail: string | null) {
    super(detail ?? errorCode ?? "خطای سبد");
    this.status = status;
    this.errorCode = errorCode;
    this.detail = detail;
  }
}

const TECHNICAL_CART_ERROR = /Held|reservation|رزرو|آزادسازی/i;

/**
 * پیام قابل‌نمایش مشتری. واژگان فنی رزرو موجودی را پنهان می‌کند.
 */
export function toCustomerCartMessage(error: unknown): string {
  if (error instanceof StorefrontCartApiError) {
    if (error.detail && !TECHNICAL_CART_ERROR.test(error.detail)) {
      return error.detail;
    }
    switch (error.errorCode) {
      case "cart.inventory.insufficient":
        return "تعداد انتخاب‌شده بیشتر از موجودی قابل فروش است.";
      case "cart.inventory.stale":
        return "موجودی این کالا تغییر کرده است. لطفاً تعداد را دوباره بررسی کنید.";
      case "cart.quantity.invalid":
        return "تعداد انتخاب‌شده معتبر نیست.";
      case "cart.offer.unavailable":
        return "این کالا در حال حاضر قابل افزودن به سبد نیست.";
      default:
        return "عملیات سبد انجام نشد. لطفاً دوباره تلاش کنید.";
    }
  }
  if (error instanceof Error) {
    return TECHNICAL_CART_ERROR.test(error.message)
      ? "موجودی این کالا تغییر کرده است. لطفاً تعداد را دوباره بررسی کنید."
      : error.message;
  }
  return "عملیات سبد شکست خورد.";
}

async function parseCartResponse(response: Response): Promise<StorefrontCartPage> {
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const record = asRecord(payload);
    throw new StorefrontCartApiError(
      response.status,
      record ? asString(readProp(record, "errorCode", "ErrorCode")) || null : null,
      record ? asString(readProp(record, "detail", "Detail")) || null : null,
    );
  }
  const cart = mapStorefrontCart(payload);
  if (!cart) {
    throw new StorefrontCartApiError(response.status, "cart.invalid", "پاسخ سبد نامعتبر است.");
  }
  if (cart.guestSecret) {
    writeCartSession(cart.cartId, cart.guestSecret);
  } else {
    writeCartSession(cart.cartId, readCartSession().guestSecret);
  }
  return cart;
}

function cartHeaders(version?: number): HeadersInit {
  const headers: Record<string, string> = { "content-type": "application/json" };
  const { guestSecret } = readCartSession();
  if (guestSecret) {
    headers["X-Tooba-Guest-Secret"] = guestSecret;
  }
  if (version != null) {
    headers["X-Tooba-Cart-Version"] = String(version);
  }
  return headers;
}

/**
 * سبد مهمان را می‌سازد اگر نشست نباشد.
 */
export async function ensureStorefrontCart(): Promise<StorefrontCartPage> {
  const session = readCartSession();
  if (session.cartId && session.guestSecret) {
    const existing = await fetch(`/v1/storefront/cart/${session.cartId}`, {
      cache: "no-store",
      headers: cartHeaders(),
    });
    if (existing.ok) {
      return parseCartResponse(existing);
    }
  }
  const created = await fetch("/v1/storefront/cart", { method: "POST", cache: "no-store" });
  return parseCartResponse(created);
}

/**
 * سبد جاری را می‌خواند. بدون نشست null است.
 */
export async function loadStorefrontCart(): Promise<StorefrontCartPage | null> {
  const session = readCartSession();
  if (!session.cartId || !session.guestSecret) {
    return null;
  }
  const response = await fetch(`/v1/storefront/cart/${session.cartId}`, {
    cache: "no-store",
    headers: cartHeaders(),
  });
  if (response.status === 404) {
    clearCartSession();
    return null;
  }
  return parseCartResponse(response);
}

/**
 * Offer انتخاب‌شده را با تعداد به سبد زنده اضافه می‌کند.
 */
export async function addOfferToCart(offerId: string, quantity: number): Promise<StorefrontCartPage> {
  const cart = await ensureStorefrontCart();
  const response = await fetch(`/v1/storefront/cart/${cart.cartId}/lines?expectedVersion=${cart.version}`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeaders(cart.version),
    body: JSON.stringify({ offerId, quantity }),
  });
  return parseCartResponse(response);
}

/**
 * تعداد خط را از پاسخ Host عوض می‌کند.
 */
export async function changeCartLineQuantity(lineId: string, quantity: number): Promise<StorefrontCartPage> {
  const cart = await ensureStorefrontCart();
  const response = await fetch(`/v1/storefront/cart/${cart.cartId}/lines/${lineId}?expectedVersion=${cart.version}`, {
    method: "PATCH",
    cache: "no-store",
    headers: cartHeaders(cart.version),
    body: JSON.stringify({ quantity }),
  });
  return parseCartResponse(response);
}

/**
 * خط را از سبد Host حذف می‌کند.
 */
export async function removeCartLine(lineId: string): Promise<StorefrontCartPage> {
  const cart = await ensureStorefrontCart();
  const response = await fetch(`/v1/storefront/cart/${cart.cartId}/lines/${lineId}?expectedVersion=${cart.version}`, {
    method: "DELETE",
    cache: "no-store",
    headers: cartHeaders(cart.version),
  });
  return parseCartResponse(response);
}
