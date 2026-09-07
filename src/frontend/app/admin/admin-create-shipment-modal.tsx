"use client";

import { useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import { Button, Dialog } from "../../design-system";
import { mapAdminErrorMessage } from "./admin-error-map";
import { executeAdminOrderOperation } from "./admin-order-operations";
import { adminHeaders, type AdminOrderDetail, type AdminOrderLine, type AdminSellerOrder } from "./admin-api";

export type ShippingMethodOption = {
  code: string;
  labelFa: string;
};

const DEFAULT_METHODS: ShippingMethodOption[] = [
  { code: "post", labelFa: "پست" },
  { code: "tipax", labelFa: "تیپاکس" },
  { code: "snapp_courier", labelFa: "اسنپ / پیک آنلاین" },
  { code: "store_courier", labelFa: "پیک فروشگاه" },
  { code: "in_person", labelFa: "تحویل حضوری" },
];

export type CreateShipmentLineSelection = {
  line: AdminOrderLine;
  quantity: number;
};

type Props = {
  open: boolean;
  onClose: () => void;
  checkoutId: string;
  sellerOrder: AdminSellerOrder;
  fulfillmentId: string | null;
  selectedLines: CreateShipmentLineSelection[];
  orderDetail?: AdminOrderDetail | null;
  onCompleted?: () => void;
};

type FieldMap = Record<string, string>;

function remainingShippable(line: AdminOrderLine): number {
  const packed = line.quantityPacked ?? 0;
  const allocated = line.quantityAllocated ?? line.quantityShipped ?? 0;
  return Math.max(0, packed - allocated);
}

function addressSummary(detail: AdminOrderDetail | null | undefined): string {
  if (!detail) return "";
  return [detail.provinceName, detail.cityName, detail.postalAddress, detail.postalCode]
    .filter((x) => x && String(x).trim())
    .join("، ");
}

/**
 * مودال ایجاد مرسوله پویا بر اساس روش ارسال — بدون prompt/alert مرورگر.
 */
export function AdminCreateShipmentModal({
  open,
  onClose,
  checkoutId,
  sellerOrder,
  fulfillmentId,
  selectedLines,
  orderDetail,
  onCompleted,
}: Props) {
  const [methods, setMethods] = useState<ShippingMethodOption[]>(DEFAULT_METHODS);
  const [methodCode, setMethodCode] = useState<string>("post");
  const [fields, setFields] = useState<FieldMap>({});
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const remainingLines = useMemo(
    () =>
      sellerOrder.lines
        .map((line) => ({ line, quantity: remainingShippable(line) }))
        .filter((x) => x.quantity > 0),
    [sellerOrder.lines],
  );

  const displayLines = selectedLines.length > 0 ? selectedLines : remainingLines;
  const canSubmit = Boolean(fulfillmentId) && displayLines.length > 0 && methodCode.trim().length > 0;

  useEffect(() => {
    if (!open) return;
    const recipient = orderDetail?.recipientName ?? "";
    const phone = orderDetail?.contactMobile ?? "";
    const address = addressSummary(orderDetail);
    const postal = orderDetail?.postalCode ?? "";
    setFields({
      recipientName: recipient,
      recipientPhone: phone,
      destinationAddress: address,
      fullAddress: address,
      postalCode: postal,
      province: orderDetail?.provinceName ?? "",
      city: orderDetail?.cityName ?? "",
      pickupAddress: address,
      pickupContactName: recipient,
      pickupContactPhone: phone,
      pickupLocation: "فروشگاه",
      packageCount: "1",
      serviceType: "پیشتاز",
    });
    setMethodCode(methods[0]?.code ?? "post");
    setError(null);
  }, [open, orderDetail, methods]);

  useEffect(() => {
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
        const mapped = rows
          .map((row) => {
            if (!row || typeof row !== "object") return null;
            const r = row as Record<string, unknown>;
            const code = String(r.code ?? r.Code ?? "").trim();
            const labelFa = String(r.labelFa ?? r.LabelFa ?? code).trim();
            return code ? { code, labelFa } : null;
          })
          .filter((x): x is ShippingMethodOption => Boolean(x));
        if (mapped.length > 0) setMethods(mapped);
      })
      .catch(() => {
        /* fallback DEFAULT_METHODS */
      });
    return () => {
      cancelled = true;
    };
  }, []);

  function setField(key: string, value: string) {
    setFields((prev) => ({ ...prev, [key]: value }));
  }

  function buildMetadata(): Record<string, string | number> {
    const num = (key: string) => {
      const raw = fields[key];
      if (!raw || !String(raw).trim()) return undefined;
      const n = Number(raw);
      return Number.isFinite(n) ? n : undefined;
    };
    const str = (key: string) => {
      const v = fields[key]?.trim();
      return v ? v : undefined;
    };

    switch (methodCode) {
      case "post":
        return {
          serviceType: str("serviceType") ?? "",
          packageCount: num("packageCount") ?? 1,
          weightKg: num("weightKg") ?? undefined,
          dimensions: str("dimensions") ?? "",
          declaredValue: num("declaredValue") ?? undefined,
          recipientName: str("recipientName") ?? "",
          recipientPhone: str("recipientPhone") ?? "",
          postalCode: str("postalCode") ?? "",
          destinationAddress: str("destinationAddress") ?? "",
          notes: str("notes") ?? "",
        };
      case "tipax":
        return {
          packageCount: num("packageCount") ?? 1,
          weightKg: num("weightKg") ?? undefined,
          dimensions: str("dimensions") ?? "",
          declaredValue: num("declaredValue") ?? undefined,
          senderContact: str("senderContact") ?? "",
          recipientName: str("recipientName") ?? "",
          recipientPhone: str("recipientPhone") ?? "",
          province: str("province") ?? "",
          city: str("city") ?? "",
          postalCode: str("postalCode") ?? "",
          fullAddress: str("fullAddress") ?? "",
          serviceType: str("serviceType") ?? "",
          notes: str("notes") ?? "",
        };
      case "snapp_courier":
        return {
          pickupAddress: str("pickupAddress") ?? "",
          destinationAddress: str("destinationAddress") ?? "",
          pickupContactName: str("pickupContactName") ?? "",
          pickupContactPhone: str("pickupContactPhone") ?? "",
          recipientName: str("recipientName") ?? "",
          recipientPhone: str("recipientPhone") ?? "",
          pickupWindow: str("pickupWindow") ?? "",
          packageDescription: str("packageDescription") ?? "",
          driverNote: str("driverNote") ?? "",
          externalReference: str("externalReference") ?? "",
        };
      case "store_courier":
        return {
          courierName: str("courierName") ?? "",
          courierPhone: str("courierPhone") ?? "",
          note: str("note") ?? "",
          deliveryReference: str("deliveryReference") ?? "",
        };
      case "in_person":
        return {
          pickupLocation: str("pickupLocation") ?? "",
          readyNote: str("readyNote") ?? "",
        };
      default:
        return {};
    }
  }

  async function submit() {
    if (!canSubmit || !fulfillmentId) return;
    setPending(true);
    setError(null);
    const selections =
      selectedLines.length > 0
        ? selectedLines.map((x) => ({
            orderLineId: x.line.orderLineId || x.line.id,
            quantity: x.quantity,
          }))
        : null;
    const method = methods.find((m) => m.code === methodCode);
    const metadata = buildMetadata();
    // strip undefined
    const cleaned = Object.fromEntries(
      Object.entries(metadata).filter(([, v]) => v !== undefined && v !== ""),
    );
    const result = await executeAdminOrderOperation(checkoutId, {
      code: "create_shipment",
      sellerOrderId: sellerOrder.id,
      fulfillmentId,
      carrierDisplayName: method?.labelFa ?? methodCode,
      shippingMethodCode: methodCode,
      providerMetadataJson: JSON.stringify(cleaned),
      selections,
    });
    setPending(false);
    if (result.state !== "ok") {
      const raw = result.message ?? "order.operation.failed";
      const fa = /^[a-z0-9._-]+$/i.test(raw) ? mapAdminErrorMessage(raw, "fa") : raw;
      setError(fa);
      toast.error(fa);
      return;
    }
    toast.success("عملیات ایجاد مرسوله انجام شد.");
    onClose();
    onCompleted?.();
  }

  function Field({
    label,
    fieldKey,
    placeholder,
  }: {
    label: string;
    fieldKey: string;
    placeholder?: string;
  }) {
    return (
      <label className="block space-y-1">
        <span className="text-xs font-bold text-gray-600">{label}</span>
        <input
          value={fields[fieldKey] ?? ""}
          onChange={(e) => setField(fieldKey, e.target.value)}
          placeholder={placeholder}
          className="w-full rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm"
        />
      </label>
    );
  }

  return (
    <Dialog title="ایجاد مرسوله" open={open} onClose={onClose} showCloseButton={false}>
      <div className="max-h-[75vh] space-y-3 overflow-y-auto text-sm" data-testid="admin-create-shipment-modal">
        <p>
          فروشنده: <strong>{sellerOrder.sellerDisplayName}</strong>
        </p>
        {orderDetail ? (
          <div className="rounded-lg border border-gray-200 bg-gray-50/60 p-2 text-xs text-gray-700">
            <p>
              گیرنده: <strong>{orderDetail.recipientName || "—"}</strong>
              {orderDetail.contactMobile ? ` · ${orderDetail.contactMobile}` : ""}
            </p>
            <p className="mt-1">{addressSummary(orderDetail) || "آدرس ثبت‌شده ندارد"}</p>
          </div>
        ) : null}
        <div className="rounded-lg border border-gray-200 bg-gray-50/60 p-2">
          <p className="mb-1 text-xs font-bold text-gray-600">اقلام انتخاب‌شده</p>
          {displayLines.length === 0 ? (
            <p className="text-xs text-gray-500">قلم قابل ارسال باقی نمانده است.</p>
          ) : (
            <ul className="divide-y divide-gray-200">
              {displayLines.map(({ line, quantity }) => (
                <li key={line.orderLineId ?? line.id} className="flex justify-between gap-2 py-1.5 text-xs">
                  <span className="truncate">{line.title}</span>
                  <span className="shrink-0 tabular-nums text-gray-600">× {quantity.toLocaleString("fa-IR")}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
        <label className="block space-y-1">
          <span className="text-xs font-bold text-gray-600">روش ارسال</span>
          <select
            value={methodCode}
            onChange={(e) => setMethodCode(e.target.value)}
            className="w-full rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm"
            data-testid="admin-create-shipment-carrier"
          >
            {methods.map((option) => (
              <option key={option.code} value={option.code}>
                {option.labelFa}
              </option>
            ))}
          </select>
        </label>

        {methodCode === "post" ? (
          <div className="grid gap-2 sm:grid-cols-2" data-testid="shipment-fields-post">
            <Field label="نوع سرویس" fieldKey="serviceType" />
            <Field label="تعداد بسته" fieldKey="packageCount" />
            <Field label="وزن (کیلو)" fieldKey="weightKg" />
            <Field label="ابعاد" fieldKey="dimensions" />
            <Field label="ارزش اظهارشده" fieldKey="declaredValue" />
            <Field label="نام گیرنده" fieldKey="recipientName" />
            <Field label="موبایل گیرنده" fieldKey="recipientPhone" />
            <Field label="کدپستی" fieldKey="postalCode" />
            <div className="sm:col-span-2">
              <Field label="آدرس مقصد" fieldKey="destinationAddress" />
            </div>
            <div className="sm:col-span-2">
              <Field label="یادداشت" fieldKey="notes" />
            </div>
          </div>
        ) : null}

        {methodCode === "tipax" ? (
          <div className="grid gap-2 sm:grid-cols-2" data-testid="shipment-fields-tipax">
            <Field label="تعداد بسته" fieldKey="packageCount" />
            <Field label="وزن (کیلو)" fieldKey="weightKg" />
            <Field label="ابعاد" fieldKey="dimensions" />
            <Field label="ارزش اظهارشده" fieldKey="declaredValue" />
            <Field label="تماس فرستنده" fieldKey="senderContact" />
            <Field label="نام گیرنده" fieldKey="recipientName" />
            <Field label="موبایل گیرنده" fieldKey="recipientPhone" />
            <Field label="استان" fieldKey="province" />
            <Field label="شهر" fieldKey="city" />
            <Field label="کدپستی" fieldKey="postalCode" />
            <div className="sm:col-span-2">
              <Field label="آدرس کامل" fieldKey="fullAddress" />
            </div>
            <Field label="نوع سرویس" fieldKey="serviceType" />
            <Field label="یادداشت" fieldKey="notes" />
          </div>
        ) : null}

        {methodCode === "snapp_courier" ? (
          <div className="grid gap-2 sm:grid-cols-2" data-testid="shipment-fields-courier">
            <div className="sm:col-span-2">
              <Field label="آدرس مبدأ" fieldKey="pickupAddress" />
            </div>
            <div className="sm:col-span-2">
              <Field label="آدرس مقصد" fieldKey="destinationAddress" />
            </div>
            <Field label="نام تماس مبدأ" fieldKey="pickupContactName" />
            <Field label="موبایل مبدأ" fieldKey="pickupContactPhone" />
            <Field label="نام گیرنده" fieldKey="recipientName" />
            <Field label="موبایل گیرنده" fieldKey="recipientPhone" />
            <Field label="بازه زمانی برداشت" fieldKey="pickupWindow" />
            <Field label="شرح بسته" fieldKey="packageDescription" />
            <Field label="یادداشت راننده" fieldKey="driverNote" />
            <Field label="مرجع خارجی" fieldKey="externalReference" />
          </div>
        ) : null}

        {methodCode === "store_courier" ? (
          <div className="grid gap-2 sm:grid-cols-2" data-testid="shipment-fields-store-courier">
            <Field label="نام پیک" fieldKey="courierName" />
            <Field label="موبایل پیک" fieldKey="courierPhone" />
            <Field label="یادداشت" fieldKey="note" />
            <Field label="مرجع تحویل" fieldKey="deliveryReference" />
          </div>
        ) : null}

        {methodCode === "in_person" ? (
          <div className="grid gap-2 sm:grid-cols-2" data-testid="shipment-fields-in-person">
            <Field label="محل تحویل حضوری" fieldKey="pickupLocation" />
            <Field label="یادداشت آماده‌سازی" fieldKey="readyNote" />
          </div>
        ) : null}

        {!fulfillmentId ? (
          <p className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-800">
            برای این فروشنده fulfillment ثبت نشده است؛ ایجاد مرسوله ممکن نیست.
          </p>
        ) : null}
        {error ? <p className="text-xs text-danger">{error}</p> : null}
        <div className="flex justify-end gap-2 pt-1">
          <Button type="button" tone="secondary" onClick={onClose} disabled={pending}>
            انصراف
          </Button>
          <Button
            type="button"
            tone="primary"
            disabled={!canSubmit || pending}
            onClick={() => void submit()}
            data-testid="admin-create-shipment-submit"
          >
            ایجاد مرسوله
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
