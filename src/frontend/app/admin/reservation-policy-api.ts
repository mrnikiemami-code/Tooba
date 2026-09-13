import { adminHeaders } from "./admin-api";

export interface ReservationPolicyFieldView {
  overrideValue: number | null;
  effectiveValue: number;
  source: string;
  overridden: boolean;
  sourceLabelFa: string;
  sourceLabelEn: string;
  labelFa: string;
  labelEn: string;
  helperFa: string;
  helperEn: string;
}

export interface ReservationPolicyEditorView {
  scope: string;
  scopeId: string | null;
  initialHold: ReservationPolicyFieldView;
  retryHold: ReservationPolicyFieldView;
  maxCycles: ReservationPolicyFieldView;
  canEdit: boolean;
  sellerCanMutate: boolean;
  flashSaleStricter: boolean;
  longHoldWarning: boolean;
  inheritLabelFa: string;
  inheritLabelEn: string;
  multiLineHelpFa: string;
  multiLineHelpEn: string;
  flashSaleHelpFa: string;
  flashSaleHelpEn: string;
  stricterNoteFa: string;
  stricterNoteEn: string;
  longHoldNoteFa: string;
  longHoldNoteEn: string;
}

function readProp(record: Record<string, unknown>, camel: string, pascal: string): unknown {
  return record[camel] ?? record[pascal];
}

