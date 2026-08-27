"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../../design-system";
import {
  createSellerOffer,
  loadSellerCatalogVariants,
  readSellerPartyId,
  writeSellerOfferInventory,
  writeSellerOfferPrice,
  type HostReadSource,
  type SellerCatalogVariantOption,
} from "../../seller-api";

/**
 * ایجاد Offer روی گونهٔ Catalog موجود + قیمت Pricing + موجودی Inventory.
 */
export default function VendorCreateOfferPage() {
  const router = useRouter();
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [variants, setVariants] = useState<SellerCatalogVariantOption[]>([]);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [catalogVariantId, setCatalogVariantId] = useState("");
  const [sellerSku, setSellerSku] = useState("");
  const [status, setStatus] = useState("Active");
  const [amount, setAmount] = useState("");
  const [onHand, setOnHand] = useState("1");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | undefined>(undefined);

  function refresh() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerCatalogVariants(sellerPartyId).then((result) => {
      setSource(result.source);
      setVariants(result.rows);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
      if (result.rows[0] && !catalogVariantId) {
        setCatalogVariantId(result.rows[0].catalogVariantId);
      }
    });
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function onCreate() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSaveError("seller.identity.missing");
      return;
    }
    if (!catalogVariantId) {
      setSaveError("گونهٔ Catalog را انتخاب کنید");
      return;
    }
    const parsedAmount = Number(amount);
    const parsedOnHand = Number(onHand);
    if (!Number.isFinite(parsedAmount) || parsedAmount < 0) {
      setSaveError("مبلغ نامعتبر است");
      return;
    }
    if (!Number.isInteger(parsedOnHand) || parsedOnHand < 0) {
      setSaveError("موجودی نامعتبر است");
      return;
    }

    setSaving(true);
    setSaveError(undefined);
    const created = await createSellerOffer(sellerPartyId, {
      catalogVariantId,
      sellerSku: sellerSku.trim() || null,
      status,
    });
    if (!created.ok) {
      setSaving(false);
      setSaveError(created.denied ? "دسترسی مجاز نیست" : created.errorCode);
      return;
    }
    const priceResult = await writeSellerOfferPrice(sellerPartyId, created.detail.offerId, {
      amount: parsedAmount,
    });
    if (!priceResult.ok) {
      setSaving(false);
      setSaveError(priceResult.errorCode);
      return;
    }
    const inventoryResult = await writeSellerOfferInventory(sellerPartyId, created.detail.offerId, {
      onHand: parsedOnHand,
      reason: "vendor-panel-create",
    });
    setSaving(false);
    if (!inventoryResult.ok) {
      setSaveError(inventoryResult.errorCode);
      return;
    }
    router.push(`/vendor-panel/products/${created.detail.offerId}`);
  }

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این Actor مجوز ایجاد پیشنهاد برای این فروشنده را ندارد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
      </main>
    );
  }

  return (
    <main>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / محصولات / پیشنهاد جدید</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">ایجاد پیشنهاد فروش</h1>
          <p className="mt-1 text-base text-muted">Catalog فقط‌خواندنی؛ Offer + قیمت + موجودی برای همین فروشنده</p>
        </div>
        <Link className="text-sm text-[#E53935] underline-offset-4 hover:underline" href="/vendor-panel/products">
          بازگشت
        </Link>
      </div>

      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : (
        <section className="max-w-2xl overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
          <div className="border-b border-gray-100 bg-gradient-to-l from-[#E53935]/5 to-transparent p-5">
            <h2 className="text-base font-semibold">فرم پیشنهاد</h2>
            <p className="mt-1 text-sm text-muted">
              محصول Catalog باید از قبل توسط ادمین ساخته شده باشد
            </p>
          </div>
          <div className="grid gap-4 p-5 sm:grid-cols-2">
            <label className="flex flex-col gap-1 text-sm sm:col-span-2">
              گونهٔ Catalog
              <select
                className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                value={catalogVariantId}
                onChange={(event) => setCatalogVariantId(event.target.value)}
                disabled={variants.length === 0}
              >
                {variants.length === 0 ? <option value="">گونه‌ای موجود نیست</option> : null}
                {variants.map((row) => (
                  <option key={row.catalogVariantId} value={row.catalogVariantId}>
                    {row.productTitle}
                    {row.catalogCode ? ` · ${row.catalogCode}` : ""}
                  </option>
                ))}
              </select>
            </label>
            <label className="flex flex-col gap-1 text-sm">
              SKU فروشنده
              <input
                className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                value={sellerSku}
                onChange={(event) => setSellerSku(event.target.value)}
                dir="ltr"
              />
            </label>
            <label className="flex flex-col gap-1 text-sm">
              وضعیت اولیه
              <select
                className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                value={status}
                onChange={(event) => setStatus(event.target.value)}
              >
                <option value="Active">فعال</option>
                <option value="Draft">پیش‌نویس</option>
              </select>
            </label>
            <label className="flex flex-col gap-1 text-sm">
              قیمت بدون مالیات (ریال)
              <input
                className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3 tabular-nums focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                inputMode="numeric"
                dir="ltr"
                placeholder="مثلاً 1850000"
              />
            </label>
            <label className="flex flex-col gap-1 text-sm">
              موجودی روی‌دست
              <input
                className="min-h-11 rounded-xl border border-gray-200 bg-gray-50 px-3 tabular-nums focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                value={onHand}
                onChange={(event) => setOnHand(event.target.value)}
                inputMode="numeric"
                dir="ltr"
              />
            </label>
          </div>
          {saveError ? <p className="px-5 text-sm text-danger">{saveError}</p> : null}
          <div className="flex flex-wrap gap-3 border-t border-border p-5">
            <button
              type="button"
              disabled={saving || variants.length === 0}
              onClick={() => void onCreate()}
              className="inline-flex min-h-11 items-center rounded-xl bg-[#E53935] px-5 text-sm font-bold text-white shadow-lg shadow-[#E53935]/30 hover:bg-[#c62828] disabled:opacity-50"
            >
              {saving ? "در حال ایجاد…" : "ایجاد پیشنهاد"}
            </button>
            <Link
              href="/vendor-panel/products"
              className="inline-flex min-h-11 items-center rounded-xl border border-gray-200 px-5 text-sm text-gray-700"
            >
              انصراف
            </Link>
          </div>
        </section>
      )}
    </main>
  );
}
