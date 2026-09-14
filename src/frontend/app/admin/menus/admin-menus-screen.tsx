"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  deleteAdminMenu,
  getAdminHeaderMenu,
  getAdminMenuUsage,
  listAdminMenus,
  setAdminHeaderMenu,
  setAdminMenuEnabled,
  type AdminMenu,
} from "./admin-menus-api.ts";

function formatUpdated(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";
  return date.toLocaleString("fa-IR");
}

function localeLabel(locale: string): string {
  return locale === "en" ? "انگلیسی" : "فارسی";
}

export function AdminMenusScreen() {
  const [rows, setRows] = useState<AdminMenu[]>([]);
  const [headerMenuId, setHeaderMenuId] = useState<string | null>(null);
  const [message, setMessage] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [denied, setDenied] = useState(false);

  const refresh = useCallback(() => {
    setLoading(true);
    void Promise.all([listAdminMenus(), getAdminHeaderMenu()]).then(([menus, header]) => {
      setLoading(false);
      if (!menus.ok) {
        setDenied(Boolean(menus.denied));
        setMessage(menus.message);
        return;
      }
      setDenied(false);
      setRows(menus.data);
      if (header.ok) setHeaderMenuId(header.data.headerMenuId);
      setMessage(undefined);
    });
  }, []);

  useEffect(refresh, [refresh]);

  const toggleEnabled = async (row: AdminMenu) => {
    setBusy(true);
    const result = await setAdminMenuEnabled(row.menuId, !row.isEnabled);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    refresh();
  };

  const assignHeader = async (row: AdminMenu) => {
    if (!row.isEnabled) {
      setMessage("برای استفاده در نوار بالا ابتدا منو را فعال کنید.");
      return;
    }
    setBusy(true);
    const result = await setAdminHeaderMenu(headerMenuId === row.menuId ? null : row.menuId);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setHeaderMenuId(result.data.headerMenuId);
  };

  const remove = async (row: AdminMenu) => {
    const usage = await getAdminMenuUsage(row.menuId);
    if (usage.ok && usage.data.length > 0) {
      setMessage(`حذف ممکن نیست: ${usage.data.map((item) => item.label).join("، ")}`);
      return;
    }
    if (!window.confirm(`منوی «${row.title}» حذف شود؟`)) return;
    setBusy(true);
    const result = await deleteAdminMenu(row.menuId);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    refresh();
  };

  if (denied) {
    return (
      <main className="rounded-2xl border border-border bg-surface-elevated p-8" data-testid="admin-menus">
        <p>دسترسی به منوها مجاز نیست.</p>
        <button type="button" className="mt-3 rounded-xl border px-4 py-2" onClick={refresh}>تلاش دوباره</button>
      </main>
    );
  }

  return (
    <main data-testid="admin-menus">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-black">منوها</h1>
          <p className="mt-1 text-sm text-muted">منوی فروشگاه را بسازید و در صورت نیاز به نوار بالای ویترین وصل کنید. اگر منویی انتخاب نشود، ناوبری فعلی فروشگاه باقی می‌ماند.</p>
        </div>
        <Link href="/admin/menus/new" className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white" data-testid="menu-create">
          ایجاد منو
        </Link>
      </div>

      {message ? <p className="mb-3 text-sm text-red-600">{message}</p> : null}

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        {loading ? (
          <p className="p-6 text-sm text-muted">در حال بارگذاری…</p>
        ) : rows.length === 0 ? (
          <div className="p-10 text-center" data-testid="menu-empty">
            <p className="font-bold">هنوز منویی ساخته نشده است.</p>
            <p className="mt-2 text-sm text-muted">با «ایجاد منو» یک فهرست پیوند حرفه‌ای بسازید.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[820px] text-sm">
              <thead className="bg-slate-50 text-right text-muted">
                <tr>
                  <th className="px-4 py-3 font-bold">نام منو</th>
                  <th className="px-4 py-3 font-bold">زبان</th>
                  <th className="px-4 py-3 font-bold">وضعیت</th>
                  <th className="px-4 py-3 font-bold">تعداد آیتم</th>
                  <th className="px-4 py-3 font-bold">نوار بالا؟</th>
                  <th className="px-4 py-3 font-bold">آخرین ویرایش</th>
                  <th className="px-4 py-3 font-bold">اقدامات</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const isHeader = headerMenuId === row.menuId;
                  return (
                    <tr key={row.menuId} className="border-t border-border" data-testid={`menu-row-${row.title}`}>
                      <td className="px-4 py-3 font-bold">{row.title}</td>
                      <td className="px-4 py-3">{localeLabel(row.locale)}</td>
                      <td className="px-4 py-3">
                        <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${row.isEnabled ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}>
                          {row.isEnabled ? "فعال" : "غیرفعال"}
                        </span>
                      </td>
                      <td className="px-4 py-3">{row.itemCount.toLocaleString("fa-IR")}</td>
                      <td className="px-4 py-3">{isHeader ? "بله" : "خیر"}</td>
                      <td className="px-4 py-3">{formatUpdated(row.updatedAt)}</td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-2">
                          <Link className="rounded-lg border px-2 py-1" href={`/admin/menus/${row.menuId}`}>ویرایش</Link>
                          <button type="button" className="rounded-lg border px-2 py-1" disabled={busy} onClick={() => void toggleEnabled(row)}>
                            {row.isEnabled ? "غیرفعال کردن" : "فعال کردن"}
                          </button>
                          <button type="button" className="rounded-lg border px-2 py-1" disabled={busy} onClick={() => void assignHeader(row)}>
                            {isHeader ? "لغو نوار بالا" : "استفاده در نوار بالا"}
                          </button>
                          <button type="button" className="rounded-lg border px-2 py-1 text-red-700" disabled={busy} onClick={() => void remove(row)}>حذف</button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
