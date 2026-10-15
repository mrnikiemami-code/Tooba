import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  DEFAULT_ONE_LEVEL_TAB,
  ONE_LEVEL_TAB_IDS,
  ONE_LEVEL_TAB_LABELS,
  ONE_LEVEL_TAB_WIRING_TASK,
  buildOneLevelSearch,
  nextTabIndex,
  parseOneLevelSelection,
  projectMethodRows,
  resolveDataState,
  sanitizeLanguageId,
  sanitizeMethodCode,
  wiringNoticeFor,
  type OneLevelMethodRow,
} from "./shipping-one-level-tabs-model.ts";
import {
  ONE_LEVEL_CATALOG_ADMIN_READ_PATH,
  ONE_LEVEL_CATALOG_SERVER_SECTION,
  canRenderOneLevelMethods,
  mapHostOneLevelMethods,
  resolveOneLevelCatalogGate,
  resolveOneLevelCatalogGateFromHost,
} from "./shipping-one-level-gate.ts";

const FA = "11111111-1111-4111-8111-111111111111";

function method(partial: Partial<OneLevelMethodRow> & { methodCode: string }): OneLevelMethodRow {
  return {
    providerKind: "post",
    isActive: true,
    legacyServiceCode: "post",
    legacyOptionCode: "standard",
    translations: [],
    ...partial,
  };
}

test("the five tab labels are exactly the locked Persian labels", () => {
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
  assert.equal(DEFAULT_ONE_LEVEL_TAB, "services");
});

test("all five tabs are genuinely wired and none advertises a wiring task", () => {
  // TB-SHIPPING-ONE-LEVEL-TABS-086..089 — every tab is genuinely wired on the server-owned binding.
  for (const id of ONE_LEVEL_TAB_IDS) {
    assert.equal(ONE_LEVEL_TAB_WIRING_TASK[id], null, `${id} should be wired`);
    assert.equal(wiringNoticeFor(id), null, `${id} should not show a wiring notice`);
  }
});

test("method codes are sanitized to the 32-character canonical identity", () => {
  assert.equal(sanitizeMethodCode("  POST:Standard "), "post:standard");
  assert.equal(sanitizeMethodCode("tipax:express"), "tipax:express");
  assert.equal(sanitizeMethodCode("store_courier"), "store_courier");
  // یک جداکنندهٔ `:` بیشتر مجاز نیست و نویسه‌های خطرناک حذف می‌شوند.
  assert.equal(sanitizeMethodCode("post:ex:press"), "post:express");
  assert.equal(sanitizeMethodCode("post standard;drop"), "poststandarddrop");
  assert.equal(sanitizeMethodCode("<script>"), "script");
  assert.equal(sanitizeMethodCode("a".repeat(80)).length, 32);
  assert.equal(sanitizeMethodCode(null), "");
  assert.equal(sanitizeMethodCode(123), "");
});

test("language ids accept only canonical uuids", () => {
  assert.equal(sanitizeLanguageId(`  ${FA.toUpperCase()} `), FA);
  assert.equal(sanitizeLanguageId("fa"), "");
  assert.equal(sanitizeLanguageId("not-a-uuid"), "");
  assert.equal(sanitizeLanguageId(undefined), "");
});

test("url selection is parsed safely and never trusts malformed input", () => {
  assert.deepEqual(parseOneLevelSelection(""), { tab: "services", methodCode: "", languageId: "" });
  assert.deepEqual(parseOneLevelSelection(null), { tab: "services", methodCode: "", languageId: "" });
  assert.deepEqual(parseOneLevelSelection("?tab=rates-coverage&method=TIPAX:Express&lang=" + FA), {
    tab: "rates-coverage",
    methodCode: "tipax:express",
    languageId: FA,
  });
  // زبانهٔ ناشناخته ⇒ پیش‌فرض؛ هیچ مقدار خامی به UI نمی‌رسد.
  assert.equal(parseOneLevelSelection("?tab=../../etc/passwd").tab, "services");
  assert.equal(parseOneLevelSelection("?tab=services&method=<img src=x>").methodCode, "imgsrcx");
});

test("url building omits defaults and round-trips sanitized values", () => {
  assert.equal(buildOneLevelSearch({ tab: "services", methodCode: "", languageId: "" }), "");
  assert.equal(
    buildOneLevelSearch({ tab: "services", methodCode: "POST:Express", languageId: "" }),
    "?method=post%3Aexpress",
  );
  assert.equal(
    buildOneLevelSearch({ tab: "cost-review", methodCode: "", languageId: FA }),
    `?tab=cost-review&lang=${FA}`,
  );

  const parsed = parseOneLevelSelection(buildOneLevelSearch({ tab: "packaging-fare", methodCode: "store_courier", languageId: FA }));
  assert.deepEqual(parsed, { tab: "packaging-fare", methodCode: "store_courier", languageId: FA });
});

