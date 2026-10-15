/**
 * TB-SHIPPING-FULL-OPERATIONS-074 — focused guards for the rates & coverage API client's handling of
 * the 073 tier condition: the persisted condition must be mapped (camel and Pascal casing), an
 * unconditioned row must stay `null`, and create/update must send the exact wire shape.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  createAdminShippingRate,
  loadAdminShippingRates,
  updateAdminShippingRate,
  type ShippingRatePolicyAdminItem,
} from "./shipping-rates-coverage-api.ts";

type Captured = { url: string; method: string; body: unknown };

function jsonResponse(payload: unknown, status = 200): Response {
  return new Response(JSON.stringify(payload), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function installFetch(payload: unknown, status = 200): { calls: Captured[]; restore: () => void } {
  const original = globalThis.fetch;
  const calls: Captured[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    calls.push({
      url: String(input),
      method: init?.method ?? "GET",
      body: init?.body ? JSON.parse(String(init.body)) : undefined,
    });
    return jsonResponse(payload, status);
  }) as typeof fetch;
  return { calls, restore: () => { globalThis.fetch = original; } };
}

test("a persisted condition is mapped from the camelCase admin payload", async () => {
  const fetchState = installFetch({
    serviceCode: "tipax",
    ratePolicies: [{
      ratePolicyId: "r1",
      serviceCode: "tipax",
      optionCode: "standard",
      countryId: "c1",
      administrativeAreaId: "a1",
      cityId: null,
      serviceAreaId: null,
      level: "AdministrativeArea",
      chargeAmount: 120000,
      currencyCode: "IRR",
      preparationDays: 1,
      leadDays: 2,
      freeAboveThreshold: null,
      isActive: true,
      condition: { weightGrams: { min: 0, max: 2000 } },
    }],
    coverageGrants: [],
  });
  try {
    const result = await loadAdminShippingRates("tipax");
    assert.equal(result.state, "ok");
    const row = result.data!.ratePolicies[0];
    assert.deepEqual(row.condition, { weightGrams: { min: 0, max: 2000 }, itemCount: null });
  } finally {
    fetchState.restore();
  }
});

test("an open upper bound is preserved as null, not coerced to zero", async () => {
  const fetchState = installFetch({
    serviceCode: "tipax",
    ratePolicies: [{
      ratePolicyId: "r1",
      serviceCode: "tipax",
      countryId: "c1",
      level: "Country",
      chargeAmount: 1,
      currencyCode: "IRR",
      preparationDays: 1,
      leadDays: 1,
      isActive: true,
      condition: { weightGrams: { min: 5000, max: null } },
    }],
    coverageGrants: [],
  });
  try {
    const result = await loadAdminShippingRates("tipax");
    assert.deepEqual(result.data!.ratePolicies[0].condition, {
      weightGrams: { min: 5000, max: null },
      itemCount: null,
    });
  } finally {
    fetchState.restore();
  }
});

test("a two-axis condition maps both axes", async () => {
  const fetchState = installFetch({
    serviceCode: "tipax",
    ratePolicies: [{
      ratePolicyId: "r1",
      serviceCode: "tipax",
      countryId: "c1",
      level: "Country",
      chargeAmount: 1,
      currencyCode: "IRR",
      preparationDays: 1,
      leadDays: 1,
      isActive: true,
      condition: { weightGrams: { min: 2000, max: 5000 }, itemCount: { min: 6, max: 11 } },
    }],
    coverageGrants: [],
  });
  try {
    const result = await loadAdminShippingRates("tipax");
    assert.deepEqual(result.data!.ratePolicies[0].condition, {
      weightGrams: { min: 2000, max: 5000 },
      itemCount: { min: 6, max: 11 },
    });
  } finally {
    fetchState.restore();
  }
});

test("PascalCase payloads are mapped too, so casing never changes the meaning", async () => {
  const fetchState = installFetch({
    ServiceCode: "tipax",
    RatePolicies: [{
      RatePolicyId: "r1",
      ServiceCode: "tipax",
      CountryId: "c1",
      Level: "Country",
      ChargeAmount: 1,
      CurrencyCode: "IRR",
      PreparationDays: 1,
      LeadDays: 1,
      IsActive: true,
      Condition: { WeightGrams: { Min: 0, Max: 2000 }, ItemCount: { Min: 1, Max: 6 } },
    }],
    CoverageGrants: [],
  });
  try {
    const result = await loadAdminShippingRates("tipax");
    assert.deepEqual(result.data!.ratePolicies[0].condition, {
      weightGrams: { min: 0, max: 2000 },
      itemCount: { min: 1, max: 6 },
    });
  } finally {
    fetchState.restore();
  }
});

test("an unconditioned row maps to null, never to a zero interval", async () => {
  const fetchState = installFetch({
    serviceCode: "tipax",
    ratePolicies: [
      { ratePolicyId: "r1", serviceCode: "tipax", countryId: "c1", level: "Country", chargeAmount: 1, currencyCode: "IRR", preparationDays: 1, leadDays: 1, isActive: true },
      { ratePolicyId: "r2", serviceCode: "tipax", countryId: "c1", level: "Country", chargeAmount: 1, currencyCode: "IRR", preparationDays: 1, leadDays: 1, isActive: true, condition: null },
      { ratePolicyId: "r3", serviceCode: "tipax", countryId: "c1", level: "Country", chargeAmount: 1, currencyCode: "IRR", preparationDays: 1, leadDays: 1, isActive: true, condition: {} },
    ],
    coverageGrants: [],
  });
  try {
    const result = await loadAdminShippingRates("tipax");
    for (const row of result.data!.ratePolicies) {
      assert.equal(row.condition, null);
    }
  } finally {
    fetchState.restore();
  }
});

test("create sends the exact condition wire shape under the route service code", async () => {
  const created: ShippingRatePolicyAdminItem = {
    ratePolicyId: "r9",
    serviceCode: "tipax",
    optionCode: "standard",
    countryId: "c1",
    administrativeAreaId: "a1",
    cityId: null,
    serviceAreaId: null,
    level: "AdministrativeArea",
    chargeAmount: 120000,
    currencyCode: "IRR",
    preparationDays: 1,
    leadDays: 2,
    freeAboveThreshold: null,
    isActive: true,
    condition: { weightGrams: { min: 0, max: 2000 }, itemCount: null },
  };
  const fetchState = installFetch(created);
  try {
    await createAdminShippingRate("tipax", {
      scope: { countryId: "c1", administrativeAreaId: "a1" },
      optionCode: "standard",
      chargeAmount: 120000,
      currencyCode: "IRR",
      preparationDays: 1,
      leadDays: 2,
      freeAboveThreshold: null,
      condition: { weightGrams: { min: 0, max: 2000 } },
    });
    const call = fetchState.calls[0];
    assert.equal(call.method, "POST");
    assert.equal(call.url, "/v1/admin/shipping-services/tipax/rates");
    assert.deepEqual(call.body, {
      serviceCode: "tipax",
      optionCode: "standard",
      scope: { countryId: "c1", administrativeAreaId: "a1" },
      chargeAmount: 120000,
      currencyCode: "IRR",
      preparationDays: 1,
      leadDays: 2,
      freeAboveThreshold: null,
      condition: { weightGrams: { min: 0, max: 2000 } },
    });
  } finally {
    fetchState.restore();
  }
});

test("create sends condition null for an unconditioned rule", async () => {
  const fetchState = installFetch({
    ratePolicyId: "r9",
    serviceCode: "tipax",
    countryId: "c1",
    level: "Country",
    chargeAmount: 1,
    currencyCode: "IRR",
    preparationDays: 1,
    leadDays: 1,
    isActive: true,
    condition: null,
  });
  try {
    await createAdminShippingRate("tipax", {
      scope: { countryId: "c1" },
      chargeAmount: 1,
      currencyCode: "IRR",
      preparationDays: 1,
      leadDays: 1,
    });
    assert.deepEqual((fetchState.calls[0].body as { condition: unknown }).condition, null);
  } finally {
    fetchState.restore();
  }
});

test("update sends the exact condition wire shape for the addressed rate policy", async () => {
  const fetchState = installFetch({
    ratePolicyId: "r1",
    serviceCode: "tipax",
    countryId: "c1",
    level: "Country",
    chargeAmount: 2,
    currencyCode: "IRR",
    preparationDays: 1,
    leadDays: 1,
    isActive: true,
    condition: { itemCount: { min: 1, max: 6 } },
  });
  try {
    await updateAdminShippingRate("tipax", "r1", {
      scope: { countryId: "c1" },
      optionCode: null,
      chargeAmount: 2,
      currencyCode: "IRR",
      preparationDays: 1,
      leadDays: 1,
      condition: { itemCount: { min: 1, max: 6 } },
    });
    const call = fetchState.calls[0];
    assert.equal(call.method, "PUT");
    assert.equal(call.url, "/v1/admin/shipping-services/tipax/rates/r1");
    assert.deepEqual((call.body as { condition: unknown }).condition, { itemCount: { min: 1, max: 6 } });
  } finally {
    fetchState.restore();
  }
});

test("a stable tier-overlap refusal surfaces the backend error code", async () => {
  const fetchState = installFetch({ errorCode: "shipping.rate_policy.tier_overlap" }, 400);
  try {
    const result = await createAdminShippingRate("tipax", {
      scope: { countryId: "c1" },
      chargeAmount: 1,
      currencyCode: "IRR",
      preparationDays: 1,
      leadDays: 1,
      condition: { weightGrams: { min: 1500, max: 3000 } },
    });
    assert.equal(result.state, "error");
    assert.equal(result.status, 400);
    assert.equal(result.message, "shipping.rate_policy.tier_overlap");
  } finally {
    fetchState.restore();
  }
});
