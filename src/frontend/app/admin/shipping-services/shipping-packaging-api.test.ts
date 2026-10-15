/**
 * TB-SHIPPING-FULL-OPERATIONS-077 — focused guards for the admin packaging/payer client and its pure
 * model, on an in-memory fixture that implements the real 075 wire contract.
 *
 * Proven here without touching the shared Development database:
 * - `GET /v1/admin/shipping-services/{code}/packaging-policies` read shape;
 * - `POST` the same route with `{serviceCode, optionCode, scope, feeAmount, currencyCode, freightPayerMode}`;
 * - `PUT /{id}` keeping the persisted scope/option identity;
 * - `PATCH /{id}/active` with `{isActive}`;
 * - stable child error codes of a `validation.failed` envelope are surfaced;
 * - money parsing refuses silent rounding and the payer-mode wording stays a stable contract.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  createAdminShippingPackagingPolicy,
  loadAdminShippingPackagingPolicies,
  packagingErrorCode,
  setAdminShippingPackagingPolicyActive,
  updateAdminShippingPackagingPolicy,
} from "./shipping-packaging-api.ts";
import {
  emptyPackagingForm,
  formatPackagingAmountInput,
  formatPackagingFee,
  mapPackagingErrorFa,
  packagingFormFromPolicy,
  packagingFormToPayload,
  packagingOptionKey,
  packagingOptionLabelFa,
  packagingStateFa,
  parsePackagingFee,
  payerModeLabelFa,
  summarizePackagingPolicies,
  validatePackagingForm,
  validatePackagingOption,
} from "./shipping-packaging-model.ts";
import { buildScopeWrite, EMPTY_DESTINATION_SELECTION } from "./shipping-rates-coverage-model.ts";

type Recorded = { method: string; url: string; body: unknown; contentType: string | null };

type PolicyDto = {
  packagingPolicyId: string;
  serviceCode: string;
  countryId: string;
  administrativeAreaId: string | null;
  cityId: string | null;
  serviceAreaId: string | null;
  level: string;
  feeAmount: number;
  currencyCode: string;
  freightPayerMode: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  optionCode: string | null;
};

/** Fixture mirrors the backend wire contract exactly: camelCase, string payer mode, decimal fee. */
function createFixture() {
  const policies = new Map<string, PolicyDto>();
  const log: Recorded[] = [];
  let counter = 0;
  let failNext: { status: number; payload: unknown } | null = null;

  function json(payload: unknown, status = 200): Response {
    return new Response(JSON.stringify(payload), { status, headers: { "Content-Type": "application/json" } });
  }

  function keyOf(optionCode: string | null, scope: { countryId: string; administrativeAreaId?: string | null }): string {
    return `${optionCode ?? ""}|${scope.countryId}|${scope.administrativeAreaId ?? ""}`;
  }

  async function handle(url: string, method: string, init?: RequestInit): Promise<Response> {
    const body = init?.body ? JSON.parse(String(init.body)) : null;
    log.push({ method, url, body, contentType: new Headers(init?.headers).get("Content-Type") });
    const path = url.split("?")[0];

    if (failNext) {
      const failure = failNext;
      failNext = null;
      return json(failure.payload, failure.status);
    }

    const activeMatch = /\/packaging-policies\/([^/]+)\/active$/.exec(path);
    if (activeMatch) {
      const found = policies.get(activeMatch[1]!);
      if (!found) return json({ errorCode: "shipping.packaging.policy.not_found" }, 400);
      found.isActive = Boolean((body as { isActive: boolean }).isActive);
      return json(found);
    }

    const updateMatch = /\/packaging-policies\/([^/]+)$/.exec(path);
    if (updateMatch && method === "PUT" && body) {
      const found = policies.get(updateMatch[1]!);
      if (!found) return json({ errorCode: "shipping.packaging.policy.not_found" }, 400);
      const request = body as {
        serviceCode: string;
        optionCode: string | null;
        scope: { countryId: string; administrativeAreaId?: string | null };
        feeAmount: number;
        currencyCode: string;
        freightPayerMode: string | null;
      };
      // Identity is immutable through this seam: the backend rejects a moved option.
      if ((request.optionCode ?? null) !== found.optionCode) {
        return json({ errorCode: "shipping.packaging.policy.option_mismatch" }, 400);
      }
      found.feeAmount = request.feeAmount;
      found.currencyCode = request.currencyCode;
      found.freightPayerMode = request.freightPayerMode ?? "Prepaid";
      return json(found);
    }

    const listMatch = /\/shipping-services\/([^/]+)\/packaging-policies$/.exec(path);
    if (listMatch) {
      const serviceCode = decodeURIComponent(listMatch[1]!);
      if (method === "GET") {
        return json({
          serviceCode,
          packagingPolicies: [...policies.values()].filter((p) => p.serviceCode === serviceCode),
        });
      }
      if (method === "POST" && body) {
        const request = body as {
          serviceCode: string;
          optionCode: string | null;
          scope: { countryId: string; administrativeAreaId?: string | null };
          feeAmount: number;
          currencyCode: string;
          freightPayerMode: string | null;
        };
        const key = keyOf(request.optionCode, request.scope);
        const duplicate = [...policies.values()].some(
          (p) => p.serviceCode === serviceCode && keyOf(p.optionCode, { countryId: p.countryId, administrativeAreaId: p.administrativeAreaId }) === key,
        );
        if (duplicate) return json({ errorCode: "shipping.packaging.policy.scope_duplicate" }, 400);
        counter += 1;
        const created: PolicyDto = {
          packagingPolicyId: `01a13f00-0000-7000-9aaa-00000000000${counter}`,
          serviceCode,
          countryId: request.scope.countryId,
          administrativeAreaId: request.scope.administrativeAreaId ?? null,
          cityId: null,
          serviceAreaId: null,
          level: request.scope.administrativeAreaId ? "AdministrativeArea" : "Country",
          feeAmount: request.feeAmount,
          currencyCode: request.currencyCode,
          freightPayerMode: request.freightPayerMode ?? "Prepaid",
          isActive: true,
          createdAt: "2026-10-15T08:00:00+00:00",
          updatedAt: "2026-10-15T08:00:00+00:00",
          optionCode: request.optionCode ?? null,
        };
        policies.set(created.packagingPolicyId, created);
        return json(created);
      }
    }

    return json({ errorCode: "unexpected" }, 404);
  }

  return {
    log,
    policies,
    install() {
      const original = globalThis.fetch;
      globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        return handle(url, (init?.method ?? "GET").toUpperCase(), init);
      }) as typeof fetch;
      return () => { globalThis.fetch = original; };
    },
    failNextRequest(status: number, payload: unknown) {
      failNext = { status, payload };
    },
  };
}

