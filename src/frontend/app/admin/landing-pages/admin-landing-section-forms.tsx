"use client";

import { useEffect, useMemo, useState } from "react";
import { AdminSearchableCombobox } from "../admin-searchable-combobox";
import { loadCategoryTree, type CategoryTreeNodeDto } from "../catalog-category-api";
import { listAdminBrandOptions, queryAdminProductGrid } from "../host-client";
import { bannerSlotCountForVariant, PRODUCT_SOURCE_CHOICES } from "./landing-section-catalog.ts";
import { listAdminMenus } from "../menus/admin-menus-api.ts";

function asStringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.map((item) => String(item)).filter(Boolean);
}

function flattenCategories(nodes: CategoryTreeNodeDto[]): { value: string; label: string }[] {
  return nodes
    .filter((node) => node.status !== "Archived")
    .map((node) => ({ value: node.id, label: node.name }));
}

type StoryItem = { imageUrl?: string; title?: string; href?: string; enabled?: boolean };
type BannerItem = { imageUrl?: string; href?: string; title?: string; text?: string; ctaLabel?: string };

function asStoryItems(value: unknown): StoryItem[] {
  if (!Array.isArray(value)) return [];
  return value.map((item) => {
    if (!item || typeof item !== "object") return { imageUrl: "", title: "", href: "", enabled: true };
    const row = item as Record<string, unknown>;
    return {
      imageUrl: typeof row.imageUrl === "string" ? row.imageUrl : "",
      title: typeof row.title === "string" ? row.title : "",
      href: typeof row.href === "string" ? row.href : "",
      enabled: row.enabled !== false,
    };
  });
}

function asBannerItems(value: unknown): BannerItem[] {
  if (!Array.isArray(value)) return [];
  return value.map((item) => {
    if (!item || typeof item !== "object") return { imageUrl: "", href: "", title: "" };
    const row = item as Record<string, unknown>;
    return {
      imageUrl: typeof row.imageUrl === "string" ? row.imageUrl : "",
      href: typeof row.href === "string" ? row.href : "",
      title: typeof row.title === "string" ? row.title : "",
      text: typeof row.text === "string" ? row.text : "",
      ctaLabel: typeof row.ctaLabel === "string" ? row.ctaLabel : "",
    };
  });
}