test("tablist keyboard navigation wraps and supports Home/End", () => {
  const count = ONE_LEVEL_TAB_IDS.length;
  assert.equal(nextTabIndex(0, "ArrowRight", count), 1);
  assert.equal(nextTabIndex(count - 1, "ArrowRight", count), 0);
  assert.equal(nextTabIndex(0, "ArrowLeft", count), count - 1);
  assert.equal(nextTabIndex(2, "Home", count), 0);
  assert.equal(nextTabIndex(2, "End", count), count - 1);
  assert.equal(nextTabIndex(1, "Enter", count), null);
  assert.equal(nextTabIndex(1, "Tab", count), null);
  assert.equal(nextTabIndex(0, "ArrowRight", 0), null);
  assert.equal(nextTabIndex(0, "ArrowRight", count, { wrap: false }), 1);
  assert.equal(nextTabIndex(count - 1, "ArrowRight", count, { wrap: false }), count - 1);
});

test("method rows keep one row per method and never invent a label", () => {
  const rows = projectMethodRows(
    [
      method({ methodCode: "post:standard", translations: [{ languageId: FA, name: " پست معمولی ", description: null }] }),
      method({ methodCode: "post:express", isActive: false }),
      method({ methodCode: "store_courier", legacyOptionCode: null, providerKind: "store_courier" }),
      method({ methodCode: "snapp_courier", isActive: false, legacyOptionCode: null }),
    ],
    FA,
  );

  assert.deepEqual(rows.map((r) => r.methodCode), ["post:standard", "post:express", "store_courier", "snapp_courier"]);
  assert.equal(rows[0]?.label, "پست معمولی");
  assert.equal(rows[0]?.hasLabel, true);
  // در نبود ترجمهٔ زبان فعال، برچسب خالی می‌ماند؛ هیچ نام اختراعی ساخته نمی‌شود.
  assert.equal(rows[1]?.label, "");
  assert.equal(rows[1]?.hasLabel, false);
});

test("inactive methods stay inactive and are never silently activated or dropped", () => {
  const rows = projectMethodRows(
    [
      method({ methodCode: "post:express", isActive: false }),
      method({ methodCode: "snapp_courier", isActive: false, legacyOptionCode: null }),
      method({ methodCode: "post:standard", isActive: true }),
    ],
    FA,
  );
  assert.equal(rows.length, 3);
  assert.equal(rows.find((r) => r.methodCode === "post:express")?.isActive, false);
  assert.equal(rows.find((r) => r.methodCode === "snapp_courier")?.isActive, false);
  assert.equal(rows.find((r) => r.methodCode === "post:standard")?.isActive, true);
});

test("non-canonical method codes are rejected instead of normalized into new identities", () => {
  const rows = projectMethodRows(
    [
      method({ methodCode: "POST:Standard" }),
      method({ methodCode: "post:ex:press" }),
      method({ methodCode: "a".repeat(33) }),
      method({ methodCode: "post:standard" }),
    ],
    FA,
  );
  assert.deepEqual(rows.map((r) => r.methodCode), ["post:standard"]);
});

test("data state is honest and has no legacy fallback path", () => {
  assert.equal(resolveDataState({ gateEnabled: false, loading: false, error: false }), "unavailable");
  assert.equal(resolveDataState({ gateEnabled: false, loading: true, error: true }), "unavailable");
  assert.equal(resolveDataState({ gateEnabled: true, loading: true, error: false }), "loading");
  assert.equal(resolveDataState({ gateEnabled: true, loading: false, error: true }), "error");
  assert.equal(resolveDataState({ gateEnabled: true, loading: false, error: false }), "ready");
});

