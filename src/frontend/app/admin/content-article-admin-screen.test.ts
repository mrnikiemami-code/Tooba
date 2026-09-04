import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";

const api = readFileSync(join(import.meta.dirname, "../content/content-api.ts"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-article-admin-screen.tsx"), "utf8");
const list = readFileSync(join(import.meta.dirname, "content-list.tsx"), "utf8");
const newScreen = readFileSync(join(import.meta.dirname, "content-article-new-screen.tsx"), "utf8");
const editor = readFileSync(join(import.meta.dirname, "content-article-editor.tsx"), "utf8");
const ck = readFileSync(join(import.meta.dirname, "content-article-ckeditor.tsx"), "utf8");

test("content article api exposes workspace load/update and locale helpers", () => {
  assert.match(api, /loadAdminArticle/);
  assert.match(api, /updateAdminArticle/);
  assert.match(api, /isArticleLocaleLocked/);
  assert.match(api, /articleEditorDirection/);
  assert.match(api, /\/v1\/admin\/content\/articles\//);
  assert.match(api, /seoImageMediaAssetId/);
  assert.match(api, /coverMediaAssetId/);
});

test("content article admin workspace uses CKEditor content editor, tabs, and DAM", () => {
  assert.match(screen, /ContentArticleEditor/);
  assert.doesNotMatch(screen, /ContentArticleRichTextEditor/);
  assert.doesNotMatch(screen, /ProductRichTextEditor/);
  assert.doesNotMatch(screen, /@tiptap/);
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
  assert.match(screen, /تصویر اشتراک‌گذاری/);
  assert.match(screen, /استفاده از تصویر شاخص مقاله/);
  assert.match(screen, /setDraftCategoryId\(""\)/);
  assert.match(screen, /نمایش در بخش مقالات صفحه اصلی/);
  assert.doesNotMatch(screen, /ویژه در ریل خانه/);
  assert.match(screen, /ContentArticleCommentsPanel/);
  assert.match(screen, /ContentHelpAffordance/);
});

test("content article editor is CKEditor 5 with professional toolbar contract", () => {
  assert.match(editor, /ContentArticleEditor/);
  assert.match(editor, /dynamic\(/);
  assert.match(editor, /ssr:\s*false/);
  assert.match(ck, /data-content-editor="article"/);
  assert.match(ck, /data-editor="ckeditor5"/);
  assert.match(ck, /ClassicEditor/);
  assert.match(ck, /@ckeditor\/ckeditor5-react/);
  assert.match(ck, /strikethrough/i);
  assert.match(ck, /heading4|heading2/);
  assert.match(ck, /FindAndReplace/);
  assert.match(ck, /damImage/);
  assert.match(ck, /onPickDamImage/);
  assert.match(ck, /sanitizeArticleRichHtml/);
  assert.match(ck, /min-h-\[22rem\]|min-height:\s*22rem/);
  assert.doesNotMatch(ck, /@tiptap/);
  assert.doesNotMatch(ck, /CKBox|CloudServices|EasyImage|Base64UploadAdapter/);
  assert.doesNotMatch(editor, /ProductRichTextEditor/);
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
