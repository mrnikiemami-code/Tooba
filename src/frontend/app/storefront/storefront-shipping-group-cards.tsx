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
import { AlertTriangle, Calendar, Check, Clock, Package, RefreshCw, Store } from "lucide-react";
import { useState } from "react";

import { formatOfferAmount } from "./storefront-api.ts";
import type { StorefrontCartLine } from "./storefront-cart-api.ts";
import { deliveryChipParts, formatExactAmountText, toPersianDigits } from "./storefront-shipping-format.ts";
import { formatJalaliDate } from "../../design-system/app-data-grid/jalali.ts";
import type {
  StorefrontCapturedPreview,
  StorefrontCapturedPreviewGroup,
  StorefrontCapturedPreviewMethod,
  StorefrontShippingMethod,
} from "./storefront-shipping-api.ts";
import type {
  StorefrontShippingGroupSchedule,
  StorefrontShippingGroupSelection,
} from "./storefront-shipping-group-schedule.ts";
import { StorefrontShippingUnavailableMethods } from "./storefront-shipping-unavailable-methods.tsx";
import type { StorefrontShippingUnavailableNotice } from "./storefront-shipping-unavailable-methods.ts";

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

/** یک تاریخ در نوار تاریخ؛ هم مسیر قدیمی و هم پیش‌نمایش capture‌شده از همین کنترل استفاده می‌کنند. */
interface DateChipItem {
  value: string;
  label: string;
  subLabel: string;
  isEarliest: boolean;
}

/**
 * نوار تاریخ مشترک (TB-150 + TB-229-R1): همان DOM/کلاس چپ‌های موجود، بدون هیچ تاریخ ساختگی.
 * `disabledBefore` فقط در مسیر قدیمی معنا دارد؛ در پیش‌نمایش capture‌شده تاریخ‌ها از قبل مجازند.
 */
