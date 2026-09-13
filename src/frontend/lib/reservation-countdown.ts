export type CountdownLocale = "fa" | "en";

const FA_DIGITS = ["۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹"] as const;

function toFaDigits(value: number): string {
  return String(value).replace(/\d/g, (digit) => FA_DIGITS[Number(digit)] ?? digit);
}

/** Display formatter only. Server ExpiresAt remains authoritative. */
export function formatCountdown(totalSeconds: number): string {
  const safe = Math.max(0, Math.floor(totalSeconds));
  const hours = Math.floor(safe / 3600);
  const minutes = Math.floor((safe % 3600) / 60);
  const seconds = safe % 60;
  if (hours > 0) {
    return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
  }
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

/** Minute-resolution label so screen readers are not updated every second. */
export function formatCountdownAccessibleLabel(totalSeconds: number, locale: CountdownLocale): string {
  const safe = Math.max(0, Math.floor(totalSeconds));
  const hours = Math.floor(safe / 3600);
  const minutes = Math.floor((safe % 3600) / 60);
  const days = Math.floor(hours / 24);
  const remainHours = hours % 24;
  if (locale === "fa") {
    if (days > 0) {
      return `${toFaDigits(days)} روز و ${toFaDigits(remainHours)} ساعت و ${toFaDigits(minutes)} دقیقه تا پایان مهلت رزرو`;
    }
    if (hours > 0) {
      return `${toFaDigits(hours)} ساعت و ${toFaDigits(minutes)} دقیقه تا پایان مهلت رزرو`;
    }
    return `${toFaDigits(minutes)} دقیقه تا پایان مهلت رزرو`;
  }
  if (days > 0) {
    return `${days} day${days === 1 ? "" : "s"} ${remainHours} hour${remainHours === 1 ? "" : "s"} and ${minutes} minute${minutes === 1 ? "" : "s"} remaining`;
  }
  if (hours > 0) {
    return `${hours} hour${hours === 1 ? "" : "s"} and ${minutes} minute${minutes === 1 ? "" : "s"} remaining`;
  }
  return `${minutes} minute${minutes === 1 ? "" : "s"} remaining`;
}
