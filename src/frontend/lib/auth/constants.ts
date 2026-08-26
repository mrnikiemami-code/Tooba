/** HttpOnly session cookie (opaque SessionId). */
export const SESSION_COOKIE_NAME = "tooba_session";

/** HttpOnly refresh cookie scoped to /api/auth. */
export const REFRESH_COOKIE_NAME = "tooba_refresh";

/** Readable CSRF cookie for double-submit validation. */
export const CSRF_COOKIE_NAME = "tooba_csrf";

/** Header carrying CSRF token on mutating BFF requests. */
export const CSRF_HEADER_NAME = "X-Tooba-Csrf";

/** Dev-only actor header forwarded server-side by BFF. */
export const DEV_ACTOR_HEADER = "X-Tooba-Dev-Actor-User-Id";

export const DEFAULT_DEV_ACTOR_ID = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";

export const AUTH_COOKIE_PATH = "/api/auth";

export const SESSION_MAX_AGE_SECONDS = 60 * 60 * 24 * 14;

export const REFRESH_MAX_AGE_SECONDS = 60 * 60 * 24 * 14;

export const CSRF_MAX_AGE_SECONDS = 60 * 60 * 24;
