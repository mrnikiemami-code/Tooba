import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  ONE_LEVEL_METHOD_ACTIVATION_SEGMENT,
  ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA,
  activationActionFa,
  activationBindingContextFa,
  activationConfirmFa,
  activationErrorFa,
  activationImpactFa,
  activationStatusFa,
  activationTargetKey,
  activationUnavailableFa,
  createActivationGenerationGuard,
  loadOneLevelMethodBinding,
  mapActivationResponse,
  oneLevelMethodActivationPath,
  oneLevelMethodBindingPath,
  resolveActivationEligibility,
  setOneLevelMethodActivation,
} from "./shipping-one-level-activation-model.ts";
import type { OneLevelBindingState } from "./shipping-one-level-rates-model.ts";
import {
  ONE_LEVEL_TAB_IDS,
  ONE_LEVEL_TAB_LABELS,
  ONE_LEVEL_UI_ALLOWED_WRITES,
  ONE_LEVEL_UI_ALLOWS_NAME_EDIT,
  ONE_LEVEL_UI_ALLOWS_OPTION_BACKED_ACTIVATION,
} from "./shipping-one-level-tabs-model.ts";

/** فقط کد منبع بدون توضیحات؛ داوری روی رفتار اجرایی است نه متن توضیح. */
function executable(file: string): string {
  const raw = fs.readFileSync(new URL(file, import.meta.url), "utf8");
  return raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");
}

function binding(partial: Partial<{ methodCode: string; serviceCode: string; optionCode: string | null; isActive: boolean }> = {}): OneLevelBindingState {
  return {
    state: "resolved",
    binding: {
      methodCode: "post:express",
      serviceCode: "post",
      optionCode: "express",
      isActive: false,
      ...partial,
    },
  };
}

/* ------------------------------------------------------------------ endpoint / payload contract */

test("the activation path is exactly the 096 seam and keeps the method code as the only identity", () => {
  assert.equal(ONE_LEVEL_METHOD_ACTIVATION_SEGMENT, "activation");
  assert.equal(
    oneLevelMethodActivationPath("POST:Express"),
    "/v1/admin/shipping-services/shipping-methods/one-level/post%3Aexpress/activation",
  );
  assert.equal(
    oneLevelMethodActivationPath("store_courier"),
    "/v1/admin/shipping-services/shipping-methods/one-level/store_courier/activation",
  );
  // کد نامعتبر به مسیر خالی/خطرناک تبدیل نمی‌شود.
  assert.equal(
    oneLevelMethodActivationPath("<script>"),
    "/v1/admin/shipping-services/shipping-methods/one-level/script/activation",
  );
  // مسیر پیوند همان resolver مجاز ۰۸۶ است (تنها منبع «گزینه‌دار بودن»).
  assert.equal(
    oneLevelMethodBindingPath("post:express"),
    "/v1/admin/shipping-services/shipping-methods/one-level/post%3Aexpress/binding",
  );
});

