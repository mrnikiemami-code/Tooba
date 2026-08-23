import type { ReactNode } from "react";
import { cn } from "../cn";
import { Badge, Button } from "./core";

/**
 * نمایش پول قالب‌بندی‌شده. محاسبهٔ قیمت/مالیات/تخفیف اینجا انجام نمی‌شود.
 */
export function MoneyDisplay({ amount, currency, locale = "fa-IR" }: { amount: string; currency: string; locale?: string }) {
  const formatted = new Intl.NumberFormat(locale, { style: "currency", currency, maximumFractionDigits: 0 }).format(Number(amount));
  return <span className="tabular-nums">{formatted}</span>;
}

/**
 * ارائهٔ قیمت از قبل حل‌شده: پایه، نهایی، پرچم تخفیف. هیچ نرخ تخفیفی محاسبه نمی‌شود.
 */
export function PricePresentation({
  exclusiveAmount,
  finalAmount,
  currency,
  discounted,
}: {
  exclusiveAmount: string;
  finalAmount: string;
  currency: string;
  discounted: boolean;
}) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <MoneyDisplay amount={finalAmount} currency={currency} />
      {discounted ? <span className="text-sm text-muted line-through"><MoneyDisplay amount={exclusiveAmount} currency={currency} /></span> : null}
    </div>
  );
}

/** نشان تخفیف نمایشی؛ موتور Promotion را صدا نمی‌زند. */
export function DiscountBadge({ label }: { label: string }) {
  return <Badge tone="danger">{label}</Badge>;
}

/** نشان موجودی از دادهٔ ازپیش‌حل‌شده؛ Inventory را کوئری نمی‌کند. */
export function AvailabilityBadge({ available }: { available: boolean }) {
  return <Badge tone={available ? "success" : "neutral"}>{available ? "موجود" : "ناموجود"}</Badge>;
}

/** کنترل تعداد نمایشی. سیاست موجودی در دامنه می‌ماند. */
export function QuantityControl({
  value,
  onChange,
  min = 1,
  max = 99,
}: {
  value: number;
  onChange: (value: number) => void;
  min?: number;
  max?: number;
}) {
  return (
    <div className="inline-flex items-center gap-2">
      <Button type="button" tone="secondary" aria-label="کاهش" onClick={() => onChange(Math.max(min, value - 1))}>
        −
      </Button>
      <span className="min-w-8 text-center tabular-nums">{value}</span>
      <Button type="button" tone="secondary" aria-label="افزایش" onClick={() => onChange(Math.min(max, value + 1))}>
        +
      </Button>
    </div>
  );
}

/** نمایش رتبه از عدد داده‌شده. */
export function RatingDisplay({ value, of = 5 }: { value: number; of?: number }) {
  return (
    <span aria-label={`امتیاز ${value} از ${of}`} className="text-sm">
      {value}/{of}
    </span>
  );
}

/** قاب رسانه با نسبت ثابت برای جلوگیری از CLS. */
export function MediaAspectBox({ ratio = "1/1", children }: { ratio?: string; children?: ReactNode }) {
  return (
    <div className="overflow-hidden rounded-ds bg-secondary" style={{ aspectRatio: ratio }}>
      {children}
    </div>
  );
}

/** هویت فروشندهٔ نمایشی از نام حل‌شده؛ Party را واکشی نمی‌کند. */
export function SellerIdentityDisplay({ name }: { name: string }) {
  return <span className="text-sm text-muted">{name}</span>;
}

/** چیدمان ستونی با فاصلهٔ منطقی. */
export function Stack({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cn("flex flex-col gap-3", className)}>{children}</div>;
}

/** چیدمان ردیفی با wrap و فاصلهٔ منطقی. */
export function Cluster({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cn("flex flex-wrap items-center gap-2", className)}>{children}</div>;
}

/** ظرف صفحه موبایل‌اول. */
export function PageContainer({ children }: { children: ReactNode }) {
  return <div className="mx-auto w-full max-w-6xl px-4 sm:px-6">{children}</div>;
}

/** نوار اقدام چسبان پایین برای موبایل. */
export function StickyActionBar({ children }: { children: ReactNode }) {
  return <div className="sticky bottom-0 z-[var(--z-header)] border-t border-border bg-surface/95 p-3">{children}</div>;
}
