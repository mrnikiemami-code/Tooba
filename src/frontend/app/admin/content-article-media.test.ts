import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";
import { articleDamImageSrc, sanitizeArticleRichHtml } from "./article-rich-html.ts";

const api = readFileSync(join(import.meta.dirname, "content-article-media-api.ts"), "utf8");
const panel = readFileSync(join(import.meta.dirname, "content-article-media-panel.tsx"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-article-admin-screen.tsx"), "utf8");
const editor = readFileSync(join(import.meta.dirname, "content-article-ckeditor.tsx"), "utf8");

test("article media api targets content media endpoints with JSON content-type", () => {
  assert.match(api, /\/v1\/admin\/content\/articles\/.+\/media/);
  assert.match(api, /\/media\/featured/);
  assert.match(api, /\/media\/seo-image/);
  assert.match(api, /\/media\/gallery/);
  assert.match(api, /Content-Type["']:\s*["']application\/json["']/);
  assert.doesNotMatch(api, /adminHeaders\(body !== undefined\)/);
});

test("article rich html allows only storefront dam src and safe CKEditor attrs", () => {
  const id = "aaaaaaaa-aaaa-4aaa-8aaa-000000000001";
  const src = articleDamImageSrc(id);
  const html = sanitizeArticleRichHtml(
    `<p style="text-align:center">x</p><figure class="image"><img class="article-dam-image image-style-side" src="${src}" alt="a" data-media-asset-id="${id}" width="50%" /></figure>`,
  );
  assert.match(html, /data-media-asset-id/);
  assert.match(html, /text-align:\s*center/);
  assert.match(html, /article-dam-image|image-style-side/);
  assert.doesNotMatch(sanitizeArticleRichHtml('<img src="data:image/png;base64,abc" />'), /img/);
  assert.equal(sanitizeArticleRichHtml('<img src="https://evil.test/x.png" />'), "");
  assert.doesNotMatch(
    sanitizeArticleRichHtml(`<p onclick="alert(1)">x</p><script>evil()</script>`),
    /script|onclick/i,
  );
  assert.doesNotMatch(sanitizeArticleRichHtml('<iframe src="https://evil.test"></iframe>'), /iframe/i);
});

test("article workspace uses media panel and CKEditor dam insert image", () => {
  assert.match(screen, /ContentArticleMediaPanel/);
  assert.match(screen, /ContentArticleEditor/);
  assert.match(screen, /assignArticleSeoImage/);
  assert.match(screen, /استفاده از تصویر شاخص مقاله/);
  assert.match(panel, /MediaLibraryDialog/);
  assert.match(panel, /onWorkspaceChange/);
  assert.match(editor, /onPickDamImage/);
  assert.match(editor, /damImage/);
  assert.match(editor, /data-media-asset-id/);
  assert.doesNotMatch(editor, /@tiptap/);
});

test("article media panel and SEO picker use Persian library labels without DAM jargon", () => {
  assert.match(panel, /متن جایگزین/);
  assert.match(panel, /توضیح تصویر/);
  assert.match(panel, /انتخاب از کتابخانه/);
  assert.match(panel, /کتابخانه رسانه/);
  assert.doesNotMatch(panel, /انتخاب از کتابخانه DAM/);
  assert.doesNotMatch(panel, /افزودن از DAM/);
  assert.match(screen, /انتخاب از کتابخانه/);
  assert.doesNotMatch(screen, /انتخاب از DAM/);
  assert.match(screen, /در حال بارگذاری…/);
  assert.doesNotMatch(screen, /در حال بارگذاری workspace/);
  assert.match(screen, /تصویر اشتراک‌گذاری/);
  assert.doesNotMatch(screen, /OpenGraph/);
  assert.doesNotMatch(screen, /مؤثر: تصویر شاخص/);
});
