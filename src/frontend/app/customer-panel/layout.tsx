import type { Metadata } from "next";
import type { ReactNode } from "react";
import { CustomerPanelShell } from "./customer-panel-shell";

export const metadata: Metadata = {
  title: "پنل مشتری توبا",
  robots: { index: false, follow: false },
};

/**
 * پوستهٔ مستقل پنل مشتری Shopeiva؛ فروشگاه و پنل فروشنده را جایگزین نمی‌کند.
 */
export default function CustomerPanelLayout({ children }: { children: ReactNode }) {
  return <CustomerPanelShell>{children}</CustomerPanelShell>;
}
