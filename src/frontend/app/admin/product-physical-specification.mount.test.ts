/**
 * Defect C — mount real product physical editor fields through save/read roundtrip.
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/product-physical-specification.mount.test.ts
 */
import "../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import {
  click,
  mount,
  q,
  setInputValue,
  waitFor,
} from "../../features/order-carrier-execution/test-dom-harness.ts";
import { PHYSICAL_SPEC_VALIDATION_FA } from "./product-physical-specification.ts";
import { ProductPhysicalSpecificationRoundtripFixture } from "./product-physical-specification-roundtrip-fixture.tsx";

const PRODUCT_ID = "prod-249r6-physical";

type Ledger = {
  patchCount: number;
  lastBody: Record<string, unknown> | null;
};

function installPhysicalFetch(store: {
  weightGrams: number | null;
  lengthCentimeters: number | null;
  widthCentimeters: number | null;
  heightCentimeters: number | null;
}): Ledger & { restore: () => void } {
  const ledger: Ledger = { patchCount: 0, lastBody: null };
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(typeof input === "string" || input instanceof URL ? input : input.url);
    const method = (init?.method ?? "GET").toUpperCase();
    if (url.includes(`/v1/admin/products/${PRODUCT_ID}/physical-specification`) && method === "PATCH") {
      ledger.patchCount += 1;
      const body = JSON.parse(String(init?.body ?? "{}")) as Record<string, unknown>;
      ledger.lastBody = body;
      store.weightGrams = (body.weightGrams as number | null) ?? null;
      store.lengthCentimeters = (body.lengthCentimeters as number | null) ?? null;
      store.widthCentimeters = (body.widthCentimeters as number | null) ?? null;
      store.heightCentimeters = (body.heightCentimeters as number | null) ?? null;
      return new Response(
        JSON.stringify({
          weightGrams: store.weightGrams,
          lengthCentimeters: store.lengthCentimeters,
          widthCentimeters: store.widthCentimeters,
          heightCentimeters: store.heightCentimeters,
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      );
    }
    return new Response(JSON.stringify({ errorCode: "not_found" }), { status: 404 });
  }) as typeof fetch;
  return {
    ...ledger,
    get patchCount() {
      return ledger.patchCount;
    },
    get lastBody() {
      return ledger.lastBody;
    },
    restore() {
      globalThis.fetch = original;
    },
  };
}

test("mount physical: g/cm labels visible and save/read roundtrip persists whole grams + cm decimals", async () => {
  const store = {
    weightGrams: 500 as number | null,
    lengthCentimeters: 12.5 as number | null,
    widthCentimeters: 8 as number | null,
    heightCentimeters: 3.25 as number | null,
  };
  const fx = installPhysicalFetch(store);
  const tree = await mount(
    createElement(ProductPhysicalSpecificationRoundtripFixture, {
      productId: PRODUCT_ID,
      initial: { ...store },
    }),
  );
  try {
    await waitFor(() => !!tree.container.querySelector('[data-testid="product-edit-physical-specification"]'));
    assert.match(tree.container.textContent ?? "", /وزن \(گرم\)/);
    assert.match(tree.container.textContent ?? "", /طول \(سانتی‌متر\)/);
    assert.match(tree.container.textContent ?? "", /عرض \(سانتی‌متر\)/);
    assert.match(tree.container.textContent ?? "", /ارتفاع \(سانتی‌متر\)/);

    const weight = q(tree.container, '[data-testid="product-edit-weight-grams"]') as HTMLInputElement;
    const length = q(tree.container, '[data-testid="product-edit-length-cm"]') as HTMLInputElement;
    assert.equal(weight.value, "500");
    assert.equal(length.value, "12.5");

    await setInputValue(weight, "750");
    await setInputValue(length, "20.5");
    await setInputValue(q(tree.container, '[data-testid="product-edit-width-cm"]'), "10");
    await setInputValue(q(tree.container, '[data-testid="product-edit-height-cm"]'), "4.5");
    await click(q(tree.container, '[data-testid="product-edit-physical-save"]'));

    await waitFor(() => fx.patchCount === 1);
    assert.deepEqual(fx.lastBody, {
      weightGrams: 750,
      lengthCentimeters: 20.5,
      widthCentimeters: 10,
      heightCentimeters: 4.5,
      expectedUpdatedAt: "2026-11-03T00:00:00.000Z",
    });
    await waitFor(() => {
      const echo = tree.container.querySelector('[data-testid="product-edit-physical-saved-echo"]');
      return (echo?.textContent ?? "").includes('"weightGrams":750');
    });
    // read roundtrip reloads draft from saved response
    assert.equal((q(tree.container, '[data-testid="product-edit-weight-grams"]') as HTMLInputElement).value, "750");
    assert.equal((q(tree.container, '[data-testid="product-edit-length-cm"]') as HTMLInputElement).value, "20.5");
  } finally {
    await tree.unmount();
    fx.restore();
  }
});

test("mount physical: nullable clearing posts nulls; fractional grams refused without PATCH", async () => {
  const store = {
    weightGrams: 500 as number | null,
    lengthCentimeters: 10 as number | null,
    widthCentimeters: 10 as number | null,
    heightCentimeters: 10 as number | null,
  };
  const fx = installPhysicalFetch(store);
  const tree = await mount(
    createElement(ProductPhysicalSpecificationRoundtripFixture, {
      productId: PRODUCT_ID,
      initial: { ...store },
    }),
  );
  try {
    await waitFor(() => !!tree.container.querySelector('[data-testid="product-edit-weight-grams"]'));

    await setInputValue(q(tree.container, '[data-testid="product-edit-weight-grams"]'), "");
    await setInputValue(q(tree.container, '[data-testid="product-edit-length-cm"]'), "");
    await setInputValue(q(tree.container, '[data-testid="product-edit-width-cm"]'), "");
    await setInputValue(q(tree.container, '[data-testid="product-edit-height-cm"]'), "");
    await click(q(tree.container, '[data-testid="product-edit-physical-save"]'));
    await waitFor(() => fx.patchCount === 1);
    assert.deepEqual(fx.lastBody?.weightGrams, null);
    assert.deepEqual(fx.lastBody?.lengthCentimeters, null);
    assert.deepEqual(fx.lastBody?.widthCentimeters, null);
    assert.deepEqual(fx.lastBody?.heightCentimeters, null);

    await setInputValue(q(tree.container, '[data-testid="product-edit-weight-grams"]'), "500.5");
    await click(q(tree.container, '[data-testid="product-edit-physical-save"]'));
    await waitFor(() => {
      const err = tree.container.querySelector('[data-testid="product-edit-physical-error"]');
      return (err?.textContent ?? "") === PHYSICAL_SPEC_VALIDATION_FA;
    });
    assert.equal(fx.patchCount, 1, "fractional grams must not PATCH");
  } finally {
    await tree.unmount();
    fx.restore();
  }
});
