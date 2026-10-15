/**
 * TB-SHIPPING-FULL-OPERATIONS-078 — focused guards for the read-only operational-quote **client** on
 * an in-memory fixture that implements the real 076 wire contract.
 *
 * Proven here without touching the shared Development database:
 * - `POST /v1/admin/shipping-services/{code}/operational-quote` request shape (route code is
 *   authoritative; the body never re-declares ownership);
 * - nullable monetary parts stay `null` and are never coerced to `0`;
 * - payer mode / schedule availability / capacity enforcement stay faithful to the backend;
 * - stable child error codes of a `validation.failed` envelope are surfaced;
 * - 401/403 map to `denied` and an unreachable host maps to a stable error.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  evaluateAdminShippingOperationalQuote,
  mapOperationalQuote,
  operationalQuoteErrorCode,
} from "./shipping-operational-quote-api.ts";
import { formatAmountFa, isQuotedResult, mapQuoteErrorFa, rawAmount } from "./shipping-operational-quote-model.ts";

type Recorded = { method: string; url: string; body: unknown; contentType: string | null };

const COUNTRY = "e3451206-e503-51a8-91ad-afa9ab70e330";
const AREA = "1f8050a8-c325-59fa-959f-82d76b5b2bc5";

/** A faithful `Quoted` body copied field-for-field from the real 076 Development answer. */
const QUOTED_BODY = {
  outcome: "Quoted",
  serviceCode: "tipax",
  optionCode: "express",
  currencyCode: "IRR",
  freightAmount: 260000.0,
  packagingAmount: 12000.0,
  totalServiceExpense: 272000.0,
  amountPayableAtCheckout: 12000.0,
  amountCollectOnDelivery: 260000.0,
  freightPayerMode: "CollectOnDelivery",
  packagingPayer: "Checkout",
  carrierFreight: {
    outcome: "Quoted",
    serviceCode: "tipax",
    chargeAmount: 260000.0,
    currencyCode: "IRR",
    preparationDays: 1,
    leadDays: 1,
    reasonCode: "shipping.quote.quoted",
    optionCode: "express",
    isQuoted: true,
  },
  packaging: {
    isCharged: true,
    feeAmount: 12000.0,
    freightPayerMode: "CollectOnDelivery",
    packagingPayer: "Checkout",
    reasonCode: "shipping.packaging.charged",
  },
  schedule: {
    availability: "Available",
    reasonCode: "shipping.schedule.available",
    timeZoneId: "Asia/Tehran",
    localWeekday: 7,
    localTime: "13:30:00",
    matchedWindowId: "11111111-1111-4111-8111-111111111111",
    capacityEnforcement: "NotEnforced",
    isConstraintSatisfied: true,
  },
  reasonCode: "shipping.operational_quote.quoted",
  isQuoted: true,
  isOperationallyEligible: true,
};

function createFixture() {
  const log: Recorded[] = [];
  let next: { status: number; payload: unknown } | null = null;

  function json(payload: unknown, status = 200): Response {
    return new Response(JSON.stringify(payload), { status, headers: { "Content-Type": "application/json" } });
  }

  return {
    log,
    install() {
      const original = globalThis.fetch;
      globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        log.push({
          method: (init?.method ?? "GET").toUpperCase(),
          url,
          body: init?.body ? JSON.parse(String(init.body)) : null,
          contentType: new Headers(init?.headers).get("Content-Type"),
        });
        if (next) {
          const failure = next;
          next = null;
          return json(failure.payload, failure.status);
        }
        if (!/\/operational-quote$/.test(url.split("?")[0]!)) return json({ errorCode: "unexpected" }, 404);
        return json(QUOTED_BODY);
      }) as typeof fetch;
      return () => { globalThis.fetch = original; };
    },
    respondNext(status: number, payload: unknown) {
      next = { status, payload };
    },
  };
}