test("the frontend gate is disabled by absence and cannot be enabled from the client", () => {
  assert.equal(ONE_LEVEL_CATALOG_SERVER_SECTION, "Tooba:ShippingOneLevelCatalog");

  assert.deepEqual(resolveOneLevelCatalogGate(), { state: "disabled", reason: "absent" });
  assert.deepEqual(resolveOneLevelCatalogGate({ serverFlag: null }), { state: "disabled", reason: "absent" });
  assert.deepEqual(resolveOneLevelCatalogGate({ serverFlag: false }), { state: "disabled", reason: "explicitly-off" });
  assert.deepEqual(resolveOneLevelCatalogGate({ serverFlag: true }), { state: "enabled", source: "server-flag" });

  // تنها یک پرچم سرورِ true اجازهٔ رندر می‌دهد؛ بقیهٔ حالت‌ها خاموش‌اند.
  assert.equal(canRenderOneLevelMethods({ state: "disabled", reason: "absent" }), false);
  assert.equal(canRenderOneLevelMethods({ state: "disabled", reason: "explicitly-off" }), false);
  assert.equal(canRenderOneLevelMethods({ state: "enabled", source: "server-flag" }), true);
});

test("host gate response is mapped fail-closed and never enables on anything but an explicit flag", () => {
  // مسیر خواندن مجوزدار سرور دقیقاً همان مسیر فقط-خواندنی ادمین است.
  assert.equal(
    ONE_LEVEL_CATALOG_ADMIN_READ_PATH,
    "/v1/admin/shipping-services/shipping-methods/one-level",
  );

  // پاسخ معتبر و کامل ⇒ تنها حالت روشن.
  assert.deepEqual(
    resolveOneLevelCatalogGateFromHost({ status: 200, payload: { gate: "Enabled", ready: true } }),
    { state: "enabled", source: "server-flag" },
  );

  // دروازهٔ صریحاً خاموش.
  assert.deepEqual(
    resolveOneLevelCatalogGateFromHost({ status: 200, payload: { gate: "Disabled", ready: true } }),
    { state: "disabled", reason: "explicitly-off" },
  );

  // دروازه روشن ولی کاتالوگ مهاجرت‌نشده ⇒ هرگز «فعال» جا زده نمی‌شود.
  assert.deepEqual(
    resolveOneLevelCatalogGateFromHost({ status: 200, payload: { gate: "Enabled", ready: false } }),
    { state: "disabled", reason: "catalog-not-ready" },
  );

  // خطای شبکه/مجوز/پاسخ نامعتبر ⇒ fail-closed.
  assert.deepEqual(
    resolveOneLevelCatalogGateFromHost({ status: 0, payload: null }),
    { state: "disabled", reason: "untrusted-source" },
  );
  assert.deepEqual(
    resolveOneLevelCatalogGateFromHost({ status: 401, payload: null }),
    { state: "disabled", reason: "untrusted-source" },
  );
  assert.deepEqual(
    resolveOneLevelCatalogGateFromHost({ status: 200, payload: "Enabled" }),
    { state: "disabled", reason: "untrusted-source" },
  );
  assert.deepEqual(
    resolveOneLevelCatalogGateFromHost({ status: 200, payload: {} }),
    { state: "disabled", reason: "explicitly-off" },
  );
  // مقدار رشته‌ای/truthy به‌جای boolean هرگز روشن نمی‌کند.
  assert.deepEqual(
    resolveOneLevelCatalogGateFromHost({ status: 200, payload: { gate: "Enabled", ready: "true" } }),
    { state: "disabled", reason: "catalog-not-ready" },
  );
});

test("host method rows are mapped defensively and never invent a label or a second level", () => {
  const rows = mapHostOneLevelMethods({
    methods: [
      { methodCode: "post:standard", name: " پست معمولی ", isActive: true },
      { methodCode: "post:express", name: "", isActive: false },
      { methodCode: "POST:Standard", name: "x", isActive: true },
      { methodCode: "a".repeat(40), name: "x", isActive: true },
      { methodCode: "store_courier", isActive: true },
      null,
      42,
    ],
  });

  assert.deepEqual(rows.map((r) => r.methodCode), ["post:standard", "post:express", "store_courier"]);
  const projected = projectMethodRows(rows, "");
  assert.equal(projected[0]?.label, "پست معمولی");
  assert.equal(projected[0]?.isActive, true);
  // برچسب غایب اختراع نمی‌شود و روش غیرفعال غیرفعال می‌ماند.
  assert.equal(projected[1]?.hasLabel, false);
  assert.equal(projected[1]?.isActive, false);
  // سطح دوم هرگز از پاسخ سرور ساخته نمی‌شود.
  assert.equal(rows.every((r) => r.providerKind === ""), true);
  assert.equal(rows.every((r) => r.legacyServiceCode === null && r.legacyOptionCode === null), true);
  assert.deepEqual(mapHostOneLevelMethods(null), []);
  assert.deepEqual(mapHostOneLevelMethods({ methods: "x" }), []);
});

