import { CalendarDays, Clock, PackageCheck, Truck } from "lucide-react";

/**
 * TB-256-R1: the TWO facts an operational group carries, rendered side by side and never merged:
 *
 * - «تحویل مشتری» — the accepted customer delivery date/window (the commitment);
 * - «موعد ارسال» — the durable planned carrier handoff derived from the real readiness/calendar facts.
 *
 * Display-only: the caller owns every value and formatter (no `app/admin` import, no derived business
 * rule here), so a historical group with no plan simply renders the customer line.
 */
export type OperationalPlannedDispatchFact = {
  handoffTimeWindow: string;
  transportWorkingDays: number;
  distinctVariants: number;
};

export type OperationalDeliveryTimelineProps = {
  groupKey: string;
  /** Already-localized (Jalali) delivery date label, or null when the accepted date is unknown. */
  deliveryDateLabel: string | null;
  deliveryTimeWindow: string;
  responsibilityLabelFa: string | null;
  /** Null for a historical/legacy group whose accepted evidence carries no plan. */
  plannedDispatch: OperationalPlannedDispatchFact | null;
  /** Already-localized (Jalali) handoff date label, or null when the plan has no date. */
  handoffDateLabel: string | null;
  /**
   * TB-264: already-formatted operational deadlines (carrier zone) of the SAME plan. All three are null
   * for a legacy policy-v1 plan, in which case the row is not rendered at all.
   */
  preparationStartLabel?: string | null;
  transferDepartureLabel?: string | null;
  handoffDeadlineLabel?: string | null;
};

export function OperationalDeliveryTimeline({
  groupKey,
  deliveryDateLabel,
  deliveryTimeWindow,
  responsibilityLabelFa,
  plannedDispatch,
  handoffDateLabel,
  preparationStartLabel,
  transferDepartureLabel,
  handoffDeadlineLabel,
}: OperationalDeliveryTimelineProps) {
  const timeWindow = deliveryTimeWindow.trim();
  const hasDeadlines = Boolean(preparationStartLabel || transferDepartureLabel || handoffDeadlineLabel);
  return (
    <>
      <p className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-gray-600">
        <span className="font-bold text-gray-700">تحویل مشتری:</span>
        <span className="inline-flex items-center gap-1">
          <CalendarDays className="size-3.5 shrink-0 text-gray-400" aria-hidden />
          {deliveryDateLabel ?? "تاریخ تحویل نامشخص"}
        </span>
        {timeWindow ? (
          <span className="inline-flex items-center gap-1">
            <Clock className="size-3.5 shrink-0 text-gray-400" aria-hidden />
            <bdi dir="ltr">{timeWindow}</bdi>
          </span>
        ) : null}
        {responsibilityLabelFa ? <span>{responsibilityLabelFa}</span> : null}
      </p>
      {plannedDispatch ? (
        /* The carrier handoff is deliberately a SEPARATE row: it is never presented as the customer promise. */
        <p
          className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-gray-700"
          data-testid={`admin-order-operational-planned-dispatch-${groupKey}`}
        >
          <span className="font-bold text-gray-800">موعد ارسال:</span>
          <span className="inline-flex items-center gap-1">
            <Truck className="size-3.5 shrink-0 text-gray-400" aria-hidden />
            {handoffDateLabel ?? "تاریخ نامشخص"}
          </span>
          <span className="inline-flex items-center gap-1">
            <Clock className="size-3.5 shrink-0 text-gray-400" aria-hidden />
            <bdi dir="ltr">{plannedDispatch.handoffTimeWindow}</bdi>
          </span>
          {plannedDispatch.transportWorkingDays >= 0 ? (
            <span className="text-gray-500">
              {`پذیرش حامل · ${plannedDispatch.transportWorkingDays.toLocaleString("fa-IR")} روز کاری حمل`}
            </span>
          ) : null}
          {plannedDispatch.distinctVariants > 1 ? (
            <span className="text-gray-500">
              {`(زودترین از ${plannedDispatch.distinctVariants.toLocaleString("fa-IR")} برنامه)`}
            </span>
          ) : null}
        </p>
      ) : null}
      {plannedDispatch && hasDeadlines ? (
        /*
         * TB-264: the operational deadlines of the SAME accepted plan, printed in the carrier time zone.
         * They never replace the customer promise above, and a legacy policy-v1 plan has no deadlines and
         * therefore renders no row at all instead of an invented one.
         */
        <p
          className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-gray-600"
          data-testid={`admin-order-operational-deadlines-${groupKey}`}
        >
          <span className="font-bold text-gray-700">مهلت‌های عملیاتی:</span>
          {preparationStartLabel ? (
            <span className="inline-flex items-center gap-1">
              <PackageCheck className="size-3.5 shrink-0 text-gray-400" aria-hidden />
              {`آماده‌سازی از ${preparationStartLabel}`}
            </span>
          ) : null}
          {transferDepartureLabel ? (
            <span className="inline-flex items-center gap-1">
              <Truck className="size-3.5 shrink-0 text-gray-400" aria-hidden />
              {`حرکت به سمت حامل ${transferDepartureLabel}`}
            </span>
          ) : null}
          {handoffDeadlineLabel ? (
            <span className="inline-flex items-center gap-1">
              <Clock className="size-3.5 shrink-0 text-gray-400" aria-hidden />
              {`آخرین مهلت تحویل به حامل ${handoffDeadlineLabel}`}
            </span>
          ) : null}
        </p>
      ) : null}
    </>
  );
}
