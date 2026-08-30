import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  altDraftsFromItems,
  formatMediaCountLabel,
  formatMediaReadinessLabel,
  isAltDraftDirty,
  moveMediaAssetId,
  sortMediaItems,
  type ProductMediaItem,
} from "./product-media-panel-model.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));

function item(partial: Partial<ProductMediaItem> & Pick<ProductMediaItem, "mediaAssetId">): ProductMediaItem {
  return {
    primary: false,
    displayOrder: 0,
    altText: null,
    ...partial,
  };
}

test("sortMediaItems puts primary first then displayOrder", () => {
  const sorted = sortMediaItems([
    item({ mediaAssetId: "b", displayOrder: 2, primary: false }),
    item({ mediaAssetId: "a", displayOrder: 1, primary: true }),
    item({ mediaAssetId: "c", displayOrder: 0, primary: false }),
  ]);
  assert.deepEqual(
    sorted.map((x) => x.mediaAssetId),
    ["a", "c", "b"],
  );
});

test("moveMediaAssetId and alt dirty helpers", () => {
  assert.deepEqual(moveMediaAssetId(["a", "b", "c"], "b", -1), ["b", "a", "c"]);
  assert.equal(moveMediaAssetId(["a", "b"], "a", -1), null);
  const items = [item({ mediaAssetId: "a", altText: "x" })];
  const drafts = altDraftsFromItems(items);
  assert.equal(isAltDraftDirty(items, drafts), false);
  drafts.a = "y";
  assert.equal(isAltDraftDirty(items, drafts), true);
});

test("readiness and count labels are Persian", () => {
  assert.match(formatMediaCountLabel(3), /رسانه/);
  assert.equal(
    formatMediaReadinessLabel({
      hasPrimaryImage: false,
      mediaCount: 0,
      isReady: false,
      messageFa: "تصویر اصلی تعیین نشده",
    }),
    "تصویر اصلی تعیین نشده",
  );
  assert.equal(
    formatMediaReadinessLabel({
      hasPrimaryImage: true,
      mediaCount: 2,
      isReady: true,
      messageFa: "رسانه کامل است",
    }),
    "رسانه کامل است",
  );
});

test("panel source opens real Media Library — no placeholder/fake attach", () => {
  const src = fs.readFileSync(path.join(root, "product-media-panel.tsx"), "utf8");
  assert.match(src, /ProductMediaPanelMode/);
  assert.match(src, /تصویر اصلی/);
  assert.match(src, /افزودن رسانه/);
  assert.match(src, /MediaLibraryDialog/);
  assert.match(src, /admin-product-media-open-library/);
  assert.match(src, /حذف از محصول/);
  assert.match(src, /aria-label="جابه‌جایی به بالا در گالری"/);
  assert.match(src, /admin-product-media-thumbs/);
  assert.match(src, /editable/);
  assert.doesNotMatch(src, /AgGridReact/);
  assert.doesNotMatch(src, /\bPrice\b|\bStock\b/);
  assert.doesNotMatch(src, /attachAdminProductPlaceholderMedia/);
  assert.doesNotMatch(src, /افزودن تصویر نمایشی/);
  assert.match(src, /کتابخانهٔ Media/);
  assert.match(src, /toast\.success/);
  assert.match(src, /رسانه به محصول اضافه شد/);
});

test("workspace wires ProductMediaPanel into رسانه tab without Guid as primary UX", () => {
  const screen = fs.readFileSync(path.join(root, "product-workspace-screen.tsx"), "utf8");
  assert.match(screen, /ProductMediaPanel/);
  assert.match(screen, /label:\s*"رسانه"/);
  assert.doesNotMatch(screen, /پیوست تصویر با شناسه دارایی/);
  assert.doesNotMatch(screen, /attachAdminProductMedia/);
});
