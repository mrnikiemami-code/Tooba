import { randomBytes } from "node:crypto";
import { CSRF_COOKIE_NAME, CSRF_HEADER_NAME, CSRF_MAX_AGE_SECONDS } from "./constants.ts";

export function createCsrfToken(): string {
  return randomBytes(32).toString("hex");
}

export function csrfCookieOptions(secure: boolean) {
  return {
    httpOnly: false,
    secure,
    sameSite: "lax" as const,
    path: "/",
    maxAge: CSRF_MAX_AGE_SECONDS,
  };
}

export function validateCsrf(request: Request, csrfCookieValue: string | undefined): boolean {
  const header = request.headers.get(CSRF_HEADER_NAME);
  if (!header || !csrfCookieValue) return false;
  return header === csrfCookieValue;
}

export { CSRF_COOKIE_NAME, CSRF_HEADER_NAME };
