"use client";

import { useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Smartphone } from "lucide-react";
import { useLocale } from "../../lib/i18n/locale-context.tsx";
import { sanitizeReturnTo } from "../../lib/auth/login-return-to.ts";
import { bffFetchHeaders, ensureCsrfCookie } from "../../lib/auth/browser-session.ts";
import { mergeStorefrontCartAfterLogin, resetStorefrontMergeTransition } from "../storefront/storefront-cart-api.ts";
import { invalidateStorefrontSession, markStorefrontSessionAnonymous, notifyAuthChanged } from "../storefront/storefront-identity-api.ts";

type Step = "mobile" | "otp";

/**
 * ورود مشتری با موبایل و OTP روی BFF موجود. فرم رمز فروشگاهی ندارد.
 */
export function StorefrontCustomerLogin() {
  const locale = useLocale();
  const router = useRouter();
  const params = useSearchParams();
  const returnTo = useMemo(
    () => sanitizeReturnTo(params.get("returnTo"), locale),
    [params, locale],
  );
  const copy = locale === "en"
    ? {
        title: "Sign in",
        lead: "Enter your mobile number to sign in.",
        mobile: "Mobile number",
        otp: "One-time code",
        send: "Send code",
        verify: "Continue",
        sending: "Sending…",
        verifying: "Signing in…",
        failed: "Sign-in failed. Please try again.",
        logout: "Sign out",
      }
    : {
        title: "ورود به حساب",
        lead: "شماره موبایل خود را وارد کنید.",
        mobile: "شماره موبایل",
        otp: "کد یک‌بارمصرف",
        send: "ارسال کد",
        verify: "ادامه",
        sending: "در حال ارسال…",
        verifying: "در حال ورود…",
        failed: "ورود انجام نشد. دوباره تماس بگیرید.",
        logout: "خروج",
      };

  const [step, setStep] = useState<Step>("mobile");
  const [mobile, setMobile] = useState("");
  const [otp, setOtp] = useState("");
  const [challengeId, setChallengeId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function requestCode() {
    setBusy(true);
    setError(null);
    try {
      await ensureCsrfCookie();
      const response = await fetch("/api/auth/otp-request", {
        method: "POST",
        credentials: "include",
        cache: "no-store",
        headers: bffFetchHeaders(true),
        body: JSON.stringify({ identifier: mobile.trim() }),
      });
      const payload = await response.json().catch(() => null) as { challengeId?: string } | null;
      if (!response.ok || !payload?.challengeId) {
        setError(copy.failed);
        return;
      }
      setChallengeId(payload.challengeId);
      setStep("otp");
    } catch {
      setError(copy.failed);
    } finally {
      setBusy(false);
    }
  }

  async function completeLogin() {
    setBusy(true);
    setError(null);
    try {
      await ensureCsrfCookie();
      const response = await fetch("/api/auth/otp-complete", {
        method: "POST",
        credentials: "include",
        cache: "no-store",
        headers: bffFetchHeaders(true),
        body: JSON.stringify({
          identifier: mobile.trim(),
          challengeId,
          secret: otp.trim(),
        }),
      });
      if (!response.ok) {
        setError(copy.failed);
        return;
      }
      invalidateStorefrontSession();
      await mergeStorefrontCartAfterLogin();
      notifyAuthChanged();
      router.replace(returnTo);
    } catch {
      setError(copy.failed);
    } finally {
      setBusy(false);
    }
  }

  async function logout() {
    setBusy(true);
    try {
      await ensureCsrfCookie();
      await fetch("/api/auth/logout", {
        method: "POST",
        credentials: "include",
        cache: "no-store",
        headers: bffFetchHeaders(true),
      });
      const { clearCartSession } = await import("../storefront/storefront-cart-api.ts");
      markStorefrontSessionAnonymous();
      resetStorefrontMergeTransition();
      clearCartSession();
      notifyAuthChanged();
      setStep("mobile");
      setOtp("");
      setChallengeId("");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="max-w-md mx-auto px-4 py-10 md:py-16" data-testid="storefront-login-page" data-storefront-surface-role="section" dir={locale === "en" ? "ltr" : "rtl"}>
      <div className="bg-surface-elevated rounded-2xl border border-gray-200 shadow-sm p-5 md:p-7" data-storefront-surface-role="elevated">
        <div className="flex items-center gap-2 mb-2">
          <Smartphone className="w-5 h-5 text-primary" />
          <h1 className="text-lg font-black text-gray-900">{copy.title}</h1>
        </div>
        <p className="text-sm text-gray-500 leading-7 mb-5">{copy.lead}</p>
        {step === "mobile" ? (
          <form
            className="space-y-4"
            onSubmit={(event) => {
              event.preventDefault();
              void requestCode();
            }}
          >
            <label className="block text-sm font-bold text-gray-800" htmlFor="login-mobile">
              {copy.mobile}
            </label>
            <input
              id="login-mobile"
              inputMode="tel"
              autoComplete="tel"
              value={mobile}
              onChange={(event) => setMobile(event.target.value)}
              className="w-full px-3.5 py-3 rounded-xl text-sm bg-surface border border-gray-200 outline-none focus:ring-2 focus:ring-primary"
              data-testid="login-mobile-input"
            />
            <button
              type="submit"
              disabled={busy || mobile.trim().length < 8}
              className="w-full py-3 rounded-2xl font-black text-sm bg-primary text-white hover:bg-primary-strong disabled:opacity-60"
              data-testid="login-send-otp"
            >
              {busy ? copy.sending : copy.send}
            </button>
          </form>
        ) : (
          <form
            className="space-y-4"
            onSubmit={(event) => {
              event.preventDefault();
              void completeLogin();
            }}
          >
            <label className="block text-sm font-bold text-gray-800" htmlFor="login-otp">
              {copy.otp}
            </label>
            <input
              id="login-otp"
              inputMode="numeric"
              autoComplete="one-time-code"
              value={otp}
              onChange={(event) => setOtp(event.target.value)}
              className="w-full px-3.5 py-3 rounded-xl text-sm bg-surface border border-gray-200 outline-none focus:ring-2 focus:ring-primary"
              data-testid="login-otp-input"
            />
            <button
              type="submit"
              disabled={busy || otp.trim().length < 4}
              className="w-full py-3 rounded-2xl font-black text-sm bg-primary text-white hover:bg-primary-strong disabled:opacity-60"
              data-testid="login-verify-otp"
            >
              {busy ? copy.verifying : copy.verify}
            </button>
          </form>
        )}
        {error ? (
          <p className="text-sm text-amber-700 mt-4" data-testid="login-error">{error}</p>
        ) : null}
        <button
          type="button"
          onClick={() => void logout()}
          className="mt-5 text-xs text-gray-400 hover:text-gray-600"
          data-testid="login-logout"
        >
          {copy.logout}
        </button>
      </div>
    </section>
  );
}