test("evaluate posts the exact 076 body shape and maps the quoted answer faithfully", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    const result = await evaluateAdminShippingOperationalQuote("tipax", {
      scope: { countryId: COUNTRY, administrativeAreaId: AREA, cityId: null, serviceAreaId: null },
      cartSubtotalExclusive: 500000,
      currencyCode: "IRR",
      optionCode: "express",
      weightGrams: 1999,
      itemCount: 2,
      requestedPickupAtUtc: "2026-10-18T10:00:00.000Z",
    });
    assert.equal(result.state, "ok");
    const data = result.data!;
    assert.equal(data.outcome, "Quoted");
    assert.equal(data.freightAmount, 260000);
    assert.equal(data.packagingAmount, 12000);
    assert.equal(data.totalServiceExpense, 272000);
    assert.equal(data.amountPayableAtCheckout, 12000);
    assert.equal(data.amountCollectOnDelivery, 260000);
    assert.equal(data.freightPayerMode, "CollectOnDelivery");
    assert.equal(data.schedule?.availability, "Available");
    assert.equal(data.schedule?.localWeekday, 7);
    assert.equal(data.schedule?.capacityEnforcement, "NotEnforced");
    assert.equal(data.isOperationallyEligible, true);

    const write = api.log[0]!;
    assert.equal(write.method, "POST");
    assert.equal(write.url, "/v1/admin/shipping-services/tipax/operational-quote");
    assert.match(write.contentType ?? "", /application\/json/);
    assert.deepEqual(write.body, {
      scope: { countryId: COUNTRY, administrativeAreaId: AREA, cityId: null, serviceAreaId: null },
      cartSubtotalExclusive: 500000,
      currencyCode: "IRR",
      optionCode: "express",
      weightGrams: 1999,
      itemCount: 2,
      requestedPickupAtUtc: "2026-10-18T10:00:00.000Z",
    });
  } finally {
    restore();
  }
});

test("absent optional inputs are sent as explicit null, never as a guessed default", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    await evaluateAdminShippingOperationalQuote("tipax", {
      scope: { countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null },
      cartSubtotalExclusive: 0,
    });
    assert.deepEqual(api.log[0]?.body, {
      scope: { countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null },
      cartSubtotalExclusive: 0,
      currencyCode: null,
      optionCode: null,
      weightGrams: null,
      itemCount: null,
      requestedPickupAtUtc: null,
    });
  } finally {
    restore();
  }
});

test("a not-quoted answer keeps every monetary part null, never a fake zero", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    api.respondNext(200, {
      outcome: "FreightNotQuoted",
      serviceCode: "tipax",
      optionCode: "express",
      currencyCode: null,
      freightAmount: null,
      packagingAmount: null,
      totalServiceExpense: null,
      amountPayableAtCheckout: null,
      amountCollectOnDelivery: null,
      freightPayerMode: "Prepaid",
      packagingPayer: "Checkout",
      carrierFreight: {
        outcome: "NotCovered",
        serviceCode: "tipax",
        chargeAmount: 0,
        currencyCode: null,
        preparationDays: 1,
        leadDays: 1,
        reasonCode: "shipping.quote.not_covered",
        optionCode: "express",
        isQuoted: false,
      },
      packaging: {
        isCharged: false,
        feeAmount: 0,
        freightPayerMode: "Prepaid",
        packagingPayer: "Checkout",
        reasonCode: "shipping.packaging.not_configured",
      },
      schedule: {
        availability: "NotEvaluated",
        reasonCode: "shipping.schedule.not_evaluated",
        capacityEnforcement: "NotEnforced",
        isConstraintSatisfied: false,
      },
      reasonCode: "shipping.operational_quote.freight_not_quoted",
      isQuoted: false,
      isOperationallyEligible: false,
    });
    const result = await evaluateAdminShippingOperationalQuote("tipax", {
      scope: { countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null },
      cartSubtotalExclusive: 0,
    });
    assert.equal(result.state, "ok");
    const data = result.data!;
    assert.equal(data.freightAmount, null);
    assert.equal(data.totalServiceExpense, null);
    assert.equal(data.amountPayableAtCheckout, null);
    assert.equal(isQuotedResult(data), false);

    // The distinction that matters: «اعلام نشده» is never rendered as «۰».
    assert.equal(formatAmountFa(data.freightAmount, data.currencyCode), "اعلام نشده");
    assert.equal(rawAmount(data.totalServiceExpense), "اعلام نشده");
    assert.notEqual(formatAmountFa(data.freightAmount, data.currencyCode), formatAmountFa(0, "IRR"));
    // A real quoted zero stays zero.
    assert.notEqual(formatAmountFa(data.packaging?.feeAmount ?? null, "IRR"), "اعلام نشده");
  } finally {
    restore();
  }
});

