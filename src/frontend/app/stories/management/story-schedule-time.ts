/**
 * Story schedule conversion — Jalali day + clock in Asia/Tehran → canonical ISO UTC.
 * Never uses browser-local Date(string) as conversion authority.
 */
import dayjs from "dayjs";
import jalaliday from "jalaliday";
import utc from "dayjs/plugin/utc";
import timezone from "dayjs/plugin/timezone";
import { jalaliInputToIso } from "../../../design-system/app-data-grid/jalali";

dayjs.extend(jalaliday);
dayjs.extend(utc);
dayjs.extend(timezone);

/** Accepted configured zone for Story schedule authoring. */
export const STORY_SCHEDULE_TIME_ZONE = "Asia/Tehran";

export type StoryScheduleDraft = {
  /** ISO date (Gregorian day) from JalaliDatePicker, or empty. */
  startDayIso: string;
  startTime: string;
  endDayIso: string;
  endTime: string;
};

export type StoryScheduleParse =
  | { ok: true; startAt: string; endAt: string | null }
  | { ok: false; field: "startAt" | "endAt"; message: string };

function normalizeTime(value: string): { hour: number; minute: number } | null {
  const trimmed = value.trim();
  if (!trimmed) return { hour: 0, minute: 0 };
  const match = /^(\d{1,2}):(\d{2})$/.exec(trimmed);
  if (!match) return null;
  const hour = Number(match[1]);
  const minute = Number(match[2]);
  if (!Number.isInteger(hour) || !Number.isInteger(minute) || hour < 0 || hour > 23 || minute < 0 || minute > 59) {
    return null;
  }
  return { hour, minute };
}

/**
 * Combine a Jalali-picked day (already as Gregorian ISO date from JalaliDatePicker) with HH:mm
 * interpreted in Asia/Tehran, independent of the browser zone.
 */
export function combineTehranDayAndTime(dayIso: string, time: string): string | null {
  const day = dayjs(dayIso);
  if (!day.isValid()) return null;
  const clock = normalizeTime(time);
  if (!clock) return null;
  const y = day.year();
  const m = day.month();
  const d = day.date();
  const local = dayjs.tz(
    `${y}-${String(m + 1).padStart(2, "0")}-${String(d).padStart(2, "0")}T${String(clock.hour).padStart(2, "0")}:${String(clock.minute).padStart(2, "0")}:00`,
    STORY_SCHEDULE_TIME_ZONE,
  );
  return local.isValid() ? local.toISOString() : null;
}

/** Jalali yyyy/mm/dd text → Gregorian ISO date (noon UTC day anchor from shared helper). */
export function jalaliDayTextToIsoDate(input: string): string | undefined {
  return jalaliInputToIso(input);
}

export function parseStoryScheduleDraft(draft: StoryScheduleDraft): StoryScheduleParse {
  if (!draft.startDayIso.trim()) {
    return { ok: false, field: "startAt", message: "زمان شروع را انتخاب کنید." };
  }
  const startAt = combineTehranDayAndTime(draft.startDayIso, draft.startTime || "00:00");
  if (!startAt) {
    return { ok: false, field: "startAt", message: "زمان شروع معتبر نیست." };
  }
  if (!draft.endDayIso.trim()) {
    return { ok: true, startAt, endAt: null };
  }
  const endAt = combineTehranDayAndTime(draft.endDayIso, draft.endTime || "00:00");
  if (!endAt) {
    return { ok: false, field: "endAt", message: "زمان پایان معتبر نیست." };
  }
  if (Date.parse(endAt) <= Date.parse(startAt)) {
    return { ok: false, field: "endAt", message: "زمان پایان باید بعد از شروع باشد." };
  }
  return { ok: true, startAt, endAt };
}

export type StoryOptionalWindowParse =
  | { ok: true; startAt: string | null; endAt: string | null }
  | { ok: false; field: "startAt" | "endAt"; message: string };

/** بازهٔ انتشار پیش‌نویس: هر دو خالی = بدون بازه؛ پایان بدون شروع رد می‌شود. */
export function parseOptionalPublicationWindow(
  draft: StoryScheduleDraft,
  options?: { requireEnd?: boolean },
): StoryOptionalWindowParse {
  const hasStart = Boolean(draft.startDayIso.trim());
  const hasEnd = Boolean(draft.endDayIso.trim());
  if (!hasStart && !hasEnd) {
    if (options?.requireEnd) {
      return { ok: false, field: "endAt", message: "زمان پایان را انتخاب کنید یا «بدون پایان» را فعال کنید." };
    }
    return { ok: true, startAt: null, endAt: null };
  }
  if (!hasStart && hasEnd) {
    return { ok: false, field: "startAt", message: "برای پایان، ابتدا زمان شروع را انتخاب کنید." };
  }
  if (options?.requireEnd && !hasEnd) {
    return { ok: false, field: "endAt", message: "زمان پایان را انتخاب کنید یا «بدون پایان» را فعال کنید." };
  }
  const parsed = parseStoryScheduleDraft(draft);
  if (!parsed.ok) return parsed;
  return { ok: true, startAt: parsed.startAt, endAt: parsed.endAt };
}

/** نمایش جلالی روز + ساعت در Asia/Tehran برای فهرست. */
export function formatStoryScheduleClock(iso: string | null | undefined, locale: "fa" | "en" = "fa"): string {
  if (!iso) return "";
  const zoned = dayjs(iso).tz(STORY_SCHEDULE_TIME_ZONE);
  if (!zoned.isValid()) return "";
  if (locale === "fa") {
    return zoned.calendar("jalali").format("YYYY/MM/DD HH:mm");
  }
  return zoned.format("YYYY-MM-DD HH:mm");
}

export function isoToScheduleDraft(iso: string | null): { dayIso: string; time: string } {
  if (!iso) return { dayIso: "", time: "" };
  const zoned = dayjs(iso).tz(STORY_SCHEDULE_TIME_ZONE);
  if (!zoned.isValid()) return { dayIso: "", time: "" };
  return {
    dayIso: zoned.format("YYYY-MM-DD"),
    time: zoned.format("HH:mm"),
  };
}
