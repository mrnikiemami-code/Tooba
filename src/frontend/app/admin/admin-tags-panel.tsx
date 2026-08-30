"use client";

import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { toast } from "react-toastify";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import {
  TAG_HELPER_FA,
  createCatalogTag,
  filterUnassignedTags,
  listCatalogTags,
  type CatalogTag,
} from "./catalog-tag-api.ts";

type TagOwnerKind = "product" | "category";

/**
 * کارت فشردهٔ برچسب‌ها — searchable multi-select + ایجاد + chips قابل حذف.
 * تاکسونومی/کشف؛ نه کلیدواژهٔ SEO و نه textbox جداشده با ویرگول.
 */
export function AdminTagsPanel({
  ownerKind,
  ownerId,
  canEdit,
  loadAssigned,
  assignTag,
  removeTag,
  testIdPrefix = "admin-tags",
}: {
  ownerKind: TagOwnerKind;
  ownerId: string;
  canEdit: boolean;
  loadAssigned: (ownerId: string) => Promise<{
    state: string;
    data: CatalogTag[] | null;
    message?: string;
  }>;
  assignTag: (
    ownerId: string,
    tagId: string,
  ) => Promise<{ state: string; data: CatalogTag[] | null; message?: string }>;
  removeTag: (
    ownerId: string,
    tagId: string,
  ) => Promise<{ state: string; data: CatalogTag[] | null; message?: string }>;
  testIdPrefix?: string;
}) {
  const listId = useId();
  const rootRef = useRef<HTMLDivElement | null>(null);
  const [assigned, setAssigned] = useState<CatalogTag[]>([]);
  const [catalog, setCatalog] = useState<CatalogTag[]>([]);
  const [busy, setBusy] = useState(false);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [nameFa, setNameFa] = useState("");
  const [nameEn, setNameEn] = useState("");

  const refresh = useCallback(async () => {
    const [assignedResult, catalogResult] = await Promise.all([
      loadAssigned(ownerId),
      listCatalogTags("fa-IR", null),
    ]);
    if (assignedResult.state === "ok" && assignedResult.data) {
      setAssigned(assignedResult.data);
    }
    if (catalogResult.state === "ok" && catalogResult.data) {
      setCatalog(catalogResult.data);
    }
  }, [loadAssigned, ownerId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (!open) return;
    function onDoc(ev: MouseEvent) {
      if (!rootRef.current?.contains(ev.target as Node)) {
        setOpen(false);
        setQuery("");
      }
    }
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  const available = useMemo(() => {
    const base = filterUnassignedTags(catalog, assigned);
    const q = query.trim().toLowerCase();
    if (!q) return base;
    return base.filter(
      (t) =>
        t.name.toLowerCase().includes(q) || t.code.toLowerCase().includes(q),
    );
  }, [assigned, catalog, query]);

  async function handleAssign(tag: CatalogTag) {
    if (!canEdit || busy) return;
    setBusy(true);
    try {
      const result = await assignTag(ownerId, tag.tagId);
      if (result.state !== "ok" || !result.data) {
        toast.error(mapAdminErrorMessage(result.message) || "اختصاص برچسب ناموفق بود.");
        return;
      }
      setAssigned(result.data);
      setOpen(false);
      setQuery("");
      toast.success("برچسب اضافه شد.");
    } finally {
      setBusy(false);
    }
  }

  async function handleRemove(tagId: string) {
    if (!canEdit || busy) return;
    setBusy(true);
    try {
      const result = await removeTag(ownerId, tagId);
      if (result.state !== "ok" || !result.data) {
        toast.error(mapAdminErrorMessage(result.message) || "حذف برچسب ناموفق بود.");
        return;
      }
      setAssigned(result.data);
      toast.success("برچسب حذف شد.");
    } finally {
      setBusy(false);
    }
  }

  async function handleCreate() {
    if (!canEdit || busy) return;
    const fa = nameFa.trim();
    if (!fa) {
      toast.error("نام فارسی برچسب الزامی است.");
      return;
    }
    setBusy(true);
    try {
      const created = await createCatalogTag({
        nameFa: fa,
        nameEn: nameEn.trim() || null,
      });
      if (created.state !== "ok" || !created.data) {
        toast.error(mapAdminErrorMessage(created.message) || "ایجاد برچسب ناموفق بود.");
        return;
      }
      setCatalog((prev) =>
        prev.some((t) => t.tagId === created.data!.tagId)
          ? prev
          : [...prev, created.data!],
      );
      const assignedResult = await assignTag(ownerId, created.data.tagId);
      if (assignedResult.state !== "ok" || !assignedResult.data) {
        toast.error(mapAdminErrorMessage(assignedResult.message) || "اختصاص برچسب جدید ناموفق بود.");
        await refresh();
        return;
      }
      setAssigned(assignedResult.data);
      setNameFa("");
      setNameEn("");
      setCreateOpen(false);
      setOpen(false);
      toast.success("برچسب ایجاد و اضافه شد.");
    } finally {
      setBusy(false);
    }
  }

  const title = ownerKind === "product" ? "برچسب‌های محصول" : "برچسب‌های دسته";

  return (
    <div
      className="rounded-2xl border border-border bg-surface-elevated p-3.5 shadow-sm"
      data-testid={testIdPrefix}
      data-owner-kind={ownerKind}
    >
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-sm font-medium">{title}</p>
          <p className="mt-1 text-xs text-muted" data-testid={`${testIdPrefix}-helper`}>
            {TAG_HELPER_FA}
          </p>
        </div>
      </div>

      <ul className="mt-3 flex flex-wrap gap-2" data-testid={`${testIdPrefix}-chips`}>
        {assigned.length === 0 ? (
          <li className="text-xs text-muted">برچسبی انتخاب نشده است.</li>
        ) : (
          assigned.map((tag) => (
            <li
              key={tag.tagId}
              className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1 text-xs text-slate-800"
              data-testid={`${testIdPrefix}-chip-${tag.tagId}`}
            >
              <span>{tag.name}</span>
              {canEdit ? (
                <button
                  type="button"
                  className="text-red-600 disabled:opacity-50"
                  disabled={busy}
                  aria-label={`حذف ${tag.name}`}
                  data-testid={`${testIdPrefix}-remove-${tag.tagId}`}
                  onClick={() => void handleRemove(tag.tagId)}
                >
                  حذف
                </button>
              ) : null}
            </li>
          ))
        )}
      </ul>

      {canEdit ? (
        <div className="relative mt-3" ref={rootRef}>
          <button
            type="button"
            disabled={busy}
            className="flex min-h-10 w-full items-center justify-between gap-2 rounded-ds border border-border bg-surface px-3 text-start text-sm disabled:opacity-50"
            aria-haspopup="listbox"
            aria-expanded={open}
            aria-controls={listId}
            data-testid={`${testIdPrefix}-picker-trigger`}
            onClick={() => {
              setOpen((v) => !v);
              setQuery("");
            }}
          >
            <span className="text-muted">جستجو و انتخاب برچسب…</span>
            <span className="text-muted" aria-hidden>
              ▾
            </span>
          </button>
          {open ? (
            <div
              className="absolute z-40 mt-1 w-full overflow-hidden rounded-ds border border-border bg-surface shadow-lg"
              data-testid={`${testIdPrefix}-picker-panel`}
            >
              <input
                autoFocus
                className="min-h-10 w-full border-b border-border bg-surface px-3 text-sm outline-none"
                placeholder="جستجوی نام برچسب…"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                data-testid={`${testIdPrefix}-picker-search`}
              />
              <ul id={listId} role="listbox" className="max-h-48 overflow-auto py-1">
                {available.length === 0 ? (
                  <li className="px-3 py-2 text-xs text-muted">برچسب قابل انتخابی نیست.</li>
                ) : (
                  available.map((tag) => (
                    <li key={tag.tagId}>
                      <button
                        type="button"
                        className="flex w-full px-3 py-2 text-start text-sm hover:bg-secondary"
                        role="option"
                        aria-selected={false}
                        data-testid={`${testIdPrefix}-option-${tag.tagId}`}
                        onClick={() => void handleAssign(tag)}
                      >
                        {tag.name}
                      </button>
                    </li>
                  ))
                )}
              </ul>
              <div className="border-t border-border p-2">
                <button
                  type="button"
                  className="text-xs font-medium text-primary underline-offset-2 hover:underline"
                  data-testid={`${testIdPrefix}-create-toggle`}
                  onClick={() => setCreateOpen((v) => !v)}
                >
                  {createOpen ? "بستن فرم ایجاد" : "ایجاد برچسب جدید"}
                </button>
                {createOpen ? (
                  <div className="mt-2 space-y-2" data-testid={`${testIdPrefix}-create-form`}>
                    <label className="block text-xs font-medium">
                      نام فارسی <span className="text-red-600">*</span>
                      <input
                        className="mt-1 min-h-9 w-full rounded-ds border border-border bg-surface px-2 text-sm"
                        value={nameFa}
                        onChange={(e) => setNameFa(e.target.value)}
                        data-testid={`${testIdPrefix}-create-fa`}
                      />
                    </label>
                    <label className="block text-xs font-medium">
                      نام انگلیسی (اختیاری)
                      <input
                        className="mt-1 min-h-9 w-full rounded-ds border border-border bg-surface px-2 text-sm"
                        dir="ltr"
                        value={nameEn}
                        onChange={(e) => setNameEn(e.target.value)}
                        data-testid={`${testIdPrefix}-create-en`}
                      />
                    </label>
                    <button
                      type="button"
                      disabled={busy || !nameFa.trim()}
                      className="inline-flex min-h-9 items-center rounded-ds bg-primary px-3 text-xs font-semibold text-white disabled:opacity-50"
                      data-testid={`${testIdPrefix}-create-submit`}
                      onClick={() => void handleCreate()}
                    >
                      {busy ? "در حال ذخیره…" : "ایجاد و افزودن"}
                    </button>
                  </div>
                ) : null}
              </div>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
