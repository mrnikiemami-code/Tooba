import { adminHeaders } from "../admin-api.ts";
import { mapAdminErrorMessage, parseAdminProblemErrorCode } from "../admin-error-map.ts";

export type AdminMenu = {
  menuId: string;
  title: string;
  locale: string;
  isEnabled: boolean;
  itemCount: number;
  updatedAt: string;
};

export type AdminMenuItem = {
  menuItemId: string;
  menuId: string;
  parentMenuItemId: string | null;
  label: string;
  linkType: string;
  targetId: string | null;
  externalUrl: string | null;
  sortOrder: number;
  isEnabled: boolean;
};

export type AdminMenuDetail = AdminMenu & { items: AdminMenuItem[] };

export type AdminHeaderMenuSelection = {
  headerMenuId: string | null;
  usesFallback: boolean;
  title: string | null;
};

export type AdminMenuUsage = { kind: string; label: string };

export type AdminResult<T> = { ok: true; data: T } | { ok: false; message: string; denied?: boolean };

function readString(record: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.trim()) return value;
  }
  return "";
}

function mapMenu(raw: unknown): AdminMenu | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const menuId = readString(record, "menuId", "MenuId");
  const title = readString(record, "title", "Title");
  if (!menuId || !title) return null;
  return {
    menuId,
    title,
    locale: readString(record, "locale", "Locale") || "fa",
    isEnabled: Boolean(record.isEnabled ?? record.IsEnabled ?? true),
    itemCount: Number(record.itemCount ?? record.ItemCount ?? 0),
    updatedAt: readString(record, "updatedAt", "UpdatedAt"),
  };
}

function mapItem(raw: unknown): AdminMenuItem | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const menuItemId = readString(record, "menuItemId", "MenuItemId");
  const menuId = readString(record, "menuId", "MenuId");
  const label = readString(record, "label", "Label");
  if (!menuItemId || !menuId || !label) return null;
  const parent = readString(record, "parentMenuItemId", "ParentMenuItemId");
  const target = readString(record, "targetId", "TargetId");
  return {
    menuItemId,
    menuId,
    parentMenuItemId: parent || null,
    label,
    linkType: readString(record, "linkType", "LinkType") || "Group",
    targetId: target || null,
    externalUrl: readString(record, "externalUrl", "ExternalUrl") || null,
    sortOrder: Number(record.sortOrder ?? record.SortOrder ?? 0),
    isEnabled: Boolean(record.isEnabled ?? record.IsEnabled ?? true),
  };
}

async function readJson(path: string, init?: RequestInit): Promise<{ status: number; body: unknown }> {
  const response = await fetch(path, { ...init, headers: { ...adminHeaders(Boolean(init?.method && init.method !== "GET")), ...(init?.headers ?? {}) } });
  const body = await response.json().catch(() => null);
  return { status: response.status, body };
}

function fail(status: number, body: unknown): AdminResult<never> {
  const code = parseAdminProblemErrorCode(body);
  return { ok: false, message: mapAdminErrorMessage(code, status === 401 || status === 403 ? "دسترسی مجاز نیست." : "درخواست منو انجام نشد."), denied: status === 401 || status === 403 };
}

export async function listAdminMenus(): Promise<AdminResult<AdminMenu[]>> {
  const { status, body } = await readJson("/v1/admin/menus");
  if (status === 401 || status === 403) return fail(status, body);
  if (status >= 400) return fail(status, body);
  const rows = Array.isArray(body) ? body.map(mapMenu).filter((row): row is AdminMenu => row !== null) : [];
  return { ok: true, data: rows };
}

export async function getAdminMenu(menuId: string): Promise<AdminResult<AdminMenuDetail>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}`);
  if (status >= 400) return fail(status, body);
  const menu = mapMenu(body);
  if (!menu || !body || typeof body !== "object") return { ok: false, message: "منو خوانده نشد." };
  const itemsRaw = (body as Record<string, unknown>).items ?? (body as Record<string, unknown>).Items;
  const items = Array.isArray(itemsRaw) ? itemsRaw.map(mapItem).filter((row): row is AdminMenuItem => row !== null) : [];
  return { ok: true, data: { ...menu, itemCount: items.length, items } };
}

export async function createAdminMenu(title: string, locale = "fa"): Promise<AdminResult<AdminMenuDetail>> {
  const { status, body } = await readJson("/v1/admin/menus", { method: "POST", body: JSON.stringify({ title, locale }) });
  if (status >= 400) return fail(status, body);
  return getAdminMenu(readString((body ?? {}) as Record<string, unknown>, "menuId", "MenuId"));
}

export async function updateAdminMenu(menuId: string, title: string, locale: string): Promise<AdminResult<AdminMenuDetail>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}`, { method: "PUT", body: JSON.stringify({ title, locale }) });
  if (status >= 400) return fail(status, body);
  return getAdminMenu(menuId);
}

