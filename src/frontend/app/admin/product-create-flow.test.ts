import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { richHtmlHasText, sanitizeProductRichHtml } from "./product-rich-html.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("sanitizeProductRichHtml strips script and keeps safe marks", () => {
  const cleaned = sanitizeProductRichHtml(
    `<p style="font-size: 16px; color: red">سلام<script>alert(1)</script></p><img src=x onerror=alert(1)>`,
  );
  assert.equal(cleaned.includes("script"), false);
  assert.equal(cleaned.includes("<img"), false);
  assert.match(cleaned, /سلام/);
  assert.match(cleaned, /font-size:\s*16px/i);
  assert.equal(cleaned.includes("color"), false);
});

test("richHtmlHasText ignores empty wrappers", () => {
  assert.equal(richHtmlHasText("<p></p>"), false);
  assert.equal(richHtmlHasText("<p>متن</p>"), true);
});

test("create route page and TipTap editor exist; CKEditor GPL avoided", () => {
  const page = fs.readFileSync(path.join(root, "app/admin/products/new/page.tsx"), "utf8");
  const editor = fs.readFileSync(path.join(root, "app/admin/product-rich-text-editor.tsx"), "utf8");
  const create = fs.readFileSync(path.join(root, "app/admin/product-create-screen.tsx"), "utf8");
  const translations = fs.readFileSync(path.join(root, "app/admin/product-translations-panel.tsx"), "utf8");
  assert.match(page, /ProductCreateScreen/);
  assert.match(editor, /@tiptap\/react/);
  assert.match(editor, /image-disabled/);
  assert.equal(/from\s+["'][^"']*ckeditor/i.test(editor), false);
  assert.match(editor, /جایگزین CKEditor/);
  assert.match(create, /admin-product-create-screen/);
  assert.match(create, /admin-product-create-steps/);
  assert.match(translations, /ProductRichTextEditor/);
  assert.match(translations, /sanitizeProductRichHtml/);
});
