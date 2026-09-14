"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  addAdminMenuItem,
  createAdminMenu,
  deleteAdminMenuItem,
  getAdminMenu,
  reorderAdminMenuItems,
  setAdminMenuItemEnabled,
  updateAdminMenu,
  updateAdminMenuItem,
  type AdminMenuDetail,
  type AdminMenuItem,
} from "./admin-menus-api.ts";
import { MenuDestinationFields } from "./admin-menu-pickers.tsx";
import { menuLinkLabel, summarizeMenuDestination } from "./admin-menu-destination.ts";

function depthOf(items: AdminMenuItem[], itemId: string): number {
  let depth = 1;
  let current = items.find((row) => row.menuItemId === itemId);
  while (current?.parentMenuItemId) {
    depth += 1;
    current = items.find((row) => row.menuItemId === current?.parentMenuItemId);
    if (depth > 4) break;
  }
  return depth;
}

function siblings(items: AdminMenuItem[], parentId: string | null): AdminMenuItem[] {
  return items.filter((item) => item.parentMenuItemId === parentId).sort((a, b) => a.sortOrder - b.sortOrder);
}

export function AdminMenuEditor({ menuId }: { menuId?: string }) {
  const router = useRouter();
  const [menu, setMenu] = useState<AdminMenuDetail | null>(null);
  const [title, setTitle] = useState("منوی جدید");
  const [locale, setLocale] = useState("fa");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [draft, setDraft] = useState<AdminMenuItem | null>(null);
  const [targetLabel, setTargetLabel] = useState<string | null>(null);
  const [message, setMessage] = useState<string>();
  const [busy, setBusy] = useState(false);

  const load = useCallback(async (id: string) => {
    const result = await getAdminMenu(id);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setMenu(result.data);
    setTitle(result.data.title);
    setLocale(result.data.locale);
  }, []);

  useEffect(() => {
    if (menuId) void load(menuId);
  }, [load, menuId]);

  const items = menu?.items ?? [];
  const selected = items.find((item) => item.menuItemId === selectedId) ?? null;
  const tree = useMemo(() => {
    const walk = (parentId: string | null, depth: number): AdminMenuItem[] =>
      siblings(items, parentId).flatMap((item) => [item, ...walk(item.menuItemId, depth + 1)]);
    return walk(null, 1);
  }, [items]);

  useEffect(() => {
    if (selected) setDraft({ ...selected });
  }, [selected?.menuItemId]);

  const saveMenu = async () => {
    setBusy(true);
    if (!menuId) {
      const created = await createAdminMenu(title, locale);
      setBusy(false);
      if (!created.ok) {
        setMessage(created.message);
        return;
      }
      router.replace(`/admin/menus/${created.data.menuId}`);
      return;
    }
    const result = await updateAdminMenu(menuId, title, locale);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setMenu(result.data);
    setMessage(undefined);
  };

  const addItem = async (parentMenuItemId: string | null) => {
    if (!menu) return;
    if (parentMenuItemId && depthOf(items, parentMenuItemId) >= 3) {
      setMessage("حداکثر سه سطح تو در تو مجاز است.");
      return;
    }
    setBusy(true);
    const result = await addAdminMenuItem(menu.menuId, {
      label: parentMenuItemId ? "زیربخش جدید" : "آیتم جدید",
      linkType: "Group",
      parentMenuItemId,
      isEnabled: true,
    });
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    await load(menu.menuId);
    setSelectedId(result.data.menuItemId);
  };

  const saveItem = async () => {
    if (!menu || !draft) return;
    setBusy(true);
    const result = await updateAdminMenuItem(menu.menuId, draft);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    await load(menu.menuId);
  };

  const move = async (item: AdminMenuItem, direction: -1 | 1) => {
    if (!menu) return;
    const group = siblings(items, item.parentMenuItemId);
    const index = group.findIndex((row) => row.menuItemId === item.menuItemId);
    const swap = group[index + direction];
    if (!swap) return;
    const next = items.map((row) => row.menuItemId);
    const a = next.indexOf(item.menuItemId);
    const b = next.indexOf(swap.menuItemId);
    [next[a], next[b]] = [next[b]!, next[a]!];
    setBusy(true);
    const result = await reorderAdminMenuItems(menu.menuId, next);
    setBusy(false);
    if (!result.ok) setMessage(result.message);
    else setMenu(result.data);
  };

  return (
    <main data-testid="admin-menu-editor">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <Link href="/admin/menus" className="text-sm text-muted">بازگشت به فهرست منوها</Link>
          <h1 className="mt-2 text-xl font-black">{menu ? "ویرایش منو" : "منوی جدید"}</h1>
        </div>
        <button type="button" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" disabled={busy} onClick={() => void saveMenu()}>
          ذخیرهٔ مشخصات
        </button>
      </div>
      {message ? <p className="mb-3 text-sm text-red-600">{message}</p> : null}

      <section className="mb-5 grid gap-3 rounded-2xl border border-border bg-surface-elevated p-4 md:grid-cols-2">
        <label className="text-sm">
          <span className="mb-1 block font-bold">نام منو</span>
          <input className="w-full rounded-xl border px-3 py-2" value={title} onChange={(event) => setTitle(event.target.value)} data-testid="menu-title" />
        </label>
        <label className="text-sm">
          <span className="mb-1 block font-bold">زبان</span>
          <select className="w-full rounded-xl border px-3 py-2" value={locale} onChange={(event) => setLocale(event.target.value)}>
            <option value="fa">فارسی</option>
            <option value="en">انگلیسی</option>
          </select>
        </label>
      </section>

      {menu ? (
        <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_360px]">
          <section className="rounded-2xl border border-border bg-surface-elevated p-4">
            <div className="mb-3 flex items-center justify-between">
              <h2 className="font-black">ساختار منو</h2>
              <button type="button" className="rounded-lg border px-3 py-1 text-sm" disabled={busy} onClick={() => void addItem(null)} data-testid="menu-add-root">
                افزودن آیتم اصلی
              </button>
            </div>
            <ul className="space-y-2" data-testid="menu-tree">
              {tree.map((item) => {
                const depth = depthOf(items, item.menuItemId);
                return (
                  <li
                    key={item.menuItemId}
                    className={`rounded-xl border px-3 py-2 ${selectedId === item.menuItemId ? "border-primary bg-primary/5" : "border-border"} ${item.isEnabled ? "" : "opacity-60"}`}
                    style={{ marginInlineStart: `${(depth - 1) * 18}px` }}
                  >
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <button type="button" className="text-start" onClick={() => setSelectedId(item.menuItemId)}>
                        <p className="font-bold">{item.label}</p>
                        <p className="text-xs text-muted">{menuLinkLabel(item.linkType)} · سطح {depth.toLocaleString("fa-IR")}</p>
                      </button>
                      <div className="flex flex-wrap gap-1 text-xs">
                        <button type="button" className="rounded border px-2 py-1" disabled={busy} onClick={() => void move(item, -1)}>بالا</button>
                        <button type="button" className="rounded border px-2 py-1" disabled={busy} onClick={() => void move(item, 1)}>پایین</button>
                        {depth < 3 ? (
                          <button type="button" className="rounded border px-2 py-1" disabled={busy} onClick={() => void addItem(item.menuItemId)} data-testid="menu-add-child">فرزند</button>
                        ) : null}
                        <button type="button" className="rounded border px-2 py-1" disabled={busy} onClick={() => void setAdminMenuItemEnabled(menu.menuId, item.menuItemId, !item.isEnabled).then((result) => result.ok ? load(menu.menuId) : setMessage(result.message))}>
                          {item.isEnabled ? "خاموش" : "روشن"}
                        </button>
                        <button type="button" className="rounded border px-2 py-1 text-red-700" disabled={busy} onClick={() => void deleteAdminMenuItem(menu.menuId, item.menuItemId).then((result) => result.ok ? load(menu.menuId) : setMessage(result.message))}>حذف</button>
                      </div>
                    </div>
                  </li>
                );
              })}
            </ul>
          </section>
          <aside className="rounded-2xl border border-border bg-surface-elevated p-4" data-testid="menu-item-drawer">
            {draft ? (
              <div className="space-y-3">
                <h2 className="font-black">ویرایش آیتم</h2>
                <label className="block text-sm">
                  <span className="mb-1 block font-bold">عنوان</span>
                  <input className="w-full rounded-xl border px-3 py-2" value={draft.label} onChange={(event) => setDraft({ ...draft, label: event.target.value })} />
                </label>
                <MenuDestinationFields
                  linkType={draft.linkType}
                  targetId={draft.targetId}
                  externalUrl={draft.externalUrl}
                  onLinkType={(linkType) => setDraft({ ...draft, linkType, targetId: null, externalUrl: null })}
                  onTarget={(id, label) => {
                    setDraft({ ...draft, targetId: id });
                    setTargetLabel(label);
                  }}
                  onExternal={(externalUrl) => setDraft({ ...draft, externalUrl })}
                />
                <p className="rounded-xl bg-slate-50 px-3 py-2 text-xs text-muted" data-testid="menu-destination-summary">
                  {summarizeMenuDestination({ linkType: draft.linkType, targetLabel, externalUrl: draft.externalUrl })}
                </p>
                <button type="button" className="w-full rounded-xl bg-[#2563EB] py-2 text-sm font-bold text-white" disabled={busy} onClick={() => void saveItem()}>
                  ذخیرهٔ آیتم
                </button>
              </div>
            ) : (
              <p className="text-sm text-muted">یک آیتم را از درخت انتخاب کنید یا آیتم اصلی بسازید.</p>
            )}
          </aside>
        </div>
      ) : (
        <p className="text-sm text-muted">ابتدا نام منو را ذخیره کنید تا بتوانید آیتم اضافه کنید.</p>
      )}
    </main>
  );
}
