/**
 * TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173 — focused guards for the per-method geographic coverage
 * mode in the existing «تعرفه و پوشش» tab.
 *
 * What is asserted, without a running server:
 *   - an absent declaration is the documented Unrestricted default, not a coverage promise;
 *   - Restricted/Unrestricted is read per method identity (service + option) and never from a
 *     sibling option or from a service-level row;
 *   - an unreadable declaration is untrusted and blocks writing instead of widening a restriction;
 *   - the write goes to the existing Shipping-owned declaration endpoint with the server binding's
 *     option, and option-scoped grants carry that same option (never a provider-level approximation);
 *   - read-only and in-flight states cannot write.
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  createAdminShippingCoverageGrant,
  loadAdminShippingRates,
  setAdminShippingMethodCoverage,
  type ShippingCoverageGrantAdminItem,
  type ShippingMethodCoverageAdminItem,
  type ShippingRatesAndCoverageView,
} from "./shipping-rates-coverage-api.ts";
import {
  COVERAGE_GRANTS_KEPT_FA,
  COVERAGE_MODE_BOUNDARY_FA,
  coverageModeFa,
  coverageModeWritable,
  grantBelongsToBinding,
  resolveCoverageMode,
  RESTRICTED_EMPTY_NOTE_FA,
  restrictedNeedsDestination,
  scopeRatesToBinding,
  type OneLevelMethodBinding,
} from "./shipping-one-level-rates-model.ts";

const TIPAX_STANDARD: OneLevelMethodBinding = {
  methodCode: "tipax:standard",
  serviceCode: "tipax",
  optionCode: "standard",
  isActive: true,
};

const TIPAX_EXPRESS: OneLevelMethodBinding = {
  methodCode: "tipax:express",
  serviceCode: "tipax",
  optionCode: "express",
  isActive: true,
};

function declaration(partial: Partial<ShippingMethodCoverageAdminItem>): ShippingMethodCoverageAdminItem {
  return {
    methodCoverageId: "mc1",
    serviceCode: "tipax",
    optionCode: "standard",
    mode: "Restricted",
    ...partial,
  };
}

function view(partial: Partial<ShippingRatesAndCoverageView>): ShippingRatesAndCoverageView {
  return {
    serviceCode: "tipax",
    ratePolicies: [],
    coverageGrants: [],
    methodCoverages: [],
    ...partial,
  };
}

type Captured = { url: string; method: string; body: unknown };

function installFetch(payload: unknown, status = 200): { calls: Captured[]; restore: () => void } {
  const original = globalThis.fetch;
  const calls: Captured[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    calls.push({
      url: String(input),
      method: init?.method ?? "GET",
      body: init?.body ? JSON.parse(String(init.body)) : undefined,
    });
    return new Response(JSON.stringify(payload), {
      status,
      headers: { "Content-Type": "application/json" },
    });
  }) as typeof fetch;
  return { calls, restore: () => { globalThis.fetch = original; } };
}

test("no declaration row is the Unrestricted default, not a claim about carrier coverage", () => {
  const state = resolveCoverageMode(view({ methodCoverages: [] }), TIPAX_STANDARD);
  assert.deepEqual(state, { state: "undeclared" });

  const text = coverageModeFa(state);
  assert.match(text, /بدون محدودیت مدیریتی/);
  assert.match(text, /پیش‌فرض/);
  // «بدون محدودیت مدیریتی» هرگز نباید ادعای آمادگی مالی یا پوشش حامل باشد.
  assert.doesNotMatch(text, /رایگان|آمادهٔ فروش|تضمین/);
  assert.match(COVERAGE_MODE_BOUNDARY_FA, /نه فعال بودن روش/);
});

test("the declaration is read per method identity, never from a sibling option", () => {
  const declared = view({ methodCoverages: [declaration({ optionCode: "express", mode: "Restricted" })] });
  // اعلام گزینهٔ هم‌خانواده به روش standard نسبت داده نمی‌شود.
  assert.deepEqual(resolveCoverageMode(declared, TIPAX_STANDARD), { state: "undeclared" });
  assert.deepEqual(resolveCoverageMode(declared, TIPAX_EXPRESS), {
    state: "declared",
    mode: "Restricted",
    coverage: declaration({ optionCode: "express", mode: "Restricted" }),
  });

  // روش بدون گزینه فقط اعلام سطح سرویس خودش را می‌خواند.
  const optionless: OneLevelMethodBinding = {
    methodCode: "store_courier",
    serviceCode: "store_courier",
    optionCode: null,
    isActive: true,
  };
  assert.deepEqual(
    resolveCoverageMode(
      view({ serviceCode: "store_courier", methodCoverages: [declaration({ serviceCode: "store_courier", optionCode: null, mode: "Unrestricted" })] }),
      optionless,
    ),
    {
      state: "declared",
      mode: "Unrestricted",
      coverage: declaration({ serviceCode: "store_courier", optionCode: null, mode: "Unrestricted" }),
    },
  );
});

test("a wrong-service or unreadable declaration is untrusted and cannot be written", () => {
  assert.deepEqual(resolveCoverageMode(view({ serviceCode: "post" }), TIPAX_STANDARD), { state: "untrusted" });
  assert.deepEqual(resolveCoverageMode(null, TIPAX_STANDARD), { state: "untrusted" });
  assert.deepEqual(resolveCoverageMode(view({ methodCoveragesUntrusted: true }), TIPAX_STANDARD), { state: "untrusted" });

  const untrusted = resolveCoverageMode(view({ methodCoveragesUntrusted: true }), TIPAX_STANDARD);
  assert.equal(coverageModeWritable({ readOnly: false, busy: false, modeState: untrusted }), false);
  assert.match(coverageModeFa(untrusted), /قابل‌اعتماد خوانده نشد/);
  // هرگز به «بدون محدودیت» تبدیل نمی‌شود.
  assert.doesNotMatch(coverageModeFa(untrusted), /بدون محدودیت مدیریتی/);
});

test("read-only and in-flight states cannot write; a readable declaration can", () => {
  const declared = resolveCoverageMode(view({ methodCoverages: [declaration({})] }), TIPAX_STANDARD);
  assert.equal(coverageModeWritable({ readOnly: false, busy: false, modeState: declared }), true);
  assert.equal(coverageModeWritable({ readOnly: true, busy: false, modeState: declared }), false);
  assert.equal(coverageModeWritable({ readOnly: false, busy: true, modeState: declared }), false);
  assert.equal(coverageModeWritable({ readOnly: false, busy: false, modeState: { state: "undeclared" } }), true);
});

test("Restricted with no active grant requires an explicit destination; Unrestricted never does", () => {
  assert.equal(
    restrictedNeedsDestination({ targetMode: "Restricted", activeOwnGrantCount: 0, hasDestination: false }),
    true,
  );
  assert.equal(
    restrictedNeedsDestination({ targetMode: "Restricted", activeOwnGrantCount: 0, hasDestination: true }),
    false,
  );
  assert.equal(
    restrictedNeedsDestination({ targetMode: "Restricted", activeOwnGrantCount: 2, hasDestination: false }),
    false,
  );
  assert.equal(
    restrictedNeedsDestination({ targetMode: "Unrestricted", activeOwnGrantCount: 0, hasDestination: false }),
    false,
  );
  assert.match(RESTRICTED_EMPTY_NOTE_FA, /هیچ مقصدی/);
  assert.match(COVERAGE_GRANTS_KEPT_FA, /حذف نمی‌کند/);
});

test("grants are method-owned: sibling and service-level rows are never inherited", () => {
  const own: ShippingCoverageGrantAdminItem = {
    coverageGrantId: "g-standard",
    serviceCode: "tipax",
    optionCode: "standard",
    countryId: "c1",
    administrativeAreaId: null,
    cityId: null,
    serviceAreaId: null,
    level: "country",
    isActive: true,
  };
  const sibling = { ...own, coverageGrantId: "g-express", optionCode: "express" };
  const serviceLevel = { ...own, coverageGrantId: "g-service", optionCode: null };

  assert.equal(grantBelongsToBinding(own, TIPAX_STANDARD), true);
  assert.equal(grantBelongsToBinding(sibling, TIPAX_STANDARD), false);
  assert.equal(grantBelongsToBinding(serviceLevel, TIPAX_STANDARD), false);

  const scoped = scopeRatesToBinding(view({ coverageGrants: [own, sibling, serviceLevel] }), TIPAX_STANDARD);
  assert.deepEqual(scoped.grants.map((g) => g.coverageGrantId), ["g-standard"]);
});

test("the declaration write uses the Shipping-owned route and the binding's own option", async () => {
  const fetchState = installFetch({
    methodCoverageId: "mc9",
    serviceCode: "tipax",
    optionCode: "standard",
    mode: "Restricted",
  });
  try {
    const result = await setAdminShippingMethodCoverage("tipax", { optionCode: "standard", mode: "Restricted" });
    assert.equal(result.state, "ok");
    assert.equal(result.data!.mode, "Restricted");
    const call = fetchState.calls[0];
    assert.equal(call.method, "PUT");
    assert.equal(call.url, "/v1/admin/shipping-services/tipax/method-coverage");
    assert.deepEqual(call.body, { optionCode: "standard", mode: "Restricted" });
  } finally {
    fetchState.restore();
  }
});

test("an unreadable or denied declaration response is never read as a success", async () => {
  const invalid = installFetch({ methodCoverageId: "mc9", serviceCode: "tipax", optionCode: "standard", mode: "All" });
  try {
    const result = await setAdminShippingMethodCoverage("tipax", { optionCode: "standard", mode: "Restricted" });
    assert.equal(result.state, "error");
    assert.match(result.message ?? "", /invalid_response/);
  } finally {
    invalid.restore();
  }

  const denied = installFetch({ errorCode: "admin.authorization.denied" }, 403);
  try {
    const result = await setAdminShippingMethodCoverage("tipax", { optionCode: "standard", mode: "Restricted" });
    assert.equal(result.state, "denied");
    assert.equal(result.status, 403);
  } finally {
    denied.restore();
  }
});

test("the grant writer sends the method's option, never a provider-level approximation", async () => {
  const fetchState = installFetch({
    coverageGrantId: "g9",
    serviceCode: "tipax",
    optionCode: "standard",
    countryId: "c1",
    level: "country",
    isActive: true,
  });
  try {
    await createAdminShippingCoverageGrant("tipax", {
      optionCode: "standard",
      scope: { countryId: "c1" },
    });
    assert.deepEqual(fetchState.calls[0].body, {
      serviceCode: "tipax",
      optionCode: "standard",
      scope: { countryId: "c1" },
    });
  } finally {
    fetchState.restore();
  }
});

test("the rate view maps declarations and flags any row it could not read", async () => {
  const readable = installFetch({
    serviceCode: "tipax",
    ratePolicies: [],
    coverageGrants: [],
    methodCoverages: [
      { methodCoverageId: "mc1", serviceCode: "tipax", optionCode: "standard", mode: "Restricted" },
    ],
  });
  try {
    const result = await loadAdminShippingRates("tipax");
    assert.equal(result.state, "ok");
    assert.deepEqual(result.data!.methodCoverages, [
      { methodCoverageId: "mc1", serviceCode: "tipax", optionCode: "standard", mode: "Restricted" },
    ]);
    assert.equal(result.data!.methodCoveragesUntrusted, false);
  } finally {
    readable.restore();
  }

  const broken = installFetch({
    serviceCode: "tipax",
    ratePolicies: [],
    coverageGrants: [],
    methodCoverages: [
      { methodCoverageId: "mc1", serviceCode: "tipax", optionCode: "standard", mode: "Restricted" },
      { methodCoverageId: "mc2", serviceCode: "tipax", optionCode: "express", mode: "Sideways" },
    ],
  });
  try {
    const result = await loadAdminShippingRates("tipax");
    assert.equal(result.data!.methodCoverages.length, 1);
    assert.equal(result.data!.methodCoveragesUntrusted, true);
    // یک ردیف ناخوانا کل اعلام را غیرقابل‌اعتماد می‌کند، نه «بدون محدودیت».
    assert.deepEqual(resolveCoverageMode(result.data!, TIPAX_STANDARD), { state: "untrusted" });
  } finally {
    broken.restore();
  }
});

test("the tab writes the method's own option and honors read-only", () => {
  const section = fs.readFileSync(new URL("./shipping-method-coverage-mode-section.tsx", import.meta.url), "utf8");
  assert.match(section, /optionCode: binding\.optionCode/);
  assert.match(section, /setAdminShippingMethodCoverage/);
  assert.match(section, /data-testid="shipping-one-level-coverage-mode"/);
  assert.match(section, /readOnly/);

  const tab = fs.readFileSync(new URL("./shipping-one-level-rates-tab.tsx", import.meta.url), "utf8");
  assert.match(tab, /<ShippingMethodCoverageModeSection/);
  assert.match(tab, /readOnly=\{readOnly\}/);
  // یک سطح دوم یا انتخابگر provider/option در این زبانه ساخته نمی‌شود.
  assert.doesNotMatch(tab, /serviceCode.*split\(/);

  const dialog = fs.readFileSync(new URL("./flat-method-detail-dialog.tsx", import.meta.url), "utf8");
  assert.match(dialog, /<ShippingOneLevelRatesTab[^>]*readOnly=\{readOnly\}/);
});
