import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { isActiveAdminNavItem } from "./admin-nav-active.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const shellSource = fs.readFileSync(path.join(root, "app/admin/admin-shell.tsx"), "utf8");

const productSiblings = [
  { id: "products", href: "/admin/products" },
  { id: "product-create", href: "/admin/products/new" },
];

const catalogSiblings = [
  { id: "catalog-categories", href: "/admin/catalog/categories" },
  { id: "catalog-attributes", href: "/admin/catalog/attributes" },
];

const contentSiblings = [
  { id: "content", href: "/admin/content" },
  { id: "content-categories", href: "/admin/content/categories" },
  { id: "content-authors", href: "/admin/content/authors" },
];

const chromeMessages = fs.readFileSync(path.join(root, "app/admin/admin-chrome-messages.ts"), "utf8");

test("admin shell marks settings nav live and clears settings from deferred", () => {
  const settingsIdx = shellSource.indexOf('href: "/admin/settings"');
  assert.ok(settingsIdx >= 0, "settings href missing");
  assert.ok(shellSource.slice(settingsIdx, settingsIdx + 120).includes("live: true"), "settings must be live: true");

  const deferredStart = shellSource.indexOf("export const ADMIN_DEFERRED_NAV_HREFS");
  assert.ok(deferredStart >= 0, "ADMIN_DEFERRED_NAV_HREFS missing");
  const deferredBlock = shellSource.slice(
    deferredStart,
    shellSource.indexOf("] as const;", deferredStart) + 11,
  );
  assert.equal(deferredBlock.includes("/admin/settings"), false, "settings must not remain deferred");
  assert.match(deferredBlock, /\/admin\/catalog\/category-schema/);
});

test("technical category-schema is not in live nav group", () => {
  assert.equal(shellSource.includes('id: "category-schema"'), false);
  assert.match(shellSource, /isActiveAdminNavItem/);
  assert.equal(shellSource.includes("/admin/catalog/category-schema"), true);
});

test("product list and add-product never double-active", () => {
  assert.equal(
    isActiveAdminNavItem("/admin/products", "", productSiblings[0]!, productSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/products", "", productSiblings[1]!, productSiblings),
    false,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/products/new", "", productSiblings[0]!, productSiblings),
    false,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/products/new", "", productSiblings[1]!, productSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/products/abc", "", productSiblings[0]!, productSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/products/abc", "", productSiblings[1]!, productSiblings),
    false,
  );
  assert.match(shellSource, /href: "\/admin\/products\/new"/);
  assert.equal(shellSource.includes("products?create=1"), false);
});

test("category and attribute library active independently", () => {
  assert.equal(
    isActiveAdminNavItem("/admin/catalog/categories", "", catalogSiblings[0]!, catalogSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/catalog/categories", "", catalogSiblings[1]!, catalogSiblings),
    false,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/catalog/attributes", "", catalogSiblings[1]!, catalogSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/catalog/attributes", "", catalogSiblings[0]!, catalogSiblings),
    false,
  );
});

test("deep category routes keep category leaf active", () => {
  assert.equal(
    isActiveAdminNavItem("/admin/catalog/categories/xyz", "", catalogSiblings[0]!, catalogSiblings),
    true,
  );
});

test("content group sits between ops and finance with Articles label", () => {
  assert.match(chromeMessages, /groupContent:\s*"محتوا"/);
  assert.match(chromeMessages, /groupContent:\s*"Content"/);
  assert.match(chromeMessages, /content:\s*"مقالات"/);
  assert.match(chromeMessages, /content:\s*"Articles"/);
  assert.doesNotMatch(chromeMessages, /محتوا \/ بلاگ/);
  assert.doesNotMatch(chromeMessages, /Content \/ blog/);

  const opsIdx = shellSource.indexOf('id: "ops"');
  const contentGroupIdx = shellSource.indexOf('id: "content"', shellSource.indexOf("labelKey: \"groupContent\"") - 40);
  const financeIdx = shellSource.indexOf('id: "finance"');
  assert.ok(opsIdx >= 0 && contentGroupIdx > opsIdx && financeIdx > contentGroupIdx);

  assert.match(shellSource, /labelKey: "groupContent"/);
  assert.match(shellSource, /icon: BookOpen/);
  assert.match(shellSource, /icon: FolderTree/);
  assert.match(shellSource, /icon: PenLine/);

  const opsBlock = shellSource.slice(opsIdx, contentGroupIdx);
  assert.equal(opsBlock.includes('id: "content-categories"'), false);
  assert.equal(opsBlock.includes('id: "content-authors"'), false);
  assert.equal(opsBlock.includes('href: "/admin/content"'), false);

  const systemIdx = shellSource.indexOf('id: "system"');
  const systemBlock = shellSource.slice(systemIdx, shellSource.indexOf("];", systemIdx));
  assert.match(systemBlock, /href: "\/admin\/languages"/);
  assert.equal(shellSource.includes('labelKey: "groupContent"') && systemBlock.includes("groupContent"), false);
});

test("content articles categories and authors active independently", () => {
  assert.equal(
    isActiveAdminNavItem("/admin/content", "", contentSiblings[0]!, contentSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/content/categories", "", contentSiblings[0]!, contentSiblings),
    false,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/content/categories", "", contentSiblings[1]!, contentSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/content/authors", "", contentSiblings[2]!, contentSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/content/authors", "", contentSiblings[0]!, contentSiblings),
    false,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/content/articles/abc", "", contentSiblings[0]!, contentSiblings),
    true,
  );
  assert.equal(
    isActiveAdminNavItem("/admin/content/articles/abc", "", contentSiblings[1]!, contentSiblings),
    false,
  );
});
