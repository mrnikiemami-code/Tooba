import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  dirtyMethodLanguages,
  methodLanguageDefinitions,
  methodNameDraftFor,
  methodNameDraftIsDirty,
  methodNameDrafts,
  methodTranslationStates,
  methodTranslationSummary,
  serverMethodNameFor,
  setMethodNameDraft,
} from "./shipping-method-language-model.ts";

const FA = "11111111-1111-4111-8111-111111111111";
const EN = "22222222-2222-4222-8222-222222222222";
const RU = "33333333-3333-4333-8333-333333333333";

/** رجیستری واقعی سرور در تست؛ هیچ شناسه/نامی در خود مدل ساخته نمی‌شود. */
const REGISTRY = [
  { languageId: FA, code: "fa-IR", nativeName: "فارسی (ایران)", displayName: "Persian", direction: "rtl", active: true, sortOrder: 0 },
  { languageId: EN, code: "en-US", nativeName: "English (US)", displayName: "English", direction: "ltr", active: true, sortOrder: 1 },
];

test("editable languages come only from the authoritative registry and carry no invented name", () => {
  const defs = methodLanguageDefinitions([
    ...REGISTRY,
    // زبان بدون شناسهٔ واقعی قابل ذخیره نیست و باید کنار گذاشته شود.
    { languageId: "", code: "de-DE", nativeName: "Deutsch", direction: "ltr", active: true, sortOrder: 2 },
    { languageId: FA, code: "fa-IR", nativeName: "تکراری", direction: "rtl", active: true, sortOrder: 3 },
  ]);

  assert.deepEqual(defs.map((row) => row.languageId), [FA, EN]);
  assert.deepEqual(defs.map((row) => row.label), ["فارسی (ایران)", "English (US)"]);
  assert.deepEqual(defs.map((row) => row.direction), ["rtl", "ltr"]);
  // ترتیب سرور با sortOrder حفظ می‌شود، نه با مرتب‌سازی الفبایی محلی.
  const reordered = methodLanguageDefinitions([
    { languageId: EN, nativeName: "English (US)", sortOrder: 5 },
    { languageId: FA, nativeName: "فارسی (ایران)", sortOrder: 1 },
  ]);
  assert.deepEqual(reordered.map((row) => row.languageId), [FA, EN]);
  // برچسب وقتی nativeName نیست از displayName و سپس code می‌آید؛ هیچ نامی ساخته نمی‌شود.
  assert.equal(methodLanguageDefinitions([{ languageId: RU, displayName: "Русский" }])[0]?.label, "Русский");
  assert.equal(methodLanguageDefinitions([{ languageId: RU, code: "ru-RU" }])[0]?.label, "ru-RU");
  assert.equal(methodLanguageDefinitions([{ languageId: RU }])[0]?.label, RU);
});

test("a missing translation stays a visible gap instead of a fabricated name", () => {
  const defs = methodLanguageDefinitions(REGISTRY);
  const states = methodTranslationStates(
    [{ languageId: FA, name: " پیشتاز " }, { languageId: RU, name: "Чужой" }],
    defs,
    FA,
  );

  assert.equal(states[0]?.name, "پیشتاز");
  assert.equal(states[0]?.translated, true);
  assert.equal(states[0]?.isListLanguage, true);
  assert.equal(states[1]?.translated, false);
  assert.equal(states[1]?.name, "");
  assert.equal(states[1]?.isListLanguage, false);
  assert.deepEqual(methodTranslationSummary(states), { translated: 1, missing: 1, total: 2 });
  assert.deepEqual(methodTranslationSummary(methodTranslationStates([], defs, EN)), {
    translated: 0,
    missing: 2,
    total: 2,
  });
});

