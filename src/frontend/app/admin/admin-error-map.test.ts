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
  assert.ok(listMappedAdminErrorCodes().includes("return.expired"));
  assert.ok(listMappedAdminErrorCodes().includes("refund.already_completed"));
  assert.equal(
    mapAdminErrorMessage("fulfillment.pack.after_delivered", "fa"),
    "پس از تحویل کامل نمی‌توان بسته‌بندی کرد.",
  );
  assert.equal(
    mapAdminErrorMessage("fulfillment.process.after_delivered", "fa"),
    "پس از تحویل کامل نمی‌توان پردازش را ادامه داد.",
  );
  assert.equal(
    mapAdminErrorMessage("fulfillment.shipment.locked_by_consolidated_package", "fa"),
    "این مرسوله عضو بسته تجمیعی است و عملیات ارسال باید از طریق همان بسته انجام شود.",
  );
  assert.equal(
    mapAdminErrorMessage("fulfillment.package.requires_multi_seller", "en"),
    "A consolidated package requires at least two distinct sellers.",
  );
  assert.ok(listMappedAdminErrorCodes().includes("fulfillment.package.deliver_before_dispatch"));
});

test("courier-origin release and restock refusals are Persian, never machine codes", () => {
  const intentConflict = mapAdminErrorMessage("inventory.held_release.intent_conflict", "fa");
  assert.ok(/[\u0600-\u06ff]/.test(intentConflict));
  assert.ok(!intentConflict.includes("inventory."));
  assert.ok(!isTechnicalAdminErrorText(intentConflict));
  assert.equal(
    mapAdminErrorMessage("inventory.held_release.intent_conflict", "en"),
    "This release request does not match the recorded one (different reservation or quantity). Refresh and review again.",
  );
  const releaseMismatch = mapAdminErrorMessage("inventory.reservation.release_mismatch", "fa");
  assert.ok(/[\u0600-\u06ff]/.test(releaseMismatch));
  assert.ok(!releaseMismatch.includes("inventory."));
  const restock = mapAdminErrorMessage("inventory.return_restock.invalid", "fa");
  assert.ok(/[\u0600-\u06ff]/.test(restock));
  assert.ok(!restock.includes("inventory."));
  // A partial claim of a replacement parcel is supported now, so its old refusal copy must be gone.
  assert.ok(!listMappedAdminErrorCodes().includes("fulfillment.returns.replacement_partial_claim_unsupported"));
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

test("a missing admin order is presented as Persian copy, never as a machine code", () => {
  const fa = mapAdminErrorMessage("admin.order.missing", "fa");
  assert.equal(fa, "سفارشی با این شناسه پیدا نشد. ممکن است حذف شده باشد یا نشانی آن درست نباشد.");
  assert.ok(/[\u0600-\u06ff]/.test(fa));
  assert.ok(!isTechnicalAdminErrorText(fa));
  assert.equal(
    mapAdminErrorMessage("admin.order.missing", "en"),
    "No order was found with this identifier; it may have been removed or the address is wrong.",
  );
});

test("content article update and media errors are humanized", () => {
  assert.equal(
    mapAdminErrorMessage("localization.language.inactive", "fa"),
    "زبان انتخاب‌شده فعال نیست.",
  );
  assert.equal(
    mapAdminErrorMessage("content.category.language_mismatch", "fa"),
    "زبان دسته‌بندی باید با زبان مقاله یکسان باشد.",
  );
  assert.equal(
    mapAdminErrorMessage("content.article.media_not_found", "en"),
    "The selected media asset is unavailable or was not found.",
  );
  assert.equal(
    mapAdminErrorMessage("content.update.rejected", "fa"),
    "ذخیرهٔ مقاله انجام نشد. لطفاً ورودی‌ها را بررسی کنید.",
  );
  assert.equal(
    mapAdminErrorMessage("content.author.inactive", "fa"),
    "نویسندهٔ غیرفعال برای انتساب جدید مجاز نیست.",
  );
  assert.equal(
    mapAdminErrorMessage("landing.slug.reserved", "fa"),
    "این آدرس برای مسیرهای سامانه رزرو شده است. آدرس دیگری انتخاب کنید.",
  );
  assert.equal(
    mapAdminErrorMessage("landing.home.ineligible", "fa"),
    "فقط صفحهٔ منتشرشده را می‌توان خانه کرد.",
  );
  assert.equal(
    parseAdminProblemErrorCode(
      {
        title: "Bad Request",
        errorCode: "content.update.rejected",
        detail: "localization.language.inactive",
      },
      400,
    ),
    "localization.language.inactive",
  );
  assert.ok(!mapAdminErrorMessage("content.update.rejected", "fa").includes("content.update"));
  assert.ok(!mapAdminErrorMessage("Bad Request", "fa").includes("Bad Request"));
});
