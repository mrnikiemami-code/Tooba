"use client";

import Link from "next/link";
import type { ComponentProps, MouseEvent } from "react";
import { useLocalizedPath } from "./locale-context.tsx";
import { useStorefrontNavigationPending } from "../storefront-navigation/navigation-pending-context.tsx";

type Props = Omit<ComponentProps<typeof Link>, "href"> & {
  href: string;
  /** اگر true، href بدون prefix locale استفاده می‌شود (پنل‌ها). */
  unprefixed?: boolean;
  /** When false, skip storefront pending acknowledgment (rare). Default true. */
  announceNavigationPending?: boolean;
};

/** Link ویترین با حفظ locale فعال در URL. */
export function LocalizedLink({
  href,
  unprefixed = false,
  announceNavigationPending = true,
  onClick,
  ...props
}: Props) {
  const lp = useLocalizedPath();
  const navPending = useStorefrontNavigationPending();
  const alreadyLocalePrefixed = /^\/(fa|en)(\/|$)/.test(href);
  const resolved =
    unprefixed ||
    alreadyLocalePrefixed ||
    href.startsWith("/admin") ||
    href.startsWith("/customer-panel") ||
    href.startsWith("/vendor-panel")
      ? href
      : lp(href.startsWith("/") ? href : `/${href}`);

  const handleClick = (event: MouseEvent<HTMLAnchorElement>) => {
    onClick?.(event);
    if (event.defaultPrevented || !announceNavigationPending) {
      return;
    }
    const accepted = navPending.begin(resolved, event);
    if (!accepted) {
      event.preventDefault();
    }
  };

  return <Link href={resolved} {...props} onClick={handleClick} />;
}