test("drafts are per language and switching language loses no unsaved text", () => {
  const defs = methodLanguageDefinitions(REGISTRY);
  const translations = [{ languageId: FA, name: "معمولی" }];

  const initial = methodNameDrafts(translations, defs);
  assert.deepEqual(initial, { [FA]: "معمولی", [EN]: "" });

  // ویرایش زبان انگلیسی، پیش‌نویس زبان فارسی را دست نمی‌زند.
  const edited = setMethodNameDraft(initial, EN, "Standard");
  assert.equal(methodNameDraftFor(edited, EN), "Standard");
  assert.equal(methodNameDraftFor(edited, FA), "معمولی");
  assert.deepEqual(dirtyMethodLanguages(edited, translations, defs), [EN]);

  // ویرایش فارسی روی پیش‌نویس انگلیسی ذخیره‌نشدهٔ خودش می‌نشیند، نه روی انگلیسی.
  const both = setMethodNameDraft(edited, FA, "معمولی جدید");
  assert.equal(methodNameDraftFor(both, FA), "معمولی جدید");
  assert.equal(methodNameDraftFor(both, EN), "Standard");
  assert.deepEqual(dirtyMethodLanguages(both, translations, defs), [FA, EN]);

  // زبان نامعتبر هیچ پیش‌نویس تازه و هیچ کلید ناشناخته‌ای نمی‌سازد.
  assert.deepEqual(setMethodNameDraft(both, "fa", "x"), both);
  assert.deepEqual(setMethodNameDraft(both, "", "x"), both);
  assert.equal(methodNameDraftFor(both, "not-a-guid"), "");
});

test("dirty detection compares against the real server value and ignores pure whitespace", () => {
  assert.equal(methodNameDraftIsDirty("معمولی", "معمولی"), false);
  assert.equal(methodNameDraftIsDirty("  معمولی  ", "معمولی"), false);
  assert.equal(methodNameDraftIsDirty("", ""), false);
  assert.equal(methodNameDraftIsDirty("معمولی جدید", "معمولی"), true);
  assert.equal(methodNameDraftIsDirty("", "معمولی"), true);
  assert.equal(methodNameDraftIsDirty(undefined as unknown as string, "معمولی"), true);
  assert.equal(serverMethodNameFor([{ languageId: FA, name: " معمولی " }], FA), "معمولی");
  assert.equal(serverMethodNameFor([{ languageId: FA, name: "معمولی" }], EN), "");
  assert.equal(serverMethodNameFor([{ languageId: FA, name: "معمولی" }], ""), "");
  // هیچ زبانی جز زبان‌های رجیستری در فهرست پیش‌نویس‌های ذخیره‌نشده ظاهر نمی‌شود.
  const defs = methodLanguageDefinitions(REGISTRY);
  assert.deepEqual(dirtyMethodLanguages({ [RU]: "x" }, [], defs), []);
});

