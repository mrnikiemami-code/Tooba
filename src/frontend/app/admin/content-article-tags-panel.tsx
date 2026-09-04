"use client";

import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { toast } from "react-toastify";
import { mapAdminErrorMessage } from "./admin-error-map.ts";
import {
  assignContentArticleTag,
  createContentTag,
  listContentArticleTags,
  removeContentArticleTag,
  searchContentTags,
  type ContentTagDto,
} from "./content-tag-api.ts";

/**
 * چیپ‌های جستجوپذیر برچسب مقاله — ایجاد درون‌خطی در صورت content.edit.
 */
export function ContentArticleTagsPanel({
  articleId,
  languageCode,
  canEdit,
  onChanged,
  testIdPrefix = "content-article-tags",
}: {
  articleId: string;
  languageCode: string;
  canEdit: boolean;
  onChanged?: () => void;
  testIdPrefix?: string;
}) {
  const listId = useId();
  const rootRef = useRef<HTMLDivElement | null>(null);
  const [assigned, setAssigned] = useState<ContentTagDto[]>([]);
  const [suggestions, setSuggestions] = useState<ContentTagDto[]>([]);
  const [busy, setBusy] = useState(false);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");

  const refreshAssigned = useCallback(async () => {
    const result = await listContentArticleTags(articleId);
    if (result.state === "ok" && result.data) setAssigned(result.data);
  }, [articleId]);

  useEffect(() => {
    void refreshAssigned();
  }, [refreshAssigned]);

  useEffect(() => {
    if (!open) return;
    const handle = window.setTimeout(() => {
      void searchContentTags(languageCode, query, true).then((result) => {
        if (result.state === "ok" && result.data) setSuggestions(result.data);
      });
    }, 200);
    return () => window.clearTimeout(handle);
  }, [languageCode, open, query]);

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
    const assignedIds = new Set(assigned.map((t) => t.tagId));
    return suggestions.filter((t) => !assignedIds.has(t.tagId) && t.isActive);
  }, [assigned, suggestions]);

  async function handleAssign(tag: ContentTagDto) {
    if (!canEdit || busy) return;
    setBusy(true);
    try {
      const result = await assignContentArticleTag(articleId, tag.tagId);
      if (result.state !== "ok" || !result.data) {
        toast.error(mapAdminErrorMessage(result.message, "fa"));
        return;
      }
      setAssigned(result.data);
      setOpen(false);
      setQuery("");
      onChanged?.();
      toast.success("برچسب اضافه شد.");
    } finally {
      setBusy(false);
    }
  }

  async function handleRemove(tagId: string) {
    if (!canEdit || busy) return;
    setBusy(true);
    try {
      const result = await removeContentArticleTag(articleId, tagId);
      if (result.state !== "ok" || !result.data) {
        toast.error(mapAdminErrorMessage(result.message, "fa"));
        return;
      }
      setAssigned(result.data);
      onChanged?.();
      toast.success("برچسب حذف شد.");
    } finally {
      setBusy(false);
    }
  }

  async function handleCreate() {
    if (!canEdit || busy) return;
    const name = query.trim();
    if (!name) {
      toast.error("نام برچسب الزامی است.");
      return;
    }
    setBusy(true);
    try {
      const created = await createContentTag({ languageCode, name });
      if (created.state !== "ok" || !created.data) {
        toast.error(mapAdminErrorMessage(created.message, "fa"));
        return;
      }
      const assignedResult = await assignContentArticleTag(articleId, created.data.tagId);
      if (assignedResult.state !== "ok" || !assignedResult.data) {
        toast.error(mapAdminErrorMessage(assignedResult.message, "fa"));
        await refreshAssigned();
        return;
      }
      setAssigned(assignedResult.data);
      setQuery("");
      setOpen(false);
      onChanged?.();
      toast.success("برچسب ایجاد و اضافه شد.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div
      ref={rootRef}
      className="rounded-xl border p-3"
      data-testid={testIdPrefix}
      data-language={languageCode}
    >
      <p className="mb-1 text-sm font-medium">برچسب‌ها</p>
      <p className="mb-3 text-xs text-muted">جستجو، انتخاب یا ایجاد برچسب هم‌زبان با مقاله.</p>

      <ul className="mb-3 flex flex-wrap gap-2" data-testid={`${testIdPrefix}-chips`}>
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
                  className="text-slate-500 hover:text-slate-900"
                  aria-label={`حذف ${tag.name}`}
                  data-testid={`${testIdPrefix}-remove-${tag.tagId}`}
                  onClick={() => void handleRemove(tag.tagId)}
                >
                  ×
                </button>
              ) : null}
            </li>
          ))
        )}
      </ul>

      {canEdit ? (
        <div className="relative">
          <input
            className="w-full rounded-xl border px-3 py-2 text-sm"
            value={query}
            placeholder="جستجو یا ایجاد برچسب…"
            disabled={busy}
            data-testid={`${testIdPrefix}-search`}
            onFocus={() => setOpen(true)}
            onChange={(e) => {
              setQuery(e.target.value);
              setOpen(true);
            }}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                const exact = available.find(
                  (t) => t.name.trim().toLowerCase() === query.trim().toLowerCase(),
                );
                if (exact) void handleAssign(exact);
                else void handleCreate();
              }
            }}
          />
          {open ? (
            <div
              className="absolute z-20 mt-1 w-full rounded-xl border bg-white p-2 shadow-lg"
              data-testid={`${testIdPrefix}-panel`}
            >
              <ul id={listId} className="max-h-48 overflow-auto">
                {available.map((tag) => (
                  <li key={tag.tagId}>
                    <button
                      type="button"
                      className="w-full rounded-lg px-2 py-2 text-start text-sm hover:bg-slate-50"
                      data-testid={`${testIdPrefix}-option-${tag.tagId}`}
                      onClick={() => void handleAssign(tag)}
                    >
                      {tag.name}
                    </button>
                  </li>
                ))}
                {query.trim() &&
                !available.some((t) => t.name.trim().toLowerCase() === query.trim().toLowerCase()) ? (
                  <li>
                    <button
                      type="button"
                      className="w-full rounded-lg px-2 py-2 text-start text-sm text-[#2563EB] hover:bg-slate-50"
                      data-testid={`${testIdPrefix}-create`}
                      onClick={() => void handleCreate()}
                    >
                      ایجاد «{query.trim()}»
                    </button>
                  </li>
                ) : null}
                {available.length === 0 && !query.trim() ? (
                  <li className="px-2 py-2 text-sm text-muted">برای یافتن برچسب جستجو کنید.</li>
                ) : null}
              </ul>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
