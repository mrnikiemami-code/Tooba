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
 */
export interface OrderOperationsTargetFact {
  /** Server-owned operational group key (stable locator). */
  groupKey: string;
  methodLabel: string;
  /** Accepted customer delivery commitment — never the dispatch deadline. */
  customerDeliverySummary: string;
  /** Accepted planned carrier handoff from durable Order evidence, or null when the evidence has none. */
  plannedDispatchSummary: string | null;
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

function TargetCard({ target }: { target: OrderOperationsTargetFact }) {
  return (
    <li
      className="rounded-xl border border-border bg-surface p-3"
      data-testid={`admin-order-operations-target-${target.groupKey}`}
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm font-bold">{target.methodLabel || "روش ارسال"}</p>
        <span className={`rounded-full px-2.5 py-1 text-xs font-bold ring-1 ${toneClass(target.readinessTone)}`}>
          {target.readinessLabel}
        </span>
      </div>
      <dl className="mt-2 grid gap-2 text-xs sm:grid-cols-2">
        <div className="min-w-0">
          <dt className="text-muted">موعد ارسال</dt>
          <dd className="mt-0.5 break-words font-semibold">
            {target.plannedDispatchSummary ?? "موعد ارسال مشخص نشده"}
          </dd>
        </div>
        <div className="min-w-0">
          <dt className="text-muted">تحویل مشتری</dt>
          <dd className="mt-0.5 break-words font-semibold">{target.customerDeliverySummary || "—"}</dd>
        </div>
        <div className="min-w-0 sm:col-span-2">
          <dt className="text-muted">مرسوله‌ها</dt>
          <dd className="mt-0.5 break-words font-semibold">{target.parcelSummary}</dd>
        </div>
      </dl>
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
  children,
}: OrderOperationsViewProps) {
  return (
    <main data-testid="admin-order-operations-view" dir="rtl" className="pb-6">
      <header className="mb-4 flex flex-wrap items-start justify-between gap-3 border-b border-border pb-3">
        <div className="min-w-0">
          <p className="text-sm text-muted">خانه / ارسال و تحویل / عملیات ارسال</p>
          <div className="mt-1 flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <h1 className="text-2xl font-semibold tracking-tight">عملیات ارسال سفارش</h1>
            <p className="font-mono text-xs text-muted" dir="ltr">{reference || checkoutId}</p>
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
        <div className="space-y-4">
          {targetRequested ? (
            <section
              className="rounded-2xl border border-border bg-surface-elevated p-4 shadow-sm"
              data-testid="admin-order-operations-target"
            >
              <h2 className="text-sm font-bold">گروه هدف</h2>
              {targets.length > 0 ? (
                <ul className="mt-2 grid gap-2 lg:grid-cols-2">
                  {targets.map((target) => (
                    <TargetCard key={target.groupKey} target={target} />
                  ))}
                </ul>
              ) : (
                <p className="mt-2 text-sm text-muted" data-testid="admin-order-operations-target-missing">
                  گروه یا مرسولهٔ انتخاب‌شده در این سفارش پیدا نشد؛ ممکن است وضعیت آن تغییر کرده باشد. فهرست کامل
                  عملیات ارسال این سفارش در ادامه آمده است.
                </p>
              )}
            </section>
          ) : null}

          <section className="rounded-2xl border border-border bg-surface-elevated p-4 shadow-sm" data-testid="admin-order-operations-recipient">
            <h2 className="text-sm font-bold">گیرنده و مقصد</h2>
            <p className="mt-1 break-words text-sm font-semibold">{recipientName || "مشتری توبا"}</p>
            {recipientMobile ? (
              <p className="mt-0.5 text-xs text-muted" dir="ltr">{recipientMobile}</p>
            ) : null}
            <p className="mt-1 break-words text-sm leading-6">{destinationLine || "—"}</p>
            <p className="mt-1 text-xs text-muted">روش ارسال: {methodLabel || "—"}</p>
          </section>

          {children}
        </div>
      )}
    </main>
  );
}
