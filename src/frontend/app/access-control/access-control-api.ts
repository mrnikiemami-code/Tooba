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
  AssignmentRow,
  CeilingEntry,
  EffectiveAccess,
  PermissionDef,
  RolePermissionGrant,
  RoleRow,
} from "./access-control-center";

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
    getRolePermissions: (id) => readJson<RolePermissionGrant[]>(`${base}/roles/${id}/permissions`, headers()),
    setRolePermissions: (id, grants) =>
      readJson(`${base}/roles/${id}/permissions`, headers(), { method: "PUT", body: JSON.stringify(grants) }),
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
    getEffective: (userId) => readJson<EffectiveAccess>(`${base}/users/${userId}/effective`, headers()),
  };
}

/** API کلاینت Admin برای Access Control یک فروشنده. */
export function createAdminSellerAccessApi(sellerId: string): AccApi {
  const headers = () => adminHeaders({ "Content-Type": "application/json" });
  const base = `/v1/admin/sellers/${encodeURIComponent(sellerId)}/access-control`;
  const platform = createAdminAccessApi();
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
    getRolePermissions: (id) => readJson<RolePermissionGrant[]>(`${base}/roles/${id}/permissions`, headers()),
    setRolePermissions: (id, grants) =>
      readJson(`${base}/roles/${id}/permissions`, headers(), { method: "PUT", body: JSON.stringify(grants) }),
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
    getEffective: (userId) => readJson<EffectiveAccess>(`${base}/users/${userId}/effective`, headers()),
    getCeiling: () => readJson<CeilingEntry[]>(`${base}/ceiling`, headers()),
    setCeiling: (entries) =>
      readJson(`${base}/ceiling`, headers(), {
        method: "PUT",
        body: JSON.stringify({ entries: entries.map((e) => ({ permissionId: e.permissionId, enabled: e.enabled })) }),
      }),
  };
}

/** API کلاینت Seller. */
export function createSellerAccessApi(): AccApi {
  const headers = (): Record<string, string> => {
    const sellerPartyId = readSellerPartyId(typeof window !== "undefined" ? window.location.search : "") ?? "";
    const actor = readActorUserId();
    const h: Record<string, string> = {
      Accept: "application/json",
      "Content-Type": "application/json",
      [SELLER_PARTY_HEADER]: sellerPartyId,
    };
    if (actor) h[DEV_ACTOR_HEADER] = actor;
    return h;
  };
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
    getRolePermissions: (id) => readJson<RolePermissionGrant[]>(`${base}/roles/${id}/permissions`, headers()),
    setRolePermissions: (id, grants) =>
      readJson(`${base}/roles/${id}/permissions`, headers(), { method: "PUT", body: JSON.stringify(grants) }),
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
    getEffective: (userId) => readJson<EffectiveAccess>(`${base}/users/${userId}/effective`, headers()),
    getCeiling: () => readJson<CeilingEntry[]>(`${base}/ceiling`, headers()),
  };
}
