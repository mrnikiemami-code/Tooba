"use client";

import { useMemo, useState } from "react";
import { toast } from "react-toastify";
import { Button, Dialog } from "../../design-system";
import {
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

const METHOD_MISMATCH_FA =
  "برای ایجاد بسته تجمیعی، روش ارسال مرسوله‌های انتخاب‌شده باید یکسان باشد.";

type PackageListTab = "current" | "cancelled";

function isCancelledPackageStatus(status: string | null | undefined): boolean {
  return status === "Cancelled" || status === "Canceled";
}

type EligibleShipmentRow = {
  shipmentId: string;
  sellerOrderId: string;
  sellerDisplayName: string;
  status: string;
  itemCount: number;
  trackingReference: string | null;
  carrierDisplayName: string;
  shippingMethodCode: string;
  shippingMethodLabel: string;
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
      const methodCode = (shipment.shippingMethodCode ?? "").trim();
      const canAdd = shipment.canAddToConsolidatedPackage === true
        || (
          shipment.canAddToConsolidatedPackage !== false
          && shipment.status === "Created"
          && !shipment.activePackageNumber
          && Boolean(methodCode)
        );
      if (!canAdd || shipment.status !== "Created") continue;
      if (shipment.activePackageNumber) continue;
      if (!methodCode) continue;
      rows.push({
        shipmentId: shipment.shipmentId,
        sellerOrderId: seller.id,
        sellerDisplayName: seller.sellerDisplayName,
        status: shipment.status,
        itemCount: shipment.itemCount,
        trackingReference: shipment.trackingReference,
        carrierDisplayName: shipment.carrierDisplayName,
        shippingMethodCode: methodCode,
        shippingMethodLabel: (shipment.shippingMethodLabel ?? "").trim()
          || shipment.carrierDisplayName
          || methodCode,
      });
    }
  }
  return rows;
}

