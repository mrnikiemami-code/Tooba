"use client";

import { adminHeaders } from "../admin/admin-api";
import {
  DEV_ACTOR_HEADER,
  SELLER_PARTY_HEADER,
  readActorUserId,
  readSellerPartyId,
} from "../vendor-panel/seller-api";
import type {
  AccApi,
  AccessUserHit,
  AssignmentRow,
  CeilingEntry,
  EffectiveAccess,
  PermissionDef,
  RolePermissionGrant,
  RoleRow,
  ScopeResourcesResult,
} from "./access-control-center";
import type { ScopeResourceItem } from "./scope-editor";

async function readJson<T>(path: string, headers: Record<string, string>, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { Accept: "application/json", ...headers, ...(init?.headers as Record<string, string> | undefined) },
  });
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.code ?? body?.title ?? `http.${response.status}`);
  }
  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

function mapRole(raw: Record<string, unknown>): RoleRow {
  return {
    id: String(raw.id),
    name: String(raw.name),
    code: String(raw.code),
    description: String(raw.description ?? ""),
    isSystem: Boolean(raw.isSystem),
    isMutable: Boolean(raw.isMutable),
    permissionCount: Number(raw.permissionCount ?? 0),
    assignmentCount: Number(raw.assignmentCount ?? 0),
  };
}

function mapPermission(raw: Record<string, unknown>): PermissionDef {
  return {
    permissionId: String(raw.permissionId),
    module: String(raw.module),
    displayNameKey: String(raw.displayNameKey ?? ""),
    descriptionKey: String(raw.descriptionKey ?? ""),
    delegable: Boolean(raw.delegable),
    scopeKinds: Array.isArray(raw.scopeKinds) ? raw.scopeKinds.map(Number) : [],
    disabledByCeiling: Boolean(raw.disabledByCeiling),
    platformOnly: Boolean(raw.platformOnly),
  };
}

function mapGrant(raw: Record<string, unknown>): RolePermissionGrant {
  return {
    permissionId: String(raw.permissionId),
    scopeKind: Number(raw.scopeKind ?? 1),
    scopeResourceId: raw.scopeResourceId == null ? null : String(raw.scopeResourceId),
    scopeDisplayName: raw.scopeDisplayName == null ? null : String(raw.scopeDisplayName),
    enabled: Boolean(raw.enabled),
  };
}

function mapCeiling(raw: Record<string, unknown>): CeilingEntry {
  return {
    permissionId: String(raw.permissionId),
    enabled: Boolean(raw.enabled),
    delegable: Boolean(raw.delegable),
    module: String(raw.module ?? ""),
    scopeKind: Number(raw.scopeKind ?? 1),
    scopeResourceId: raw.scopeResourceId == null ? null : String(raw.scopeResourceId),
    scopeDisplayName: raw.scopeDisplayName == null ? null : String(raw.scopeDisplayName),
  };
}

function mapEffective(raw: Record<string, unknown>): EffectiveAccess {
  const permissions = Array.isArray(raw.permissions) ? raw.permissions : [];
  return {
    userId: String(raw.userId),
    roleCodes: Array.isArray(raw.roleCodes) ? raw.roleCodes.map(String) : [],
    permissions: permissions.map((p) => {
      const row = p as Record<string, unknown>;
      return {
        permissionId: String(row.permissionId),
        module: String(row.module ?? ""),
        scopeKind: Number(row.scopeKind ?? 1),
        scopeResourceId: row.scopeResourceId == null ? null : String(row.scopeResourceId),
        scopeDisplayName: row.scopeDisplayName == null ? null : String(row.scopeDisplayName),
        inheritedViaRoleCodes: Array.isArray(row.inheritedViaRoleCodes)
          ? row.inheritedViaRoleCodes.map(String)
          : [],
        deniedByCeiling: Boolean(row.deniedByCeiling),
      };
    }),
  };
}

