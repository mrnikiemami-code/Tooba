import { adminHeaders } from "../admin-api.ts";
import { mapAdminErrorMessage, parseAdminProblemErrorCode } from "../admin-error-map.ts";

export type CampaignLifecycleStatus = "Draft" | "Published" | "Archived" | string;
export type CampaignRuntimeKey = "draft" | "scheduled" | "active" | "expired" | "archived" | string;

export type AdminCampaignListItem = {
  campaignId: string;
  title: string;
  promotionTypeDisplayName: string;
  lifecycleStatus: CampaignLifecycleStatus;
  runtimeLabel: CampaignRuntimeKey;
  startAt: string;
  endAt: string | null;
  priority: number;
  memberCount: number;
  updatedAt: string;
};

export type AdminCampaignTranslation = {
  locale: string;
  title: string;
  subtitle: string;
  badgeText: string;
};

export type AdminCampaignMember = {
  sellerOfferId: string;
  sortOrder: number;
  productTitle: string;
  sellerDisplayName: string;
  baseAmount: number;
  campaignAmount: number | null;
  currency: string;
  availableUnits: number;
  inStock: boolean;
};

export type AdminCampaignDetail = {
  campaignId: string;
  promotionTypeId: string;
  promotionTypeDisplayName: string;
  lifecycleStatus: CampaignLifecycleStatus;
  runtimeLabel: CampaignRuntimeKey;
  startAt: string;
  endAt: string | null;
  priority: number;
  updatedAt: string;
  translations: AdminCampaignTranslation[];
  members: AdminCampaignMember[];
};

export type AdminCampaignTypeOption = {
  promotionTypeId: string;
  displayName: string;
};

export type AdminOfferCandidate = {
  sellerOfferId: string;
  productTitle: string;
  sellerDisplayName: string;
  baseAmount: number;
  currency: string;
  availableUnits: number;
  inStock: boolean;
};

export type AdminCampaignWriteInput = {
  promotionTypeId: string;
  startAt: string;
  endAt: string | null;
  priority: number;
  translations: AdminCampaignTranslation[];
};

export type AdminCampaignUpdateInput = {
  startAt: string;
  endAt: string | null;
  priority: number;
  translations: AdminCampaignTranslation[];
};

export type AdminResult<T> = { ok: true; data: T } | { ok: false; message: string; denied?: boolean };

const BASE = "/v1/admin/merchandising-campaigns";
const DEFAULT_LOCALE = "fa-IR";

function readString(record: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.trim()) return value;
  }
  return "";
}

function readNumber(record: Record<string, unknown>, ...keys: string[]): number {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) return value;
    if (typeof value === "string" && value.trim() && Number.isFinite(Number(value))) return Number(value);
  }
  return 0;
}

function readBool(record: Record<string, unknown>, ...keys: string[]): boolean {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") return value;
  }
  return false;
}

function normalizeRuntime(value: string): CampaignRuntimeKey {
  const key = value.trim().toLowerCase();
  if (key === "draft" || key === "پیش‌نویس") return "draft";
  if (key === "scheduled" || key === "future" || key === "زمان‌بندی‌شده") return "scheduled";
  if (key === "active" || key === "فعال") return "active";
  if (key === "expired" || key === "منقضی") return "expired";
  if (key === "archived" || key === "بایگانی‌شده") return "archived";
  return key || "draft";
}

function normalizeLifecycle(value: string): CampaignLifecycleStatus {
  const raw = value.trim();
  if (!raw) return "Draft";
  if (raw === "0" || /^draft$/i.test(raw)) return "Draft";
  if (raw === "1" || /^published$/i.test(raw)) return "Published";
  if (raw === "2" || /^archived$/i.test(raw)) return "Archived";
  return raw;
}

async function readJson(path: string, init?: RequestInit): Promise<{ status: number; body: unknown }> {
  const response = await fetch(path, {
    ...init,
    headers: {
      ...adminHeaders(init?.headers as Record<string, string> | undefined),
      ...(init?.headers ?? {}),
    },
  });
  const text = await response.text();
  let body: unknown = null;
  if (text) {
    try {
      body = JSON.parse(text);
    } catch {
      body = { title: text };
    }
  }
  return { status: response.status, body };
}

