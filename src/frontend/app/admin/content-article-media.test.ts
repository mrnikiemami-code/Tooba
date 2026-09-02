import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";
import { articleDamImageSrc, sanitizeArticleRichHtml } from "./article-rich-html.ts";

const api = readFileSync(join(import.meta.dirname, "content-article-media-api.ts"), "utf8");
const panel = readFileSync(join(import.meta.dirname, "content-article-media-panel.tsx"), "utf8");
const screen = readFileSync(join(import.meta.dirname, "content-article-admin-screen.tsx"), "utf8");
const editor = readFileSync(join(import.meta.dirname, "product-rich-text-editor.tsx"), "utf8");

test("article media api targets content media endpoints", () => {
  assert.match(api, /\/v1\/admin\/content\/articles\/.+\/media/);
  assert.match(api, /\/media\/featured/);
  assert.match(api, /\/media\/seo-image/);
  assert.match(api, /\/media\/gallery/);
});

test("article rich html allows only storefront dam src", () => {
  const id = "aaaaaaaa-aaaa-4aaa-8aaa-000000000001";
  const src = articleDamImageSrc(id);
  const html = sanitizeArticleRichHtml(`<p>x</p><img src="${src}" alt="a" data-media-asset-id="${id}" />`);
  assert.match(html, /data-media-asset-id/);
  assert.doesNotMatch(sanitizeArticleRichHtml('<img src="data:image/png;base64,abc" />'), /img/);
  assert.equal(sanitizeArticleRichHtml('<img src="https://evil.test/x.png" />'), "");
});

test("article workspace uses media panel and dam insert image", () => {
  assert.match(screen, /ContentArticleMediaPanel/);
  assert.match(screen, /assignArticleSeoImage/);
  assert.match(screen, /استفاده از تصویر شاخص/);
  assert.match(panel, /MediaLibraryDialog/);
  assert.match(editor, /onPickDamImage/);
  assert.match(editor, /insert-image/);
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
});
