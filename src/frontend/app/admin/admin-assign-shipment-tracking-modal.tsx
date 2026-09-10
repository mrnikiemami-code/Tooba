"use client";

import { useState } from "react";
import { Button, Dialog } from "../../design-system";
import type { AdminSellerOrder, AdminShipment } from "./admin-api";

type Props = {
  open: boolean;
  seller: AdminSellerOrder;
  shipment: AdminShipment;
  pending: boolean;
  /** کدهای رهگیری موجود در همین سفارش (برای هشدار سریع UI). */
  existingTrackingCodes: string[];
  onClose: () => void;
  onSubmit: (trackingReference: string) => Promise<boolean>;
};

export function suggestShipmentTracking(shipmentId: string): string {
  const suffix = shipmentId.replace(/[^A-Za-z0-9]/g, "").slice(0, 8).toUpperCase();
  return `TRK-${suffix || Date.now().toString(36).toUpperCase()}`;
}

/**
 * مدال ثبت کد رهگیری مرسوله — ورود دستی یا تولید خودکار؛ تکراری رد می‌شود.
 */
export function AdminAssignShipmentTrackingModal({
  open,
  seller,
  shipment,
  pending,
  existingTrackingCodes,
  onClose,
  onSubmit,
}: Props) {
  const suggested = suggestShipmentTracking(shipment.shipmentId);
  const [value, setValue] = useState(suggested);
  const [error, setError] = useState<string | null>(null);

  function isDuplicate(code: string): boolean {
    const normalized = code.trim().toLowerCase();
    if (!normalized) return false;
    return existingTrackingCodes.some((c) => c.trim().toLowerCase() === normalized);
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="ثبت کد رهگیری مرسوله"
      showCloseButton={false}
    >
      <div className="space-y-3" data-testid="admin-shipment-tracking-dialog">
        <p className="text-xs text-gray-600">
          کد رهگیری را خودتان وارد کنید یا از کد پیشنهادی خودکار استفاده کنید.
          کد تکراری مجاز نیست.
        </p>
        <p className="text-[11px] text-gray-500">
          فروشنده: <span className="font-bold text-gray-800">{seller.sellerDisplayName}</span>
          {" · "}
          {shipment.itemCount.toLocaleString("fa-IR")} قلم · {shipment.carrierDisplayName}
        </p>
        <label className="block text-xs font-bold text-gray-700">
          کد رهگیری
          <input
            className="mt-1 w-full rounded-lg border border-gray-200 px-2 py-1.5 text-sm"
            dir="ltr"
            value={value}
            onChange={(e) => {
              setValue(e.target.value);
              setError(null);
            }}
            data-testid="admin-shipment-tracking-input"
          />
        </label>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            tone="secondary"
            data-testid="admin-shipment-tracking-autofill"
            onClick={() => {
              setValue(suggestShipmentTracking(shipment.shipmentId));
              setError(null);
            }}
          >
            تولید خودکار
          </Button>
        </div>
        {error ? (
          <p className="text-xs font-semibold text-red-700" data-testid="admin-shipment-tracking-error">
            {error}
          </p>
        ) : null}
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" tone="secondary" onClick={onClose} disabled={pending}>
            انصراف
          </Button>
          <Button
            type="button"
            disabled={pending || !value.trim()}
            data-testid="admin-shipment-tracking-submit"
            onClick={() => {
              const trimmed = value.trim();
              if (!trimmed) {
                setError("کد رهگیری الزامی است.");
                return;
              }
              if (isDuplicate(trimmed)) {
                setError("این کد رهگیری قبلاً ثبت شده است. کد دیگری وارد کنید.");
                return;
              }
              void onSubmit(trimmed).then((ok) => {
                if (!ok) {
                  // Toast از والد آمده؛ مدال باز می‌ماند تا کاربر کد را اصلاح کند.
                  setError((prev) => prev ?? "ثبت کد رهگیری ناموفق بود. کد را بررسی کنید.");
                }
              });
            }}
          >
            ثبت کد رهگیری
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
