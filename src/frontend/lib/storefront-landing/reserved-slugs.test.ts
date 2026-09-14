import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { isReservedLandingSlug, RESERVED_LANDING_SLUGS } from "./reserved-slugs.ts";

const root = join(dirname(fileURLToPath(import.meta.url)), "../../../..");

test("reserved landing slugs cover system first segments", () => {
  assert.equal(isReservedLandingSlug("cart"), true);
  assert.equal(isReservedLandingSlug("admin"), true);
  assert.equal(isReservedLandingSlug("summer-sale"), false);
  const host = readFileSync(join(root, "src/backend/Modules/Catalog/Tooba.Catalog.Domain/StoreLandingPageSlug.cs"), "utf8");
  for (const slug of RESERVED_LANDING_SLUGS) {
    assert.match(host, new RegExp(`"${slug}"`));
  }
});
