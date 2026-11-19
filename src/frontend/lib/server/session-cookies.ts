import type { ResponseCookie } from "next/dist/compiled/@edge-runtime/cookies";
import {
  AUTH_COOKIE_PATH,
  CSRF_COOKIE_NAME,
  REFRESH_COOKIE_NAME,
  REFRESH_MAX_AGE_SECONDS,
  SESSION_COOKIE_NAME,
  SESSION_MAX_AGE_SECONDS,
} from "../auth/constants.ts";
import { createCsrfToken, csrfCookieOptions } from "../auth/csrf.ts";

/**
 * Cookie Secure for BFF auth cookies.
 * Default remains Secure in production. The only Soft exception is the request URL itself
 * on canonical loopback HTTP (127.0.0.1 / localhost / ::1) so local `next start` can store CSRF.
 * Public HTTP cannot downgrade. Spoofed x-forwarded-proto=http cannot downgrade HTTPS.
 * Unrestricted TOOBA_COOKIE_SECURE=false is not honored.
 */
export function authCookieSecure(request?: Request): boolean {
  const override = process.env.TOOBA_COOKIE_SECURE?.trim();
  if (override === "1" || override === "true") return true;

  if (request) {
    try {
      const url = new URL(request.url);
      const host = url.hostname.toLowerCase();
      const loopback = host === "127.0.0.1" || host === "localhost" || host === "::1" || host === "[::1]";
      if (url.protocol === "https:") return true;
      if (url.protocol === "http:" && loopback) return false;
      // Non-loopback HTTP keeps Secure (no public-HTTP downgrade).
      return true;
    } catch {
      /* fall through */
    }
  }
  return process.env.NODE_ENV === "production";
}

export function sessionCookieOptions(secure: boolean): Omit<ResponseCookie, "name" | "value"> {
  return {
    httpOnly: true,
    secure,
    sameSite: "lax",
    path: "/",
    maxAge: SESSION_MAX_AGE_SECONDS,
  };
}

export function refreshCookieOptions(secure: boolean): Omit<ResponseCookie, "name" | "value"> {
  return {
    httpOnly: true,
    secure,
    sameSite: "lax",
    path: AUTH_COOKIE_PATH,
    maxAge: REFRESH_MAX_AGE_SECONDS,
  };
}

export function buildAuthCookies(
  sessionId: string,
  refreshToken: string,
  request?: Request,
): { name: string; value: string; options: ResponseCookie }[] {
  const secure = authCookieSecure(request);
  const csrf = createCsrfToken();
  return [
    { name: SESSION_COOKIE_NAME, value: sessionId, options: { ...sessionCookieOptions(secure), name: SESSION_COOKIE_NAME, value: sessionId } },
    { name: REFRESH_COOKIE_NAME, value: refreshToken, options: { ...refreshCookieOptions(secure), name: REFRESH_COOKIE_NAME, value: refreshToken } },
    { name: CSRF_COOKIE_NAME, value: csrf, options: { ...csrfCookieOptions(secure), name: CSRF_COOKIE_NAME, value: csrf } },
  ];
}

export function clearAuthCookieOptions(secure: boolean): { name: string; options: ResponseCookie }[] {
  const expired = { maxAge: 0, path: "/" as const };
  return [
    { name: SESSION_COOKIE_NAME, options: { ...sessionCookieOptions(secure), ...expired, name: SESSION_COOKIE_NAME, value: "" } },
    { name: REFRESH_COOKIE_NAME, options: { ...refreshCookieOptions(secure), maxAge: 0, name: REFRESH_COOKIE_NAME, value: "" } },
    { name: CSRF_COOKIE_NAME, options: { ...csrfCookieOptions(secure), maxAge: 0, name: CSRF_COOKIE_NAME, value: "" } },
  ];
}

export { SESSION_COOKIE_NAME, REFRESH_COOKIE_NAME, CSRF_COOKIE_NAME };
