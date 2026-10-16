"use client";

/**
 * TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — کارت «زمان تحویل» مسیر **تک‌گروهی**.
 *
 * این کارت فقط برای سبد تک‌گروهی/تک‌خطی رندر می‌شود و عیناً رفتار و ظاهر قبلی را نگه می‌دارد
 * (افق = فیلدهای همان projection سرور). در سبد چندگروهی هیچ انتخاب‌کنندهٔ تاریخ/بازهٔ سراسری وجود
 * ندارد؛ انتخاب تاریخ/بازه داخل کارت هر گروه ارسال انجام می‌شود.
 */
import { Calendar, Clock } from "lucide-react";

import { formatJalaliDate } from "../../design-system/app-data-grid/jalali.ts";
import { deliveryChipParts } from "./storefront-shipping-format.ts";
import type { StorefrontShippingDateHorizon } from "./storefront-shipping-group-horizon.ts";

export function StorefrontShippingDeliveryCard({
  horizon,
  deliveryDate,
  deliveryTime,
  onSelectDate,
  onSelectWindow,
}: {
  horizon: StorefrontShippingDateHorizon;
  deliveryDate: string;
  deliveryTime: string;
  onSelectDate: (value: string) => void;
  onSelectWindow: (value: string) => void;
}) {
  return (
    <div
      className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm"
      data-storefront-surface-role="card"
      data-testid="shipping-delivery"
    >
      <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-1">
        <Calendar className="w-4 h-4 text-primary" />
        زمان تحویل
      </h3>
      <p className="text-[11px] text-gray-500 mb-4">روز و بازهٔ ساعت را مثل فروشگاه‌های بزرگ از ردیف زیر انتخاب کنید.</p>
      <div className="space-y-5">
        <div>
          <label className="block text-xs md:text-sm font-bold text-gray-700 mb-2">تاریخ تحویل</label>
          {horizon.dates.length === 0 ? (
            <p className="text-xs text-gray-500 bg-gray-50 border border-dashed border-gray-200 rounded-xl p-3">
              ابتدا روش ارسال را انتخاب کنید تا نزدیک‌ترین تاریخ مجاز فروشگاه نمایش داده شود.
            </p>
          ) : (
            <>
              {horizon.minimumDeliveryDate ? (
                <p className="text-[11px] text-gray-500 mb-3">
                  اولین نوبت ممکن:{" "}
                  <span className="font-bold text-gray-800">
                    {formatJalaliDate(`${horizon.minimumDeliveryDate}T12:00:00`, "fa")}
                  </span>
                </p>
              ) : null}
              <div className="-mx-1 flex gap-2 overflow-x-auto pb-1 [scrollbar-width:thin]">
                {horizon.dates.map((d) => {
                  const disabled = Boolean(horizon.minimumDeliveryDate && d.value < horizon.minimumDeliveryDate);
                  const selected = deliveryDate === d.value;
                  const chip = deliveryChipParts(d);
                  return (
                    <button
                      key={d.value}
                      type="button"
                      disabled={disabled}
                      onClick={() => {
                        if (disabled) return;
                        onSelectDate(d.value);
                      }}
                      title={disabled ? "قبل از حداقل زمان فروشگاه مجاز نیست" : `${chip.weekday} ${chip.day} ${chip.month}`}
                      className={`w-[4.75rem] shrink-0 rounded-2xl border-2 px-2 py-3 text-center transition-all disabled:cursor-not-allowed disabled:opacity-40 ${
                        selected
                          ? "border-primary bg-primary/5 text-primary shadow-sm"
                          : "border-gray-200 bg-surface text-gray-700 hover:border-gray-300"
                      }`}
                      data-testid={`shipping-date-${d.value}`}
                    >
                      <p className="text-[11px] font-medium">{chip.weekday}</p>
                      <p className="mt-1 text-xl font-black leading-none tabular-nums">{chip.day}</p>
                      <p className="mt-1 text-[10px] text-gray-500">{chip.month}</p>
                      {d.isEarliest ? <p className="mt-1 text-[9px] font-bold text-primary">نزدیک‌ترین</p> : null}
                    </button>
                  );
                })}
              </div>
            </>
          )}
        </div>
        <div>
          <label className="block text-xs md:text-sm font-bold text-gray-700 mb-2">بازهٔ ساعت تحویل</label>
          <div className="flex flex-wrap gap-2" data-testid="shipping-time" role="radiogroup" aria-label="ساعت تحویل">
            {horizon.timeWindows.length === 0 ? (
              <p className="text-xs text-gray-500">بازه‌ای برای این روز اعلام نشده است.</p>
            ) : (
              horizon.timeWindows.map((t) => {
                const selected = deliveryTime === t.value;
                return (
                  <button
                    key={t.value}
                    type="button"
                    role="radio"
                    aria-checked={selected}
                    onClick={() => onSelectWindow(t.value)}
                    className={`inline-flex items-center gap-2 rounded-2xl border-2 px-3 py-2.5 text-xs font-bold transition-all ${
                      selected
                        ? "border-primary bg-primary/5 text-primary"
                        : "border-gray-200 bg-gray-50 text-gray-600 hover:border-gray-300"
                    }`}
                  >
                    <Clock className="size-3.5 shrink-0" />
                    {t.label}
                  </button>
                );
              })
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
