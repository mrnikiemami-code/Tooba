import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  cancelAdminEditMode,
  createAdminFormModeState,
  enterAdminEditMode,
  markAdminFormDirty,
} from "../../design-system/admin-form-mode/use-admin-form-mode.ts";
import {
  buildTranslationStatuses,
  resolveTranslationReadiness,
  translationReadinessLabel,
} from "../../design-system/app-category-tree/tree-model.ts";
import {
  buildStorefrontCategoryRoute,
  CATEGORY_SLUG_DUPLICATE_MESSAGE,
  mapCategoryMutationError,
  slugifyCategoryName,
  slugLooksLikeIdSuffixed,
} from "./catalog-category-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const screenPath = path.join(root, "app/admin/category-admin-screen.tsx");
const apiPath = path.join(root, "app/admin/catalog-category-api.ts");
const treePath = path.join(root, "design-system/app-category-tree/AppCategoryTree.tsx");

test("General VIEW default markers and readable cards", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /data-form-mode=\{categoryId \? formMode\.mode : undefined\}/);
  assert.match(screen, /formMode\.resetToView/);
  assert.match(screen, /category-general-summary/);
  assert.match(screen, /نام دسته در زبان فعلی/);
  assert.match(screen, /دسته والد/);
  assert.match(screen, /وضعیت ترجمه‌ها/);
  assert.match(screen, /آدرس عمومی دسته/);
  assert.equal(screen.includes('data-form-mode="view"'), true);
  const viewBlock = screen.slice(
    screen.indexOf("function GeneralViewSummary"),
    screen.indexOf("function GeneralEditForm"),
  );
  assert.equal(viewBlock.includes("disabled"), false);
  assert.equal(viewBlock.includes("<input"), false);
});

test("General EDIT fields and save/cancel", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /category-general-edit/);
  assert.match(screen, /category-edit-name/);
  assert.match(screen, /category-edit-slug/);
  assert.match(screen, /category-edit-status/);
  assert.match(screen, /category-edit-sort-order/);
  assert.match(screen, /category-edit-visible/);
  assert.match(screen, /category-edit-parent/);
  assert.match(screen, /category-edit-save/);
  assert.match(screen, /category-edit-cancel/);
  assert.match(screen, /handleEnterGeneralEdit/);
  assert.match(screen, /handleCancelGeneralEdit/);
  assert.match(screen, /handleSaveGeneral/);
  assert.match(screen, /formMode\.onEdit/);
  assert.match(screen, /formMode\.onCancel/);
  assert.match(screen, /formMode\.onSaved/);
  assert.match(screen, /confirmDiscardIfDirty/);
});

test("status localization uses Persian labels in workspace", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /پیش‌نویس/);
  assert.match(screen, /منتشرشده/);
  assert.match(screen, /بایگانی‌شده/);
  assert.match(screen, /function workspaceStatusLabel/);
  // raw English enum keys must not be primary UI labels in options text
  assert.match(screen, /<option value="Draft">پیش‌نویس<\/option>/);
  assert.match(screen, /<option value="Published">منتشرشده<\/option>/);
  assert.match(screen, /<option value="Archived">بایگانی‌شده<\/option>/);
});

test("parent selector is searchable path/name without raw ID labels", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /ParentCategorySelector/);
  assert.match(screen, /category-edit-parent-search/);
  assert.match(screen, /buildCategoryPath\(flatNodes, n\.id\)\.join/);
  assert.match(screen, /collectDescendantIds/);
  assert.match(screen, /blocked\.add\(categoryId\)/);
  // options use path labels; hidden value may hold id but labels are paths/names
  assert.match(screen, /جستجوی نام یا مسیر/);
  assert.equal(screen.includes("option value={opt.id"), false);
});

test("media uses clean به‌زودی placeholder without fake upload", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /MediaPlaceholder/);
  assert.match(screen, /انتخاب رسانه — به‌زودی/);
  assert.equal(screen.includes("type=\"file\""), false);
  assert.equal(screen.includes("FormData"), false);
});

test("permission-aware edit action gated by canEdit", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /headerEditVisible/);
  assert.match(screen, /formMode\.canEdit/);
  assert.match(screen, /useAdminFormMode\(\{ canView, canEdit \}\)/);
  assert.match(screen, /canEdit=\{formMode\.canEdit\}/);
});

test("Translations tab: locale switcher, statuses, create, VIEW/EDIT", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /category-translations-panel/);
  assert.match(screen, /category-locale-switcher/);
  assert.match(screen, /translation-locale-/);
  assert.match(screen, /ایجاد نشده|translationReadinessLabel/);
  assert.match(screen, /category-translation-missing/);
  assert.match(screen, /category-translation-create/);
  assert.match(screen, /ایجاد ترجمه/);
  assert.match(screen, /category-translation-view/);
  assert.match(screen, /category-translation-edit/);
  assert.match(screen, /translation-edit-name/);
  assert.match(screen, /translation-edit-slug/);
  assert.match(screen, /translation-edit-short-description/);
  assert.match(screen, /translation-edit-description/);
  assert.match(screen, /translation-edit-seo-title/);
  assert.match(screen, /translation-edit-save/);
  assert.match(screen, /translation-edit-cancel/);
  assert.match(screen, /handleSelectLocale/);
  assert.match(screen, /handleCreateTranslation/);
  assert.match(screen, /handleSaveTranslation/);
  assert.match(screen, /این یک دسته‌بندی است با چند نسخهٔ زبانی/);
  assert.match(screen, /LOCALE_DISPLAY/);
  assert.match(screen, /فارسی/);
  assert.match(screen, /English/);
  assert.match(screen, /العربية/);
});

