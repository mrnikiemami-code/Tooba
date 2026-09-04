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
  assert.match(screen, /ویرایش کامل/);
  assert.match(screen, /دسته‌بندی‌ها/);
  assert.match(screen, /loadAdminArticle/);
  assert.match(screen, /loadAdminLanguages/);
  assert.match(screen, /AdminSearchableCombobox/);
  assert.match(screen, /content-article-author-combobox/);
  assert.doesNotMatch(screen, /content-article-author-filter/);
  assert.match(screen, /زبان این مقاله به‌دلیل وجود محتوا یا وابستگی‌های ثبت‌شده قابل تغییر نیست/);
  assert.doesNotMatch(screen, /LANGUAGE_OPTIONS/);
  assert.doesNotMatch(screen, /__articleDamPickResolve/);
  assert.match(screen, /damPickResolveRef/);
  assert.match(screen, /damPickKind/);
  assert.match(screen, /pickDamFile/);
  assert.match(screen, /pickDamVideo/);
  assert.match(screen, /assetKind/);
  assert.match(screen, /content-article-workspace-header/);
  assert.match(screen, /تصویر اشتراک‌گذاری/);
  assert.match(screen, /استفاده از تصویر شاخص مقاله/);
  assert.match(screen, /setDraftCategoryId\(""\)/);
  assert.match(screen, /نمایش در بخش مقالات صفحه اصلی/);
  assert.doesNotMatch(screen, /ویژه در ریل خانه/);
  assert.match(screen, /ContentArticleCommentsPanel/);
  assert.match(screen, /ContentHelpAffordance/);
  assert.match(screen, /useState<TabId>\("full"\)/);
});

test("existing article workspace locale identity comes from applyArticle, not Admin UI or URL", () => {
  assert.match(screen, /const \[draftLocale, setDraftLocale\] = useState\(""\)/);
  assert.doesNotMatch(screen, /const \[draftLocale, setDraftLocale\] = useState\("fa-IR"\)/);
  assert.match(screen, /setDraftLocale\(data\.locale\)/);
  assert.match(screen, /Article identity is the persisted locale/);
  assert.match(screen, /localeSelectOptions/);
  assert.match(screen, /inactiveLocaleOption/);
  assert.doesNotMatch(screen, /searchParams\.get\(["']language["']\)/);
  assert.match(screen, /content-article-workspace-language/);
  assert.match(screen, /draftLocale \|\| article\?\.locale/);
});

test("history tab uses paged Previous\/Next UI with Persian page label", () => {
  assert.match(screen, /historyPage/);
  assert.match(screen, /HISTORY_PAGE_SIZE/);
  assert.match(screen, /historyLoading/);
  assert.match(screen, /content-article-history-pager/);
  assert.match(screen, /صفحه \$\{historyPage\} از \$\{historyTotalPages\}/);
  assert.match(screen, /قبلی/);
  assert.match(screen, /بعدی/);
  assert.match(screen, /refreshPriorPublication/);
  assert.match(screen, /loadArticleHistory\(id, skip, HISTORY_PAGE_SIZE\)/);
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
  assert.match(ck, /damFile/);
  assert.match(ck, /damVideo/);
  assert.match(ck, /onPickDamImage/);
  assert.match(ck, /onPickDamFile/);
  assert.match(ck, /onPickDamVideo/);
  assert.match(ck, /fontFamily/);
  assert.match(ck, /fontFamily:\s*\{/);
  assert.match(ck, /fontSize:\s*\{/);
  assert.match(ck, /B Nazanin/);
  assert.match(ck, /Times New Roman/);
  assert.match(ck, /Vazirmatn, Tahoma/);
  assert.match(ck, /"12px"/);
  assert.match(ck, /"28px"/);
  assert.match(ck, /translations:\s*isRtl\s*\?\s*\[translationsFa\]\s*:\s*undefined/);
  assert.match(ck, /damStorefrontSrc/);
  assert.doesNotMatch(ck, /articleDamMediaSrc/);
  assert.match(ck, /RemoveFormat/);
  assert.match(ck, /HorizontalLine/);
  assert.match(ck, /sanitizeArticleRichHtml/);
  assert.match(ck, /min-h-\[22rem\]|min-height:\s*22rem/);
  assert.doesNotMatch(ck, /@tiptap/);
  assert.doesNotMatch(ck, /CKBox|CloudServices|EasyImage|Base64UploadAdapter/);
  assert.doesNotMatch(editor, /ProductRichTextEditor/);
});

test("content article save refreshes readiness and full-edit polish", () => {
  assert.match(screen, /await refreshReadiness\(article\.articleId\)/);
  assert.match(screen, /refreshReadiness,/);
  assert.match(screen, /نویسنده انتخاب شده — پس از ذخیره در آمادگی لحاظ می‌شود/);
  assert.match(screen, /lg:grid-cols-\[minmax\(0,1\.6fr\)_minmax\(240px,20rem\)\]/);
  assert.match(screen, /lg:sticky lg:top-4/);
  assert.match(screen, /min-h-\[28rem\]/);
  assert.match(screen, /در حال ذخیره…/);
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