export function LandingSectionForm({
  type,
  value,
  onChange,
}: {
  type: string;
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
}) {
  const title = typeof value.title === "string" ? value.title : "";
  const set = (patch: Record<string, unknown>) => onChange({ ...value, ...patch });
  const variantKey = typeof value.variantKey === "string" ? value.variantKey : undefined;
  const showBannerSlots = type === "BannerShowcase" || (variantKey?.startsWith("banner.") ?? false);
  const showStoryItems = type === "StoryRail" || (variantKey?.startsWith("story.") ?? false);

  if (showBannerSlots) {
    const slots = Math.max(1, bannerSlotCountForVariant(variantKey || "banner.single"));
    const current = asBannerItems(value.items);
    const items = Array.from({ length: slots }, (_, index) => current[index] ?? { imageUrl: "", href: "", title: "" });
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <div className="space-y-3" data-testid="banner-slot-editor">
          <p className="text-sm font-bold">جایگاه‌های بنر ({slots.toLocaleString("fa-IR")} مورد)</p>
          {items.map((item, index) => (
            <div key={index} className="rounded-xl border border-border p-3 space-y-2">
              <p className="text-xs font-bold text-muted">جایگاه {(index + 1).toLocaleString("fa-IR")}</p>
              <TextField
                label="آدرس تصویر"
                value={item.imageUrl ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, imageUrl: next };
                  set({ items: nextItems });
                }}
                placeholder="https://…"
              />
              <TextField
                label="پیوند مقصد"
                value={item.href ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, href: next };
                  set({ items: nextItems });
                }}
                placeholder="/offers"
              />
              <TextField
                label="عنوان کوتاه"
                value={item.title ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, title: next };
                  set({ items: nextItems });
                }}
              />
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (showStoryItems) {
    const items = asStoryItems(value.items);
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <div className="space-y-3" data-testid="story-items-editor">
          <div className="flex items-center justify-between">
            <p className="text-sm font-bold">فهرست استوری</p>
            <button
              type="button"
              className="rounded-lg border px-3 py-1 text-xs font-bold"
              onClick={() => set({ items: [...items, { imageUrl: "", title: "", href: "/products", enabled: true }] })}
            >
              افزودن استوری
            </button>
          </div>
          {items.length === 0 ? <p className="text-xs text-muted">هنوز استوری اضافه نشده است.</p> : null}
          {items.map((item, index) => (
            <div key={index} className="rounded-xl border border-border p-3 space-y-2">
              <TextField
                label="آدرس تصویر"
                value={item.imageUrl ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, imageUrl: next };
                  set({ items: nextItems });
                }}
              />
              <TextField
                label="عنوان"
                value={item.title ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, title: next };
                  set({ items: nextItems });
                }}
              />
              <TextField
                label="پیوند مقصد"
                value={item.href ?? ""}
                onChange={(next) => {
                  const nextItems = items.slice();
                  nextItems[index] = { ...item, href: next };
                  set({ items: nextItems });
                }}
              />
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={item.enabled !== false}
                  onChange={(event) => {
                    const nextItems = items.slice();
                    nextItems[index] = { ...item, enabled: event.target.checked };
                    set({ items: nextItems });
                  }}
                />
                فعال
              </label>
              <button
                type="button"
                className="text-xs text-red-600"
                onClick={() => set({ items: items.filter((_, i) => i !== index) })}
              >
                حذف
              </button>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (type === "Hero" || type === "PromoBanner") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} required />
        {type === "Hero" ? (
          <TextField
            label="توضیح کوتاه"
            value={typeof value.subtitle === "string" ? value.subtitle : ""}
            onChange={(next) => set({ subtitle: next })}
          />
        ) : null}
        <TextField
          label="پیوند دکمه"
          value={typeof value.href === "string" ? value.href : ""}
          onChange={(next) => set({ href: next })}
          placeholder="/products"
        />
      </div>
    );
  }

  if (type === "RichText") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <label className="block text-sm">
          <span className="mb-1 block font-bold">متن</span>
          <textarea
            className="min-h-32 w-full rounded-xl border border-border bg-surface px-3 py-2"
            value={typeof value.text === "string" ? value.text : ""}
            onChange={(event) => set({ text: event.target.value })}
          />
        </label>
      </div>
    );
  }

  if (type === "Reviews" || type === "ArticleList") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        {type === "ArticleList" ? (
          <TakeField value={typeof value.take === "number" ? value.take : 6} onChange={(take) => set({ take, source: "Latest" })} />
        ) : null}
        <p className="text-xs text-muted">
          {type === "ArticleList" ? "فقط آخرین مطالب منتشرشده نمایش داده می‌شود." : "نظرهای تأییدشدهٔ فروشگاه نمایش داده می‌شود."}
        </p>
      </div>
    );
  }

  if (type === "CategoryGrid") {
    const selected = asStringArray(value.categoryIds ?? value.ids);
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <EntityMultiPicker kind="category" selected={selected} onChange={(categoryIds) => set({ categoryIds })} />
      </div>
    );
  }

  if (type === "BrandStrip") {
    const selected = asStringArray(value.brandIds ?? value.ids);
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <EntityMultiPicker kind="brand" selected={selected} onChange={(brandIds) => set({ brandIds })} />
      </div>
    );
  }

  if (type === "ProductCollection") {
    const source = typeof value.source === "string" ? value.source : "Newest";
    return (
      <div className="space-y-3" data-testid="product-section-editor">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <label className="block text-sm">
          <span className="mb-1 block font-bold">منبع کالا</span>
          <select
            className="w-full rounded-xl border border-border bg-surface px-3 py-2"
            value={source}
            onChange={(event) => set({ source: event.target.value })}
          >
            {PRODUCT_SOURCE_CHOICES.map((item) => (
              <option key={item.value} value={item.value}>{item.label}</option>
            ))}
          </select>
        </label>
        <TakeField value={typeof value.take === "number" ? value.take : 8} onChange={(take) => set({ take })} />
        {source === "Category" ? (
          <EntitySinglePicker
            kind="category"
            value={typeof value.categoryId === "string" ? value.categoryId : null}
            onChange={(categoryId) => set({ categoryId })}
          />
        ) : null}
        {source === "Brand" ? (
          <EntitySinglePicker
            kind="brand"
            value={typeof value.brandId === "string" ? value.brandId : null}
            onChange={(brandId) => set({ brandId })}
          />
        ) : null}
        {source === "Manual" ? (
          <EntityMultiPicker
            kind="product"
            selected={asStringArray(value.productIds)}
            onChange={(productIds) => set({ productIds })}
          />
        ) : null}
      </div>
    );
  }

  if (type === "NavigationMenu") {
    return (
      <div className="space-y-3" data-testid="landing-section-form">
        <TextField label="عنوان" value={title} onChange={(next) => set({ title: next })} />
        <MenuPicker value={typeof value.menuId === "string" ? value.menuId : null} onChange={(menuId) => set({ menuId })} />
      </div>
    );
  }

  return <p className="text-sm text-muted">این بخش قابل ویرایش نیست.</p>;
}

