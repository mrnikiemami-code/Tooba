"use client";

/**
 * TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229 — پیش‌نمایش فقط‌خواندنی گروه‌های ارسال
 * capture‌شده.
 *
 * این بخش **جایگزین** کارت‌های موجود نیست و به فهرست روش‌ها/قیمت قدیمی چیزی تزریق نمی‌کند: فقط
 * نتیجهٔ واقعی سرور (سبد → انتساب Offer → انتخاب ۲۱۹ → یک ترکیب ۲۱۸) را با همان کلاس‌های کارت
 * موجود نشان می‌دهد تا معلوم شود کدام گروه‌ها و با چه مبلغ/تاریخی واقعاً پاسخ داده‌اند.
 *
 * هیچ انتخاب/ثبتی از این‌جا انجام نمی‌شود: انتخاب تاریخ فقط بازهٔ **همان روز** را نشان می‌دهد و
 * دکمه‌ای برای ذخیره/ثبت وجود ندارد، چون سرور هم این مسیر را پیش از نوشتن رد می‌کند.
 */
import { AlertTriangle, Calendar, Clock, Package, Store } from "lucide-react";
import { useState } from "react";

import { formatOfferAmount } from "./storefront-api.ts";
import type { StorefrontCapturedPreview } from "./storefront-shipping-api.ts";
import { deliveryChipParts, toPersianDigits } from "./storefront-shipping-format.ts";

/** توضیح کوتاه و پایدار برای هر کد امتناع سرور؛ کد ناشناخته متن عمومی می‌گیرد. */
const REASON_TEXT: Record<string, string> = {
  "storefront.captured_preview.unassigned": "مسئولیت ارسال این کالا هنوز تعیین نشده است.",
  "storefront.captured_preview.type_unavailable": "نوع ارسال این کالا فعال نیست.",
  "storefront.captured_preview.seller_owner_unsupported": "ارسال فروشنده در این نصب پشتیبانی نمی‌شود.",
  "storefront.captured_preview.no_selected_capture": "برای این گروه هنوز روش ارسالی انتخاب نشده است.",
  "storefront.captured_preview.destination_required": "برای زمان و هزینه، مقصد را انتخاب کنید.",
  "storefront.captured_preview.assignment_contradictory": "اطلاعات ارسال این کالاها کامل نیست.",
  "storefront.captured_preview.assignment_missing": "اطلاعات ارسال این کالاها کامل نیست.",
  "storefront.captured_preview.composition_refused": "محاسبهٔ ارسال این سبد ممکن نشد.",
};

function reasonText(code: string | null): string {
  if (!code) return "این گروه ارسال در دسترس نیست.";
  return REASON_TEXT[code] ?? "این گروه ارسال در دسترس نیست.";
}

