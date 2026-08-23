/**
 * کلاس‌های Tailwind را بدون وابستگی اضافی به هم می‌چسباند.
 */
export function cn(...parts: Array<string | false | null | undefined>): string {
  return parts.filter(Boolean).join(" ");
}