test("the multilingual editor is bound to the authoritative registry and the method-scoped save seam", () => {
  const source = fs.readFileSync(new URL("./flat-method-language-editor.tsx", import.meta.url), "utf8");
  const editor = source.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  // تنها نوشتن: نام همین روش در همین زبان، از همان درز مجاز ۰۹۴.
  assert.match(editor, /saveOneLevelMethodName\(requestedCode, requestedLanguage, requestedName\)/);
  assert.doesNotMatch(editor, /serviceCode|optionCode|providerKind/);
  assert.doesNotMatch(editor, /writeAdminShippingService|deactivateAdminShippingService|ShippingServiceOptionWrite/);
  assert.doesNotMatch(editor, /createAdminShippingRate|updateAdminShippingRate|setAdminShippingRateActive/);
  // هیچ شناسه/نام زبان جعلی و هیچ ترجمهٔ ساختگی در کد اجرایی نیست.
  assert.doesNotMatch(editor, /"fa"|'fa'|"en"|'en'|"ru"|'ru'|fa-IR|en-US/);
  assert.doesNotMatch(editor, /fakeName|hardcodedName/);
  // نام ذخیره‌شده هرگز یک رشتهٔ ثابت نیست؛ همان پیش‌نویس واقعی کاربر ارسال می‌شود.
  assert.doesNotMatch(editor, /saveOneLevelMethodName\([^)]*"/);
  // زبان‌ها فقط از props رجیستری می‌آیند؛ خودِ ویرایشگر هیچ درخواست زبان دیگری نمی‌فرستد.
  assert.doesNotMatch(editor, /loadAdminLanguages|fetch\(/);
  // وضعیت‌های صریح بارگذاری/رد/خطا/خالی زبان وجود دارند و هیچ‌کدام زبان جعلی نمی‌سازند.
  for (const id of [
    "admin-shipping-method-languages-loading",
    "admin-shipping-method-languages-denied",
    "admin-shipping-method-languages-error",
    "admin-shipping-method-languages-empty",
  ]) {
    assert.match(editor, new RegExp(id));
  }
  // حالت مشاهده فقط-خواندنی است و ذخیره در آن ممکن نیست.
  assert.match(editor, /readOnly=\{readOnly\}/);
  assert.match(editor, /disabled=\{saving \|\| !canEdit \|\| !dirty\}/);
  assert.match(editor, /canEdit = !readOnly/);
  // پیش‌نویس‌های ذخیره‌نشده صریح نشان داده می‌شوند تا جابه‌جایی زبان باعث از دست رفتن متن نشود.
  assert.match(editor, /admin-shipping-method-name-unsaved/);
  assert.match(editor, /draftsOwner\.current === methodCode/);
});

test("the dialog discovers real languages from the authoritative Admin endpoint once per method", () => {
  const source = fs.readFileSync(new URL("./flat-method-detail-dialog.tsx", import.meta.url), "utf8");
  const dialog = source.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");

  assert.match(dialog, /loadAdminLanguages\(\)/);
  assert.match(dialog, /languages=\{languages\}/);
  assert.match(dialog, /languagesState=\{languagesState\}/);
  // سه زبانهٔ تخصصی و پیوند authoritative بدون تغییر باقی مانده‌اند.
  assert.match(dialog, /bindingOverride=\{bindingOverride\}/);
  assert.match(dialog, /loadOneLevelMethodBinding\(row\.methodCode\)/);
  // نام/وضعیت از دادهٔ تازهٔ سرور می‌آید تا پس از ذخیره بدون بستن دیالوگ به‌روز شود.
  assert.match(dialog, /method\?\.translations \?\? \[\]/);
  assert.match(dialog, /onSavedInPlace=\{onRefreshed\}/);
  assert.match(dialog, /onSavedAndClose=\{onSaved\}/);
  assert.doesNotMatch(dialog, /options-tab|service-types|انواع سرویس/);
});

test("the General tab is compact, honest about financial status and makes no fabricated readiness claim", () => {
  const source = fs.readFileSync(new URL("./flat-general-tab.tsx", import.meta.url), "utf8");
  const general = source.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/.*$/gm, "");
  const raw = source;

  // وضعیت فعال‌بودن و وضعیت مالی جداگانه و صریح نمایش داده می‌شوند.
  assert.match(raw, /admin-shipping-method-financial-status/);
  assert.match(raw, /admin-shipping-method-activation/);
  assert.match(raw, /admin-shipping-method-header/);
  assert.match(raw, /رایگان نیست/);
  // هیچ ادعای «آمادهٔ قیمت/استعلام» و هیچ محاسبهٔ مبلغ ساخته نمی‌شود؛ فقط نبود صریح پیکربندی مالی.
  // هر علت شکست پیام و اقدام مستقل خودش را دارد؛ «نبود داده» با «سرور در دسترس نیست» یکی نمی‌شود.
  assert.match(raw, /FINANCIAL_STATUS_UNAVAILABLE_FA/);
  assert.match(raw, /FINANCIAL_STATUS_UNREACHABLE_FA/);
  assert.match(raw, /FINANCIAL_STATUS_ROUTE_MISSING_FA/);
  assert.match(raw, /FINANCIAL_STATUS_DENIED_FA/);
  assert.match(raw, /FINANCIAL_STATUS_DISTINCTNESS_FA/);
  assert.match(raw, /admin-shipping-method-financial-unavailable/);
  assert.match(raw, /admin-shipping-method-financial-retry/);
  assert.doesNotMatch(general, /quoteReady|isQuoteReady|financialReady|computeFee|calcPrice|amount|toman|ریال/);
  // فرادادهٔ ارائه‌دهنده تاشو و برچسب‌دار است و ویرایش نمی‌شود.
  assert.match(general, /<details/);
  assert.match(general, /admin-shipping-method-shared-metadata/);
  // تنها ویرایشگر زبان از کامپوننت اختصاصی و مبتنی بر رجیستری می‌آید.
  assert.match(general, /FlatMethodLanguageEditor/);
  assert.doesNotMatch(general, /loadAdminLanguages|fetch\(/);
});
