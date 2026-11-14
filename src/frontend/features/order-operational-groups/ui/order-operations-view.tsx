"use client";

import Link from "next/link";
import type { ReactNode } from "react";

/**
 * TB-260 — the finance-free shell of the shared operational order view.
 *
 * The queue row and the full order page must both reach the SAME operational surface. This component owns
 * only the frame around it (heading, the exact target the operator came for, the recipient facts an operator
 * needs to prepare a parcel) and renders the caller's existing operational panel as `children`, so there is
 * exactly ONE panel, ONE set of dialogs and ONE action owner — never a second lifecycle next to the order.
 *
 * No money, settlement, refund, wallet or payment table is rendered here by construction: the caller cannot
 * inject them into this frame, and this module imports nothing from `app/admin`.
 *
 * TB-267 (presentation only): the frame is a compact header plus ONE overview that carries the work target
 * and the recipient side by side on desktop and stacked on mobile. Every date arrives already localized by
 * the caller (Jalali, one shape), every time range is its own LTR node, and the plan's zone is named once
 * instead of beside every instant. No value is derived or re-timed here.
 */
export interface OrderOperationsTargetFact {
  /** Server-owned operational group key (stable locator). */
  groupKey: string;
  methodLabel: string;
  /** The group's own scope wording («ارسال مستقل» / «ارسال مشترک مارکت‌پلیس»), or null. */
  scopeLabel?: string | null;
  /** Accepted customer delivery commitment — already-localized date label. */
  customerDeliveryDateLabel: string | null;
  /** Accepted customer delivery window, rendered as its own LTR range. */
  customerDeliveryTimeWindow: string | null;
  /** Accepted planned carrier handoff from durable Order evidence. */
  plannedDispatchDateLabel: string | null;
  plannedDispatchTimeWindow: string | null;
  /** One human label of the plan's zone → «به وقت تهران», printed once per target. */
  plannedDispatchTimeZoneLabel: string | null;
  /** Physical parcels/packages already created for this group. */
  parcelSummary: string;
  /** Truthful operator-facing readiness of this group. */
  readinessLabel: string;
  readinessTone: "ready" | "blocked" | "done";
}

export interface OrderOperationsViewProps {
  checkoutId: string;
  reference: string;
  state: "loading" | "ready" | "denied" | "error";
  errorMessage?: string | null;
  onRetry?: () => void;
  backHref: string;
  fullOrderHref: string;
  recipientName?: string;
  recipientMobile?: string;
  destinationLine?: string;
  methodLabel?: string;
  /** True when the operator arrived from a queue row / locator, so the target must be shown explicitly. */
  targetRequested: boolean;
  /** The exact group(s) the locator resolved to, in server order. Empty means the locator no longer exists. */
  targets: OrderOperationsTargetFact[];
  /**
   * TB-260-R1: the exact consolidated package number when the queue row pointed at one. The package is the
   * single physical action owner of its locked member parcels, so it is named explicitly here.
   */
  targetPackageNumber?: string | null;
  /** The canonical operational panel owned by the caller (same component the full order page renders). */
  children?: ReactNode;
}

function toneClass(tone: OrderOperationsTargetFact["readinessTone"]): string {
  switch (tone) {
    case "ready":
      return "bg-emerald-50 text-emerald-700 ring-emerald-200";
    case "blocked":
      return "bg-amber-50 text-amber-800 ring-amber-200";
    default:
      return "bg-slate-100 text-slate-700 ring-slate-200";
  }
}

