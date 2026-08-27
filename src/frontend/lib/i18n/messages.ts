import type { Locale } from "./locale.ts";

/**
 * رشته‌های chrome مشترک fa/en.
 * سبک کلید هم‌راستا با design-system/workspace/messages.ts (retry/empty/error).
 */
export type ChromeMessages = {
  localeFa: string;
  localeEn: string;
  localeSwitcherLabel: string;
  empty: string;
  error: string;
  retry: string;
  notFound: string;
};

export const faChromeMessages: ChromeMessages = {
  localeFa: "فا",
  localeEn: "EN",
  localeSwitcherLabel: "زبان",
  empty: "موردی نیست",
  error: "خطا",
  retry: "تلاش دوباره",
  notFound: "یافت نشد",
};

export const enChromeMessages: ChromeMessages = {
  localeFa: "FA",
  localeEn: "EN",
  localeSwitcherLabel: "Language",
  empty: "Nothing here",
  error: "Error",
  retry: "Retry",
  notFound: "Not found",
};

export function chromeMessagesFor(locale: Locale): ChromeMessages {
  return locale === "en" ? enChromeMessages : faChromeMessages;
}
