import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const screen = readFileSync(join(import.meta.dirname, "content-article-admin-screen.tsx"), "utf8");
const picker = readFileSync(join(import.meta.dirname, "content-article-category-picker.tsx"), "utf8");
const tagsPanel = readFileSync(join(import.meta.dirname, "content-article-tags-panel.tsx"), "utf8");
const categoryAdmin = readFileSync(join(import.meta.dirname, "content-category-admin-screen.tsx"), "utf8");
const errorMap = readFileSync(join(import.meta.dirname, "admin-error-map.ts"), "utf8");
const ck = readFileSync(join(import.meta.dirname, "content-article-ckeditor.tsx"), "utf8");

test("article category picker is hierarchical searchable without flat select/raw ids", () => {
  assert.match(screen, /ContentArticleCategoryPicker/);
  assert.match(picker, /buildContentArticleCategoryOptions/);
  assert.match(picker, /›/);
  assert.match(picker, /دسته اصلی/);
  assert.match(picker, /زیردسته/);
  assert.match(picker, /content-article-category-picker/);
  assert.doesNotMatch(screen, /content-article-category-select/);
  assert.doesNotMatch(picker, /option value=\{row\.id\}/);
});

test("article tags use searchable chips without CSV input", () => {
  assert.match(screen, /ContentArticleTagsPanel/);
  assert.match(tagsPanel, /\$\{testIdPrefix\}-chips/);
  assert.match(tagsPanel, /searchContentTags/);
  assert.match(tagsPanel, /createContentTag/);
  assert.match(tagsPanel, /assignContentArticleTag/);
  assert.doesNotMatch(screen, /برچسب‌ها \(با ویرگول\)/);
  assert.doesNotMatch(screen, /tagsFromString|tagsToString|draftTags/);
});

test("content category admin enforces two-level UX labels and maxDepth", () => {
  assert.match(categoryAdmin, /MAX_CONTENT_CATEGORY_DEPTH/);
  assert.match(categoryAdmin, /maxDepth=\{MAX_CONTENT_CATEGORY_DEPTH\}/);
  assert.match(categoryAdmin, /دسته اصلی/);
  assert.match(categoryAdmin, /زیردسته/);
  assert.match(categoryAdmin, /allowDrag=\{false\}/);
});

test("admin error map covers category depth and tag language/duplicate", () => {
  assert.match(errorMap, /content\.category\.max_depth_exceeded/);
  assert.match(errorMap, /content\.category\.language_mismatch/);
  assert.match(errorMap, /content\.tag\.duplicate_name/);
  assert.match(errorMap, /content\.tag\.language_mismatch/);
  assert.match(errorMap, /دسته‌بندی مقاله حداکثر می‌تواند دو سطح داشته باشد/);
  assert.match(errorMap, /این برچسب قبلاً ثبت شده است/);
});

test("CKEditor article editor remains after taxonomy/tags work", () => {
  assert.match(screen, /ContentArticleEditor/);
  assert.doesNotMatch(screen, /@tiptap/);
  assert.match(ck, /data-editor="ckeditor5"/);
  assert.match(ck, /ClassicEditor/);
  assert.doesNotMatch(ck, /CKBox|CloudServices|EasyImage|Base64UploadAdapter/);
});
