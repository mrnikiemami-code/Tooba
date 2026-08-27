"use client";

import Link from "next/link";
import type { ComponentProps } from "react";
import { useLocalizedPath } from "./locale-context.tsx";

type Props = Omit<ComponentProps<typeof Link>, "href"> & {
  href: string;
  /** اگر true، href بدون prefix locale استفاده می‌شود (پنل‌ها). */
  unprefixed?: boolean;
};

/** Link ویترین با حفظ locale فعال در URL. */
export function LocalizedLink({ href, unprefixed = false, ...props }: Props) {
  const lp = useLocalizedPath();
  const resolved = unprefixed || href.startsWith("/admin") || href.startsWith("/customer-panel") || href.startsWith("/vendor-panel")
    ? href
    : lp(href.startsWith("/") ? href : `/${href}`);
  return <Link href={resolved} {...props} />;
}
