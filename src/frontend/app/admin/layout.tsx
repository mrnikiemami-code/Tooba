import type { Metadata } from "next";
import { Suspense, type ReactNode } from "react";
import { AdminShell } from "./admin-shell";

export const metadata: Metadata = {
  title: "Tooba Admin Product Workspace",
  robots: { index: false, follow: false },
};

/**
 * پوستهٔ Admin. این layout فروشگاه یا پنل Seller نیست.
 */
export default function AdminLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <Suspense
        fallback={
          <div className="flex min-h-screen items-center justify-center bg-gray-50 text-gray-500">
            در حال آماده‌سازی پنل مدیریت…
          </div>
        }
      >
        <AdminShell>{children}</AdminShell>
      </Suspense>
    </div>
  );
}
