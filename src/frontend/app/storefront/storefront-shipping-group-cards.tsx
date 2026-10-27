"use client";

/**
 * TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150 — کارت مستقل برای هر گروه ارسال واقعی.
 *
 * هر کارت = یک گروه ارسال (فروشندهٔ واقعی سبد × روش canonical) با:
 * نام فروشنده، کالاها/تعداد، خدمت ارسال، هزینهٔ همان گروه، «اولین نوبت ممکن» و انتخاب تاریخ/بازهٔ
 * **خودش**. هیچ انتخاب‌کنندهٔ تاریخ/بازهٔ سراسری وجود ندارد و افق هیچ گروهی با گروه دیگر تلاقی
 * نمی‌گیرد؛ همهٔ تاریخ/بازه‌ها از ردیف authoritative سرور همان گروه می‌آید.
 *
 * خطوطی که هنوز روش ندارند در بلوک «انتخاب روش» همان فروشنده می‌مانند (رفتار قبلی، بدون ادغام
 * فروشندگان) و به‌محض انتخاب روش، گروه مستقل آن‌ها ساخته می‌شود.
 */
import { Calendar, Check, Clock, Package, Store } from "lucide-react";

import { formatOfferAmount } from "./storefront-api.ts";
import type { StorefrontCartLine } from "./storefront-cart-api.ts";
import { deliveryChipParts, toPersianDigits } from "./storefront-shipping-format.ts";
import { formatJalaliDate } from "../../design-system/app-data-grid/jalali.ts";
import type { StorefrontShippingMethod } from "./storefront-shipping-api.ts";
import type {
  StorefrontShippingGroupSchedule,
  StorefrontShippingGroupSelection,
} from "./storefront-shipping-group-schedule.ts";
import { StorefrontShippingUnavailableMethods } from "./storefront-shipping-unavailable-methods.tsx";
import type { StorefrontShippingUnavailableNotice } from "./storefront-shipping-unavailable-methods.ts";
import { StorefrontShippingCapturedPreview } from "./storefront-shipping-captured-preview.tsx";
import type { StorefrontCapturedPreview } from "./storefront-shipping-api.ts";

export interface ShippingGroupCard {
  /** کلید پایدار گروه در کلاینت (فروشندهٔ واقعی + روش canonical). */
  key: string;
  /** هویت نمایشی/آزمون: شناسهٔ پایدار سرور در صورت وجود، وگرنه کلید پیش‌نویس. */
  identity: string;
  sellerPartyId: string;
  sellerDisplayName: string;
  methodCode: string;
  methodLabel: string;
  amount: number;
  lines: StorefrontCartLine[];
}

export interface UnassignedSellerBlock {
  sellerPartyId: string;
  sellerDisplayName: string;
  lines: StorefrontCartLine[];
}

function iconFor(method: StorefrontShippingMethod) {
  const key = method.iconKey || method.serviceCode;
  if (key.includes("zap") || key.includes("express") || method.methodCode.includes("express")) return Clock;
  if (key.includes("store") || key.includes("person") || method.serviceCode === "in_person") return Store;
  return Package;
}