function mapUserHit(raw: Record<string, unknown>): AccessUserHit {
  return {
    userId: String(raw.userId),
    roleCodes: Array.isArray(raw.roleCodes) ? raw.roleCodes.map(String) : [],
    displayName: raw.displayName == null ? null : String(raw.displayName),
    email: raw.email == null ? null : String(raw.email),
    mobile: raw.mobile == null ? null : String(raw.mobile),
  };
}

/** مسیر scope-resources بر اساس ScopeKind عددی. */
export const SCOPE_KIND_PATH: Record<number, string> = {
  2: "categories",
  3: "products",
  4: "brands",
  5: "warehouses",
  6: "stores",
  7: "order-segments",
};

function mapScopeItems(kind: number, items: unknown[]): ScopeResourceItem[] {
  return items.map((raw) => {
    const item = raw as Record<string, unknown>;
    if (kind === 2) {
      return {
        id: String(item.categoryId ?? item.id),
        parentId: item.parentCategoryId == null ? null : String(item.parentCategoryId),
        name: String(item.name ?? ""),
      };
    }
    if (kind === 3) {
      return {
        id: String(item.productId ?? item.id),
        name: String(item.title ?? item.name ?? ""),
      };
    }
    if (kind === 4) {
      return {
        id: String(item.brandId ?? item.id),
        name: String(item.name ?? ""),
      };
    }
    return {
      id: String(item.id ?? ""),
      name: String(item.name ?? item.id ?? ""),
      deferred: Boolean(item.deferred),
    };
  });
}

async function fetchScopeResources(
  base: string,
  headers: Record<string, string>,
  kind: number,
  q: string,
): Promise<ScopeResourcesResult> {
  const pathSeg = SCOPE_KIND_PATH[kind];
  if (!pathSeg) {
    return { deferred: kind !== 1, items: [] };
  }
  const qs = q.trim() ? `?q=${encodeURIComponent(q.trim())}` : "";
  const raw = await readJson<Record<string, unknown>>(`${base}/scope-resources/${pathSeg}${qs}`, headers);
  const items = Array.isArray(raw.items) ? raw.items : [];
  return {
    deferred: Boolean(raw.deferred),
    items: mapScopeItems(kind, items),
  };
}

async function searchUsersAt(base: string, headers: Record<string, string>, q: string): Promise<AccessUserHit[]> {
  const qs = q.trim() ? `?q=${encodeURIComponent(q.trim())}` : "";
  const rows = await readJson<Record<string, unknown>[]>(`${base}/users${qs}`, headers);
  return rows.map(mapUserHit);
}

/** API کلاینت Admin برای Access Control پلتفرم. */
export function createAdminAccessApi(): AccApi {
  const headers = () => adminHeaders({ "Content-Type": "application/json" });
  const base = "/v1/admin/access-control";
  return {
    bootstrap: () => readJson(`${base}/bootstrap`, headers(), { method: "POST" }),
    listCatalog: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/permissions`, headers());
      return rows.map(mapPermission);
    },
    listRoles: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/roles`, headers());
      return rows.map(mapRole);
    },
    createRole: async (body) => mapRole(await readJson(`${base}/roles`, headers(), { method: "POST", body: JSON.stringify(body) })),
    updateRole: async (id, body) =>
      mapRole(await readJson(`${base}/roles/${id}`, headers(), { method: "PUT", body: JSON.stringify(body) })),
    cloneRole: async (id, body) =>
      mapRole(await readJson(`${base}/roles/${id}/clone`, headers(), { method: "POST", body: JSON.stringify(body) })),
    archiveRole: (id) => readJson(`${base}/roles/${id}`, headers(), { method: "DELETE" }),
    getRolePermissions: async (id) => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/roles/${id}/permissions`, headers());
      return rows.map(mapGrant);
    },
    setRolePermissions: (id, grants) =>
      readJson(`${base}/roles/${id}/permissions`, headers(), {
        method: "PUT",
        body: JSON.stringify(
          grants.map((g) => ({
            permissionId: g.permissionId,
            scopeKind: g.scopeKind,
            scopeResourceId: g.scopeResourceId,
            enabled: g.enabled,
          })),
        ),
      }),
    listAssignments: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/assignments`, headers());
      return rows.map(
        (r): AssignmentRow => ({
          id: String(r.id),
          userId: String(r.userId),
          roleId: String(r.roleId),
          roleName: String(r.roleName),
          roleCode: String(r.roleCode),
        }),
      );
    },
    assignRole: (userId, roleId) =>
      readJson(`${base}/assignments`, headers(), { method: "POST", body: JSON.stringify({ userId, roleId }) }),
    removeAssignment: (id) => readJson(`${base}/assignments/${id}`, headers(), { method: "DELETE" }),
    searchUsers: (q) => searchUsersAt(base, headers(), q),
    getEffective: async (userId) => mapEffective(await readJson(`${base}/users/${userId}/effective`, headers())),
    listScopeResources: (kind, q) => fetchScopeResources(base, headers(), kind, q),
    getMyCapabilities: async () => mapEffective(await readJson(`${base}/me/capabilities`, headers())),
  };
}

