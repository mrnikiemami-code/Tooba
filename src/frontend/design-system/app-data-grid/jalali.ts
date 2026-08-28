import dayjs from "dayjs";
import jalaliday from "jalaliday";

dayjs.extend(jalaliday);

/** نمایش تاریخ جلالی برای UI؛ API همیشه ISO/Gregorian می‌ماند. */
export function formatJalaliDate(iso: string | null | undefined, locale: "fa" | "en"): string {
  if (!iso) return "—";
  const d = dayjs(iso);
  if (!d.isValid()) return iso.slice(0, 10);
  if (locale === "fa") {
    return d.calendar("jalali").locale("fa").format("YYYY/MM/DD");
  }
  return d.format("YYYY-MM-DD");
}

/** تبدیل ورودی جلالی yyyy/mm/dd به ISO UTC برای API. */
export function jalaliInputToIso(input: string): string | undefined {
  const trimmed = input.trim();
  if (!trimmed) return undefined;
  const parts = trimmed.split(/[/-]/).map((p) => Number.parseInt(p, 10));
  if (parts.length !== 3 || parts.some((n) => Number.isNaN(n))) return undefined;
  const [jy, jm, jd] = parts;
  const g = dayjs()
    .calendar("jalali")
    .year(jy)
    .month(jm - 1)
    .date(jd)
    .hour(12)
    .minute(0)
    .second(0);
  return g.isValid() ? g.toDate().toISOString() : undefined;
}
