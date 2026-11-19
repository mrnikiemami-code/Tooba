import assert from "node:assert/strict";
import test from "node:test";
import { authCookieSecure } from "./session-cookies.ts";

test("authCookieSecure is false for http localhost request even when NODE_ENV=production", () => {
  const previous = process.env.NODE_ENV;
  const previousOverride = process.env.TOOBA_COOKIE_SECURE;
  process.env.NODE_ENV = "production";
  delete process.env.TOOBA_COOKIE_SECURE;
  try {
    const request = new Request("http://127.0.0.1:3000/api/auth/csrf");
    assert.equal(authCookieSecure(request), false);
  } finally {
    process.env.NODE_ENV = previous;
    if (previousOverride === undefined) delete process.env.TOOBA_COOKIE_SECURE;
    else process.env.TOOBA_COOKIE_SECURE = previousOverride;
  }
});

test("authCookieSecure is true for https request", () => {
  const previousOverride = process.env.TOOBA_COOKIE_SECURE;
  delete process.env.TOOBA_COOKIE_SECURE;
  try {
    const request = new Request("https://shop.example/api/auth/csrf");
    assert.equal(authCookieSecure(request), true);
  } finally {
    if (previousOverride === undefined) delete process.env.TOOBA_COOKIE_SECURE;
    else process.env.TOOBA_COOKIE_SECURE = previousOverride;
  }
});

test("authCookieSecure respects x-forwarded-proto https", () => {
  const previousOverride = process.env.TOOBA_COOKIE_SECURE;
  delete process.env.TOOBA_COOKIE_SECURE;
  try {
    const request = new Request("http://127.0.0.1:3000/api/auth/csrf", {
      headers: { "x-forwarded-proto": "https" },
    });
    assert.equal(authCookieSecure(request), true);
  } finally {
    if (previousOverride === undefined) delete process.env.TOOBA_COOKIE_SECURE;
    else process.env.TOOBA_COOKIE_SECURE = previousOverride;
  }
});
