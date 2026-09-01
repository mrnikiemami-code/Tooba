"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Pencil } from "lucide-react";
import {
  AppDataGrid,
  ErrorState,
  faWorkspaceMessages,
  createClientGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../design-system";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import type { SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";
import { loadAdminLanguages, patchAdminLanguage } from "./language-api";
import { createHostSavedViewStore } from "./saved-view-store";

type AdminLanguageRow = SupportedLocaleDefinition & { id: string };

const ADMIN_LANGUAGES_GRID_VIEW_KEY = "admin-languages";

function boolFa(value: boolean): string {
  return value ? "بله" : "خیر";
}

function calendarFa(value: string): string {
  return value === "jalali" ? "جلالی" : "میلادی";
}

function mapRows(defs: SupportedLocaleDefinition[]): AdminLanguageRow[] {
  return defs.map((row) => ({ ...row, id: row.code }));
}

function buildColumns(onEdit: (row: AdminLanguageRow) => void): GridColumnDef<AdminLanguageRow>[] {
  return [
    { id: "nativeName", header: "نام", accessor: (row) => row.nativeName, width: 140, minWidth: 120, flex: 1.2 },
    { id: "displayName", header: "نام نمایشی", accessor: (row) => row.displayName, width: 160, minWidth: 140, flex: 1.4 },
    {
      id: "code",
      header: "کد",
      accessor: (row) => row.code,
      cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.code}</span>,
      width: 110,
      minWidth: 100,
    },
    {
      id: "direction",
      header: "جهت",
      accessor: (row) => row.direction,
      cell: (row) => <span dir="ltr" className="text-xs uppercase">{row.direction}</span>,
      width: 90,
      minWidth: 80,
    },
    {
      id: "active",
      header: "فعال",
      accessor: (row) => row.active,
      cell: (row) => (
        <span className={row.active ? "rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-bold text-emerald-700" : "rounded-full bg-gray-100 px-2 py-0.5 text-xs font-bold text-gray-600"}>
          {boolFa(row.active)}
        </span>
      ),
      width: 90,
      minWidth: 80,
    },
    {
      id: "default",
      header: "پیش‌فرض",
      accessor: (row) => row.default,
      cell: (row) => (
        <span className={row.default ? "rounded-full bg-blue-50 px-2 py-0.5 text-xs font-bold text-blue-700" : "rounded-full bg-gray-100 px-2 py-0.5 text-xs font-bold text-gray-600"}>
          {boolFa(row.default)}
        </span>
      ),
      width: 100,
      minWidth: 90,
    },
    {
      id: "culture",
      header: "فرهنگ",
      accessor: (row) => row.culture,
      cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.culture}</span>,
      width: 110,
      minWidth: 100,
    },
    {
      id: "calendarDisplay",
      header: "تقویم",
      accessor: (row) => row.calendarDisplay,
      cell: (row) => calendarFa(row.calendarDisplay),
      width: 100,
      minWidth: 90,
    },
    { id: "sortOrder", header: "ترتیب", accessor: (row) => row.sortOrder, width: 90, minWidth: 80 },
    {
      id: "actions",
      header: "عملیات",
      accessor: () => "",
      pinned: "left",
      width: 88,
      minWidth: 72,
      maxWidth: 96,
      sortable: false,
      filter: false,
      cell: (row) => (
        <button
          type="button"
          className="inline-flex size-8 items-center justify-center rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50"
          aria-label="ویرایش"
          onClick={() => onEdit(row)}
        >
          <Pencil className="size-4" />
        </button>
      ),
    },
  ];
}

