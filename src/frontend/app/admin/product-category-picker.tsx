"use client";

/**
 * انتخابگر دستهٔ محصول — مسیر انسانی، جستجوپذیر، بدون نمایش شناسهٔ خام.
 */

import { useEffect, useMemo, useRef, useState } from "react";
import { buildCategoryPath, type AppCategoryTreeNode } from "../../design-system";
import {
  loadCategoryTree,
  type CategoryTreeNodeDto,
} from "./catalog-category-api";

const API_LOCALE = "fa-IR";

function toTreeNodes(rows: CategoryTreeNodeDto[]): AppCategoryTreeNode[] {
  return rows.map((r) => ({
    id: r.id,
    parentId: r.parentId,
    name: r.name,
    slug: r.slug,
    status: r.status,
    sortOrder: r.sortOrder,
    isVisible: r.isVisible,
    hasChildren: r.hasChildren,
    productCount: r.productCount,
  }));
}

export function ProductCategoryPicker({
  value,
  onChange,
  label = "دسته",
  required = false,
  disabled = false,
  emptyLabel = "انتخاب دسته…",
}: {
  value: string | null;
  onChange: (next: string | null) => void;
  label?: string;
  required?: boolean;
  disabled?: boolean;
  emptyLabel?: string;
}) {
  const [nodes, setNodes] = useState<AppCategoryTreeNode[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    void loadCategoryTree(API_LOCALE).then((result) => {
      if (cancelled) return;
      setLoading(false);
      if (result.state !== "ok" || !result.data) {
        setLoadError(result.message ?? "بارگذاری دسته‌ها ناموفق بود");
        setNodes([]);
        return;
      }
      setLoadError(null);
      setNodes(toTreeNodes(result.data));
    });
    return () => {
      cancelled = true;
    };
  }, []);

  const options = useMemo(() => {
    return nodes.map((n) => {
      const path = buildCategoryPath(nodes, n.id).join(" › ");
      return { id: n.id, label: path || n.name };
    });
  }, [nodes]);

  const selected = options.find((o) => o.id === value) ?? null;
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return options;
    return options.filter((o) => o.label.toLowerCase().includes(q));
  }, [options, query]);

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  return (
    <div className="relative" ref={rootRef} data-testid="product-category-picker">
      <span className="block text-sm font-medium text-slate-700">
        {label}
        {required ? <span className="text-danger"> *</span> : null}
      </span>
      <button
        type="button"
        disabled={disabled || loading}
        className="mt-1 flex min-h-11 w-full items-center justify-between rounded-xl border border-gray-200 bg-white px-3 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => {
          if (disabled || loading) return;
          setOpen((v) => !v);
          setQuery("");
        }}
        data-testid="product-category-picker-trigger"
      >
        <span className="truncate text-start">
          {loading ? "در حال بارگذاری…" : (selected?.label ?? emptyLabel)}
        </span>
        <span className="ms-2 text-slate-400" aria-hidden>
          ▾
        </span>
      </button>
      {loadError ? <p className="mt-1 text-sm text-danger">{loadError}</p> : null}
      {open ? (
        <div
          className="absolute z-20 mt-1 max-h-64 w-full overflow-hidden rounded-xl border border-gray-200 bg-white shadow-lg"
          role="listbox"
          data-testid="product-category-picker-list"
        >
          <div className="border-b border-gray-100 p-2">
            <input
              className="min-h-10 w-full rounded-lg border border-gray-200 px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="جستجوی نام یا مسیر…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              autoFocus
              data-testid="product-category-picker-search"
              aria-label="جستجوی دسته"
            />
          </div>
          <ul className="max-h-48 overflow-y-auto py-1">
            {filtered.length === 0 ? (
              <li className="px-3 py-2 text-sm text-slate-400">موردی یافت نشد</li>
            ) : (
              filtered.map((opt) => {
                const active = opt.id === value;
                return (
                  <li key={opt.id}>
                    <button
                      type="button"
                      role="option"
                      aria-selected={active}
                      className={
                        active
                          ? "flex min-h-10 w-full px-3 py-2 text-start text-sm font-semibold text-[#2563EB] bg-blue-50"
                          : "flex min-h-10 w-full px-3 py-2 text-start text-sm text-slate-700 hover:bg-slate-50"
                      }
                      onClick={() => {
                        onChange(opt.id);
                        setOpen(false);
                        setQuery("");
                      }}
                      data-testid={`product-category-option-${opt.id}`}
                    >
                      {opt.label}
                    </button>
                  </li>
                );
              })
            )}
          </ul>
        </div>
      ) : null}
      <input type="hidden" value={value ?? ""} data-testid="product-category-picker-value" readOnly />
    </div>
  );
}
