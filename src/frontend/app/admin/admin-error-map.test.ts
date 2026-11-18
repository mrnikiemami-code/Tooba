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

test("TB-276: every proven no-effect refusal the reviewed workflow can receive is localized", async () => {
  const { PROVEN_NO_EFFECT_REFUSAL_CODES } = await import("../returns/return-review-intent.ts");
  const unknownFa = unknownAdminErrorMessage("fa");
  const missing: string[] = [];
  for (const code of PROVEN_NO_EFFECT_REFUSAL_CODES) {
    const fa = mapAdminErrorMessage(code, "fa");
    if (fa === unknownFa || fa.includes(code) || !/[\u0600-\u06ff]/.test(fa)) missing.push(code);
  }
  assert.deepEqual(missing, []);
});

test("TB-279-R2: every operator refusal of the correction/resolution/custody/allocation surface is localized", () => {
  const unknownFa = unknownAdminErrorMessage("fa");
  const codes = [
    "return.resolution.required",
    "return.resolution.incomplete",
    "return.resolution.invalid",
    "return.resolution.received_exceeded",
    "return.resolution.state_invalid",
    "return.resolution.version_stale",
    "return.resolution.actor_required",
    "return.resolution.item_invalid",
    "return.resolution.portion_required",
    "return.resolution.portion_invalid",
    "return.resolution.review_version_required",
    "return.correction.not_applicable",
    "return.correction.state_invalid",
    "return.correction.financial_effect_recorded",
    "return.correction.custody_recorded",
    "return.correction.physical_effect_recorded",
    "return.correction.source_effect_recorded",
    "return.correction.source_unsupported",
    "return.correction.capacity_conflict",
    "return.correction.actor_required",
    "return.correction.reason_required",
    "return.correction.version_stale",
    "return.correction.occurrence_conflict",
    "return.custody.item_invalid",
    "return.custody.quantity_invalid",
    "return.custody.proof_required",
    "return.custody.recipient_invalid",
    "return.custody.over_handed",
    "return.custody.state_invalid",
    "return.custody.occurrence_conflict",
    "return.custody.actor_required",
    "return.custody.proof_kind_required",
    "return.custody.proof_kind_unavailable",
    "return.allocation.required",
    "return.allocation.exceeded",
    "return.allocation.stale",
    "return.allocation.legacy_ambiguous",
    "return.refund.budget_exceeded",
    "return.refund.budget_binding_invalid",
    "return.refund.review_version_required",
    "return.approved_amount.conflict",
  ];
  const missing: string[] = [];
  for (const code of codes) {
    const fa = mapAdminErrorMessage(code, "fa");
    const en = mapAdminErrorMessage(code, "en");
    if (fa === unknownFa || fa.includes("return.") || !/[\u0600-\u06ff]/.test(fa)) missing.push(`${code}:fa`);
    if (en.includes("return.") || !/[A-Za-z]/.test(en)) missing.push(`${code}:en`);
  }
  assert.deepEqual(missing, []);
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
