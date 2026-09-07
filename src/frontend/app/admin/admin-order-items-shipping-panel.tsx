"use client";

import { useEffect, useState } from "react";
import { MoreHorizontal, Package, Truck } from "lucide-react";
import { toast } from "react-toastify";
import { formatAdminMoney, formatAdminStatus, type AdminOrderDetail, type AdminOrderLine, type AdminSellerOrder } from "./admin-api";
import { AdminCreateShipmentModal } from "./admin-create-shipment-modal";
import { mapAdminErrorMessage } from "./admin-error-map";
import {
  executeAdminOrderOperation,
  loadAdminOrderOperations,
  type AdminOrderOperationAction,
} from "./admin-order-operations";
import { sellerQuickActionLabels } from "./admin-order-operations-scope";

type Props = {
  detail: AdminOrderDetail;
  checkoutId: string;
  onCompleted?: () => void;
};

function lineKey(line: AdminOrderLine): string {
  return line.orderLineId || line.id;
}

function allocationSummary(line: AdminOrderLine): string {
  if (line.quantityShipped == null) return "—";
  const remaining = Math.max(0, line.quantity - line.quantityShipped);
  return `${line.quantityShipped.toLocaleString("fa-IR")} / ${line.quantity.toLocaleString("fa-IR")} ارسال‌شده · باقی ${remaining.toLocaleString("fa-IR")}`;
}

function actionFor(
  actions: AdminOrderOperationAction[],
  code: string,
  sellerOrderId: string,
): AdminOrderOperationAction | undefined {
  return actions.find((a) => a.code === code && a.sellerOrderId === sellerOrderId);
}

/**
 * بخش اقلام و ارسال — گروه‌بندی فروشنده، انتخاب خط، کارت مرسوله، مودال ایجاد.
 */