/** روش‌های همین گروه: هزینهٔ گروه = هزینهٔ روش همان گروه (از سرور، بدون محاسبهٔ کلاینت). */
function GroupSchedulePicker({
  schedule,
  selection,
  identity,
  onSelectDate,
  onSelectWindow,
}: {
  schedule: StorefrontShippingGroupSchedule | null;
  selection: StorefrontShippingGroupSelection | undefined;
  identity: string;
  onSelectDate: (value: string) => void;
  onSelectWindow: (value: string) => void;
}) {
  const deliveryDate = selection?.deliveryDate ?? "";
  const deliveryTime = selection?.deliveryTime ?? "";

  if (!schedule?.resolved) {
    return (
      <p
        className="mt-3 text-[11px] text-gray-500 bg-surface border border-dashed border-gray-200 rounded-xl p-3"
        data-testid={`shipping-group-schedule-pending-${identity}`}
      >
        زمان تحویل این گروه در دسترس نیست؛ مقصد یا روش این گروه را بررسی کنید. هیچ تاریخی به‌جای پاسخ
        سرور ساخته نمی‌شود.
      </p>
    );
  }

  return (
    <div className="mt-3 border-t border-dashed border-gray-200 pt-3" data-testid={`shipping-group-schedule-${identity}`}>
      <p className="text-[11px] md:text-xs font-bold text-gray-700 mb-1 flex items-center gap-1.5">
        <Calendar className="w-3.5 h-3.5 text-primary" />
        زمان تحویل این گروه (مستقل)
      </p>
      {schedule.minimumDeliveryDate ? (
        <p className="text-[11px] text-gray-500 mb-2">
          اولین نوبت ممکن:{" "}
          <span className="font-bold text-gray-800">
            {formatJalaliDate(`${schedule.minimumDeliveryDate}T12:00:00`, "fa")}
          </span>
        </p>
      ) : null}
      <div className="-mx-1 flex gap-2 overflow-x-auto pb-1 [scrollbar-width:thin]">
        {schedule.dates.map((d) => {
          const disabled = Boolean(schedule.minimumDeliveryDate && d.value < schedule.minimumDeliveryDate);
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
              data-testid={`shipping-group-date-${identity}-${d.value}`}
            >
              <p className="text-[11px] font-medium">{chip.weekday}</p>
              <p className="mt-1 text-xl font-black leading-none tabular-nums">{chip.day}</p>
              <p className="mt-1 text-[10px] text-gray-500">{chip.month}</p>
              {d.isEarliest ? <p className="mt-1 text-[9px] font-bold text-primary">نزدیک‌ترین</p> : null}
            </button>
          );
        })}
      </div>
      <div className="mt-3">
        <p className="text-[11px] md:text-xs font-bold text-gray-700 mb-2">بازهٔ ساعت تحویل همین گروه</p>
        <div
          className="flex flex-wrap gap-2"
          data-testid={`shipping-group-time-${identity}`}
          role="radiogroup"
          aria-label="ساعت تحویل این گروه"
        >
          {schedule.timeWindows.map((t) => {
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
                data-testid={`shipping-group-window-${identity}-${t.value}`}
              >
                <Clock className="size-3.5 shrink-0" />
                {t.label}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

function MethodChooser({
  lineId,
  selectedCode,
  methods,
  testIdPrefix,
  onSelectMethod,
  className = "",
}: {
  lineId: string;
  selectedCode: string;
  methods: readonly StorefrontShippingMethod[];
  testIdPrefix: string;
  onSelectMethod: (lineId: string, methodCode: string) => void;
  className?: string;
}) {
  if (methods.length === 0) return null;
  return (
    <div className={`grid grid-cols-1 sm:grid-cols-2 gap-2 ${className}`} data-testid={`shipping-line-methods-${lineId}`}>
      {methods.map((m) => {
        const Icon = iconFor(m);
        const selected = selectedCode === m.methodCode;
        return (
          <button
            key={`${lineId}-${m.methodCode}`}
            type="button"
            onClick={() => onSelectMethod(lineId, m.methodCode)}
            className={`relative flex items-center gap-2 p-3 rounded-2xl border-2 text-right transition-all ${
              selected ? "border-primary bg-primary/5 shadow-sm" : "border-gray-200 hover:border-gray-300 bg-surface"
            }`}
            data-testid={`${testIdPrefix}-${m.methodCode}`}
          >
            <Icon className={`w-4 h-4 shrink-0 ${selected ? "text-primary" : "text-gray-400"}`} />
            <div className="min-w-0 flex-1">
              <p className={`text-[11px] md:text-xs font-bold ${selected ? "text-primary" : "text-gray-700"}`}>
                {m.label}
              </p>
              <p className="text-[10px] text-gray-500 mt-0.5 flex items-center gap-0.5">
                <Clock className="w-2.5 h-2.5" /> {m.estimationLabel}
              </p>
            </div>
            {selected ? <Check className="w-3.5 h-3.5 text-primary shrink-0" /> : null}
          </button>
        );
      })}
    </div>
  );
}

function CartLineRow({
  line,
  methodCode,
  methods,
  testIdPrefix,
  onSelectMethod,
}: {
  line: StorefrontCartLine;
  methodCode: string;
  methods: readonly StorefrontShippingMethod[];
  testIdPrefix: string;
  onSelectMethod: (lineId: string, methodCode: string) => void;
}) {
  return (
    <li className="rounded-xl bg-surface border border-gray-100 p-2" data-testid={`shipping-group-line-${line.lineId}`}>
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-xl bg-gray-100 flex items-center justify-center shrink-0 overflow-hidden">
          {line.mediaAssetId ? (
            <img src={`/media/${line.mediaAssetId}`} alt="" className="w-full h-full object-cover" />
          ) : (
            <Package className="w-4 h-4 text-gray-400" />
          )}
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-[11px] md:text-xs font-bold text-gray-800 line-clamp-1">{line.title}</p>
          <p className="text-[10px] text-gray-500">تعداد: {toPersianDigits(line.quantity)}</p>
        </div>
      </div>
      <MethodChooser
        lineId={line.lineId}
        selectedCode={methodCode}
        methods={methods}
        testIdPrefix={testIdPrefix}
        onSelectMethod={onSelectMethod}
        className="mt-2"
      />
    </li>
  );
}

export function StorefrontShippingGroupCards({
  groups,
  unassigned,
  methodByLine,
  methods,
  unavailableNotices = [],
  currency,
  schedules,
  selections,
  capturedPreview = null,
  onSelectMethod,
  onSelectDate,
  onSelectWindow,
}: {
  groups: readonly ShippingGroupCard[];
  unassigned: readonly UnassignedSellerBlock[];
  methodByLine: Readonly<Record<string, string>>;
  methods: readonly StorefrontShippingMethod[];
  /**
   * TB-165: توضیح سطح-روش برای روش‌های بدون نرخ معتبر. نرخ‌دهی مالک روش به فروشندهٔ سبد وابسته
   * نیست، بنابراین این بلوک یک‌بار و بدون نسبت‌دادن به فروشندهٔ خاص رندر می‌شود و هرگز روش را
   * قابل انتخاب نمی‌کند.
   */
  unavailableNotices?: readonly StorefrontShippingUnavailableNotice[];
  currency: string;
  schedules: Readonly<Record<string, StorefrontShippingGroupSchedule>>;
  selections: Readonly<Record<string, StorefrontShippingGroupSelection>>;
  /** TB-229: پیش‌نمایش خواندنی گروه‌های capture‌شده؛ `null` یعنی کلید خاموش و رفتار قبلی. */
  capturedPreview?: StorefrontCapturedPreview | null;
  onSelectMethod: (lineId: string, methodCode: string) => void;
  onSelectDate: (groupKey: string, value: string) => void;
  onSelectWindow: (groupKey: string, value: string) => void;
}) {
  // TB-229: نام نمایشی فروشنده از همان گروه‌های واقعی سبد؛ هیچ نام/شناسهٔ ساختگی ساخته نمی‌شود.
  const sellerNames: Record<string, string> = {};
  for (const group of groups) sellerNames[group.sellerPartyId] = group.sellerDisplayName;
  for (const seller of unassigned) sellerNames[seller.sellerPartyId] = seller.sellerDisplayName;

  return (
    <div
      className="bg-surface rounded-2xl border border-gray-200 p-4 md:p-5 shadow-sm"
      data-storefront-surface-role="card"
      data-testid="shipping-groups"
    >
      <h3 className="text-sm md:text-base font-black text-gray-900 flex items-center gap-2 mb-1">
        <Package className="w-4 h-4 text-primary" />
        اقلام ارسال به‌تفکیک گروه ارسال
      </h3>
      <p className="text-[11px] text-gray-500 mb-4">
        هر گروه ارسال زمان تحویل مستقل خودش را دارد؛ هزینه و تاریخ هر گروه از نرخ و وعدهٔ معتبر همان گروه
        می‌آید و با گروه‌های دیگر اشتراک گذاشته نمی‌شود.
      </p>

      <StorefrontShippingUnavailableMethods
        notices={unavailableNotices}
        testIdPrefix="shipping-group-unavailable"
      />

      <StorefrontShippingCapturedPreview preview={capturedPreview} sellerNames={sellerNames} />

      <div className="space-y-4">
        {unassigned.map((seller) => (
          <div
            key={`unassigned-${seller.sellerPartyId}`}
            className="rounded-2xl border border-gray-200 bg-gray-50/60 p-3 md:p-4"
            data-testid={`shipping-group-unassigned-${seller.sellerPartyId}`}
          >
            <div className="flex items-center gap-2 mb-3">
              <Store className="w-4 h-4 text-primary shrink-0" />
              <p className="text-xs md:text-sm font-bold text-gray-900 truncate">{seller.sellerDisplayName}</p>
            </div>
            <ul className="space-y-2">
              {seller.lines.map((line) => (
                <CartLineRow
                  key={line.lineId}
                  line={line}
                  methodCode={methodByLine[line.lineId] ?? ""}
                  methods={methods}
                  testIdPrefix={`shipping-group-method-${seller.sellerPartyId}`}
                  onSelectMethod={onSelectMethod}
                />
              ))}
            </ul>
          </div>
        ))}

        {groups.map((group) => (
          <div
            key={group.key}
            className="rounded-2xl border border-gray-200 bg-gray-50/60 p-3 md:p-4"
            data-testid={`shipping-group-${group.identity}`}
          >
            <div className="flex items-start justify-between gap-2 mb-3">
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <Store className="w-4 h-4 text-primary shrink-0" />
                  <p className="text-xs md:text-sm font-bold text-gray-900 truncate">{group.sellerDisplayName}</p>
                </div>
                <p className="text-[10px] md:text-[11px] text-gray-500 mt-1 truncate">
                  روش: {group.methodLabel}
                  <span className="text-gray-400"> · {group.methodCode}</span>
                </p>
              </div>
              <span className="text-[11px] md:text-xs font-black text-gray-900 shrink-0">
                {formatOfferAmount(group.amount, currency)}
              </span>
            </div>

            <ul className="space-y-2">
              {group.lines.map((line) => (
                <CartLineRow
                  key={line.lineId}
                  line={line}
                  methodCode={methodByLine[line.lineId] ?? group.methodCode}
                  methods={methods}
                  testIdPrefix={`shipping-group-method-${group.identity}`}
                  onSelectMethod={onSelectMethod}
                />
              ))}
            </ul>

            <GroupSchedulePicker
              schedule={schedules[group.key] ?? null}
              selection={selections[group.key]}
              identity={group.identity}
              onSelectDate={(value) => onSelectDate(group.key, value)}
              onSelectWindow={(value) => onSelectWindow(group.key, value)}
            />
          </div>
        ))}
      </div>
    </div>
  );
}
