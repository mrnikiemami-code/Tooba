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
 * اعلام تغییر محتوای سبد برای UI (نشان هدر / مینی‌سبد / صفحهٔ سبد).
 * فقط بعد از جهش واقعی فراخوانی شود — نه بعد از GET معمولی؛ وگرنه حلقهٔ رفرش ساخته می‌شود.
 */
export function notifyCartChanged(): void {
  if (typeof window === "undefined") {
    return;
  }
  window.dispatchEvent(new Event(CART_CHANGED_EVENT));
}

/**
 * پس از ساخت سبد، فقط شناسه و راز را ذخیره می‌کند.
 * به‌تنهایی رویداد UI شلیک نمی‌کند.
 */
export function writeCartSession(cartId: string, guestSecret: string | null | undefined): void {
  window.sessionStorage.setItem(CART_ID_KEY, cartId);
  if (guestSecret) {
    window.sessionStorage.setItem(GUEST_SECRET_KEY, guestSecret);
  }
}

/**
 * برای User-Preview: اگر URL شامل cartId/guestSecret باشد نشست را یک‌بار seed می‌کند.
 * فقط Development/preview؛ حقیقت مبلغ همچنان از Host است.
 */
export function bootstrapCartSessionFromQuery(params: {
  cartId?: string | null;
  guestSecret?: string | null;
}): boolean {
  if (typeof window === "undefined") {
    return false;
  }
  const cartId = params.cartId?.trim();
  const guestSecret = params.guestSecret?.trim();
  if (!cartId || !guestSecret) {
    return false;
  }
  writeCartSession(cartId, guestSecret);
  notifyCartChanged();
  return true;
}

/**
 * فقط اشاره‌گر سبد فعال را پاک می‌کند. اثبات سفارش/پرداخت متعهد باقی می‌ماند.
 */
export function clearCartSession(): void {
  if (typeof window === "undefined") {
    return;
  }
  window.sessionStorage.removeItem(CART_ID_KEY);
  window.sessionStorage.removeItem(GUEST_SECRET_KEY);
  notifyCartChanged();
}

const PAYMENT_RESULT_PROOF_KEY = "tooba.storefront.paymentResultProof";
const COMMITTED_CHECKOUT_PROOFS_KEY = "tooba.storefront.committedCheckoutProofs";
const COMMITTED_PROOF_STORE_KEY = "storefront";

export type StorefrontPaymentResultProof = {
  paymentId: string;
  checkoutId: string;
  cartId: string;
  guestSecret: string;
};

export type StorefrontCommittedCheckoutProof = {
  checkoutId: string;
  cartId: string;
  guestSecret: string;
  storeKey: string;
};

export type StorefrontCommittedAccess = {
  cartId: string | null;
  guestSecret: string | null;
};

/**
 * قبل از detach سبد فعال، اثبات باریک نتیجهٔ پرداخت را نگه می‌دارد.
 */
export function writePaymentResultProof(proof: StorefrontPaymentResultProof): void {
  if (typeof window === "undefined") {
    return;
  }
  const paymentId = proof.paymentId?.trim();
  const checkoutId = proof.checkoutId?.trim();
  const cartId = proof.cartId?.trim();
  const guestSecret = proof.guestSecret?.trim() ?? "";
  if (!paymentId || !checkoutId || !cartId) {
    return;
  }
  window.sessionStorage.setItem(
    PAYMENT_RESULT_PROOF_KEY,
    JSON.stringify({ paymentId, checkoutId, cartId, guestSecret }),
  );
}

/**
 * اثبات نتیجهٔ پرداخت برای همان paymentId (یا آخرین ذخیره).
 */