test("a quoted zero packaging fee is a real amount and is never read as 'not declared'", () => {
  const mapped = mapOperationalQuote({
    ...QUOTED_BODY,
    packagingAmount: 0,
    amountPayableAtCheckout: 0,
    totalServiceExpense: 260000,
    amountCollectOnDelivery: 260000,
  });
  assert.equal(mapped?.packagingAmount, 0);
  assert.equal(formatAmountFa(mapped!.packagingAmount, "IRR").includes("IRR"), true);
  assert.notEqual(formatAmountFa(mapped!.packagingAmount, "IRR"), "اعلام نشده");
});

test("stable failure codes are surfaced, including the child code of a validation envelope", async () => {
  const api = createFixture();
  const restore = api.install();
  try {
    api.respondNext(400, {
      errorCode: "validation.failed",
      errors: { "Request.WeightGrams": ["shipping.validation.operational_weight_negative"] },
    });
    const rejected = await evaluateAdminShippingOperationalQuote("tipax", {
      scope: { countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null },
      cartSubtotalExclusive: 0,
      weightGrams: -1,
    });
    assert.equal(rejected.state, "error");
    assert.equal(rejected.status, 400);
    assert.equal(rejected.message, "shipping.validation.operational_weight_negative");
    assert.equal(mapQuoteErrorFa(rejected.message, "x"), "وزن نمی‌تواند منفی باشد.");

    api.respondNext(400, { errorCode: "shipping.service_option.unknown" });
    const unknownOption = await evaluateAdminShippingOperationalQuote("tipax", {
      scope: { countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null },
      cartSubtotalExclusive: 0,
      optionCode: "no-such-option",
    });
    assert.equal(unknownOption.message, "shipping.service_option.unknown");
    assert.notEqual(mapQuoteErrorFa(unknownOption.message, "x"), "x");
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
      const denied = await evaluateAdminShippingOperationalQuote("tipax", {
        scope: { countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null },
        cartSubtotalExclusive: 0,
      });
      assert.equal(denied.state, "denied");
      assert.equal(denied.status, status);
    }

    globalThis.fetch = (async () => { throw new TypeError("fetch failed"); }) as typeof fetch;
    const unreachable = await evaluateAdminShippingOperationalQuote("tipax", {
      scope: { countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null },
      cartSubtotalExclusive: 0,
    });
    assert.equal(unreachable.state, "error");
    assert.equal(unreachable.message, "host-unreachable");
    assert.equal(mapQuoteErrorFa(unreachable.message, "x"), "ارتباط با سرور برقرار نشد؛ دوباره تلاش کنید.");
  } finally {
    restore();
  }
});

test("error code extraction prefers the actionable child code over the validation envelope", () => {
  assert.equal(operationalQuoteErrorCode({ errorCode: "shipping.service_code.unknown" }), "shipping.service_code.unknown");
  assert.equal(
    operationalQuoteErrorCode({ errorCode: "validation.failed", errors: { Request: ["shipping.validation.currency_invalid"] } }),
    "shipping.validation.currency_invalid",
  );
  assert.equal(operationalQuoteErrorCode({ title: "boom" }), "boom");
  assert.equal(operationalQuoteErrorCode(null), "");
});

test("a body without the mandatory contract fields is refused as an invalid response", () => {
  assert.equal(mapOperationalQuote({ serviceCode: "tipax" }), null);
  assert.equal(mapOperationalQuote(null), null);
  assert.equal(mapOperationalQuote("not-an-object"), null);
});
