import assert from "node:assert/strict";
import fs from "node:fs";
import test from "node:test";
import {
  activeOptionTranslationName,
  buildShippingServiceWriteRequest,
  mapShippingServiceErrorFa,
  normalizeShippingServiceCode,
  parseShippingServiceSortOrder,
  validateShippingServiceForm,
  type ShippingServiceFormFields,
} from "./shipping-services-form-model.ts";

const FA = "11111111-1111-4111-8111-111111111111";
const EN = "22222222-2222-4222-8222-222222222222";

function form(partial: Partial<ShippingServiceFormFields> = {}): ShippingServiceFormFields {
  return {
    code: "post",
    providerKind: "post",
    iconKey: "post",
    colorKey: "blue",
    isActive: true,
    sortOrder: "10",
    translations: [
      { languageId: FA, name: "پست" },
      { languageId: EN, name: "Post" },
    ],
    options: [
      {
        code: "express",
        isActive: true,
        sortOrder: 10,
        translations: [
          { languageId: FA, name: "پیشتاز" },
          { languageId: EN, name: "Express" },
        ],
      },
    ],
    ...partial,
  };
}

const context = { activeLanguageId: FA, otherServiceCodes: [] as string[] };

test("a complete form produces no issues", () => {
  assert.deepEqual(validateShippingServiceForm(form(), context), []);
});

test("service code is required, must stay canonical, and must be unique", () => {
  assert.equal(validateShippingServiceForm(form({ code: "   " }), context)[0]?.field, "code");

  const withColon = validateShippingServiceForm(form({ code: "post:express" }), context);
  assert.equal(withColon[0]?.field, "code");

  const withUpper = validateShippingServiceForm(form({ code: "Post Service" }), context);
  assert.equal(withUpper[0]?.field, "code");

  const duplicate = validateShippingServiceForm(form({ code: " POST " }), {
    activeLanguageId: FA,
    otherServiceCodes: ["tipax", "post"],
  });
  assert.equal(duplicate[0]?.field, "codeDuplicate");

  // کدی که خودِ سرویس ویرایش‌شونده دارد نباید تکراری شمرده شود (صفحه آن را از فهرست کنار می‌گذارد).
  const selfEdit = validateShippingServiceForm(form({ code: "post" }), {
    activeLanguageId: FA,
    otherServiceCodes: ["tipax"],
  });
  assert.deepEqual(selfEdit, []);
});

test("sort order must be a non-negative integer instead of silently becoming zero", () => {
  assert.equal(parseShippingServiceSortOrder("15"), 15);
  assert.equal(parseShippingServiceSortOrder(" 0 "), 0);
  assert.equal(parseShippingServiceSortOrder(""), null);
  assert.equal(parseShippingServiceSortOrder("abc"), null);
  assert.equal(parseShippingServiceSortOrder("-5"), null);
  assert.equal(parseShippingServiceSortOrder("1.5"), null);

  assert.equal(validateShippingServiceForm(form({ sortOrder: "abc" }), context)[0]?.field, "sortOrder");
  assert.deepEqual(validateShippingServiceForm(form({ sortOrder: "0" }), context), []);
});

test("a display name for the active language is required", () => {
  const onlyOtherLanguage = [
    { languageId: FA, name: "  " },
    { languageId: EN, name: "Post" },
  ];
  const issues = validateShippingServiceForm(form({ translations: onlyOtherLanguage }), context);
  assert.equal(issues[0]?.field, "serviceName");

  const tooLong = [{ languageId: FA, name: "x".repeat(129) }];
  assert.equal(validateShippingServiceForm(form({ translations: tooLong }), context)[0]?.field, "serviceName");
});

