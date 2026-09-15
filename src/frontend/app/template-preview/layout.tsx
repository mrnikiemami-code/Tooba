import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  robots: { index: false, follow: false },
  title: "پیش‌نمایش قالب",
};

/** Preview routes: no Admin chrome; Storefront presentation only. */
export default function TemplatePreviewLayout({ children }: { children: ReactNode }) {
  return (
    <div data-testid="template-preview-layout" data-template-preview-shell="1">
      {children}
    </div>
  );
}
