import { adminHeaders } from "./admin-api";

export type QuantityRoundingMode = "Floor" | "Ceiling" | "Nearest";

export interface StoreQuantitySettingsView {
  globalRoundingMode: QuantityRoundingMode;
  labelFa: string;
  labelEn: string;
}

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function mapSettings(payload: unknown): StoreQuantitySettingsView | null {
  if (!payload || typeof payload !== "object") {
    return null;
  }
  const item = payload as Record<string, unknown>;
  const mode = String(readProp(item, "globalRoundingMode", "GlobalRoundingMode") ?? "");
  if (mode !== "Floor" && mode !== "Ceiling" && mode !== "Nearest") {
    return null;
  }
  return {
    globalRoundingMode: mode,
    labelFa: String(readProp(item, "labelFa", "LabelFa") ?? ""),
    labelEn: String(readProp(item, "labelEn", "LabelEn") ?? ""),
  };
}

export async function loadStoreQuantitySettings(): Promise<
  { ok: true; data: StoreQuantitySettingsView } | { ok: false; denied?: boolean }
> {
  try {
    const response = await fetch("/v1/admin/settings/quantity-rounding", { headers: adminHeaders() });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, denied: true };
    }
    if (!response.ok) {
      return { ok: false };
    }
    const data = mapSettings(await response.json());
    return data ? { ok: true, data } : { ok: false };
  } catch {
    return { ok: false };
  }
}

export async function saveStoreQuantitySettings(
  globalRoundingMode: QuantityRoundingMode,
): Promise<{ ok: true; data: StoreQuantitySettingsView } | { ok: false; denied?: boolean }> {
  try {
    const response = await fetch("/v1/admin/settings/quantity-rounding", {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify({ globalRoundingMode }),
    });
    if (response.status === 401 || response.status === 403) {
      return { ok: false, denied: true };
    }
    if (!response.ok) {
      return { ok: false };
    }
    const data = mapSettings(await response.json());
    return data ? { ok: true, data } : { ok: false };
  } catch {
    return { ok: false };
  }
}
