import assert from "node:assert/strict";
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
