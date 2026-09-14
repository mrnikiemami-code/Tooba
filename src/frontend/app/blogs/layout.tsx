import type { ReactNode } from "react";
import { StorefrontShell } from "../storefront/storefront-shell.tsx";
import { loadStorefrontHome } from "../storefront/storefront-api.ts";

/**
 * Content/blog routes inherit the same Storefront shell and global appearance as the rest of the store.
 */
export default async function BlogsLayout({ children }: { children: ReactNode }) {
  const home = await loadStorefrontHome();
  return <StorefrontShell categories={home?.categories ?? []}>{children}</StorefrontShell>;
}
