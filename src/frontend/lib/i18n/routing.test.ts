import assert from "node:assert/strict";
import test from "node:test";
import {
  buildLocaleAlternates,
  canonicalForLocale,
  isExcludedFromLocalePrefix,
  isPublicStorefrontPath,
  localePath,
  parseInvalidLocalePrefix,
  parseLocalePrefix,
  parseRegistryPanelPrefix,
  stripLocalePrefix,
} from "./routing.ts";

test("localePath prefixes internal routes", () => {
  assert.equal(localePath("fa", "/"), "/fa");
  assert.equal(localePath("en", "/products"), "/en/products");
  assert.equal(localePath("fa", "/blogs/guide"), "/fa/blogs/guide");
});

test("parseLocalePrefix extracts locale and internal path", () => {
  assert.deepEqual(parseLocalePrefix("/fa/products"), { locale: "fa", pathname: "/products" });
  assert.deepEqual(parseLocalePrefix("/en"), { locale: "en", pathname: "/" });
  assert.equal(parseLocalePrefix("/products"), null);
});

test("registry panel prefix supports configured multi-character prefixes", () => {
  assert.deepEqual(parseRegistryPanelPrefix("/ru/admin/offers"), { prefix: "ru", pathname: "/admin/offers" });
  assert.deepEqual(parseRegistryPanelPrefix("/en-US/vendor-panel/products"), { prefix: "en-US", pathname: "/vendor-panel/products" });
  assert.equal(parseRegistryPanelPrefix("/admin/offers"), null);
});

test("stripLocalePrefix removes prefix", () => {
  assert.equal(stripLocalePrefix("/fa/blogs"), "/blogs");
  assert.equal(stripLocalePrefix("/products"), "/products");
});

test("invalid locale prefix detected", () => {
  assert.equal(parseInvalidLocalePrefix("/fr/products"), "fr");
  assert.equal(parseInvalidLocalePrefix("/fa/products"), null);
});

test("public vs excluded paths", () => {
  assert.equal(isPublicStorefrontPath("/"), true);
  assert.equal(isPublicStorefrontPath("/products"), true);
  assert.equal(isPublicStorefrontPath("/category"), true);
  assert.equal(isPublicStorefrontPath("/category/mobile"), true);
  assert.equal(isPublicStorefrontPath("/admin"), false);
  assert.equal(isPublicStorefrontPath("/summer-sale"), true);
  assert.equal(isExcludedFromLocalePrefix("/admin/orders"), true);
  assert.equal(isExcludedFromLocalePrefix("/customer-panel"), true);
});

test("canonical and hreflang alternates", () => {
  const alt = buildLocaleAlternates("/blogs", { includeXDefault: true });
  assert.equal(canonicalForLocale("fa", "/blogs"), "/fa/blogs");
  assert.equal(alt.languages["fa-IR"], "/fa/blogs");
  assert.equal(alt.languages.en, "/en/blogs");
  assert.equal(alt.languages["x-default"], "/fa/blogs");
});
