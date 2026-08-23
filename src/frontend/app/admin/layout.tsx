import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  title: "Tooba Admin Product Workspace",
  robots: { index: false, follow: false },
};

/**
 * پوستهٔ Admin. این layout فروشگاه یا پنل Seller نیست.
 */
export default function AdminLayout({ children }: { children: ReactNode }) {
  return <div className="min-h-screen bg-background text-foreground">{children}</div>;
}