test("translation completeness: missing / partial / complete", () => {
  assert.equal(resolveTranslationReadiness("", ""), "missing");
  assert.equal(resolveTranslationReadiness("نام", ""), "partial");
  assert.equal(resolveTranslationReadiness("", "slug"), "partial");
  assert.equal(resolveTranslationReadiness("نام", "slug"), "complete");
  assert.equal(translationReadinessLabel("missing"), "ایجاد نشده");
  assert.equal(translationReadinessLabel("partial"), "ناقص");
  assert.equal(translationReadinessLabel("complete"), "کامل");

  const statuses = buildTranslationStatuses(
    [
      { locale: "fa-IR", name: "کتاب", slug: "ketab" },
      { locale: "en-US", name: "Books", slug: "" },
    ],
    ["fa-IR", "en-US", "ar-SA"],
  );
  assert.equal(statuses[0]?.readiness, "complete");
  assert.equal(statuses[1]?.readiness, "partial");
  assert.equal(statuses[2]?.readiness, "missing");
  assert.equal(statuses[0]?.label, "فارسی");
  assert.equal(statuses[1]?.label, "English");
  assert.equal(statuses[2]?.label, "العربية");
});

test("dirty locale-switch protection", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /handleSelectLocale/);
  assert.match(
    screen,
    /if \(formMode\.isDirty && !formMode\.confirmDiscardIfDirty\(\)\) return;/,
  );
  // tab switch + category navigate also protected
  assert.match(screen, /navigateToCategory/);
});

test("human slug preview has no CategoryId suffix", () => {
  const slug = slugifyCategoryName("گوشی موبایل");
  assert.equal(slug, "گوشی-موبایل");
  const route = buildStorefrontCategoryRoute("fa", slug);
  assert.equal(route, "/fa/category/گوشی-موبایل");
  assert.equal(slugLooksLikeIdSuffixed(slug), false);
  assert.equal(route.includes("01a03826"), false);

  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /buildStorefrontCategoryRoute/);
  assert.match(screen, /category-route-preview/);
  assert.match(screen, /translation-route-preview/);
  assert.equal(/category\/\$\{.*Id/.test(screen), false);
  // never append visible CategoryId suffix into public route builders
  assert.equal(screen.includes("/category/${") && screen.includes("categoryId}"), false);
  assert.equal(screen.includes("slug +") && screen.toLowerCase().includes("categoryid"), false);
});

test("slug auto-suggest respects manual edits", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /slugTouched \? draft\.slug : slugifyCategoryName/);
  assert.match(screen, /slugTouched \? activeDraft\.slug : slugifyCategoryName/);
  assert.match(screen, /slugifyCategoryName/);
});

test("duplicate slug maps to Persian UX message", () => {
  assert.equal(
    mapCategoryMutationError({ message: "catalog.category.slug.duplicate" }),
    CATEGORY_SLUG_DUPLICATE_MESSAGE,
  );
  assert.equal(
    mapCategoryMutationError({ message: "slug رده برای این locale تکراری است." }),
    CATEGORY_SLUG_DUPLICATE_MESSAGE,
  );
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /category-slug-error/);
  assert.match(screen, /translation-slug-error/);
  assert.match(screen, /mapCategoryMutationError/);
});

test("form mode state machine: view default, edit, cancel, save", () => {
  let s = createAdminFormModeState(true, true);
  assert.equal(s.mode, "view");
  s = enterAdminEditMode(s);
  assert.equal(s.mode, "edit");
  s = markAdminFormDirty(s);
  assert.equal(s.isDirty, true);
  s = cancelAdminEditMode(s);
  assert.equal(s.mode, "view");
  assert.equal(s.isDirty, false);
});

test("view-only user cannot enter edit via state machine", () => {
  const s = createAdminFormModeState(true, false);
  assert.equal(enterAdminEditMode(s).mode, "view");
});

test("tree context preservation after current-locale Name save", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /softRefreshTreeLabel/);
  assert.match(screen, /setFlatNodes\(\(prev\) =>/);
  assert.match(screen, /expandedKeys=\{expandedKeys\}/);
  assert.match(screen, /selectedKeys=\{categoryId \? \[categoryId\] : \[\]\}/);
  const tree = fs.readFileSync(treePath, "utf8");
  assert.match(tree, /category-tree-drag-handle/);
});

test("AppCategoryTree file untouched by T006 workspace work", () => {
  // contract: no redesign — file still exports drag handle / search patterns from approved baseline
  const tree = fs.readFileSync(treePath, "utf8");
  assert.match(tree, /category-tree-search/);
  assert.match(tree, /category-tree-drag-handle/);
  assert.match(tree, /direction/);
});

test("future tabs remain progressive placeholders", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /category-tab-coming-soon/);
  assert.match(screen, /این بخش در تسک بعدی تکمیل می‌شود/);
  assert.match(screen, /implemented: false/);
});

test("mobile layout markers present", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /data-layout=\{isNarrow \? "mobile" : "desktop"\}/);
  assert.match(screen, /category-workspace-back/);
  assert.match(screen, /sticky bottom-0/);
});

test("no raw AgGridReact in category admin screen", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.equal(screen.includes("AgGridReact"), false);
  assert.equal(screen.includes("AppDataGrid"), false);
});

test("API client exposes updateCore, upsertTranslation with full fields, move", () => {
  const api = fs.readFileSync(apiPath, "utf8");
  assert.match(api, /export async function updateCategoryCore/);
  assert.match(api, /export async function upsertCategoryTranslation/);
  assert.match(api, /export async function moveCategory/);
  assert.match(api, /shortDescription/);
  assert.match(api, /seoTitle/);
  assert.match(api, /metaKeywords/);
  assert.match(api, /CATEGORY_SLUG_DUPLICATE_ERROR_CODE/);
  assert.match(api, /buildStorefrontCategoryRoute/);
});
