import type { StorefrontProductCard } from "./storefront-model.ts";
import { bffFetchHeaders, ensureCsrfCookie } from "../../lib/auth/browser-session.ts";

export const WISHLIST_CHANGED_EVENT = "tooba-wishlist-changed";

/** یک قلم علاقه‌مندی با کارت ترکیب‌شده از حقیقت زندهٔ فروشگاه. */
export interface StorefrontWishlistItem {
  productId: string;
  savedAt: string;
  card: StorefrontProductCard;
}

/** صفحهٔ علاقه‌مندی مشتری که شمارش و کارت‌های آن از Host می‌آید. */
export interface StorefrontWishlistPage {
  items: StorefrontWishlistItem[];
  totalCount: number;
}

/** خطای قابل تشخیص API علاقه‌مندی، از جمله پایان نشست با وضعیت ۴۰۱. */
export class StorefrontWishlistApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? value as Record<string, unknown> : null;
}

function prop(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function number(value: unknown): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function nullableNumber(value: unknown): number | null {
  return value == null ? null : number(value);
}

function nullableText(value: unknown): string | null {
  return value == null || value === "" ? null : String(value);
}

function authHeaders(json = false): Record<string, string> {
  return bffFetchHeaders(json);
}

/** کارت ترکیب‌شدهٔ Wishlist را بدون ساخت قیمت، موجودی یا امتیاز نگاشت می‌کند. */
export function mapWishlistCard(value: unknown): StorefrontProductCard | null {
  const row = recordOf(value);
  if (!row) return null;
  const productId = text(prop(row, "productId", "ProductId"));
  const slug = text(prop(row, "slug", "Slug"));
  if (!productId || !slug) return null;
  const reviewCount = number(prop(row, "reviewCount", "ReviewCount"));
  const averageRating = nullableNumber(prop(row, "averageRating", "AverageRating"));
  return {
    productId,
    slug,
    title: text(prop(row, "title", "Title"), "کالا"),
    categoryName: text(prop(row, "categoryName", "CategoryName")),
    categoryId: nullableText(prop(row, "categoryId", "CategoryId")),
    mediaAssetId: nullableText(prop(row, "mediaAssetId", "MediaAssetId")),
    primaryOfferId: text(prop(row, "primaryOfferId", "PrimaryOfferId")),
    sellerPartyId: text(prop(row, "sellerPartyId", "SellerPartyId")),
    sellerDisplayName: text(prop(row, "sellerDisplayName", "SellerDisplayName"), "فروشنده"),
    offerAmountExclusiveOfTax: number(prop(row, "offerAmountExclusiveOfTax", "OfferAmountExclusiveOfTax")),
    promotionalAmountExclusiveOfTax: nullableNumber(prop(row, "promotionalAmountExclusiveOfTax", "PromotionalAmountExclusiveOfTax")),
    currency: text(prop(row, "currency", "Currency"), "IRR"),
    availableUnits: number(prop(row, "availableUnits", "AvailableUnits")),
    inStock: prop(row, "inStock", "InStock") === true,
    promotionLabel: nullableText(prop(row, "promotionLabel", "PromotionLabel")),
    averageRating: reviewCount > 0 ? averageRating : null,
    reviewCount,
  };
}

/** پاسخ صفحه یا آرایهٔ Wishlist را با savedAt و کارت زنده نگاشت می‌کند. */
export function mapWishlistPage(value: unknown): StorefrontWishlistPage | null {
  const page = recordOf(value);
  const rawItems = Array.isArray(value) ? value : page ? prop(page, "items", "Items") : null;
  if (!Array.isArray(rawItems)) return null;
  const items = rawItems.flatMap((value): StorefrontWishlistItem[] => {
    const row = recordOf(value);
    if (!row) return [];
    const nested = prop(row, "product", "Product") ?? prop(row, "card", "Card") ?? row;
    const card = mapWishlistCard(nested);
    if (!card) return [];
    return [{
      productId: card.productId,
      savedAt: text(prop(row, "savedAt", "SavedAt") ?? prop(row, "createdAt", "CreatedAt")),
      card,
    }];
  });
  return { items, totalCount: page ? number(prop(page, "totalCount", "TotalCount")) || items.length : items.length };
}

async function request(path: string, init?: RequestInit): Promise<unknown> {
  if (init?.method && init.method !== "GET") {
    await ensureCsrfCookie();
  }
  const response = await fetch(path, {
    cache: "no-store",
    credentials: "include",
    ...init,
    headers: authHeaders(Boolean(init?.body)),
  });
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    throw new StorefrontWishlistApiError(response.status, response.status === 401
      ? "برای مدیریت علاقه‌مندی‌ها وارد حساب خود شوید."
      : "عملیات علاقه‌مندی انجام نشد.");
  }
  return payload;
}

/** صفحهٔ علاقه‌مندی را با دادهٔ جاری قیمت و موجودی می‌خواند. */
export async function loadWishlist(): Promise<StorefrontWishlistPage> {
  const mapped = mapWishlistPage(await request("/api/customer/wishlist"));
  if (!mapped) throw new StorefrontWishlistApiError(200, "پاسخ علاقه‌مندی نامعتبر است.");
  return mapped;
}

/** عضویت چند ProductId را با یک درخواست batch می‌خواند. */
export async function loadWishlistMembership(productIds: string[]): Promise<Set<string>> {
  if (productIds.length === 0) return new Set();
  const payload = await request("/api/customer/wishlist/membership", {
    method: "POST",
    body: JSON.stringify({ productIds }),
  });
  const row = recordOf(payload);
  const raw = Array.isArray(payload) ? payload : row
    ? prop(row, "productIds", "ProductIds") ?? prop(row, "containedProductIds", "ContainedProductIds") ?? prop(row, "items", "Items")
    : [];
  if (!Array.isArray(raw)) return new Set();
  return new Set(raw.flatMap((item): string[] => {
    if (typeof item === "string") return [item];
    const record = recordOf(item);
    if (!record || prop(record, "contains", "Contains") === false) return [];
    const id = text(prop(record, "productId", "ProductId"));
    return id ? [id] : [];
  }));
}

/** Product را پس از موفقیت Host به علاقه‌مندی اضافه می‌کند. */
export async function addWishlistProduct(productId: string): Promise<void> {
  await request(`/api/customer/wishlist/${encodeURIComponent(productId)}`, { method: "POST" });
  window.dispatchEvent(new Event(WISHLIST_CHANGED_EVENT));
}

/** Product را پس از موفقیت Host از علاقه‌مندی حذف می‌کند. */
export async function removeWishlistProduct(productId: string): Promise<void> {
  await request(`/api/customer/wishlist/${encodeURIComponent(productId)}`, { method: "DELETE" });
  window.dispatchEvent(new Event(WISHLIST_CHANGED_EVENT));
}

/** پیام امن و فارسی خطای mutation علاقه‌مندی را برمی‌گرداند. */
export function wishlistErrorMessage(error: unknown): string {
  return error instanceof StorefrontWishlistApiError ? error.message : "ارتباط با علاقه‌مندی‌ها برقرار نشد.";
}

/** متن حالت خالی را مستقل از UI برای آزمون و نمایش یکسان برمی‌گرداند. */
export function wishlistEmptyMessage(count: number): string | null {
  return count === 0 ? "هنوز محصولی به علاقه‌مندی‌ها اضافه نکرده‌اید." : null;
}
