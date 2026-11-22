"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../../design-system";
import { useLocale, useLocalizedPath } from "../../../../lib/i18n/locale-context";
import {
  createSellerOffer,
  loadSellerCatalogVariants,
  readSellerPartyId,
  writeSellerOfferInventory,
  writeSellerOfferPrice,
  type HostReadSource,
  type SellerCatalogVariantOption,
} from "../../seller-api";
import { clearOfferIntent, readOrCreateOfferIntent } from "../../offer-create-intent";

export default function VendorCreateOfferPage() {
  const router = useRouter();
  const locale = useLocale();
  const localizePath = useLocalizedPath();
  const isEn = locale === "en";
  const t = (fa: string, en: string) => (isEn ? en : fa);
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [variants, setVariants] = useState<SellerCatalogVariantOption[]>([]);
  const [pickerSearch, setPickerSearch] = useState("");
  const [pickerPage, setPickerPage] = useState(1);
  const [pickerTotal, setPickerTotal] = useState(0);
  const [pickerHasMore, setPickerHasMore] = useState(false);
  const [pickerLoading, setPickerLoading] = useState(false);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [catalogVariantId, setCatalogVariantId] = useState("");
  const [selectedTitle, setSelectedTitle] = useState("");
  const [sellerSku, setSellerSku] = useState("");
  const [amount, setAmount] = useState("");
  const [onHand, setOnHand] = useState("1");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | undefined>(undefined);
  const [createdOfferId, setCreatedOfferId] = useState<string | undefined>(undefined);

  const [sellerPartyId, setSellerPartyId] = useState("");

  function loadVariantsWith(sellerId: string, search = pickerSearch, page = pickerPage) {
    if (!sellerId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    setPickerLoading(true);
    void loadSellerCatalogVariants(sellerId, search, page, 20).then((result) => {
      setSource(result.source);
      setVariants(result.rows);
      setPickerTotal(result.totalCount);
      setPickerHasMore(result.hasMore);
      setPickerPage(result.page);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
      setPickerLoading(false);
      if (!catalogVariantId && result.rows[0]) {
        setCatalogVariantId(result.rows[0].catalogVariantId);
        setSelectedTitle(result.rows[0].productTitle);
      }
    });
  }

  function loadVariants(search = pickerSearch, page = pickerPage) {
    loadVariantsWith(sellerPartyId, search, page);
  }

  useEffect(() => {
    const id = readSellerPartyId(window.location.search) ?? "";
    setSellerPartyId(id);
    loadVariantsWith(id, "", 1);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function onCreate() {
    if (!sellerPartyId) {
      setSaveError("seller.identity.missing");
      return;
    }
    if (!catalogVariantId) {
      setSaveError(t("گونهٔ Catalog را انتخاب کنید", "Choose a Catalog variant"));
      return;
    }
    const parsedAmount = amount.trim() ? Number(amount) : null;
    const parsedOnHand = onHand.trim() ? Number(onHand) : null;
    if (parsedAmount != null && (!Number.isFinite(parsedAmount) || parsedAmount < 0)) {
      setSaveError(t("مبلغ نامعتبر است", "Invalid price"));
      return;
    }
    if (parsedOnHand != null && (!Number.isInteger(parsedOnHand) || parsedOnHand < 0)) {
      setSaveError(t("موجودی نامعتبر است", "Invalid stock"));
      return;
    }
    const scope = `${sellerPartyId}:${catalogVariantId}`;
    let requestId: string;
    try {
      requestId = readOrCreateOfferIntent(scope);
    } catch {
      setSaveError(t("ذخیرهٔ درخواست در این مرورگر ممکن نیست؛ هیچ نوشتنی انجام نشد.", "This browser cannot retain the create intent; nothing was written."));
      return;
    }

    setSaving(true);
    setSaveError(undefined);
    const created = await createSellerOffer(sellerPartyId, {
      catalogVariantId,
      sellerSku: sellerSku.trim() || null,
      status: "Draft",
      requestId,
    });
    if (!created.ok) {
      setSaving(false);
      setSaveError(created.denied ? t("دسترسی مجاز نیست", "Access denied") : created.errorCode);
      return;
    }
    setCreatedOfferId(created.detail.offerId);
    let latest = created.detail;
    if (parsedAmount != null && latest.amount !== parsedAmount) {
      const priceResult = await writeSellerOfferPrice(sellerPartyId, latest.offerId, { amount: parsedAmount });
      if (!priceResult.ok) {
        setSaving(false);
        setSaveError(t("پیش‌نویس ساخته شد؛ قیمت کامل نشد. با همین پیش‌نویس ادامه دهید.", "Draft created; price is incomplete. Resume the same draft."));
        return;
      }
      latest = priceResult.detail;
    }
    if (parsedOnHand != null && latest.onHand !== parsedOnHand) {
      const inventoryResult = await writeSellerOfferInventory(sellerPartyId, latest.offerId, {
        onHand: parsedOnHand,
        reason: "vendor-panel-create",
      });
      if (!inventoryResult.ok) {
        setSaving(false);
        setSaveError(t("پیش‌نویس ساخته شد؛ موجودی کامل نشد. با همین پیش‌نویس ادامه دهید.", "Draft created; stock is incomplete. Resume the same draft."));
        return;
      }
      latest = inventoryResult.detail;
    }
    clearOfferIntent(scope);
    setSaving(false);
    router.push(localizePath(`/vendor-panel/products/${latest.offerId}`));
  }

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title={t("دسترسی مجاز نیست", "Access denied")}
          detail={t("این Actor مجوز ایجاد پیشنهاد برای این فروشنده را ندارد.", "This actor cannot create an offer for this seller.")}
          onRetry={() => loadVariants("", 1)}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  return (
    <main>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-sm text-muted">{t("خانه / محصولات / پیشنهاد جدید", "Home / Products / New offer")}</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">{t("ایجاد پیش‌نویس پیشنهاد فروش", "Create a seller offer draft")}</h1>
          <p className="mt-1 text-base text-muted">
            {t("Catalog فقط‌خواندنی است؛ Offer، قیمت و موجودی مالک جدا دارند.", "Catalog is read-only; Offer, price and stock keep separate owners.")}
          </p>
        </div>
        <Link className="text-sm text-[#b71c1c] underline-offset-4 hover:underline" href="/vendor-panel/products">
          {t("بازگشت", "Back")}
        </Link>
      </div>

      {source === "error" ? (
        <ErrorState title={t("Host در دسترس نیست", "Host unavailable")} detail={message} onRetry={() => loadVariants("", 1)} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <section className="max-w-3xl overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
          <div className="border-b border-gray-100 bg-gradient-to-l from-[#E53935]/5 to-transparent p-5">
            <h2 className="text-base font-semibold">{t("فرم پیش‌نویس", "Draft form")}</h2>
            <p className="mt-1 text-sm text-muted">{t("محصول منتشرشده را جست‌وجو کنید؛ صفحه و انتخاب حفظ می‌شود.", "Search published products; paging and selection are retained.")}</p>
          </div>
          <div className="grid gap-4 p-5">
            <div className="grid gap-2 sm:grid-cols-[minmax(0,1fr)_auto_auto]">
              <label className="flex flex-col gap-1 text-sm">
                {t("جستجوی محصول یا کد Catalog", "Search product or Catalog code")}
                <input
                  className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3 placeholder:text-gray-600 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                  value={pickerSearch}
                  onChange={(event) => setPickerSearch(event.target.value)}
                  onKeyDown={(event) => { if (event.key === "Enter") { event.preventDefault(); loadVariants(pickerSearch, 1); } }}
                  data-testid="seller-catalog-search"
                />
              </label>
              <button type="button" data-testid="seller-catalog-search-submit" className="min-h-11 self-end rounded-xl border border-gray-200 px-4 text-sm" onClick={() => loadVariants(pickerSearch, 1)}>
                {t("جست‌وجو", "Search")}
              </button>
              <button type="button" className="min-h-11 self-end rounded-xl border border-gray-200 px-4 text-sm" disabled={pickerLoading || !pickerHasMore} onClick={() => loadVariants(pickerSearch, pickerPage + 1)} data-testid="seller-catalog-next">
                {t("صفحهٔ بعد", "Next page")}
              </button>
            </div>
            <div className="grid max-h-64 gap-2 overflow-y-auto" data-testid="seller-catalog-results">
              {variants.length === 0 ? <p className="rounded-lg border border-dashed border-border p-3 text-sm text-muted">{t("نتیجه‌ای پیدا نشد.", "No result found.")}</p> : null}
              {variants.map((row) => {
                const active = row.catalogVariantId === catalogVariantId;
                return (
                  <button
                    key={row.catalogVariantId}
                    type="button"
                    aria-pressed={active}
                    data-testid="seller-catalog-result"
                    onClick={() => { setCatalogVariantId(row.catalogVariantId); setSelectedTitle(row.productTitle); }}
                    className={`min-h-11 rounded-xl border px-3 py-2 text-start text-sm ${active ? "border-[#E53935] bg-[#E53935]/5" : "border-gray-200 bg-gray-50"}`}
                  >
                    <span className="block font-medium">{row.productTitle}</span>
                    <span className="block text-xs text-muted" dir="ltr">{row.catalogCode ?? "—"}</span>
                  </button>
                );
              })}
            </div>
            <p className="text-xs text-muted" data-testid="seller-catalog-page">
              {t(`صفحهٔ ${pickerPage} · ${pickerTotal} نتیجه · انتخاب: ${selectedTitle || "—"}`, `Page ${pickerPage} · ${pickerTotal} results · selected: ${selectedTitle || "—"}`)}
            </p>
            <div className="grid gap-4 sm:grid-cols-2">
              <label className="flex flex-col gap-1 text-sm">
                {t("SKU فروشنده", "Seller SKU")}
                <input className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3" value={sellerSku} onChange={(event) => setSellerSku(event.target.value)} dir="ltr" data-testid="seller-create-sku" />
              </label>
              <div className="rounded-xl border border-border bg-gray-50 p-3 text-sm">
                <p className="font-medium">{t("وضعیت اولیه", "Initial status")}</p>
                <p className="mt-1 text-muted">{t("پیش‌نویس؛ فعال‌سازی مرحلهٔ جداگانه است.", "Draft; activation is a separate step.")}</p>
              </div>
              <label className="flex flex-col gap-1 text-sm">
                {t("قیمت بدون مالیات (ریال)", "Price before tax (IRR)")}
                <input className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3 tabular-nums placeholder:text-gray-700" value={amount} onChange={(event) => setAmount(event.target.value)} inputMode="numeric" dir="ltr" data-testid="seller-create-price" placeholder={t("اختیاری برای پیش‌نویس", "Optional for a draft")} />
              </label>
              <label className="flex flex-col gap-1 text-sm">
                {t("موجودی اولیه", "Initial stock")}
                <input className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3 tabular-nums" value={onHand} onChange={(event) => setOnHand(event.target.value)} inputMode="numeric" dir="ltr" data-testid="seller-create-stock" />
              </label>
            </div>
            {saveError ? <p className="text-sm text-danger" data-testid="seller-create-error">{saveError}</p> : null}
            {createdOfferId ? (
              <p className="rounded-xl border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900" data-testid="seller-create-partial">
                {t("پیش‌نویس ساخته شد و برای تکمیل باقی مراحل حفظ شده است.", "The draft was created and retained for remaining steps.")}{" "}
                <Link className="underline" href={`/vendor-panel/products/${createdOfferId}`}>{t("ادامه در ویرایش", "Continue in edit")}</Link>
              </p>
            ) : null}
          </div>
          <div className="flex flex-wrap gap-3 border-t border-border p-5">
            <button type="button" disabled={saving || variants.length === 0} onClick={() => void onCreate()} className="inline-flex min-h-11 items-center rounded-xl bg-[#c62828] px-5 text-sm font-bold text-white shadow-lg shadow-[#c62828]/30 hover:bg-[#b71c1c] disabled:opacity-50" data-testid="seller-create-submit">
              {saving ? t("در حال ذخیره…", "Saving…") : t("ذخیرهٔ پیش‌نویس", "Save draft")}
            </button>
            <Link href="/vendor-panel/products" className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-5 text-sm text-gray-700" data-testid="seller-create-cancel">
              {t("انصراف", "Cancel")}
            </Link>
          </div>
        </section>
      )}
    </main>
  );
}