function DateChipStrip({
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
      <DateChipStrip
        dates={schedule.dates}
        identity={identity}
        testIdPrefix="shipping-group-date"
        active={deliveryDate}
        disabledBefore={schedule.minimumDeliveryDate}
        onPick={onSelectDate}
      />
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

/** توضیح کوتاه و پایدار برای هر کد امتناع سرور؛ کد ناشناخته متن عمومی می‌گیرد. */
const CAPTURED_REASON_TEXT: Record<string, string> = {
  "storefront.captured_preview.unassigned": "مسئولیت ارسال این کالا هنوز تعیین نشده است.",
  "storefront.captured_preview.type_unavailable": "نوع ارسال این کالا فعال نیست.",
  "storefront.captured_preview.seller_owner_unsupported": "ارسال فروشنده در این نصب پشتیبانی نمی‌شود.",
  "storefront.captured_preview.no_selected_capture": "برای این گروه هنوز روش ارسالی انتخاب نشده است.",
  "storefront.captured_preview.destination_required": "برای زمان و هزینه، مقصد را انتخاب کنید.",
  "storefront.captured_preview.assignment_contradictory": "اطلاعات ارسال این کالاها کامل نیست.",
  "storefront.captured_preview.assignment_missing": "اطلاعات ارسال این کالاها کامل نیست.",
  "storefront.captured_preview.responsibility_invalid": "مسئولیت ارسال این کالا معتبر نیست.",
  "storefront.captured_preview.composition_refused": "محاسبهٔ ارسال این سبد ممکن نشد.",
  "storefront.captured_preview.available_date_count_invalid": "بازهٔ تاریخ‌های ارسال تنظیم نشده است.",
  "storefront.captured_preview.destination_pending": "در حال محاسبهٔ ارسال برای مقصد جدید…",
  "storefront.captured_preview.destination_failed": "ارسال این مقصد محاسبه نشد؛ قیمت مقصد قبلی نمایش داده نمی‌شود.",
  "storefront.captured_preview.untrusted": "پاسخ ارسال این سبد قابل اتکا نیست؛ دوباره تلاش کنید.",
};

function capturedReasonText(code: string | null): string {
  if (!code) return "این گروه ارسال در دسترس نیست.";
  return CAPTURED_REASON_TEXT[code] ?? "این گروه ارسال در دسترس نیست.";
}

/**
 * TB-229-R1: یک روش capture‌شده با تقویم **خودش**.
 *
 * انتخاب تاریخ فقط بازهٔ همان روز را نشان می‌دهد؛ هیچ انتخاب/ثبتی به سرور نمی‌رود، چون سرور این
 * مسیر را پیش از نوشتن رد می‌کند. مبالغ متن دقیق سرور هستند و «نامشخص» هرگز صفر نمایش داده نمی‌شود.
 */
function CapturedMethodRow({
  groupKey,
  method,
  currency,
}: {
  groupKey: string;
  method: StorefrontCapturedPreviewMethod;
  currency: string | null;
}) {
  const [picked, setPicked] = useState<string | null>(null);
  const identity = `${groupKey}-${method.serviceCode}-${method.optionCode ?? "none"}`;
  const active = picked ?? method.earliestEligibleDate ?? method.availableDates[0]?.value ?? "";
  const activeDate = method.availableDates.find((d) => d.value === active) ?? null;
  const freight = formatExactAmountText(method.freightAmountText, method.currency ?? currency);
  const packaging = formatExactAmountText(method.packagingAmountText, method.currency ?? currency);

  return (
    <div
      className="mt-3 border-t border-dashed border-gray-200 pt-3"
      data-testid={`shipping-group-captured-method-${identity}`}
    >
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="text-[11px] md:text-xs font-bold text-gray-800 flex items-center gap-1.5">
            <Package className="w-3.5 h-3.5 text-primary shrink-0" />
            {method.methodLabel}
          </p>
          <p className="text-[10px] text-gray-500 mt-0.5">
            {freight === null ? "کرایه نامشخص" : `کرایه ${freight}`}
            {packaging === null ? "" : ` · بسته‌بندی ${packaging}`}
          </p>
        </div>
      </div>

      {method.availableDates.length === 0 ? (
        <p
          className="mt-2 text-[11px] text-gray-500"
          data-testid={`shipping-group-captured-dates-empty-${identity}`}
        >
          {capturedReasonText(method.unavailableReason)}
        </p>
      ) : (
        <>
          <p className="mt-2 text-[10px] text-gray-500 flex items-center gap-1">
            <Calendar className="w-2.5 h-2.5" /> تاریخ‌های مجاز همین روش
          </p>
          <DateChipStrip
            dates={method.availableDates}
            identity={identity}
            testIdPrefix="shipping-group-captured-date"
            active={active}
            onPick={setPicked}
          />
          <p
            className="mt-1 text-[10px] text-gray-600"
            data-testid={`shipping-group-captured-interval-${identity}`}
          >
            {/* TB-230-R1: بازه ساعت واقعی سرور؛ جهت‌دهی صریح LTR تا در RTL هم 09:00–17:00 درست خوانده شود. */}
            بازهٔ همین روز:{" "}
            <bdi dir="ltr" className="inline-block">
              {activeDate?.intervalText ?? "ثبت نشده"}
            </bdi>
          </p>
        </>
      )}
    </div>
  );
}

/** کارت یک گروه capture‌شده با همان کارت/خط‌های موجود؛ فقط‌خواندنی و بدون دکمهٔ ثبت. */
function CapturedGroupCard({
  group,
  lines,
  sellerName,
  currency,
}: {
  group: StorefrontCapturedPreviewGroup;
  lines: readonly StorefrontCartLine[];
  sellerName: string;
  currency: string | null;
}) {
  const goodsText = formatExactAmountText(group.finalGoodsAmountText, currency);
  return (
    <div
      className="rounded-2xl border border-gray-200 bg-gray-50/60 p-3 md:p-4"
      data-testid={`shipping-group-${group.groupKey}`}
    >
      <div className="flex items-start justify-between gap-2 mb-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
            <Store className="w-4 h-4 text-primary shrink-0" />
            <p className="text-xs md:text-sm font-bold text-gray-900 truncate">{sellerName}</p>
          </div>
          <p className="text-[10px] md:text-[11px] text-gray-500 mt-1 truncate">
            {group.typeLabel ?? "نوع ارسال تعیین‌نشده"}
          </p>
        </div>
        <span className="text-[11px] md:text-xs font-black text-gray-900 shrink-0">
          {goodsText ?? "مبلغ کالا نامشخص"}
        </span>
      </div>

      <ul className="space-y-2">
        {lines.map((line) => (
          <CartLineRow
            key={line.lineId}
            line={line}
            methodCode=""
            methods={[]}
            testIdPrefix="shipping-group-captured-line"
            onSelectMethod={() => {}}
          />
        ))}
      </ul>

      {group.unavailableReason ? (
        <p
          className="mt-3 flex items-start gap-1.5 text-[11px] text-amber-700 bg-amber-50 border border-amber-200 rounded-xl p-3"
          data-testid={`shipping-group-unavailable-${group.groupKey}`}
        >
          <AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5" />
          {capturedReasonText(group.unavailableReason)}
        </p>
      ) : null}

      {group.methods.map((method) => (
        <CapturedMethodRow
          key={`${method.serviceCode}|${method.optionCode ?? ""}`}
          groupKey={group.groupKey}
          method={method}
          currency={currency}
        />
      ))}
    </div>
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
  destinationRefreshState = null,
  onRetryDestination,
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
  /**
   * TB-230-R1: وضعیت صریح تازه‌سازی مقصد. `failed` تنها حالت شکست برای همین هویت است و یک Retry
   * صریح نشان می‌دهد؛ `pending` در حال محاسبه است؛ `current` یعنی همین پیش‌نمایش مال همین مقصد است.
   */
  destinationRefreshState?: "current" | "pending" | "failed" | null;
  onRetryDestination?: () => void;
  onSelectMethod: (lineId: string, methodCode: string) => void;
  onSelectDate: (groupKey: string, value: string) => void;
  onSelectWindow: (groupKey: string, value: string) => void;
}) {
  // TB-229: نام نمایشی فروشنده از همان گروه‌های واقعی سبد؛ هیچ نام/شناسهٔ ساختگی ساخته نمی‌شود.
  const sellerNames: Record<string, string> = {};
  const lineById: Record<string, StorefrontCartLine> = {};
  for (const group of groups) {
    sellerNames[group.sellerPartyId] = group.sellerDisplayName;
    for (const line of group.lines) lineById[line.lineId] = line;
  }
  for (const seller of unassigned) {
    sellerNames[seller.sellerPartyId] = seller.sellerDisplayName;
    for (const line of seller.lines) lineById[line.lineId] = line;
  }

  // TB-229-R1: وقتی پیش‌نمایش روشن است، **همین** کارت‌ها تک‌منبع نمایش ارسال‌اند (چه سبد تک‌خطی و چه
  // چندخطی) و هیچ کارت/روش/قیمت قدیمی در کنارشان رندر نمی‌شود. پاسخ نامعتبر هم مسیر قدیمی را جای
  // خودش نمی‌بیند: فقط پیام صادقانهٔ «در دسترس نیست» می‌آید.
  const capturedActive = capturedPreview !== null;
  const capturedGroups = capturedPreview?.state === "ok" && capturedPreview.enabled
    ? capturedPreview.groups
    : [];

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

      {capturedActive ? (
        <>
          <p className="text-[11px] text-gray-500 mb-3" data-testid="shipping-captured-preview-notice">
            {capturedPreview?.state === "untrusted"
              ? "پاسخ ارسال این سبد قابل اتکا نیست؛ تا اصلاح آن، روش یا تاریخ قدیمی نمایش داده نمی‌شود."
              : "این پیش‌نمایش فقط‌خواندنی است؛ انتخاب و ثبت روش ارسال هنوز فعال نیست."}
          </p>
          <div className="space-y-4" data-testid="shipping-captured-groups">
            {capturedPreview?.state === "untrusted" ? (
              <p
                className="flex items-start gap-1.5 text-[11px] text-amber-700 bg-amber-50 border border-amber-200 rounded-xl p-3"
                data-testid="shipping-captured-preview-untrusted"
              >
                <AlertTriangle className="w-3.5 h-3.5 shrink-0 mt-0.5" />
                {capturedReasonText(capturedPreview.unavailableReason)}
              </p>
            ) : null}

            {capturedGroups.length === 0 && capturedPreview?.state === "ok" ? (
              <div
                className="text-[11px] text-gray-500 bg-surface border border-dashed border-gray-200 rounded-xl p-3"
                data-testid="shipping-captured-preview-empty"
              >
                <p>{capturedReasonText(capturedPreview.unavailableReason)}</p>
                {/* TB-230-R1: تنها راه تکرار، یک Retry صریح برای همان مقصدِ شکست‌خورده است. */}
                {destinationRefreshState === "failed" && onRetryDestination ? (
                  <button
                    type="button"
                    onClick={onRetryDestination}
                    className="mt-2 inline-flex items-center gap-1 rounded-lg border border-primary/30 bg-primary/5 px-3 py-1 text-[11px] font-bold text-primary"
                    data-testid="shipping-captured-destination-retry"
                  >
                    <RefreshCw className="w-3 h-3" />
                    تلاش دوباره
                  </button>
                ) : null}
              </div>
            ) : null}

            {capturedGroups.map((group) => (
              <CapturedGroupCard
                key={group.groupKey}
                group={group}
                lines={group.cartLineIds.map((lineId) => lineById[lineId]).filter((line): line is StorefrontCartLine => Boolean(line))}
                sellerName={sellerNames[group.sellerPartyId] ?? ""}
                currency={capturedPreview?.currency ?? null}
              />
            ))}
          </div>
        </>
      ) : (
        <>
          <StorefrontShippingUnavailableMethods
            notices={unavailableNotices}
            testIdPrefix="shipping-group-unavailable"
          />

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
        </>
      )}
    </div>
  );
}