function fail(status: number, body: unknown): AdminResult<never> {
  if (status === 401 || status === 403) {
    return { ok: false, message: mapAdminErrorMessage("admin.authorization.denied", "fa"), denied: true };
  }
  const code = parseAdminProblemErrorCode(body, status);
  return { ok: false, message: mapAdminErrorMessage(code, "fa") };
}

function jsonHeaders(): Record<string, string> {
  return { "Content-Type": "application/json" };
}

function mapTranslation(raw: unknown): AdminCampaignTranslation | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const locale = readString(record, "locale", "Locale");
  if (!locale) return null;
  return {
    locale,
    title: readString(record, "title", "Title"),
    subtitle: readString(record, "subtitle", "Subtitle"),
    badgeText: readString(record, "badgeText", "BadgeText"),
  };
}

function mapMember(raw: unknown): AdminCampaignMember | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const sellerOfferId = readString(record, "sellerOfferId", "SellerOfferId");
  if (!sellerOfferId) return null;
  const campaignRaw = record.campaignAmount ?? record.CampaignAmount;
  const campaignAmount =
    campaignRaw == null || campaignRaw === ""
      ? null
      : typeof campaignRaw === "number"
        ? campaignRaw
        : Number(campaignRaw);
  return {
    sellerOfferId,
    sortOrder: readNumber(record, "sortOrder", "SortOrder"),
    productTitle: readString(record, "productTitle", "ProductTitle") || "کالا",
    sellerDisplayName: readString(record, "sellerDisplayName", "SellerDisplayName") || "فروشنده",
    baseAmount: readNumber(record, "baseAmount", "BaseAmount"),
    campaignAmount: campaignAmount != null && Number.isFinite(campaignAmount) ? campaignAmount : null,
    currency: readString(record, "currency", "Currency") || "IRR",
    availableUnits: readNumber(record, "availableUnits", "AvailableUnits"),
    inStock: readBool(record, "inStock", "InStock") || readNumber(record, "availableUnits", "AvailableUnits") > 0,
  };
}

function mapListItem(raw: unknown): AdminCampaignListItem | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const campaignId = readString(record, "campaignId", "CampaignId", "id", "Id");
  if (!campaignId) return null;
  const endRaw = record.endAt ?? record.EndAt;
  return {
    campaignId,
    title: readString(record, "title", "Title") || "بدون عنوان",
    promotionTypeDisplayName: readString(record, "promotionTypeDisplayName", "PromotionTypeDisplayName") || "—",
    lifecycleStatus: normalizeLifecycle(readString(record, "lifecycleStatus", "LifecycleStatus")),
    runtimeLabel: normalizeRuntime(readString(record, "runtimeLabel", "RuntimeLabel")),
    startAt: readString(record, "startAt", "StartAt"),
    endAt: endRaw == null || endRaw === "" ? null : String(endRaw),
    priority: readNumber(record, "priority", "Priority"),
    memberCount: readNumber(record, "memberCount", "MemberCount"),
    updatedAt: readString(record, "updatedAt", "UpdatedAt"),
  };
}

function mapDetail(raw: unknown): AdminCampaignDetail | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const list = mapListItem(raw);
  if (!list) return null;
  const translationsRaw = record.translations ?? record.Translations;
  const membersRaw = record.members ?? record.Members;
  return {
    campaignId: list.campaignId,
    promotionTypeId: readString(record, "promotionTypeId", "PromotionTypeId"),
    promotionTypeDisplayName: list.promotionTypeDisplayName,
    lifecycleStatus: list.lifecycleStatus,
    runtimeLabel: list.runtimeLabel,
    startAt: list.startAt,
    endAt: list.endAt,
    priority: list.priority,
    updatedAt: list.updatedAt,
    translations: Array.isArray(translationsRaw)
      ? translationsRaw.map(mapTranslation).filter((row): row is AdminCampaignTranslation => row != null)
      : [],
    members: Array.isArray(membersRaw)
      ? membersRaw
          .map(mapMember)
          .filter((row): row is AdminCampaignMember => row != null)
          .sort((a, b) => a.sortOrder - b.sortOrder)
      : [],
  };
}

function mapType(raw: unknown): AdminCampaignTypeOption | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const promotionTypeId = readString(record, "promotionTypeId", "PromotionTypeId", "id", "Id");
  const displayName = readString(record, "displayName", "DisplayName");
  if (!promotionTypeId || !displayName) return null;
  return { promotionTypeId, displayName };
}