test("no service or option identity is ever sent by the activation client", () => {
  const source = executable("./shipping-one-level-activation-model.ts");
  const bodies = [...source.matchAll(/JSON\.stringify\(\s*\{([^}]*)\}/g)].map((m) => m[1]);
  assert.equal(bodies.length, 1, "exactly one request body may exist");
  assert.match(bodies[0] ?? "", /isActive/);
  // هیچ serviceCode/optionCode/UUID گزینه‌ای و حتی هیچ کد روشی در بدنه نیست.
  assert.doesNotMatch(bodies[0] ?? "", /serviceCode|optionCode|shippingServiceOptionId|methodCode|ServiceCode/);
  // هیچ endpoint دیگری ساخته نمی‌شود و متد فقط PUT است.
  const paths = [...source.matchAll(/"\/v1\/admin\/shipping-services[^"]*"/g)].map((m) => m[0]);
  assert.equal(paths.length, 0, "paths must be built by the pure path helpers, not hand-written literals");
  assert.match(source, /method: "PUT"/);
  assert.doesNotMatch(source, /method: "POST"|method: "DELETE"|method: "PATCH"/);
});

/* ------------------------------------------------------------------ eligibility */

test("only a server-resolved option-backed method is eligible for activation", () => {
  assert.deepEqual(resolveActivationEligibility(binding(), "post:express"), {
    state: "supported",
    methodCode: "post:express",
    currentIsActive: false,
  });
  assert.deepEqual(resolveActivationEligibility(binding({ isActive: true }), "post:express"), {
    state: "supported",
    methodCode: "post:express",
    currentIsActive: true,
  });
});

test("optionless methods never get a toggle, no matter how the code looks", () => {
  for (const code of ["snapp_courier", "store_courier", "in_person"]) {
    assert.deepEqual(
      resolveActivationEligibility(binding({ methodCode: code, optionCode: null }), code),
      { state: "optionless", methodCode: code },
    );
  }
  // هیچ فعال‌سازی سطح سرویس برای این روش‌ها حتی حدس زده نمی‌شود.
  assert.equal(ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA.includes("پشتیبانی نمی‌شود"), true);
});

test("an unproven binding is fail-closed and never becomes an activation switch", () => {
  assert.deepEqual(resolveActivationEligibility(null, "post:express"), { state: "unavailable", reason: "unresolved" });
  assert.deepEqual(resolveActivationEligibility({ state: "gate-off" }, "post:express"), {
    state: "unavailable",
    reason: "gate-off",
  });
  assert.deepEqual(resolveActivationEligibility({ state: "not-ready" }, "post:express"), {
    state: "unavailable",
    reason: "not-ready",
  });
  assert.deepEqual(resolveActivationEligibility({ state: "unresolved" }, "post:express"), {
    state: "unavailable",
    reason: "unresolved",
  });
  assert.deepEqual(resolveActivationEligibility({ state: "untrusted" }, "post:express"), {
    state: "unavailable",
    reason: "untrusted",
  });
  assert.deepEqual(resolveActivationEligibility(binding(), ""), { state: "unavailable", reason: "no-method" });
  assert.deepEqual(resolveActivationEligibility(binding(), "  "), { state: "unavailable", reason: "no-method" });
});

/* ------------------------------------------------------------------ response mapping */

test("a server response is accepted only when it confirms the same immutable method and a real boolean", () => {
  assert.deepEqual(
    mapActivationResponse({ methodCode: "post:express", isActive: true, changed: true }, "post:express"),
    { methodCode: "post:express", isActive: true, changed: true },
  );
  // کد روش متفاوت ⇒ نه موفقیت، نه نشستن روی روش دیگر.
  assert.equal(mapActivationResponse({ methodCode: "post:standard", isActive: true }, "post:express"), null);
  // isActive غیر boolean هرگز به «فعال» تبدیل نمی‌شود.
  assert.equal(mapActivationResponse({ methodCode: "post:express", isActive: "true" }, "post:express"), null);
  assert.equal(mapActivationResponse({ methodCode: "post:express" }, "post:express"), null);
  assert.equal(mapActivationResponse(null, "post:express"), null);
  assert.equal(mapActivationResponse([], "post:express"), null);
  // changed غایب یعنی false، نه فرض موفقیت.
  assert.deepEqual(mapActivationResponse({ methodCode: "post:express", isActive: false }, "post:express"), {
    methodCode: "post:express",
    isActive: false,
    changed: false,
  });
});

/* ------------------------------------------------------------------ transport outcomes */

type FetchCall = { url: string; method: string; body: string | null; headers: Record<string, string> };

function stubFetch(handler: (call: FetchCall) => { status: number; payload: unknown }): {
  outcome: { calls: FetchCall[]; restore: () => void };
} {
  const calls: FetchCall[] = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: unknown, init?: { method?: string; body?: string; headers?: Record<string, string> }) => {
    const call: FetchCall = {
      url: String(input),
      method: init?.method ?? "GET",
      body: init?.body ?? null,
      headers: init?.headers ?? {},
    };
    calls.push(call);
    const { status, payload } = handler(call);
    return {
      ok: status >= 200 && status < 300,
      status,
      json: async () => payload,
    } as unknown as Response;
  }) as unknown as typeof fetch;
  return { outcome: { calls, restore: () => { globalThis.fetch = original; } } };
}

function stubNetworkFailure(): { restore: () => void } {
  const original = globalThis.fetch;
  globalThis.fetch = (async () => {
    throw new Error("offline");
  }) as unknown as typeof fetch;
  return { restore: () => { globalThis.fetch = original; } };
}