function MenuPicker({ value, onChange }: { value: string | null; onChange: (next: string | null) => void }) {
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let cancelled = false;
    void listAdminMenus().then((result) => {
      if (cancelled) return;
      setLoading(false);
      if (!result.ok) return;
      setOptions(result.data.filter((row) => row.isEnabled).map((row) => ({ value: row.menuId, label: row.title })));
    });
    return () => {
      cancelled = true;
    };
  }, []);
  return (
    <div data-testid="landing-menu-picker">
      <p className="mb-1 text-sm font-bold">منو</p>
      {loading ? <p className="text-xs text-muted">در حال بارگذاری…</p> : null}
      {!loading && options.length === 0 ? <p className="text-xs text-muted">منوی فعالی یافت نشد.</p> : null}
      <AdminSearchableCombobox
        value={value}
        options={options}
        onChange={onChange}
        placeholder="جستجوی منو…"
        testId="landing-menu-combobox"
      />
    </div>
  );
}

function TextField({
  label,
  value,
  onChange,
  required,
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (next: string) => void;
  required?: boolean;
  placeholder?: string;
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-bold">{label}</span>
      <input
        required={required}
        className="w-full rounded-xl border border-border bg-surface px-3 py-2"
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}

function TakeField({ value, onChange }: { value: number; onChange: (next: number) => void }) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-bold">تعداد نمایش</span>
      <input
        type="number"
        min={1}
        max={24}
        className="w-full rounded-xl border border-border bg-surface px-3 py-2"
        value={value}
        onChange={(event) => onChange(Math.min(24, Math.max(1, Number(event.target.value) || 1)))}
      />
    </label>
  );
}

function EntitySinglePicker({
  kind,
  value,
  onChange,
}: {
  kind: "category" | "brand";
  value: string | null;
  onChange: (next: string | null) => void;
}) {
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    const load = kind === "category"
      ? loadCategoryTree("fa-IR").then((result) => {
        if (result.state !== "ok" || !result.data) return [];
        return flattenCategories(result.data);
      })
      : listAdminBrandOptions().then((result) => result.ok ? result.items.map((item) => ({ value: item.brandId, label: item.name })) : []);
    void load.then((rows) => {
      if (cancelled) return;
      setOptions(rows);
      setLoading(false);
    });
    return () => {
      cancelled = true;
    };
  }, [kind]);

  return (
    <div>
      <p className="mb-1 text-sm font-bold">{kind === "category" ? "دسته" : "برند"}</p>
      {loading ? <p className="text-xs text-muted">در حال بارگذاری…</p> : null}
      <AdminSearchableCombobox
        value={value}
        options={options}
        onChange={onChange}
        placeholder={kind === "category" ? "جستجوی دسته…" : "جستجوی برند…"}
        testId={`landing-${kind}-picker`}
      />
    </div>
  );
}

