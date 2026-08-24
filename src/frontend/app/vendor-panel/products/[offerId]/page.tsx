"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../../design-system";
import {
  formatMoney,
  formatOfferStatus,
  formatUnits,
  loadSellerOfferDetail,
  patchSellerOffer,
  readSellerPartyId,
  type HostReadSource,
  type SellerOfferDetail,
} from "../../seller-api";

/**
 * seam ویرایش Offer فروشنده؛ زمینهٔ Catalog فقط‌خواندنی است.
 */
export default function VendorProductDetailPage() {
  const params = useParams<{ offerId: string }>();
  const offerId = params.offerId;
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [detail, setDetail] = useState<SellerOfferDetail | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [sellerSku, setSellerSku] = useState("");
  const [status, setStatus] = useState("Active");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | undefined>(undefined);

  function refresh() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerOfferDetail(sellerPartyId, offerId).then((result) => {
      setSource(result.source);
      setDetail(result.detail);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
      if (result.detail) {
        setSellerSku(result.detail.sellerSku ?? "");
        setStatus(result.detail.status);
      }
    });
  }

  useEffect(() => {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerOfferDetail(sellerPartyId, offerId).then((result) => {
      setSource(result.source);
      setDetail(result.detail);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
      if (result.detail) {
        setSellerSku(result.detail.sellerSku ?? "");
        setStatus(result.detail.status);
      }
    });
  }, [offerId]);

  async function onSave() {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSaveError("seller.identity.missing");
      return;
    }
    setSaving(true);
    setSaveError(undefined);
    const result = await patchSellerOffer(sellerPartyId, offerId, { sellerSku, status });
    setSaving(false);
    if (!result.ok) {
      setSaveError(result.denied ? "دسترسی مجاز نیست" : result.errorCode);
      return;
    }
    setDetail(result.detail);
  }

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این پیشنهاد متعلق به فروشندهٔ دیگری است یا پیدا نشد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
        <Link className="mt-4 inline-flex text-primary underline" href="/vendor-panel/products">
          بازگشت به فهرست
        </Link>
      </main>
    );
  }

  return (
    <main>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / محصولات / ویرایش</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">ویرایش پیشنهاد</h1>
          <p className="mt-1 text-base text-muted">{detail?.productTitle ?? "…"}</p>
        </div>
        <Link className="text-sm text-primary underline-offset-4 hover:underline" href="/vendor-panel/products">
          بازگشت
        </Link>
      </div>
      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : detail ? (
        <div className="grid max-w-3xl gap-6">
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="text-base font-semibold">زمینهٔ Catalog (فقط‌خواندنی)</h2>
            <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2">
              <div>
                <dt className="text-muted">عنوان</dt>
                <dd className="mt-1 font-medium">{detail.productTitle}</dd>
              </div>
              <div>
                <dt className="text-muted">برند</dt>
                <dd className="mt-1 font-medium">{detail.brandName ?? "—"}</dd>
              </div>
              <div>
                <dt className="text-muted">کانال</dt>
                <dd className="mt-1 font-medium">{detail.channel === "Marketplace" ? "بازارگاه" : detail.channel}</dd>
              </div>
              <div>
                <dt className="text-muted">وضعیت Catalog</dt>
                <dd className="mt-1 font-medium">{detail.catalogReadOnly ? "فقط‌خواندنی" : "قابل ویرایش"}</dd>
              </div>
            </dl>
          </section>
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="text-base font-semibold">seam تجاری فروشنده</h2>
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <label className="flex flex-col gap-1 text-sm">
                SKU فروشنده
                <input
                  className="min-h-11 rounded-ds border border-border bg-surface px-3"
                  value={sellerSku}
                  onChange={(event) => setSellerSku(event.target.value)}
                  dir="ltr"
                />
              </label>
              <label className="flex flex-col gap-1 text-sm">
                وضعیت
                <select
                  className="min-h-11 rounded-ds border border-border bg-surface px-3"
                  value={status}
                  onChange={(event) => setStatus(event.target.value)}
                >
                  <option value="Active">فعال</option>
                  <option value="Suspended">معلق</option>
                </select>
              </label>
            </div>
            <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-3">
              <div>
                <dt className="text-muted">قیمت جاری Offer</dt>
                <dd className="mt-1 tabular-nums font-medium">{formatMoney(detail.amount, detail.currency)}</dd>
              </div>
              <div>
                <dt className="text-muted">موجودی قابل‌فروش</dt>
                <dd className="mt-1 tabular-nums font-medium">
                  {detail.availableUnits <= 0 ? "ناموجود" : formatUnits(detail.availableUnits)}
                </dd>
              </div>
              <div>
                <dt className="text-muted">وضعیت</dt>
                <dd className="mt-1 font-medium">{formatOfferStatus(detail.status)}</dd>
              </div>
            </dl>
            <p className="mt-3 text-sm text-muted">قیمت و موجودی در این slice فقط‌خواندنی نمایش داده می‌شوند.</p>
            {saveError ? <p className="mt-3 text-sm text-danger">{saveError}</p> : null}
            <button
              type="button"
              disabled={saving}
              onClick={() => void onSave()}
              className="mt-4 inline-flex min-h-11 items-center rounded-ds bg-primary px-5 text-sm font-medium text-primary-foreground shadow-sm disabled:opacity-50"
            >
              {saving ? "در حال ذخیره…" : "ذخیره تغییرات"}
            </button>
          </section>
        </div>
      ) : (
        <p className="text-muted">در حال بارگذاری…</p>
      )}
    </main>
  );
}
