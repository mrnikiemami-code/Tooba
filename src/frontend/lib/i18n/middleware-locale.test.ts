import assert from "node:assert/strict";
import test from "node:test";
import { planLocaleMiddleware } from "./middleware-locale.ts";

test("prefixed public routes rewrite instead of redirect", () => {
  assert.deepEqual(planLocaleMiddleware("/fa", undefined, null), {
    type: "rewrite",
    locale: "fa",
    internalPath: "/",
  });
  assert.deepEqual(planLocaleMiddleware("/en", undefined, null), {
    type: "rewrite",
    locale: "en",
    internalPath: "/",
  });
  assert.deepEqual(planLocaleMiddleware("/fa/blogs", undefined, null), {
    type: "rewrite",
    locale: "fa",
    internalPath: "/blogs",
  });
  assert.deepEqual(planLocaleMiddleware("/en/blogs", undefined, null), {
    type: "rewrite",
    locale: "en",
    internalPath: "/blogs",
  });
  assert.deepEqual(planLocaleMiddleware("/fa/blogs/guide-online-shopping", undefined, null), {
    type: "rewrite",
    locale: "fa",
    internalPath: "/blogs/guide-online-shopping",
  });
});

test("internal public path after rewrite does not 308 back to itself", () => {
  assert.equal(planLocaleMiddleware("/blogs", undefined, "fa").type, "pass");
  assert.equal(planLocaleMiddleware("/blogs", undefined, "en").type, "pass");
  assert.equal(planLocaleMiddleware("/", undefined, "fa").type, "pass");
  assert.deepEqual(planLocaleMiddleware("/blogs", undefined, null), {
    type: "redirect",
    location: "/fa/blogs",
  });
  assert.deepEqual(planLocaleMiddleware("/blogs", "en", null), {
    type: "redirect",
    location: "/en/blogs",
  });
});

test("admin stays unprefixed", () => {
  assert.equal(planLocaleMiddleware("/admin/content", undefined, null).type, "pass");
  assert.equal(planLocaleMiddleware("/admin/languages", undefined, null).type, "pass");
});
