"use client";

import { useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import { Button, Dialog } from "../../design-system";
import {
  adminHeaders,
  formatAdminStatus,
  type AdminConsolidatedPackage,
  type AdminOrderDetail,
  type AdminSellerOrder,
} from "./admin-api";
import { mapAdminErrorMessage } from "./admin-error-map";
import {
  executeAdminOrderOperation,
  type AdminOrderOperationAction,
} from "./admin-order-operations";

type ShippingMethodOption = { code: string; labelFa: string };

const DEFAULT_METHODS: ShippingMethodOption[] = [
  { code: "post", labelFa: "پست" },
  { code: "tipax", labelFa: "تیپاکس" },
  { code: "snapp_courier", labelFa: "اسنپ / پیک آنلاین" },
  { code: "store_courier", labelFa: "پیک فروشگاه" },
  { code: "in_person", labelFa: "تحویل حضوری" },
];

type EligibleShipmentRow = {
  shipmentId: string;
  sellerOrderId: string;
  sellerDisplayName: string;
  status: string;
  itemCount: number;
  trackingReference: string | null;
  carrierDisplayName: string;
};

type Props = {
  detail: AdminOrderDetail;
  checkoutId: string;
  checkoutActions: AdminOrderOperationAction[];
  pendingCode: string | null;
  setPendingCode: (code: string | null) => void;
  onCompleted?: () => void;
};

function distinctSellerCount(sellers: AdminSellerOrder[]): number {
  const ids = new Set(sellers.map((s) => s.id || s.orderNumber).filter(Boolean));
  return ids.size;
}

function collectEligible(detail: AdminOrderDetail): EligibleShipmentRow[] {
  const rows: EligibleShipmentRow[] = [];
  for (const seller of detail.sellerOrders) {
    for (const shipment of seller.shipments ?? []) {
      const canAdd = shipment.canAddToConsolidatedPackage === true
        || (
          shipment.canAddToConsolidatedPackage !== false
          && shipment.status === "Created"
          && !shipment.activePackageNumber
        );
      if (!canAdd || shipment.status !== "Created") continue;
      if (shipment.activePackageNumber) continue;
      rows.push({
        shipmentId: shipment.shipmentId,
        sellerOrderId: seller.id,
        sellerDisplayName: seller.sellerDisplayName,
        status: shipment.status,
        itemCount: shipment.itemCount,
        trackingReference: shipment.trackingReference,
        carrierDisplayName: shipment.carrierDisplayName,
      });
    }
  }
  return rows;
}

/**
 * بخش بسته‌بندی مرکزی — فقط سفارش‌های چندفروشنده‌ای.
 */
export function AdminConsolidatedPackageSection({
  detail,
  checkoutId,
  checkoutActions,
  pendingCode,
  setPendingCode,
  onCompleted,
}: Props) {
  const multiSeller = distinctSellerCount(detail.sellerOrders) >= 2
    || (detail.sellerCount ?? 0) >= 2
    || detail.sellerOrders.length >= 2;
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);
  const [methodCode, setMethodCode] = useState("post");
  const [tracking, setTracking] = useState("");
  const [methods, setMethods] = useState<ShippingMethodOption[]>(DEFAULT_METHODS);
  const [error, setError] = useState<string | null>(null);

  const eligible = useMemo(() => collectEligible(detail), [detail]);
  const packages = detail.consolidatedPackages ?? [];
  const canCreate = checkoutActions.some((a) => a.code === "create_consolidated_package");

  const selectedSellerCount = useMemo(() => {
    const sellers = new Set(
      eligible.filter((r) => selected.includes(r.shipmentId)).map((r) => r.sellerOrderId),
    );
    return sellers.size;
  }, [eligible, selected]);

  useEffect(() => {
    if (!dialogOpen) return;
    let cancelled = false;
    void fetch("/v1/admin/shipping-methods", { headers: adminHeaders() })
      .then(async (res) => {
        if (!res.ok) return;
        const payload = await res.json().catch(() => null);
        const rows = Array.isArray(payload)
          ? payload
          : Array.isArray((payload as { items?: unknown })?.items)
            ? ((payload as { items: unknown[] }).items)
            : null;
        if (!rows || cancelled) return;
        const mapped = rows.flatMap((raw): ShippingMethodOption[] => {
          if (!raw || typeof raw !== "object") return [];
          const row = raw as Record<string, unknown>;
          const code = String(row.code ?? row.Code ?? "").trim();
          if (!code) return [];
          return [{
            code,
            labelFa: String(row.labelFa ?? row.LabelFa ?? row.label ?? row.Label ?? code),
          }];
        });
        if (mapped.length > 0) setMethods(mapped);
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, [dialogOpen]);

  if (!multiSeller) return null;

  async function runPackageOp(
    code: string,
    body: {
      consolidatedPackageId?: string | null;
      shipmentIds?: string[] | null;
      shippingMethodCode?: string | null;
      trackingReference?: string | null;
      note?: string | null;
    },
  ) {
    const action = checkoutActions.find((a) => {
      if (a.code !== code) return false;
      if (body.consolidatedPackageId && a.consolidatedPackageId) {
        return a.consolidatedPackageId === body.consolidatedPackageId;
      }
      return true;
    });
    if (!action) {
      toast.error("این عملیات در وضعیت فعلی مجاز نیست.");
      return;
    }
    if (action.requiresConfirm && action.confirmMessageFa && !window.confirm(action.confirmMessageFa)) {
      return;
    }
    setPendingCode(code);
    setError(null);
    const result = await executeAdminOrderOperation(checkoutId, {
      code,
      consolidatedPackageId: body.consolidatedPackageId ?? action.consolidatedPackageId ?? null,
      shipmentIds: body.shipmentIds ?? null,
      shippingMethodCode: body.shippingMethodCode ?? null,
      trackingReference: body.trackingReference ?? null,
      note: body.note ?? null,
    });
    setPendingCode(null);
    if (result.state !== "ok") {
      const msg = mapAdminErrorMessage(result.message, "fa");
      setError(msg);
      toast.error(msg);
      return;
    }
    toast.success("عملیات با موفقیت انجام شد.");
    setDialogOpen(false);
    setSelected([]);
    setTracking("");
    onCompleted?.();
  }

  function toggleShipment(id: string) {
    setSelected((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
  }

  function packageAction(pkg: AdminConsolidatedPackage, code: string): AdminOrderOperationAction | undefined {
    return checkoutActions.find(
      (a) => a.code === code && a.consolidatedPackageId === pkg.consolidatedPackageId,
    );
  }

  return (
    <section
      className="rounded-xl border border-indigo-100 bg-indigo-50/30 p-3 md:p-4"
      data-testid="admin-consolidated-package-section"
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-black text-gray-900">بسته‌بندی مرکزی</h3>
        {canCreate ? (
          <button
            type="button"
            className="rounded-lg bg-[#2563EB] px-3 py-1.5 text-xs font-bold text-white disabled:opacity-50"
            disabled={pendingCode !== null}
            data-testid="admin-consolidated-package-create"
            onClick={() => {
              setDialogOpen(true);
              setError(null);
              setSelected([]);
            }}
          >
            ایجاد بسته تجمیعی
          </button>
        ) : null}
      </div>

      {packages.length === 0 ? (
        <p className="mt-2 text-xs text-gray-500" data-testid="admin-consolidated-package-empty">
          هنوز بسته تجمیعی ایجاد نشده است.
        </p>
      ) : (
        <ul className="mt-3 space-y-2">
          {packages.map((pkg) => {
            const canCancel = Boolean(packageAction(pkg, "cancel_consolidated_package"));
            const canDispatch = Boolean(packageAction(pkg, "dispatch_consolidated_package"));
            const canDeliver = Boolean(packageAction(pkg, "deliver_consolidated_package"));
            return (
              <li
                key={pkg.consolidatedPackageId}
                className="rounded-lg border border-gray-200 bg-white p-3 shadow-sm"
                data-testid={`admin-consolidated-package-card-${pkg.consolidatedPackageId}`}
              >
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div>
                    <p className="font-mono text-xs font-bold text-gray-900" dir="ltr">{pkg.packageNumber}</p>
                    <p className="mt-1 text-[11px] text-gray-600">
                      {pkg.sellerCount.toLocaleString("fa-IR")} فروشنده ·{" "}
                      {pkg.memberShipmentCount.toLocaleString("fa-IR")} مرسوله · {pkg.shippingMethodLabel}
                    </p>
                    {pkg.trackingReference ? (
                      <p className="mt-1 text-[11px] text-gray-600" dir="ltr">
                        رهگیری مرکزی: {pkg.trackingReference}
                      </p>
                    ) : null}
                  </div>
                  <span className="rounded-full bg-indigo-50 px-2 py-0.5 text-[10px] font-bold text-indigo-800">
                    {formatAdminStatus(pkg.status)}
                  </span>
                </div>
                <div className="mt-2 flex flex-wrap gap-1">
                  {canCancel ? (
                    <button
                      type="button"
                      className="rounded border border-red-200 px-2 py-1 text-[10px] font-bold text-red-700 disabled:opacity-50"
                      disabled={pendingCode !== null}
                      data-testid={`admin-consolidated-package-cancel-${pkg.consolidatedPackageId}`}
                      onClick={() => void runPackageOp("cancel_consolidated_package", {
                        consolidatedPackageId: pkg.consolidatedPackageId,
                      })}
                    >
                      ابطال بسته تجمیعی
                    </button>
                  ) : null}
                  {canDispatch ? (
                    <button
                      type="button"
                      className="rounded border border-gray-200 px-2 py-1 text-[10px] font-bold text-gray-700 disabled:opacity-50"
                      disabled={pendingCode !== null}
                      data-testid={`admin-consolidated-package-dispatch-${pkg.consolidatedPackageId}`}
                      onClick={() => void runPackageOp("dispatch_consolidated_package", {
                        consolidatedPackageId: pkg.consolidatedPackageId,
                      })}
                    >
                      ارسال بسته تجمیعی
                    </button>
                  ) : null}
                  {canDeliver ? (
                    <button
                      type="button"
                      className="rounded border border-gray-200 px-2 py-1 text-[10px] font-bold text-gray-700 disabled:opacity-50"
                      disabled={pendingCode !== null}
                      data-testid={`admin-consolidated-package-deliver-${pkg.consolidatedPackageId}`}
                      onClick={() => void runPackageOp("deliver_consolidated_package", {
                        consolidatedPackageId: pkg.consolidatedPackageId,
                      })}
                    >
                      تحویل بسته تجمیعی
                    </button>
                  ) : null}
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title="ایجاد بسته تجمیعی"
        showCloseButton={false}
      >
        <div className="space-y-3" data-testid="admin-consolidated-package-create-dialog">
          <p className="text-xs text-gray-600">
            حداقل دو مرسوله از فروشنده‌های متمایز را انتخاب کنید.
          </p>
          {eligible.length === 0 ? (
            <p className="text-xs text-amber-800">مرسولهٔ واجد شرایطی برای بسته‌بندی مرکزی وجود ندارد.</p>
          ) : (
            <ul className="max-h-64 space-y-2 overflow-y-auto">
              {eligible.map((row) => (
                <li key={row.shipmentId} className="rounded border border-gray-100 bg-gray-50 px-2 py-2 text-xs">
                  <label className="flex cursor-pointer items-start gap-2">
                    <input
                      type="checkbox"
                      checked={selected.includes(row.shipmentId)}
                      onChange={() => toggleShipment(row.shipmentId)}
                      data-testid={`admin-consolidated-package-eligible-${row.shipmentId}`}
                    />
                    <span>
                      <span className="font-bold text-gray-900">{row.sellerDisplayName}</span>
                      {" · "}
                      {row.itemCount.toLocaleString("fa-IR")} قلم · {row.carrierDisplayName}
                      {row.trackingReference ? (
                        <>
                          {" · "}
                          <span className="font-mono" dir="ltr">
                            {row.trackingReference}
                          </span>
                        </>
                      ) : null}
                    </span>
                  </label>
                </li>
              ))}
            </ul>
          )}
          <div className="grid gap-2 sm:grid-cols-2">
            <label className="text-xs font-bold text-gray-700">
              روش ارسال مرکزی
              <select
                className="mt-1 w-full rounded-lg border border-gray-200 px-2 py-1.5 text-sm"
                value={methodCode}
                onChange={(e) => setMethodCode(e.target.value)}
                data-testid="admin-consolidated-package-method"
              >
                {methods.map((m) => (
                  <option key={m.code} value={m.code}>{m.labelFa}</option>
                ))}
              </select>
            </label>
            <label className="text-xs font-bold text-gray-700">
              کد رهگیری مرکزی (اختیاری)
              <input
                className="mt-1 w-full rounded-lg border border-gray-200 px-2 py-1.5 text-sm"
                dir="ltr"
                value={tracking}
                onChange={(e) => setTracking(e.target.value)}
                data-testid="admin-consolidated-package-tracking"
              />
            </label>
          </div>
          {error ? <p className="text-xs font-semibold text-red-700">{error}</p> : null}
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" tone="secondary" onClick={() => setDialogOpen(false)}>انصراف</Button>
            <Button
              type="button"
              disabled={pendingCode !== null || selectedSellerCount < 2 || !methodCode}
              data-testid="admin-consolidated-package-submit"
              onClick={() => void runPackageOp("create_consolidated_package", {
                shipmentIds: selected,
                shippingMethodCode: methodCode,
                trackingReference: tracking.trim() || null,
              })}
            >
              ایجاد بسته تجمیعی
            </Button>
          </div>
        </div>
      </Dialog>
    </section>
  );
}
