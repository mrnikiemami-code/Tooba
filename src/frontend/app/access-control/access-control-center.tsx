"use client";

/**
 * مرکز کنترل دسترسی مشترک Admin/Seller — زبان بصری Shopeiva settings + customersList.
 * تفاوت فقط DATA SCOPE + CAPABILITIES + CEILING است؛ بدون UI جداگانه.
 */

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ChevronDown,
  ChevronLeft,
  Copy,
  KeyRound,
  Plus,
  Save,
  Search,
  Shield,
  Trash2,
  Users,
  X,
} from "lucide-react";
import { ScopeEditor, type ScopeResourceItem } from "./scope-editor";
import {
  getModuleLabel,
  getPermissionLabel,
  resolvePermissionLocale,
  type PermissionLocale,
} from "./permission-labels";

export type AccMode = "admin" | "seller" | "admin-seller";

export type PermissionDef = {
  permissionId: string;
  module: string;
  displayNameKey: string;
  descriptionKey: string;
  delegable: boolean;
  scopeKinds: number[];
  disabledByCeiling?: boolean;
  platformOnly?: boolean;
};

export type RoleRow = {
  id: string;
  name: string;
  code: string;
  description: string;
  isSystem: boolean;
  isMutable: boolean;
  permissionCount: number;
  assignmentCount: number;
};

export type RolePermissionGrant = {
  permissionId: string;
  scopeKind: number;
  scopeResourceId: string | null;
  scopeDisplayName?: string | null;
  enabled: boolean;
};

export type AssignmentRow = {
  id: string;
  userId: string;
  roleId: string;
  roleName: string;
  roleCode: string;
};

export type AccessUserHit = {
  userId: string;
  roleCodes: string[];
  displayName?: string | null;
  email?: string | null;
  mobile?: string | null;
};

export type EffectiveAccess = {
  userId: string;
  roleCodes: string[];
  permissions: Array<{
    permissionId: string;
    module: string;
    scopeKind: number;
    scopeResourceId: string | null;
    scopeDisplayName?: string | null;
    inheritedViaRoleCodes: string[];
    deniedByCeiling: boolean;
  }>;
};

export type CeilingEntry = {
  permissionId: string;
  enabled: boolean;
  delegable: boolean;
  module: string;
  scopeKind?: number;
  scopeResourceId?: string | null;
  scopeDisplayName?: string | null;
};

export type ScopeResourcesResult = {
  deferred?: boolean;
  items: ScopeResourceItem[];
};

export type AccApi = {
  listCatalog: () => Promise<PermissionDef[]>;
  listRoles: () => Promise<RoleRow[]>;
  createRole: (body: { name: string; code: string; description: string }) => Promise<RoleRow>;
  updateRole: (id: string, body: { name: string; description: string }) => Promise<RoleRow>;
  cloneRole: (id: string, body: { name: string; code: string; description?: string }) => Promise<RoleRow>;
  archiveRole: (id: string) => Promise<void>;
  getRolePermissions: (id: string) => Promise<RolePermissionGrant[]>;
  setRolePermissions: (id: string, grants: RolePermissionGrant[]) => Promise<void>;
  listAssignments: () => Promise<AssignmentRow[]>;
  assignRole: (userId: string, roleId: string) => Promise<void>;
  removeAssignment: (id: string) => Promise<void>;
  searchUsers: (q: string) => Promise<AccessUserHit[]>;
  getEffective: (userId: string) => Promise<EffectiveAccess>;
  getCeiling?: () => Promise<CeilingEntry[]>;
  setCeiling?: (entries: CeilingEntry[]) => Promise<void>;
  bootstrap?: () => Promise<void>;
  listScopeResources: (kind: number, q: string) => Promise<ScopeResourcesResult>;
  getMyCapabilities: () => Promise<EffectiveAccess>;
};

const SCOPE_KIND_LABELS: Record<number, string> = {
  1: "کل محدوده",
  2: "دسته",
  3: "محصول",
  4: "برند",
  5: "انبار",
  6: "فروشگاه",
  7: "قطعه سفارش",
};

function supportsScopedEditor(p: PermissionDef): boolean {
  return p.scopeKinds.some((k) => k !== 1);
}

function formatScopeLabel(scopeKind: number, scopeDisplayName?: string | null, scopeResourceId?: string | null): string {
  const kindLabel = SCOPE_KIND_LABELS[scopeKind] ?? `محدوده ${scopeKind}`;
  if (scopeKind === 1 || !scopeResourceId) {
    return kindLabel;
  }
  const name = scopeDisplayName?.trim();
  if (name) {
    return `${kindLabel}: «${name}»`;
  }
  return `${kindLabel}: منبع بدون نام`;
}

function userPrimaryLabel(hit: AccessUserHit | undefined): string {
  if (!hit) return "کاربر";
  return hit.displayName?.trim() || hit.email?.trim() || hit.mobile?.trim() || "کاربر";
}

function userSecondaryLabel(hit: AccessUserHit | undefined): string | null {
  if (!hit) return null;
  const parts = [hit.email, hit.mobile].filter((x) => x && x.trim());
  return parts.length ? parts.join(" · ") : null;
}

type TabId = "roles" | "users" | "ceiling";