test("option rows are validated instead of being dropped silently", () => {
  const blankCode = form({
    options: [{ code: "  ", isActive: true, sortOrder: 10, translations: [{ languageId: FA, name: "پیشتاز" }] }],
  });
  assert.equal(validateShippingServiceForm(blankCode, context)[0]?.field, "optionCode");

  const duplicateOptions = form({
    options: [
      { code: "Express", isActive: true, sortOrder: 10, translations: [{ languageId: FA, name: "پیشتاز" }] },
      { code: "express", isActive: true, sortOrder: 20, translations: [{ languageId: FA, name: "سریع" }] },
    ],
  });
  const duplicateIssues = validateShippingServiceForm(duplicateOptions, context);
  assert.equal(duplicateIssues.length, 1);
  assert.equal(duplicateIssues[0]?.field, "optionCode");
  assert.equal(duplicateIssues[0]?.optionIndex, 1);

  const blankName = form({
    options: [{ code: "express", isActive: true, sortOrder: 10, translations: [{ languageId: FA, name: "" }] }],
  });
  const blankNameIssues = validateShippingServiceForm(blankName, context);
  assert.equal(blankNameIssues[0]?.field, "optionName");
  assert.equal(blankNameIssues[0]?.optionIndex, 0);
});

test("write request is canonical and keeps option identity", () => {
  const input = form({
    code: "  POST ",
    providerKind: " post ",
    sortOrder: " 25 ",
    translations: [
      { languageId: FA, name: " پست ", description: "  پست پیشتاز  " },
      { languageId: EN, name: "   " },
    ],
    options: [
      {
        shippingServiceOptionId: "33333333-3333-4333-8333-333333333333",
        code: " Express ",
        isActive: false,
        sortOrder: 30,
        translations: [
          { languageId: FA, name: " پیشتاز " },
          { languageId: EN, name: "" },
        ],
      },
    ],
  });

  const request = buildShippingServiceWriteRequest(input);
  assert.equal(request.code, "post");
  assert.equal(request.providerKind, "post");
  assert.equal(request.sortOrder, 25);
  assert.deepEqual(request.translations, [
    { languageId: FA, name: "پست", description: "پست پیشتاز" },
  ]);
  assert.deepEqual(request.options, [
    {
      shippingServiceOptionId: "33333333-3333-4333-8333-333333333333",
      code: "express",
      isActive: false,
      sortOrder: 30,
      translations: [{ languageId: FA, name: "پیشتاز" }],
    },
  ]);
});

test("active helpers resolve the active language only", () => {
  assert.equal(normalizeShippingServiceCode(" TIPAX "), "tipax");
  assert.equal(
    activeOptionTranslationName(
      { code: "express", isActive: true, sortOrder: 10, translations: [{ languageId: EN, name: "Express" }] },
      FA,
    ),
    "",
  );
});

test("known fulfillment failure codes map to fa and unknown codes fall back", () => {
  assert.match(mapShippingServiceErrorFa("shipping_service.code_duplicate", "fallback"), /قبلاً ثبت شده/);
  assert.equal(mapShippingServiceErrorFa("host-unreachable", "ذخیره ناموفق بود"), "ذخیره ناموفق بود");
  assert.equal(mapShippingServiceErrorFa(undefined, "ذخیره ناموفق بود"), "ذخیره ناموفق بود");
});

test("screen wires validation, page-level feedback, and confirmed row actions", () => {
  const screen = fs.readFileSync(
    new URL("../shipping-services-screen.tsx", import.meta.url),
    "utf8",
  );

  assert.match(screen, /validateShippingServiceForm\(/);
  assert.match(screen, /buildShippingServiceWriteRequest\(/);
  assert.match(screen, /AppGridRowActionsCell/);
  assert.match(screen, /confirm: \(row\) =>/);
  assert.match(screen, /data-testid="admin-shipping-services-notice"/);
  assert.match(screen, /data-testid="admin-shipping-service-form-errors"/);

  // حذف بی‌صدای مقادیر ناقص نباید برگردد.
  assert.doesNotMatch(screen, /translations\.filter\(\(t\) => t\.name\.trim\(\)\)/);
  assert.doesNotMatch(screen, /options\.filter\(\(o\) => o\.code\.trim\(\)\)/);
  assert.doesNotMatch(screen, /sortOrder: Number\(sortOrder\) \|\| 0/);
});
