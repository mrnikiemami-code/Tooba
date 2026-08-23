import type { Metadata } from "next";
import type { ReactNode } from "react";
import { AppProviders } from "./providers";
import "./globals.css";

/**
 * فرادادهٔ پوسته. Design System مالک تجربه است نه ماژول بک‌اند.
 */
export const metadata: Metadata = {
  title: "Tooba",
  description: "Tooba experience shell",
};

/**
 * لایهٔ ریشه. جهت و تم از Design System روی html اعمال می‌شود.
 */
export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="fa" dir="rtl" suppressHydrationWarning>
      <body>
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