function numberOrNull(value: unknown): number | null {
  if (value == null || value === "") return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function mapField(value: unknown): ReservationPolicyFieldView | null {
  if (!value || typeof value !== "object") return null;
  const item = value as Record<string, unknown>;
  return {
    overrideValue: numberOrNull(readProp(item, "overrideValue", "OverrideValue")),
    effectiveValue: Number(readProp(item, "effectiveValue", "EffectiveValue") ?? 0),
    source: String(readProp(item, "source", "Source") ?? "platform"),
    overridden: Boolean(readProp(item, "overridden", "Overridden")),
    sourceLabelFa: String(readProp(item, "sourceLabelFa", "SourceLabelFa") ?? ""),
    sourceLabelEn: String(readProp(item, "sourceLabelEn", "SourceLabelEn") ?? ""),
    labelFa: String(readProp(item, "labelFa", "LabelFa") ?? ""),
    labelEn: String(readProp(item, "labelEn", "LabelEn") ?? ""),
    helperFa: String(readProp(item, "helperFa", "HelperFa") ?? ""),
    helperEn: String(readProp(item, "helperEn", "HelperEn") ?? ""),
  };
}

export function mapReservationPolicy(payload: unknown): ReservationPolicyEditorView | null {
  if (!payload || typeof payload !== "object") return null;
  const item = payload as Record<string, unknown>;
  const initial = mapField(readProp(item, "initialHold", "InitialHold"));
  const retry = mapField(readProp(item, "retryHold", "RetryHold"));
  const max = mapField(readProp(item, "maxCycles", "MaxCycles"));
  if (!initial || !retry || !max) return null;
  const scopeId = readProp(item, "scopeId", "ScopeId");
  return {
    scope: String(readProp(item, "scope", "Scope") ?? ""),
    scopeId: scopeId == null || scopeId === "" ? null : String(scopeId),
    initialHold: initial,
    retryHold: retry,
    maxCycles: max,
    canEdit: Boolean(readProp(item, "canEdit", "CanEdit")),
    sellerCanMutate: Boolean(readProp(item, "sellerCanMutate", "SellerCanMutate")),
    flashSaleStricter: Boolean(readProp(item, "flashSaleStricter", "FlashSaleStricter")),
    longHoldWarning: Boolean(readProp(item, "longHoldWarning", "LongHoldWarning")),
    inheritLabelFa: String(readProp(item, "inheritLabelFa", "InheritLabelFa") ?? ""),
    inheritLabelEn: String(readProp(item, "inheritLabelEn", "InheritLabelEn") ?? ""),
    multiLineHelpFa: String(readProp(item, "multiLineHelpFa", "MultiLineHelpFa") ?? ""),
    multiLineHelpEn: String(readProp(item, "multiLineHelpEn", "MultiLineHelpEn") ?? ""),
    flashSaleHelpFa: String(readProp(item, "flashSaleHelpFa", "FlashSaleHelpFa") ?? ""),
    flashSaleHelpEn: String(readProp(item, "flashSaleHelpEn", "FlashSaleHelpEn") ?? ""),
    stricterNoteFa: String(readProp(item, "stricterNoteFa", "StricterNoteFa") ?? ""),
    stricterNoteEn: String(readProp(item, "stricterNoteEn", "StricterNoteEn") ?? ""),
    longHoldNoteFa: String(readProp(item, "longHoldNoteFa", "LongHoldNoteFa") ?? ""),
    longHoldNoteEn: String(readProp(item, "longHoldNoteEn", "LongHoldNoteEn") ?? ""),
  };
}

export function parseReservationInteger(raw: string): { ok: true; value: number | null } | { ok: false } {
  const trimmed = raw.trim();
  if (!trimmed) return { ok: true, value: null };
  if (!/^-?\d+$/.test(trimmed)) return { ok: false };
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? { ok: true, value: parsed } : { ok: false };
}

async function readError(response: Response): Promise<string | undefined> {
  const payload = await response.json().catch(() => null) as { title?: string } | null;
  return payload?.title;
}

export async function loadReservationPolicy(
  path: string,
  headers?: HeadersInit,
): Promise<{ ok: true; data: ReservationPolicyEditorView } | { ok: false; denied?: boolean; message?: string }> {
  try {
    const response = await fetch(path, { headers: headers ?? adminHeaders() });
    if (response.status === 401 || response.status === 403) return { ok: false, denied: true };
    if (!response.ok) return { ok: false, message: await readError(response) };
    const data = mapReservationPolicy(await response.json());
    return data ? { ok: true, data } : { ok: false };
  } catch {
    return { ok: false };
  }
}

export async function saveReservationPolicy(
  path: string,
  body: {
    initialReservationHoldMinutes: number | null;
    retryReservationHoldMinutes: number | null;
    maxReservationCycles: number | null;
  },
): Promise<{ ok: true; data: ReservationPolicyEditorView } | { ok: false; denied?: boolean; message?: string }> {
  try {
    const response = await fetch(path, {
      method: "PUT",
      headers: adminHeaders({ "Content-Type": "application/json" }),
      body: JSON.stringify(body),
    });
    if (response.status === 401 || response.status === 403) return { ok: false, denied: true, message: await readError(response) };
    if (!response.ok) return { ok: false, message: await readError(response) };
    const data = mapReservationPolicy(await response.json());
    return data ? { ok: true, data } : { ok: false };
  } catch {
    return { ok: false };
  }
}

export async function loadOfferReservationPolicies(
  offerIds: string[],
): Promise<{ ok: true; items: ReservationPolicyEditorView[] } | { ok: false; denied?: boolean }> {
  if (offerIds.length === 0) return { ok: true, items: [] };
  try {
    const response = await fetch(
      `/v1/admin/settings/reservation-policy/offers?offerIds=${encodeURIComponent(offerIds.join(","))}`,
      { headers: adminHeaders() },
    );
    if (response.status === 401 || response.status === 403) return { ok: false, denied: true };
    if (!response.ok) return { ok: false };
    const payload = await response.json() as { items?: unknown[] };
    const items = (payload.items ?? [])
      .map(mapReservationPolicy)
      .filter((row): row is ReservationPolicyEditorView => row !== null);
    return { ok: true, items };
  } catch {
    return { ok: false };
  }
}

export function sourceCaption(field: ReservationPolicyFieldView): string {
  if (field.overridden) {
    return `${field.effectiveValue} — overridden`;
  }
  return `${field.effectiveValue} — inherited from ${field.sourceLabelEn}`;
}

export function sourceCaptionFa(field: ReservationPolicyFieldView): string {
  if (field.overridden) {
    return `${field.effectiveValue} دقیقه — بازنویسی‌شده`;
  }
  return `${field.effectiveValue} — ارث از ${field.sourceLabelFa}`;
}