export async function setAdminMenuEnabled(menuId: string, isEnabled: boolean): Promise<AdminResult<AdminMenuDetail>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}/enabled`, { method: "PUT", body: JSON.stringify({ isEnabled }) });
  if (status >= 400) return fail(status, body);
  return getAdminMenu(menuId);
}

export async function getAdminMenuUsage(menuId: string): Promise<AdminResult<AdminMenuUsage[]>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}/usage`);
  if (status >= 400) return fail(status, body);
  const rows = Array.isArray(body)
    ? body.flatMap((item): AdminMenuUsage[] => {
      if (!item || typeof item !== "object") return [];
      const record = item as Record<string, unknown>;
      const label = readString(record, "label", "Label");
      return label ? [{ kind: readString(record, "kind", "Kind"), label }] : [];
    })
    : [];
  return { ok: true, data: rows };
}

export async function deleteAdminMenu(menuId: string): Promise<AdminResult<true>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}`, { method: "DELETE" });
  if (status >= 400) return fail(status, body);
  return { ok: true, data: true };
}

export async function addAdminMenuItem(menuId: string, item: Partial<AdminMenuItem>): Promise<AdminResult<AdminMenuItem>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}/items`, {
    method: "POST",
    body: JSON.stringify({
      label: item.label,
      linkType: item.linkType,
      parentMenuItemId: item.parentMenuItemId,
      targetId: item.targetId,
      externalUrl: item.externalUrl,
      isEnabled: item.isEnabled ?? true,
    }),
  });
  if (status >= 400) return fail(status, body);
  const mapped = mapItem(body);
  return mapped ? { ok: true, data: mapped } : { ok: false, message: "آیتم ذخیره نشد." };
}

export async function updateAdminMenuItem(menuId: string, item: AdminMenuItem): Promise<AdminResult<AdminMenuItem>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}/items/${item.menuItemId}`, {
    method: "PUT",
    body: JSON.stringify({
      label: item.label,
      linkType: item.linkType,
      parentMenuItemId: item.parentMenuItemId,
      targetId: item.targetId,
      externalUrl: item.externalUrl,
      sortOrder: item.sortOrder,
      isEnabled: item.isEnabled,
    }),
  });
  if (status >= 400) return fail(status, body);
  const mapped = mapItem(body);
  return mapped ? { ok: true, data: mapped } : { ok: false, message: "آیتم ذخیره نشد." };
}

export async function setAdminMenuItemEnabled(menuId: string, itemId: string, isEnabled: boolean): Promise<AdminResult<AdminMenuItem>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}/items/${itemId}/enabled`, { method: "PUT", body: JSON.stringify({ isEnabled }) });
  if (status >= 400) return fail(status, body);
  const mapped = mapItem(body);
  return mapped ? { ok: true, data: mapped } : { ok: false, message: "وضعیت آیتم ذخیره نشد." };
}

export async function deleteAdminMenuItem(menuId: string, itemId: string): Promise<AdminResult<true>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}/items/${itemId}`, { method: "DELETE" });
  if (status >= 400) return fail(status, body);
  return { ok: true, data: true };
}

export async function reorderAdminMenuItems(menuId: string, itemIds: string[]): Promise<AdminResult<AdminMenuDetail>> {
  const { status, body } = await readJson(`/v1/admin/menus/${menuId}/items/reorder`, { method: "PUT", body: JSON.stringify({ itemIds }) });
  if (status >= 400) return fail(status, body);
  return getAdminMenu(menuId);
}

export async function getAdminHeaderMenu(): Promise<AdminResult<AdminHeaderMenuSelection>> {
  const { status, body } = await readJson("/v1/admin/menus/header");
  if (status >= 400) return fail(status, body);
  const record = body && typeof body === "object" ? body as Record<string, unknown> : {};
  return {
    ok: true,
    data: {
      headerMenuId: readString(record, "headerMenuId", "HeaderMenuId") || null,
      usesFallback: Boolean(record.usesFallback ?? record.UsesFallback ?? true),
      title: readString(record, "title", "Title") || null,
    },
  };
}

export async function setAdminHeaderMenu(headerMenuId: string | null): Promise<AdminResult<AdminHeaderMenuSelection>> {
  const { status, body } = await readJson("/v1/admin/menus/header", { method: "PUT", body: JSON.stringify({ headerMenuId }) });
  if (status >= 400) return fail(status, body);
  return getAdminHeaderMenu();
}
