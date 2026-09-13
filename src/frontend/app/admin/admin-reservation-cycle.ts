export type AdminReservationLocale = "fa" | "en";

export type AdminReservationCycleHistoryRow = {
  cycleNumber: number;
  statusLabelFa: string;
  statusLabelEn: string;
  reasonLabelFa: string;
  reasonLabelEn: string;
  startedAt: string;
  expiresAt: string;
  endedAt: string | null;
  effectiveHoldMinutes: number;
  effectiveMaxCycles: number;
  policySource: string;
  policySourceLabelFa: string;
  policySourceLabelEn: string;
  paymentAttemptRef: string | null;
};

export type AdminReservationCycleEventView = {
  kindLabelFa: string;
  kindLabelEn: string;
  occurredAt: string;
  cycleNumber: number | null;
  detailFa: string;
  detailEn: string;
};

export type AdminReservationShortageLine = {
  itemTitle: string;
  required: number;
  available: number;
  shortage: number;
  unitCode: string | null;
};

export type AdminReservationCycleAudit = {
  statusLabelFa: string;
  statusLabelEn: string;
  reasonLabelFa: string;
  reasonLabelEn: string;
  currentCycleNumber: number | null;
  totalCyclesUsed: number;
  maxCycles: number;
  retryCountRemaining: number;
  startedAt: string | null;
  expiresAt: string | null;
  serverTime: string;
  secondsRemaining: number;
  supplyStatus: string;
  supplyStatusLabelFa: string;
  retryLimitReached: boolean;
  canRetryReservation: boolean;
  canExtendTimer: boolean;
  history: AdminReservationCycleHistoryRow[];
  events: AdminReservationCycleEventView[];
  shortages: AdminReservationShortageLine[];
};

export type AdminReservationCycleSummary = {
  reservationLabel: string;
  reservationLabelEn: string;
  reservationState: string;
  reservationCycleNumber: number | null;
  reservationRetryPossible: boolean;
  reservationNeedsReacquire: boolean;
  reservationRetryLimitReached: boolean;
};

export const reservationStateEnumOptions = [
  { value: "active", label: "فعال" },
  { value: "expired", label: "پایان‌یافته" },
  { value: "committed", label: "نهایی‌شده" },
  { value: "released", label: "آزادشده" },
  { value: "reacquireFailed", label: "رزرو مجدد ناموفق" },
  { value: "none", label: "—" },
];

export function remainingSecondsFromServer(
  expiresAt: string | null | undefined,
  serverTime: string,
  clientReceivedAtMs: number,
  nowMs = Date.now(),
): number {
  if (!expiresAt) {
    return 0;
  }
  const end = Date.parse(expiresAt);
  const server = Date.parse(serverTime);
  if (!Number.isFinite(end) || !Number.isFinite(server)) {
    return 0;
  }
  const elapsed = Math.max(0, nowMs - clientReceivedAtMs);
  return Math.max(0, Math.floor((end - (server + elapsed)) / 1000));
}

export {
  formatCountdown as formatReservationCountdown,
  formatCountdownAccessibleLabel,
} from "../../lib/reservation-countdown.ts";

export function shouldRefreshOnceAtZero(previousSeconds: number, nextSeconds: number, alreadyRefreshed: boolean): boolean {
  return !alreadyRefreshed && previousSeconds > 0 && nextSeconds <= 0;
}

export function pickReservationLabel(fa: string, en: string, locale: AdminReservationLocale): string {
  return locale === "en" ? en : fa;
}

export function emptyReservationSummary(): AdminReservationCycleSummary {
  return {
    reservationLabel: "—",
    reservationLabelEn: "—",
    reservationState: "none",
    reservationCycleNumber: null,
    reservationRetryPossible: false,
    reservationNeedsReacquire: false,
    reservationRetryLimitReached: false,
  };
}

export function reservationBadgeClass(state: string): string {
  switch (state) {
    case "active":
      return "bg-emerald-50 text-emerald-700";
    case "committed":
      return "bg-blue-50 text-blue-700";
    case "expired":
      return "bg-amber-50 text-amber-800";
    case "reacquireFailed":
      return "bg-rose-50 text-rose-700";
    case "released":
      return "bg-gray-100 text-gray-700";
    default:
      return "bg-gray-50 text-gray-500";
  }
}
