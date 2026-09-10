/**
 * Admin client for two-level shipping services catalog.
 */
import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "./admin-api.ts";

function actorId(): string {
  if (typeof window === "undefined") return "";
  return window.localStorage.getItem("tooba.adminActorUserId") ?? "";
}

function adminHeaders(extra?: Record<string, string>): Record<string, string> {
  return { Accept: "application/json", [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}

export type ShippingServiceTranslationWrite = {
  languageId: string;
  name: string;
  description?: string | null;
};

export type ShippingServiceOptionTranslationWrite = {
  languageId: string;
  name: string;
};

export type ShippingServiceOptionWrite = {
  shippingServiceOptionId?: string | null;
  code: string;
  isActive: boolean;
  sortOrder: number;
  translations: ShippingServiceOptionTranslationWrite[];
};

export type ShippingServiceWriteRequest = {
  code: string;
  providerKind: string;
  iconKey: string;
  colorKey: string;
  isActive: boolean;
  sortOrder: number;
  translations: ShippingServiceTranslationWrite[];
  options: ShippingServiceOptionWrite[];
};

export type ShippingServiceListItem = {
  shippingServiceId: string;
  code: string;
  providerKind: string;
  iconKey: string;
  colorKey: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
  optionCount: number;
  activeOptionCount: number;
};

export type ShippingServiceDetail = {
  shippingServiceId: string;
  code: string;
  providerKind: string;
  iconKey: string;
  colorKey: string;
  isActive: boolean;
  sortOrder: number;
  translations: ShippingServiceTranslationWrite[];
  options: Array<{
    shippingServiceOptionId: string;
    code: string;
    isActive: boolean;
    sortOrder: number;
    translations: ShippingServiceOptionTranslationWrite[];
  }>;
};

export type ShippingMethodTreeOption = {
  code: string;
  labelFa: string;
  name: string;
};

export type ShippingMethodTreeItem = {
  code: string;
  labelFa: string;
  name: string;
  providerKind: string;
  iconKey: string;
  colorKey: string;
  options: ShippingMethodTreeOption[];
};

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function text(value: unknown): string {
  return value == null ? "" : String(value);
}

function mapListItem(raw: unknown): ShippingServiceListItem | null {
  const row = asRecord(raw);
  if (!row) return null;
  const id = text(row.shippingServiceId ?? row.ShippingServiceId);
  const code = text(row.code ?? row.Code);
  if (!id || !code) return null;
  return {
    shippingServiceId: id,
    code,
    providerKind: text(row.providerKind ?? row.ProviderKind) || code,
    iconKey: text(row.iconKey ?? row.IconKey) || "truck",
    colorKey: text(row.colorKey ?? row.ColorKey) || "blue",
    name: text(row.name ?? row.Name) || code,
    isActive: Boolean(row.isActive ?? row.IsActive),
    sortOrder: Number(row.sortOrder ?? row.SortOrder ?? 0) || 0,
    optionCount: Number(row.optionCount ?? row.OptionCount ?? 0) || 0,
    activeOptionCount: Number(row.activeOptionCount ?? row.ActiveOptionCount ?? 0) || 0,
  };
}

function mapDetail(raw: unknown): ShippingServiceDetail | null {
  const row = asRecord(raw);
  if (!row) return null;
  const id = text(row.shippingServiceId ?? row.ShippingServiceId);
  const code = text(row.code ?? row.Code);
  if (!id || !code) return null;
  const translations = Array.isArray(row.translations ?? row.Translations)
    ? (row.translations ?? row.Translations) as unknown[]
    : [];
  const options = Array.isArray(row.options ?? row.Options)
    ? (row.options ?? row.Options) as unknown[]
    : [];
  return {
    shippingServiceId: id,
    code,
    providerKind: text(row.providerKind ?? row.ProviderKind) || code,
    iconKey: text(row.iconKey ?? row.IconKey) || "truck",
    colorKey: text(row.colorKey ?? row.ColorKey) || "blue",
    isActive: Boolean(row.isActive ?? row.IsActive),
    sortOrder: Number(row.sortOrder ?? row.SortOrder ?? 0) || 0,
    translations: translations.flatMap((t) => {
      const tr = asRecord(t);
      if (!tr) return [];
      const languageId = text(tr.languageId ?? tr.LanguageId);
      const name = text(tr.name ?? tr.Name);
      if (!languageId || !name) return [];
      return [{
        languageId,
        name,
        description: text(tr.description ?? tr.Description) || null,
      }];
    }),
    options: options.flatMap((o) => {
      const opt = asRecord(o);
      if (!opt) return [];
      const optionId = text(opt.shippingServiceOptionId ?? opt.ShippingServiceOptionId);
      const optionCode = text(opt.code ?? opt.Code);
      if (!optionId || !optionCode) return [];
      const otr = Array.isArray(opt.translations ?? opt.Translations)
        ? (opt.translations ?? opt.Translations) as unknown[]
        : [];
      return [{
        shippingServiceOptionId: optionId,
        code: optionCode,
        isActive: Boolean(opt.isActive ?? opt.IsActive),
        sortOrder: Number(opt.sortOrder ?? opt.SortOrder ?? 0) || 0,
        translations: otr.flatMap((t) => {
          const tr = asRecord(t);
          if (!tr) return [];
          const languageId = text(tr.languageId ?? tr.LanguageId);
          const name = text(tr.name ?? tr.Name);
          if (!languageId || !name) return [];
          return [{ languageId, name }];
        }),
      }];
    }),
  };
}

export async function loadAdminShippingServices(language?: string): Promise<AdminResult<ShippingServiceListItem[]>> {
  try {
    const qs = language ? `?language=${encodeURIComponent(language)}` : "";
    const response = await fetch(`/v1/admin/shipping-services${qs}`, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "shipping_service.load_failed" };
    }
    const rows = Array.isArray(payload) ? payload : [];
    return {
      state: "ok",
      data: rows.map(mapListItem).filter((x): x is ShippingServiceListItem => Boolean(x)),
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function loadAdminShippingService(serviceId: string): Promise<AdminResult<ShippingServiceDetail>> {
  try {
    const response = await fetch(`/v1/admin/shipping-services/${encodeURIComponent(serviceId)}`, {
      headers: adminHeaders(),
    });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "shipping_service.load_failed" };
    }
    const mapped = mapDetail(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "shipping_service.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function writeAdminShippingService(
  serviceId: string | null,
  body: ShippingServiceWriteRequest,
): Promise<AdminResult<ShippingServiceDetail>> {
  try {
    const response = await fetch(
      serviceId
        ? `/v1/admin/shipping-services/${encodeURIComponent(serviceId)}`
        : "/v1/admin/shipping-services/",
      {
        method: serviceId ? "PUT" : "POST",
        headers: adminHeaders({ "Content-Type": "application/json" }),
        body: JSON.stringify({
          code: body.code,
          providerKind: body.providerKind,
          iconKey: body.iconKey,
          colorKey: body.colorKey,
          isActive: body.isActive,
          sortOrder: body.sortOrder,
          translations: body.translations.map((t) => ({
            languageId: t.languageId,
            name: t.name,
            description: t.description ?? null,
          })),
          options: body.options.map((o) => ({
            shippingServiceOptionId: o.shippingServiceOptionId || null,
            code: o.code,
            isActive: o.isActive,
            sortOrder: o.sortOrder,
            translations: o.translations.map((t) => ({
              languageId: t.languageId,
              name: t.name,
            })),
          })),
        }),
      },
    );
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      const code = text(asRecord(payload)?.errorCode ?? asRecord(payload)?.title) || "shipping_service.write_failed";
      return { state: "error", data: null, status: response.status, message: code };
    }
    const mapped = mapDetail(payload);
    if (!mapped) {
      return { state: "error", data: null, status: response.status, message: "shipping_service.invalid_response" };
    }
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function deactivateAdminShippingService(serviceId: string): Promise<AdminResult<{ ok: boolean }>> {
  try {
    const response = await fetch(`/v1/admin/shipping-services/${encodeURIComponent(serviceId)}/deactivate`, {
      method: "POST",
      headers: adminHeaders(),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "shipping_service.deactivate_failed" };
    }
    return { state: "ok", data: { ok: true }, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function loadShippingMethodTree(language?: string): Promise<AdminResult<ShippingMethodTreeItem[]>> {
  try {
    const qs = language ? `?language=${encodeURIComponent(language)}` : "";
    const response = await fetch(`/v1/admin/shipping-methods${qs}`, { headers: adminHeaders() });
    const payload = await response.json().catch(() => null);
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: "shipping_methods.load_failed" };
    }
    const rows = Array.isArray(payload) ? payload : [];
    const mapped = rows.flatMap((raw) => {
      const row = asRecord(raw);
      if (!row) return [];
      const code = text(row.code ?? row.Code);
      if (!code) return [];
      const optionsRaw = Array.isArray(row.options ?? row.Options)
        ? (row.options ?? row.Options) as unknown[]
        : [];
      return [{
        code,
        labelFa: text(row.labelFa ?? row.LabelFa ?? row.name ?? row.Name) || code,
        name: text(row.name ?? row.Name ?? row.labelFa ?? row.LabelFa) || code,
        providerKind: text(row.providerKind ?? row.ProviderKind) || code,
        iconKey: text(row.iconKey ?? row.IconKey) || code,
        colorKey: text(row.colorKey ?? row.ColorKey) || "blue",
        options: optionsRaw.flatMap((o) => {
          const opt = asRecord(o);
          if (!opt) return [];
          const optionCode = text(opt.code ?? opt.Code);
          if (!optionCode) return [];
          const name = text(opt.labelFa ?? opt.LabelFa ?? opt.name ?? opt.Name) || optionCode;
          return [{ code: optionCode, labelFa: name, name }];
        }),
      } satisfies ShippingMethodTreeItem];
    });
    return { state: "ok", data: mapped, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