const COUNTRY = "e3451206-e503-51a8-91ad-afa9ab70e330";
const AREA = "1f8050a8-c325-59fa-959f-82d76b5b2bc5";

test("read an empty packaging set: a real empty answer meaning a zero fee, not a hidden charge", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const result = await loadAdminShippingPackagingPolicies("tipax");
    assert.equal(result.state, "ok");
    assert.deepEqual(result.data, { serviceCode: "tipax", packagingPolicies: [] });
    assert.equal(api.log[0]?.method, "GET");
    assert.equal(api.log[0]?.url, "/v1/admin/shipping-services/tipax/packaging-policies");
    assert.equal(packagingStateFa(result.data!.packagingPolicies), packagingStateFa([]));
  } finally {
    restore();
  }
});

test("create sends the exact 075 wire shape and reads back the persisted row", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const created = await createAdminShippingPackagingPolicy("tipax", {
      scope: { countryId: COUNTRY, administrativeAreaId: AREA },
      optionCode: "express",
      feeAmount: 12000,
      currencyCode: "IRR",
      freightPayerMode: "CollectOnDelivery",
    });
    assert.equal(created.state, "ok");
    assert.equal(created.data?.feeAmount, 12000);
    assert.equal(created.data?.freightPayerMode, "CollectOnDelivery");
    assert.equal(created.data?.optionCode, "express");

    const write = api.log[0]!;
    assert.equal(write.method, "POST");
    assert.equal(write.url, "/v1/admin/shipping-services/tipax/packaging-policies");
    assert.match(write.contentType ?? "", /application\/json/);
    assert.deepEqual(write.body, {
      serviceCode: "tipax",
      optionCode: "express",
      scope: { countryId: COUNTRY, administrativeAreaId: AREA },
      feeAmount: 12000,
      currencyCode: "IRR",
      freightPayerMode: "CollectOnDelivery",
    });

    const readBack = await loadAdminShippingPackagingPolicies("tipax");
    assert.equal(readBack.data?.packagingPolicies.length, 1);
    assert.equal(readBack.data?.packagingPolicies[0]?.feeAmount, 12000);
  } finally {
    restore();
  }
});