test("the activation call sends the exact payload and only trusts a confirmed server answer", async () => {
  const stub = stubFetch(() => ({ status: 200, payload: { methodCode: "post:express", isActive: true, changed: true } }));
  try {
    const result = await setOneLevelMethodActivation("post:express", true);
    assert.equal(result.state, "ok");
    assert.deepEqual(result.data, { methodCode: "post:express", isActive: true, changed: true });
    assert.equal(stub.outcome.calls.length, 1);
    assert.equal(stub.outcome.calls[0]?.method, "PUT");
    assert.equal(
      stub.outcome.calls[0]?.url,
      "/v1/admin/shipping-services/shipping-methods/one-level/post%3Aexpress/activation",
    );
    assert.equal(stub.outcome.calls[0]?.body, JSON.stringify({ isActive: true }));
  } finally {
    stub.outcome.restore();
  }
});

test("an invalid method code never reaches the network", async () => {
  const stub = stubFetch(() => ({ status: 200, payload: {} }));
  try {
    const result = await setOneLevelMethodActivation("<<>>", true);
    assert.equal(result.state, "error");
    assert.equal(result.message, "shipping_method.code.invalid");
    assert.equal(stub.outcome.calls.length, 0);
  } finally {
    stub.outcome.restore();
  }
});

test("denial, refusals and unexpected failures stay explicit and fail closed", async () => {
  for (const status of [401, 403]) {
    const stub = stubFetch(() => ({ status, payload: null }));
    try {
      const result = await setOneLevelMethodActivation("post:express", true);
      assert.equal(result.state, "denied");
      assert.equal(result.status, status);
    } finally {
      stub.outcome.restore();
    }
  }

  // کدهای واقعی سرور برای دروازهٔ خاموش، والد غیرفعال/گزینه غایب/روش بدون گزینه و روش ناشناخته.
  const cases: [number, string][] = [
    [400, "fulfillment.rejected"],
    [400, "fulfillment.shipping_method.unsupported"],
    [404, "shipping_method.not_found"],
  ];
  for (const [status, code] of cases) {
    const stub = stubFetch(() => ({ status, payload: { errorCode: code } }));
    try {
      const result = await setOneLevelMethodActivation("post:express", true);
      assert.equal(result.state, "error");
      assert.equal(result.message, code);
      assert.notEqual(activationErrorFa(code), "");
    } finally {
      stub.outcome.restore();
    }
  }

  // پاسخ تأییدشده اما نامعتبر ⇒ خطا، نه موفقیت جعلی.
  const invalid = stubFetch(() => ({ status: 200, payload: { methodCode: "post:standard", isActive: true } }));
  try {
    const result = await setOneLevelMethodActivation("post:express", true);
    assert.equal(result.state, "error");
    assert.equal(result.message, "shipping_method_activation.invalid_response");
  } finally {
    invalid.outcome.restore();
  }

  const offline = stubNetworkFailure();
  try {
    const result = await setOneLevelMethodActivation("post:express", true);
    assert.equal(result.state, "error");
    assert.equal(result.message, "host-unreachable");
    assert.equal(activationErrorFa("host-unreachable").includes("هیچ تغییری اعمال نشده"), true);
  } finally {
    offline.restore();
  }
});

test("the binding read is read-only and maps host truth fail-closed", async () => {
  const resolved = stubFetch(() => ({
    status: 200,
    payload: { gate: "Enabled", ready: true, resolved: true, methodCode: "post:express", serviceCode: "post", optionCode: "express", isActive: false },
  }));
  try {
    const state = await loadOneLevelMethodBinding("post:express");
    assert.deepEqual(state, binding());
    assert.equal(resolved.outcome.calls[0]?.method, "GET");
    assert.equal(resolved.outcome.calls[0]?.body, null);
  } finally {
    resolved.outcome.restore();
  }

  const optionless = stubFetch(() => ({
    status: 200,
    payload: { gate: "Enabled", ready: true, resolved: true, methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true },
  }));
  try {
    assert.deepEqual(await loadOneLevelMethodBinding("store_courier"), {
      state: "resolved",
      binding: { methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true },
    });
  } finally {
    optionless.outcome.restore();
  }

  const gateOff = stubFetch(() => ({ status: 200, payload: { gate: "Disabled", ready: true } }));
  try {
    assert.deepEqual(await loadOneLevelMethodBinding("post:express"), { state: "gate-off" });
  } finally {
    gateOff.outcome.restore();
  }

  const denied = stubFetch(() => ({ status: 403, payload: null }));
  try {
    assert.deepEqual(await loadOneLevelMethodBinding("post:express"), { state: "untrusted" });
  } finally {
    denied.outcome.restore();
  }
});