export function AdminLanguagesScreen() {
  const [rows, setRows] = useState<AdminLanguageRow[]>([]);
  const [state, setState] = useState<"loading" | "ok" | "error" | "denied">("loading");
  const [message, setMessage] = useState("");
  const [editing, setEditing] = useState<AdminLanguageRow | null>(null);
  const [draftActive, setDraftActive] = useState(true);
  const [draftDefault, setDraftDefault] = useState(false);
  const [draftSort, setDraftSort] = useState(0);
  const [saving, setSaving] = useState(false);

  const savedViewStore = useMemo(() => createHostSavedViewStore(ADMIN_LANGUAGES_GRID_VIEW_KEY), []);

  const refresh = useCallback(async () => {
    setState("loading");
    const result = await loadAdminLanguages();
    if (result.state === "denied") {
      setState("denied");
      return;
    }
    if (result.state !== "ok" || !result.data) {
      setState("error");
      setMessage(result.message ?? "زبان‌ها خوانده نشد");
      return;
    }
    setRows(mapRows(result.data));
    setState("ok");
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const openEdit = useCallback((row: AdminLanguageRow) => {
    setEditing(row);
    setDraftActive(row.active);
    setDraftDefault(row.default);
    setDraftSort(row.sortOrder);
  }, []);

  const columns = useMemo(() => buildColumns(openEdit), [openEdit]);

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(rows, columns)(query),
    [columns, rows],
  );

  const gridProps = useLegacyAdminGridDirectProps({
    gridId: ADMIN_LANGUAGES_GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

  const saveEdit = useCallback(async () => {
    if (!editing) return;
    setSaving(true);
    const result = await patchAdminLanguage(editing.code, {
      active: draftActive,
      default: draftDefault,
      sortOrder: draftSort,
    });
    setSaving(false);
    if (result.state !== "ok") {
      setMessage(result.message ?? "ذخیره نشد");
      return;
    }
    setEditing(null);
    await refresh();
  }, [draftActive, draftDefault, draftSort, editing, refresh]);

  if (state === "denied") {
    return (
      <ErrorState
        title="دسترسی مجاز نیست"
        detail="سامانه هویت فعلی را مدیر تشخیص نداد."
        onRetry={refresh}
        retryLabel={faWorkspaceMessages.retry}
      />
    );
  }

  return (
    <main className="space-y-4" data-testid="admin-languages">
      <header>
        <h1 className="text-xl font-black text-gray-900">زبان‌ها و محلیه‌ها</h1>
        <p className="mt-1 text-sm text-gray-500">رجیستری کانونی زبان برای محتوا و ویترین — هر مقاله یک زبان مستقل دارد.</p>
      </header>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="flex items-center justify-between gap-3 border-b border-border px-4 py-3 md:px-5">
          <span className="text-sm text-muted">SMALL_BOUNDED_CLIENT_SAFE — canonical locale registry (fa-IR, en-US)</span>
          <span className="rounded-full bg-secondary px-3 py-1 text-xs">{rows.length.toLocaleString("fa-IR")} مورد</span>
        </div>
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title="زبان‌ها خوانده نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : state === "loading" ? (
            <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <AppDataGrid<AdminLanguageRow> {...gridProps} />
          )}
        </div>
      </section>

      {editing ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-md rounded-xl border border-gray-200 bg-white p-4 shadow-xl">
            <h2 className="text-lg font-black text-gray-900">ویرایش زبان</h2>
            <p className="mt-1 text-sm text-gray-500" dir="ltr">{editing.code}</p>
            <div className="mt-4 space-y-3">
              <label className="flex items-center justify-between gap-3 text-sm">
                <span>فعال</span>
                <input type="checkbox" checked={draftActive} onChange={(e) => setDraftActive(e.target.checked)} />
              </label>
              <label className="flex items-center justify-between gap-3 text-sm">
                <span>پیش‌فرض</span>
                <input type="checkbox" checked={draftDefault} onChange={(e) => setDraftDefault(e.target.checked)} />
              </label>
              <label className="block text-sm">
                <span>ترتیب</span>
                <input
                  type="number"
                  className="mt-1 w-full rounded-lg border border-gray-200 px-3 py-2"
                  value={draftSort}
                  onChange={(e) => setDraftSort(Number(e.target.value))}
                />
              </label>
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <button type="button" className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm" onClick={() => setEditing(null)}>
                انصراف
              </button>
              <button
                type="button"
                disabled={saving}
                className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-sm font-bold text-white disabled:opacity-60"
                onClick={() => void saveEdit()}
              >
                ذخیره
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </main>
  );
}
