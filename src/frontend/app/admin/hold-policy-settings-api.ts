import { adminHeaders } from "./admin-api";

export interface HoldPolicyDurationView {
  hours: number | null;
  effectiveHours: number;
  source: string;
  labelFa: string;
  labelEn: string;
  helperFa: string;
  helperEn: string;
}

export interface PaymentMethodHoldView {
  providerCode: string;
  labelFa: string;
  labelEn: string;
  onlinePaymentHoldHours: number | null;
  manualPaymentInitialHoldHours: number | null;
  manualPaymentReviewHoldHours: number | null;
}

export interface HoldPolicySettingsView {
  cartPersistence: HoldPolicyDurationView;
  onlinePaymentHold: HoldPolicyDurationView;
  manualInitialHold: HoldPolicyDurationView;
  manualReviewHold: HoldPolicyDurationView;
  methods: PaymentMethodHoldView[];
}

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function numberOrNull(value: unknown): number | null {
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function mapDuration(value: unknown): HoldPolicyDurationView | null {
  if (!value || typeof value !== "object") return null;
  const item = value as Record<string, unknown>;
  return {
    hours: numberOrNull(readProp(item, "hours", "Hours")),
    effectiveHours: Number(readProp(item, "effectiveHours", "EffectiveHours") ?? 0),
    source: String(readProp(item, "source", "Source") ?? "platform"),
    labelFa: String(readProp(item, "labelFa", "LabelFa") ?? ""),
    labelEn: String(readProp(item, "labelEn", "LabelEn") ?? ""),
    helperFa: String(readProp(item, "helperFa", "HelperFa") ?? ""),
    helperEn: String(readProp(item, "helperEn", "HelperEn") ?? ""),
  };
}

function mapMethod(value: unknown): PaymentMethodHoldView | null {
  if (!value || typeof value !== "object") return null;
  const item = value as Record<string, unknown>;
  const providerCode = String(readProp(item, "providerCode", "ProviderCode") ?? "");
  if (!providerCode) return null;
  return {
    providerCode,
    labelFa: String(readProp(item, "labelFa", "LabelFa") ?? providerCode),
    labelEn: String(readProp(item, "labelEn", "LabelEn") ?? providerCode),
    onlinePaymentHoldHours: numberOrNull(readProp(item, "onlinePaymentHoldHours", "OnlinePaymentHoldHours")),
    manualPaymentInitialHoldHours: numberOrNull(readProp(item, "manualPaymentInitialHoldHours", "ManualPaymentInitialHoldHours")),
    manualPaymentReviewHoldHours: numberOrNull(readProp(item, "manualPaymentReviewHoldHours", "ManualPaymentReviewHoldHours")),
  };
}

function mapSettings(payload: unknown): HoldPolicySettingsView | null {
  if (!payload || typeof payload !== "object") return null;
  const item = payload as Record<string, unknown>;
  const cart = mapDuration(readProp(item, "cartPersistence", "CartPersistence"));
  const online = mapDuration(readProp(item, "onlinePaymentHold", "OnlinePaymentHold"));
  const manual = mapDuration(readProp(item, "manualInitialHold", "ManualInitialHold"));
  const review = mapDuration(readProp(item, "manualReviewHold", "ManualReviewHold"));
  if (!cart || !online || !manual || !review) return null;
  const methodsRaw = readProp(item, "methods", "Methods");
  return {
    cartPersistence: cart,
    onlinePaymentHold: online,
    manualInitialHold: manual,
    manualReviewHold: review,
    methods: Array.isArray(methodsRaw)
      ? methodsRaw.map(mapMethod).filter((row): row is PaymentMethodHoldView => row !== null)
      : [],
  };
}

export async function loadHoldPolicySettings(): Promise<
  { ok: true; data: HoldPolicySettingsView } | { ok: false; denied?: boolean }
> {
  try {
    const response = await fetch("/v1/admin/settings/hold-policy", { headers: adminHeaders() });
    if (response.status === 401 || response.status === 403) return { ok: false, denied: true };
    if (!response.ok) return { ok: false };
    const data = mapSettings(await response.json());
    return data ? { ok: true, data } : { ok: false };
  } catch {
    return { ok: false };
  }
}

export async function saveHoldPolicySettings(body: {
  cartPersistenceHours: number | null;
  onlinePaymentHoldHours: number | null;
  manualPaymentInitialHoldHours: number | null;
  manualPaymentReviewHoldHours: number | null;
  methods: PaymentMethodHoldView[];
}): Promise<{ ok: true; data: HoldPolicySettingsView } | { ok: false; denied?: boolean; message?: string }> {
  try {
    const response = await fetch("/v1/admin/settings/hold-policy", {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify(body),
    });
    if (response.status === 401 || response.status === 403) return { ok: false, denied: true };
    if (!response.ok) {
      const payload = await response.json().catch(() => null) as { title?: string } | null;
      return { ok: false, message: payload?.title };
    }
    const data = mapSettings(await response.json());
    return data ? { ok: true, data } : { ok: false };
  } catch {
    return { ok: false };
  }
}