test("update changes money and payer mode while keeping the persisted option identity", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const created = await createAdminShippingPackagingPolicy("tipax", {
      scope: { countryId: COUNTRY, administrativeAreaId: AREA },
      optionCode: "standard",
      feeAmount: 12000,
      currencyCode: "IRR",
      freightPayerMode: "CollectOnDelivery",
    });
    const id = created.data!.packagingPolicyId;

    const updated = await updateAdminShippingPackagingPolicy("tipax", id, {
      scope: {
        countryId: created.data!.countryId,
        administrativeAreaId: created.data!.administrativeAreaId,
        cityId: created.data!.cityId,
        serviceAreaId: created.data!.serviceAreaId,
      },
      optionCode: created.data!.optionCode,
      feeAmount: 15000,
      currencyCode: "IRR",
      freightPayerMode: "Prepaid",
    });
    assert.equal(updated.state, "ok");
    assert.equal(updated.data?.feeAmount, 15000);
    assert.equal(updated.data?.freightPayerMode, "Prepaid");
    assert.equal(updated.data?.optionCode, "standard");
    assert.equal(api.log.at(-1)?.method, "PUT");
    assert.equal(api.log.at(-1)?.url, `/v1/admin/shipping-services/tipax/packaging-policies/${id}`);
  } finally {
    restore();
  }
});

test("active toggle persists through PATCH {id}/active and a re-read", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const created = await createAdminShippingPackagingPolicy("tipax", {
      scope: { countryId: COUNTRY },
      feeAmount: 0,
      currencyCode: "IRR",
    });
    const id = created.data!.packagingPolicyId;
    assert.equal(created.data?.isActive, true);

    const off = await setAdminShippingPackagingPolicyActive("tipax", id, false);
    assert.equal(off.state, "ok");
    assert.equal(off.data?.isActive, false);
    const patch = api.log.at(-1)!;
    assert.equal(patch.method, "PATCH");
    assert.equal(patch.url, `/v1/admin/shipping-services/tipax/packaging-policies/${id}/active`);
    assert.deepEqual(patch.body, { isActive: false });

    const view = await loadAdminShippingPackagingPolicies("tipax");
    assert.equal(view.data?.packagingPolicies[0]?.isActive, false);
  } finally {
    restore();
  }
});

test("a zero fee is a real charged policy, never read as 'not configured'", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const created = await createAdminShippingPackagingPolicy("tipax", {
      scope: { countryId: COUNTRY },
      feeAmount: 0,
      currencyCode: "IRR",
    });
    assert.equal(created.data?.feeAmount, 0);
    const summary = summarizePackagingPolicies([created.data!]);
    assert.equal(summary.policyCount, 1);
    assert.equal(summary.activeCount, 1);
  } finally {
    restore();
  }
});

test("stable failure codes are surfaced, including the child code of a validation envelope", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    api.failNextRequest(400, {
      errorCode: "validation.failed",
      errors: { "Request.FeeAmount": ["shipping.validation.packaging_fee_negative"] },
    });
    const rejected = await createAdminShippingPackagingPolicy("tipax", {
      scope: { countryId: COUNTRY },
      feeAmount: -1,
      currencyCode: "IRR",
    });
    assert.equal(rejected.state, "error");
    assert.equal(rejected.status, 400);
    assert.equal(rejected.message, "shipping.validation.packaging_fee_negative");
    assert.equal(mapPackagingErrorFa(rejected.message, "x"), "هزینهٔ بسته‌بندی نمی‌تواند منفی باشد.");

    api.failNextRequest(400, { errorCode: "shipping.packaging.policy.scope_duplicate" });
    const duplicate = await createAdminShippingPackagingPolicy("tipax", {
      scope: { countryId: COUNTRY },
      feeAmount: 1000,
      currencyCode: "IRR",
    });
    assert.equal(duplicate.message, "shipping.packaging.policy.scope_duplicate");
    assert.notEqual(mapPackagingErrorFa(duplicate.message, "x"), "x");
  } finally {
    restore();
  }
});

test("401/403 map to denied and an unreachable host maps to a stable error", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    for (const status of [401, 403]) {
      globalThis.fetch = (async () => new Response(null, { status })) as typeof fetch;
      assert.equal((await loadAdminShippingPackagingPolicies("tipax")).state, "denied");
      assert.equal((await createAdminShippingPackagingPolicy("tipax", {
        scope: { countryId: COUNTRY },
        feeAmount: 0,
        currencyCode: "IRR",
      })).state, "denied");
      assert.equal((await setAdminShippingPackagingPolicyActive("tipax", "x", false)).state, "denied");
    }

    globalThis.fetch = (async () => { throw new TypeError("fetch failed"); }) as typeof fetch;
    const unreachable = await loadAdminShippingPackagingPolicies("tipax");
    assert.equal(unreachable.state, "error");
    assert.equal(unreachable.message, "host-unreachable");
  } finally {
    restore();
  }
});