export function StorefrontShippingCapturedPreview({
  preview,
  sellerNames,
}: {
  preview: StorefrontCapturedPreview | null;
  sellerNames: Readonly<Record<string, string>>;
}) {
  const [picked, setPicked] = useState<Record<string, string>>({});
  if (!preview?.enabled) return null;

  return (
    <div className="mt-4 space-y-3" data-testid="shipping-captured-preview">
      <p className="text-[11px] text-gray-500" data-testid="shipping-captured-preview-notice">
        پیش‌نمایش روش‌های ارسال هر گروه؛ فعلاً فقط‌خواندنی است و انتخاب/ثبت روش ارسال هنوز فعال نیست.
      </p>
      {preview.groups.length === 0 ? (
        <p
          className="text-[11px] text-gray-500 bg-surface border border-dashed border-gray-200 rounded-xl p-3"
          data-testid="shipping-captured-preview-empty"
        >
          {reasonText(preview.unavailableReason)}
        </p>
      ) : null}
      {preview.groups.map((group) => {
        const seller = sellerNames[group.sellerPartyId] ?? "";
        const key = group.groupKey;
        return (
          <div
            key={key}
            className="rounded-2xl border border-gray-200 bg-gray-50/60 p-3 md:p-4"
            data-testid={`shipping-captured-group-${key}`}
          >
            <div className="flex items-start justify-between gap-2">
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <Store className="w-4 h-4 text-primary shrink-0" />
                  <p className="text-xs md:text-sm font-bold text-gray-900 truncate">{seller}</p>
                </div>
                <p className="text-[10px] md:text-[11px] text-gray-500 mt-1 truncate">
                  {group.typeLabel ?? "نوع ارسال تعیین‌نشده"}
                </p>
              </div>
              <span className="text-[11px] md:text-xs font-black text-gray-900 shrink-0">
                {formatOfferAmount(group.finalGoodsAmountAfterDiscountAndTax, preview.currency)}
              </span>
            </div>

            {group.unavailableReason ? (
              <p
                className="mt-3 flex items-start gap-1.5 text-[11px] text-amber-700 bg-amber-50 border border-amber-200 rounded-xl p-3"
                data-testid={`shipping-captured-group-unavailable-${key}`}
              >
                <AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                {reasonText(group.unavailableReason)}
              </p>
            ) : null}

            {group.methods.map((method) => {
              const methodKey = `${key}|${method.serviceCode}|${method.optionCode ?? ""}`;
              const active = picked[methodKey] ?? method.earliestEligibleDate ?? "";
              const activeDate = method.availableDates.find((d) => d.value === active) ?? null;
              const freight = method.freightAmount;
              const packaging = method.packagingAmount;
              return (
                <div
                  key={methodKey}
                  className="mt-3 border-t border-dashed border-gray-200 pt-3"
                  data-testid={`shipping-captured-method-${methodKey}`}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="text-[11px] md:text-xs font-bold text-gray-800 flex items-center gap-1.5">
                        <Package className="w-3.5 h-3.5 text-primary shrink-0" />
                        {method.methodLabel}
                      </p>
                      <p className="text-[10px] text-gray-500 mt-0.5">
                        {freight === null ? "کرایه نامشخص" : `کرایه ${formatOfferAmount(freight, method.currency)}`}
                        {packaging === null
                          ? ""
                          : ` · بسته‌بندی ${formatOfferAmount(packaging, method.currency)}`}
                      </p>
                    </div>
                    {method.earliestEligibleDate ? (
                      <span className="text-[10px] text-gray-600 shrink-0 flex items-center gap-1">
                        <Clock className="w-2.5 h-2.5" />
                        {toPersianDigits(method.earliestEligibleDate)}
                      </span>
                    ) : null}
                  </div>

                  {method.availableDates.length === 0 ? (
                    <p
                      className="mt-2 text-[11px] text-gray-500"
                      data-testid={`shipping-captured-dates-empty-${methodKey}`}
                    >
                      {reasonText(method.unavailableReason)}
                    </p>
                  ) : (
                    <>
                      <p className="mt-2 text-[10px] text-gray-500 flex items-center gap-1">
                        <Calendar className="w-2.5 h-2.5" /> تاریخ‌های مجاز همین روش
                      </p>
                      <div className="-mx-1 flex gap-2 overflow-x-auto pb-1 [scrollbar-width:thin]">
                        {method.availableDates.map((d) => {
                          const chip = deliveryChipParts(d);
                          const selected = active === d.value;
                          return (
                            <button
                              key={d.value}
                              type="button"
                              aria-pressed={selected}
                              onClick={() => setPicked((prev) => ({ ...prev, [methodKey]: d.value }))}
                              className={`w-[4.75rem] shrink-0 rounded-2xl border-2 px-2 py-3 text-center transition-all ${
                                selected
                                  ? "border-primary bg-primary/5 text-primary shadow-sm"
                                  : "border-gray-200 bg-surface text-gray-700 hover:border-gray-300"
                              }`}
                              data-testid={`shipping-captured-date-${methodKey}-${d.value}`}
                            >
                              <p className="text-[11px] font-medium">{chip.weekday}</p>
                              <p className="mt-1 text-xl font-black leading-none tabular-nums">{chip.day}</p>
                              <p className="text-[9px] text-gray-500 mt-0.5">{chip.month}</p>
                            </button>
                          );
                        })}
                      </div>
                      {activeDate ? (
                        <p
                          className="mt-1 text-[10px] text-gray-600"
                          data-testid={`shipping-captured-interval-${methodKey}`}
                        >
                          بازهٔ همین روز: {activeDate.subLabel || "ثبت نشده"}
                        </p>
                      ) : null}
                    </>
                  )}
                </div>
              );
            })}
          </div>
        );
      })}
    </div>
  );
}