function mapCandidate(raw: unknown): AdminOfferCandidate | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const sellerOfferId = readString(record, "sellerOfferId", "SellerOfferId");
  if (!sellerOfferId) return null;
  return {
    sellerOfferId,
    productTitle: readString(record, "productTitle", "ProductTitle") || "کالا",
    sellerDisplayName: readString(record, "sellerDisplayName", "SellerDisplayName") || "فروشنده",
    baseAmount: readNumber(record, "baseAmount", "BaseAmount"),
    currency: readString(record, "currency", "Currency") || "IRR",
    availableUnits: readNumber(record, "availableUnits", "AvailableUnits"),
    inStock: readBool(record, "inStock", "InStock") || readNumber(record, "availableUnits", "AvailableUnits") > 0,
  };
}

function itemsOf(body: unknown): unknown[] {
  if (Array.isArray(body)) return body;
  if (body && typeof body === "object") {
    const record = body as Record<string, unknown>;
    const items = record.items ?? record.Items;
    if (Array.isArray(items)) return items;
  }
  return [];
}

function totalOf(body: unknown, fallback: number): number {
  if (body && typeof body === "object") {
    const record = body as Record<string, unknown>;
    const total = record.total ?? record.Total ?? record.totalCount ?? record.TotalCount;
    if (typeof total === "number" && Number.isFinite(total)) return total;
  }
  return fallback;
}

export type ListCampaignsQuery = {
  search?: string;
  lifecycle?: string;
  promotionTypeId?: string;
  runtimeWindow?: string;
  skip?: number;
  take?: number;
  locale?: string;
};

export async function listAdminCampaigns(
  query: ListCampaignsQuery = {},
): Promise<AdminResult<{ items: AdminCampaignListItem[]; total: number }>> {
  const params = new URLSearchParams();
  if (query.search?.trim()) params.set("search", query.search.trim());
  if (query.lifecycle?.trim()) params.set("lifecycle", query.lifecycle.trim());
  if (query.promotionTypeId?.trim()) params.set("promotionTypeId", query.promotionTypeId.trim());
  if (query.runtimeWindow?.trim()) params.set("runtimeWindow", query.runtimeWindow.trim());
  params.set("skip", String(query.skip ?? 0));
  params.set("take", String(query.take ?? 100));
  params.set("locale", query.locale ?? DEFAULT_LOCALE);
  const { status, body } = await readJson(`${BASE}?${params.toString()}`);
  if (status >= 400) return fail(status, body);
  const items = itemsOf(body).map(mapListItem).filter((row): row is AdminCampaignListItem => row != null);
  return { ok: true, data: { items, total: totalOf(body, items.length) } };
}

export async function listAdminCampaignTypes(
  locale = DEFAULT_LOCALE,
): Promise<AdminResult<AdminCampaignTypeOption[]>> {
  const { status, body } = await readJson(`${BASE}/types?locale=${encodeURIComponent(locale)}`);
  if (status >= 400) return fail(status, body);
  const items = itemsOf(body).map(mapType).filter((row): row is AdminCampaignTypeOption => row != null);
  return { ok: true, data: items };
}

export async function getAdminCampaign(campaignId: string): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(`${BASE}/${encodeURIComponent(campaignId)}`);
  if (status >= 400) return fail(status, body);
  const detail = mapDetail(body);
  if (!detail) return { ok: false, message: "کمپین خوانده نشد." };
  return { ok: true, data: detail };
}

export async function createAdminCampaign(
  input: AdminCampaignWriteInput,
): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(BASE, {
    method: "POST",
    headers: jsonHeaders(),
    body: JSON.stringify(input),
  });
  if (status >= 400) return fail(status, body);
  const created = mapDetail(body);
  if (created) return { ok: true, data: created };
  const id = body && typeof body === "object"
    ? readString(body as Record<string, unknown>, "campaignId", "CampaignId", "id", "Id")
    : "";
  if (!id) return { ok: false, message: "کمپین ایجاد شد ولی شناسه برنگشت." };
  return getAdminCampaign(id);
}

export async function updateAdminCampaign(
  campaignId: string,
  input: AdminCampaignUpdateInput,
): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(`${BASE}/${encodeURIComponent(campaignId)}`, {
    method: "PUT",
    headers: jsonHeaders(),
    body: JSON.stringify(input),
  });
  if (status >= 400) return fail(status, body);
  const updated = mapDetail(body);
  if (updated) return { ok: true, data: updated };
  return getAdminCampaign(campaignId);
}

