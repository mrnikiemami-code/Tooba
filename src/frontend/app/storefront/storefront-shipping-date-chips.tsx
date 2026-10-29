"use client";

/**
 * TB-150 / TB-229-R1 / TB-241 / TB-242 / TB-243 — کنترل‌های مشترک زمان تحویل.
 *
 * این فایل فقط DOM مشترک را نگه می‌دارد تا کارت گروه کوچک بماند:
 * - `DateChipStrip` نوار تاریخ مشترک مسیر قدیمی و پیش‌نمایش capture‌شده است (بدون تاریخ ساختگی).
 * - `CapturedSlotPicker` نوبت‌های ساعتی همان روز را نشان می‌دهد که سرور فرستاده است؛ هیچ نوبتی
 *   ساخته، گرد یا با فاصلهٔ حدسی تولید نمی‌شود. ظاهر با `GroupSchedulePicker` هم‌سبک است.
 */
import { Check, Clock } from "lucide-react";

import { useLocale } from "../../lib/i18n/locale-context.tsx";
import { shippingDeliveryMessagesFor } from "../../lib/i18n/messages.ts";
import type { StorefrontCapturedSlot } from "./storefront-shipping-api.ts";
import { deliveryChipParts } from "./storefront-shipping-format.ts";

/** یک تاریخ در نوار تاریخ؛ هم مسیر قدیمی و هم پیش‌نمایش capture‌شده از همین کنترل استفاده می‌کنند. */
export interface DateChipItem {
  value: string;
  label: string;
  subLabel: string;
  isEarliest: boolean;
}

/**
 * نوار تاریخ مشترک: همان DOM/کلاس چپ‌های موجود، بدون هیچ تاریخ ساختگی. `disabledBefore` فقط در
 * مسیر قدیمی معنا دارد؛ در پیش‌نمایش capture‌شده تاریخ‌ها از قبل مجازند.
 */
export function DateChipStrip({
  dates,
  identity,
  testIdPrefix,
  active,
  disabledBefore = null,
  onPick,
}: {
  dates: readonly DateChipItem[];
  identity: string;
  testIdPrefix: string;
  active: string;
  disabledBefore?: string | null;
  onPick: (value: string) => void;
}) {
  const locale = useLocale();
  const copy = shippingDeliveryMessagesFor(locale);
  return (
    <div className="-mx-1 flex gap-2 overflow-x-auto pb-1 [scrollbar-width:thin]">
      {dates.map((d) => {
        const disabled = Boolean(disabledBefore && d.value < disabledBefore);
        const selected = active === d.value;
        const chip = deliveryChipParts(d, locale);
        return (
          <button
            key={d.value}
            type="button"
            disabled={disabled}
            // TB-240-R7: انتخاب باید در DOM هم قابل خواندن باشد؛ یک تاریخ «نزدیک‌ترین» تا وقتی
            // واقعاً انتخاب نشده aria-pressed=false می‌ماند.
            aria-pressed={selected}
            data-selected={selected ? "true" : "false"}
            onClick={() => {
              if (disabled) return;
              onPick(d.value);
            }}
            title={disabled ? copy.beforeMinimum : `${chip.weekday} ${chip.day} ${chip.month}`}
            className={`w-[4.75rem] shrink-0 rounded-2xl border-2 px-2 py-3 text-center transition-all disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary ${
              selected
                ? "border-primary bg-primary/5 text-primary shadow-sm"
                : "border-gray-200 bg-surface text-gray-700 hover:border-gray-300"
            }`}
            data-testid={`${testIdPrefix}-${identity}-${d.value}`}
          >
            <p className="text-[11px] font-medium">{chip.weekday}</p>
            <p className="mt-1 text-xl font-black leading-none tabular-nums">{chip.day}</p>
            <p className="mt-1 text-[10px] text-gray-500">{chip.month}</p>
            {d.isEarliest ? <p className="mt-1 text-[9px] font-bold text-primary">{copy.nearest}</p> : null}
          </button>
        );
      })}
    </div>
  );
}

/**
 * TB-241 / TB-242 / TB-243 — نوبت‌های همان روز به‌صورت کارت‌های قابل‌انتخاب هم‌سبک با
 * `GroupSchedulePicker` (نه pillهای ریز radio). برای یک نوبت همان بازه تنها گزینه است؛ برای چند
 * نوبت تا انتخاب صریح هیچ نوبتی فرض نمی‌شود. همهٔ بازه‌ها فقط از سرور می‌آیند.
 */
export function CapturedSlotPicker({
  slots,
  identity,
  activeSlotKey,
  onPick,
}: {
  slots: readonly StorefrontCapturedSlot[];
  identity: string;
  activeSlotKey: string | null;
  onPick: (startTicks: number, endTicks: number) => void;
}) {
  const locale = useLocale();
  const copy = shippingDeliveryMessagesFor(locale);
  const groupName = `shipping-slot-${identity}`;
  return (
    <fieldset className="mt-3 min-w-0" data-testid={`shipping-group-captured-slots-${identity}`}>
      <legend className="mb-2 text-[11px] md:text-xs font-bold text-gray-700 flex items-center gap-1.5">
        <Clock className="size-3.5 text-primary shrink-0" aria-hidden />
        {copy.deliveryHourLegend}
      </legend>
      <div
        className="grid grid-cols-2 gap-2 min-w-0 sm:grid-cols-3"
        role="radiogroup"
        aria-label={copy.deliveryHourAria}
      >
        {slots.map((slot) => {
          const chosen = activeSlotKey === slot.slotKey;
          const range = `${slot.opensAt}–${slot.closesAt}`;
          return (
            <label
              key={slot.slotKey}
              className={`relative inline-flex min-h-11 min-w-0 cursor-pointer items-center gap-2 rounded-2xl border-2 px-3 py-2.5 text-[13px] md:text-sm font-bold transition-all ${
                chosen
                  ? "border-primary bg-primary/5 text-primary"
                  : "border-gray-200 bg-gray-50 text-gray-600 hover:border-gray-300"
              }`}
              data-testid={`shipping-group-captured-slot-${identity}-${slot.slotKey}`}
              data-selected={chosen ? "true" : "false"}
            >
              <input
                type="radio"
                className="peer sr-only"
                name={groupName}
                value={slot.slotKey}
                checked={chosen}
                onChange={() => onPick(slot.startTicks, slot.endTicks)}
              />
              <Clock className="size-3.5 shrink-0 opacity-80" aria-hidden />
              <bdi dir="ltr" className="min-w-0 whitespace-nowrap tabular-nums">
                {range}
              </bdi>
              {chosen ? <Check className="ms-auto size-3.5 shrink-0" aria-hidden /> : null}
              <span
                className="pointer-events-none absolute inset-0 rounded-2xl ring-2 ring-transparent peer-focus-visible:ring-primary"
                aria-hidden
              />
            </label>
          );
        })}
      </div>
      {slots.length > 1 && !activeSlotKey ? (
        <p
          className="mt-2 text-[11px] font-semibold text-amber-700"
          data-testid={`shipping-group-captured-slot-required-${identity}`}
        >
          {copy.pickOneHour}
        </p>
      ) : null}
    </fieldset>
  );
}
