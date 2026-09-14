"use client";

import { useEffect, useState } from "react";
import { AdminSearchableCombobox } from "../admin-searchable-combobox";
import { loadCategoryTree, type CategoryTreeNodeDto } from "../catalog-category-api";
import { listAdminBrandOptions, queryAdminProductGrid } from "../host-client";
import { listAdminLandingPages } from "../landing-pages/admin-landing-pages-api.ts";
import { isArticlePublished, queryAdminContentArticlesGrid } from "../../content/content-api.ts";
import { MENU_LINK_CHOICES } from "./admin-menu-destination.ts";

function flattenCategories(nodes: CategoryTreeNodeDto[]): { value: string; label: string }[] {
  return nodes.filter((node) => node.status !== "Archived").map((node) => ({ value: node.id, label: node.name }));
}

export function MenuDestinationFields({
  linkType,
  targetId,
  externalUrl,
  onLinkType,
  onTarget,
  onExternal,
}: {
  linkType: string;
  targetId: string | null;
  externalUrl: string | null;
  onLinkType: (next: string) => void;
  onTarget: (id: string | null, label: string | null) => void;
  onExternal: (next: string) => void;
}) {
  return (
    <div className="space-y-3">
      <label className="block text-sm">
        <span className="mb-1 block font-bold">نوع مقصد</span>
        <select
          className="w-full rounded-xl border border-border bg-surface px-3 py-2"
          value={linkType}
          onChange={(event) => onLinkType(event.target.value)}
          data-testid="menu-link-type"
        >
          {MENU_LINK_CHOICES.map((item) => (
            <option key={item.value} value={item.value}>{item.label}</option>
          ))}
        </select>
      </label>
      {linkType === "External" ? (
        <label className="block text-sm">
          <span className="mb-1 block font-bold">نشانی وب</span>
          <input
            className="w-full rounded-xl border border-border bg-surface px-3 py-2"
            dir="ltr"
            value={externalUrl ?? ""}
            onChange={(event) => onExternal(event.target.value)}
            placeholder="https://example.com"
            data-testid="menu-external-url"
          />
        </label>
      ) : null}
      {["LandingPage", "Product", "Category", "Brand", "Article"].includes(linkType) ? (
        <MenuEntityPicker kind={linkType} value={targetId} onChange={onTarget} />
      ) : null}
      {linkType === "Home" ? <p className="text-xs text-muted">به صفحهٔ اصلی فروشگاه می‌رود.</p> : null}
      {linkType === "Group" ? <p className="text-xs text-muted">فقط عنوان گروه است و پیوند ندارد.</p> : null}
    </div>
  );
}

function MenuEntityPicker({
  kind,
  value,
  onChange,
}: {
  kind: string;
  value: string | null;
  onChange: (id: string | null, label: string | null) => void;
}) {
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [query, setQuery] = useState("");

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    void (async () => {
      try {
        if (kind === "LandingPage") {
          const result = await listAdminLandingPages();
          if (!result.ok) throw new Error(result.message);
          return result.data
            .filter((page) => page.status === "Published")
            .filter((page) => !query || page.title.includes(query))
            .map((page) => ({ value: page.pageId, label: page.title }));
        }
        if (kind === "Category") {
          const result = await loadCategoryTree("fa-IR", query);
          if (result.state !== "ok" || !result.data) throw new Error(result.message ?? "دسته‌ها خوانده نشد");
          return flattenCategories(result.data);
        }
        if (kind === "Brand") {
          const result = await listAdminBrandOptions(query);
          if (!result.ok) throw new Error(result.message);
          return result.items.map((item) => ({ value: item.brandId, label: item.name }));
        }
        if (kind === "Product") {
          const result = await queryAdminProductGrid({
            page: 1,
            pageSize: 20,
            sorts: [],
            filters: {},
            search: query || undefined,
          });
          if (result.source === "error") throw new Error("فهرست کالا خوانده نشد");
          return result.page.rows.map((row) => ({ value: row.id, label: row.title || "کالا" }));
        }
        const result = await queryAdminContentArticlesGrid({
          page: 1,
          pageSize: 20,
          sorts: [],
          filters: {},
          search: query || undefined,
        });
        if (result.source === "error") throw new Error("فهرست مقاله خوانده نشد");
        return result.page.rows
          .filter((row) => isArticlePublished(row.status))
          .map((row) => ({ value: row.articleId || row.id, label: row.title || "مقاله" }));
      } catch (err) {
        throw err;
      }
    })()
      .then((rows) => {
        if (cancelled) return;
        setOptions(rows);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : "بارگذاری ناموفق بود");
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [kind, query]);

  const placeholder = kind === "LandingPage" ? "جستجوی صفحهٔ منتشرشده…"
    : kind === "Product" ? "جستجوی کالا…"
      : kind === "Category" ? "جستجوی دسته…"
        : kind === "Brand" ? "جستجوی برند…"
          : "جستجوی مقاله…";

  return (
    <div data-testid="menu-destination-picker">
      <p className="mb-1 text-sm font-bold">مقصد</p>
      <input
        className="mb-2 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
        placeholder={placeholder}
        value={query}
        onChange={(event) => setQuery(event.target.value)}
      />
      {loading ? <p className="text-xs text-muted">در حال بارگذاری…</p> : null}
      {error ? <p className="text-xs text-red-600">{error}</p> : null}
      {!loading && options.length === 0 ? <p className="text-xs text-muted">موردی یافت نشد.</p> : null}
      <AdminSearchableCombobox
        value={value}
        options={options}
        onChange={(next) => onChange(next, options.find((item) => item.value === next)?.label ?? null)}
        placeholder={placeholder}
        testId="menu-entity-combobox"
      />
    </div>
  );
}