function EntityMultiPicker({
  kind,
  selected,
  onChange,
}: {
  kind: "category" | "brand" | "product";
  selected: string[];
  onChange: (next: string[]) => void;
}) {
  const [query, setQuery] = useState("");
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);
  const [labels, setLabels] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    const run = async () => {
      if (kind === "category") {
        const result = await loadCategoryTree("fa-IR", query);
        if (result.state !== "ok" || !result.data) throw new Error(result.message ?? "بارگذاری دسته‌ها ناموفق بود");
        return flattenCategories(result.data);
      }
      if (kind === "brand") {
        const result = await listAdminBrandOptions(query);
        if (!result.ok) throw new Error(result.message);
        return result.items.map((item) => ({ value: item.brandId, label: item.name }));
      }
      const result = await queryAdminProductGrid({
        page: 1,
        pageSize: 20,
        sorts: [],
        filters: {},
        search: query || undefined,
      });
      if (result.source === "error") throw new Error("فهرست کالا خوانده نشد");
      return result.page.rows.map((row) => ({ value: row.id, label: row.title || row.brandName || "کالا" }));
    };
    void run()
      .then((rows) => {
        if (cancelled) return;
        setOptions(rows);
        setLabels((current) => {
          const next = { ...current };
          for (const row of rows) next[row.value] = row.label;
          return next;
        });
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

  const selectedLabels = useMemo(
    () => selected.map((id) => ({ id, label: labels[id] ?? "مورد انتخاب‌شده" })),
    [labels, selected],
  );

  return (
    <div data-testid={`landing-${kind}-multi-picker`}>
      <p className="mb-1 text-sm font-bold">
        {kind === "product" ? "کالاها" : kind === "category" ? "دسته‌ها" : "برندها"}
      </p>
      <input
        className="mb-2 w-full rounded-xl border border-border bg-surface px-3 py-2 text-sm"
        placeholder="جستجو با نام…"
        value={query}
        onChange={(event) => setQuery(event.target.value)}
      />
      {loading ? <p className="text-xs text-muted">در حال جستجو…</p> : null}
      {error ? <p className="text-xs text-red-600">{error}</p> : null}
      {!loading && options.length === 0 ? <p className="text-xs text-muted">موردی یافت نشد.</p> : null}
      <ul className="mb-3 max-h-40 overflow-auto rounded-xl border border-border">
        {options.map((option) => {
          const checked = selected.includes(option.value);
          return (
            <li key={option.value}>
              <button
                type="button"
                className={`flex w-full items-center justify-between px-3 py-2 text-start text-sm ${checked ? "bg-emerald-50" : "hover:bg-slate-50"}`}
                onClick={() => onChange(checked ? selected.filter((id) => id !== option.value) : [...selected, option.value])}
              >
                <span>{option.label}</span>
                <span className="text-xs text-muted">{checked ? "انتخاب شده" : "افزودن"}</span>
              </button>
            </li>
          );
        })}
      </ul>
      {selectedLabels.length > 0 ? (
        <div className="flex flex-wrap gap-2">
          {selectedLabels.map((item, index) => (
            <span key={item.id} className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1 text-xs">
              {item.label}
              <button type="button" onClick={() => onChange(selected.filter((id) => id !== item.id))}>حذف</button>
              {index > 0 ? (
                <button type="button" onClick={() => {
                  const next = selected.slice();
                  [next[index - 1], next[index]] = [next[index]!, next[index - 1]!];
                  onChange(next);
                }}>بالا</button>
              ) : null}
            </span>
          ))}
        </div>
      ) : null}
    </div>
  );
}