/* ------------------------------------------------------------------ stale response guard */

test("an in-flight result can never land on another method", () => {
  const guard = createActivationGenerationGuard();
  const first = guard.begin();
  const second = guard.begin();
  assert.equal(first(), false, "the earlier request must be invalidated");
  assert.equal(second(), true, "only the latest request may apply its result");
  const third = guard.begin();
  assert.equal(second(), false);
  assert.equal(third(), true);
  assert.equal(activationTargetKey("POST:Express"), "post:express");
  assert.equal(activationTargetKey("post:standard"), "post:standard");
  assert.notEqual(activationTargetKey("post:express"), activationTargetKey("post:standard"));
});

/* ------------------------------------------------------------------ persian ui contract */

test("the current state, the target action and the storefront impact are explicit in Persian", () => {
  assert.equal(activationStatusFa(true), "فعال");
  assert.equal(activationStatusFa(false), "غیرفعال");
  assert.equal(activationActionFa(true), "غیرفعال‌سازی");
  assert.equal(activationActionFa(false), "فعال‌سازی");
  // اثر عملیات باید صریحاً «واجد شرایط بودن فروشگاه» را نام ببرد.
  assert.match(activationImpactFa(true), /واجد شرایط/);
  assert.match(activationImpactFa(true), /فروشگاه/);
  assert.match(activationImpactFa(false), /واجد شرایط/);
  // تأیید صریح برای هر دو جهت، با اثر روی فروشگاه.
  assert.match(activationConfirmFa(true), /فروشگاه/);
  assert.match(activationConfirmFa(false), /فروشگاه/);
  assert.notEqual(activationConfirmFa(true), activationConfirmFa(false));
  assert.equal(activationUnavailableFa("gate-off").includes("خاموش"), true);
  assert.equal(activationUnavailableFa("no-method").length > 0, true);
  assert.equal(
    activationBindingContextFa({ methodCode: "store_courier", serviceCode: "store_courier", optionCode: null, isActive: true })
      .includes("بدون گزینه"),
    true,
  );
});

/* ------------------------------------------------------------------ control component source contract */

test("the control shows a functional toggle only for option-backed methods", () => {
  const control = executable("./shipping-one-level-activation-control.tsx");
  // شرط سوئیچ فقط حالت «supported» است، یعنی پیوند حل‌شده با گزینه.
  assert.match(control, /eligibility\.state === "supported"/);
  assert.match(control, /resolveActivationEligibility\(bindingState, code\)/);
  // روش بدون گزینه: فقط پیام صریح، بدون هیچ دکمهٔ کاربردی.
  assert.match(control, /ONE_LEVEL_OPTIONLESS_ACTIVATION_UNSUPPORTED_FA/);
  const optionlessBranch = /eligibility\.state === "optionless"([\s\S]*?): null\}/.exec(control)?.[1] ?? "";
  assert.equal(optionlessBranch.includes("<Button"), false, "optionless must have no functional toggle");
  assert.equal(optionlessBranch.includes("onClick"), false, "optionless must not be clickable");
  // هیچ فعال‌سازی خودکار والد و هیچ نوشتن سطح سرویس وجود ندارد.
  assert.doesNotMatch(control, /SetActive|serviceCode\s*:/);
});

test("the control confirms explicitly, guards duplicates and keeps no optimistic state", () => {
  const control = executable("./shipping-one-level-activation-control.tsx");
  // تأیید صریح پیش از عمل.
  assert.match(control, /activationConfirmFa\(currentIsActive\)/);
  assert.match(control, /data-testid="admin-shipping-one-level-activation-confirm-submit"/);
  // ارسال تکراری بسته است.
  assert.match(control, /disabled=\{busy\}/);
  assert.match(control, /disabled=\{busy \|\| eligibility\.state !== "supported"\}/);
  assert.equal((control.match(/aria-busy=\{busy\}/g) ?? []).length >= 2, true);
  // وضعیت نمایش‌داده‌شده فقط از پیوند سرور می‌آید؛ هیچ وضعیت محلی isActive ساخته نمی‌شود.
  assert.doesNotMatch(control, /useState<boolean>/);
  assert.doesNotMatch(control, /setIsActive|setActivation|optimistic/i);
  // پس از تأیید سرور فقط والد را خبر می‌کند تا منبع حقیقت دوباره خوانده شود.
  assert.match(control, /onCompleted\(\{ methodCode: result\.data\.methodCode, isActive: result\.data\.isActive \}\)/);
  // نتیجهٔ سرور با کلید هدف سنجیده می‌شود؛ موفقیت روش دیگر نمی‌نشیند.
  assert.match(control, /activationTargetKey\(result\.data\.methodCode\) !== activationTargetKey\(requestedCode\)/);
});

