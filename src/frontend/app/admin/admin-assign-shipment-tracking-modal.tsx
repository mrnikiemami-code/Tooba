"use client";

import { useRef, useState } from "react";
import { Button, Dialog } from "../../design-system";
import type { AdminSellerOrder, AdminShipment } from "./admin-api";

/** پیشوند کد رهگیری داخلی (غیر حاملی) — هم‌ارز `FulfillmentInternalTrackingReference.Prefix`. */
export const TRACKING_REFERENCE_PREFIX = "TRK-";

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

/**
 * TB-257: پیشنهاد کد باید از «کل» شناسه مرسوله ساخته شود، نه از پیشوند کوتاه آن.
 * پیشوند هشت‌کاراکتری قبلی فقط هر ~۶۵ ثانیه تغییر می‌کرد، پس دو مرسوله در یک دقیقه
 * کد یکسان می‌گرفتند و ثبت دومی روی یکتایی `ix_shipments_tracking_reference` می‌خورد.
 * این تابع آینهٔ `FulfillmentInternalTrackingReference.ForShipment` در بک‌اند است؛
 * همان مرسوله همیشه همان پیشنهاد را می‌دهد تا تلاش مجدد/دوبار کلیک همان کد را بازیابی کند.
 */
export function suggestShipmentTracking(shipmentId: string): string {
  const core = shipmentId.replace(/[^A-Za-z0-9]/g, "").toUpperCase();
  return `${TRACKING_REFERENCE_PREFIX}${core || ""}`;
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
  // TB-257: same-tick single flight. The parent's `pending` flag only flips after an await, so a burst of clicks in
  // one tick could post the same parcel several times; the ref closes that window synchronously.
  const inFlight = useRef(false);
  const [submitting, setSubmitting] = useState(false);
  const busy = pending || submitting;

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
          <Button type="button" tone="secondary" onClick={onClose} disabled={busy}>
            انصراف
          </Button>
          <Button
            type="button"
            disabled={busy || !value.trim()}
            data-testid="admin-shipment-tracking-submit"
            onClick={() => {
              if (inFlight.current || pending) return;
              const trimmed = value.trim();
              if (!trimmed) {
                setError("کد رهگیری الزامی است.");
                return;
              }
              if (isDuplicate(trimmed)) {
                setError("این کد رهگیری قبلاً ثبت شده است. کد دیگری وارد کنید.");
                return;
              }
              inFlight.current = true;
              setSubmitting(true);
              void onSubmit(trimmed)
                .then((ok) => {
                  if (!ok) {
                    // Toast از والد آمده؛ مدال باز می‌ماند تا کاربر کد را اصلاح کند.
                    setError((prev) => prev ?? "ثبت کد رهگیری ناموفق بود. کد را بررسی کنید.");
                  }
                })
                .finally(() => {
                  inFlight.current = false;
                  setSubmitting(false);
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
