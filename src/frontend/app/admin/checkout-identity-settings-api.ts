import { adminHeaders } from "./admin-api";

export interface CheckoutIdentitySettingsView {
  policy: "AuthenticatedOnly" | "GuestAllowed";
  labelFa: string;
  labelEn: string;
  warningFa: string | null;
}

function mapView(payload: unknown): CheckoutIdentitySettingsView | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const row = payload as Record<string, unknown>;
  const policy = row.policy === "GuestAllowed" ? "GuestAllowed" : "AuthenticatedOnly";
  return {
    policy,
    labelFa: String(row.labelFa ?? ""),
    labelEn: String(row.labelEn ?? ""),
    warningFa: row.warningFa == null ? null : String(row.warningFa),
  };
}

export async function loadCheckoutIdentitySettings(): Promise<
  { ok: true; data: CheckoutIdentitySettingsView } | { ok: false; denied?: boolean }
> {
  const response = await fetch("/v1/admin/settings/checkout-identity", { headers: adminHeaders() });
  if (response.status === 401 || response.status === 403) {
    return { ok: false, denied: true };
  }
  if (!response.ok) {
    return { ok: false };
  }
  const data = mapView(await response.json().catch(() => null));
  return data ? { ok: true, data } : { ok: false };
}

export async function saveCheckoutIdentitySettings(
  policy: "AuthenticatedOnly" | "GuestAllowed",
): Promise<{ ok: true; data: CheckoutIdentitySettingsView } | { ok: false; denied?: boolean; message?: string }> {
  const response = await fetch("/v1/admin/settings/checkout-identity", {
    method: "PUT",
    headers: { ...adminHeaders(), "Content-Type": "application/json" },
    body: JSON.stringify({ policy }),
  });
  if (response.status === 401 || response.status === 403) {
    return { ok: false, denied: true };
  }
  if (!response.ok) {
    return { ok: false, message: "ذخیرهٔ هویت خرید انجام نشد." };
  }
  const data = mapView(await response.json().catch(() => null));
  return data ? { ok: true, data } : { ok: false, message: "پاسخ تنظیم هویت نامعتبر است." };
}
