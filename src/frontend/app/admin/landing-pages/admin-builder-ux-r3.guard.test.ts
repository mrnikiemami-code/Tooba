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

test("seed summary shows 8 category trees + 15 products", () => {
  assert.match(workspace, /۸ درخت دسته‌بندی سه‌سطحی/);
  assert.match(workspace, /۱۵ محصول نمونه/);
  assert.match(workspace, /template-seed-counts/);
});

test("articles/stories/reviews are not marked template-specific", () => {
  assert.match(workspace, /مقالات مخصوص قالب نیستند/);
  assert.match(workspace, /استوری‌های نمونه عمومی/);
  assert.match(workspace, /نظرات نمونه عمومی/);
  assert.match(workspace, /template-shared-demo-note/);
});

test("cleanup actions are non-destructive in this wireframe task", () => {
  assert.match(workspace, /cleanup-sample-data-action/);
  assert.match(workspace, /prepare-store-action/);
  assert.match(workspace, /disabled/);
  assert.match(workspace, /در مرحله بعد فعال می‌شود|پس از تأیید الگوی داده نمونه فعال می‌شود/);
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