export async function publishAdminCampaign(campaignId: string): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(`${BASE}/${encodeURIComponent(campaignId)}/publish`, {
    method: "POST",
    headers: jsonHeaders(),
  });
  if (status >= 400) return fail(status, body);
  return getAdminCampaign(campaignId);
}

export async function archiveAdminCampaign(campaignId: string): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(`${BASE}/${encodeURIComponent(campaignId)}/archive`, {
    method: "POST",
    headers: jsonHeaders(),
  });
  if (status >= 400) return fail(status, body);
  return getAdminCampaign(campaignId);
}

export async function addAdminCampaignMember(
  campaignId: string,
  sellerOfferId: string,
): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(`${BASE}/${encodeURIComponent(campaignId)}/members`, {
    method: "POST",
    headers: jsonHeaders(),
    body: JSON.stringify({ sellerOfferId }),
  });
  if (status >= 400) return fail(status, body);
  return getAdminCampaign(campaignId);
}

export async function removeAdminCampaignMember(
  campaignId: string,
  sellerOfferId: string,
): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(
    `${BASE}/${encodeURIComponent(campaignId)}/members/${encodeURIComponent(sellerOfferId)}`,
    { method: "DELETE" },
  );
  if (status >= 400) return fail(status, body);
  return getAdminCampaign(campaignId);
}

export async function reorderAdminCampaignMembers(
  campaignId: string,
  orderedSellerOfferIds: string[],
): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(`${BASE}/${encodeURIComponent(campaignId)}/members/order`, {
    method: "PUT",
    headers: jsonHeaders(),
    body: JSON.stringify({ orderedSellerOfferIds }),
  });
  if (status >= 400) return fail(status, body);
  return getAdminCampaign(campaignId);
}

export async function setAdminCampaignMemberPrice(
  campaignId: string,
  sellerOfferId: string,
  amount: number,
  extras?: { currency?: string; market?: string; channel?: string },
): Promise<AdminResult<AdminCampaignDetail>> {
  const { status, body } = await readJson(
    `${BASE}/${encodeURIComponent(campaignId)}/members/${encodeURIComponent(sellerOfferId)}/price`,
    {
      method: "PUT",
      headers: jsonHeaders(),
      body: JSON.stringify({
        amount,
        ...(extras?.currency ? { currency: extras.currency } : {}),
        ...(extras?.market ? { market: extras.market } : {}),
        ...(extras?.channel ? { channel: extras.channel } : {}),
      }),
    },
  );
  if (status >= 400) return fail(status, body);
  return getAdminCampaign(campaignId);
}

export async function listAdminOfferCandidates(query: {
  search?: string;
  skip?: number;
  take?: number;
}): Promise<AdminResult<{ items: AdminOfferCandidate[]; total: number }>> {
  const params = new URLSearchParams();
  if (query.search?.trim()) params.set("search", query.search.trim());
  params.set("skip", String(query.skip ?? 0));
  params.set("take", String(query.take ?? 30));
  const { status, body } = await readJson(`${BASE}/offer-candidates?${params.toString()}`);
  if (status >= 400) return fail(status, body);
  const items = itemsOf(body).map(mapCandidate).filter((row): row is AdminOfferCandidate => row != null);
  return { ok: true, data: { items, total: totalOf(body, items.length) } };
}

/** برچسب فارسی وضعیت زمان‌اجرا. */
export function campaignRuntimeFa(key: CampaignRuntimeKey): string {
  switch (normalizeRuntime(String(key))) {
    case "draft":
      return "پیش‌نویس";
    case "scheduled":
      return "زمان‌بندی‌شده";
    case "active":
      return "فعال";
    case "expired":
      return "منقضی";
    case "archived":
      return "بایگانی‌شده";
    default:
      return "—";
  }
}

/** برچسب فارسی چرخه عمر. */
export function campaignLifecycleFa(status: CampaignLifecycleStatus): string {
  switch (normalizeLifecycle(String(status))) {
    case "Draft":
      return "پیش‌نویس";
    case "Published":
      return "منتشرشده";
    case "Archived":
      return "بایگانی‌شده";
    default:
      return String(status);
  }
}
