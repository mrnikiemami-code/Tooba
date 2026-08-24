import type { Metadata } from "next";
import type { ReactNode } from "react";
import { VendorShell } from "./vendor-shell";

export const metadata: Metadata = {
  title: "Tooba Seller Panel",
  robots: { index: false, follow: false },
};

/**
 * Layout پنل فروشنده. پوستهٔ Admin یا فروشگاه نیست.
 */
export default function VendorPanelLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <VendorShell>{children}</VendorShell>
    </div>
  );
}
