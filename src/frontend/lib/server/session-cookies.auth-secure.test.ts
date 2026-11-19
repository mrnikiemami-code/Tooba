import assert from "node:assert/strict";
import test from "node:test";
import { authCookieSecure } from "./session-cookies.ts";

function withEnv(run: () => void): void {
  const previous = process.env.NODE_ENV;
  const previousOverride = process.env.TOOBA_COOKIE_SECURE;
  process.env.NODE_ENV = "production";
  delete process.env.TOOBA_COOKIE_SECURE;
  try {
    run();
  } finally {
    process.env.NODE_ENV = previous;
    if (previousOverride === undefined) delete process.env.TOOBA_COOKIE_SECURE;
    else process.env.TOOBA_COOKIE_SECURE = previousOverride;
  }
}

test("canonical loopback HTTP allows Secure=false for local next start", () => {
  withEnv(() => {
    assert.equal(authCookieSecure(new Request("http://127.0.0.1:3000/api/auth/csrf")), false);
    assert.equal(authCookieSecure(new Request("http://localhost:3000/api/auth/csrf")), false);
  });
});

test("public HTTP cannot downgrade production Secure policy", () => {
  withEnv(() => {
    assert.equal(authCookieSecure(new Request("http://shop.example/api/auth/csrf")), true);
  });
});

test("HTTPS keeps Secure even when x-forwarded-proto claims http", () => {
  withEnv(() => {
    const request = new Request("https://shop.example/api/auth/csrf", {
      headers: { "x-forwarded-proto": "http" },
    });
    assert.equal(authCookieSecure(request), true);
  });
});

test("spoofed forwarded-http on loopback does not change loopback HTTP exception", () => {
  withEnv(() => {
    const request = new Request("http://127.0.0.1:3000/api/auth/csrf", {
      headers: { "x-forwarded-proto": "http" },
    });
    assert.equal(authCookieSecure(request), false);
  });
});

test("TOOBA_COOKIE_SECURE=false cannot force a global insecure downgrade", () => {
  withEnv(() => {
    process.env.TOOBA_COOKIE_SECURE = "false";
    assert.equal(authCookieSecure(new Request("https://shop.example/api/auth/csrf")), true);
    assert.equal(authCookieSecure(new Request("http://shop.example/api/auth/csrf")), true);
  });
});

test("TOOBA_COOKIE_SECURE=true forces Secure", () => {
  withEnv(() => {
    process.env.TOOBA_COOKIE_SECURE = "true";
    assert.equal(authCookieSecure(new Request("http://127.0.0.1:3000/api/auth/csrf")), true);
  });
});