/** One label above one value: the operator scans a column of facts instead of a run-on sentence. */
function FactRow({
  label,
  value,
  range,
  note,
  empty = "—",
  span2 = false,
}: {
  label: string;
  value: string | null | undefined;
  /** An already-localized time range, isolated LTR so RTL never reorders its start/end. */
  range?: string | null;
  /** A short qualifier beside the value (e.g. the plan's human zone label). */
  note?: string | null;
  empty?: string;
  span2?: boolean;
}) {
  const hasValue = Boolean((value ?? "").trim()) || Boolean((range ?? "").trim());
  return (
    <div className={`min-w-0 ${span2 ? "sm:col-span-2" : ""}`}>
      <dt className="text-xs text-gray-500">{label}</dt>
      <dd className="mt-0.5 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm font-semibold text-gray-900">
        {hasValue ? (
          <>
            {value ? <span className="break-words">{value}</span> : null}
            {range ? (
              <bdi dir="ltr" className="tabular-nums">
                {range}
              </bdi>
            ) : null}
            {note ? <span className="text-xs font-normal text-gray-500">{note}</span> : null}
          </>
        ) : (
          <span className="font-normal text-gray-500">{empty}</span>
        )}
      </dd>
    </div>
  );
}

function TargetCard({ target }: { target: OrderOperationsTargetFact }) {
  return (
    <li
      className="min-w-0"
      data-testid={`admin-order-operations-target-${target.groupKey}`}
    >
      <div className="flex flex-wrap items-center gap-x-2 gap-y-1.5">
        <p className="text-sm font-bold text-gray-900">{target.methodLabel || "روش ارسال"}</p>
        {target.scopeLabel ? (
          <span className="rounded-full bg-secondary px-2 py-0.5 text-xs font-semibold text-muted">
            {target.scopeLabel}
          </span>
        ) : null}
        <span className={`rounded-full px-2.5 py-0.5 text-xs font-bold ring-1 ${toneClass(target.readinessTone)}`}>
          {target.readinessLabel}
        </span>
      </div>
      <dl className="mt-2 grid gap-x-6 gap-y-2 sm:grid-cols-2">
        <FactRow
          label="تحویل مشتری"
          value={target.customerDeliveryDateLabel}
          range={target.customerDeliveryTimeWindow}
          empty="تاریخ تحویل ثبت نشده"
        />
        <FactRow
          label="موعد ارسال حامل"
          value={target.plannedDispatchDateLabel}
          range={target.plannedDispatchTimeWindow}
          note={target.plannedDispatchTimeZoneLabel}
          empty="موعد ارسال مشخص نشده"
        />
        <FactRow label="مرسوله‌ها" value={target.parcelSummary} span2 />
      </dl>
      {/*
       * TB-267: the deadline triple is NOT repeated here. It belongs to the group card that owns the plan,
       * so the operator reads one authoritative deadline block per group instead of two identical ones.
       */}
    </li>
  );
}

