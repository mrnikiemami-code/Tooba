"use client";

import type { ReactNode } from "react";
import { ThemeProvider } from "../design-system";

/**
 * پوشش کلاینت برای تم معنایی. منطق دامنه اینجا نیست.
 */
export function AppProviders({ children }: { children: ReactNode }) {
  return <ThemeProvider>{children}</ThemeProvider>;
}
