"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import {
  getAdminLandingHome,
  listAdminLandingPages,
  setAdminLandingHome,
  setAdminLandingPageStatus,
  type AdminLandingPage,
} from "./admin-landing-pages-api.ts";

function formatUpdated(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "—";
  return date.toLocaleString("fa-IR");
}

function localeLabel(locale: string): string {
  return locale === "en" ? "انگلیسی" : "فارسی";
}

export function AdminLandingPagesScreen() {
  const [rows, setRows] = useState<AdminLandingPage[]>([]);
  const [homePageId, setHomePageId] = useState<string | null>(null);
  const [message, setMessage] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [denied, setDenied] = useState(false);

  const refresh = useCallback(() => {
    setLoading(true);
    void Promise.all([listAdminLandingPages(), getAdminLandingHome()]).then(([pages, home]) => {
      setLoading(false);
      if (!pages.ok) {
        setDenied(Boolean(pages.denied));
        setMessage(pages.message);
        return;
      }
      setDenied(false);
      setRows(pages.data);
      if (home.ok) setHomePageId(home.data.homePageId);
      setMessage(undefined);
    });
  }, []);

  useEffect(refresh, [refresh]);

  const publish = async (row: AdminLandingPage) => {
    setBusy(true);
    const next = row.status === "Published" ? "Draft" : "Published";
    const result = await setAdminLandingPageStatus(row.pageId, next);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    refresh();
  };

  const setHome = async (row: AdminLandingPage) => {
    if (row.status !== "Published") {
      setMessage("برای انتخاب به‌عنوان صفحهٔ اصلی ابتدا صفحه را منتشر کنید.");
      return;
    }
    if (homePageId && homePageId !== row.pageId && !window.confirm("صفحهٔ اصلی فعلی جایگزین شود؟")) {
      return;
    }
    setBusy(true);
    const result = await setAdminLandingHome(homePageId === row.pageId ? null : row.pageId);
    setBusy(false);
    if (!result.ok) {
      setMessage(result.message);
      return;
    }
    setHomePageId(result.data.homePageId);
    setMessage(undefined);
  };

  if (denied) {
    return (
      <main className="rounded-2xl border border-border bg-surface-elevated p-8" data-testid="admin-landing-pages">
        <p>دسترسی به صفحات فرود مجاز نیست.</p>
        <button type="button" className="mt-3 rounded-xl border px-4 py-2" onClick={refresh}>تلاش دوباره</button>
      </main>
    );
  }

  return (
    <main data-testid="admin-landing-pages">
      <div className="mb-5 flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-black">صفحات فرود</h1>
          <p className="mt-1 text-sm text-muted">صفحات قابل انتشار فروشگاه را بسازید، پیش‌نمایش کنید و در صورت نیاز خانه را عوض کنید.</p>
        </div>
        <Link
          href="/admin/landing-pages/new"
          className="rounded-xl bg-[#2563EB] px-4 py-2 text-sm font-bold text-white"
          data-testid="landing-create"
        >
          ایجاد صفحه
        </Link>
      </div>

      <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
        {loading ? (
          <p className="p-6 text-sm text-muted">در حال بارگذاری…</p>
        ) : rows.length === 0 ? (
          <div className="p-10 text-center" data-testid="landing-empty">
            <p className="font-bold">هنوز صفحه‌ای ساخته نشده است.</p>
            <p className="mt-2 text-sm text-muted">با «ایجاد صفحه» یک پیش‌نویس بسازید و بخش‌ها را اضافه کنید.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[820px] text-sm">
              <thead className="bg-slate-50 text-right text-muted">
                <tr>
                  <th className="px-4 py-3 font-bold">عنوان</th>
                  <th className="px-4 py-3 font-bold">آدرس صفحه</th>
                  <th className="px-4 py-3 font-bold">زبان</th>
                  <th className="px-4 py-3 font-bold">وضعیت</th>
                  <th className="px-4 py-3 font-bold">صفحه اصلی؟</th>
                  <th className="px-4 py-3 font-bold">آخرین ویرایش</th>
                  <th className="px-4 py-3 font-bold">اقدامات</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const isHome = homePageId === row.pageId;
                  return (
                    <tr key={row.pageId} className="border-t border-border" data-testid={`landing-row-${row.slug}`}>
                      <td className="px-4 py-3 font-bold">{row.title}</td>
                      <td className="px-4 py-3" dir="ltr">/{row.slug}</td>
                      <td className="px-4 py-3">{localeLabel(row.locale)}</td>
                      <td className="px-4 py-3">
                        <span className={`rounded-full px-2 py-0.5 text-xs font-bold ${row.status === "Published" ? "bg-emerald-50 text-emerald-700" : "bg-amber-50 text-amber-700"}`}>
                          {row.status === "Published" ? "منتشرشده" : "پیش‌نویس"}
                        </span>
                      </td>
                      <td className="px-4 py-3">{isHome ? "بله" : "خیر"}</td>
                      <td className="px-4 py-3">{formatUpdated(row.updatedAt)}</td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-2">
                          <Link className="rounded-lg border px-2 py-1" href={`/admin/landing-pages/${row.pageId}`}>ویرایش</Link>
                          <Link className="rounded-lg border px-2 py-1" href={`/admin/landing-pages/${row.pageId}/preview`}>پیش‌نمایش</Link>
                          <button type="button" className="rounded-lg border px-2 py-1" disabled={busy} onClick={() => void publish(row)}>
                            {row.status === "Published" ? "بازگرداندن به پیش‌نویس" : "انتشار"}
                          </button>
                          <button type="button" className="rounded-lg border px-2 py-1" disabled={busy} onClick={() => void setHome(row)}>
                            {isHome ? "لغو خانه" : "انتخاب به‌عنوان صفحه اصلی"}
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
        {message ? <p className="px-4 py-3 text-sm text-red-600">{message}</p> : null}
      </section>
    </main>
  );
}
