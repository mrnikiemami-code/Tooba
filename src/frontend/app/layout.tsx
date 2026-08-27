import type { Metadata } from "next";
import type { ReactNode } from "react";
import { cookies } from "next/headers";
import { AppProviders } from "./providers";
import {
  DEFAULT_LOCALE,
  LOCALE_COOKIE_NAME,
  dirForLocale,
  langForLocale,
  parseLocale,
} from "../lib/i18n/locale";
import "./globals.css";

/**
 * فرادادهٔ پوسته. Design System مالک تجربه است نه ماژول بک‌اند.
 */
export const metadata: Metadata = {
  title: "Tooba",
  description: "Tooba experience shell",
};

/**
 * لایهٔ ریشه. lang/dir از کوکی tooba_locale؛ فارسی پیش‌فرض و RTL.
 */
export default async function RootLayout({ children }: { children: ReactNode }) {
  const jar = await cookies();
  const locale = parseLocale(jar.get(LOCALE_COOKIE_NAME)?.value) ?? DEFAULT_LOCALE;

  return (
    <html lang={langForLocale(locale)} dir={dirForLocale(locale)} suppressHydrationWarning>
      <body>
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
