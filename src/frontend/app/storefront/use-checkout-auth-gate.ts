"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useLocale } from "../../lib/i18n/locale-context.tsx";
import { loginPath } from "../../lib/auth/login-return-to.ts";
import { requiresCheckoutLogin } from "./storefront-identity-api.ts";

/** هدایت ناشناس به ورود وقتی سیاست AuthenticatedOnly است. */
export function useCheckoutAuthGate(returnPath: string): void {
  const locale = useLocale();
  const router = useRouter();
  useEffect(() => {
    let cancelled = false;
    void requiresCheckoutLogin().then((needsLogin) => {
      if (!cancelled && needsLogin) {
        router.replace(loginPath(locale, `/${locale}${returnPath.startsWith("/") ? returnPath : `/${returnPath}`}`));
      }
    });
    return () => {
      cancelled = true;
    };
  }, [locale, returnPath, router]);
}
