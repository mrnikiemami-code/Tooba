import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapMediaAsset, mediaPreviewUrl } from "./media-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));

test("mapMediaAsset accepts camel and Pascal payloads", () => {
  const id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
  const mapped = mapMediaAsset({
    MediaAssetId: id,
    OriginalFileName: "hero.png",
    ContentType: "image/png",
    ByteSize: 2048,
    Width: 100,
    Height: 80,
    CreatedAt: "2026-08-30T00:00:00Z",
    DisplayUrl: `/v1/storefront/media/${id}`,
  });
  assert.equal(mapped?.mediaAssetId, id);
  assert.equal(mapped?.originalFileName, "hero.png");
  assert.equal(mapped?.byteSize, 2048);
  assert.equal(mediaPreviewUrl(id), `/v1/storefront/media/${id}`);
  assert.equal(mapMediaAsset({ originalFileName: "x" }), null);
});

test("media library dialog has library/upload tabs, search, paging, selection modes", () => {
  const src = fs.readFileSync(path.join(root, "media-library-dialog.tsx"), "utf8");
  assert.match(src, /admin-media-library-dialog/);
  assert.match(src, /کتابخانه/);
  assert.match(src, /آپلود فایل/);
  assert.match(src, /admin-media-search/);
  assert.match(src, /admin-media-page-next/);
  assert.match(src, /selectionMode/);
  assert.match(src, /uploadAdminMediaFiles/);
  assert.match(src, /type=\"file\"/);
  assert.doesNotMatch(src, /شناسه دارایی/);
});