/** API کلاینت Admin برای Access Control یک فروشنده. */
export function createAdminSellerAccessApi(sellerId: string): AccApi {
  const headers = () => adminHeaders({ "Content-Type": "application/json" });
  const base = `/v1/admin/sellers/${encodeURIComponent(sellerId)}/access-control`;
  const platform = createAdminAccessApi();
  const platformBase = "/v1/admin/access-control";
  return {
    listCatalog: platform.listCatalog,
    listRoles: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/roles`, headers());
      return rows.map(mapRole);
    },
    createRole: async (body) => mapRole(await readJson(`${base}/roles`, headers(), { method: "POST", body: JSON.stringify(body) })),
    updateRole: async (id, body) =>
      mapRole(await readJson(`${base}/roles/${id}`, headers(), { method: "PUT", body: JSON.stringify(body) })),
    cloneRole: async (id, body) =>
      mapRole(await readJson(`${base}/roles/${id}/clone`, headers(), { method: "POST", body: JSON.stringify(body) })),
    archiveRole: (id) => readJson(`${base}/roles/${id}`, headers(), { method: "DELETE" }),
    getRolePermissions: async (id) => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/roles/${id}/permissions`, headers());
      return rows.map(mapGrant);
    },
    setRolePermissions: (id, grants) =>
      readJson(`${base}/roles/${id}/permissions`, headers(), {
        method: "PUT",
        body: JSON.stringify(
          grants.map((g) => ({
            permissionId: g.permissionId,
            scopeKind: g.scopeKind,
            scopeResourceId: g.scopeResourceId,
            enabled: g.enabled,
          })),
        ),
      }),
    listAssignments: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/assignments`, headers());
      return rows.map(
        (r): AssignmentRow => ({
          id: String(r.id),
          userId: String(r.userId),
          roleId: String(r.roleId),
          roleName: String(r.roleName ?? ""),
          roleCode: String(r.roleCode ?? ""),
        }),
      );
    },
    assignRole: (userId, roleId) =>
      readJson(`${base}/assignments`, headers(), { method: "POST", body: JSON.stringify({ userId, roleId }) }),
    removeAssignment: (id) => readJson(`${base}/assignments/${id}`, headers(), { method: "DELETE" }),
    searchUsers: (q) => searchUsersAt(platformBase, headers(), q),
    getEffective: async (userId) => mapEffective(await readJson(`${base}/users/${userId}/effective`, headers())),
    getCeiling: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/ceiling`, headers());
      return rows.map(mapCeiling);
    },
    setCeiling: (entries) =>
      readJson(`${base}/ceiling`, headers(), {
        method: "PUT",
        body: JSON.stringify({
          entries: entries.map((e) => ({
            permissionId: e.permissionId,
            enabled: e.enabled,
            scopeKind: e.scopeKind ?? 1,
            scopeResourceId: e.scopeResourceId ?? null,
          })),
        }),
      }),
    listScopeResources: (kind, q) => fetchScopeResources(platformBase, headers(), kind, q),
    getMyCapabilities: platform.getMyCapabilities,
  };
}

