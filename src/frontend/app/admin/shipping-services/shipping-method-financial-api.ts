/**
 * TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155 — admin client for the SERVER-authoritative financial
 * status of one canonical shipping method.
 *
 * The identity passed here is the authoritative `(serviceCode, optionCode)` pair that the server's
 * one-level binding resolver already returned; this module never splits a method code to build it.
 * Authorization uses the existing admin transport and 401/403 fail closed.
 */
import { adminHeaders, type AdminResult } from "../admin-api.ts";
import {
  mapShippingMethodFinancialStatus,
  type ShippingMethodFinancialStatus,
} from "./shipping-method-financial-model.ts";

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function errorCode(payload: unknown, fallback: string): string {
  const row = asRecord(payload);
  return String(row?.errorCode ?? row?.ErrorCode ?? row?.title ?? row?.Title ?? "") || fallback;
}

/** Path of the read-only financial-status seam. `optionCode` is omitted when the method is optionless. */
export function shippingMethodFinancialStatusPath(serviceCode: string, optionCode: string | null): string {
  const base = `/v1/admin/shipping-services/${encodeURIComponent(serviceCode)}/financial-status`;
  const option = optionCode?.trim();
  return option ? `${base}?optionCode=${encodeURIComponent(option)}` : base;
}

/** Reads the proven financial status of one method identity. Never invents a ready state. */
export async function loadShippingMethodFinancialStatus(
  serviceCode: string,
  optionCode: string | null,
): Promise<AdminResult<ShippingMethodFinancialStatus>> {
  try {
    const response = await fetch(shippingMethodFinancialStatusPath(serviceCode, optionCode), {
      headers: adminHeaders(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (response.status === 404) {
      // 404 با بدنهٔ تهی یعنی مسیر روی این نسخهٔ Host نگاشت نشده است، نه «این روش نرخ ندارد».
      // پیام باید به اقدام درست (به‌روزرسانی/راه‌اندازی مجدد سرور) راهنمایی کند.
      return {
        state: "error",
        data: null,
        status: response.status,
        message: "shipping_method_financial.route_unavailable",
      };
    }
    if (!response.ok) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: errorCode(payload, "shipping_method_financial.load_failed"),
      };
    }
    const mapped = mapShippingMethodFinancialStatus(payload);
    if (!mapped) {
      return {
        state: "error",
        data: null,
        status: response.status,
        message: "shipping_method_financial.invalid_response",
      };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
