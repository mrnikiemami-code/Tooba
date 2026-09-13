import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  invalidateStorefrontSession,
  loadStorefrontSession,
  markStorefrontSessionAnonymous,
  resetStorefrontSessionCacheForTests,
  storefrontAccountLabel,
} from "./storefront-identity-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("logged-in with profile name uses name", () => {
  assert.equal(storefrontAccountLabel({ displayName: "علی رضایی", mobile: "09111111111" }, "حساب کاربری"), "علی رضایی");
});

test("logged-in without name uses mobile", () => {
  assert.equal(storefrontAccountLabel({ displayName: "", firstName: "", lastName: "", mobile: "09111111111" }, "حساب کاربری"), "09111111111");
});

test("generic account label is last fallback only", () => {
  assert.equal(storefrontAccountLabel({ displayName: "  ", mobile: "" }, "حساب کاربری"), "حساب کاربری");
});

test("first and last compose when displayName is absent", () => {
  assert.equal(storefrontAccountLabel({ firstName: "محمد", lastName: "امامی", mobile: "0912" }, "حساب کاربری"), "محمد امامی");
});

test("account label never uses shipping recipient fields", () => {
  const identity = fs.readFileSync(path.join(root, "app/storefront/storefront-identity-api.ts"), "utf8");
  const menu = fs.readFileSync(path.join(root, "app/storefront/storefront-account-menu.tsx"), "utf8");
  assert.doesNotMatch(identity, /recipientName|RecipientName/);
  assert.doesNotMatch(menu, /recipientName|RecipientName/);
  assert.match(menu, /header-account-label/);
  assert.match(menu, /loadStorefrontSession/);
  assert.match(menu, /markStorefrontSessionAnonymous/);
  assert.doesNotMatch(identity, /setInterval/);
  assert.doesNotMatch(menu, /setInterval/);
});

function installBrowser() {
  (globalThis as { window?: Window }).window = {
    dispatchEvent: () => true,
  } as unknown as Window;
}

test("concurrent session consumers share one auth/me request", async () => {
  resetStorefrontSessionCacheForTests();
  installBrowser();
  let fetches = 0;
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: string | URL | Request) => {
    if (String(input).includes("/api/auth/me")) {
      fetches += 1;
      return new Response(JSON.stringify({ userId: "u1", mobile: "09111111111" }), { status: 200 });
    }
    return new Response("no", { status: 404 });
  }) as typeof fetch;
  try {
    const [a, b, c] = await Promise.all([
      loadStorefrontSession("حساب کاربری"),
      loadStorefrontSession("حساب کاربری"),
      loadStorefrontSession("Account"),
    ]);
    assert.equal(fetches, 1);
    assert.equal(a.authenticated, true);
    assert.equal(b.mobile, "09111111111");
    assert.equal(c.label, "09111111111");
    await loadStorefrontSession();
    assert.equal(fetches, 1);
  } finally {
    globalThis.fetch = original;
    resetStorefrontSessionCacheForTests();
  }
});

test("expected anonymous 401 is cached and not retried", async () => {
  resetStorefrontSessionCacheForTests();
  installBrowser();
  let fetches = 0;
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: string | URL | Request) => {
    if (String(input).includes("/api/auth/me")) {
      fetches += 1;
      return new Response("", { status: 401 });
    }
    return new Response("no", { status: 404 });
  }) as typeof fetch;
  try {
    const first = await loadStorefrontSession();
    const second = await loadStorefrontSession();
    markStorefrontSessionAnonymous();
    const third = await loadStorefrontSession();
    assert.equal(first.authenticated, false);
    assert.equal(second.authenticated, false);
    assert.equal(third.authenticated, false);
    assert.equal(fetches, 1);
  } finally {
    globalThis.fetch = original;
    resetStorefrontSessionCacheForTests();
  }
});

test("invalidate allows one new auth/me after login transition", async () => {
  resetStorefrontSessionCacheForTests();
  installBrowser();
  let fetches = 0;
  const original = globalThis.fetch;
  globalThis.fetch = (async () => {
    fetches += 1;
    return new Response(JSON.stringify({ userId: "u2", mobile: "09111111111" }), { status: 200 });
  }) as typeof fetch;
  try {
    await loadStorefrontSession();
    invalidateStorefrontSession();
    await loadStorefrontSession();
    await loadStorefrontSession();
    assert.equal(fetches, 2);
  } finally {
    globalThis.fetch = original;
    resetStorefrontSessionCacheForTests();
  }
});
