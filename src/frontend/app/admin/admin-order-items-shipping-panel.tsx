"use client";

import { useEffect, useState } from "react";
import { MoreHorizontal, Package, Truck } from "lucide-react";
import { toast } from "react-toastify";
import { formatAdminMoney, formatAdminStatus, type AdminOrderDetail, type AdminOrderLine, type AdminSellerOrder, type AdminShipment } from "./admin-api";
import { AdminCreateShipmentModal } from "./admin-create-shipment-modal";
import { AdminConsolidatedPackageSection } from "./admin-consolidated-package-section";
import {
  AdminAssignShipmentTrackingModal,
} from "./admin-assign-shipment-tracking-modal";
import { mapAdminErrorMessage } from "./admin-error-map";
import {
  canonicalReturnDisplay,
  deriveLineCapability,
  intersectLifecycleCodes,
  isIncompatibleSelection,
  isPaymentLockedSeller,
  lineLifecycleActions,
  MIXED_SELECTION_MESSAGE_FA,
  PAYMENT_LOCKED_BANNER_FA,
} from "./admin-order-line-actions";
import {
  executeAdminOrderOperation,
  loadAdminOrderOperations,
  type AdminOrderLineCapability,
  type AdminOrderOperationAction,
  type AdminSellerCapability,
} from "./admin-order-operations";
import { sellerQuickActionLabels } from "./admin-order-operations-scope";
import { parseQuantityInput } from "../../lib/quantity-display";

type Props = {
  detail: AdminOrderDetail;
  checkoutId: string;
  onCompleted?: () => void;
};

type QtyMap = Record<string, number>;

function lineKey(line: AdminOrderLine): string {
  return line.orderLineId || line.id;
}

function processingQty(line: AdminOrderLine): number {
  return line.quantityProcessing ?? 0;
}

function processableQty(line: AdminOrderLine): number {
  return Math.max(0, line.quantity - processingQty(line));
}

function packableQty(line: AdminOrderLine): number {
  return Math.max(0, processingQty(line) - (line.quantityPacked ?? 0));
}

function unprocessableQty(line: AdminOrderLine): number {
  return Math.max(0, processingQty(line) - (line.quantityPacked ?? 0));
}

function shippableQty(line: AdminOrderLine): number {
  const packed = line.quantityPacked ?? 0;
  const allocated = line.quantityAllocated ?? line.quantityShipped ?? 0;
  return Math.max(0, packed - allocated);
}

function unpackableQty(line: AdminOrderLine): number {
  const packed = line.quantityPacked ?? 0;
  const allocated = line.quantityAllocated ?? line.quantityShipped ?? 0;
  return Math.max(0, packed - allocated);
}

function allocationSummary(line: AdminOrderLine): string {
  const packed = line.quantityPacked;
  const shipped = line.quantityShipped;
  const allocated = line.quantityAllocated;
  if (packed == null && shipped == null) return "—";
  const p = packed ?? 0;
  const s = shipped ?? 0;
  const a = allocated ?? s;
  const remaining = Math.max(0, line.quantity - a);
  return `بسته‌بندی ${p.toLocaleString("fa-IR")} · تخصیص ${a.toLocaleString("fa-IR")} · ارسال ${s.toLocaleString("fa-IR")} · باقی ${remaining.toLocaleString("fa-IR")}`;
}

function returnSummary(line: AdminOrderLine): string {
  return canonicalReturnDisplay(line);
}

function actionFor(
  actions: AdminOrderOperationAction[],
  code: string,
  sellerOrderId: string,
  shipmentId?: string | null,
  orderLineId?: string | null,
): AdminOrderOperationAction | undefined {
  return actions.find((a) => {
    if (a.code !== code || a.sellerOrderId !== sellerOrderId) return false;
    if (shipmentId != null && a.shipmentId !== shipmentId) return false;
    if (orderLineId === undefined) return !a.orderLineId;
    return a.orderLineId === orderLineId;
  });
}

/**
 * بخش اقلام و ارسال — گروه‌بندی فروشنده، انتخاب خط/تعداد، کارت مرسوله، مودال ایجاد.
 */
