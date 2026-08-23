import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./globals.css";

/**
 * فرادادهٔ پوستهٔ Next برای bootstrap پلتفرم.
 * UI تجاری، Design System و Data Grid اینجا تعریف نمی‌شوند.
 */
export const metadata: Metadata = {
  title: "Tooba",
  description: "Tooba storefront shell",
};

/**
 * لایهٔ ریشهٔ App Router. مرز ماژول بک‌اند را نشان نمی‌دهد و تم فروشگاه را اعمال نمی‌کند.
 * @param children خروجی مسیر جاری.
 */
export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
