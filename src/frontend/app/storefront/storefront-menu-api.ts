import { storefrontHostOrigin } from "./storefront-api.ts";

function menuApiOrigin(): string {
  return typeof window === "undefined" ? storefrontHostOrigin() : "";
}

export type StorefrontMenuItem = {
  menuItemId: string;
  parentMenuItemId: string | null;
  label: string;
  href: string | null;
  depth: number;
};

export type StorefrontHeaderMenu = {
  headerMenuId: string | null;
  usesFallback: boolean;
  items: StorefrontMenuItem[];
};

function readString(record: Record<string, unknown>, ...keys: string[]): string {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.trim()) return value;
  }
  return "";
}

function mapItem(raw: unknown): StorefrontMenuItem | null {
  if (!raw || typeof raw !== "object") return null;
  const record = raw as Record<string, unknown>;
  const menuItemId = readString(record, "menuItemId", "MenuItemId");
  const label = readString(record, "label", "Label");
  if (!menuItemId || !label) return null;
  const parent = readString(record, "parentMenuItemId", "ParentMenuItemId");
  const href = readString(record, "href", "Href");
  return {
    menuItemId,
    parentMenuItemId: parent || null,
    label,
    href: href || null,
    depth: Number(record.depth ?? record.Depth ?? 1),
  };
}

export async function loadStorefrontHeaderMenu(): Promise<StorefrontHeaderMenu> {
  try {
    const response = await fetch(`${menuApiOrigin()}/v1/storefront/header-menu`, { cache: "no-store" });
    if (!response.ok) return { headerMenuId: null, usesFallback: true, items: [] };
    const body = await response.json() as Record<string, unknown>;
    const itemsRaw = body.items ?? body.Items;
    return {
      headerMenuId: readString(body, "headerMenuId", "HeaderMenuId") || null,
      usesFallback: Boolean(body.usesFallback ?? body.UsesFallback ?? true),
      items: Array.isArray(itemsRaw) ? itemsRaw.map(mapItem).filter((row): row is StorefrontMenuItem => row !== null) : [],
    };
  } catch {
    return { headerMenuId: null, usesFallback: true, items: [] };
  }
}

export async function loadStorefrontMenu(menuId: string): Promise<StorefrontMenuItem[]> {
  try {
    const response = await fetch(`${menuApiOrigin()}/v1/storefront/menus/${menuId}`, { cache: "no-store" });
    if (!response.ok) return [];
    const body = await response.json() as unknown;
    return Array.isArray(body) ? body.map(mapItem).filter((row): row is StorefrontMenuItem => row !== null) : [];
  } catch {
    return [];
  }
}

export function menuChildren(items: StorefrontMenuItem[], parentId: string | null): StorefrontMenuItem[] {
  return items.filter((item) => item.parentMenuItemId === parentId);
}
