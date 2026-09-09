"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Pencil, Plus, Ban } from "lucide-react";
import {
  AppDataGrid,
  Dialog,
  ErrorState,
  faWorkspaceMessages,
  createClientGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../design-system";
import type { GridColumnDef, GridServerQuery } from "../../design-system/data-grid";
import { resolveAdminChromeLocale } from "./admin-chrome-messages.ts";
import { loadAdminLanguages } from "./language-api.ts";
import {
  deactivateAdminUnit,
  loadAdminUnit,
  loadAdminUnits,
  writeAdminUnit,
  type UnitOfMeasureListItem,
  type UnitTranslationWrite,
} from "./catalog-units-api.ts";
import { createHostSavedViewStore } from "./saved-view-store";
import type { SupportedLocaleDefinition } from "../../lib/i18n/supported-locales.ts";

const GRID_VIEW_KEY = "admin-catalog-units";
const DIMENSIONS = ["Count", "Mass", "Volume", "Length"] as const;

function boolFa(value: boolean): string {
  return value ? "بله" : "خیر";
}

function dimensionFa(value: string): string {
  switch (value) {
    case "Mass":
      return "جرم";
    case "Volume":
      return "حجم";
    case "Length":
      return "طول";
    default:
      return "شمارشی";
  }
}

function emptyTranslations(languages: SupportedLocaleDefinition[]): UnitTranslationWrite[] {
  return languages
    .filter((row) => row.languageId)
    .map((row) => ({ languageId: row.languageId as string, name: "", shortName: "" }));
}

export function CatalogUnitsScreen() {
  const [state, setState] = useState<"loading" | "ok" | "denied" | "error">("loading");
  const [rows, setRows] = useState<UnitOfMeasureListItem[]>([]);
  const [languages, setLanguages] = useState<SupportedLocaleDefinition[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [code, setCode] = useState("");
  const [dimension, setDimension] = useState<string>("Count");
  const [isActive, setIsActive] = useState(true);
  const [sortOrder, setSortOrder] = useState("0");
  const [translations, setTranslations] = useState<UnitTranslationWrite[]>([]);
  const [langTab, setLangTab] = useState<string>("");
  const [referenced, setReferenced] = useState(false);
  const [saving, setSaving] = useState(false);

  const refresh = useCallback(async () => {
    setState("loading");
    const locale = resolveAdminChromeLocale();
    const [unitsResult, langsResult] = await Promise.all([
      loadAdminUnits(locale),
      loadAdminLanguages(),
    ]);
    if (unitsResult.state === "denied" || langsResult.state === "denied") {
      setState("denied");
      return;
    }
    if (unitsResult.state !== "ok" || langsResult.state !== "ok" || !unitsResult.data || !langsResult.data) {
      setState("error");
      setMessage(unitsResult.message ?? langsResult.message);
      return;
    }
    setRows(unitsResult.data);
    setLanguages(langsResult.data.filter((row) => row.active && row.languageId));
    setState("ok");
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  function openCreate() {
    const next = emptyTranslations(languages);
    setEditingId(null);
    setCode("");
    setDimension("Count");
    setIsActive(true);
    setSortOrder(String((rows.at(-1)?.sortOrder ?? 0) + 10));
    setTranslations(next);
    setLangTab(next[0]?.languageId ?? "");
    setReferenced(false);
    setDialogOpen(true);
  }

  async function openEdit(unitId: string) {
    const result = await loadAdminUnit(unitId);
    if (result.state !== "ok" || !result.data) {
      setMessage(result.message ?? "واحد خوانده نشد");
      return;
    }
    const merged = emptyTranslations(languages).map((blank) => {
      const existing = result.data!.translations.find((row) => row.languageId === blank.languageId);
      return existing ?? blank;
    });
    setEditingId(unitId);
    setCode(result.data.code);
    setDimension(result.data.dimension);
    setIsActive(result.data.isActive);
    setSortOrder(String(result.data.sortOrder));
    setTranslations(merged);
    setLangTab(merged[0]?.languageId ?? "");
    setReferenced(result.data.isReferenced);
    setDialogOpen(true);
  }

  async function onSave() {
    if (!code.trim()) {
      setMessage("کد واحد لازم است");
      return;
    }
    const sort = Number(sortOrder);
    if (!Number.isInteger(sort)) {
      setMessage("ترتیب نمایش باید عدد صحیح باشد");
      return;
    }
    if (translations.some((row) => !row.name.trim() || !row.shortName.trim())) {
      setMessage("نام و نام کوتاه هر زبان فعال لازم است");
      return;
    }
    setSaving(true);
    const result = await writeAdminUnit(editingId, {
      code: code.trim(),
      dimension,
      isActive,
      sortOrder: sort,
      translations,
    });
    setSaving(false);
    if (result.state !== "ok") {
      setMessage(result.message ?? "ذخیره نشد");
      return;
    }
    setDialogOpen(false);
    await refresh();
  }

  async function onDeactivate(unitId: string) {
    const result = await deactivateAdminUnit(unitId);
    if (result.state !== "ok") {
      setMessage(result.message ?? "غیرفعال‌سازی انجام نشد");
      return;
    }
    setDialogOpen(false);
    await refresh();
  }

  const columns = useMemo<GridColumnDef<UnitOfMeasureListItem>[]>(
    () => [
      {
        id: "code",
        header: "کد",
        accessor: (row) => row.code,
        cell: (row) => <span dir="ltr" className="font-mono text-xs">{row.code}</span>,
        width: 110,
        minWidth: 90,
      },
      { id: "name", header: "نام", accessor: (row) => row.name, width: 160, minWidth: 120, flex: 1.2 },
      { id: "shortName", header: "نام کوتاه", accessor: (row) => row.shortName, width: 110, minWidth: 90 },
      {
        id: "dimension",
        header: "بعد",
        accessor: (row) => row.dimension,
        cell: (row) => dimensionFa(row.dimension),
        width: 100,
        minWidth: 80,
      },
      {
        id: "isActive",
        header: "فعال",
        accessor: (row) => row.isActive,
        cell: (row) => (
          <span className={row.isActive ? "rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-bold text-emerald-700" : "rounded-full bg-gray-100 px-2 py-0.5 text-xs font-bold text-gray-600"}>
            {boolFa(row.isActive)}
          </span>
        ),
        width: 90,
        minWidth: 80,
      },
      {
        id: "sortOrder",
        header: "ترتیب",
        accessor: (row) => row.sortOrder,
        width: 80,
        minWidth: 70,
      },
      {
        id: "ops",
        header: "عملیات",
        accessor: (row) => row.unitOfMeasureId,
        cell: (row) => (
          <div className="flex items-center gap-2">
            <button type="button" className="text-primary" data-testid={`unit-edit-${row.unitOfMeasureId}`} onClick={() => void openEdit(row.unitOfMeasureId)}>
              <Pencil className="h-4 w-4" />
            </button>
            {row.isActive ? (
              <button type="button" className="text-amber-700" data-testid={`unit-deactivate-${row.unitOfMeasureId}`} onClick={() => void onDeactivate(row.unitOfMeasureId)}>
                <Ban className="h-4 w-4" />
              </button>
            ) : null}
          </div>
        ),
        width: 90,
        minWidth: 80,
      },
    ],
    [languages],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(rows, columns)(query),
    [columns, rows],
  );
  const savedViewStore = useMemo(() => createHostSavedViewStore(GRID_VIEW_KEY), []);
  const gridProps = useLegacyAdminGridDirectProps({
    gridId: GRID_VIEW_KEY,
    columns,
    queryAdapter,
    savedViewStore,
  });

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

  const activeLang = languages.find((row) => row.languageId === langTab) ?? languages[0];

  return (
    <main className="space-y-4" data-testid="admin-catalog-units">
      <header className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-black text-gray-900">واحدهای اندازه‌گیری</h1>
          <p className="mt-1 text-sm text-gray-500">ترجمه‌ها از رجیستری زبان خوانده می‌شوند؛ ستون ثابت فارسی/انگلیسی وجود ندارد.</p>
        </div>
        <button
          type="button"
          className="inline-flex items-center gap-2 rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
          data-testid="unit-create"
          onClick={openCreate}
        >
          <Plus className="h-4 w-4" />
          واحد جدید
        </button>
      </header>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        <div className="p-2 md:p-4">
          {state === "error" ? (
            <ErrorState title="واحدها خوانده نشد" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
          ) : state === "loading" ? (
            <p className="py-8 text-center text-sm text-muted">در حال بارگذاری…</p>
          ) : (
            <AppDataGrid<UnitOfMeasureListItem> {...gridProps} />
          )}
        </div>
      </section>

      <Dialog title={editingId ? "ویرایش واحد" : "واحد جدید"} open={dialogOpen} onClose={() => setDialogOpen(false)} showCloseButton>
        <div className="space-y-3" data-testid="unit-edit-form">
          {message ? <p className="text-sm text-red-600">{message}</p> : null}
          <label className="block text-sm">
            کد
            <input className="mt-1 w-full rounded-lg border border-gray-200 px-3 py-2 font-mono text-sm" dir="ltr" value={code} onChange={(e) => setCode(e.target.value)} data-testid="unit-code" />
          </label>
          <label className="block text-sm">
            بعد
            <select className="mt-1 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm" value={dimension} onChange={(e) => setDimension(e.target.value)} data-testid="unit-dimension">
              {DIMENSIONS.map((item) => (
                <option key={item} value={item}>{dimensionFa(item)}</option>
              ))}
            </select>
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} data-testid="unit-active" />
            فعال
          </label>
          <label className="block text-sm">
            ترتیب
            <input className="mt-1 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm" dir="ltr" value={sortOrder} onChange={(e) => setSortOrder(e.target.value)} data-testid="unit-sort" />
          </label>
          <div>
            <div className="flex flex-wrap gap-2 border-b border-gray-200 pb-2">
              {languages.map((lang) => (
                <button
                  key={lang.languageId}
                  type="button"
                  className={`rounded-full px-3 py-1 text-xs ${lang.languageId === (activeLang?.languageId ?? langTab) ? "bg-[#2563EB] text-white" : "bg-gray-100 text-gray-700"}`}
                  data-testid={`unit-lang-tab-${lang.code}`}
                  onClick={() => setLangTab(lang.languageId ?? "")}
                >
                  {lang.nativeName || lang.code}
                </button>
              ))}
            </div>
            {activeLang?.languageId ? (
              <div className="mt-3 grid gap-3 sm:grid-cols-2">
                <label className="block text-sm">
                  نام
                  <input
                    className="mt-1 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
                    value={translations.find((row) => row.languageId === activeLang.languageId)?.name ?? ""}
                    onChange={(e) => {
                      const next = e.target.value;
                      setTranslations((prev) => prev.map((row) => row.languageId === activeLang.languageId ? { ...row, name: next } : row));
                    }}
                    data-testid="unit-name"
                  />
                </label>
                <label className="block text-sm">
                  نام کوتاه
                  <input
                    className="mt-1 w-full rounded-lg border border-gray-200 px-3 py-2 text-sm"
                    value={translations.find((row) => row.languageId === activeLang.languageId)?.shortName ?? ""}
                    onChange={(e) => {
                      const next = e.target.value;
                      setTranslations((prev) => prev.map((row) => row.languageId === activeLang.languageId ? { ...row, shortName: next } : row));
                    }}
                    data-testid="unit-short-name"
                  />
                </label>
              </div>
            ) : null}
          </div>
          <div className="flex justify-end gap-2 pt-2">
            {editingId && referenced ? (
              <button type="button" className="rounded-xl border border-amber-200 px-4 py-2 text-sm text-amber-800" onClick={() => void onDeactivate(editingId)}>
                غیرفعال‌سازی (حذف سخت نیست)
              </button>
            ) : null}
            <button type="button" disabled={saving} className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white disabled:opacity-70" data-testid="unit-save" onClick={() => void onSave()}>
              ذخیره
            </button>
          </div>
        </div>
      </Dialog>
    </main>
  );
}