test("screen reads the authoritative gate server-side and exposes no client bypass", () => {
  const raw = fs.readFileSync(new URL("./page.tsx", import.meta.url), "utf8");
  // داوری روی کد اجرایی؛ توضیحات مجازند نام منابع غیرمجاز را بشمارند.
  const page = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  // دروازه از پاسخ مجوزدار سرور تعیین می‌شود، نه از پارامتر کلاینتی.
  assert.match(page, /resolveOneLevelCatalogGateFromHost\(hostRead\)/);
  assert.match(page, /ONE_LEVEL_CATALOG_ADMIN_READ_PATH/);
  assert.match(page, /buildUpstreamAuthHeaders\(\)/);
  assert.match(page, /cache: "no-store"/);
  assert.match(page, /gate\.state === "enabled" \? \(\s*<ShippingOneLevelTabs gate=\{gate\} activeLanguageId=\{activeLanguageId\} initialMethods=\{initialMethods\} \/>\s*\) : \(\s*<ShippingServicesScreen \/>/);
  // هیچ پارامتر URL یا منبع کلاینتی نباید دروازه را روشن کند.
  assert.doesNotMatch(page, /useSearchParams/);
  assert.doesNotMatch(page, /URLSearchParams/);
  assert.doesNotMatch(page, /localStorage/);
  assert.doesNotMatch(page, /\?tab=|\?method=|\?gate=/);
  // هیچ اعتبارنامه‌ای به HTML/مرورگر نمی‌رسد و هیچ لاگی از توکن نیست.
  assert.doesNotMatch(page, /console\./);
  assert.doesNotMatch(page, /"use client"/);
  assert.doesNotMatch(page, /Authorization.*NEXT_PUBLIC/);
  // هیچ نوشتن/قیمت/رزرو در این صفحه نیست.
  assert.doesNotMatch(page, /deactivateAdminShippingService|writeAdminShippingService/);
  assert.doesNotMatch(page, /ShippingRateAuthority|ShippingOperationalShadow/);
});

test("shell renders server-provided rows without a client fetch and edits only the method name", () => {
  const raw = fs.readFileSync(new URL("./shipping-one-level-tabs.tsx", import.meta.url), "utf8");
  const shell = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  // ردیف‌های سرور به‌عنوان مقدار اولیه مصرف می‌شوند؛ نبود خوانندهٔ کلاینتی خطا نیست.
  assert.match(shell, /useState<readonly OneLevelMethodRow\[\]>\(initialMethods \?\? \[\]\)/);
  assert.match(shell, /setLoadError\(initialMethods === undefined\)/);
  // TB-094: تنها نوشتن این شل، ذخیرهٔ نام روش است؛ هیچ نوشتن دیگری وجود ندارد.
  assert.doesNotMatch(shell, /deactivateAdminShippingService|writeAdminShippingService/);
  assert.doesNotMatch(shell, /Authorization/);
  assert.doesNotMatch(shell, /localStorage/);
});

test("new shell edits only the method name and contains no second-level configuration", () => {
  const raw = fs.readFileSync(new URL("./shipping-one-level-tabs.tsx", import.meta.url), "utf8");
  // نظرات توضیحی اجازه دارند از «ذخیره»/providerKind حرف بزنند؛ داوری روی کد اجرایی است.
  const shell = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  // TB-094: تنها ویرایش، «نام نمایشی» است؛ هیچ حذف/فعال‌سازی/ایجاد سرویس و هیچ ویرایشگر نوع سرویس.
  assert.match(shell, /ویرایش نام/);
  assert.doesNotMatch(shell, /deactivateAdminShippingService|writeAdminShippingService/);
  // سطح دوم حذف شده است: نه ویرایشگر انواع سرویس، نه ستون تعداد گزینه.
  assert.doesNotMatch(shell, /انواع سرویس/);
  assert.doesNotMatch(shell, /optionCount|activeOptionCount/);
  assert.doesNotMatch(shell, /ShippingServiceOptionWrite/);
  // providerKind فرادادهٔ داخلی است و هرگز به‌عنوان گزینهٔ قابل انتخاب رندر نمی‌شود.
  assert.doesNotMatch(shell, /providerKind/);
  // معناشناسی tablist/tab/tabpanel و پیمایش کلید درست است.
  assert.match(shell, /role="tablist"/);
  assert.match(shell, /role="tab"/);
  assert.match(shell, /role="tabpanel"/);
  assert.match(shell, /onKeyDown=\{\(event\) => onTabKeyDown\(event, index\)\}/);
  assert.match(shell, /dir="rtl"/);
});
