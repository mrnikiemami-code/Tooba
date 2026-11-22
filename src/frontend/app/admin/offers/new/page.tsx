"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { ErrorState } from "../../../../design-system";
import { useLocale, useLocalizedPath } from "../../../../lib/i18n/locale-context";
import { queryAdminSellersGrid, type AdminSellerRow } from "../../../../features/admin-sellers/api/sellers-api";
import { createAdminOffer, loadAdminCatalogVariants, type AdminCatalogVariant } from "../../admin-offer-api";
import { prepareAdminDevActor } from "../../admin-api";
import { adminOfferIntent, clearAdminOfferIntent } from "../../offer-create-intent";

export default function AdminCreateOfferPage() {
  const router = useRouter();
  const locale = useLocale();
  const localizePath = useLocalizedPath();
  const isEn = locale === "en";
  const t = (fa: string, en: string) => (isEn ? en : fa);
  const [sellers, setSellers] = useState<AdminSellerRow[]>([]);
  const [sellerPartyId, setSellerPartyId] = useState("");
  const [sellerSearch, setSellerSearch] = useState("");
  const [variants, setVariants] = useState<AdminCatalogVariant[]>([]);
  const [variantSearch, setVariantSearch] = useState("");
  const [variantPage, setVariantPage] = useState(1);
  const [variantTotal, setVariantTotal] = useState(0);
  const [variantHasMore, setVariantHasMore] = useState(false);
  const [catalogVariantId, setCatalogVariantId] = useState("");
  const [selectedTitle, setSelectedTitle] = useState("");
  const [sellerSku, setSellerSku] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const [createdId, setCreatedId] = useState<string | undefined>(undefined);

  const selectedSeller = useMemo(() => sellers.find((row) => row.sellerPartyId === sellerPartyId), [sellers, sellerPartyId]);

  function loadSellers(search = sellerSearch) {
    void queryAdminSellersGrid({ page: 1, pageSize: 50, sorts: [], filters: {}, search: search.trim() || undefined }).then((result) => {
      if (result.source === "host") {
        setSellers(result.page.rows);
        if (!sellerPartyId && result.page.rows[0]) setSellerPartyId(result.page.rows[0].sellerPartyId);
      }
    });
  }

  function loadVariants(search = variantSearch, page = variantPage) {
    void loadAdminCatalogVariants(search, page, 20).then((result) => {
      if (result.source !== "host") {
        setError(result.message ?? "admin.catalog-variants.error");
        return;
      }
      setVariants(result.rows);
      setVariantTotal(result.totalCount);
      setVariantHasMore(result.hasMore);
      setVariantPage(result.page);
      if (!catalogVariantId && result.rows[0]) {
        setCatalogVariantId(result.rows[0].catalogVariantId);
        setSelectedTitle(result.rows[0].productTitle);
      }
    });
  }

  useEffect(() => {
    void prepareAdminDevActor().then(() => {
      loadSellers("");
      loadVariants("", 1);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function onCreate() {
    if (!sellerPartyId) {
      setError(t("فروشنده را انتخاب کنید", "Select a seller"));
      return;
    }
    if (!catalogVariantId) {
      setError(t("گونهٔ Catalog را انتخاب کنید", "Select a Catalog variant"));
      return;
    }
    const scope = `admin:${sellerPartyId}:${catalogVariantId}`;
    let requestId: string;
    try {
      requestId = adminOfferIntent(scope);
    } catch {
      setError(t("این مرورگر نمی‌تواند درخواست را حفظ کند؛ چیزی نوشته نشد.", "This browser cannot retain the intent; nothing was written."));
      return;
    }
    setBusy(true);
    setError(undefined);
    const created = await createAdminOffer({ sellerPartyId, catalogVariantId, sellerSku: sellerSku.trim() || null, requestId });
    setBusy(false);
    if (!created.ok) {
      setError(created.denied ? t("دسترسی مجاز نیست", "Access denied") : created.errorCode);
      return;
    }
    clearAdminOfferIntent(scope);
    setCreatedId(created.detail.offerId);
    router.push(localizePath(`/admin/offers/${created.detail.offerId}`));
  }

  return (
    <main className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Link className="text-sm text-[#2563EB]" href="/admin/offers">{t("پیشنهادها", "Offers")}</Link>
          <h1 className="mt-1 text-xl font-bold text-gray-900">{t("ایجاد پیش‌نویس برای فروشنده", "Create a draft for a seller")}</h1>
          <p className="text-sm text-gray-500">{t("فروشنده، محصول منتشرشده و پیش‌نویس؛ بدون جعل هویت فروشنده.", "Choose seller, published product and Draft; no seller impersonation.")}</p>
        </div>
      </div>
      {error ? <ErrorState title={t("ذخیره انجام نشد", "Save failed")} detail={error} onRetry={() => setError(undefined)} retryLabel={t("بستن", "Dismiss")} /> : null}
      <section className="grid gap-4 rounded-2xl border border-gray-200 bg-white p-4 shadow-sm lg:grid-cols-2">
        <div className="space-y-2">
          <h2 className="font-semibold">{t("فروشنده", "Seller")}</h2>
          <div className="flex gap-2">
            <input className="min-h-11 flex-1 rounded-xl border border-gray-200 px-3 placeholder:text-gray-600" value={sellerSearch} onChange={(event) => setSellerSearch(event.target.value)} placeholder={t("جستجوی فروشنده", "Search seller")} data-testid="admin-offer-seller-search" />
            <button type="button" className="min-h-11 rounded-xl border border-gray-200 px-3" onClick={() => loadSellers(sellerSearch)}>{t("جست‌وجو", "Search")}</button>
          </div>
          <div className="max-h-56 space-y-2 overflow-y-auto">
            {sellers.map((seller) => (
              <button key={seller.sellerPartyId} type="button" aria-pressed={seller.sellerPartyId === sellerPartyId} onClick={() => setSellerPartyId(seller.sellerPartyId)} className={`min-h-11 w-full rounded-xl border px-3 text-start text-sm ${seller.sellerPartyId === sellerPartyId ? "border-[#2563EB] bg-blue-50" : "border-gray-200"}`} data-testid="admin-offer-seller">
                {seller.displayName}
              </button>
            ))}
          </div>
          <p className="text-xs text-gray-500">{t("فروشندهٔ انتخابی", "Selected seller")}: {selectedSeller?.displayName ?? "—"}</p>
        </div>
        <div className="space-y-2">
          <h2 className="font-semibold">{t("محصول منتشرشده", "Published product")}</h2>
          <div className="flex gap-2">
            <input className="min-h-11 flex-1 rounded-xl border border-gray-200 px-3 placeholder:text-gray-600" value={variantSearch} onChange={(event) => setVariantSearch(event.target.value)} placeholder={t("جستجوی نام یا کد Catalog", "Search name or Catalog code")} data-testid="admin-offer-variant-search" />
            <button type="button" data-testid="admin-offer-variant-search-submit" className="min-h-11 rounded-xl border border-gray-200 px-3" onClick={() => loadVariants(variantSearch, 1)}>{t("جست‌وجو", "Search")}</button>
            <button type="button" className="min-h-11 rounded-xl border border-gray-200 px-3" disabled={!variantHasMore} onClick={() => loadVariants(variantSearch, variantPage + 1)} data-testid="admin-offer-variant-next">{t("بعدی", "Next")}</button>
          </div>
          <div className="max-h-56 space-y-2 overflow-y-auto">
            {variants.map((variant) => (
              <button key={variant.catalogVariantId} type="button" aria-pressed={variant.catalogVariantId === catalogVariantId} onClick={() => { setCatalogVariantId(variant.catalogVariantId); setSelectedTitle(variant.productTitle); }} className={`min-h-11 w-full rounded-xl border px-3 text-start text-sm ${variant.catalogVariantId === catalogVariantId ? "border-[#2563EB] bg-blue-50" : "border-gray-200"}`} data-testid="admin-offer-variant">
                <span className="block font-medium">{variant.productTitle}</span>
                <span className="block text-xs text-gray-500" dir="ltr">{variant.catalogCode ?? "—"}</span>
              </button>
            ))}
          </div>
          <p className="text-xs text-gray-500" data-testid="admin-offer-variant-page">{t(`صفحهٔ ${variantPage} · ${variantTotal} نتیجه`, `Page ${variantPage} · ${variantTotal} results`)} · {selectedTitle || "—"}</p>
        </div>
        <label className="flex flex-col gap-1 text-sm lg:col-span-2">
          {t("SKU فروشنده", "Seller SKU")}
          <input className="min-h-11 rounded-xl border border-gray-200 px-3" value={sellerSku} onChange={(event) => setSellerSku(event.target.value)} dir="ltr" data-testid="admin-offer-sku" />
        </label>
        {createdId ? <p className="text-sm text-amber-800 lg:col-span-2" data-testid="admin-offer-created">{t("پیش‌نویس ساخته شد", "Draft created")}: <Link className="underline" href={`/admin/offers/${createdId}`}>{createdId}</Link></p> : null}
        <div className="flex gap-2 lg:col-span-2">
          <button type="button" className="min-h-11 rounded-xl bg-[#2563EB] px-4 text-sm font-bold text-white disabled:opacity-50" disabled={busy} onClick={() => void onCreate()} data-testid="admin-offer-submit">{busy ? t("در حال ذخیره…", "Saving…") : t("ذخیرهٔ پیش‌نویس", "Save draft")}</button>
          <Link className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-4 text-sm" href="/admin/offers" data-testid="admin-offer-cancel">{t("انصراف", "Cancel")}</Link>
        </div>
      </section>
    </main>
  );
}




