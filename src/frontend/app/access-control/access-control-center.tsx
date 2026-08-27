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
} from "lucide-react";

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
  enabled: boolean;
};

export type AssignmentRow = {
  id: string;
  userId: string;
  roleId: string;
  roleName: string;
  roleCode: string;
};

export type EffectiveAccess = {
  userId: string;
  roleCodes: string[];
  permissions: Array<{
    permissionId: string;
    module: string;
    scopeKind: number;
    scopeResourceId: string | null;
    inheritedViaRoleCodes: string[];
    deniedByCeiling: boolean;
  }>;
};

export type CeilingEntry = {
  permissionId: string;
  enabled: boolean;
  delegable: boolean;
  module: string;
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
  getEffective: (userId: string) => Promise<EffectiveAccess>;
  getCeiling?: () => Promise<CeilingEntry[]>;
  setCeiling?: (entries: CeilingEntry[]) => Promise<void>;
  bootstrap?: () => Promise<void>;
};

type TabId = "roles" | "users" | "ceiling";

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
  const [effectiveUserId, setEffectiveUserId] = useState("");
  const [effective, setEffective] = useState<EffectiveAccess | null>(null);
  const [assignUserId, setAssignUserId] = useState("");
  const [newRoleName, setNewRoleName] = useState("");
  const [newRoleCode, setNewRoleCode] = useState("");

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      if (api.bootstrap) {
        await api.bootstrap().catch(() => undefined);
      }
      const [c, r, a] = await Promise.all([api.listCatalog(), api.listRoles(), api.listAssignments()]);
      setCatalog(c);
      setRoles(r);
      setAssignments(a);
      if (api.getCeiling) {
        setCeiling(await api.getCeiling());
      }
      if (!selectedRoleId && r[0]) {
        setSelectedRoleId(r[0].id);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "خطا در بارگذاری");
    } finally {
      setLoading(false);
    }
  }, [api, selectedRoleId]);

  useEffect(() => {
    void refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!selectedRoleId) return;
    void api.getRolePermissions(selectedRoleId).then((g) => {
      setGrants(g);
      setDirty(false);
    });
  }, [api, selectedRoleId]);

  const modules = useMemo(() => {
    const set = new Set(catalog.map((c) => c.module));
    return ["all", ...Array.from(set).sort()];
  }, [catalog]);

  const enabledSet = useMemo(() => new Set(grants.filter((g) => g.enabled).map((g) => g.permissionId)), [grants]);

  const grouped = useMemo(() => {
    const q = search.trim().toLowerCase();
    const rows = catalog.filter((p) => {
      if (moduleFilter !== "all" && p.module !== moduleFilter) return false;
      if (selectedOnly && !enabledSet.has(p.permissionId)) return false;
      if (!q) return true;
      return (
        p.permissionId.toLowerCase().includes(q) ||
        p.module.toLowerCase().includes(q) ||
        p.displayNameKey.toLowerCase().includes(q)
      );
    });
    const map = new Map<string, PermissionDef[]>();
    for (const p of rows) {
      const list = map.get(p.module) ?? [];
      list.push(p);
      map.set(p.module, list);
    }
    return Array.from(map.entries());
  }, [catalog, search, moduleFilter, selectedOnly, enabledSet]);

  function togglePermission(p: PermissionDef) {
    if (!canManage) return;
    if (p.disabledByCeiling || p.platformOnly) return;
    setDirty(true);
    setGrants((prev) => {
      const exists = prev.find((g) => g.permissionId === p.permissionId && g.scopeKind === 1);
      if (exists?.enabled) {
        return prev.filter((g) => !(g.permissionId === p.permissionId && g.scopeKind === 1));
      }
      return [
        ...prev.filter((g) => !(g.permissionId === p.permissionId && g.scopeKind === 1)),
        { permissionId: p.permissionId, scopeKind: 1, scopeResourceId: null, enabled: true },
      ];
    });
  }

  function selectAllGroup(module: string, items: PermissionDef[]) {
    if (!canManage) return;
    setDirty(true);
    setGrants((prev) => {
      const next = prev.filter((g) => !items.some((i) => i.permissionId === g.permissionId && g.scopeKind === 1));
      for (const p of items) {
        if (p.disabledByCeiling || p.platformOnly) continue;
        next.push({ permissionId: p.permissionId, scopeKind: 1, scopeResourceId: null, enabled: true });
      }
      return next;
    });
  }

  function clearGroup(items: PermissionDef[]) {
    if (!canManage) return;
    setDirty(true);
    setGrants((prev) => prev.filter((g) => !items.some((i) => i.permissionId === g.permissionId)));
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
    <main className="space-y-6" data-testid="access-control-center" data-mode={mode}>
      <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 md:p-6">
        <div className="flex items-center gap-3">
          <span className="w-10 h-10 bg-[#2563EB]/10 text-[#2563EB] rounded-xl flex items-center justify-center">
            <Shield className="w-5 h-5" />
          </span>
          <div>
            <h1 className="font-black text-lg">{title}</h1>
            <p className="text-xs text-gray-500 mt-1">
              نقش، مجوز، تخصیص و پیش‌نمایش دسترسی — بدون نمایش tupleهای SpiceDB
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
                    <p className="text-xs text-gray-500 mt-1">{role.code}</p>
                    <p className="text-[11px] text-gray-400 mt-1">
                      {role.permissionCount} مجوز · {role.assignmentCount} کاربر
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
                  placeholder="کد (مثلاً mobile-ops)"
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
                  <p className="text-xs text-gray-500 mt-1">{selectedRole?.description || selectedRole?.code}</p>
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
                      {m === "all" ? "همه ماژول‌ها" : m}
                    </option>
                  ))}
                </select>
                <label className="inline-flex items-center gap-2 rounded-xl border border-gray-200 px-3 py-2 text-xs font-bold">
                  <input type="checkbox" checked={selectedOnly} onChange={(e) => setSelectedOnly(e.target.checked)} />
                  فقط انتخاب‌شده
                </label>
              </div>
            </div>

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
                      <span className="font-black text-sm">{module}</span>
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
                          return (
                            <li key={p.permissionId} className={`px-4 py-3 flex items-start gap-3 ${blocked ? "opacity-60" : ""}`}>
                              <input
                                type="checkbox"
                                className="mt-1"
                                checked={on}
                                disabled={!canManage || blocked || selectedRole?.isSystem}
                                onChange={() => togglePermission(p)}
                              />
                              <div className="flex-1">
                                <div className="flex flex-wrap items-center gap-2">
                                  <span className="text-sm font-bold">{p.permissionId}</span>
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
                                </div>
                                <p className="text-xs text-gray-500 mt-1">{p.descriptionKey}</p>
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
          <section className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <h2 className="font-black text-sm mb-3">تخصیص نقش</h2>
            {canManage ? (
              <div className="flex flex-col sm:flex-row gap-2 mb-4">
                <input
                  className="flex-1 rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  placeholder="UserId (GUID)"
                  value={assignUserId}
                  onChange={(e) => setAssignUserId(e.target.value)}
                />
                <select
                  className="rounded-xl border border-gray-200 px-3 py-2 text-sm"
                  value={selectedRoleId ?? ""}
                  onChange={(e) => setSelectedRoleId(e.target.value)}
                >
                  {roles.map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.name}
                    </option>
                  ))}
                </select>
                <button
                  type="button"
                  className="rounded-xl bg-[#2563EB] text-white px-4 py-2 text-sm font-bold"
                  onClick={() =>
                    void (async () => {
                      if (!selectedRoleId || !assignUserId) return;
                      await api.assignRole(assignUserId.trim(), selectedRoleId);
                      setAssignUserId("");
                      await refresh();
                    })()
                  }
                >
                  تخصیص
                </button>
              </div>
            ) : null}
            <ul className="divide-y divide-gray-50">
              {assignments.map((a) => (
                <li key={a.id} className="py-3 flex items-center justify-between gap-2">
                  <div>
                    <p className="text-sm font-bold font-mono">{a.userId}</p>
                    <p className="text-xs text-gray-500 mt-1">{a.roleName}</p>
                  </div>
                  {canManage ? (
                    <button
                      type="button"
                      className="text-xs text-red-600 font-bold"
                      onClick={() => void api.removeAssignment(a.id).then(refresh)}
                    >
                      حذف
                    </button>
                  ) : null}
                </li>
              ))}
            </ul>
          </section>

          <section className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
            <h2 className="font-black text-sm mb-3">پیش‌نمایش دسترسی مؤثر</h2>
            <div className="flex gap-2 mb-4">
              <input
                className="flex-1 rounded-xl border border-gray-200 px-3 py-2 text-sm"
                placeholder="UserId"
                value={effectiveUserId}
                onChange={(e) => setEffectiveUserId(e.target.value)}
              />
              <button
                type="button"
                className="rounded-xl border border-gray-200 px-4 py-2 text-sm font-bold hover:bg-gray-50"
                onClick={() =>
                  void api.getEffective(effectiveUserId.trim()).then(setEffective)
                }
              >
                نمایش
              </button>
            </div>
            {effective ? (
              <div className="space-y-3">
                <p className="text-xs text-gray-500">نقش‌ها: {effective.roleCodes.join("، ") || "—"}</p>
                <ul className="space-y-2 max-h-80 overflow-auto">
                  {effective.permissions.map((p) => (
                    <li key={`${p.permissionId}-${p.scopeKind}-${p.scopeResourceId}`} className="rounded-xl bg-gray-50 px-3 py-2">
                      <p className="text-sm font-bold">{p.permissionId}</p>
                      <p className="text-[11px] text-gray-500 mt-1">
                        {p.module}
                        {p.scopeResourceId ? ` · scope ${p.scopeResourceId}` : " · کل محدوده"}
                        {" · از "}
                        {p.inheritedViaRoleCodes.join("، ")}
                      </p>
                    </li>
                  ))}
                </ul>
              </div>
            ) : (
              <p className="text-xs text-gray-400">کاربر را انتخاب کنید تا «چه کاری / کجا» نشان داده شود.</p>
            )}
          </section>
        </div>
      ) : null}

      {tab === "ceiling" && api.getCeiling && api.setCeiling ? (
        <section className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
          <h2 className="font-black text-sm mb-3">سقف مجوز قابل تفویض فروشنده</h2>
          <ul className="divide-y divide-gray-50">
            {ceiling.map((c) => (
              <li key={c.permissionId} className="py-2 flex items-center justify-between">
                <div>
                  <p className="text-sm font-bold">{c.permissionId}</p>
                  <p className="text-[11px] text-gray-500">{c.module}</p>
                </div>
                <input
                  type="checkbox"
                  checked={c.enabled}
                  disabled={!canManage}
                  onChange={(e) =>
                    setCeiling((prev) =>
                      prev.map((row) => (row.permissionId === c.permissionId ? { ...row, enabled: e.target.checked } : row)),
                    )
                  }
                />
              </li>
            ))}
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
    </main>
  );
}
