import { CSRF_COOKIE_NAME, CSRF_HEADER_NAME } from "./constants.ts";

/** Reads readable CSRF cookie for double-submit header. */
export function readCsrfCookie(): string | null {
  if (typeof document === "undefined") return null;
  const parts = document.cookie.split(";").map((part) => part.trim());
  for (const part of parts) {
    if (part.startsWith(`${CSRF_COOKIE_NAME}=`)) {
      return decodeURIComponent(part.slice(CSRF_COOKIE_NAME.length + 1));
    }
  }
  return null;
}

/** Builds browser fetch init for same-origin BFF with session cookies. */
export function bffFetchHeaders(json = false): Record<string, string> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (json) headers["Content-Type"] = "application/json";
  const csrf = readCsrfCookie();
  if (csrf) headers[CSRF_HEADER_NAME] = csrf;
  return headers;
}

/** Ensures CSRF cookie exists before mutating auth/customer calls. */
export async function ensureCsrfCookie(): Promise<void> {
  if (typeof window === "undefined") return;
  if (readCsrfCookie()) return;
  await fetch("/api/auth/csrf", { credentials: "include", cache: "no-store" });
}
