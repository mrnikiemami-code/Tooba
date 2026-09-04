import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { join } from "node:path";
import test from "node:test";
import { articleDamImageSrc, articleDamMediaSrc, sanitizeArticleRichHtml } from "./article-rich-html.ts";

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
  assert.equal(articleDamMediaSrc(id), src);
  const html = sanitizeArticleRichHtml(
    `<p style="text-align:center;color:#112233;background-color:rgb(255, 240, 0)">x</p><figure class="image"><img class="article-dam-image image-style-side" src="${src}" alt="a" data-media-asset-id="${id}" width="50%" /></figure>`,
  );
  assert.match(html, /data-media-asset-id/);
  assert.match(html, /text-align:\s*center/);
  assert.match(html, /color:\s*#112233/);
  assert.match(html, /background-color:\s*rgb\(255, 240, 0\)/);
  assert.match(html, /article-dam-image|image-style-side/);
  assert.doesNotMatch(sanitizeArticleRichHtml('<img src="data:image/png;base64,abc" />'), /img/);
  assert.equal(sanitizeArticleRichHtml('<img src="https://evil.test/x.png" />'), "");
  assert.doesNotMatch(
    sanitizeArticleRichHtml(`<p onclick="alert(1)">x</p><script>evil()</script>`),
    /script|onclick/i,
  );
  assert.doesNotMatch(sanitizeArticleRichHtml('<iframe src="https://evil.test"></iframe>'), /iframe/i);

  const fileHtml = sanitizeArticleRichHtml(
    `<p><a class="article-dam-file" href="${src}" data-media-asset-id="${id}" target="_blank" rel="noopener noreferrer">doc.pdf</a></p>`,
  );
  assert.match(fileHtml, /article-dam-file/);
  assert.match(fileHtml, /data-media-asset-id/);
  assert.doesNotMatch(
    sanitizeArticleRichHtml(
      `<a class="article-dam-file" href="https://evil.test/x.pdf" data-media-asset-id="${id}">bad</a>`,
    ),
    /article-dam-file|evil\.test/,
  );

  const videoHtml = sanitizeArticleRichHtml(
    `<figure class="article-dam-video"><video class="article-dam-video" controls preload="metadata" src="${src}" data-media-asset-id="${id}"></video></figure><hr />`,
  );
  assert.match(videoHtml, /<video[^>]*data-media-asset-id/);
  assert.match(videoHtml, /article-dam-video/);
  assert.match(videoHtml, /<hr\b/i);

  const wrappedVideo = sanitizeArticleRichHtml(
    `<figure class="article-dam-video"><p><video class="article-dam-video" controls preload="metadata" src="${src}" data-media-asset-id="${id}"></video></p></figure>`,
  );
  assert.match(wrappedVideo, /<video[^>]*data-media-asset-id/);
  assert.doesNotMatch(wrappedVideo, /<figure[^>]*>\s*<p>\s*<video/i);
  assert.doesNotMatch(wrappedVideo, /<p>\s*<video/i);

  assert.doesNotMatch(
    sanitizeArticleRichHtml(`<video src="https://evil.test/x.mp4" data-media-asset-id="${id}"></video>`),
    /video/i,
  );
  assert.doesNotMatch(sanitizeArticleRichHtml('<embed src="/v1/storefront/media/x"></embed>'), /embed/i);
});

test("article rich html keeps allowlisted font-family stacks including Times New Roman and B Nazanin", () => {
  const times = sanitizeArticleRichHtml(
    `<p style="font-family:&quot;Times New Roman&quot;, Times, serif;font-size:18px">Hello</p>`,
  );
  assert.match(times, /font-family/i);
  assert.match(times, /Times New Roman/i);
  assert.match(times, /font-size:\s*18px/i);

  const nazanin = sanitizeArticleRichHtml(
    `<span style="font-family:B Nazanin, Tahoma, Arial, sans-serif;font-size:16px">متن</span>`,
  );
  assert.match(nazanin, /font-family/i);
  assert.match(nazanin, /B Nazanin/i);
  assert.match(nazanin, /font-size:\s*16px/i);

  const quoted = sanitizeArticleRichHtml(
    `<p style='font-family:"Times New Roman", Times, serif'>x</p>`,
  );
  assert.match(quoted, /Times New Roman/i);

  const rejected = sanitizeArticleRichHtml(
    `<p style="font-family:Comic Sans MS, cursive;font-size:99px">bad</p>`,
  );
  assert.doesNotMatch(rejected, /Comic Sans/i);
  assert.doesNotMatch(rejected, /99px/);
});

test("article workspace uses media panel and CKEditor dam insert image", () => {
  assert.match(screen, /ContentArticleMediaPanel/);
  assert.match(screen, /ContentArticleEditor/);
  assert.match(screen, /assignArticleSeoImage/);
  assert.match(screen, /استفاده از تصویر شاخص مقاله/);
  assert.match(panel, /MediaLibraryDialog/);
  assert.match(panel, /onWorkspaceChange/);
  assert.match(editor, /onPickDamImage/);
  assert.match(editor, /onPickDamFile/);
  assert.match(editor, /onPickDamVideo/);
  assert.match(editor, /damImage/);
  assert.match(editor, /damFile/);
  assert.match(editor, /damVideo/);
  assert.match(editor, /data-media-asset-id/);
  assert.match(editor, /damStorefrontSrc/);
  assert.match(editor, /\/v1\/storefront\/media\//);
  assert.doesNotMatch(editor, /articleDamMediaSrc/);
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
