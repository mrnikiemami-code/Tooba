import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const api = readFileSync(join(import.meta.dirname, "../content/content-api.ts"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-article-admin-screen.tsx"), "utf8");
const list = readFileSync(join(import.meta.dirname, "content-list.tsx"), "utf8");
const newScreen = readFileSync(join(import.meta.dirname, "content-article-new-screen.tsx"), "utf8");
const editor = readFileSync(join(import.meta.dirname, "content-article-rich-text-editor.tsx"), "utf8");

test("content article api exposes workspace load/update and locale helpers", () => {
  assert.match(api, /loadAdminArticle/);
  assert.match(api, /updateAdminArticle/);
  assert.match(api, /isArticleLocaleLocked/);
  assert.match(api, /articleEditorDirection/);
  assert.match(api, /\/v1\/admin\/content\/articles\//);
  assert.match(api, /seoImageMediaAssetId/);
  assert.match(api, /coverMediaAssetId/);
});

test("content article admin workspace uses TipTap content editor, tabs, and DAM", () => {
  assert.match(screen, /ContentArticleRichTextEditor/);
  assert.doesNotMatch(screen, /ProductRichTextEditor/);
  assert.match(screen, /MediaLibraryDialog/);
  assert.match(screen, /useAdminFormMode/);
  assert.match(screen, /content-article-tab-/);
  assert.match(screen, /دسته‌بندی‌ها/);
  assert.match(screen, /loadAdminArticle/);
  assert.match(screen, /loadAdminLanguages/);
  assert.match(screen, /content-article-author-filter/);
  assert.match(screen, /زبان این مقاله به‌دلیل وجود محتوا یا وابستگی‌های ثبت‌شده قابل تغییر نیست/);
  assert.doesNotMatch(screen, /LANGUAGE_OPTIONS/);
  assert.doesNotMatch(screen, /__articleDamPickResolve/);
  assert.match(screen, /damPickResolveRef/);
  assert.match(screen, /content-article-workspace-header/);
  assert.match(screen, /تصویر اشتراک‌گذاری و شبکه‌های اجتماعی/);
  assert.match(screen, /استفاده از تصویر شاخص مقاله/);
  assert.match(screen, /setDraftCategoryId\(""\)/);
});

test("content article rich text editor has professional toolbar contract", () => {
  assert.match(editor, /data-content-editor="article"/);
  assert.match(editor, /toggleStrike/);
  assert.match(editor, /toggleHeading\(\{ level: 4 \}\)/);
  assert.match(editor, /block-style/);
  assert.match(editor, /transformPastedHTML/);
  assert.match(editor, /sanitizeArticleRichHtml/);
  assert.match(editor, /insert-image/);
  assert.match(editor, /min-h-\[22rem\]/);
  assert.doesNotMatch(editor, /ckeditor/i);
});

test("content list links to language-first create and article workspace edit", () => {
  assert.match(list, /\/admin\/content\/articles\/new/);
  assert.match(list, /\/admin\/content\/articles\/\$\{/);
  assert.match(list, /\?mode=edit/);
  assert.match(list, /formatArticleLocaleLabel/);
  assert.match(list, /\?language=/);
  assert.doesNotMatch(list, /showCreate/);
  assert.doesNotMatch(newScreen, /LANGUAGE_OPTIONS/);
  assert.match(newScreen, /loadAdminLanguages/);
  assert.match(newScreen, /createAdminArticle/);
  assert.match(newScreen, /mapAdminErrorMessage/);
});

test("content list maps grid error keys to Persian friendly detail", () => {
  assert.match(list, /contentListGridErrorDetail/);
  assert.match(list, /mapAdminErrorMessage/);
});
