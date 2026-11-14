import { CalendarDays, Clock, PackageCheck, Truck } from "lucide-react";
import { operationalTimeWindowLabel } from "../../../lib/operational-format";

/**
 * TB-256-R1: the TWO facts an operational group carries, rendered side by side and never merged:
 *
 * - «تحویل مشتری» — the accepted customer delivery date/window (the commitment);
 * - «موعد ارسال حامل» — the durable planned carrier handoff derived from the real readiness/calendar facts.
 *
 * TB-267 (presentation only): each fact is one labelled value row instead of a run-on sentence, the time
 * range is isolated in its own LTR node, and the plan's zone is named ONCE for the deadline block instead of
 * being repeated next to every instant. Display-only: the caller owns every value and formatter (shared
 * pure helper only, no derived business rule here), so a historical group with no plan simply renders the
 * customer line.
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
  /** TB-267: one human label of the plan's zone → «به وقت تهران», printed once for the deadline block. */
  timeZoneLabel?: string | null;
};

function DeadlineRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs text-gray-500">{label}</dt>
      <dd className="mt-0.5 font-semibold tabular-nums text-gray-900">{value}</dd>
    </div>
  );
}

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
  timeZoneLabel,
}: OperationalDeliveryTimelineProps) {
  const timeWindow = operationalTimeWindowLabel(deliveryTimeWindow);
  const hasDeadlines = Boolean(preparationStartLabel || transferDepartureLabel || handoffDeadlineLabel);
  return (
    <>
      <dl className="mt-2 grid gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
        <div className="min-w-0">
          <dt className="text-xs text-gray-500">تحویل مشتری</dt>
          <dd className="mt-0.5 flex flex-wrap items-center gap-x-2 gap-y-1 font-semibold text-gray-900">
            <span className="inline-flex items-center gap-1.5">
              <CalendarDays className="size-4 shrink-0 text-gray-400" aria-hidden />
              {deliveryDateLabel ?? "تاریخ تحویل نامشخص"}
            </span>
            {timeWindow ? (
              <span className="inline-flex items-center gap-1.5">
                <Clock className="size-4 shrink-0 text-gray-400" aria-hidden />
                <bdi dir="ltr" className="tabular-nums">{timeWindow}</bdi>
              </span>
            ) : null}
            {responsibilityLabelFa ? (
              <span className="text-xs font-normal text-gray-500">{responsibilityLabelFa}</span>
            ) : null}
          </dd>
        </div>
        {plannedDispatch ? (
          /* The carrier handoff is deliberately a SEPARATE row: it is never presented as the customer promise. */
          <div className="min-w-0" data-testid={`admin-order-operational-planned-dispatch-${groupKey}`}>
            <dt className="text-xs text-gray-500">موعد ارسال حامل</dt>
            <dd className="mt-0.5 flex flex-wrap items-center gap-x-2 gap-y-1 font-semibold text-gray-900">
              <span className="inline-flex items-center gap-1.5">
                <Truck className="size-4 shrink-0 text-gray-400" aria-hidden />
                {handoffDateLabel ?? "تاریخ نامشخص"}
              </span>
              <bdi dir="ltr" className="tabular-nums">
                {operationalTimeWindowLabel(plannedDispatch.handoffTimeWindow)}
              </bdi>
              {plannedDispatch.transportWorkingDays >= 0 ? (
                <span className="text-xs font-normal text-gray-500">
                  {`پذیرش حامل · ${plannedDispatch.transportWorkingDays.toLocaleString("fa-IR")} روز کاری حمل`}
                </span>
              ) : null}
              {plannedDispatch.distinctVariants > 1 ? (
                <span className="text-xs font-normal text-gray-500">
                  {`(زودترین از ${plannedDispatch.distinctVariants.toLocaleString("fa-IR")} برنامه)`}
                </span>
              ) : null}
              {/* TB-267: one human zone label for these instants when no deadline block carries it. */}
              {!hasDeadlines && timeZoneLabel ? (
                <span className="text-xs font-normal text-gray-500">{timeZoneLabel}</span>
              ) : null}
            </dd>
          </div>
        ) : null}
      </dl>
      {plannedDispatch && hasDeadlines ? (
        /*
         * TB-264: the operational deadlines of the SAME accepted plan, printed in the carrier time zone.
         * They never replace the customer promise above, and a legacy policy-v1 plan has no deadlines and
         * therefore renders no block at all instead of an invented one.
         */
        <div
          className="mt-2 rounded-lg border border-gray-100 bg-gray-50/70 px-3 py-2"
          data-testid={`admin-order-operational-deadlines-${groupKey}`}
        >
          <p className="flex flex-wrap items-center gap-1 text-xs font-bold text-gray-700">
            <PackageCheck className="size-3.5 shrink-0 text-gray-400" aria-hidden />
            مهلت‌های عملیاتی
            {timeZoneLabel ? (
              <>
                <span className="text-gray-400" aria-hidden>·</span>
                <span className="font-normal text-gray-500">{timeZoneLabel}</span>
              </>
            ) : null}
          </p>
          <dl className="mt-1.5 grid gap-x-6 gap-y-1.5 text-sm sm:grid-cols-3">
            {preparationStartLabel ? (
              <DeadlineRow label="آماده‌سازی از" value={preparationStartLabel} />
            ) : null}
            {transferDepartureLabel ? (
              <DeadlineRow label="خروج به سوی حامل" value={transferDepartureLabel} />
            ) : null}
            {handoffDeadlineLabel ? (
              <DeadlineRow label="آخرین مهلت تحویل به حامل" value={handoffDeadlineLabel} />
            ) : null}
          </dl>
        </div>
      ) : null}
    </>
  );
}