export function readPaymentResultProof(paymentId?: string | null): StorefrontPaymentResultProof | null {
  if (typeof window === "undefined") {
    return null;
  }
  const raw = window.sessionStorage.getItem(PAYMENT_RESULT_PROOF_KEY);
  if (!raw) {
    return null;
  }
  try {
    const parsed = JSON.parse(raw) as StorefrontPaymentResultProof;
    if (!parsed?.paymentId || !parsed.cartId) {
      return null;
    }
    if (paymentId && parsed.paymentId !== paymentId) {
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

function readCommittedCheckoutProofMap(): Record<string, StorefrontCommittedCheckoutProof> {
  if (typeof window === "undefined") {
    return {};
  }
  const raw = window.sessionStorage.getItem(COMMITTED_CHECKOUT_PROOFS_KEY);
  if (!raw) {
    return {};
  }
  try {
    const parsed = JSON.parse(raw) as Record<string, StorefrontCommittedCheckoutProof>;
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

/**
 * اثبات سفارش متعهد را به‌ازای checkoutId می‌نویسد و کلیدهای دیگر را بازنویسی نمی‌کند.
 */
export function writeCommittedCheckoutProof(proof: StorefrontCommittedCheckoutProof): void {
  if (typeof window === "undefined") {
    return;
  }
  const checkoutId = proof.checkoutId?.trim();
  const cartId = proof.cartId?.trim();
  const guestSecret = proof.guestSecret?.trim() ?? "";
  const storeKey = (proof.storeKey?.trim() || COMMITTED_PROOF_STORE_KEY);
  if (!checkoutId || !cartId) {
    return;
  }
  const next = readCommittedCheckoutProofMap();
  next[checkoutId] = { checkoutId, cartId, guestSecret, storeKey };
  window.sessionStorage.setItem(COMMITTED_CHECKOUT_PROOFS_KEY, JSON.stringify(next));
}

export function readCommittedCheckoutProof(checkoutId: string): StorefrontCommittedCheckoutProof | null {
  const id = checkoutId?.trim();
  if (!id) {
    return null;
  }
  const proof = readCommittedCheckoutProofMap()[id];
  if (!proof?.checkoutId || !proof.cartId) {
    return null;
  }
  if (proof.storeKey && proof.storeKey !== COMMITTED_PROOF_STORE_KEY) {
    return null;
  }
  return proof;
}

/**
 * مالکیت نتیجهٔ پرداخت. به سبد فعال جدید برنمی‌گردد.
 */
export function resolvePaymentResultAccess(paymentId?: string | null): StorefrontCommittedAccess {
  const proof = readPaymentResultProof(paymentId);
  if (proof) {
    return { cartId: proof.cartId, guestSecret: proof.guestSecret };
  }
  return { cartId: null, guestSecret: null };
}

/**
 * مالکیت پرداخت/checkout پس از commit. سبد فعال منبع اختیار نیست.
 */
export function resolveCommittedCheckoutAccess(
  checkoutId: string,
  paymentId?: string | null,
): StorefrontCommittedAccess {
  if (paymentId) {
    const paymentProof = readPaymentResultProof(paymentId);
    if (paymentProof && paymentProof.checkoutId === checkoutId) {
      return { cartId: paymentProof.cartId, guestSecret: paymentProof.guestSecret };
    }
  }
  const committed = readCommittedCheckoutProof(checkoutId);
  if (committed) {
    return { cartId: committed.cartId, guestSecret: committed.guestSecret };
  }
  return { cartId: null, guestSecret: null };
}

/**
 * پس از commit موفق: اثبات سفارش را می‌نویسد و اشاره‌گر سبد فعال را جدا می‌کند.
 */
export function persistCommittedCheckoutAndDetachActiveCart(checkoutId: string, sourceCartId: string): void {
  const session = readCartSession();
  writeCommittedCheckoutProof({
    checkoutId,
    cartId: sourceCartId,
    guestSecret: session.guestSecret ?? "",
    storeKey: COMMITTED_PROOF_STORE_KEY,
  });
  clearCartSession();
}

export function persistPaymentResultProofFromAccess(
  mapped: { paymentId: string; checkoutId: string },
  access: StorefrontCommittedAccess,
): void {
  if (!access.cartId) {
    return;
  }
  writePaymentResultProof({
    paymentId: mapped.paymentId,
    checkoutId: mapped.checkoutId,
    cartId: access.cartId,
    guestSecret: access.guestSecret ?? "",
  });
}

export function isActiveShoppingCartStatus(status: string | null | undefined): boolean {
  const normalized = (status ?? "Active").trim().toLowerCase();
  return normalized === "active";
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
  unitCode: string | null;
  unitDisplayName: string | null;
  quantityDecimalPlaces: number;
  quantityStep: number | null;
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
  status: string;
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
    status: asString(readProp(item, "status", "Status"), "Active"),
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
            unitCode: (() => {
              const raw = readProp(line, "unitCode", "UnitCode");
              const text = asString(raw);
              return text.length > 0 ? text : null;
            })(),
            unitDisplayName: (() => {
              const raw = readProp(line, "unitDisplayName", "UnitDisplayName");
              const text = asString(raw);
              return text.length > 0 ? text : null;
            })(),
            quantityDecimalPlaces: asNumber(readProp(line, "quantityDecimalPlaces", "QuantityDecimalPlaces")),
            quantityStep: (() => {
              const raw = readProp(line, "quantityStep", "QuantityStep");
              return raw == null || raw === "" ? null : asNumber(raw);
            })(),
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
  persistActiveCartSession(cart);
  return cart;
}

function persistActiveCartSession(cart: StorefrontCartPage): void {
  if (!isActiveShoppingCartStatus(cart.status)) {
    return;
  }
  if (cart.guestSecret) {
    writeCartSession(cart.cartId, cart.guestSecret);
  } else {
    writeCartSession(cart.cartId, readCartSession().guestSecret);
  }
}

export function cartHeaders(version?: number): HeadersInit {
  return cartHeadersFromAccess(readCartSession(), version);
}

export function cartHeadersFromAccess(access: StorefrontCommittedAccess, version?: number): HeadersInit {
  const headers: Record<string, string> = { "content-type": "application/json" };
  if (access.guestSecret) {
    headers["X-Tooba-Guest-Secret"] = access.guestSecret;
  }
  if (version != null) {
    headers["X-Tooba-Cart-Version"] = String(version);
  }
  return headers;
}

async function createFreshActiveCart(): Promise<StorefrontCartPage> {
  const created = await fetch("/v1/storefront/cart", { method: "POST", cache: "no-store" });
  return parseCartResponse(created);
}

function shouldRotateActiveCart(status: number): boolean {
  return status === 401 || status === 403 || status === 404;
}

/**
 * فقط سبد Active را برای خرید برمی‌گرداند. Converted/غیرقابل‌دسترسی rotate می‌شود.
 */
export async function ensureStorefrontCart(): Promise<StorefrontCartPage> {
  const session = readCartSession();
  if (session.cartId && session.guestSecret) {
    const existing = await fetch(`/v1/storefront/cart/${session.cartId}`, {
      cache: "no-store",
      headers: cartHeaders(),
    });
    if (existing.ok) {
      const payload: unknown = await existing.json().catch(() => null);
      const cart = mapStorefrontCart(payload);
      if (cart && isActiveShoppingCartStatus(cart.status)) {
        persistActiveCartSession(cart);
        return cart;
      }
      clearCartSession();
    } else if (shouldRotateActiveCart(existing.status)) {
      clearCartSession();
    } else {
      return parseCartResponse(existing);
    }
  }
  return createFreshActiveCart();
}

/**
 * سبد جاری را می‌خواند. بدون نشست یا سبد غیر Active مقدار null است.
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
  if (shouldRotateActiveCart(response.status)) {
    clearCartSession();
    return null;
  }
  if (!response.ok) {
    return parseCartResponse(response);
  }
  const payload: unknown = await response.json().catch(() => null);
  const cart = mapStorefrontCart(payload);
  if (!cart || !isActiveShoppingCartStatus(cart.status)) {
    clearCartSession();
    return null;
  }
  persistActiveCartSession(cart);
  return cart;
}

/**
 * Offer انتخاب‌شده را با تعداد به سبد زنده اضافه می‌کند.
 * سبد Converted اینجا نگه داشته نمی‌شود؛ ensure فقط Active می‌سازد.
 */
export async function addOfferToCart(offerId: string, quantity: number): Promise<StorefrontCartPage> {
  const cart = await ensureStorefrontCart();
  const response = await fetch(`/v1/storefront/cart/${cart.cartId}/lines?expectedVersion=${cart.version}`, {
    method: "POST",
    cache: "no-store",
    headers: cartHeaders(cart.version),
    body: JSON.stringify({ offerId, quantity }),
  });
  const next = await parseCartResponse(response);
  notifyCartChanged();
  return next;
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
  const next = await parseCartResponse(response);
  notifyCartChanged();
  return next;
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
  const next = await parseCartResponse(response);
  notifyCartChanged();
  return next;
}