test("packagingErrorCode prefers the actionable child code over the validation envelope", () => {
  assert.equal(packagingErrorCode({ errorCode: "shipping.packaging.policy.not_found" }), "shipping.packaging.policy.not_found");
  assert.equal(
    packagingErrorCode({ errorCode: "validation.failed", errors: { Request: ["shipping.validation.packaging_currency_invalid"] } }),
    "shipping.validation.packaging_currency_invalid",
  );
  assert.equal(packagingErrorCode({ title: "boom" }), "boom");
  assert.equal(packagingErrorCode(null), "");
});

test("money parsing refuses silent rounding and keeps exact two-decimal values", () => {
  assert.equal(parsePackagingFee("12000"), 12000);
  assert.equal(parsePackagingFee("12000.50"), 12000.5);
  assert.equal(parsePackagingFee("0"), 0);
  assert.equal(parsePackagingFee(""), null);
  assert.equal(parsePackagingFee("-1"), null);
  assert.equal(parsePackagingFee("1.005"), null, "پیش از گرد کردن خاموش باید رد شود");
  assert.equal(parsePackagingFee("1e3"), null);
  assert.equal(parsePackagingFee("۱۲۰۰۰"), null, "رقم فارسی روی سیم پذیرفته نمی‌شود");

  assert.equal(formatPackagingAmountInput(12000), "12000");
  assert.equal(formatPackagingAmountInput(12000.5), "12000.5");
  assert.equal(formatPackagingAmountInput(0), "0");
});

test("form validation is field-adjacent and the payload is canonical", () => {
  assert.deepEqual(validatePackagingForm({ feeAmount: "12000", currencyCode: "IRR", payerMode: "Prepaid" }), []);
  const bad = validatePackagingForm({ feeAmount: "-5", currencyCode: "ir", payerMode: "Prepaid" });
  assert.equal(bad.length, 2);
  assert.deepEqual(bad.map((i) => i.field), ["feeAmount", "currencyCode"]);

  assert.deepEqual(packagingFormToPayload({ feeAmount: "12000.5", currencyCode: "irr", payerMode: "CollectOnDelivery" }), {
    feeAmount: 12000.5,
    currencyCode: "IRR",
    freightPayerMode: "CollectOnDelivery",
  });
});

test("payer-mode wording is a stable contract and the form round-trips a persisted policy", () => {
  assert.equal(payerModeLabelFa("Prepaid"), "پیش‌کرایه");
  assert.equal(payerModeLabelFa("CollectOnDelivery"), "پس‌کرایه");

  const restored = packagingFormFromPolicy({
    packagingPolicyId: "p1",
    serviceCode: "tipax",
    countryId: COUNTRY,
    administrativeAreaId: AREA,
    cityId: null,
    serviceAreaId: null,
    level: "AdministrativeArea",
    feeAmount: 15000,
    currencyCode: "IRR",
    freightPayerMode: "CollectOnDelivery",
    isActive: false,
    createdAt: "",
    updatedAt: "",
    optionCode: "standard",
  });
  assert.deepEqual(restored, { feeAmount: "15000", currencyCode: "IRR", payerMode: "CollectOnDelivery" });
  assert.deepEqual(packagingFormToPayload(restored), {
    feeAmount: 15000,
    currencyCode: "IRR",
    freightPayerMode: "CollectOnDelivery",
  });

  assert.deepEqual(emptyPackagingForm(), { feeAmount: "", currencyCode: "IRR", payerMode: "Prepaid" });
  assert.equal(formatPackagingFee(12000, "IRR").endsWith("IRR"), true);
});

test("option identity is validated before any API call and labels never invent a service", () => {
  const options = [
    { code: "standard", labelFa: "معمولی" },
    { code: "express", labelFa: "پیشتاز" },
  ];
  assert.deepEqual(validatePackagingOption(null, options), []);
  assert.deepEqual(validatePackagingOption("standard", options), []);
  assert.equal(validatePackagingOption("nope", options).length, 1);
  assert.equal(validatePackagingOption("standard", []).length, 1);

  assert.equal(packagingOptionLabelFa(null, options), "سطح خدمت (پیش‌فرض خدمت)");
  assert.equal(packagingOptionLabelFa("standard", options), "معمولی");
  assert.equal(packagingOptionLabelFa("unknown", options), "unknown");
  assert.equal(packagingOptionKey(null), "");
  assert.equal(packagingOptionKey("standard"), "standard");
});

test("the destination scope builder never guesses a deeper level than the user chose", () => {
  assert.deepEqual(buildScopeWrite(EMPTY_DESTINATION_SELECTION), {
    countryId: "",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
  });
  assert.deepEqual(
    buildScopeWrite({ countryId: COUNTRY, administrativeAreaId: AREA, cityId: null, serviceAreaId: null }),
    { countryId: COUNTRY, administrativeAreaId: AREA, cityId: null, serviceAreaId: null },
  );
});
