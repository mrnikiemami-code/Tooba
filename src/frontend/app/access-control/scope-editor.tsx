"use client";

/**
 * ویرایشگر محدودهٔ واقعی — لیست/جستجو از API مالک (Catalog).
 * زبان بصری Shopeiva settings/customers (کارت سفید، آبی #2563EB).
 */

import { useEffect, useState } from "react";
import { Search, X } from "lucide-react";

export type ScopeResourceItem = {
  id: string;
  parentId?: string | null;
  name: string;
  deferred?: boolean;
};

export type ScopeKindOption = {
  kind: number;
  label: string;
  live: boolean;
};

const SCOPE_OPTIONS: ScopeKindOption[] = [
  { kind: 1, label: "کل محدوده", live: true },
  { kind: 2, label: "دسته", live: true },
  { kind: 3, label: "محصول", live: true },
  { kind: 4, label: "برند", live: true },
  { kind: 5, label: "انبار", live: false },
  { kind: 6, label: "فروشگاه", live: false },
  { kind: 7, label: "قطعه سفارش", live: false },
];

/**
 * انتخاب scope واقعی برای یک مجوز.
 */
export function ScopeEditor({
  permissionId,
  scopeKind,
  scopeResourceId,
  scopeDisplayName,
  disabled,
  canManage,
  loadResources,
  onChange,
}: {
  permissionId: string;
  scopeKind: number;
  scopeResourceId: string | null;
  scopeDisplayName?: string | null;
  disabled?: boolean;
  canManage: boolean;
  loadResources: (kind: number, q: string) => Promise<{ deferred?: boolean; items: ScopeResourceItem[] }>;
  onChange: (next: { scopeKind: number; scopeResourceId: string | null; scopeDisplayName?: string | null }) => void;
}) {
  const [q, setQ] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [items, setItems] = useState<ScopeResourceItem[]>([]);
  const [deferred, setDeferred] = useState(false);

  useEffect(() => {
    if (scopeKind === 1 || disabled) {
      setItems([]);
      setDeferred(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    setError(null);
    void loadResources(scopeKind, q)
      .then((res) => {
        if (cancelled) return;
        setDeferred(Boolean(res.deferred));
        setItems(res.items);
      })
      .catch((e: unknown) => {
        if (cancelled) return;
        setError(e instanceof Error ? e.message : "خطا");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [scopeKind, q, loadResources, disabled]);

  const option = SCOPE_OPTIONS.find((o) => o.kind === scopeKind);

  return (
    <div className="mt-2 rounded-xl border border-gray-100 bg-gray-50/80 p-3 space-y-2" data-testid="scope-editor" data-permission={permissionId}>
      <div className="flex flex-wrap gap-2 items-center">
        <label className="text-[11px] font-bold text-gray-500">نوع محدوده</label>
        <select
          className="rounded-lg border border-gray-200 bg-white px-2 py-1 text-xs font-bold"
          disabled={!canManage || disabled}
          value={scopeKind}
          onChange={(e) => {
            const kind = Number(e.target.value);
            onChange({ scopeKind: kind, scopeResourceId: kind === 1 ? null : scopeResourceId, scopeDisplayName: kind === 1 ? null : scopeDisplayName });
          }}
        >
          {SCOPE_OPTIONS.map((o) => (
            <option key={o.kind} value={o.kind} disabled={!o.live}>
              {o.label}
              {!o.live ? " (به‌زودی)" : ""}
            </option>
          ))}
        </select>
        {scopeResourceId && scopeKind !== 1 ? (
          <span className="inline-flex items-center gap-1 rounded-full bg-[#2563EB]/10 text-[#2563EB] text-[11px] font-bold px-2 py-0.5">
            {scopeDisplayName || scopeResourceId.slice(0, 8)}
            {canManage && !disabled ? (
              <button
                type="button"
                className="hover:text-red-600"
                onClick={() => onChange({ scopeKind, scopeResourceId: null, scopeDisplayName: null })}
                aria-label="clear"
              >
                <X className="w-3 h-3" />
              </button>
            ) : null}
          </span>
        ) : null}
      </div>

      {scopeKind !== 1 && option?.live ? (
        <>
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute right-2 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              className="w-full rounded-lg border border-gray-200 bg-white pr-8 pl-2 py-1.5 text-xs"
              placeholder={`جستجوی ${option.label}…`}
              value={q}
              disabled={!canManage || disabled}
              onChange={(e) => setQ(e.target.value)}
            />
          </div>
          {loading ? <p className="text-[11px] text-gray-400">در حال بارگذاری…</p> : null}
          {error ? <p className="text-[11px] text-red-600">{error}</p> : null}
          {deferred ? <p className="text-[11px] text-amber-700 font-bold">این نوع محدوده هنوز منبع زنده ندارد.</p> : null}
          {!loading && !deferred && items.length === 0 ? (
            <p className="text-[11px] text-gray-400">موردی یافت نشد</p>
          ) : null}
          <ul className="max-h-36 overflow-auto divide-y divide-gray-100 rounded-lg bg-white border border-gray-100">
            {items.map((item) => (
              <li key={item.id}>
                <button
                  type="button"
                  disabled={!canManage || disabled}
                  className={`w-full text-right px-3 py-2 text-xs hover:bg-[#2563EB]/5 ${
                    scopeResourceId === item.id ? "bg-[#2563EB]/10 font-bold text-[#2563EB]" : ""
                  }`}
                  onClick={() => onChange({ scopeKind, scopeResourceId: item.id, scopeDisplayName: item.name })}
                >
                  {item.name}
                </button>
              </li>
            ))}
          </ul>
        </>
      ) : null}
    </div>
  );
}