/** انتخابگر کاربر قابل جستجو — بدون ورودی GUID. */
function UserPicker({
  api,
  selected,
  onSelect,
  placeholder = "جستجوی نام، ایمیل یا موبایل…",
}: {
  api: AccApi;
  selected: AccessUserHit | null;
  onSelect: (hit: AccessUserHit | null) => void;
  placeholder?: string;
}) {
  const [q, setQ] = useState("");
  const [open, setOpen] = useState(false);
  const [hits, setHits] = useState<AccessUserHit[]>([]);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    const handle = window.setTimeout(() => {
      setBusy(true);
      void api
        .searchUsers(q)
        .then((rows) => {
          if (!cancelled) setHits(rows);
        })
        .catch(() => {
          if (!cancelled) setHits([]);
        })
        .finally(() => {
          if (!cancelled) setBusy(false);
        });
    }, 220);
    return () => {
      cancelled = true;
      window.clearTimeout(handle);
    };
  }, [api, open, q]);

  if (selected) {
    return (
      <div
        className="flex items-center justify-between gap-2 rounded-xl border border-[#2563EB]/30 bg-[#2563EB]/5 px-3 py-2"
        data-testid="user-picker-selected"
      >
        <div className="min-w-0">
          <p className="text-sm font-bold truncate">{userPrimaryLabel(selected)}</p>
          {userSecondaryLabel(selected) ? (
            <p className="text-[11px] text-gray-500 truncate mt-0.5">{userSecondaryLabel(selected)}</p>
          ) : null}
        </div>
        <button
          type="button"
          className="shrink-0 rounded-lg p-1 text-gray-500 hover:bg-white"
          aria-label="پاک کردن انتخاب"
          onClick={() => onSelect(null)}
        >
          <X className="w-4 h-4" />
        </button>
      </div>
    );
  }

  return (
    <div className="relative" data-testid="user-picker">
      <div className="relative">
        <Search className="w-4 h-4 absolute right-3 top-1/2 -translate-y-1/2 text-gray-400" />
        <input
          className="w-full rounded-xl border border-gray-200 pr-9 pl-3 py-2 text-sm"
          placeholder={placeholder}
          value={q}
          onChange={(e) => {
            setQ(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
        />
      </div>
      {open ? (
        <ul className="absolute z-20 mt-1 w-full max-h-56 overflow-auto rounded-xl border border-gray-200 bg-white shadow-lg divide-y divide-gray-50">
          {busy ? <li className="px-3 py-2 text-xs text-gray-400">در حال جستجو…</li> : null}
          {!busy && hits.length === 0 ? (
            <li className="px-3 py-2 text-xs text-gray-400">نتیجه‌ای یافت نشد</li>
          ) : null}
          {hits.map((h) => (
            <li key={h.userId}>
              <button
                type="button"
                className="w-full text-right px-3 py-2 hover:bg-gray-50"
                onClick={() => {
                  onSelect(h);
                  setOpen(false);
                  setQ("");
                }}
              >
                <p className="text-sm font-bold">{userPrimaryLabel(h)}</p>
                {userSecondaryLabel(h) ? (
                  <p className="text-[11px] text-gray-500 mt-0.5">{userSecondaryLabel(h)}</p>
                ) : null}
                {h.roleCodes.length ? (
                  <p className="text-[10px] text-[#2563EB] mt-0.5 font-bold">{h.roleCodes.join("، ")}</p>
                ) : null}
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

/**
 * پوستهٔ مشترک Access Control Center.
 */
export function AccessControlCenter({
  mode,
  title,
  api,
  canManage,
}: {
  mode: AccMode;
  title: string;
  api: AccApi;
  canManage: boolean;
}) {
  const [tab, setTab] = useState<TabId>("roles");
  const [roles, setRoles] = useState<RoleRow[]>([]);
  const [catalog, setCatalog] = useState<PermissionDef[]>([]);
  const [assignments, setAssignments] = useState<AssignmentRow[]>([]);
  const [ceiling, setCeiling] = useState<CeilingEntry[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState<string | null>(null);
  const [grants, setGrants] = useState<RolePermissionGrant[]>([]);
  const [dirty, setDirty] = useState(false);
  const [search, setSearch] = useState("");
  const [moduleFilter, setModuleFilter] = useState<string>("all");
  const [selectedOnly, setSelectedOnly] = useState(false);
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [effectiveUserId, setEffectiveUserId] = useState<string | null>(null);
  const [effective, setEffective] = useState<EffectiveAccess | null>(null);
  const [assignPick, setAssignPick] = useState<AccessUserHit | null>(null);
  const [memberPick, setMemberPick] = useState<AccessUserHit | null>(null);
  const [memberFilter, setMemberFilter] = useState("");
  const [userFilter, setUserFilter] = useState("");
  const [userHits, setUserHits] = useState<AccessUserHit[]>([]);
  const [newRoleName, setNewRoleName] = useState("");
  const [newRoleCode, setNewRoleCode] = useState("");
  const [locale, setLocale] = useState<PermissionLocale>("fa");
  const [usersAssignRoleId, setUsersAssignRoleId] = useState<string>("");

  useEffect(() => {
    setLocale(resolvePermissionLocale());
  }, []);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      if (api.bootstrap) {
        await api.bootstrap().catch(() => undefined);
      }
      const [c, r, a, u] = await Promise.all([
        api.listCatalog(),
        api.listRoles(),
        api.listAssignments(),
        api.searchUsers("").catch(() => [] as AccessUserHit[]),
      ]);
      setCatalog(c);
      setRoles(r);
      setAssignments(a);
      setUserHits(u);
      if (api.getCeiling) {
        setCeiling(await api.getCeiling());
      }
      if (!selectedRoleId && r[0]) {
        setSelectedRoleId(r[0].id);
      }
      if (!usersAssignRoleId && r[0]) {
        setUsersAssignRoleId(r[0].id);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "خطا در بارگذاری");
    } finally {
      setLoading(false);
    }
  }, [api, selectedRoleId, usersAssignRoleId]);

  useEffect(() => {
    void refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!selectedRoleId) return;
    void (async () => {
      const g = await api.getRolePermissions(selectedRoleId);
      const missing = g.filter((row) => row.enabled && row.scopeResourceId && !row.scopeDisplayName && row.scopeKind !== 1);
      if (missing.length === 0) {
        setGrants(g);
        setDirty(false);
        return;
      }
      const byKind = new Map<number, string[]>();
      for (const row of missing) {
        const list = byKind.get(row.scopeKind) ?? [];
        list.push(row.scopeResourceId!);
        byKind.set(row.scopeKind, list);
      }
      const nameById = new Map<string, string>();
      await Promise.all(
        Array.from(byKind.entries()).map(async ([kind, ids]) => {
          const res = await api.listScopeResources(kind, "");
          for (const item of res.items) {
            if (ids.includes(item.id)) nameById.set(item.id, item.name);
          }
        }),
      );
      setGrants(
        g.map((row) =>
          row.scopeResourceId && !row.scopeDisplayName && nameById.has(row.scopeResourceId)
            ? { ...row, scopeDisplayName: nameById.get(row.scopeResourceId) }
            : row,
        ),
      );
      setDirty(false);
    })();
  }, [api, selectedRoleId]);

  const modules = useMemo(() => {
    const set = new Set(catalog.map((c) => c.module));
    return ["all", ...Array.from(set).sort()];
  }, [catalog]);

  const enabledSet = useMemo(() => new Set(grants.filter((g) => g.enabled).map((g) => g.permissionId)), [grants]);

  const grantByPermission = useMemo(() => {
    const map = new Map<string, RolePermissionGrant>();
    for (const g of grants) {
      if (!g.enabled) continue;
      if (!map.has(g.permissionId)) map.set(g.permissionId, g);
    }
    return map;
  }, [grants]);

  const loadResources = useCallback(
    (kind: number, q: string) => api.listScopeResources(kind, q),
    [api],
  );

  const hitByUserId = useMemo(() => {
    const map = new Map<string, AccessUserHit>();
    for (const h of userHits) map.set(h.userId, h);
    return map;
  }, [userHits]);

  const roleNameByCode = useMemo(() => {
    const map = new Map<string, string>();
    for (const r of roles) map.set(r.code, r.name);
    return map;
  }, [roles]);

  const membersForSelectedRole = useMemo(() => {
    if (!selectedRoleId) return [];
    const q = memberFilter.trim().toLowerCase();
    return assignments
      .filter((a) => a.roleId === selectedRoleId)
      .filter((a) => {
        if (!q) return true;
        const hit = hitByUserId.get(a.userId);
        const label = userPrimaryLabel(hit).toLowerCase();
        const secondary = (userSecondaryLabel(hit) ?? "").toLowerCase();
        return label.includes(q) || secondary.includes(q) || a.userId.toLowerCase().includes(q);
      });
  }, [assignments, selectedRoleId, memberFilter, hitByUserId]);

  const usersOverview = useMemo(() => {
    const map = new Map<
      string,
      { userId: string; hit?: AccessUserHit; roles: AssignmentRow[] }
    >();
    for (const a of assignments) {
      const row = map.get(a.userId) ?? { userId: a.userId, hit: hitByUserId.get(a.userId), roles: [] };
      row.roles.push(a);
      row.hit = row.hit ?? hitByUserId.get(a.userId);
      map.set(a.userId, row);
    }
    for (const h of userHits) {
      if (!map.has(h.userId)) {
        map.set(h.userId, { userId: h.userId, hit: h, roles: [] });
      }
    }
    const q = userFilter.trim().toLowerCase();
    return Array.from(map.values())
      .filter((u) => {
        if (!q) return true;
        const label = userPrimaryLabel(u.hit).toLowerCase();
        const secondary = (userSecondaryLabel(u.hit) ?? "").toLowerCase();
        const roleNames = u.roles.map((r) => r.roleName.toLowerCase()).join(" ");
        return label.includes(q) || secondary.includes(q) || roleNames.includes(q);
      })
      .sort((a, b) =>
        userPrimaryLabel(a.hit).localeCompare(userPrimaryLabel(b.hit), "fa"),
      );
  }, [assignments, userHits, hitByUserId, userFilter]);

  const grouped = useMemo(() => {
    const q = search.trim().toLowerCase();
    const rows = catalog.filter((p) => {
      if (moduleFilter !== "all" && p.module !== moduleFilter) return false;
      if (selectedOnly && !enabledSet.has(p.permissionId)) return false;
      if (!q) return true;
      const label = getPermissionLabel(p.permissionId, locale);
      return (
        p.permissionId.toLowerCase().includes(q) ||
        p.module.toLowerCase().includes(q) ||
        label.title.toLowerCase().includes(q) ||
        label.description.toLowerCase().includes(q) ||
        getModuleLabel(p.module, locale).toLowerCase().includes(q)
      );
    });
    const map = new Map<string, PermissionDef[]>();
    for (const p of rows) {
      const list = map.get(p.module) ?? [];
      list.push(p);
      map.set(p.module, list);
    }
    return Array.from(map.entries());
  }, [catalog, search, moduleFilter, selectedOnly, enabledSet, locale]);

  function togglePermission(p: PermissionDef) {
    if (!canManage) return;
    if (p.disabledByCeiling || p.platformOnly) return;
    setDirty(true);
    setGrants((prev) => {
      const on = prev.some((g) => g.permissionId === p.permissionId && g.enabled);
      if (on) {
        return prev.filter((g) => g.permissionId !== p.permissionId);
      }
      return [
        ...prev.filter((g) => g.permissionId !== p.permissionId),
        { permissionId: p.permissionId, scopeKind: 1, scopeResourceId: null, scopeDisplayName: null, enabled: true },
      ];
    });
  }

  function updateGrantScope(
    permissionId: string,
    next: { scopeKind: number; scopeResourceId: string | null; scopeDisplayName?: string | null },
  ) {
    if (!canManage) return;
    setDirty(true);
    setGrants((prev) => {
      const rest = prev.filter((g) => g.permissionId !== permissionId);
      return [
        ...rest,
        {
          permissionId,
          scopeKind: next.scopeKind,
          scopeResourceId: next.scopeKind === 1 ? null : next.scopeResourceId,
          scopeDisplayName: next.scopeKind === 1 ? null : (next.scopeDisplayName ?? null),
          enabled: true,
        },
      ];
    });
  }

  function selectAllGroup(_module: string, items: PermissionDef[]) {
    if (!canManage) return;
    setDirty(true);
    setGrants((prev) => {
      const next = prev.filter((g) => !items.some((i) => i.permissionId === g.permissionId));
      for (const p of items) {
        if (p.disabledByCeiling || p.platformOnly) continue;
        next.push({
          permissionId: p.permissionId,
          scopeKind: 1,
          scopeResourceId: null,
          scopeDisplayName: null,
          enabled: true,
        });
      }
      return next;
    });
  }

  function clearGroup(items: PermissionDef[]) {
    if (!canManage) return;
    setDirty(true);
    setGrants((prev) => prev.filter((g) => !items.some((i) => i.permissionId === g.permissionId)));
  }

  async function loadEffectivePreview(userId: string) {
    setEffectiveUserId(userId);
    const raw = await api.getEffective(userId);
    const missing = raw.permissions.filter((p) => p.scopeResourceId && !p.scopeDisplayName && p.scopeKind !== 1);
    if (missing.length === 0) {
      setEffective(raw);
      return;
    }
    const byKind = new Map<number, string[]>();
    for (const p of missing) {
      const list = byKind.get(p.scopeKind) ?? [];
      list.push(p.scopeResourceId!);
      byKind.set(p.scopeKind, list);
    }
    const nameById = new Map<string, string>();
    await Promise.all(
      Array.from(byKind.entries()).map(async ([kind, ids]) => {
        const res = await api.listScopeResources(kind, "");
        for (const item of res.items) {
          if (ids.includes(item.id)) nameById.set(item.id, item.name);
        }
      }),
    );
    setEffective({
      ...raw,
      permissions: raw.permissions.map((p) =>
        p.scopeResourceId && !p.scopeDisplayName && nameById.has(p.scopeResourceId)
          ? { ...p, scopeDisplayName: nameById.get(p.scopeResourceId) }
          : p,
      ),
    });
  }

  async function savePermissions() {
    if (!selectedRoleId || !canManage) return;
    await api.setRolePermissions(selectedRoleId, grants);
    setDirty(false);
    await refresh();
  }

  async function createRole() {
    if (!canManage || !newRoleName || !newRoleCode) return;
    const role = await api.createRole({ name: newRoleName, code: newRoleCode, description: "" });
    setNewRoleName("");
    setNewRoleCode("");
    setSelectedRoleId(role.id);
    await refresh();
  }

  async function cloneSelected() {
    if (!selectedRoleId || !canManage) return;
    const source = roles.find((r) => r.id === selectedRoleId);
    if (!source) return;
    const role = await api.cloneRole(selectedRoleId, {
      name: `${source.name} (کپی)`,
      code: `${source.code}-copy-${Date.now().toString(36).slice(-4)}`,
      description: source.description,
    });
    setSelectedRoleId(role.id);
    await refresh();
  }

  const selectedRole = roles.find((r) => r.id === selectedRoleId) ?? null;

  return (
    <div className="space-y-6" data-testid="access-control-center" data-mode={mode}>
      <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 md:p-6">
        <div className="flex items-center gap-3">
          <span className="w-10 h-10 bg-[#2563EB]/10 text-[#2563EB] rounded-xl flex items-center justify-center">
            <Shield className="w-5 h-5" />
          </span>
          <div>
            <h1 className="font-black text-lg">{title}</h1>
            <p className="text-xs text-gray-500 mt-1">
              نقش، مجوز، تخصیص و پیش‌نمایش دسترسی — بدون نمایش کلیدهای فنی یا tuple
            </p>
          </div>
        </div>

        <div className="mt-5 flex flex-wrap gap-2">
          {(
            [
              ["roles", "نقش‌ها", KeyRound],
              ["users", "کاربران", Users],
              ...(mode !== "seller" && api.getCeiling ? ([["ceiling", "سقف تفویض", Shield]] as const) : []),
            ] as const
          ).map(([id, label, Icon]) => (
            <button
              key={id}
              type="button"
              onClick={() => setTab(id as TabId)}
              className={`inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-bold transition ${
                tab === id ? "bg-[#2563EB] text-white shadow-md shadow-[#2563EB]/25" : "bg-gray-50 text-gray-700 hover:bg-gray-100"
              }`}
            >
              <Icon className="w-4 h-4" />
              {label}
            </button>
          ))}
        </div>
      </div>

      {error ? (
        <div className="rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">{error}</div>
      ) : null}
      {loading ? <div className="text-sm text-gray-500">در حال بارگذاری…</div> : null}

      {tab === "roles" ? (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
          <section className="lg:col-span-4 bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
            <div className="p-4 border-b border-gray-100 flex items-center justify-between">
              <h2 className="font-black text-sm">فهرست نقش‌ها</h2>
              <span className="text-xs bg-[#2563EB]/10 text-[#2563EB] rounded-full px-2 py-0.5 font-bold">{roles.length}</span>
            </div>
            <ul className="divide-y divide-gray-50 max-h-[28rem] overflow-auto">
              {roles.map((role) => (
                <li key={role.id}>
                  <button
                    type="button"
                    onClick={() => {
                      if (dirty && !window.confirm("تغییرات ذخیره‌نشده از بین می‌رود. ادامه؟")) return;
                      setSelectedRoleId(role.id);
                    }}
                    className={`w-full text-right px-4 py-3 hover:bg-gray-50 transition ${
                      selectedRoleId === role.id ? "bg-[#2563EB]/5" : ""
                    }`}
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="font-bold text-sm">{role.name}</span>
                      {role.isSystem ? (
                        <span className="text-[10px] rounded-full bg-amber-50 text-amber-700 px-2 py-0.5 font-bold">سیستمی</span>
                      ) : null}
                    </div>
                    <p className="text-[11px] text-gray-400 mt-1">
                      {role.permissionCount} مجوز · {role.assignmentCount} عضو
                    </p>
                  </button>
                </li>
              ))}
            </ul>
            {canManage ? (
              <div className="p-4 border-t border-gray-100 space-y-2">
                <input
                  className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  placeholder="نام نقش جدید"
                  value={newRoleName}
                  onChange={(e) => setNewRoleName(e.target.value)}
                />
                <input
                  className="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  placeholder="کد داخلی (مثلاً mobile-ops)"
                  value={newRoleCode}
                  onChange={(e) => setNewRoleCode(e.target.value)}
                />
                <button
                  type="button"
                  onClick={() => void createRole()}
                  className="w-full inline-flex items-center justify-center gap-2 rounded-xl bg-[#2563EB] text-white py-2 text-sm font-bold hover:bg-[#1d4ed8] transition"
                >
                  <Plus className="w-4 h-4" />
                  ایجاد نقش
                </button>
              </div>
            ) : null}
          </section>

          <section className="lg:col-span-8 space-y-4">
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-4 md:p-5">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h2 className="font-black text-base">{selectedRole?.name ?? "نقش را انتخاب کنید"}</h2>
                  <p className="text-xs text-gray-500 mt-1">{selectedRole?.description || "مجوزها و اعضای این نقش"}</p>
                </div>
                <div className="flex flex-wrap gap-2">
                  {canManage && selectedRole && !selectedRole.isSystem ? (
                    <>
                      <button
                        type="button"
                        onClick={() => void cloneSelected()}
                        className="inline-flex items-center gap-1.5 rounded-xl border border-gray-200 px-3 py-2 text-xs font-bold hover:bg-gray-50"
                      >
                        <Copy className="w-3.5 h-3.5" />
                        کلون
                      </button>
                      <button
                        type="button"
                        onClick={() =>
                          void (async () => {
                            if (!selectedRoleId) return;
                            if (!window.confirm("بایگانی نقش؟")) return;
                            await api.archiveRole(selectedRoleId);
                            setSelectedRoleId(null);
                            await refresh();
                          })()
                        }
                        className="inline-flex items-center gap-1.5 rounded-xl border border-red-200 text-red-600 px-3 py-2 text-xs font-bold hover:bg-red-50"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                        بایگانی
                      </button>
                    </>
                  ) : null}
                  {canManage && selectedRole && selectedRole.isMutable ? (
                    <button
                      type="button"
                      disabled={!dirty}
                      onClick={() => void savePermissions()}
                      className="inline-flex items-center gap-1.5 rounded-xl bg-[#2563EB] text-white px-3 py-2 text-xs font-bold disabled:opacity-40 hover:bg-[#1d4ed8]"
                    >
                      <Save className="w-3.5 h-3.5" />
                      ذخیره مجوزها
                    </button>
                  ) : null}
                </div>
              </div>

              <div className="mt-4 flex flex-wrap gap-2">
                <div className="relative flex-1 min-w-[12rem]">
                  <Search className="w-4 h-4 absolute right-3 top-1/2 -translate-y-1/2 text-gray-400" />
                  <input
                    className="w-full rounded-xl border border-gray-200 pr-9 pl-3 py-2 text-sm"
                    placeholder="جستجوی مجوز…"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                  />
                </div>
                <select
                  className="rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  value={moduleFilter}
                  onChange={(e) => setModuleFilter(e.target.value)}
                >
                  {modules.map((m) => (
                    <option key={m} value={m}>
                      {m === "all" ? "همه ماژول‌ها" : getModuleLabel(m, locale)}
                    </option>
                  ))}
                </select>
                <label className="inline-flex items-center gap-2 rounded-xl border border-gray-200 px-3 py-2 text-xs font-bold">
                  <input type="checkbox" checked={selectedOnly} onChange={(e) => setSelectedOnly(e.target.checked)} />
                  فقط انتخاب‌شده
                </label>
              </div>
            </div>

            {selectedRole ? (
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-4 md:p-5" data-testid="role-members">
                <div className="flex flex-wrap items-center justify-between gap-2 mb-3">
                  <h3 className="font-black text-sm">
                    اعضای نقش
                    <span className="mr-2 text-xs bg-[#2563EB]/10 text-[#2563EB] rounded-full px-2 py-0.5 font-bold">
                      {assignments.filter((a) => a.roleId === selectedRole.id).length}
                    </span>
                  </h3>
                  <div className="relative min-w-[10rem] flex-1 max-w-xs">
                    <Search className="w-3.5 h-3.5 absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400" />
                    <input
                      className="w-full rounded-xl border border-gray-200 pr-8 pl-3 py-1.5 text-xs"
                      placeholder="فیلتر عضو…"
                      value={memberFilter}
                      onChange={(e) => setMemberFilter(e.target.value)}
                    />
                  </div>
                </div>
                {canManage ? (
                  <div className="flex flex-col sm:flex-row gap-2 mb-3">
                    <div className="flex-1">
                      <UserPicker api={api} selected={memberPick} onSelect={setMemberPick} />
                    </div>
                    <button
                      type="button"
                      className="rounded-xl bg-[#2563EB] text-white px-4 py-2 text-sm font-bold disabled:opacity-40"
                      disabled={!memberPick}
                      onClick={() =>
                        void (async () => {
                          if (!memberPick || !selectedRoleId) return;
                          await api.assignRole(memberPick.userId, selectedRoleId);
                          setMemberPick(null);
                          await refresh();
                        })()
                      }
                    >
                      افزودن عضو
                    </button>
                  </div>
                ) : null}
                <ul className="divide-y divide-gray-50 max-h-48 overflow-auto">
                  {membersForSelectedRole.length === 0 ? (
                    <li className="py-3 text-xs text-gray-400">هنوز عضوی برای این نقش نیست.</li>
                  ) : (
                    membersForSelectedRole.map((a) => {
                      const hit = hitByUserId.get(a.userId);
                      return (
                        <li key={a.id} className="py-2.5 flex items-center justify-between gap-2">
                          <button
                            type="button"
                            className="text-right min-w-0 hover:opacity-80"
                            onClick={() => {
                              setTab("users");
                              void loadEffectivePreview(a.userId);
                            }}
                          >
                            <p className="text-sm font-bold truncate">{userPrimaryLabel(hit)}</p>
                            {userSecondaryLabel(hit) ? (
                              <p className="text-[11px] text-gray-500 truncate mt-0.5">{userSecondaryLabel(hit)}</p>
                            ) : null}
                          </button>
                          <div className="flex items-center gap-2 shrink-0">
                            <button
                              type="button"
                              className="text-[11px] text-[#2563EB] font-bold"
                              onClick={() => {
                                setTab("users");
                                void loadEffectivePreview(a.userId);
                              }}
                            >
                              دسترسی مؤثر
                            </button>
                            {canManage ? (
                              <button
                                type="button"
                                className="text-[11px] text-red-600 font-bold"
                                onClick={() => void api.removeAssignment(a.id).then(refresh)}
                              >
                                حذف
                              </button>
                            ) : null}
                          </div>
                        </li>
                      );
                    })
                  )}
                </ul>
              </div>
            ) : null}

            <div className="space-y-3" data-testid="permission-matrix">
              {grouped.map(([module, items]) => {
                const open = expanded[module] ?? true;
                return (
                  <div key={module} className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
                    <button
                      type="button"
                      className="w-full flex items-center justify-between px-4 py-3 hover:bg-gray-50"
                      onClick={() => setExpanded((s) => ({ ...s, [module]: !open }))}
                    >
                      <span className="font-black text-sm">{getModuleLabel(module, locale)}</span>
                      <span className="flex items-center gap-2">
                        {canManage ? (
                          <>
                            <span
                              role="button"
                              tabIndex={0}
                              className="text-[11px] text-[#2563EB] font-bold"
                              onClick={(e) => {
                                e.stopPropagation();
                                selectAllGroup(module, items);
                              }}
                            >
                              انتخاب همه
                            </span>
                            <span
                              role="button"
                              tabIndex={0}
                              className="text-[11px] text-gray-500 font-bold"
                              onClick={(e) => {
                                e.stopPropagation();
                                clearGroup(items);
                              }}
                            >
                              پاک کردن
                            </span>
                          </>
                        ) : null}
                        {open ? <ChevronDown className="w-4 h-4" /> : <ChevronLeft className="w-4 h-4" />}
                      </span>
                    </button>
                    {open ? (
                      <ul className="divide-y divide-gray-50">
                        {items.map((p) => {
                          const on = enabledSet.has(p.permissionId);
                          const blocked = Boolean(p.disabledByCeiling || p.platformOnly);
                          const grant = grantByPermission.get(p.permissionId);
                          const showScope = on && supportsScopedEditor(p);
                          const label = getPermissionLabel(p.permissionId, locale);
                          return (
                            <li key={p.permissionId} className={`px-4 py-3 ${blocked ? "opacity-60" : ""}`}>
                              <div className="flex items-start gap-3">
                                <input
                                  type="checkbox"
                                  className="mt-1"
                                  checked={on}
                                  disabled={!canManage || blocked || selectedRole?.isSystem}
                                  onChange={() => togglePermission(p)}
                                />
                                <div className="flex-1 min-w-0">
                                  <div className="flex flex-wrap items-center gap-2">
                                    <span className="text-sm font-bold">{label.title}</span>
                                    {p.disabledByCeiling ? (
                                      <span className="text-[10px] rounded-full bg-gray-100 text-gray-600 px-2 py-0.5 font-bold">
                                        غیرفعال توسط سقف پلتفرم
                                      </span>
                                    ) : null}
                                    {p.platformOnly ? (
                                      <span className="text-[10px] rounded-full bg-rose-50 text-rose-700 px-2 py-0.5 font-bold">
                                        فقط پلتفرم
                                      </span>
                                    ) : null}
                                    {grant && grant.scopeKind !== 1 && grant.scopeResourceId ? (
                                      <span className="text-[10px] rounded-full bg-[#2563EB]/10 text-[#2563EB] px-2 py-0.5 font-bold">
                                        {formatScopeLabel(grant.scopeKind, grant.scopeDisplayName, grant.scopeResourceId)}
                                      </span>
                                    ) : null}
                                  </div>
                                  <p className="text-xs text-gray-500 mt-1">{label.description}</p>
                                  {showScope ? (
                                    <ScopeEditor
                                      permissionId={p.permissionId}
                                      scopeKind={grant?.scopeKind ?? 1}
                                      scopeResourceId={grant?.scopeResourceId ?? null}
                                      scopeDisplayName={grant?.scopeDisplayName ?? null}
                                      disabled={blocked || Boolean(selectedRole?.isSystem)}
                                      canManage={canManage && Boolean(selectedRole?.isMutable)}
                                      loadResources={loadResources}
                                      onChange={(next) => updateGrantScope(p.permissionId, next)}
                                    />
                                  ) : null}
                                </div>
                              </div>
                            </li>
                          );
                        })}
                      </ul>
                    ) : null}
                  </div>
                );
              })}
            </div>
          </section>
        </div>
      ) : null}

      {tab === "users" ? (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <section className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5" data-testid="users-overview">
            <div className="flex flex-wrap items-center justify-between gap-2 mb-3">
              <h2 className="font-black text-sm">کاربران و نقش‌های تخصیص‌یافته</h2>
              <div className="relative min-w-[10rem] flex-1 max-w-xs">
                <Search className="w-3.5 h-3.5 absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  className="w-full rounded-xl border border-gray-200 pr-8 pl-3 py-1.5 text-xs"
                  placeholder="جستجوی کاربر…"
                  value={userFilter}
                  onChange={(e) => setUserFilter(e.target.value)}
                />
              </div>
            </div>
            {canManage ? (
              <div className="flex flex-col gap-2 mb-4 p-3 rounded-xl bg-gray-50 border border-gray-100">
                <UserPicker api={api} selected={assignPick} onSelect={setAssignPick} />
                <div className="flex flex-col sm:flex-row gap-2">
                  <select
                    className="flex-1 rounded-xl border border-gray-200 px-3 py-2 text-sm"
                    value={usersAssignRoleId}
                    onChange={(e) => setUsersAssignRoleId(e.target.value)}
                  >
                    {roles.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name}
                      </option>
                    ))}
                  </select>
                  <button
                    type="button"
                    className="rounded-xl bg-[#2563EB] text-white px-4 py-2 text-sm font-bold disabled:opacity-40"
                    disabled={!assignPick || !usersAssignRoleId}
                    onClick={() =>
                      void (async () => {
                        if (!assignPick || !usersAssignRoleId) return;
                        await api.assignRole(assignPick.userId, usersAssignRoleId);
                        setAssignPick(null);
                        await refresh();
                      })()
                    }
                  >
                    تخصیص نقش
                  </button>
                </div>
              </div>
            ) : null}
            <ul className="divide-y divide-gray-50 max-h-[28rem] overflow-auto">
              {usersOverview.length === 0 ? (
                <li className="py-3 text-xs text-gray-400">کاربری یافت نشد.</li>
              ) : (
                usersOverview.map((u) => (
                  <li key={u.userId} className="py-3">
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0">
                        <p className="text-sm font-bold truncate">{userPrimaryLabel(u.hit)}</p>
                        {userSecondaryLabel(u.hit) ? (
                          <p className="text-[11px] text-gray-500 truncate mt-0.5">{userSecondaryLabel(u.hit)}</p>
                        ) : null}
                        <div className="flex flex-wrap gap-1.5 mt-2">
                          {u.roles.length === 0 ? (
                            <span className="text-[10px] text-gray-400">بدون نقش</span>
                          ) : (
                            u.roles.map((r) => (
                              <span
                                key={r.id}
                                className="inline-flex items-center gap-1 text-[10px] rounded-full bg-[#2563EB]/10 text-[#2563EB] px-2 py-0.5 font-bold"
                              >
                                {r.roleName}
                                {canManage ? (
                                  <button
                                    type="button"
                                    className="hover:text-red-600"
                                    aria-label={`حذف نقش ${r.roleName}`}
                                    onClick={() => void api.removeAssignment(r.id).then(refresh)}
                                  >
                                    <X className="w-3 h-3" />
                                  </button>
                                ) : null}
                              </span>
                            ))
                          )}
                        </div>
                      </div>
                      <button
                        type="button"
                        className="shrink-0 text-[11px] text-[#2563EB] font-bold"
                        onClick={() => void loadEffectivePreview(u.userId)}
                      >
                        دسترسی مؤثر
                      </button>
                    </div>
                  </li>
                ))
              )}
            </ul>
          </section>

          <section className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5" data-testid="effective-access">
            <h2 className="font-black text-sm mb-3">پیش‌نمایش دسترسی مؤثر</h2>
            {!effectiveUserId ? (
              <p className="text-xs text-gray-400">یک کاربر را انتخاب کنید تا «چه کاری / کجا» نشان داده شود.</p>
            ) : null}
            {effective ? (
              <div className="space-y-3">
                <div>
                  <p className="text-sm font-bold">
                    {userPrimaryLabel(hitByUserId.get(effective.userId))}
                  </p>
                  <p className="text-xs text-gray-500 mt-1">
                    نقش‌ها:{" "}
                    {effective.roleCodes.length
                      ? effective.roleCodes.map((c) => roleNameByCode.get(c) ?? c).join("، ")
                      : "—"}
                  </p>
                </div>
                <ul className="space-y-2 max-h-80 overflow-auto">
                  {effective.permissions.map((p) => {
                    const label = getPermissionLabel(p.permissionId, locale);
                    const roleSources = p.inheritedViaRoleCodes
                      .map((c) => roleNameByCode.get(c) ?? c)
                      .join("، ");
                    return (
                      <li
                        key={`${p.permissionId}-${p.scopeKind}-${p.scopeResourceId}`}
                        className="rounded-xl bg-gray-50 px-3 py-2"
                      >
                        <p className="text-sm font-bold">{label.title}</p>
                        <p className="text-[11px] text-gray-500 mt-1">
                          نقش: {roleSources || "—"}
                          {" · "}
                          محدوده: {formatScopeLabel(p.scopeKind, p.scopeDisplayName, p.scopeResourceId)}
                        </p>
                        {p.deniedByCeiling ? (
                          <p className="text-[10px] text-amber-700 font-bold mt-1">محدود توسط سقف تفویض</p>
                        ) : null}
                      </li>
                    );
                  })}
                </ul>
              </div>
            ) : effectiveUserId ? (
              <p className="text-xs text-gray-400">در حال بارگذاری…</p>
            ) : null}
          </section>
        </div>
      ) : null}

      {tab === "ceiling" && api.getCeiling && api.setCeiling ? (
        <section className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
          <h2 className="font-black text-sm mb-3">سقف مجوز قابل تفویض فروشنده</h2>
          <ul className="divide-y divide-gray-50">
            {ceiling.map((c) => {
              const def = catalog.find((p) => p.permissionId === c.permissionId);
              const scopeKind = c.scopeKind ?? 1;
              const showScope = c.enabled && def && supportsScopedEditor(def);
              const rowKey = `${c.permissionId}-${scopeKind}-${c.scopeResourceId ?? ""}`;
              const label = getPermissionLabel(c.permissionId, locale);
              return (
                <li key={rowKey} className="py-3">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-bold">{label.title}</p>
                      <p className="text-[11px] text-gray-500">
                        {getModuleLabel(c.module, locale)}
                        {c.enabled && scopeKind !== 1
                          ? ` · ${formatScopeLabel(scopeKind, c.scopeDisplayName, c.scopeResourceId ?? null)}`
                          : ""}
                      </p>
                    </div>
                    <input
                      type="checkbox"
                      checked={c.enabled}
                      disabled={!canManage}
                      onChange={(e) =>
                        setCeiling((prev) =>
                          prev.map((row) =>
                            row.permissionId === c.permissionId &&
                            (row.scopeKind ?? 1) === scopeKind &&
                            (row.scopeResourceId ?? null) === (c.scopeResourceId ?? null)
                              ? {
                                  ...row,
                                  enabled: e.target.checked,
                                  scopeKind: e.target.checked ? (row.scopeKind ?? 1) : 1,
                                  scopeResourceId: e.target.checked ? row.scopeResourceId ?? null : null,
                                  scopeDisplayName: e.target.checked ? row.scopeDisplayName ?? null : null,
                                }
                              : row,
                          ),
                        )
                      }
                    />
                  </div>
                  {showScope ? (
                    <ScopeEditor
                      permissionId={c.permissionId}
                      scopeKind={scopeKind}
                      scopeResourceId={c.scopeResourceId ?? null}
                      scopeDisplayName={c.scopeDisplayName ?? null}
                      canManage={canManage}
                      loadResources={loadResources}
                      onChange={(next) =>
                        setCeiling((prev) =>
                          prev.map((row) =>
                            row.permissionId === c.permissionId &&
                            (row.scopeKind ?? 1) === scopeKind &&
                            (row.scopeResourceId ?? null) === (c.scopeResourceId ?? null)
                              ? {
                                  ...row,
                                  scopeKind: next.scopeKind,
                                  scopeResourceId: next.scopeKind === 1 ? null : next.scopeResourceId,
                                  scopeDisplayName: next.scopeKind === 1 ? null : (next.scopeDisplayName ?? null),
                                }
                              : row,
                          ),
                        )
                      }
                    />
                  ) : null}
                </li>
              );
            })}
          </ul>
          {canManage ? (
            <button
              type="button"
              className="mt-4 rounded-xl bg-[#2563EB] text-white px-4 py-2 text-sm font-bold"
              onClick={() => void api.setCeiling?.(ceiling).then(refresh)}
            >
              ذخیره سقف
            </button>
          ) : null}
        </section>
      ) : null}

      {dirty ? (
        <div className="fixed bottom-4 left-4 right-4 md:left-auto md:right-6 md:w-auto z-40 rounded-2xl bg-amber-50 border border-amber-200 px-4 py-3 text-sm font-bold text-amber-800 shadow-lg">
          تغییرات ذخیره‌نشده دارید
          <button type="button" className="mr-3 text-[#2563EB]" onClick={() => void savePermissions()}>
            ذخیره
          </button>
        </div>
      ) : null}
    </div>
  );
}
