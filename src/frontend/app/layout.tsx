import type { Metadata } from "next";
import type { ReactNode } from "react";
import { cookies, headers } from "next/headers";
import { AppProviders } from "./providers";
import { LocaleProvider } from "../lib/i18n/locale-context";
import {
  DEFAULT_LOCALE,
  LOCALE_COOKIE_NAME,
  dirForLocale,
  isLocale,
  langForLocale,
  parseLocale,
} from "../lib/i18n/locale";
import { LOCALE_HEADER_NAME } from "../lib/i18n/routing";
import "./globals.css";

/**
 * فرادادهٔ پوسته. Design System مالک تجربه است نه ماژول بک‌اند.
 */
export const metadata: Metadata = {
  title: "Tooba",
  description: "Tooba experience shell",
};

/**
 * لایهٔ ریشه. lang/dir از prefix URL (x-tooba-locale) یا کوکی ترجیحی.
 */
export default async function RootLayout({ children }: { children: ReactNode }) {
  const headerStore = await headers();
  const headerLocale = headerStore.get(LOCALE_HEADER_NAME);
  const jar = await cookies();
  const locale =
    headerLocale && isLocale(headerLocale)
      ? headerLocale
      : parseLocale(jar.get(LOCALE_COOKIE_NAME)?.value) ?? DEFAULT_LOCALE;

  return (
    <html lang={langForLocale(locale)} dir={dirForLocale(locale)} suppressHydrationWarning>
      <body>
        <AppProviders>
          <LocaleProvider locale={locale}>{children}</LocaleProvider>
        </AppProviders>
      </body>
    </html>
  );
}
