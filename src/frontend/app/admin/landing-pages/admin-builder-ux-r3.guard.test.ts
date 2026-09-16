import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const workspace = readFileSync(join(dir, "admin-template-selection-workspace.tsx"), "utf8");
const composer = readFileSync(join(dir, "admin-landing-page-composer.tsx"), "utf8");
const locks = readFileSync(join(dir, "../../../../../docs/architecture/TOOBA-LOCKS.md"), "utf8");

test("template picker exposes Desktop/Tablet/Mobile modes", () => {
  assert.match(workspace, /template-device-\$\{item\.id\}|template-device-desktop/);
  assert.match(workspace, /id: "desktop"/);
  assert.match(workspace, /id: "tablet"/);
  assert.match(workspace, /id: "mobile"/);
  assert.match(workspace, /TemplatePreviewDevice/);
});

test("selected device mode changes preview frame contract", () => {
  assert.match(workspace, /data-preview-device/);
  assert.match(workspace, /max-w-\[920px\]/);
  assert.match(workspace, /max-w-\[640px\]/);
  assert.match(workspace, /max-w-\[360px\]/);
  assert.doesNotMatch(workspace, /transform:\s*scale|zoom:/);
});

test("template previews are structurally distinct", () => {
  assert.match(workspace, /industryWireframeBlocks/);
  assert.match(workspace, /fashion|shoes|beauty/);
  assert.match(workspace, /auto-parts|tools-hardware/);
  assert.match(workspace, /interior-decor|tile-ceramic/);
  assert.match(workspace, /plants/);
});

test("seed pack summary card is not shown on template picker", () => {
  assert.doesNotMatch(workspace, /template-seed-pack-summary/);
  assert.doesNotMatch(workspace, /template-seed-counts/);
  assert.doesNotMatch(workspace, /داده نمونه این قالب/);
});

test("shared demo note card is not shown on template picker", () => {
  assert.doesNotMatch(workspace, /template-shared-demo-note/);
  assert.doesNotMatch(workspace, /مقالات مخصوص قالب نیستند/);
});

test("cleanup and prepare-store actions are removed from template picker", () => {
  assert.doesNotMatch(workspace, /cleanup-sample-data-action/);
  assert.doesNotMatch(workspace, /prepare-store-action/);
  assert.doesNotMatch(workspace, /پاک‌سازی داده‌های نمونه/);
  assert.doesNotMatch(workspace, /آماده‌سازی فروشگاه برای ورود اطلاعات/);
  assert.doesNotMatch(workspace, /fetch\(.*cleanup|DELETE.*seed/i);
});

test("no per-section language selector in template workspace", () => {
  assert.doesNotMatch(workspace, /زبان بخش|section-language|per-section language/i);
  assert.doesNotMatch(workspace, /page-language-create-field/);
  assert.match(composer, /page-language-create|زبان صفحه/);
});

test("R3 locks registered", () => {
  for (const lock of ["LOCK-SF-274", "LOCK-SF-275", "LOCK-SF-276", "LOCK-SF-277", "LOCK-SF-278", "LOCK-SF-279"]) {
    assert.match(locks, new RegExp(lock));
  }
});
