import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { blogsAuthorPath, blogsCategoryPath, blogsCopy } from "./blogs-copy.ts";

test("blogsCopy returns fa shell strings for fa locale", () => {
  const copy = blogsCopy("fa");
  assert.equal(copy.title, "مجله توبا");
  assert.equal(copy.readMore, "مطالعه");
  assert.equal(copy.categoryHeading, "دسته");
  assert.equal(copy.authorHeading, "نویسنده");
});

test("blogsCopy returns en shell strings for non-fa locale", () => {
  const copy = blogsCopy("en");
  assert.equal(copy.title, "Tooba Magazine");
  assert.equal(copy.backToMagazine, "Back to magazine");
  assert.equal(copy.articlesEmpty, "No articles in this list.");
});

test("taxonomy public paths use /blogs not /blog", () => {
  assert.equal(blogsCategoryPath("guides"), "/blogs/category/guides");
  assert.equal(blogsAuthorPath("editorial"), "/blogs/author/editorial");
  assert.doesNotMatch(blogsCategoryPath("x"), /\/blog\//);
  assert.doesNotMatch(blogsAuthorPath("x"), /\/blog\//);
});

test("blog shells use locale-aware chevron/arrow and backToMagazine", () => {
  const listing = readFileSync(new URL("./blogs-ui.tsx", import.meta.url), "utf8");
  const taxonomy = readFileSync(new URL("./blogs-taxonomy-ui.tsx", import.meta.url), "utf8");
  const detail = readFileSync(new URL("./[slug]/blog-detail-ui.tsx", import.meta.url), "utf8");
  assert.match(listing, /locale === "en" \? ChevronRight : ChevronLeft/);
  assert.match(taxonomy, /locale === "en" \? ChevronRight : ChevronLeft/);
  assert.match(taxonomy, /locale === "en" \? ArrowLeft : ArrowRight/);
  assert.match(detail, /locale === "en" \? ArrowLeft : ArrowRight/);
  assert.match(detail, /copy\.backToMagazine/);
  assert.match(detail, /px-3 py-6 md:px-4/);
  assert.doesNotMatch(detail, /\{copy\.title\}/);
});
