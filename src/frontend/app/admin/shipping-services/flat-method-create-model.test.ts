import assert from "node:assert/strict";
import test from "node:test";
import {
  MAX_METHOD_CODE_LENGTH,
  MAX_METHOD_NAME_LENGTH,
  createMethodErrorMessage,
  mapCreatedMethod,
  validateCreateMethodInput,
} from "./flat-method-create-model.ts";

const FA = "01900000-0000-7000-8000-00000000aa01";
const EN = "01900000-0000-7000-8000-00000000aa02";

test("builds a request with the canonical code, trimmed names and no client binding", () => {
  const result = validateCreateMethodInput({
    methodCode: "  tipax:sameday  ",
    providerKind: "",
    names: [
      { languageId: FA, name: "  همان‌روز  " },
      { languageId: EN, name: "   " },
    ],
  });

  assert.equal(result.ok, true);
  if (!result.ok) return;
  assert.deepEqual(result.request, {
    methodCode: "tipax:sameday",
    translations: [{ languageId: FA, name: "همان‌روز" }],
  });
  // The client never sends a legacy binding or activation.
  assert.equal("serviceCode" in result.request, false);
  assert.equal("optionCode" in result.request, false);
  assert.equal("isActive" in result.request, false);
});

test("keeps explicit provider metadata when supplied", () => {
  const result = validateCreateMethodInput({
    methodCode: "mahex",
    providerKind: "mahex",
    names: [{ languageId: FA, name: "ماهکس" }],
  });
  assert.equal(result.ok, true);
  if (!result.ok) return;
  assert.equal(result.request.providerKind, "mahex");
});

test("rejects a blank or over-long canonical code", () => {
  const blank = validateCreateMethodInput({ methodCode: "   ", providerKind: "", names: [{ languageId: FA, name: "x" }] });
  assert.equal(blank.ok, false);
  if (!blank.ok) assert.equal(blank.field, "code");

  const long = validateCreateMethodInput({
    methodCode: "a".repeat(MAX_METHOD_CODE_LENGTH + 1),
    providerKind: "",
    names: [{ languageId: FA, name: "x" }],
  });
  assert.equal(long.ok, false);
  if (!long.ok) assert.equal(long.field, "code");
});

test("rejects ambiguous code separators", () => {
  for (const code of ["tipax:sameday:extra", "tipax:", ":sameday", "tipax sameday", "tipax/sameday"]) {
    const result = validateCreateMethodInput({ methodCode: code, providerKind: "", names: [{ languageId: FA, name: "x" }] });
    assert.equal(result.ok, false, code);
    if (!result.ok) assert.equal(result.field, "code", code);
  }
});

test("rejects non-canonical provider metadata", () => {
  const result = validateCreateMethodInput({
    methodCode: "tipax:sameday",
    providerKind: "Tipax Express",
    names: [{ languageId: FA, name: "x" }],
  });
  assert.equal(result.ok, false);
  if (!result.ok) assert.equal(result.field, "provider");
});

test("requires at least one real language name and bounds its width", () => {
  const empty = validateCreateMethodInput({
    methodCode: "tipax:sameday",
    providerKind: "",
    names: [{ languageId: FA, name: "  " }],
  });
  assert.equal(empty.ok, false);
  if (!empty.ok) assert.equal(empty.field, "name");

  const long = validateCreateMethodInput({
    methodCode: "tipax:sameday",
    providerKind: "",
    names: [{ languageId: FA, name: "ا".repeat(MAX_METHOD_NAME_LENGTH + 1) }],
  });
  assert.equal(long.ok, false);
  if (!long.ok) assert.equal(long.field, "name");
});

test("maps only a server response with a stable identity", () => {
  assert.equal(mapCreatedMethod(null), null);
  assert.equal(mapCreatedMethod({ providerKind: "tipax" }), null);

  const mapped = mapCreatedMethod({
    methodCode: "tipax:sameday",
    providerKind: "tipax",
    legacyServiceCode: "tipax",
    legacyOptionCode: "sameday",
    isActive: false,
    sortOrder: 61,
    translations: [{ languageId: FA, name: "همان‌روز" }],
  });

  assert.ok(mapped);
  assert.equal(mapped.methodCode, "tipax:sameday");
  assert.equal(mapped.legacyOptionCode, "sameday");
  assert.equal(mapped.isActive, false);
  assert.equal(mapped.sortOrder, 61);
  assert.deepEqual(mapped.translations, [{ languageId: FA, name: "همان‌روز" }]);
});

test("maps stable server failures to Persian messages", () => {
  assert.match(createMethodErrorMessage("shipping_method.code.duplicate", 400), /از قبل وجود دارد/);
  assert.match(createMethodErrorMessage("shipping_method.binding.duplicate", 400), /روش دیگری/);
  assert.match(createMethodErrorMessage("admin.authorization.denied", 403), /مجوز/);
  assert.match(createMethodErrorMessage("unknown", 500), /خطا/);
});