export function AdminOrderItemsShippingPanel({ detail, checkoutId, onCompleted }: Props) {
  const [selectedBySeller, setSelectedBySeller] = useState<Record<string, string[]>>({});
  const [opsBySeller, setOpsBySeller] = useState<Record<string, AdminOrderOperationAction[]>>({});
  const [opsLoaded, setOpsLoaded] = useState(false);
  const [pendingCode, setPendingCode] = useState<string | null>(null);
  const [shipmentModalSellerId, setShipmentModalSellerId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    void loadAdminOrderOperations(checkoutId).then((result) => {
      if (cancelled) return;
      if (result.state !== "ok" || !result.data) {
        setOpsLoaded(true);
        return;
      }
      const map: Record<string, AdminOrderOperationAction[]> = {};
      for (const action of result.data.actions) {
        const key = action.sellerOrderId ?? "__checkout__";
        (map[key] ??= []).push(action);
      }
      setOpsBySeller(map);
      setOpsLoaded(true);
    });
    return () => {
      cancelled = true;
    };
  }, [checkoutId, detail.sellerOrders]);

  function toggleLine(sellerOrderId: string, lineId: string) {
    setSelectedBySeller((prev) => {
      const current = new Set(prev[sellerOrderId] ?? []);
      if (current.has(lineId)) current.delete(lineId);
      else current.add(lineId);
      return { ...prev, [sellerOrderId]: [...current] };
    });
  }

  function toggleAll(seller: AdminSellerOrder) {
    const ids = seller.lines.map(lineKey);
    const current = selectedBySeller[seller.id] ?? [];
    const allSelected = ids.length > 0 && ids.every((id) => current.includes(id));
    setSelectedBySeller((prev) => ({
      ...prev,
      [seller.id]: allSelected ? [] : ids,
    }));
  }

  async function runSellerOp(seller: AdminSellerOrder, code: string) {
    const actions = [
      ...(opsBySeller[seller.id] ?? []),
      ...(opsBySeller.__checkout__ ?? []),
    ];
    const action = actionFor(actions, code, seller.id);
    if (!action) {
      toast.error("این عملیات در وضعیت فعلی برای این فروشنده مجاز نیست.");
      return;
    }
    setPendingCode(`${seller.id}:${code}`);
    const result = await executeAdminOrderOperation(checkoutId, {
      code: action.code,
      sellerOrderId: action.sellerOrderId,
      fulfillmentId: action.fulfillmentId,
      shipmentId: action.shipmentId,
      returnRequestId: action.returnRequestId,
    });
    setPendingCode(null);
    if (result.state !== "ok") {
      const raw = result.message ?? "order.operation.failed";
      const fa = /^[a-z0-9._-]+$/i.test(raw) ? mapAdminErrorMessage(raw, "fa") : raw;
      toast.error(fa);
      return;
    }
    toast.success(`عملیات ${action.labelFa} انجام شد.`);
    onCompleted?.();
  }

  const modalSeller = detail.sellerOrders.find((s) => s.id === shipmentModalSellerId) ?? null;

  return (
    <section
      className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
      data-testid="admin-order-items-shipping"
      dir="rtl"
    >
      <div className="border-b border-gray-200 bg-gray-50/70 px-3 py-2.5">
        <h2 className="text-sm font-black text-gray-900">اقلام و ارسال</h2>
        <p className="text-xs text-gray-500">عملیات fulfillment به‌ازای هر فروشنده — انتخاب خط فقط داخل همان فروشنده</p>
      </div>

      <div className="space-y-4 p-3 md:p-4">
        {detail.sellerOrders.map((seller) => {
          const selected = selectedBySeller[seller.id] ?? [];
          const hasSelection = selected.length > 0;
          const labels = sellerQuickActionLabels(hasSelection);
          const actions = opsBySeller[seller.id] ?? [];
          const canPack = Boolean(actionFor(actions, "mark_packed", seller.id));
          const canCreate = Boolean(actionFor(actions, "create_shipment", seller.id));
          const canDispatch = Boolean(actionFor(actions, "dispatch_shipment", seller.id));
          const shipments = seller.shipments ?? [];
          const itemCount = seller.lines.reduce((sum, line) => sum + line.quantity, 0);

          return (
            <article
              key={seller.id}
              className="rounded-xl border border-gray-200 bg-white"
              data-testid={`admin-order-seller-group-${seller.id}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-2 border-b border-gray-100 px-3 py-2.5">
                <div>
                  <h3 className="text-sm font-black text-gray-900">{seller.sellerDisplayName}</h3>
                  <p className="text-xs text-gray-500">
                    {itemCount.toLocaleString("fa-IR")} قلم · {seller.orderNumber}
                    {seller.fulfillmentStatus ? ` · ${formatAdminStatus(seller.fulfillmentStatus)}` : ""}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-1.5">
                  <button
                    type="button"
                    disabled={!canCreate || pendingCode !== null}
                    onClick={() => setShipmentModalSellerId(seller.id)}
                    className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white disabled:opacity-50"
                    data-testid={`admin-order-seller-create-shipment-${seller.id}`}
                  >
                    {labels.createShipment}
                  </button>
                  <button
                    type="button"
                    disabled={!canPack || pendingCode !== null}
                    onClick={() => void runSellerOp(seller, "mark_packed")}
                    className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 disabled:opacity-50"
                    title={hasSelection ? "بسته‌بندی Host فعلاً کل fulfillment است؛ انتخاب خط در T005" : undefined}
                  >
                    {labels.pack}
                  </button>
                  <button
                    type="button"
                    disabled={!canDispatch || pendingCode !== null}
                    onClick={() => void runSellerOp(seller, "dispatch_shipment")}
                    className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 disabled:opacity-50"
                    title={hasSelection ? "ارسال Host روی مرسوله است؛ انتخاب خط در T005" : undefined}
                  >
                    {labels.dispatch}
                  </button>
                </div>
              </div>

              <div className="grid gap-3 p-3 lg:grid-cols-[minmax(0,1.4fr)_minmax(260px,0.8fr)]">
                <div className="overflow-x-auto rounded-lg border border-gray-100">
                  <table className="min-w-full text-sm">
                    <thead className="bg-gray-50 text-xs text-gray-600">
                      <tr>
                        <th className="px-2 py-2 text-right">
                          <input
                            type="checkbox"
                            aria-label="انتخاب همه اقلام این فروشنده"
                            checked={seller.lines.length > 0 && seller.lines.every((l) => selected.includes(lineKey(l)))}
                            onChange={() => toggleAll(seller)}
                            data-testid={`admin-order-seller-select-all-${seller.id}`}
                          />
                        </th>
                        <th className="px-2 py-2 text-right">#</th>
                        <th className="px-2 py-2 text-right">محصول</th>
                        <th className="px-2 py-2 text-right">تعداد</th>
                        <th className="px-2 py-2 text-right">قیمت</th>
                        <th className="px-2 py-2 text-right">جمع</th>
                        <th className="px-2 py-2 text-right">وضعیت</th>
                        <th className="px-2 py-2 text-right">تخصیص ارسال</th>
                        <th className="px-2 py-2 text-right">عملیات</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {seller.lines.map((line, index) => {
                        const key = lineKey(line);
                        return (
                          <tr key={key} className="hover:bg-gray-50/70" data-testid={`admin-order-line-row-${key}`}>
                            <td className="px-2 py-2">
                              <input
                                type="checkbox"
                                checked={selected.includes(key)}
                                onChange={() => toggleLine(seller.id, key)}
                                aria-label={`انتخاب ${line.title}`}
                                data-testid={`admin-order-line-select-${key}`}
                              />
                            </td>
                            <td className="px-2 py-2 tabular-nums text-xs text-gray-500">{(index + 1).toLocaleString("fa-IR")}</td>
                            <td className="px-2 py-2">
                              <div className="flex min-w-0 items-center gap-2">
                                <span className="inline-flex size-8 shrink-0 items-center justify-center rounded bg-gray-100 text-gray-400">
                                  {line.imageUrl ? (
                                    // eslint-disable-next-line @next/next/no-img-element
                                    <img src={line.imageUrl} alt="" className="size-8 rounded object-cover" />
                                  ) : (
                                    <Package className="size-3.5" aria-hidden />
                                  )}
                                </span>
                                <span className="truncate font-semibold text-gray-900">{line.title}</span>
                              </div>
                            </td>
                            <td className="px-2 py-2 tabular-nums">{line.quantity.toLocaleString("fa-IR")}</td>
                            <td className="px-2 py-2 tabular-nums text-xs">{formatAdminMoney(line.unitAmount, line.currency)}</td>
                            <td className="px-2 py-2 tabular-nums text-xs font-semibold">{formatAdminMoney(line.linePayable, line.currency)}</td>
                            <td className="px-2 py-2">
                              <span className="rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-bold text-amber-800">
                                {formatAdminStatus(line.operationalStatus || seller.fulfillmentStatus || seller.status)}
                              </span>
                            </td>
                            <td className="px-2 py-2 text-[11px] text-gray-600">{allocationSummary(line)}</td>
                            <td className="px-2 py-2">
                              <button
                                type="button"
                                className="inline-flex size-7 items-center justify-center rounded-full border border-gray-200 text-gray-500"
                                aria-label="عملیات ردیف"
                                title="عملیات ردیف در T005 تکمیل می‌شود"
                                disabled
                              >
                                <MoreHorizontal className="size-3.5" aria-hidden />
                              </button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                  {hasSelection ? (
                    <div className="border-t border-blue-100 bg-blue-50 px-3 py-2 text-xs font-semibold text-blue-800" data-testid={`admin-order-seller-selection-bar-${seller.id}`}>
                      افزودن مرسوله جدید برای {selected.length.toLocaleString("fa-IR")} قلم انتخاب‌شده
                    </div>
                  ) : null}
                </div>

                <aside className="rounded-lg border border-gray-100 bg-gray-50/40 p-3" data-testid={`admin-order-seller-shipments-${seller.id}`}>
                  <h4 className="text-xs font-black text-gray-800">
                    مرسوله‌های این فروشنده ({shipments.length.toLocaleString("fa-IR")})
                  </h4>
                  {shipments.length === 0 ? (
                    <div className="mt-6 flex flex-col items-center gap-2 py-6 text-center text-xs text-gray-500" data-testid={`admin-order-seller-shipments-empty-${seller.id}`}>
                      <Truck className="size-8 text-gray-300" aria-hidden />
                      <p>هنوز مرسوله‌ای ایجاد نشده است.</p>
                    </div>
                  ) : (
                    <ul className="mt-2 space-y-2">
                      {shipments.map((shipment) => (
                        <li
                          key={shipment.shipmentId}
                          className="rounded-lg border border-gray-200 bg-white p-2.5 shadow-sm"
                          data-testid={`admin-order-shipment-card-${shipment.shipmentId}`}
                        >
                          <div className="flex items-start justify-between gap-2">
                            <div>
                              <p className="font-mono text-[11px] text-gray-500" dir="ltr">
                                #{shipment.shipmentId.slice(0, 8)}
                              </p>
                              <p className="mt-0.5 text-xs font-bold text-gray-900">{shipment.carrierDisplayName}</p>
                              <p className="text-[11px] text-gray-500">
                                {shipment.itemCount.toLocaleString("fa-IR")} قلم
                              </p>
                            </div>
                            <span className="rounded-full bg-blue-50 px-2 py-0.5 text-[10px] font-bold text-blue-700">
                              {formatAdminStatus(shipment.status)}
                            </span>
                          </div>
                          {shipment.trackingReference ? (
                            <p className="mt-2 text-[11px] text-gray-600" dir="ltr">
                              رهگیری: {shipment.trackingReference}
                            </p>
                          ) : (
                            <p className="mt-2 text-[11px] text-gray-400">کد رهگیری ثبت نشده</p>
                          )}
                        </li>
                      ))}
                    </ul>
                  )}
                  {!opsLoaded ? <p className="mt-2 text-[11px] text-gray-400">بارگذاری عملیات…</p> : null}
                </aside>
              </div>
            </article>
          );
        })}
      </div>

      {modalSeller ? (
        <AdminCreateShipmentModal
          open
          onClose={() => setShipmentModalSellerId(null)}
          checkoutId={checkoutId}
          sellerOrder={modalSeller}
          fulfillmentId={modalSeller.fulfillmentId}
          selectedLines={(selectedBySeller[modalSeller.id] ?? [])
            .map((id) => modalSeller.lines.find((l) => lineKey(l) === id))
            .filter((l): l is AdminOrderLine => Boolean(l))
            .map((line) => ({
              line,
              quantity: Math.max(0, line.quantity - (line.quantityShipped ?? 0)) || line.quantity,
            }))}
          onCompleted={() => {
            setShipmentModalSellerId(null);
            onCompleted?.();
          }}
        />
      ) : null}
    </section>
  );
}
