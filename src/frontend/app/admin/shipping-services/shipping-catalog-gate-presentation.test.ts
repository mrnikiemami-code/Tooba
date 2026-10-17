/**
 * قرارداد نمایش و نگاشت شکست‌های خواندن کاتالوگ یک‌سطحی روش‌های ارسال.
 *
 * این تست‌ها سه چیز را قفل می‌کنند:
 *   - هر دلیلِ خاموش‌بودن دروازه کد شکست مستقل خودش را دارد و هیچ دلیلی به دیگری تا نشود؛
 *   - «تلاش دوباره» فقط برای شکست گذرا پیشنهاد می‌شود؛ «دروازهٔ خاموش در این محیط» شکست
 *     پیکربندی است و تلاش دوباره ندارد؛
 *   - متن توضیح هرگز متنِ دکمه نیست (خطای قبلی: detail دقیقاً «تلاش دوباره» بود).
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  catalogFailureCodeForGate,
  loadOneLevelCatalog,
} from "./flat-method-catalog-api.ts";
import { presentShippingCatalogFailure } from "./shipping-catalog-gate-presentation.ts";

function withFetch<T>(fake: typeof fetch, run: () => Promise<T>): Promise<T> {
  const original = globalThis.fetch;
  globalThis.fetch = fake;
  return run().finally(() => {
    globalThis.fetch = original;
  });
}

const json = (payload: unknown, status = 200) =>
  new Response(JSON.stringify(payload), {
    status,
    headers: { "Content-Type": "application/json" },
  });

test("a disabled gate is a configuration state, not a retryable load failure", () => {
  const view = presentShippingCatalogFailure("shipping_catalog.gate_disabled");
  assert.equal(view.retryable, false);
  assert.match(view.title, /فعال نیست/);
  assert.notEqual(view.detail, "تلاش دوباره");
  assert.ok(view.detail.length > 10);
});

test("transient and authorization failures stay retryable and carry a real explanation", () => {
  for (const code of [
    "admin.authorization.denied",
    "shipping_catalog.not_ready",
    "shipping_catalog.invalid_response",
    "shipping_catalog.load_failed",
    "host-unreachable",
  ]) {
    const view = presentShippingCatalogFailure(code);
    assert.equal(view.retryable, true, code);
    assert.ok(view.title.length > 0, code);
    assert.notEqual(view.detail, "تلاش دوباره", code);
  }
});

test("an unknown or empty code falls back to the generic load failure, never to the button label", () => {
  for (const code of [undefined, null, "", "something.new"]) {
    const view = presentShippingCatalogFailure(code);
    assert.equal(view.title, presentShippingCatalogFailure("shipping_catalog.load_failed").title);
    assert.equal(view.retryable, true);
    assert.notEqual(view.detail, "تلاش دوباره");
  }
});

test("each disabled reason maps to its own stable failure code", () => {
  assert.equal(catalogFailureCodeForGate({ state: "disabled", reason: "catalog-not-ready" }), "shipping_catalog.not_ready");
  assert.equal(catalogFailureCodeForGate({ state: "disabled", reason: "untrusted-source" }), "shipping_catalog.invalid_response");
  assert.equal(catalogFailureCodeForGate({ state: "disabled", reason: "explicitly-off" }), "shipping_catalog.gate_disabled");
  assert.equal(catalogFailureCodeForGate({ state: "disabled", reason: "absent" }), "shipping_catalog.gate_disabled");
});

test("the reader surfaces the Host gate answer without inventing a successful empty catalog", async () => {
  const disabled = await withFetch(
    (async () => json({ gate: "Disabled", ready: false, methods: [] })) as typeof fetch,
    loadOneLevelCatalog,
  );
  assert.equal(disabled.state, "error");
  assert.equal(disabled.message, "shipping_catalog.gate_disabled");
  assert.equal(disabled.data, null);

  const notReady = await withFetch(
    (async () => json({ gate: "Enabled", ready: false, methods: [] })) as typeof fetch,
    loadOneLevelCatalog,
  );
  assert.equal(notReady.state, "error");
  assert.equal(notReady.message, "shipping_catalog.not_ready");

  const enabled = await withFetch(
    (async () => json({ gate: "Enabled", ready: true, languageId: "11111111-1111-4111-8111-111111111111", methods: [] })) as typeof fetch,
    loadOneLevelCatalog,
  );
  assert.equal(enabled.state, "ok");
  assert.deepEqual(enabled.data?.methods, []);
});

test("malformed, unauthorized and unreachable answers map to distinct codes", async () => {
  // دروازهٔ روشن با `ready` غیر‌بولیِ truthy ⇒ کاتالوگ «آماده» جا زده نمی‌شود.
  const notReady = await withFetch(
    (async () => json({ gate: "Enabled", ready: "true" })) as typeof fetch,
    loadOneLevelCatalog,
  );
  assert.equal(notReady.state, "error");
  assert.equal(notReady.message, "shipping_catalog.not_ready");

  const malformed = await withFetch(
    (async () => json("Enabled")) as typeof fetch,
    loadOneLevelCatalog,
  );
  assert.equal(malformed.state, "error");
  assert.equal(malformed.message, "shipping_catalog.invalid_response");

  const denied = await withFetch(
    (async () => json({}, 403)) as typeof fetch,
    loadOneLevelCatalog,
  );
  assert.equal(denied.state, "denied");

  const serverError = await withFetch(
    (async () => json({}, 500)) as typeof fetch,
    loadOneLevelCatalog,
  );
  assert.equal(serverError.state, "error");
  assert.equal(serverError.message, "shipping_catalog.load_failed");

  const unreachable = await withFetch(
    (async () => { throw new TypeError("fetch failed"); }) as typeof fetch,
    loadOneLevelCatalog,
  );
  assert.equal(unreachable.state, "error");
  assert.equal(unreachable.message, "host-unreachable");
});
