import type { Metadata } from "next";
import { DesignSystemShowcase } from "./showcase";

export const metadata: Metadata = {
  title: "Tooba Design System (internal)",
  robots: { index: false, follow: false },
};

/**
 * مسیر ویترین داخلی. ایندکس جستجو ندارد و ACCEPT بصری محصول نیست.
 */
export default function DesignSystemPage() {
  return <DesignSystemShowcase />;
}