/**
 * بخش بسته‌بندی مرکزی — فقط سفارش‌های چندفروشنده‌ای.
 * روش ارسال از مرسوله‌های عضو inherit می‌شود؛ انتخاب روش جدید در Dialog نیست.
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
  const [tracking, setTracking] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [trackingDialogPkg, setTrackingDialogPkg] = useState<AdminConsolidatedPackage | null>(null);
  const [packageTrackingInput, setPackageTrackingInput] = useState("");
  const [trackingDialogError, setTrackingDialogError] = useState<string | null>(null);
  const [dispatchAfterTracking, setDispatchAfterTracking] = useState(false);
  const [packageTab, setPackageTab] = useState<PackageListTab>("current");

  const eligible = useMemo(() => collectEligible(detail), [detail]);
  const packages = detail.consolidatedPackages ?? [];
  const currentPackages = useMemo(
    () => packages.filter((pkg) => !isCancelledPackageStatus(pkg.status)),
    [packages],
  );
  const cancelledPackages = useMemo(
    () => packages.filter((pkg) => isCancelledPackageStatus(pkg.status)),
    [packages],
  );
  const visiblePackages = packageTab === "cancelled" ? cancelledPackages : currentPackages;
  const canCreate = checkoutActions.some((a) => a.code === "create_consolidated_package");

  const selectedRows = useMemo(
    () => eligible.filter((r) => selected.includes(r.shipmentId)),
    [eligible, selected],
  );

  const selectedSellerCount = useMemo(() => {
    const sellers = new Set(selectedRows.map((r) => r.sellerOrderId));
    return sellers.size;
  }, [selectedRows]);

  const anchorMethod = selectedRows[0]?.shippingMethodCode ?? null;
  const inheritedMethodLabel = selectedRows[0]?.shippingMethodLabel ?? null;
  const selectedMethods = useMemo(
    () => new Set(selectedRows.map((r) => r.shippingMethodCode.toLowerCase())),
    [selectedRows],
  );
  const hasMethodMismatch = selectedMethods.size > 1;

  function isCheckboxEnabled(row: EligibleShipmentRow): boolean {
    if (selected.includes(row.shipmentId)) return true;
    if (!anchorMethod) return true;
    if (row.shippingMethodCode.toLowerCase() !== anchorMethod.toLowerCase()) return false;
    // یک فروشنده نباید دو مرسوله هم‌زمان عضو بسته شود (حداقل دو فروشنده متمایز).
    const alreadySelectedSeller = selectedRows.some((r) => r.sellerOrderId === row.sellerOrderId);
    return !alreadySelectedSeller;
  }

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
    options?: { skipConfirm?: boolean; silentSuccess?: boolean },
  ): Promise<boolean> {
    const action = checkoutActions.find((a) => {
      if (a.code !== code) return false;
      if (body.consolidatedPackageId && a.consolidatedPackageId) {
        return a.consolidatedPackageId === body.consolidatedPackageId;
      }
      return true;
    });
    const allowWithoutProjectedAction = code === "assign_consolidated_package_tracking"
      && Boolean(body.consolidatedPackageId);
    if (!action && !allowWithoutProjectedAction) {
      toast.error("این عملیات در وضعیت فعلی مجاز نیست.");
      return false;
    }
    if (
      !options?.skipConfirm
      && action?.requiresConfirm
      && action.confirmMessageFa
      && !window.confirm(action.confirmMessageFa)
    ) {
      return false;
    }
    setPendingCode(code);
    setError(null);
    const result = await executeAdminOrderOperation(checkoutId, {
      code,
      consolidatedPackageId: body.consolidatedPackageId ?? action?.consolidatedPackageId ?? null,
      shipmentIds: body.shipmentIds ?? null,
      shippingMethodCode: body.shippingMethodCode ?? null,
      trackingReference: body.trackingReference ?? null,
      note: body.note ?? null,
    });
    setPendingCode(null);
    if (result.state !== "ok") {
      const msg = mapAdminErrorMessage(result.message, "fa");
      setError(msg);
      setTrackingDialogError(msg);
      toast.error(msg);
      return false;
    }
    if (!options?.silentSuccess) {
      toast.success("عملیات با موفقیت انجام شد.");
    }
    setDialogOpen(false);
    setSelected([]);
    setTracking("");
    onCompleted?.();
    return true;
  }

  function toggleShipment(id: string) {
    const row = eligible.find((r) => r.shipmentId === id);
    if (!row) return;
    setSelected((prev) => {
      if (prev.includes(id)) return prev.filter((x) => x !== id);
      if (prev.length === 0) return [id];
      const anchor = eligible.find((r) => r.shipmentId === prev[0]);
      if (!anchor) return [id];
      if (row.shippingMethodCode.toLowerCase() !== anchor.shippingMethodCode.toLowerCase()) {
        setError(METHOD_MISMATCH_FA);
        return prev;
      }
      if (prev.some((sid) => eligible.find((r) => r.shipmentId === sid)?.sellerOrderId === row.sellerOrderId)) {
        return prev;
      }
      setError(null);
      return [...prev, id];
    });
  }

  function packageAction(pkg: AdminConsolidatedPackage, code: string): AdminOrderOperationAction | undefined {
    return checkoutActions.find(
      (a) => a.code === code && a.consolidatedPackageId === pkg.consolidatedPackageId,
    );
  }

  function suggestPackageTracking(pkg: AdminConsolidatedPackage): string {
    const suffix = pkg.packageNumber.replace(/^MP-?/i, "").replace(/[^A-Za-z0-9]/g, "").slice(0, 12);
    return `CENTRAL-${suffix || pkg.consolidatedPackageId.slice(0, 8).toUpperCase()}`;
  }

  function openTrackingDialog(pkg: AdminConsolidatedPackage, thenDispatch: boolean) {
    setTrackingDialogPkg(pkg);
    setPackageTrackingInput(pkg.trackingReference?.trim() || suggestPackageTracking(pkg));
    setTrackingDialogError(null);
    setDispatchAfterTracking(thenDispatch);
  }

  async function submitPackageTracking() {
    if (!trackingDialogPkg) return;
    const value = packageTrackingInput.trim();
    if (!value) {
      setTrackingDialogError("کد رهگیری الزامی است.");
      return;
    }
    const packageId = trackingDialogPkg.consolidatedPackageId;
    const shouldDispatch = dispatchAfterTracking;
    const assigned = await runPackageOp(
      "assign_consolidated_package_tracking",
      {
        consolidatedPackageId: packageId,
        trackingReference: value,
      },
      { skipConfirm: true, silentSuccess: shouldDispatch },
    );
    if (!assigned) return;
    setTrackingDialogPkg(null);
    setPackageTrackingInput("");
    setDispatchAfterTracking(false);
    if (shouldDispatch) {
      await runPackageOp("dispatch_consolidated_package", {
        consolidatedPackageId: packageId,
      });
    }
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
              setTracking("");
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
        <>
          <div
            className="mt-3 flex gap-1 rounded-lg bg-white p-0.5 ring-1 ring-indigo-100"
            role="tablist"
            aria-label="فیلتر بسته‌های تجمیعی"
            data-testid="admin-consolidated-package-tabs"
          >
            <button
              type="button"
              role="tab"
              aria-selected={packageTab === "current"}
              data-testid="admin-consolidated-package-tab-current"
              className={
                packageTab === "current"
                  ? "flex-1 rounded-md bg-indigo-50 px-2 py-1.5 text-[11px] font-bold text-indigo-900"
                  : "flex-1 rounded-md px-2 py-1.5 text-[11px] font-semibold text-gray-600 hover:bg-white/70"
              }
              onClick={() => setPackageTab("current")}
            >
              مرسوله جاری
              {currentPackages.length > 0
                ? ` (${currentPackages.length.toLocaleString("fa-IR")})`
                : ""}
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={packageTab === "cancelled"}
              data-testid="admin-consolidated-package-tab-cancelled"
              className={
                packageTab === "cancelled"
                  ? "flex-1 rounded-md bg-red-50 px-2 py-1.5 text-[11px] font-bold text-red-700"
                  : "flex-1 rounded-md px-2 py-1.5 text-[11px] font-semibold text-gray-600 hover:bg-white/70"
              }
              onClick={() => setPackageTab("cancelled")}
            >
              لغو شده‌ها
              {cancelledPackages.length > 0
                ? ` (${cancelledPackages.length.toLocaleString("fa-IR")})`
                : ""}
            </button>
          </div>
          {visiblePackages.length === 0 ? (
            <p
              className="mt-3 text-center text-[11px] text-gray-500"
              data-testid={`admin-consolidated-package-tab-empty-${packageTab}`}
            >
              {packageTab === "cancelled"
                ? "بسته تجمیعی لغوشده‌ای وجود ندارد."
                : "بسته تجمیعی جاری‌ای وجود ندارد."}
            </p>
          ) : (
            <ul
              className="mt-3 space-y-2"
              data-testid={`admin-consolidated-package-list-${packageTab}`}
            >
              {visiblePackages.map((pkg) => {
                const canCancel = Boolean(packageAction(pkg, "cancel_consolidated_package"));
                const canAssignTracking = Boolean(packageAction(pkg, "assign_consolidated_package_tracking"))
                  || (pkg.status === "Created" && !pkg.trackingReference);
                const canDispatch = Boolean(packageAction(pkg, "dispatch_consolidated_package"));
                const canDeliver = Boolean(packageAction(pkg, "deliver_consolidated_package"));
                const needsTracking = !pkg.trackingReference?.trim();
                const packageCancelled = isCancelledPackageStatus(pkg.status);
                return (
                  <li
                    key={pkg.consolidatedPackageId}
                    className={
                      packageCancelled
                        ? "rounded-lg border border-red-100/80 bg-red-50/40 p-3 shadow-sm"
                        : "rounded-lg border border-gray-200 bg-white p-3 shadow-sm"
                    }
                    data-testid={`admin-consolidated-package-card-${pkg.consolidatedPackageId}`}
                    data-cancelled={packageCancelled ? "true" : undefined}
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
                        ) : (
                          <p className="mt-1 text-[11px] font-semibold text-amber-800">
                            کد رهگیری مرکزی ثبت نشده است.
                          </p>
                        )}
                      </div>
                      <span
                        className={
                          packageCancelled
                            ? "rounded-full bg-red-50 px-2 py-0.5 text-[10px] font-bold text-red-600"
                            : "rounded-full bg-indigo-50 px-2 py-0.5 text-[10px] font-bold text-indigo-800"
                        }
                      >
                        {formatAdminStatus(pkg.status)}
                      </span>
                    </div>
                    <div className="mt-2 flex flex-wrap gap-1">
                      {canAssignTracking && needsTracking ? (
                        <button
                          type="button"
                          className="rounded border border-blue-200 px-2 py-1 text-[10px] font-bold text-blue-800 disabled:opacity-50"
                          disabled={pendingCode !== null}
                          data-testid={`admin-consolidated-package-tracking-${pkg.consolidatedPackageId}`}
                          onClick={() => openTrackingDialog(pkg, false)}
                        >
                          ثبت کد رهگیری
                        </button>
                      ) : null}
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
                          onClick={() => {
                            if (needsTracking) {
                              openTrackingDialog(pkg, true);
                              return;
                            }
                            void runPackageOp("dispatch_consolidated_package", {
                              consolidatedPackageId: pkg.consolidatedPackageId,
                            });
                          }}
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
        </>
      )}

      <Dialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        title="ایجاد بسته تجمیعی"
        showCloseButton={false}
      >
        <div className="space-y-3" data-testid="admin-consolidated-package-create-dialog">
          <p className="text-xs text-gray-600">
            حداقل دو مرسوله از فروشنده‌های متمایز با روش ارسال یکسان را انتخاب کنید.
            روش ارسال بسته از مرسوله‌های انتخاب‌شده به ارث می‌رسد.
          </p>
          {eligible.length === 0 ? (
            <p className="text-xs text-amber-800">مرسولهٔ واجد شرایطی برای بسته‌بندی مرکزی وجود ندارد.</p>
          ) : (
            <ul className="max-h-64 space-y-2 overflow-y-auto">
              {eligible.map((row) => {
                const enabled = isCheckboxEnabled(row);
                const disabledReason = !enabled && anchorMethod
                  && row.shippingMethodCode.toLowerCase() !== anchorMethod.toLowerCase()
                  ? METHOD_MISMATCH_FA
                  : !enabled
                    ? "برای هر فروشنده فقط یک مرسوله قابل انتخاب است."
                    : null;
                return (
                  <li key={row.shipmentId} className="rounded border border-gray-100 bg-gray-50 px-2 py-2 text-xs">
                    <label className={`flex items-start gap-2 ${enabled ? "cursor-pointer" : "cursor-not-allowed opacity-60"}`}>
                      <input
                        type="checkbox"
                        checked={selected.includes(row.shipmentId)}
                        disabled={!enabled}
                        onChange={() => toggleShipment(row.shipmentId)}
                        data-testid={`admin-consolidated-package-eligible-${row.shipmentId}`}
                      />
                      <span>
                        <span className="font-bold text-gray-900">{row.sellerDisplayName}</span>
                        {" · "}
                        {row.itemCount.toLocaleString("fa-IR")} قلم · {row.shippingMethodLabel}
                        {row.trackingReference ? (
                          <>
                            {" · "}
                            <span className="font-mono" dir="ltr">
                              {row.trackingReference}
                            </span>
                          </>
                        ) : null}
                        {disabledReason ? (
                          <span className="mt-1 block text-[10px] font-semibold text-amber-800">
                            {disabledReason}
                          </span>
                        ) : null}
                      </span>
                    </label>
                  </li>
                );
              })}
            </ul>
          )}
          <div className="grid gap-2 sm:grid-cols-2">
            <div className="text-xs font-bold text-gray-700" data-testid="admin-consolidated-package-method-readonly">
              روش ارسال بسته تجمیعی
              <p className="mt-1 rounded-lg border border-gray-200 bg-gray-50 px-2 py-1.5 text-sm font-semibold text-gray-900">
                {inheritedMethodLabel ?? "پس از انتخاب مرسوله مشخص می‌شود"}
              </p>
            </div>
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
          {hasMethodMismatch || error ? (
            <p className="text-xs font-semibold text-red-700" data-testid="admin-consolidated-package-method-error">
              {hasMethodMismatch ? METHOD_MISMATCH_FA : error}
            </p>
          ) : null}
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" tone="secondary" onClick={() => setDialogOpen(false)}>انصراف</Button>
            <Button
              type="button"
              disabled={
                pendingCode !== null
                || selectedSellerCount < 2
                || !anchorMethod
                || hasMethodMismatch
              }
              data-testid="admin-consolidated-package-submit"
              onClick={() => {
                if (hasMethodMismatch || !anchorMethod) {
                  setError(METHOD_MISMATCH_FA);
                  return;
                }
                void runPackageOp("create_consolidated_package", {
                  shipmentIds: selected,
                  // Backend inherits from members; send shared code for compatibility.
                  shippingMethodCode: anchorMethod,
                  trackingReference: tracking.trim() || null,
                });
              }}
            >
              ایجاد بسته تجمیعی
            </Button>
          </div>
        </div>
      </Dialog>

      <Dialog
        open={trackingDialogPkg !== null}
        onClose={() => {
          setTrackingDialogPkg(null);
          setDispatchAfterTracking(false);
          setTrackingDialogError(null);
        }}
        title={dispatchAfterTracking ? "ثبت کد رهگیری و ارسال" : "ثبت کد رهگیری بسته تجمیعی"}
        showCloseButton={false}
      >
        <div className="space-y-3" data-testid="admin-consolidated-package-tracking-dialog">
          <p className="text-xs text-gray-600">
            {dispatchAfterTracking
              ? "قبل از ارسال بسته تجمیعی باید کد رهگیری مرکزی ثبت شود. می‌توانید مقدار پیشنهادی را بپذیرید یا خودتان وارد کنید."
              : "کد رهگیری مرکزی را وارد کنید یا با تولید خودکار یک کد بسازید."}
          </p>
          {trackingDialogPkg ? (
            <p className="font-mono text-[11px] text-gray-500" dir="ltr">
              {trackingDialogPkg.packageNumber}
            </p>
          ) : null}
          <label className="block text-xs font-bold text-gray-700">
            کد رهگیری مرکزی
            <input
              className="mt-1 w-full rounded-lg border border-gray-200 px-2 py-1.5 text-sm"
              dir="ltr"
              value={packageTrackingInput}
              onChange={(e) => setPackageTrackingInput(e.target.value)}
              data-testid="admin-consolidated-package-tracking-input"
            />
          </label>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              tone="secondary"
              data-testid="admin-consolidated-package-tracking-autofill"
              onClick={() => {
                if (!trackingDialogPkg) return;
                setPackageTrackingInput(suggestPackageTracking(trackingDialogPkg));
                setTrackingDialogError(null);
              }}
            >
              تولید خودکار
            </Button>
          </div>
          {trackingDialogError ? (
            <p className="text-xs font-semibold text-red-700">{trackingDialogError}</p>
          ) : null}
          <div className="flex justify-end gap-2 pt-1">
            <Button
              type="button"
              tone="secondary"
              onClick={() => {
                setTrackingDialogPkg(null);
                setDispatchAfterTracking(false);
                setTrackingDialogError(null);
              }}
            >
              انصراف
            </Button>
            <Button
              type="button"
              disabled={pendingCode !== null || !packageTrackingInput.trim()}
              data-testid="admin-consolidated-package-tracking-submit"
              onClick={() => void submitPackageTracking()}
            >
              {dispatchAfterTracking ? "ثبت و ارسال" : "ثبت کد رهگیری"}
            </Button>
          </div>
        </div>
      </Dialog>
    </section>
  );
}