export function AdminOrderItemsShippingPanel({ detail, checkoutId, onCompleted }: Props) {
  const [selectedBySeller, setSelectedBySeller] = useState<Record<string, string[]>>({});
  const [qtyByLine, setQtyByLine] = useState<QtyMap>({});
  const [opsBySeller, setOpsBySeller] = useState<Record<string, AdminOrderOperationAction[]>>({});
  const [lineCaps, setLineCaps] = useState<Record<string, AdminOrderLineCapability>>({});
  const [sellerCaps, setSellerCaps] = useState<Record<string, AdminSellerCapability>>({});
  const [opsLoaded, setOpsLoaded] = useState(false);
  const [pendingCode, setPendingCode] = useState<string | null>(null);
  const [openKebabLineId, setOpenKebabLineId] = useState<string | null>(null);
  const [shipmentModalSellerId, setShipmentModalSellerId] = useState<string | null>(null);
  const [trackingTarget, setTrackingTarget] = useState<{
    seller: AdminSellerOrder;
    shipment: AdminShipment;
  } | null>(null);

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
      const nextLines: Record<string, AdminOrderLineCapability> = {};
      for (const cap of result.data.lineCapabilities) nextLines[cap.orderLineId] = cap;
      setLineCaps(nextLines);
      const nextSellers: Record<string, AdminSellerCapability> = {};
      for (const cap of result.data.sellerCapabilities) nextSellers[cap.sellerOrderId] = cap;
      setSellerCaps(nextSellers);
      setOpsLoaded(true);
    });
    return () => {
      cancelled = true;
    };
  }, [checkoutId, detail.sellerOrders]);

  function toggleLine(sellerOrderId: string, line: AdminOrderLine) {
    const id = lineKey(line);
    setSelectedBySeller((prev) => {
      const current = new Set(prev[sellerOrderId] ?? []);
      if (current.has(id)) current.delete(id);
      else {
        current.add(id);
        setQtyByLine((q) => ({
          ...q,
          [id]: q[id] ?? (processableQty(line) || packableQty(line) || unprocessableQty(line) || shippableQty(line) || line.quantity),
        }));
      }
      return { ...prev, [sellerOrderId]: [...current] };
    });
  }

  function capabilityFor(seller: AdminSellerOrder, line: AdminOrderLine) {
    const projected = sellerCaps[seller.id];
    const fromApi = lineCaps[line.orderLineId || line.id];
    const paymentLocked = projected?.paymentLocked ?? isPaymentLockedSeller(seller);
    const cancelled = seller.status === "Cancelled";
    const derived = deriveLineCapability({
      paymentLocked,
      cancelled,
      operationalStatus: line.operationalStatus,
      packable: packableQty(line),
      unpackable: unpackableQty(line),
      unprocessable: unprocessableQty(line),
      shippable: shippableQty(line),
      quantity: line.quantity,
      projectedCodes: (opsBySeller[seller.id] ?? []).map((a) => a.code),
    });
    if (!fromApi) return derived;
    return {
      selectable: fromApi.selectable,
      selectableQuantityMax: fromApi.selectableQuantityMax,
      rowActionCodes: fromApi.rowActionCodes,
      bulkActionCodes: fromApi.bulkActionCodes,
      shipmentEligibleQuantity: fromApi.shipmentEligibleQuantity,
      paymentLocked,
    };
  }

  function toggleAll(seller: AdminSellerOrder) {
    const ids = seller.lines.filter((line) => capabilityFor(seller, line).selectable).map(lineKey);
    const current = selectedBySeller[seller.id] ?? [];
    const allSelected = ids.length > 0 && ids.every((id) => current.includes(id));
    setSelectedBySeller((prev) => ({
      ...prev,
      [seller.id]: allSelected ? [] : ids,
    }));
    if (!allSelected) {
      setQtyByLine((q) => {
        const next = { ...q };
        for (const line of seller.lines) {
          const cap = capabilityFor(seller, line);
          if (!cap.selectable) continue;
          const id = lineKey(line);
          next[id] = next[id] ?? Math.min(cap.selectableQuantityMax || line.quantity, processableQty(line) || packableQty(line) || unprocessableQty(line) || shippableQty(line) || line.quantity);
        }
        return next;
      });
    }
  }

  function buildSelections(seller: AdminSellerOrder, mode: "pack" | "ship" | "unpack" | "process" | "unprocess") {
    const selected = selectedBySeller[seller.id] ?? [];
    if (selected.length === 0) return null;
    const lines = selected
      .map((id) => seller.lines.find((l) => lineKey(l) === id))
      .filter((l): l is AdminOrderLine => Boolean(l));
    return lines
      .map((line) => {
        const id = lineKey(line);
        const max =
          mode === "pack" ? packableQty(line)
          : mode === "unpack" ? unpackableQty(line)
          : mode === "process" ? processableQty(line)
          : mode === "unprocess" ? unprocessableQty(line)
          : shippableQty(line);
        const qty = Math.min(qtyByLine[id] ?? max, max);
        return {
          orderLineId: line.orderLineId || line.id,
          quantity: qty,
        };
      })
      .filter((s) => s.quantity > 0);
  }

  async function runSellerOp(
    seller: AdminSellerOrder,
    code: string,
    opts?: {
      shipmentId?: string | null;
      orderLineId?: string | null;
      selections?: Array<{ orderLineId: string; quantity: number }> | null;
      carrierDisplayName?: string;
      trackingReference?: string;
    },
  ): Promise<boolean> {
    const actions = [
      ...(opsBySeller[seller.id] ?? []),
      ...(opsBySeller.__checkout__ ?? []),
    ];
    const action = actionFor(actions, code, seller.id, opts?.shipmentId, opts?.orderLineId);
    if (!action) {
      toast.error("این عملیات در وضعیت فعلی برای این فروشنده مجاز نیست.");
      return false;
    }
    setPendingCode(`${seller.id}:${code}:${opts?.shipmentId ?? ""}`);
    const result = await executeAdminOrderOperation(checkoutId, {
      code: action.code,
      sellerOrderId: action.sellerOrderId,
      fulfillmentId: action.fulfillmentId,
      shipmentId: opts?.shipmentId ?? action.shipmentId,
      returnRequestId: action.returnRequestId,
      carrierDisplayName: opts?.carrierDisplayName,
      trackingReference: opts?.trackingReference,
      selections: opts?.selections,
    });
    setPendingCode(null);
    if (result.state !== "ok") {
      const raw = result.message ?? "order.operation.failed";
      const fa = /^[a-z0-9._-]+$/i.test(raw) ? mapAdminErrorMessage(raw, "fa") : raw;
      toast.error(fa);
      return false;
    }
    toast.success(`عملیات ${action.labelFa} انجام شد.`);
    onCompleted?.();
    return true;
  }

  const existingTrackingCodes = detail.sellerOrders.flatMap((s) =>
    (s.shipments ?? [])
      .filter((sh) => sh.shipmentId !== trackingTarget?.shipment.shipmentId)
      .map((sh) => sh.trackingReference)
      .filter((t): t is string => Boolean(t && t.trim())),
  );

  const modalSeller = detail.sellerOrders.find((s) => s.id === shipmentModalSellerId) ?? null;

  return (
    <section
      className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
      data-testid="admin-order-items-shipping"
      dir="rtl"
    >
      <div className="border-b border-gray-200 bg-gray-50/70 px-3 py-2.5">
        <h2 className="text-sm font-black text-gray-900">اقلام و ارسال</h2>
      </div>

      <div className="space-y-4 p-3 md:p-4">
        <AdminConsolidatedPackageSection
          detail={detail}
          checkoutId={checkoutId}
          checkoutActions={opsBySeller.__checkout__ ?? []}
          pendingCode={pendingCode}
          setPendingCode={setPendingCode}
          onCompleted={onCompleted}
        />
        {detail.sellerOrders.map((seller) => {
          const selected = selectedBySeller[seller.id] ?? [];
          const hasSelection = selected.length > 0;
          const labels = sellerQuickActionLabels(hasSelection);
          const actions = opsBySeller[seller.id] ?? [];
          const sellerCap = sellerCaps[seller.id];
          const paymentLocked = sellerCap?.paymentLocked ?? isPaymentLockedSeller(seller);
          const selectedLines = selected
            .map((id) => seller.lines.find((l) => lineKey(l) === id))
            .filter((l): l is AdminOrderLine => Boolean(l));
          const perLineCaps = selectedLines.map((line) => capabilityFor(seller, line));
          const perLineCodes = perLineCaps.map((cap) => new Set(cap.bulkActionCodes));
          const bulkCodes = intersectLifecycleCodes(perLineCodes);
          const mixedSelection = isIncompatibleSelection(selected.length, bulkCodes, perLineCodes);
          const canStartGroup = !hasSelection && !paymentLocked && Boolean(actionFor(actions, "mark_processing", seller.id));
          const canStartSelected = hasSelection && !mixedSelection && bulkCodes.has("mark_processing")
            && Boolean(actionFor(actions, "mark_processing", seller.id));
          const canPackAll = !hasSelection && !paymentLocked && Boolean(actionFor(actions, "mark_packed", seller.id));
          const canPackSelected = hasSelection && !mixedSelection && bulkCodes.has("pack_selected")
            && Boolean(actionFor(actions, "pack_selected", seller.id));
          const canUnprocess = hasSelection && !mixedSelection && bulkCodes.has("unprocess")
            && Boolean(actionFor(actions, "unprocess", seller.id));
          const canUnpack = hasSelection && !mixedSelection && bulkCodes.has("unpack")
            && Boolean(actionFor(actions, "unpack", seller.id));
          const selectedShippable = hasSelection && !mixedSelection && perLineCaps.length > 0
            && perLineCaps.every((cap) => cap.shipmentEligibleQuantity > 0);
          const canCreate = !paymentLocked
            && Boolean(actionFor(actions, "create_shipment", seller.id))
            && (hasSelection
              ? selectedShippable
              : (sellerCap?.shipmentCreationPossible ?? seller.lines.some((line) => shippableQty(line) > 0)));
          const shipments = seller.shipments ?? [];
          const itemCount = seller.lines.reduce((sum, line) => sum + line.quantity, 0);
          const selectableLines = seller.lines.filter((line) => capabilityFor(seller, line).selectable);
          const headerStatus = paymentLocked
            ? "PendingPayment"
            : (seller.fulfillmentStatus || seller.status);

          return (
            <article
              key={seller.id}
              className="rounded-xl border border-gray-200 bg-white"
              data-testid={`admin-order-seller-group-${seller.id}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-2 border-b border-gray-100 px-3 py-2.5">
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="text-sm font-black text-gray-900">{seller.sellerDisplayName}</h3>
                    <span
                      className="rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-bold text-amber-800"
                      data-testid={`admin-order-seller-status-${seller.id}`}
                    >
                      {formatAdminStatus(headerStatus)}
                    </span>
                  </div>
                  <p className="text-xs text-gray-500">
                    {itemCount.toLocaleString("fa-IR")} قلم · {seller.orderNumber}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-1.5">
                  {canStartGroup ? (
                    <button
                      type="button"
                      disabled={pendingCode !== null}
                      onClick={() => void runSellerOp(seller, "mark_processing")}
                      className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-1.5 text-xs font-bold text-amber-900 disabled:opacity-50"
                      data-testid={`admin-order-seller-start-processing-${seller.id}`}
                    >
                      {labels.startProcessing}
                    </button>
                  ) : null}
                  {canStartSelected ? (
                    <button
                      type="button"
                      disabled={pendingCode !== null}
                      onClick={() => void runSellerOp(seller, "mark_processing", { selections: buildSelections(seller, "process") })}
                      className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white disabled:opacity-50"
                      data-testid={`admin-order-seller-start-selected-${seller.id}`}
                    >
                      {labels.startProcessing}
                    </button>
                  ) : null}
                  {canPackAll ? (
                    <button
                      type="button"
                      disabled={pendingCode !== null}
                      onClick={() => void runSellerOp(seller, "mark_packed")}
                      className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 disabled:opacity-50"
                      data-testid={`admin-order-seller-pack-${seller.id}`}
                    >
                      {labels.pack}
                    </button>
                  ) : null}
                  {canPackSelected ? (
                    <button
                      type="button"
                      disabled={pendingCode !== null}
                      onClick={() => void runSellerOp(seller, "pack_selected", { selections: buildSelections(seller, "pack") })}
                      className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white disabled:opacity-50"
                      data-testid={`admin-order-seller-pack-selected-${seller.id}`}
                    >
                      {labels.pack}
                    </button>
                  ) : null}
                  {canUnprocess ? (
                    <button
                      type="button"
                      disabled={pendingCode !== null}
                      onClick={() => void runSellerOp(seller, "unprocess", { selections: buildSelections(seller, "unprocess") })}
                      className="rounded-lg border border-amber-200 bg-white px-3 py-1.5 text-xs font-bold text-amber-900 disabled:opacity-50"
                      data-testid={`admin-order-seller-unprocess-${seller.id}`}
                    >
                      {labels.unprocess}
                    </button>
                  ) : null}
                  {canUnpack ? (
                    <button
                      type="button"
                      disabled={pendingCode !== null}
                      onClick={() => void runSellerOp(seller, "unpack", { selections: buildSelections(seller, "unpack") })}
                      className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-xs font-bold text-gray-700 disabled:opacity-50"
                      data-testid={`admin-order-seller-unpack-${seller.id}`}
                    >
                      {labels.unpack}
                    </button>
                  ) : null}
                </div>
              </div>
              {paymentLocked ? (
                <div
                  className="mx-3 mt-3 rounded-lg border border-sky-100 bg-sky-50 px-3 py-2 text-xs font-semibold text-sky-900"
                  data-testid={`admin-order-seller-payment-locked-${seller.id}`}
                >
                  {sellerCap?.infoMessageFa || PAYMENT_LOCKED_BANNER_FA}
                </div>
              ) : null}

              <div className="grid gap-3 p-3 lg:grid-cols-[minmax(0,1.4fr)_minmax(260px,0.8fr)]">
                <div className="overflow-x-auto rounded-lg border border-gray-100" data-testid={`admin-order-seller-lines-scroll-${seller.id}`}>
                  <table className="min-w-[920px] w-full text-sm">
                    <thead className="bg-gray-50 text-xs text-gray-600">
                      <tr>
                        <th className="px-2 py-2 text-right">
                          {selectableLines.length === 0 ? null : (
                            <input
                              type="checkbox"
                              aria-label="انتخاب همه اقلام این فروشنده"
                              checked={selectableLines.length > 0 && selectableLines.every((l) => selected.includes(lineKey(l)))}
                              onChange={() => toggleAll(seller)}
                              data-testid={`admin-order-seller-select-all-${seller.id}`}
                            />
                          )}
                        </th>
                        <th className="px-2 py-2 text-right">#</th>
                        <th className="px-2 py-2 text-right">محصول</th>
                        <th className="px-2 py-2 text-right">تعداد</th>
                        <th className="px-2 py-2 text-right">انتخاب تعداد</th>
                        <th className="px-2 py-2 text-right">قیمت</th>
                        <th className="px-2 py-2 text-right">جمع</th>
                        <th className="px-2 py-2 text-right">وضعیت</th>
                        <th className="px-2 py-2 text-right">تخصیص ارسال</th>
                        <th className="px-2 py-2 text-right">مهلت مرجوعی</th>
                        <th className="px-2 py-2 text-right">عملیات</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {seller.lines.map((line, index) => {
                        const key = lineKey(line);
                        const cap = capabilityFor(seller, line);
                        const maxQty = cap.selectableQuantityMax || packableQty(line) || shippableQty(line) || line.quantity;
                        const selectedQty = qtyByLine[key] ?? Math.min(maxQty, packableQty(line) || shippableQty(line) || maxQty);
                        const qtyEnabled = cap.selectable && maxQty > 0 && (cap.rowActionCodes.includes("pack_selected") || cap.rowActionCodes.includes("unprocess") || cap.rowActionCodes.includes("unpack") || cap.shipmentEligibleQuantity > 0);
                        const lineActions = lineLifecycleActions(actions, line.orderLineId || line.id, {
                          packable: cap.rowActionCodes.includes("pack_selected"),
                          unpackable: cap.rowActionCodes.includes("unpack"),
                          startable: cap.rowActionCodes.includes("mark_processing"),
                          unprocessable: cap.rowActionCodes.includes("unprocess"),
                        });
                        return (
                          <tr
                            key={key}
                            className={selected.includes(key) ? "bg-blue-50/70" : "hover:bg-gray-50/70"}
                            data-testid={`admin-order-line-row-${key}`}
                          >
                            <td className="px-2 py-2">
                              {cap.selectable ? (
                                <input
                                  type="checkbox"
                                  checked={selected.includes(key)}
                                  onChange={() => toggleLine(seller.id, line)}
                                  aria-label={`انتخاب ${line.title}`}
                                  data-testid={`admin-order-line-select-${key}`}
                                />
                              ) : null}
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
                            <td className="px-2 py-2">
                              {qtyEnabled ? (
                                <input
                                  inputMode="decimal"
                                  dir="ltr"
                                  min={0}
                                  max={maxQty}
                                  step="any"
                                  value={selectedQty}
                                  disabled={!selected.includes(key)}
                                  onChange={(e) => {
                                    const parsed = parseQuantityInput(e.target.value.replace(",", "."));
                                    if (parsed == null) return;
                                    setQtyByLine((q) => ({ ...q, [key]: Math.min(maxQty, parsed) }));
                                  }}
                                  className="w-16 rounded border border-gray-200 px-1.5 py-1 text-xs tabular-nums disabled:opacity-40"
                                  data-testid={`admin-order-line-qty-${key}`}
                                  aria-label={`تعداد انتخابی ${line.title}`}
                                />
                              ) : (
                                <span className="text-xs text-gray-400">{line.quantity.toLocaleString("fa-IR")}</span>
                              )}
                            </td>
                            <td className="px-2 py-2 tabular-nums text-xs">{formatAdminMoney(line.unitAmount, line.currency)}</td>
                            <td className="px-2 py-2 tabular-nums text-xs font-semibold">{formatAdminMoney(line.linePayable, line.currency)}</td>
                            <td className="px-2 py-2">
                              <span className="rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-bold text-amber-800">
                                {formatAdminStatus(line.operationalStatus || seller.fulfillmentStatus || seller.status)}
                              </span>
                            </td>
                            <td className="px-2 py-2 text-[11px] text-gray-600">{allocationSummary(line)}</td>
                            <td className="px-2 py-2 text-[11px] text-gray-700" data-testid={`admin-order-line-return-${key}`}>
                              {returnSummary(line)}
                            </td>
                            <td className="relative px-2 py-2">
                              {lineActions.length === 0 ? null : (
                                <div className="relative">
                                  <button
                                    type="button"
                                    className="inline-flex size-7 items-center justify-center rounded-full border border-gray-200 text-gray-600"
                                    aria-label="عملیات ردیف"
                                    aria-expanded={openKebabLineId === key}
                                    data-testid={`admin-order-line-kebab-${key}`}
                                    disabled={pendingCode !== null}
                                    onClick={() => setOpenKebabLineId((cur) => (cur === key ? null : key))}
                                  >
                                    <MoreHorizontal className="size-3.5" aria-hidden />
                                  </button>
                                  {openKebabLineId === key ? (
                                    <div
                                      className="absolute left-0 top-8 z-20 min-w-[11rem] rounded-lg border border-gray-200 bg-white py-1 shadow-lg"
                                      data-testid={`admin-order-line-kebab-menu-${key}`}
                                    >
                                      {lineActions.map((action) => (
                                        <button
                                          key={`${action.code}:${action.orderLineId}`}
                                          type="button"
                                          className="block w-full px-3 py-1.5 text-right text-xs font-bold text-gray-800 hover:bg-gray-50"
                                          data-testid={`admin-order-line-action-${action.code}-${key}`}
                                          onClick={() => {
                                            setOpenKebabLineId(null);
                                            const lineId = line.orderLineId || line.id;
                                            if (action.code === "mark_processing") {
                                              const max = processableQty(line);
                                              const qty = Math.min(qtyByLine[key] ?? max, max);
                                              void runSellerOp(seller, action.code, {
                                                orderLineId: action.orderLineId ?? undefined,
                                                selections: qty > 0 ? [{ orderLineId: lineId, quantity: qty }] : null,
                                              });
                                              return;
                                            }
                                            const mode = action.code === "unpack" ? "unpack" : action.code === "unprocess" ? "unprocess" : "pack";
                                            const max =
                                              mode === "pack" ? packableQty(line)
                                              : mode === "unprocess" ? unprocessableQty(line)
                                              : unpackableQty(line);
                                            const qty = Math.min(qtyByLine[key] ?? max, max);
                                            void runSellerOp(seller, action.code, {
                                              orderLineId: action.orderLineId ?? undefined,
                                              selections: qty > 0 ? [{ orderLineId: lineId, quantity: qty }] : null,
                                            });
                                          }}
                                        >
                                          {action.labelFa}
                                        </button>
                                      ))}
                                    </div>
                                  ) : null}
                                </div>
                              )}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                  {mixedSelection ? (
                    <div className="border-t border-amber-100 bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-900" data-testid={`admin-order-seller-mixed-selection-${seller.id}`}>
                      {MIXED_SELECTION_MESSAGE_FA}
                    </div>
                  ) : hasSelection ? (
                    <div className="border-t border-blue-100 bg-blue-50 px-3 py-2 text-xs font-semibold text-blue-800" data-testid={`admin-order-seller-selection-bar-${seller.id}`}>
                      {`${selected.length.toLocaleString("fa-IR")} قلم انتخاب شده`}
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
                      {shipments.map((shipment) => {
                        const packageLocked = Boolean(
                          shipment.packageLockedReasonFa || shipment.activePackageNumber,
                        );
                        const canCancel = !packageLocked
                          && Boolean(actionFor(actions, "cancel_shipment", seller.id, shipment.shipmentId));
                        const canTrack = !packageLocked
                          && Boolean(actionFor(actions, "assign_tracking", seller.id, shipment.shipmentId));
                        const canDispatch = !packageLocked
                          && Boolean(actionFor(actions, "dispatch_shipment", seller.id, shipment.shipmentId));
                        const canDeliver = !packageLocked
                          && Boolean(actionFor(actions, "deliver_shipment", seller.id, shipment.shipmentId));
                        const shipmentCancelled =
                          shipment.status === "Cancelled" || shipment.status === "Canceled";
                        return (
                          <li
                            key={shipment.shipmentId}
                            className={
                              shipmentCancelled
                                ? "rounded-lg border border-red-100/80 bg-red-50/40 p-2.5 shadow-sm"
                                : "rounded-lg border border-gray-200 bg-white p-2.5 shadow-sm"
                            }
                            data-testid={`admin-order-shipment-card-${shipment.shipmentId}`}
                            data-cancelled={shipmentCancelled ? "true" : undefined}
                          >
                            <div className="flex items-start justify-between gap-2">
                              <div>
                                <p className="font-mono text-[11px] text-gray-500" dir="ltr">
                                  {shipment.trackingReference
                                    ? shipment.trackingReference
                                    : `مرسوله · ${shipment.itemCount.toLocaleString("fa-IR")} قلم`}
                                </p>
                                <p className="mt-0.5 text-xs font-bold text-gray-900">{shipment.carrierDisplayName}</p>
                                <p className="text-[11px] text-gray-500">
                                  {shipment.itemCount.toLocaleString("fa-IR")} قلم
                                </p>
                              </div>
                              <span
                                className={
                                  shipmentCancelled
                                    ? "rounded-full bg-red-50 px-2 py-0.5 text-[10px] font-bold text-red-600"
                                    : "rounded-full bg-blue-50 px-2 py-0.5 text-[10px] font-bold text-blue-700"
                                }
                              >
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
                            {shipment.packageLockedReasonFa || shipment.activePackageNumber ? (
                              <p
                                className="mt-2 rounded border border-indigo-100 bg-indigo-50/70 px-2 py-1.5 text-[11px] font-semibold text-indigo-900"
                                data-testid={`admin-order-shipment-package-lock-${shipment.shipmentId}`}
                              >
                                {shipment.packageLockedReasonFa
                                  || `این مرسوله عضو بسته تجمیعی ${shipment.activePackageNumber} است و عملیات ارسال از طریق بسته تجمیعی انجام می‌شود.`}
                              </p>
                            ) : null}
                            <div className="mt-2 flex flex-wrap gap-1">
                              {canTrack ? (
                                <button
                                  type="button"
                                  className="rounded border border-gray-200 px-2 py-1 text-[10px] font-bold text-gray-700 disabled:opacity-50"
                                  disabled={pendingCode !== null}
                                  data-testid={`admin-order-shipment-assign-tracking-${shipment.shipmentId}`}
                                  onClick={() => setTrackingTarget({ seller, shipment })}
                                >
                                  ثبت کد رهگیری
                                </button>
                              ) : null}
                              {canDispatch ? (
                                <button
                                  type="button"
                                  className="rounded border border-gray-200 px-2 py-1 text-[10px] font-bold text-gray-700 disabled:opacity-50"
                                  disabled={pendingCode !== null}
                                  onClick={() => void runSellerOp(seller, "dispatch_shipment", { shipmentId: shipment.shipmentId })}
                                >
                                  ارسال
                                </button>
                              ) : null}
                              {canDeliver ? (
                                <button
                                  type="button"
                                  className="rounded border border-gray-200 px-2 py-1 text-[10px] font-bold text-gray-700 disabled:opacity-50"
                                  disabled={pendingCode !== null}
                                  onClick={() => void runSellerOp(seller, "deliver_shipment", { shipmentId: shipment.shipmentId })}
                                >
                                  ثبت تحویل
                                </button>
                              ) : null}
                              {canCancel ? (
                                <button
                                  type="button"
                                  className="rounded border border-red-200 px-2 py-1 text-[10px] font-bold text-red-700 disabled:opacity-50"
                                  disabled={pendingCode !== null}
                                  data-testid={`admin-order-shipment-cancel-${shipment.shipmentId}`}
                                  onClick={() => void runSellerOp(seller, "cancel_shipment", { shipmentId: shipment.shipmentId })}
                                >
                                  ابطال مرسوله
                                </button>
                              ) : null}
                            </div>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                  {canCreate ? (
                    <button
                      type="button"
                      disabled={pendingCode !== null}
                      onClick={() => setShipmentModalSellerId(seller.id)}
                      className="mt-3 w-full rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white disabled:opacity-50"
                      data-testid={`admin-order-seller-create-shipment-${seller.id}`}
                    >
                      {hasSelection ? labels.createShipment : "+ ایجاد مرسوله جدید"}
                    </button>
                  ) : null}
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
          orderDetail={detail}
          selectedLines={(selectedBySeller[modalSeller.id] ?? [])
            .map((id) => modalSeller.lines.find((l) => lineKey(l) === id))
            .filter((l): l is AdminOrderLine => Boolean(l))
            .map((line) => ({
              line,
              quantity: Math.min(
                qtyByLine[lineKey(line)] ?? shippableQty(line),
                shippableQty(line) || line.quantity,
              ),
            }))
            .filter((x) => x.quantity > 0)}
          onCompleted={() => {
            setShipmentModalSellerId(null);
            onCompleted?.();
          }}
        />
      ) : null}

      {trackingTarget ? (
        <AdminAssignShipmentTrackingModal
          key={trackingTarget.shipment.shipmentId}
          open
          seller={trackingTarget.seller}
          shipment={trackingTarget.shipment}
          pending={pendingCode !== null}
          existingTrackingCodes={existingTrackingCodes}
          onClose={() => setTrackingTarget(null)}
          onSubmit={async (trackingReference) => {
            const ok = await runSellerOp(trackingTarget.seller, "assign_tracking", {
              shipmentId: trackingTarget.shipment.shipmentId,
              trackingReference,
            });
            if (ok) setTrackingTarget(null);
            return ok;
          }}
        />
      ) : null}
    </section>
  );
}
