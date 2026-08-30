import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  TAG_HELPER_FA,
  filterUnassignedTags,
  mapCatalogTag,
  type CatalogTag,
} from "./catalog-tag-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const apiPath = path.join(root, "app/admin/catalog-tag-api.ts");
const panelPath = path.join(root, "app/admin/admin-tags-panel.tsx");
const cardPath = path.join(root, "app/admin/catalog-tags-card.tsx");
const productScreenPath = path.join(root, "app/admin/product-workspace-screen.tsx");
const categoryScreenPath = path.join(root, "app/admin/category-admin-screen.tsx");

function tag(partial: Partial<CatalogTag> & Pick<CatalogTag, "tagId" | "name">): CatalogTag {
  return {
    code: partial.name,
    slugSeam: null,
    status: "Draft",
    createdAt: "",
    updatedAt: "",
    ...partial,
  };
}

test("mapCatalogTag reads camel and Pascal casing with human name", () => {
  const mapped = mapCatalogTag({
    TagId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    Code: "bestseller",
    Name: "پرفروش",
    Status: "Draft",
    SlugSeam: null,
    CreatedAt: "2026-01-01T00:00:00Z",
    UpdatedAt: "2026-01-01T00:00:00Z",
  });
  assert.equal(mapped?.tagId, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  assert.equal(mapped?.name, "پرفروش");
  assert.equal(mapped?.code, "bestseller");
});

test("mapCatalogTag rejects payload without identity", () => {
  assert.equal(mapCatalogTag({ name: "x" }), null);
});

test("filterUnassignedTags excludes already assigned ids", () => {
  const all = [
    tag({ tagId: "1", name: "A" }),
    tag({ tagId: "2", name: "B" }),
  ];
  const assigned = [tag({ tagId: "1", name: "A" })];
  const available = filterUnassignedTags(all, assigned);
  assert.equal(available.length, 1);
  assert.equal(available[0]?.tagId, "2");
});

test("helper text is locked Persian taxonomy copy not SEO keywords", () => {
  assert.match(TAG_HELPER_FA, /گروه‌بندی/);
  assert.doesNotMatch(TAG_HELPER_FA, /keyword/i);
  assert.doesNotMatch(TAG_HELPER_FA, /meta/i);
});

test("admin-tags-panel: searchable multi-select, create fa/en, removable chips, no comma textbox", () => {
  const panel = fs.readFileSync(panelPath, "utf8");
  assert.match(panel, /AdminTagsPanel/);
  assert.match(panel, /picker-search/);
  assert.match(panel, /create-fa/);
  assert.match(panel, /create-en/);
  assert.match(panel, /chips/);
  assert.match(panel, /TAG_HELPER_FA/);
  assert.doesNotMatch(panel, /name=["']keywords["']/);
  assert.doesNotMatch(panel, /comma-separated/i);
});

test("catalog-tags-card wraps AdminTagsPanel", () => {
  const card = fs.readFileSync(cardPath, "utf8");
  assert.match(card, /AdminTagsPanel/);
  assert.match(card, /CatalogTagsCard/);
});

test("product and category General screens host tags card", () => {
  const product = fs.readFileSync(productScreenPath, "utf8");
  const category = fs.readFileSync(categoryScreenPath, "utf8");
  assert.match(product, /CatalogTagsCard/);
  assert.match(product, /ownerKind="product"/);
  assert.match(category, /CatalogTagsCard/);
  assert.match(category, /ownerKind="category"/);
});

test("api client exposes CRUD and assign routes without meta keywords", () => {
  const api = fs.readFileSync(apiPath, "utf8");
  assert.match(api, /\/v1\/admin\/catalog\/tags/);
  assert.match(api, /\/products\/\$\{productId\}\/tags/);
  assert.match(api, /\/categories\/\$\{categoryId\}\/tags/);
  assert.match(api, /createCatalogTag/);
  assert.match(api, /assignProductTag/);
  assert.match(api, /removeCategoryTag/);
  assert.doesNotMatch(api, /metaKeywords/);
  assert.doesNotMatch(api, /name="keywords"/);
});
