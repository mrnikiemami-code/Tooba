"use client";

/**
 * TB-150 / TB-229-R1 / TB-241 — کنترل‌های مشترک زمان تحویل.
 *
 * این فایل فقط DOM مشترک را نگه می‌دارد تا کارت گروه کوچک بماند:
 * - `DateChipStrip` نوار تاریخ مشترک مسیر قدیمی و پیش‌نمایش capture‌شده است (بدون تاریخ ساختگی).
 * - `CapturedSlotPicker` نوبت‌های ساعتی همان روز را نشان می‌دهد که سرور فرستاده است؛ هیچ نوبتی
 *   ساخته، گرد یا با فاصلهٔ حدسی تولید نمی‌شود.
 */
import { Clock } from "lucide-react";

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
  return (
    <div className="-mx-1 flex gap-2 overflow-x-auto pb-1 [scrollbar-width:thin]">
      {dates.map((d) => {
        const disabled = Boolean(disabledBefore && d.value < disabledBefore);
        const selected = active === d.value;
        const chip = deliveryChipParts(d);
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
            title={disabled ? "قبل از حداقل زمان فروشگاه مجاز نیست" : `${chip.weekday} ${chip.day} ${chip.month}`}
            className={`w-[4.75rem] shrink-0 rounded-2xl border-2 px-2 py-3 text-center transition-all disabled:cursor-not-allowed disabled:opacity-40 ${
              selected
                ? "border-primary bg-primary/5 text-primary shadow-sm"
                : "border-gray-200 bg-surface text-gray-700 hover:border-gray-300"
            }`}
            data-testid={`${testIdPrefix}-${identity}-${d.value}`}
          >
            <p className="text-[11px] font-medium">{chip.weekday}</p>
            <p className="mt-1 text-xl font-black leading-none tabular-nums">{chip.day}</p>
            <p className="mt-1 text-[10px] text-gray-500">{chip.month}</p>
            {d.isEarliest ? <p className="mt-1 text-[9px] font-bold text-primary">نزدیک‌ترین</p> : null}
          </button>
        );
      })}
    </div>
  );
}

/**
 * TB-241 — نوبت‌های همان روز. برای یک نوبت، همان بازه به‌عنوان تنها گزینه دیده می‌شود؛ برای چند
 * نوبت، رادیو واقعی و تا انتخاب هیچ نوبتی فرض نمی‌شود.
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
  return (
    <fieldset className="mt-2 grid gap-1" data-testid={`shipping-group-captured-slots-${identity}`}>
      <legend className="text-[10px] text-gray-500 flex items-center gap-1">
        <Clock className="w-2.5 h-2.5" /> ساعت تحویل همین روز
      </legend>
      <div className="flex flex-wrap gap-1.5">
        {slots.map((slot) => {
          const chosen = activeSlotKey === slot.slotKey;
          return (
            <label
              key={slot.slotKey}
              className={`inline-flex min-h-8 cursor-pointer items-center gap-1 rounded-lg border px-2 text-[11px] ${
                chosen ? "border-primary bg-primary/10 text-primary font-bold" : "border-gray-200 bg-white text-gray-700"
              }`}
              data-testid={`shipping-group-captured-slot-${identity}-${slot.slotKey}`}
            >
              <input
                type="radio"
                className="size-3"
                name={`shipping-slot-${identity}`}
                checked={chosen}
                onChange={() => onPick(slot.startTicks, slot.endTicks)}
              />
              <bdi dir="ltr">
                {slot.opensAt}–{slot.closesAt}
              </bdi>
            </label>
          );
        })}
      </div>
      {slots.length > 1 && !activeSlotKey ? (
        <p className="text-[10px] font-semibold text-amber-700" data-testid={`shipping-group-captured-slot-required-${identity}`}>
          یکی از ساعت‌های بالا را انتخاب کنید.
        </p>
      ) : null}
    </fieldset>
  );
}
