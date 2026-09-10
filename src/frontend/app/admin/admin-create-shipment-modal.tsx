"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Bike,
  MapPin,
  Package,
  Store,
  Truck,
} from "lucide-react";
import { toast } from "react-toastify";
import { Button, Dialog } from "../../design-system";
import { mapAdminErrorMessage } from "./admin-error-map";
import { executeAdminOrderOperation } from "./admin-order-operations";
import { type AdminOrderDetail, type AdminOrderLine, type AdminSellerOrder } from "./admin-api";
import {
  loadShippingMethodTree,
  type ShippingMethodTreeItem,
} from "./shipping-services-api";
import { resolveAdminChromeLocale } from "./admin-chrome-messages";
import type { ReactNode } from "react";

export type ShippingMethodOption = {
  code: string;
  labelFa: string;
};

const FALLBACK_TREE: ShippingMethodTreeItem[] = [
  {
    code: "post",
    labelFa: "پست",
    name: "پست",
    providerKind: "post",
    iconKey: "post",
    colorKey: "blue",
    options: [
      { code: "express", labelFa: "پیشتاز", name: "پیشتاز" },
      { code: "standard", labelFa: "معمولی", name: "معمولی" },
    ],
  },
  {
    code: "tipax",
    labelFa: "تیپاکس",
    name: "تیپاکس",
    providerKind: "tipax",
    iconKey: "tipax",
    colorKey: "amber",
    options: [
      { code: "express", labelFa: "پیشتاز", name: "پیشتاز" },
      { code: "standard", labelFa: "معمولی", name: "معمولی" },
    ],
  },
  {
    code: "snapp_courier",
    labelFa: "اسنپ / پیک آنلاین",
    name: "اسنپ / پیک آنلاین",
    providerKind: "courier",
    iconKey: "courier",
    colorKey: "emerald",
    options: [],
  },
  {
    code: "store_courier",
    labelFa: "پیک فروشگاه",
    name: "پیک فروشگاه",
    providerKind: "store_courier",
    iconKey: "bike",
    colorKey: "violet",
    options: [],
  },
  {
    code: "in_person",
    labelFa: "تحویل حضوری",
    name: "تحویل حضوری",
    providerKind: "in_person",
    iconKey: "store",
    colorKey: "rose",
    options: [],
  },
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

function colorClasses(colorKey: string): string {
  switch (colorKey) {
    case "amber":
      return "bg-amber-100 text-amber-700 ring-amber-200";
    case "emerald":
      return "bg-emerald-100 text-emerald-700 ring-emerald-200";
    case "violet":
      return "bg-violet-100 text-violet-700 ring-violet-200";
    case "rose":
      return "bg-rose-100 text-rose-700 ring-rose-200";
    case "cyan":
      return "bg-cyan-100 text-cyan-700 ring-cyan-200";
    default:
      return "bg-blue-100 text-blue-700 ring-blue-200";
  }
}

function MethodIcon({ iconKey, colorKey }: { iconKey: string; colorKey: string }) {
  const Icon =
    iconKey === "bike" ? Bike
      : iconKey === "store" ? Store
        : iconKey === "courier" || iconKey === "tipax" ? Package
          : Truck;
  return (
    <span className={`inline-flex size-9 shrink-0 items-center justify-center rounded-xl ring-1 ${colorClasses(colorKey)}`}>
      <Icon className="size-4" aria-hidden />
    </span>
  );
}

/**
 * مودال ایجاد مرسوله — جدول دو‌سطحی سرویس ارسال + آیکن رنگی.
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
  const [methods, setMethods] = useState<ShippingMethodTreeItem[]>(FALLBACK_TREE);
  const [methodCode, setMethodCode] = useState<string>("post");
  const [serviceOptionCode, setServiceOptionCode] = useState<string>("express");
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
  const selectedMethod = methods.find((m) => m.code === methodCode) ?? methods[0];
  const serviceOptions = selectedMethod?.options ?? [];
  const selectedServiceOption = serviceOptions.find((o) => o.code === serviceOptionCode) ?? serviceOptions[0];
  const canSubmit = Boolean(fulfillmentId) && displayLines.length > 0 && Boolean(methodCode.trim());

  useEffect(() => {
    if (!open) return;
    const locale = resolveAdminChromeLocale();
    let cancelled = false;
    void loadShippingMethodTree(locale).then((result) => {
      if (cancelled || result.state !== "ok" || !result.data?.length) return;
      setMethods(result.data);
      const first = result.data[0];
      setMethodCode(first.code);
      setServiceOptionCode(first.options[0]?.code ?? "");
    });
    return () => {
      cancelled = true;
    };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const recipient = orderDetail?.recipientName ?? "";
    const phone = orderDetail?.contactMobile ?? "";
    const address = addressSummary(orderDetail);
    const postal = orderDetail?.postalCode ?? "";
    const serviceLabel = selectedServiceOption?.name || selectedServiceOption?.labelFa || "پیشتاز";
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
      serviceType: serviceLabel,
    });
    setError(null);
  }, [open, orderDetail, selectedServiceOption?.code, selectedServiceOption?.name, selectedServiceOption?.labelFa]);

  useEffect(() => {
    const label = selectedServiceOption?.name || selectedServiceOption?.labelFa || "";
    if (!label) return;
    setFields((prev) => ({ ...prev, serviceType: label }));
  }, [selectedServiceOption?.code, selectedServiceOption?.name, selectedServiceOption?.labelFa]);

  function setField(key: string, value: string) {
    setFields((prev) => ({ ...prev, [key]: value }));
  }

  function selectMethod(code: string) {
    setMethodCode(code);
    const method = methods.find((m) => m.code === code);
    setServiceOptionCode(method?.options[0]?.code ?? "");
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
    const method = selectedMethod;
    const metadata = buildMetadata();
    const cleaned = Object.fromEntries(
      Object.entries(metadata).filter(([, v]) => v !== undefined && v !== ""),
    );
    const result = await executeAdminOrderOperation(checkoutId, {
      code: "create_shipment",
      sellerOrderId: sellerOrder.id,
      fulfillmentId,
      carrierDisplayName: method?.name || method?.labelFa || methodCode,
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
    icon,
  }: {
    label: string;
    fieldKey: string;
    placeholder?: string;
    icon?: ReactNode;
  }) {
    return (
      <label className="block space-y-1">
        <span className="inline-flex items-center gap-1.5 text-xs font-bold text-gray-600">
          {icon}
          {label}
        </span>
        <input
          value={fields[fieldKey] ?? ""}
          onChange={(e) => setField(fieldKey, e.target.value)}
          placeholder={placeholder}
          className="w-full rounded-xl border border-gray-200 bg-white px-3 py-2.5 text-sm shadow-sm focus:border-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-100"
        />
      </label>
    );
  }

  return (
    <Dialog title="ایجاد مرسوله" open={open} onClose={onClose} showCloseButton={false} size="xl">
      <div className="flex max-h-[min(82vh,44rem)] flex-col text-sm" data-testid="admin-create-shipment-modal">
        <div className="min-h-0 flex-1 space-y-4 overflow-y-auto pe-1">
        <div className="flex items-start gap-3 rounded-2xl border border-blue-100 bg-gradient-to-l from-blue-50 to-white p-3">
          <span className="inline-flex size-11 items-center justify-center rounded-2xl bg-blue-600 text-white shadow-sm">
            <Truck className="size-5" aria-hidden />
          </span>
          <div>
            <p className="text-base font-black text-gray-900">ایجاد مرسوله</p>
            <p className="mt-0.5 text-xs text-gray-600">
              فروشنده: <strong>{sellerOrder.sellerDisplayName}</strong>
            </p>
          </div>
        </div>

        {orderDetail ? (
          <div className="rounded-2xl border border-emerald-100 bg-emerald-50/50 p-3">
            <div className="flex items-start gap-2">
              <span className="inline-flex size-8 items-center justify-center rounded-xl bg-emerald-100 text-emerald-700 ring-1 ring-emerald-200">
                <MapPin className="size-4" aria-hidden />
              </span>
              <div className="text-xs text-gray-700">
                <p>
                  گیرنده: <strong>{orderDetail.recipientName || "—"}</strong>
                  {orderDetail.contactMobile ? ` · ${orderDetail.contactMobile}` : ""}
                </p>
                <p className="mt-1">{addressSummary(orderDetail) || "آدرس ثبت‌شده ندارد"}</p>
              </div>
            </div>
          </div>
        ) : null}

        <div className="rounded-2xl border border-amber-100 bg-amber-50/40 p-3">
          <div className="mb-2 flex items-center gap-2">
            <span className="inline-flex size-8 items-center justify-center rounded-xl bg-amber-100 text-amber-700 ring-1 ring-amber-200">
              <Package className="size-4" aria-hidden />
            </span>
            <p className="text-xs font-black text-gray-800">اقلام انتخاب‌شده</p>
          </div>
          {displayLines.length === 0 ? (
            <p className="text-xs text-gray-500">قلم قابل ارسال باقی نمانده است.</p>
          ) : (
            <ul className="divide-y divide-amber-100/80">
              {displayLines.map(({ line, quantity }) => (
                <li key={line.orderLineId ?? line.id} className="flex justify-between gap-2 py-1.5 text-xs">
                  <span className="truncate">{line.title}</span>
                  <span className="shrink-0 tabular-nums text-gray-600">× {quantity.toLocaleString("fa-IR")}</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-2xl border border-indigo-100 bg-white shadow-sm" data-testid="admin-create-shipment-method-table">
          <div className="border-b border-indigo-50 bg-indigo-50/60 px-3 py-2">
            <p className="text-xs font-black text-indigo-950">روش‌های ارسال (جدول دو‌سطحی)</p>
            <p className="text-[11px] text-indigo-900/70">نوع سرویس از جدول چندزبانه خوانده می‌شود — فقط یک روش باز است</p>
          </div>
          <table className="min-w-full text-xs">
            <thead className="bg-gray-50 text-gray-600">
              <tr>
                <th className="px-3 py-2 text-start font-bold">روش ارسال</th>
                <th className="px-3 py-2 text-start font-bold">نوع سرویس</th>
              </tr>
            </thead>
            <tbody>
              {methods.map((method) => {
                const selected = method.code === methodCode;
                return (
                  <tr key={method.code} className={selected ? "bg-blue-50/40" : "bg-white"}>
                    <td className="align-top border-t border-gray-100 px-3 py-2">
                      <button
                        type="button"
                        className="flex w-full items-center gap-2 text-start"
                        onClick={() => selectMethod(method.code)}
                        data-testid={`admin-create-shipment-method-${method.code}`}
                      >
                        <MethodIcon iconKey={method.iconKey} colorKey={method.colorKey} />
                        <span className="font-bold text-gray-900">{method.name || method.labelFa}</span>
                      </button>
                    </td>
                    <td className="align-top border-t border-gray-100 px-3 py-2">
                      {method.options.length === 0 ? (
                        <span className="text-gray-400">—</span>
                      ) : selected ? (
                        <div className="flex flex-col gap-1.5">
                          {method.options.map((option) => {
                            const optionSelected = serviceOptionCode === option.code;
                            return (
                              <label
                                key={option.code}
                                className={
                                  optionSelected
                                    ? "flex cursor-pointer items-center gap-2 rounded-lg border border-blue-200 bg-blue-50 px-2 py-1.5 font-bold text-blue-800"
                                    : "flex cursor-pointer items-center gap-2 rounded-lg border border-transparent px-2 py-1.5 text-gray-700 hover:bg-gray-50"
                                }
                              >
                                <input
                                  type="radio"
                                  name="shipment-service-option"
                                  checked={optionSelected}
                                  onChange={() => {
                                    selectMethod(method.code);
                                    setServiceOptionCode(option.code);
                                  }}
                                />
                                <span>{option.name || option.labelFa}</span>
                              </label>
                            );
                          })}
                        </div>
                      ) : (
                        <button
                          type="button"
                          className="text-[11px] font-semibold text-blue-700"
                          onClick={() => selectMethod(method.code)}
                        >
                          انتخاب برای نمایش انواع
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {serviceOptions.length > 0 ? (
          <label className="block space-y-1" data-testid="admin-create-shipment-service-type">
            <span className="text-xs font-bold text-gray-600">نوع سرویس</span>
            <select
              value={serviceOptionCode}
              onChange={(e) => {
                setServiceOptionCode(e.target.value);
                const opt = serviceOptions.find((o) => o.code === e.target.value);
                if (opt) setField("serviceType", opt.name || opt.labelFa);
              }}
              className="w-full rounded-xl border border-gray-200 bg-white px-3 py-2.5 text-sm shadow-sm"
            >
              {serviceOptions.map((option) => (
                <option key={option.code} value={option.code}>
                  {option.name || option.labelFa}
                </option>
              ))}
            </select>
          </label>
        ) : null}

        {/* keep hidden select for tests expecting carrier control */}
        <select
          value={methodCode}
          onChange={(e) => selectMethod(e.target.value)}
          className="sr-only"
          data-testid="admin-create-shipment-carrier"
          aria-hidden
          tabIndex={-1}
        >
          {methods.map((option) => (
            <option key={option.code} value={option.code}>
              {option.labelFa}
            </option>
          ))}
        </select>

        {methodCode === "post" ? (
          <div className="grid gap-3 sm:grid-cols-2" data-testid="shipment-fields-post">
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
          <div className="grid gap-3 sm:grid-cols-2" data-testid="shipment-fields-tipax">
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
            <div className="sm:col-span-2">
              <Field label="یادداشت" fieldKey="notes" />
            </div>
          </div>
        ) : null}

        {methodCode === "snapp_courier" ? (
          <div className="grid gap-3 sm:grid-cols-2" data-testid="shipment-fields-courier">
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
          <div className="grid gap-3 sm:grid-cols-2" data-testid="shipment-fields-store-courier">
            <Field label="نام پیک" fieldKey="courierName" />
            <Field label="موبایل پیک" fieldKey="courierPhone" />
            <Field label="یادداشت" fieldKey="note" />
            <Field label="مرجع تحویل" fieldKey="deliveryReference" />
          </div>
        ) : null}

        {methodCode === "in_person" ? (
          <div className="grid gap-3 sm:grid-cols-2" data-testid="shipment-fields-in-person">
            <Field label="محل تحویل حضوری" fieldKey="pickupLocation" />
            <Field label="یادداشت آماده‌سازی" fieldKey="readyNote" />
          </div>
        ) : null}

        {!fulfillmentId ? (
          <p className="rounded-xl bg-amber-50 px-3 py-2 text-xs text-amber-800">
            برای این فروشنده fulfillment ثبت نشده است؛ ایجاد مرسوله ممکن نیست.
          </p>
        ) : null}
        {error ? <p className="text-xs text-danger">{error}</p> : null}
        </div>
        <div className="flex shrink-0 justify-end gap-2 border-t border-gray-100 bg-white pt-3">
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
            <span className="inline-flex items-center gap-2">
              <Truck className="size-4" aria-hidden />
              ایجاد مرسوله
            </span>
          </Button>
        </div>
      </div>
    </Dialog>
  );
}
