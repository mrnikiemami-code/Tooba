import assert from "node:assert/strict";
import test from "node:test";
import { createAdminMenu, listAdminMenus } from "./admin-menus-api.ts";

const ADMIN_DEV_ACTOR_HEADER = "X-Tooba-Dev-Actor-User-Id";

type CapturedRequest = { url: string; init: RequestInit | undefined; headers: Record<string, string> };

/** Stubs global fetch and returns the captured Admin requests for assertion. */
function stubFetch(status: number, body: unknown): CapturedRequest[] {
  const captured: CapturedRequest[] = [];
  globalThis.fetch = (async (url: string | URL | Request, init?: RequestInit) => {
    captured.push({
      url: String(url),
      init,
      headers: (init?.headers ?? {}) as Record<string, string>,
    });
    return new Response(JSON.stringify(body), {
      status,
      headers: { "Content-Type": "application/json" },
    });
  }) as typeof fetch;
  return captured;
}

test("menu requests carry the canonical admin headers", async () => {
  const original = globalThis.fetch;
  try {
    const captured = stubFetch(200, []);
    await listAdminMenus();
    assert.equal(captured.length, 1);
    const headers = captured[0]!.headers;
    assert.equal(headers.Accept, "application/json");
    assert.ok(ADMIN_DEV_ACTOR_HEADER in headers);
  } finally {
    globalThis.fetch = original;
  }
});

test("menu mutations stay on the admin menus route", async () => {
  const original = globalThis.fetch;
  try {
    const captured = stubFetch(200, { menuId: "m1", title: "منو" });
    await createAdminMenu("منو");
    assert.equal(captured[0]!.url, "/v1/admin/menus");
    assert.equal(captured[0]!.init?.method, "POST");
    assert.equal(captured[0]!.init?.body, JSON.stringify({ title: "منو", locale: "fa" }));
  } finally {
    globalThis.fetch = original;
  }
});

test("denied menu calls use the canonical denied contract", async () => {
  const original = globalThis.fetch;
  try {
    stubFetch(403, { title: "Forbidden" });
    const result = await listAdminMenus();
    assert.equal(result.ok, false);
    assert.equal(result.ok === false && result.denied, true);
    assert.equal(result.ok === false && result.message, "دسترسی مجاز نیست.");
  } finally {
    globalThis.fetch = original;
  }
});

test("known menu error codes resolve to their localized menu message", async () => {
  const original = globalThis.fetch;
  try {
    stubFetch(400, { errorCode: "menu.item.cycle" });
    const result = await listAdminMenus();
    assert.equal(result.ok === false && result.message, "چرخه در درخت منو مجاز نیست.");
  } finally {
    globalThis.fetch = original;
  }
});

test("unmapped failures never leak technical text to the admin", async () => {
  const original = globalThis.fetch;
  try {
    stubFetch(400, { title: "Bad Request" });
    const result = await listAdminMenus();
    assert.equal(result.ok, false);
    const message = result.ok === false ? result.message : "";
    assert.equal(message, "خطایی رخ داد. لطفاً دوباره تلاش کنید.");
    assert.doesNotMatch(message, /Bad Request|admin\.http/);
  } finally {
    globalThis.fetch = original;
  }
});