export function OrderOperationsView({
  checkoutId,
  reference,
  state,
  errorMessage,
  onRetry,
  backHref,
  fullOrderHref,
  recipientName,
  recipientMobile,
  destinationLine,
  methodLabel,
  targetRequested,
  targets,
  targetPackageNumber,
  children,
}: OrderOperationsViewProps) {
  return (
    <main data-testid="admin-order-operations-view" dir="rtl" className="pb-6">
      <header className="mb-3 border-b border-border pb-2.5">
        <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-2">
          <div className="min-w-0">
            <p className="text-xs text-muted">خانه / ارسال و تحویل / عملیات ارسال</p>
            <div className="mt-0.5 flex flex-wrap items-baseline gap-x-3 gap-y-0.5">
              <h1 className="text-xl font-semibold tracking-tight">عملیات ارسال</h1>
              <span
                className="min-w-0 truncate font-mono text-xs text-muted"
                dir="ltr"
                data-testid="admin-order-operations-reference"
              >
                {reference || checkoutId}
              </span>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Link
              href={backHref}
              className="inline-flex min-h-11 items-center rounded-xl border border-border bg-surface px-3 text-xs font-bold hover:bg-secondary"
              data-testid="admin-order-operations-back"
            >
              بازگشت به صف کار
            </Link>
            <Link
              href={fullOrderHref}
              className="inline-flex min-h-11 items-center rounded-xl border border-border bg-surface px-3 text-xs font-bold hover:bg-secondary"
              data-testid="admin-order-operations-full-order"
            >
              جزئیات کامل سفارش
            </Link>
          </div>
        </div>
      </header>

      {state === "denied" ? (
        <section
          className="rounded-2xl border border-border bg-surface-elevated p-4 text-sm font-semibold text-rose-700"
          data-testid="admin-order-operations-denied"
        >
          دسترسی مدیریتی برای این عملیات تأیید نشد.
        </section>
      ) : state === "error" ? (
        <section
          className="rounded-2xl border border-border bg-surface-elevated p-4"
          data-testid="admin-order-operations-error"
        >
          <p className="text-sm font-semibold text-rose-700">سفارش خوانده نشد.</p>
          {errorMessage ? <p className="mt-1 text-xs text-muted">{errorMessage}</p> : null}
          {onRetry ? (
            <button
              type="button"
              onClick={onRetry}
              className="mt-3 inline-flex min-h-11 items-center rounded-xl bg-primary px-4 text-xs font-bold text-primary-foreground"
              data-testid="admin-order-operations-retry"
            >
              تلاش مجدد
            </button>
          ) : null}
        </section>
      ) : state === "loading" ? (
        <section className="rounded-2xl border border-border bg-surface-elevated p-4 text-sm text-muted" data-testid="admin-order-operations-loading">
          در حال بارگذاری عملیات ارسال…
        </section>
      ) : (
        <div className="space-y-3">
          {/* ONE overview: the work target and the recipient, side by side on desktop and stacked on mobile. */}
          <section
            className="rounded-xl border border-border bg-surface-elevated p-3 shadow-sm sm:p-4"
          >
            <div
              className={
                targetRequested
                  ? "grid gap-4 lg:grid-cols-[minmax(0,1.4fr)_minmax(260px,0.8fr)]"
                  : "grid gap-4"
              }
            >
              {targetRequested ? (
                <div className="min-w-0" data-testid="admin-order-operations-target">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="text-sm font-bold text-gray-900">هدف کاری</h2>
                    {targetPackageNumber ? (
                      <span
                        className="rounded-full bg-secondary px-2 py-0.5 font-mono text-xs font-semibold text-foreground"
                        dir="ltr"
                        data-testid="admin-order-operations-target-package"
                      >
                        {targetPackageNumber}
                      </span>
                    ) : null}
                  </div>
                  {targets.length > 0 ? (
                    <ul className="mt-2 space-y-3">
                      {targets.map((target) => (
                        <TargetCard key={target.groupKey} target={target} />
                      ))}
                    </ul>
                  ) : (
                    <p className="mt-2 text-sm text-muted" data-testid="admin-order-operations-target-missing">
                      گروه، مرسوله یا بستهٔ انتخاب‌شده در این سفارش پیدا نشد؛ ممکن است وضعیت آن تغییر کرده باشد.
                      فهرست کامل عملیات ارسال این سفارش در ادامه آمده است.
                    </p>
                  )}
                </div>
              ) : null}

              <div
                className={
                  targetRequested
                    ? "min-w-0 border-t border-border pt-3 lg:border-t-0 lg:pt-0"
                    : "min-w-0"
                }
                data-testid="admin-order-operations-recipient"
              >
                <h2 className="text-sm font-bold text-gray-900">گیرنده و مقصد</h2>
                <dl className="mt-2 grid gap-x-6 gap-y-2">
                  <FactRow label="نام گیرنده" value={recipientName || "مشتری توبا"} />
                  <FactRow label="موبایل" value={null} range={recipientMobile || null} empty="ثبت نشده" />
                  <div className="min-w-0">
                    <dt className="text-xs text-gray-500">نشانی</dt>
                    <dd className="mt-0.5 break-words text-sm font-semibold leading-6 text-gray-900">
                      {destinationLine || "—"}
                    </dd>
                  </div>
                  {!targetRequested ? (
                    <FactRow label="روش ارسال" value={methodLabel || null} />
                  ) : null}
                </dl>
              </div>
            </div>
          </section>

          {children}
        </div>
      )}
    </main>
  );
}
