"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { ErrorState, faWorkspaceMessages } from "../../../../design-system";
import {
  formatFulfillmentStatus,
  fulfillmentStatusBadgeClass,
  loadSellerFulfillmentDetail,
  sellerAssignTracking,
  sellerCreateShipment,
  sellerDeliverShipment,
  sellerDispatchShipment,
  sellerMarkPacked,
  sellerMarkProcessing,
  type FulfillmentSnapshot,
} from "../../../fulfillment/fulfillment-api";
import { FulfillmentShipmentList } from "../../../fulfillment/fulfillment-ui";
import { readSellerPartyId, type HostReadSource } from "../../seller-api";

/** جزئیات fulfillment فروشنده با mutationهای عملیاتی. */
export default function VendorFulfillmentDetailPage() {
  const params = useParams<{ fulfillmentId: string }>();
  const fulfillmentId = params.fulfillmentId;
  const [source, setSource] = useState<HostReadSource | "loading">("loading");
  const [snapshot, setSnapshot] = useState<FulfillmentSnapshot | null>(null);
  const [message, setMessage] = useState<string | undefined>(undefined);
  const [denied, setDenied] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [carrierName, setCarrierName] = useState("پست");
  const [trackingDraft, setTrackingDraft] = useState<Record<string, string>>({});
  const [actionError, setActionError] = useState<string | null>(null);

  const sellerPartyId = useMemo(
    () => (typeof window !== "undefined" ? readSellerPartyId(window.location.search) : null),
    [],
  );

  function refresh() {
    if (!sellerPartyId) {
      setSource("error");
      setMessage("seller.identity.missing");
      return;
    }
    void loadSellerFulfillmentDetail(sellerPartyId, fulfillmentId).then((result) => {
      setSource(result.source);
      setSnapshot(result.snapshot);
      setMessage(result.message);
      setDenied(Boolean(result.denied));
    });
  }

  useEffect(refresh, [fulfillmentId, sellerPartyId]);

  async function runAction(key: string, action: () => Promise<{ ok: true; snapshot: FulfillmentSnapshot } | { ok: false; errorCode: string }>) {
    if (!sellerPartyId) return;
    setBusy(key);
    setActionError(null);
    const result = await action();
    setBusy(null);
    if (!result.ok) {
      setActionError(result.errorCode);
      return;
    }
    setSnapshot(result.snapshot);
  }

  if (denied) {
    return (
      <main data-testid="seller-auth-denied">
        <ErrorState
          title="دسترسی مجاز نیست"
          detail="این fulfillment متعلق به فروشندهٔ دیگری است یا پیدا نشد."
          onRetry={refresh}
          retryLabel={faWorkspaceMessages.retry}
        />
        <Link className="mt-4 inline-flex text-primary underline" href="/vendor-panel/fulfillments">
          بازگشت به فهرست
        </Link>
      </main>
    );
  }

  return (
    <main>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-sm text-muted">خانه / ارسال / جزئیات</p>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">جزئیات fulfillment</h1>
          <p className="mt-1 text-base text-muted">{snapshot?.fulfillmentId.slice(0, 8) ?? "…"}</p>
        </div>
        <Link className="text-sm text-primary underline-offset-4 hover:underline" href="/vendor-panel/fulfillments">
          بازگشت
        </Link>
      </div>
      {source === "error" ? (
        <ErrorState title="Host در دسترس نیست" detail={message} onRetry={refresh} retryLabel={faWorkspaceMessages.retry} />
      ) : snapshot ? (
        <div className="grid max-w-4xl gap-6">
          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <div className="flex flex-wrap items-center gap-3">
              <span className={fulfillmentStatusBadgeClass(snapshot.status)}>{formatFulfillmentStatus(snapshot.status)}</span>
              <span className="text-sm text-muted">سفارش فروشنده: {snapshot.sellerOrderId.slice(0, 8)}</span>
            </div>
            <p className="mt-4 text-sm text-muted">
              {snapshot.recipientName} · {snapshot.provinceName} {snapshot.cityName} · {snapshot.postalAddress} · {snapshot.postalCode}
            </p>
            <p className="mt-2 text-sm">{snapshot.shippingMethodLabel || "—"} · {snapshot.contactMobile}</p>
            {actionError ? <p className="mt-3 text-sm text-red-600">{actionError}</p> : null}
            <div className="mt-4 flex flex-wrap gap-2">
              <button
                type="button"
                disabled={busy !== null}
                onClick={() => void runAction("processing", () => sellerMarkProcessing(sellerPartyId!, fulfillmentId))}
                className="rounded-ds bg-primary px-3 py-2 text-sm text-primary-foreground disabled:opacity-60"
              >
                {busy === "processing" ? "…" : "شروع پردازش"}
              </button>
              <button
                type="button"
                disabled={busy !== null}
                onClick={() => void runAction("packed", () => sellerMarkPacked(sellerPartyId!, fulfillmentId))}
                className="rounded-ds bg-secondary px-3 py-2 text-sm disabled:opacity-60"
              >
                {busy === "packed" ? "…" : "بسته‌بندی شد"}
              </button>
            </div>
          </section>

          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm">
            <h2 className="text-base font-semibold">خطوط fulfillment</h2>
            <ul className="mt-4 divide-y divide-border">
              {snapshot.items.map((item) => (
                <li key={item.fulfillmentItemId} className="flex flex-wrap justify-between gap-3 py-3 text-sm">
                  <span>خط {item.orderLineId.slice(0, 8)}</span>
                  <span className="tabular-nums">
                    {item.quantityShipped.toLocaleString("fa-IR")} / {item.quantityOrdered.toLocaleString("fa-IR")} ارسال‌شده
                  </span>
                </li>
              ))}
            </ul>
          </section>

          <section className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm space-y-4">
            <h2 className="text-base font-semibold">ایجاد محموله</h2>
            <div className="flex flex-wrap gap-3 items-end">
              <label className="text-sm">
                <span className="text-muted">حامل</span>
                <input
                  className="mt-1 block w-full min-w-48 rounded-ds border border-border px-3 py-2 text-sm"
                  value={carrierName}
                  onChange={(event) => setCarrierName(event.target.value)}
                />
              </label>
              <button
                type="button"
                disabled={busy !== null}
                onClick={() => {
                  const remaining = snapshot.items
                    .map((item) => ({ orderLineId: item.orderLineId, quantity: Math.max(item.quantityOrdered - item.quantityShipped, 0) }))
                    .filter((item) => item.quantity > 0);
                  void runAction("shipment", () => sellerCreateShipment(sellerPartyId!, fulfillmentId, carrierName, remaining));
                }}
                className="rounded-ds bg-primary px-3 py-2 text-sm text-primary-foreground disabled:opacity-60"
              >
                {busy === "shipment" ? "…" : "ثبت محموله باقیمانده"}
              </button>
            </div>
            <FulfillmentShipmentList shipments={snapshot.shipments} />
          </section>

          {snapshot.shipments.map((shipment) => (
            <section key={shipment.shipmentId} className="rounded-2xl border border-border bg-surface-elevated p-5 shadow-sm space-y-3">
              <h3 className="text-sm font-semibold">عملیات محموله {shipment.shipmentId.slice(0, 8)}</h3>
              <div className="flex flex-wrap gap-2 items-end">
                <label className="text-sm grow min-w-48">
                  <span className="text-muted">کد رهگیری</span>
                  <input
                    className="mt-1 block w-full rounded-ds border border-border px-3 py-2 text-sm"
                    dir="ltr"
                    value={trackingDraft[shipment.shipmentId] ?? shipment.trackingReference ?? ""}
                    onChange={(event) => setTrackingDraft((prev) => ({ ...prev, [shipment.shipmentId]: event.target.value }))}
                  />
                </label>
                <button
                  type="button"
                  disabled={busy !== null}
                  onClick={() => {
                    const tracking = trackingDraft[shipment.shipmentId] ?? shipment.trackingReference ?? "";
                    void runAction(`track-${shipment.shipmentId}`, () =>
                      sellerAssignTracking(sellerPartyId!, fulfillmentId, shipment.shipmentId, tracking));
                  }}
                  className="rounded-ds bg-secondary px-3 py-2 text-sm disabled:opacity-60"
                >
                  ثبت رهگیری
                </button>
                <button
                  type="button"
                  disabled={busy !== null}
                  onClick={() => void runAction(`dispatch-${shipment.shipmentId}`, () =>
                    sellerDispatchShipment(sellerPartyId!, fulfillmentId, shipment.shipmentId))}
                  className="rounded-ds bg-primary px-3 py-2 text-sm text-primary-foreground disabled:opacity-60"
                >
                  dispatch
                </button>
                <button
                  type="button"
                  disabled={busy !== null}
                  onClick={() => void runAction(`deliver-${shipment.shipmentId}`, () =>
                    sellerDeliverShipment(sellerPartyId!, fulfillmentId, shipment.shipmentId))}
                  className="rounded-ds bg-secondary px-3 py-2 text-sm disabled:opacity-60"
                >
                  تحویل
                </button>
              </div>
            </section>
          ))}
        </div>
      ) : (
        <p className="text-muted">در حال بارگذاری…</p>
      )}
    </main>
  );
}