function sellerHeaders(): Record<string, string> {
  const sellerPartyId = readSellerPartyId(typeof window !== "undefined" ? window.location.search : "") ?? "";
  const actor = readActorUserId();
  const h: Record<string, string> = {
    Accept: "application/json",
    "Content-Type": "application/json",
    [SELLER_PARTY_HEADER]: sellerPartyId,
  };
  if (actor) h[DEV_ACTOR_HEADER] = actor;
  return h;
}

/** API کلاینت Seller. */
export function createSellerAccessApi(): AccApi {
  const headers = (): Record<string, string> => sellerHeaders();
  const base = "/v1/seller/access-control";
  return {
    listCatalog: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/permissions`, headers());
      return rows.map(mapPermission);
    },
    listRoles: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/roles`, headers());
      return rows.map(mapRole);
    },
    createRole: async (body) => mapRole(await readJson(`${base}/roles`, headers(), { method: "POST", body: JSON.stringify(body) })),
    updateRole: async (id, body) =>
      mapRole(await readJson(`${base}/roles/${id}`, headers(), { method: "PUT", body: JSON.stringify(body) })),
    cloneRole: async (id, body) =>
      mapRole(await readJson(`${base}/roles/${id}/clone`, headers(), { method: "POST", body: JSON.stringify(body) })),
    archiveRole: (id) => readJson(`${base}/roles/${id}`, headers(), { method: "DELETE" }),
    getRolePermissions: async (id) => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/roles/${id}/permissions`, headers());
      return rows.map(mapGrant);
    },
    setRolePermissions: (id, grants) =>
      readJson(`${base}/roles/${id}/permissions`, headers(), {
        method: "PUT",
        body: JSON.stringify(
          grants.map((g) => ({
            permissionId: g.permissionId,
            scopeKind: g.scopeKind,
            scopeResourceId: g.scopeResourceId,
            enabled: g.enabled,
          })),
        ),
      }),
    listAssignments: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/assignments`, headers());
      return rows.map(
        (r): AssignmentRow => ({
          id: String(r.id),
          userId: String(r.userId),
          roleId: String(r.roleId),
          roleName: String(r.roleName),
          roleCode: String(r.roleCode),
        }),
      );
    },
    assignRole: (userId, roleId) =>
      readJson(`${base}/assignments`, headers(), { method: "POST", body: JSON.stringify({ userId, roleId }) }),
    removeAssignment: (id) => readJson(`${base}/assignments/${id}`, headers(), { method: "DELETE" }),
    searchUsers: (q) => searchUsersAt(base, headers(), q),
    getEffective: async (userId) => mapEffective(await readJson(`${base}/users/${userId}/effective`, headers())),
    getCeiling: async () => {
      const rows = await readJson<Record<string, unknown>[]>(`${base}/ceiling`, headers());
      return rows.map(mapCeiling);
    },
    listScopeResources: (kind, q) => fetchScopeResources(base, headers(), kind, q),
    getMyCapabilities: async () => mapEffective(await readJson(`${base}/me/capabilities`, headers())),
  };
}

/** آیا مجموعهٔ capabilities شامل مجوز view است (بدون hardcode نقش). */
export function hasViewCapability(permissionIds: ReadonlySet<string>, viewPermissionId: string): boolean {
  return permissionIds.has(viewPermissionId);
}

/** آیا مجموعهٔ capabilities شامل مجوز داده‌شده است (بدون hardcode نقش). */
export function hasCapability(permissionIds: ReadonlySet<string>, permissionId: string): boolean {
  return permissionIds.has(permissionId);
}

/** شناسه‌های مجوز فعال از پاسخ me/capabilities. */
export function capabilityPermissionIds(effective: EffectiveAccess | null): Set<string> {
  if (!effective) return new Set();
  return new Set(effective.permissions.filter((p) => !p.deniedByCeiling).map((p) => p.permissionId));
}