/* ------------------------------------------------------------------ shell integration */

test("tab 1 wires the activation control through the binding read and refreshes server truth", () => {
  const shell = executable("./shipping-one-level-tabs.tsx");
  // کنترل فقط برای روش انتخاب‌شده و روی پیوند خوانده‌شدهٔ سرور mount می‌شود.
  assert.match(shell, /<ShippingOneLevelActivationControl/);
  assert.match(shell, /methodCode=\{selection\.methodCode\}/);
  assert.match(shell, /bindingState=\{bindingState\}/);
  assert.match(shell, /bindingLoading=\{bindingLoading\}/);
  assert.match(shell, /loadOneLevelMethodBinding\(code\)/);
  // پس از تأیید سرور، کاتالوگ و پیوند دوباره خوانده می‌شوند (بدون وضعیت خوش‌بینانه).
  assert.match(shell, /void load\(\);\s*void loadBinding\(\);\s*router\.refresh\(\);/);
  assert.match(shell, /onCompleted=\{onActivationCompleted\}/);
  // نگهبان نسل برای پاسخ‌های در پرواز همان روش.
  assert.match(shell, /bindingGuard\.current\.begin\(\)/);
  // هیچ نوشتن مستقل روی shipping_methods و هیچ سطح دومی وجود ندارد.
  assert.doesNotMatch(shell, /providerKind/);
  assert.doesNotMatch(shell, /deactivateAdminShippingService|writeAdminShippingService/);
  assert.doesNotMatch(shell, /localStorage|Authorization/);
});

test("the 094 name editor, the five tabs and the RTL tablist semantics are unchanged", () => {
  const shell = executable("./shipping-one-level-tabs.tsx");
  // ویرایش نام روش هنوز همان مسیر قبلی است.
  assert.match(shell, /ویرایش نام/);
  assert.match(shell, /ShippingOneLevelMethodNameDialog/);
  assert.match(shell, /applySavedMethodName/);
  // پنج زبانه و برچسب‌های قفل‌شده دست‌نخورده‌اند.
  assert.deepEqual([...ONE_LEVEL_TAB_IDS], [
    "services",
    "rates-coverage",
    "packaging-fare",
    "schedule-capacity",
    "cost-review",
  ]);
  assert.deepEqual(
    ONE_LEVEL_TAB_IDS.map((id) => ONE_LEVEL_TAB_LABELS[id]),
    ["سرویس‌ها", "تعرفه و پوشش", "بسته‌بندی و کرایه", "برنامه کاری و ظرفیت", "بررسی هزینه"],
  );
  assert.match(shell, /role="tablist"/);
  assert.match(shell, /role="tab"/);
  assert.match(shell, /role="tabpanel"/);
  assert.match(shell, /onKeyDown=\{\(event\) => onTabKeyDown\(event, index\)\}/);
  assert.match(shell, /dir="rtl"/);
  assert.match(shell, /data-testid="admin-shipping-one-level-shell"/);
  // ردیف‌های سرور همچنان مقدار اولیه‌اند؛ نبود خوانندهٔ کلاینتی خطا نیست.
  assert.match(shell, /useState<readonly OneLevelMethodRow\[\]>\(initialMethods \?\? \[\]\)/);
  assert.match(shell, /setLoadError\(initialMethods === undefined\)/);
});

test("the shell declares exactly the two allowed writes and no more", () => {
  assert.deepEqual([...ONE_LEVEL_UI_ALLOWED_WRITES], ["method-name", "option-backed-activation"]);
  assert.equal(ONE_LEVEL_UI_ALLOWS_NAME_EDIT, true);
  assert.equal(ONE_LEVEL_UI_ALLOWS_OPTION_BACKED_ACTIVATION, true);
  // هیچ نامزدی برای نوشتن سطح سرویس/قیمت/رزرو در این شل وجود ندارد.
  const model = executable("./shipping-one-level-tabs-model.ts");
  assert.doesNotMatch(model, /setActive|IsActive\s*=/);
});
