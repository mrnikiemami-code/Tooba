"use client";

import type { CSSProperties, ReactNode } from "react";
import { useStorefrontAppearanceStyle } from "../../lib/storefront-appearance/storefront-appearance-context.tsx";

/** Applies store appearance CSS vars only inside the storefront canvas boundary. */
export function StorefrontThemedCanvas({
  children,
  className,
  style,
}: {
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
}) {
  const appearanceStyle = useStorefrontAppearanceStyle();
  return (
    <div
      className={className}
      data-storefront-canvas
      data-storefront-surface-role="page"
      data-storefront-theme-scope="storefront"
      style={{ ...appearanceStyle, ...style }}
    >
      {children}
    </div>
  );
}
