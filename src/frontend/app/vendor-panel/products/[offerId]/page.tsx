"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../../design-system";
import {
  formatMoney,
  formatOfferStatus,
  formatUnits,
  loadSellerOfferDetail,
  loadSellerReservationPolicy,
  patchSellerOffer,
  readSellerPartyId,
  writeSellerOfferInventory,
  writeSellerOfferPrice,
  type HostReadSource,
  type SellerOfferDetail,
} from "../../seller-api";
import { SellerProductAttributesPanel } from "../../../admin/catalog-attribute-ui";
import { ReservationPolicyEditor, draftFromView, type ReservationPolicyDraft } from "../../../admin/reservation-policy-editor.tsx";
import type { ReservationPolicyEditorView } from "../../../admin/reservation-policy-api";

/**
 * seam ویرایش Offer فروشنده؛ زمینهٔ Catalog فقط‌خواندنی است؛ قیمت و موجودی از Pricing/Inventory.
 */
export default function VendorProductDetailPage() {
  const params = useParams<{ offerId: string }>();
  const router = useRouter();
  const offerId = params.offerId;
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [detail, setDetail] = useState<SellerOfferDetail | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [sellerSku, setSellerSku] = useState("");
  const [status, setStatus] = useState("Active");
  const [amount, setAmount] = useState("");
  const [inventoryDrafts, setInventoryDrafts] = useState<Record<string, string>>({});
  const [saveNotice, setSaveNotice] = useState<string | undefined>(undefined);
  const [returnPolicyChoice, setReturnPolicyChoice] = useState("Default");
  const [customReturnWindowDays, setCustomReturnWindowDays] = useState("");
  const [minimumOrderQuantity, setMinimumOrderQuantity] = useState("");
  const [maximumOrderQuantity, setMaximumOrderQuantity] = useState("");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | undefined>(undefined);
  const [reservation, setReservation] = useState<ReservationPolicyEditorView | null>(null);
  const [reservationDraft, setReservationDraft] = useState<ReservationPolicyDraft>({
    initial: "",
    retry: "",
    max: "",
    inheritInitial: true,
    inheritRetry: true,
    inheritMax: true,
  });

  function applyDetail(next: SellerOfferDetail) {
    setDetail(next);
    setSellerSku(next.sellerSku ?? "");
    setStatus(next.status);
    setAmount(next.amount != null ? String(next.amount) : "");
    setInventoryDrafts(Object.fromEntries(next.inventoryLocations.map((row) => [row.stockItemId, String(row.onHand)])));
    setReturnPolicyChoice(next.returnPolicyChoice || "Default");
    setCustomReturnWindowDays(
      next.customReturnWindowDays != null ? String(next.customReturnWindowDays) : "",
    );
    setMinimumOrderQuantity(next.minimumOrderQuantity != null ? String(next.minimumOrderQuantity) : "");
    setMaximumOrderQuantity(next.maximumOrderQuantity != null ? String(next.maximumOrderQuantity) : "");
  }

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
        applyDetail(result.detail);
      }
    });
    void loadSellerReservationPolicy(sellerPartyId, offerId).then((policy) => {
      if (policy.ok) {
        setReservation(policy.data);
        setReservationDraft(draftFromView(policy.data));
      }
    });
  }

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- load once per offerId
  }, [offerId]);

  function parseNumber(value: string): number | null {
    const text = value.trim();
    return text ? Number(text.replace(",", ".")) : null;
  }

  function hasChanges(): boolean {
    if (!detail) return false;
    const currentMin = parseNumber(minimumOrderQuantity);
    const currentMax = parseNumber(maximumOrderQuantity);
    const locationChanged = detail.inventoryLocations.some(
      (row) => (inventoryDrafts[row.stockItemId] ?? "") !== String(row.onHand),
    );
    return (
      (sellerSku ?? "") !== (detail.sellerSku ?? "") ||
      status !== detail.status ||
      returnPolicyChoice !== detail.returnPolicyChoice ||
      (returnPolicyChoice === "Custom" ? parseNumber(customReturnWindowDays) : null) !== detail.customReturnWindowDays ||
      currentMin !== detail.minimumOrderQuantity ||
      currentMax !== detail.maximumOrderQuantity ||
      parseNumber(amount) !== detail.amount ||
      locationChanged
    );
  }

  async function reconcileVersion(expected: (next: SellerOfferDetail) => boolean) {
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSaveError("هویت فروشنده مشخص نیست.");
      return;
    }
    const result = await loadSellerOfferDetail(sellerPartyId, offerId);
    if (result.detail && expected(result.detail)) {
      setDetail(result.detail);
      setSaveNotice("نتیجهٔ اولیه نامشخص بود؛ بازخوانی مستقل تغییر را تأیید کرد.");
      setSaveError(undefined);
      return;
    }
    setSaveError("نتیجهٔ ذخیره نامشخص است. صفحه را بازخوانی کنید و مقدار را بررسی کنید.");
  }

  async function onSave() {
    if (!detail) return;
    const sellerPartyId = readSellerPartyId(window.location.search);
    if (!sellerPartyId) {
      setSaveError("هویت فروشنده مشخص نیست.");
      return;
    }
    if (!hasChanges()) {
      setSaveNotice("تغییری برای ذخیره وجود ندارد.");
      setSaveError(undefined);
      return;
    }
    const parsedAmount = parseNumber(amount);
    const parsedMin = parseNumber(minimumOrderQuantity);
    const parsedMax = parseNumber(maximumOrderQuantity);
    if (parsedAmount == null || !Number.isFinite(parsedAmount) || parsedAmount < 0) {
      setSaveError("مبلغ نامعتبر است");
      return;
    }
    if (parsedMin != null && !(Number.isFinite(parsedMin) && parsedMin > 0)) {
      setSaveError("حداقل مقدار خرید نامعتبر است");
      return;
    }
    if (parsedMax != null && !(Number.isFinite(parsedMax) && parsedMax > 0)) {
      setSaveError("حداکثر مقدار خرید نامعتبر است");
      return;
    }
    if (parsedMin != null && parsedMax != null && parsedMin > parsedMax) {
      setSaveError("حداقل مقدار خرید نباید از حداکثر بیشتر باشد");
      return;
    }
    const parsedCustomDays =
      returnPolicyChoice === "Custom" && customReturnWindowDays.trim()
        ? Number(customReturnWindowDays)
        : null;
    if (returnPolicyChoice === "Custom" && (!Number.isInteger(parsedCustomDays) || (parsedCustomDays ?? 0) <= 0)) {
      setSaveError("مهلت اختصاصی مرجوعی نامعتبر است");
      return;
    }
    const inventoryChanges = detail.inventoryLocations
      .map((row) => ({ row, value: parseNumber(inventoryDrafts[row.stockItemId] ?? "") }))
      .filter(({ row, value }) => value !== row.onHand);
    if (inventoryChanges.some(({ value }) => value == null || !Number.isFinite(value) || value < 0)) {
      setSaveError("موجودی یک یا چند محل نامعتبر است");
      return;
    }
    if (inventoryChanges.some(({ row, value }) => (value ?? 0) < row.reserved)) {
      setSaveError("موجودی هر محل نباید کمتر از مقدار رزرو همان محل باشد");
      return;
    }

    setSaving(true);
    setSaveError(undefined);
    setSaveNotice(undefined);
    let latest = detail;
    const commercialChanged =
      (sellerSku ?? "") !== (detail.sellerSku ?? "") ||
      status !== detail.status ||
      returnPolicyChoice !== detail.returnPolicyChoice ||
      (returnPolicyChoice === "Custom" ? parsedCustomDays : null) !== detail.customReturnWindowDays ||
      parsedMin !== detail.minimumOrderQuantity ||
      parsedMax !== detail.maximumOrderQuantity;
    if (commercialChanged) {
      const patchResult = await patchSellerOffer(sellerPartyId, offerId, {
        sellerSku,
        status: status === detail.status ? undefined : status,
        returnPolicyChoice,
        customReturnWindowDays: parsedCustomDays,
        minimumOrderQuantity: parsedMin,
        maximumOrderQuantity: parsedMax,
        clearOrderQuantityLimits:
          parsedMin == null &&
          parsedMax == null &&
          (detail.minimumOrderQuantity != null || detail.maximumOrderQuantity != null),
      });
      if (!patchResult.ok) {
        setSaving(false);
        setSaveError(patchResult.denied ? "دسترسی مجاز نیست" : patchResult.errorCode);
        if (patchResult.errorCode === "host-unreachable" || patchResult.errorCode.endsWith("mismatch")) {
          await reconcileVersion((next) =>
            (next.sellerSku ?? "") === (sellerSku ?? "") &&
            next.status === (status === detail.status ? next.status : status),
          );
        }
        return;
      }
      latest = patchResult.detail;
      setDetail(latest);
    }
    if (parsedAmount !== detail.amount) {
      const priceResult = await writeSellerOfferPrice(sellerPartyId, offerId, { amount: parsedAmount });
      if (!priceResult.ok) {
        setSaving(false);
        setSaveError(priceResult.denied ? "دسترسی مجاز نیست" : priceResult.errorCode);
        if (priceResult.errorCode === "host-unreachable" || priceResult.errorCode.endsWith("mismatch")) {
          await reconcileVersion((next) => next.amount === parsedAmount);
        }
        return;
      }
      latest = priceResult.detail;
      setDetail(latest);
    }
    for (const { row, value } of inventoryChanges) {
      const inventoryResult = await writeSellerOfferInventory(sellerPartyId, offerId, {
        onHand: value!,
        reason: "vendor-panel-edit",
        stockItemId: row.stockItemId,
      });
      if (!inventoryResult.ok) {
        setSaving(false);
        setSaveError(inventoryResult.denied ? "دسترسی مجاز نیست" : inventoryResult.errorCode);
        if (inventoryResult.errorCode === "host-unreachable" || inventoryResult.errorCode.endsWith("mismatch")) {
          await reconcileVersion((next) =>
            next.inventoryLocations.some((location) => location.stockItemId === row.stockItemId && location.onHand === value),
          );
        }
        return;
      }
      latest = inventoryResult.detail;
      setDetail(latest);
    }
    setSaving(false);
    setSaveNotice("تغییرات ذخیره شد.");
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
        <Link className="mt-4 inline-flex text-[#E53935] underline" href="/vendor-panel/products">
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
        <Link className="text-sm text-[#E53935] underline-offset-4 hover:underline" href="/vendor-panel/products">
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
            <div className="border-b border-gray-100 bg-gradient-to-l from-[#E53935]/5 to-transparent pb-4">
              <h2 className="text-base font-semibold">seam تجاری فروشنده</h2>
              <p className="mt-1 text-sm text-muted">قیمت از Pricing و موجودی از Inventory؛ نه روی Product</p>
            </div>
            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <label className="flex flex-col gap-1 text-sm">
                SKU فروشنده
                <input
                  className="min-h-11 rounded-ds border border-border bg-surface px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                  value={sellerSku}
                  onChange={(event) => setSellerSku(event.target.value)}
                  dir="ltr"
                />
              </label>
              <label className="flex flex-col gap-1 text-sm">
                وضعیت
                <select
                  className="min-h-11 rounded-ds border border-border bg-surface px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                  value={status}
                  onChange={(event) => setStatus(event.target.value)}
                >
                  <option value="Active">فعال</option>
                  <option value="Suspended">معلق</option>
                </select>
              </label>
              <label className="flex flex-col gap-1 text-sm">
                قیمت بدون مالیات (ریال)
                <input
                  className="min-h-11 rounded-ds border border-border bg-surface px-3 tabular-nums focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                  value={amount}
                  onChange={(event) => setAmount(event.target.value)}
                  inputMode="decimal"
                  dir="ltr"
                />
              </label>
              <div className="rounded-xl border border-border bg-surface p-4 sm:col-span-2" data-testid="offer-inventory-positions">
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div>
                    <h3 className="text-sm font-bold">موجودی هر محل</h3>
                    <p className="mt-1 text-xs leading-5 text-muted">
                      جمع موجودی فقط خواندنی است؛ برای تغییر، مقدار همان محل را ویرایش کنید.
                    </p>
                  </div>
                  <span className="rounded-full border border-border px-3 py-1 text-xs font-bold tabular-nums">
                    جمع: {formatUnits(detail.onHand)}
                  </span>
                </div>
                {detail.inventoryLocations.length === 0 ? (
                  <p className="mt-3 rounded-lg border border-dashed border-border px-3 py-2 text-xs text-muted">
                    هنوز موقعیت انباری برای این آفر ثبت نشده است.
                  </p>
                ) : (
                  <div className="mt-3 grid gap-3">
                    {detail.inventoryLocations.map((location) => (
                      <label
                        key={location.stockItemId}
                        className="grid gap-2 rounded-lg border border-border/70 bg-surface-elevated p-3 sm:grid-cols-[minmax(0,1fr)_150px] sm:items-center"
                        data-testid="offer-inventory-position"
                        data-stock-item-id={location.stockItemId}
                      >
                        <span className="min-w-0">
                          <span className="block break-words text-sm font-bold">محل {location.locationCode}</span>
                          <span className="mt-1 block text-xs leading-5 text-muted">
                            رزرو: {formatUnits(location.reserved)} · قابل فروش: {formatUnits(location.available)}
                          </span>
                        </span>
                        <input
                          className="min-h-11 w-full rounded-ds border border-border bg-surface px-3 tabular-nums focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                          value={inventoryDrafts[location.stockItemId] ?? ""}
                          onChange={(event) =>
                            setInventoryDrafts((current) => ({
                              ...current,
                              [location.stockItemId]: event.target.value,
                            }))
                          }
                          inputMode="decimal"
                          dir="ltr"
                          aria-label={`موجودی محل ${location.locationCode}`}
                          data-testid="offer-inventory-input"
                        />
                      </label>
                    ))}
                  </div>
                )}
              </div>
              <label className="flex flex-col gap-1 text-sm">
                حداقل مقدار خرید
                <input
                  className="min-h-11 rounded-ds border border-border bg-surface px-3 tabular-nums focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                  value={minimumOrderQuantity}
                  onChange={(event) => setMinimumOrderQuantity(event.target.value)}
                  inputMode="decimal"
                  dir="ltr"
                  data-testid="offer-min-qty"
                />
                <span className="text-xs text-muted" data-testid="offer-product-unit">
                  واحد کالا: {detail.productUnitName ?? detail.productUnitCode ?? "—"}
                  {detail.productUnitShortName ? ` (${detail.productUnitShortName})` : ""}
                </span>
              </label>
              <label className="flex flex-col gap-1 text-sm">
                حداکثر مقدار خرید
                <input
                  className="min-h-11 rounded-ds border border-border bg-surface px-3 tabular-nums focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                  value={maximumOrderQuantity}
                  onChange={(event) => setMaximumOrderQuantity(event.target.value)}
                  inputMode="decimal"
                  dir="ltr"
                  data-testid="offer-max-qty"
                />
                <span className="text-xs text-muted">
                  واحد کالا: {detail.productUnitName ?? detail.productUnitCode ?? "—"}
                  {detail.productUnitShortName ? ` (${detail.productUnitShortName})` : ""}
                </span>
              </label>
              <label className="flex flex-col gap-1 text-sm sm:col-span-2" data-testid="offer-return-policy">
                سیاست مرجوعی
                <select
                  className="min-h-11 rounded-ds border border-border bg-surface px-3 focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                  value={returnPolicyChoice}
                  onChange={(event) => setReturnPolicyChoice(event.target.value)}
                  disabled={detail ? !detail.sellerCanOverrideReturnPolicy && returnPolicyChoice === "Default" : false}
                >
                  <option value="Default">استفاده از سیاست پیش‌فرض فروشگاه</option>
                  {detail?.sellerCanOverrideReturnPolicy ? (
                    <option value="Custom" disabled={!detail.sellerCanOverrideReturnPolicy}>
                      مهلت اختصاصی
                    </option>
                  ) : null}
                  {detail?.allowNonReturnableOffers && detail.sellerCanOverrideReturnPolicy ? (
                    <option value="NonReturnable">غیرقابل مرجوعی</option>
                  ) : null}
                </select>
                <span className="text-xs text-muted">
                  پیش‌فرض فروشگاه: {detail?.defaultReturnWindowDays ?? 7} روز پس از تحویل
                  {detail?.sellerCanOverrideReturnPolicy
                    ? ` · محدوده مجاز ${detail.minReturnWindowDays} تا ${detail.maxReturnWindowDays} روز`
                    : " · تغییر توسط فروشنده مجاز نیست"}
                </span>
              </label>
              {returnPolicyChoice === "Custom" ? (
                <label className="flex flex-col gap-1 text-sm">
                  مهلت اختصاصی (روز)
                  <input
                    className="min-h-11 rounded-ds border border-border bg-surface px-3 tabular-nums focus:outline-none focus:ring-2 focus:ring-[#E53935]"
                    value={customReturnWindowDays}
                    onChange={(event) => setCustomReturnWindowDays(event.target.value)}
                    inputMode="numeric"
                    dir="ltr"
                    data-testid="offer-custom-return-days"
                  />
                </label>
              ) : null}
            </div>
            <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-3">
              <div>
                <dt className="text-muted">قیمت جاری Offer</dt>
                <dd className="mt-1 tabular-nums font-medium text-[#E53935]">{formatMoney(detail.amount, detail.currency)}</dd>
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
            {hasChanges() ? (
              <p className="mt-3 text-sm font-medium" data-testid="offer-dirty">تغییرات ذخیره‌نشده دارید.</p>
            ) : null}
            {saveError ? <p className="mt-3 text-sm text-danger" data-testid="offer-save-error">{saveError}</p> : null}
            {saveNotice ? <p className="mt-3 text-sm text-emerald-700" data-testid="offer-save-notice">{saveNotice}</p> : null}
            <button
              type="button"
              disabled={saving}
              data-testid="offer-save"
              onClick={() => void onSave()}
              className="mt-4 inline-flex min-h-11 items-center rounded-xl bg-[#E53935] px-5 text-sm font-bold text-white shadow-lg shadow-[#E53935]/30 hover:bg-[#c62828] disabled:opacity-50"
            >
              {saving ? "در حال ذخیره…" : "ذخیره تغییرات"}
            </button>
            <button
              type="button"
              className="mt-3 mr-3 text-sm text-muted underline-offset-4 hover:underline"
              data-testid="offer-cancel"
              onClick={() => router.push("/vendor-panel/products")}
            >
              انصراف
            </button>
          </section>
          {reservation ? (
            <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm" data-testid="seller-reservation-policy-readonly">
              <ReservationPolicyEditor
                view={reservation}
                draft={reservationDraft}
                onDraftChange={setReservationDraft}
                canEdit={false}
                locale="fa"
              />
            </section>
          ) : null}
          <SellerProductAttributesPanel productId={detail.productId} />
        </div>
      ) : (
        <p className="text-muted">در حال بارگذاری…</p>
      )}
    </main>
  );
}
