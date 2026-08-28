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
  buildStorefrontCategoryRoute,
  CATEGORY_SLUG_DUPLICATE_MESSAGE,
  mapCategoryMutationError,
  slugifyCategoryName,
  slugLooksLikeIdSuffixed,
} from "./catalog-category-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const screenPath = path.join(root, "app/admin/category-admin-screen.tsx");

test("workspace defaults to VIEW mode markers", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /data-form-mode=\{categoryId \? formMode\.mode : undefined\}/);
  assert.match(screen, /formMode\.resetToView/);
  assert.match(screen, /data-form-mode="view"/);
  assert.match(screen, /category-edit-action/);
  assert.match(screen, /ویرایش/);
});

test("view presentation is readable cards not disabled inputs", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /category-general-summary/);
  assert.match(screen, /SummaryCard label="نام"/);
  assert.equal(screen.includes('data-form-mode="view"'), true);
  assert.match(screen, /category-general-edit/);
  assert.match(screen, /category-edit-save/);
  assert.match(screen, /category-edit-cancel/);
  // VIEW surface must not use disabled inputs as the presentation pattern
  const viewBlock = screen.slice(
    screen.indexOf("function GeneralViewSummary"),
    screen.indexOf("function GeneralEditForm"),
  );
  assert.equal(viewBlock.includes("disabled"), false);
  assert.equal(viewBlock.includes("<input"), false);
});

test("edit enter / save / cancel wiring present", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /handleEnterEdit/);
  assert.match(screen, /handleCancelEdit/);
  assert.match(screen, /handleSave/);
  assert.match(screen, /formMode\.onEdit/);
  assert.match(screen, /formMode\.onCancel/);
  assert.match(screen, /formMode\.onSaved/);
  assert.match(screen, /confirmDiscardIfDirty/);
});

test("permission-aware edit action gated by canEdit", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /!isEdit && formMode\.canEdit/);
  assert.match(screen, /useAdminFormMode\(\{ canView, canEdit \}\)/);
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
  assert.equal(/category\/\$\{.*Id/.test(screen), false);
  assert.equal(screen.includes("slug +") && screen.includes("categoryId"), false);
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

test("tree context preservation: save updates node label without remounting tree props pattern", () => {
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /setFlatNodes\(\(prev\) =>/);
  assert.match(screen, /expandedKeys=\{expandedKeys\}/);
  assert.match(screen, /selectedKeys=\{categoryId \? \[categoryId\] : \[\]\}/);
  // AppCategoryTree file must remain untouched by this repair
  const tree = fs.readFileSync(
    path.join(root, "design-system/app-category-tree/AppCategoryTree.tsx"),
    "utf8",
  );
  assert.match(tree, /category-tree-drag-handle/);
});

test("API client exposes updateCore and upsertTranslation", () => {
  const api = fs.readFileSync(path.join(root, "app/admin/catalog-category-api.ts"), "utf8");
  assert.match(api, /export async function updateCategoryCore/);
  assert.match(api, /export async function upsertCategoryTranslation/);
  assert.match(api, /CATEGORY_SLUG_DUPLICATE_ERROR_CODE/);
});
