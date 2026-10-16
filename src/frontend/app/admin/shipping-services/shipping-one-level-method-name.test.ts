import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  ONE_LEVEL_METHOD_NAME_MAX_LENGTH,
  applySavedMethodName,
  createMethodNameGenerationGuard,
  findMethodRow,
  initialMethodName,
  methodNameTargetKey,
  oneLevelMethodNameIssueFa,
  oneLevelMethodTranslationPath,
  resolveEffectiveLanguageId,
  validateOneLevelMethodName,
} from "./shipping-one-level-method-name-model.ts";
import { mapHostOneLevelMethods, resolveOneLevelActiveLanguageId } from "./shipping-one-level-gate.ts";
import type { OneLevelMethodRow } from "./shipping-one-level-tabs-model.ts";

const FA = "11111111-1111-4111-8111-111111111111";
const EN = "22222222-2222-4222-8222-222222222222";

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

test("the write path carries only the canonical method code and the language id", () => {
  assert.equal(
    oneLevelMethodTranslationPath("POST:Standard", FA.toUpperCase()),
    `/v1/admin/shipping-services/shipping-methods/one-level/post%3Astandard/translations/${FA}`,
  );
  // کد غیرcanonical هرگز به هویت تازه تبدیل نمی‌شود و زبان نامعتبر خالی می‌شود.
  assert.equal(
    oneLevelMethodTranslationPath("post:ex:press", "not-a-uuid"),
    "/v1/admin/shipping-services/shipping-methods/one-level/post%3Aexpress/translations/",
  );
  assert.match(oneLevelMethodTranslationPath("store_courier", FA), /\/one-level\/store_courier\/translations\//);
});

test("a name must be non-blank and within the stable column width", () => {
  assert.equal(validateOneLevelMethodName("پست معمولی"), null);
  assert.equal(validateOneLevelMethodName("  "), "required");
  assert.equal(validateOneLevelMethodName(""), "required");
  assert.equal(validateOneLevelMethodName(undefined), "required");
  assert.equal(validateOneLevelMethodName(42), "required");
  assert.equal(validateOneLevelMethodName("a".repeat(ONE_LEVEL_METHOD_NAME_MAX_LENGTH)), null);
  assert.equal(validateOneLevelMethodName("a".repeat(ONE_LEVEL_METHOD_NAME_MAX_LENGTH + 1)), "too_long");
  assert.notEqual(oneLevelMethodNameIssueFa("required"), "");
  assert.notEqual(oneLevelMethodNameIssueFa("too_long"), "");
  assert.equal(oneLevelMethodNameIssueFa(null), "");
});

test("the edit form is prefilled only from the real translation of the selected language", () => {
  const row = method({
    methodCode: "post:standard",
    translations: [
      { languageId: FA, name: " پست معمولی ", description: null },
      { languageId: EN, name: "Standard Post", description: null },
    ],
  });
  assert.equal(initialMethodName(row, FA), "پست معمولی");
  assert.equal(initialMethodName(row, EN), "Standard Post");
  // زبان بدون ترجمه ⇒ خالی؛ هیچ نامی اختراع نمی‌شود.
  assert.equal(initialMethodName(row, "33333333-3333-4333-8333-333333333333"), "");
  assert.equal(initialMethodName(undefined, FA), "");
  assert.equal(initialMethodName(row, ""), "");
});

test("a saved name updates exactly one row/language and never touches identity or activation", () => {
  const rows = [
    method({
      methodCode: "post:standard",
      isActive: false,
      translations: [{ languageId: FA, name: "معمولی", description: null }],
    }),
    method({ methodCode: "post:express", isActive: false, translations: [] }),
  ];

  const next = applySavedMethodName(rows, "post:standard", FA, "پست معمولی");
  const standard = next.find((r) => r.methodCode === "post:standard");
  assert.equal(standard?.translations.find((t) => t.languageId === FA)?.name, "پست معمولی");
  // روش غیرفعال غیرفعال می‌ماند و کد روش تغییر نمی‌کند.
  assert.equal(standard?.isActive, false);
  assert.equal(standard?.methodCode, "post:standard");
  // ردیف دیگر دست‌نخورده است.
  assert.deepEqual(next.find((r) => r.methodCode === "post:express")?.translations, []);
  assert.equal(next.length, rows.length);
});

test("a saved name for an unknown row or language leaves the list unchanged", () => {
  const rows = [method({ methodCode: "post:standard", translations: [] })];
  assert.equal(applySavedMethodName(rows, "tipax:express", FA, "x"), rows);
  assert.equal(applySavedMethodName(rows, "post:standard", "nope", "x"), rows);
  assert.equal(applySavedMethodName(rows, "", FA, "x"), rows);
});

test("the edit target key distinguishes method+language and the generation guard rejects stale results", () => {
  assert.notEqual(methodNameTargetKey("post:standard", FA), methodNameTargetKey("post:standard", EN));
  assert.notEqual(methodNameTargetKey("post:standard", FA), methodNameTargetKey("post:express", FA));
  assert.equal(methodNameTargetKey("POST:Standard", FA.toUpperCase()), methodNameTargetKey("post:standard", FA));

  const guard = createMethodNameGenerationGuard();
  const first = guard.begin();
  assert.equal(first(), true);
  const second = guard.begin();
  // نتیجهٔ ذخیرهٔ هدف قبلی هرگز روی هدف تازه نمی‌نشیند.
  assert.equal(first(), false);
  assert.equal(second(), true);
});

test("method rows are found by canonical code only", () => {
  const rows = [method({ methodCode: "post:standard" })];
  assert.equal(findMethodRow(rows, "POST:Standard")?.methodCode, "post:standard");
  assert.equal(findMethodRow(rows, "post:express"), undefined);
  assert.equal(findMethodRow(rows, ""), undefined);
});

test("host rows keep the real per-language translations and never invent a label or a second level", () => {
  const rows = mapHostOneLevelMethods({
    languageId: FA,
    methods: [
      {
        methodCode: "post:standard",
        name: "پست معمولی",
        isActive: true,
        translations: [
          { languageId: FA, name: " پست معمولی " },
          { languageId: EN, name: "Standard Post" },
          { languageId: "bad", name: "x" },
          { languageId: FA, name: "duplicate" },
        ],
      },
      { methodCode: "post:express", name: "", isActive: false, translations: [] },
      { methodCode: "POST:Standard", name: "x", isActive: true },
    ],
  });

  assert.deepEqual(rows.map((r) => r.methodCode), ["post:standard", "post:express"]);
  assert.deepEqual(rows[0]?.translations, [
    { languageId: FA, name: "پست معمولی", description: null },
    { languageId: EN, name: "Standard Post", description: null },
  ]);
  assert.deepEqual(rows[1]?.translations, []);
  // هیچ سطح دومی از پاسخ سرور ساخته نمی‌شود.
  assert.equal(rows.every((r) => r.providerKind === ""), true);
  assert.equal(rows.every((r) => r.legacyServiceCode === null && r.legacyOptionCode === null), true);
  assert.equal(resolveOneLevelActiveLanguageId({ languageId: FA.toUpperCase() }), FA);
  assert.equal(resolveOneLevelActiveLanguageId({}), "");
  assert.equal(resolveOneLevelActiveLanguageId(null), "");
});

test("the shell exposes a name editor only and keeps the five-tab contract", () => {
  const raw = fs.readFileSync(new URL("./shipping-one-level-tabs.tsx", import.meta.url), "utf8");
  const shell = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  // تنها ویرایشگر، دیالوگ نام است و تنها نوشتن فراخوانی‌شده، ذخیرهٔ نام.
  assert.match(shell, /ShippingOneLevelMethodNameDialog/);
  assert.match(shell, /ویرایش نام/);
  assert.doesNotMatch(shell, /deactivateAdminShippingService|writeAdminShippingService/);
  // سطح دوم حذف‌شده باقی می‌ماند: نه ویرایشگر انواع سرویس، نه ستون تعداد گزینه.
  assert.doesNotMatch(shell, /انواع سرویس/);
  assert.doesNotMatch(shell, /optionCount|activeOptionCount/);
  assert.doesNotMatch(shell, /ShippingServiceOptionWrite/);
  // معناشناسی tablist/tab/tabpanel و RTL و پیمایش کلید حفظ شده است.
  assert.match(shell, /role="tablist"/);
  assert.match(shell, /role="tab"/);
  assert.match(shell, /role="tabpanel"/);
  assert.match(shell, /onKeyDown=\{\(event\) => onTabKeyDown\(event, index\)\}/);
  assert.match(shell, /dir="rtl"/);
});

test("the dialog writes only the name, keeps the code read-only and never fabricates success", () => {
  const raw = fs.readFileSync(new URL("./shipping-one-level-method-name-dialog.tsx", import.meta.url), "utf8");
  const dialog = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  assert.match(dialog, /saveOneLevelMethodName/);
  assert.doesNotMatch(dialog, /serviceCode|optionCode|providerKind|isActive/);
  // نتیجه فقط پس از ok و تطابق هدف اعمال می‌شود.
  assert.match(dialog, /result\.state !== "ok"/);
  assert.match(dialog, /methodNameTargetKey\(result\.data\.methodCode, result\.data\.languageId\) !== requestedKey/);
  assert.match(dialog, /stillCurrent\(\)/);
});

test("the effective language never stays empty or invalid when the language list arrives late", () => {
  const loaded = [{ languageId: FA }, { languageId: EN }];

  // فهرست هنوز نرسیده ⇒ خالی و ویرایش غیرفعال (fail-closed)، نه زبان جعلی.
  assert.equal(resolveEffectiveLanguageId([], FA, ""), "");
  assert.equal(resolveEffectiveLanguageId([], "", FA), "");

  // فهرست رسیده و انتخاب خالی ⇒ زبان پیش‌فرض مؤثر، نه خالی.
  assert.equal(resolveEffectiveLanguageId(loaded, FA, ""), FA);
  // انتخاب کاربر معتبر است و بر پیش‌فرض مقدم می‌شود.
  assert.equal(resolveEffectiveLanguageId(loaded, FA, EN), EN);
  // انتخاب نامعتبر/خارج از فهرست ⇒ بازگشت به پیش‌فرض مؤثر.
  assert.equal(resolveEffectiveLanguageId(loaded, FA, "not-a-uuid"), FA);
  assert.equal(resolveEffectiveLanguageId(loaded, FA, "33333333-3333-4333-8333-333333333333"), FA);
  // پیش‌فرض نامعتبر ⇒ نخستین زبان معتبر فهرست.
  assert.equal(resolveEffectiveLanguageId(loaded, "", ""), FA);
  assert.equal(resolveEffectiveLanguageId([{ languageId: "bad" }, { languageId: EN }], FA.toUpperCase(), ""), EN);
});

test("the dialog resolves the effective language from the real list and never sends an empty one", () => {
  const raw = fs.readFileSync(new URL("./shipping-one-level-method-name-dialog.tsx", import.meta.url), "utf8");
  const dialog = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  assert.match(dialog, /resolveEffectiveLanguageId\(languageOptions, defaultLanguageId, languageId\)/);
  // ارسال با زبان مؤثر انجام می‌شود، نه با حالت خام (که می‌تواند خالی/کهنه باشد).
  assert.match(dialog, /const requestedLanguage = effectiveLanguageId;/);
  assert.match(dialog, /disabled=\{saving \|\| !effectiveLanguageId \|\| !code\}/);
  assert.match(dialog, /value=\{effectiveLanguageId\}/);
  assert.match(dialog, /initialMethodName\(method, effectiveLanguageId\)/);
});

test("the api client sends only the name and maps denied/error truthfully", () => {
  const raw = fs.readFileSync(new URL("./shipping-one-level-method-name-api.ts", import.meta.url), "utf8");
  const api = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  assert.match(api, /method: "PUT"/);
  assert.match(api, /JSON\.stringify\(\{ name \}\)/);
  assert.match(api, /response\.status === 401 \|\| response\.status === 403/);
  assert.match(api, /state: "denied"/);
  assert.doesNotMatch(api, /serviceCode|optionCode|providerKind/);
});

test("the server page composes the flat method screen without a client bypass", () => {
  const raw = fs.readFileSync(new URL("./page.tsx", import.meta.url), "utf8");
  // نظرات توضیحی اجازه دارند نام منابع غیرمجاز را بشمارند؛ داوری روی کد اجرایی است.
  const page = raw.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");
  // TB-SHIPPING-ADMIN-FLAT-SERVICE-FULL-IMPLEMENTATION-146 / TB-147: مسیر رندر صفحه همان فهرست تخت
  // است؛ دروازهٔ سرور تصمیم اول را می‌گیرد و در نبود هویت سرور، همان دروازه با مسیر مجاز مرورگر
  // تصمیم می‌گیرد — هیچ سقوط بی‌صدایی به صفحهٔ قدیمی و هیچ سوئیچ کلاینتیِ دلخواه وجود ندارد.
  assert.match(page, /<ShippingServicesFlatScreenGate\b/);
  assert.match(page, /serverResolved=\{gate\.state === "enabled"\}/);
  assert.doesNotMatch(page, /ShippingServicesScreen/);
  assert.doesNotMatch(page, /ShippingOneLevelTabs/);
  assert.doesNotMatch(page, /useSearchParams/);
  assert.doesNotMatch(page, /localStorage/);
});

