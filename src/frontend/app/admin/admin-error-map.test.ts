import assert from "node:assert/strict";
import test from "node:test";
import {
  extractAdminErrorCode,
  isTechnicalAdminErrorText,
  listMappedAdminErrorCodes,
  mapAdminErrorMessage,
  normalizeAdminClientError,
  parseAdminProblemErrorCode,
  unknownAdminErrorMessage,
} from "./admin-error-map.ts";

test("maps known catalog attribute duplicate codes in fa and en", () => {
  assert.equal(
    mapAdminErrorMessage("catalog.attribute.name.duplicate", "fa"),
    "ویژگی‌ای با این نام قبلاً وجود دارد.",
  );
  assert.equal(
    mapAdminErrorMessage("catalog.attribute.code.duplicate", "en"),
    "This attribute code is already in use.",
  );
  assert.equal(
    mapAdminErrorMessage("catalog.category.attribute.invalid", "fa"),
    "اطلاعات واردشده معتبر نیست.",
  );
  assert.equal(
    mapAdminErrorMessage("catalog.product.category.level.invalid", "fa"),
    "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.",
  );
  assert.equal(
    mapAdminErrorMessage("catalog.category.assignment.level.invalid", "fa"),
    "محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.",
  );
  assert.equal(
    mapAdminErrorMessage("catalog.category.assignment.level.invalid", "en"),
    "A product must be assigned to a level-3 category.",
  );
  assert.equal(
    mapAdminErrorMessage("admin.authorization.denied", "en"),
    "You are not allowed to perform this action.",
  );
  assert.equal(
    mapAdminErrorMessage("host-unreachable", "fa"),
    "اتصال به سرویس برقرار نیست. لطفاً دوباره تلاش کنید.",
  );
  assert.equal(
    mapAdminErrorMessage("media.type.unsupported", "fa"),
    "نوع فایل رسانه پشتیبانی نمی‌شود. فقط تصویر JPEG، PNG، WebP یا GIF مجاز است.",
  );
  assert.equal(
    mapAdminErrorMessage("media.too_large", "en"),
    "The file exceeds the allowed size limit.",
  );
  assert.ok(listMappedAdminErrorCodes().includes("media.missing"));
  assert.ok(listMappedAdminErrorCodes().includes("media.storage.unavailable"));
});

test("unknown fallback never exposes Bad Request / HTTP / raw codes", () => {
  assert.equal(mapAdminErrorMessage("Bad Request", "fa"), unknownAdminErrorMessage("fa"));
  assert.equal(mapAdminErrorMessage("HTTP 400", "en"), unknownAdminErrorMessage("en"));
  assert.equal(mapAdminErrorMessage("admin.http.500", "fa"), unknownAdminErrorMessage("fa"));
  assert.equal(mapAdminErrorMessage("Some.Unknown.Exception", "fa"), unknownAdminErrorMessage("fa"));
  assert.equal(isTechnicalAdminErrorText("Bad Request"), true);
  assert.equal(isTechnicalAdminErrorText("host-grid-http-409"), true);
  assert.ok(!mapAdminErrorMessage(null, "fa").includes("Bad Request"));
  assert.ok(!mapAdminErrorMessage("catalog.attribute.code.duplicate", "fa").includes("catalog."));
});

test("extracts code from legacy title (code) payloads", () => {
  assert.equal(
    extractAdminErrorCode("Conflict (catalog.attribute.code.duplicate)"),
    "catalog.attribute.code.duplicate",
  );
  assert.ok(listMappedAdminErrorCodes().includes("workspace.product.category.level.invalid"));
  assert.ok(listMappedAdminErrorCodes().includes("catalog.schema.invalid"));
  assert.ok(listMappedAdminErrorCodes().includes("catalog.facet.invalid"));
});

test("parseAdminProblemErrorCode prefers stable errorCode never title", () => {
  assert.equal(
    parseAdminProblemErrorCode({ title: "Bad Request", errorCode: "catalog.facet.invalid" }, 400),
    "catalog.facet.invalid",
  );
  assert.equal(parseAdminProblemErrorCode({ title: "Bad Request" }, 400), "admin.http.400");
  assert.equal(
    normalizeAdminClientError({ title: "Bad Request", errorCode: "catalog.attribute.code.duplicate" }, 409, "fa"),
    "این کد ویژگی قبلاً استفاده شده است.",
  );
  assert.ok(!normalizeAdminClientError({ title: "Bad Request" }, 400, "fa").includes("Bad Request"));
});
