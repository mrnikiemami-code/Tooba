/**
 * Admin client for seller promotion/coupon supervision.
 */
import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../../../lib/admin/admin-result.ts";

function actorId(): string {
  if (typeof window === "undefined") return "";
  return window.localStorage.getItem("tooba.adminActorUserId") ?? "";
}

function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}

export interface AdminPromotionRow {
  id: string;
  promotionId: string;
  name: string;
  status: string;
  couponCode: string | null;
  discountKind: string;
  percentageRate: number;
  fixedAmount: number;
  sellerPartyId: string | null;
  effectiveTo: string | null;
}

function normalizeAdminPromotionStatus(value: unknown): string {
  if (value === 0 || value === "0") return "Draft";
  if (value === 1 || value === "1") return "Active";
  if (value === 2 || value === "2") return "Expired";
  return String(value ?? "Draft");
}

function normalizeAdminDiscountKind(value: unknown): string {
  if (value === 0 || value === "0") return "PercentageOff";
  if (value === 1 || value === "1") return "FixedAmountOff";
  return String(value ?? "PercentageOff");
}

export function mapAdminPromotions(value: unknown): AdminPromotionRow[] | null {
  if (!Array.isArray(value)) {
    return null;
  }
  return value
    .map((row) => {
      if (!row || typeof row !== "object") {
        return null;
      }
      const item = row as Record<string, unknown>;
      const promotionId = String(item.promotionId ?? item.PromotionId ?? "");
      if (!promotionId) {
        return null;
      }
      return {
        id: promotionId,
        promotionId,
        name: String(item.name ?? item.Name ?? ""),
        status: normalizeAdminPromotionStatus(item.status ?? item.Status),
        couponCode: item.couponCode == null && item.CouponCode == null
          ? null
          : String(item.couponCode ?? item.CouponCode),
        discountKind: normalizeAdminDiscountKind(item.discountKind ?? item.DiscountKind),
        percentageRate: Number(item.percentageRate ?? item.PercentageRate ?? 0),
        fixedAmount: Number(item.fixedAmount ?? item.FixedAmount ?? 0),
        sellerPartyId: item.sellerPartyId == null && item.SellerPartyId == null
          ? null
          : String(item.sellerPartyId ?? item.SellerPartyId),
        effectiveTo: item.effectiveTo == null && item.EffectiveTo == null
          ? null
          : String(item.effectiveTo ?? item.EffectiveTo),
      };
    })
    .filter((row): row is AdminPromotionRow => row != null);
}

/** فهرست نظارتی پروموشن‌ها. */
export async function loadAdminPromotions(sellerPartyId?: string): Promise<AdminResult<AdminPromotionRow[]>> {
  const query = sellerPartyId ? `?sellerPartyId=${encodeURIComponent(sellerPartyId)}` : "";
  try {
    const response = await fetch(`/v1/admin/promotions${query}`, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    const data = mapAdminPromotions(payload);
    if (data == null) {
      return { state: "error", data: null, status: response.status, message: "admin.invalid-response" };
    }
    return { state: "ok", data, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** غیرفعال‌سازی نظارتی پروموشن. */
export async function deactivateAdminPromotion(promotionId: string): Promise<AdminResult<null>> {
  try {
    const response = await fetch(`/v1/admin/promotions/${encodeURIComponent(promotionId)}/deactivate`, {
      method: "POST",
      headers: adminHeaders(),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    return { state: "ok", data: null, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
