"use client";

import { createContext, useContext, type CSSProperties, type ReactNode } from "react";
import type { StorefrontAppearanceProjection } from "../../app/storefront/storefront-appearance-api.ts";
import { storefrontAppearanceStyle } from "../../app/storefront/storefront-appearance-api.ts";

const StorefrontAppearanceContext = createContext<StorefrontAppearanceProjection | null>(null);

/** Provides store appearance tokens to storefront/customer canvases only — never to Admin/Seller. */
export function StorefrontAppearanceProvider({
  appearance,
  children,
}: {
  appearance: StorefrontAppearanceProjection;
  children: ReactNode;
}) {
  return (
    <StorefrontAppearanceContext.Provider value={appearance}>
      {children}
    </StorefrontAppearanceContext.Provider>
  );
}

export function useStorefrontAppearance(): StorefrontAppearanceProjection | null {
  return useContext(StorefrontAppearanceContext);
}

export function useStorefrontAppearanceStyle(): CSSProperties | undefined {
  const appearance = useStorefrontAppearance();
  if (!appearance) return undefined;
  return storefrontAppearanceStyle(appearance) as CSSProperties;
}
