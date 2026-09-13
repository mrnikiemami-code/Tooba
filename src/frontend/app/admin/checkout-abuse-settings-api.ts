import { adminHeaders } from "./admin-api";

export interface CheckoutAbuseSettingsView {
  maxOpenUnpaidOrdersPerCustomer: number;
  reservationCommitWindowMinutes: number;
  maxCheckoutCommitsPerCustomerInWindow: number;
  minOpenUnpaid: number;
  maxOpenUnpaid: number;
  minWindowMinutes: number;
  maxWindowMinutes: number;
  minCommits: number;
  maxCommits: number;
}

function asNumber(value: unknown, fallback: number): number {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function mapView(payload: unknown): CheckoutAbuseSettingsView | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const row = payload as Record<string, unknown>;
  return {
    maxOpenUnpaidOrdersPerCustomer: asNumber(row.maxOpenUnpaidOrdersPerCustomer, 2),
    reservationCommitWindowMinutes: asNumber(row.reservationCommitWindowMinutes, 30),
    maxCheckoutCommitsPerCustomerInWindow: asNumber(row.maxCheckoutCommitsPerCustomerInWindow, 3),
    minOpenUnpaid: asNumber(row.minOpenUnpaid, 1),
    maxOpenUnpaid: asNumber(row.maxOpenUnpaid, 20),
    minWindowMinutes: asNumber(row.minWindowMinutes, 1),
    maxWindowMinutes: asNumber(row.maxWindowMinutes, 10080),
    minCommits: asNumber(row.minCommits, 1),
    maxCommits: asNumber(row.maxCommits, 30),
  };
}

export async function loadCheckoutAbuseSettings(): Promise<
  { ok: true; data: CheckoutAbuseSettingsView } | { ok: false; denied?: boolean }
> {
  const response = await fetch("/v1/admin/settings/checkout-abuse", { headers: adminHeaders() });
  if (response.status === 401 || response.status === 403) {
    return { ok: false, denied: true };
  }
  if (!response.ok) {
    return { ok: false };
  }
  const data = mapView(await response.json().catch(() => null));
  return data ? { ok: true, data } : { ok: false };
}

export async function saveCheckoutAbuseSettings(body: {
  maxOpenUnpaidOrdersPerCustomer: number;
  reservationCommitWindowMinutes: number;
  maxCheckoutCommitsPerCustomerInWindow: number;
}): Promise<{ ok: true; data: CheckoutAbuseSettingsView } | { ok: false; denied?: boolean; message?: string }> {
  const response = await fetch("/v1/admin/settings/checkout-abuse", {
    method: "PUT",
    headers: { ...adminHeaders(), "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (response.status === 401 || response.status === 403) {
    return { ok: false, denied: true };
  }
  if (!response.ok) {
    const payload = await response.json().catch(() => null) as { errorCode?: string } | null;
    return { ok: false, message: payload?.errorCode === "settings.max_open_unpaid.invalid"
      || payload?.errorCode === "settings.reservation_commit_window.invalid"
      || payload?.errorCode === "settings.max_checkout_commits.invalid"
      || payload?.errorCode === "settings.checkout_abuse.invalid"
      ? "مقدار واردشده معتبر نیست."
      : "ذخیرهٔ محدودیت سفارش انجام نشد." };
  }
  const data = mapView(await response.json().catch(() => null));
  return data ? { ok: true, data } : { ok: false, message: "پاسخ تنظیم محدودیت نامعتبر است." };
}
