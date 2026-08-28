import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  mapCategoryTreeNode,
  mapCategoryWorkspace,
  parseCategoryStatus,
  slugifyCategoryName,
} from "../../app/admin/catalog-category-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("AppCategoryTree wraps antd Tree without exporting Ant types to pages", () => {
  const component = fs.readFileSync(
    path.join(root, "design-system/app-category-tree/AppCategoryTree.tsx"),
    "utf8",
  );
  assert.match(component, /from ["']antd["']/);
  assert.match(component, /<Tree[\s\S]*treeData=/);

  const screen = fs.readFileSync(path.join(root, "app/admin/category-admin-screen.tsx"), "utf8");
  assert.equal(screen.includes('from "antd"'), false);
  assert.equal(screen.includes("AgGridReact"), false);
  assert.equal(screen.includes("این بخش در تسک بعدی تکمیل می‌شود"), true);
  assert.equal(screen.includes("به‌زودی"), true);

  const index = fs.readFileSync(path.join(root, "design-system/index.ts"), "utf8");
  assert.match(index, /AppCategoryTree/);
});

test("category admin routes exist under app/admin/catalog/categories", () => {
  for (const rel of [
    "app/admin/catalog/categories/page.tsx",
    "app/admin/catalog/categories/[categoryId]/page.tsx",
    "app/admin/catalog/categories/[categoryId]/[tab]/page.tsx",
  ]) {
    assert.equal(fs.existsSync(path.join(root, rel)), true, rel);
  }
});

test("nav includes categories labels", () => {
  const shell = fs.readFileSync(path.join(root, "app/admin/admin-shell.tsx"), "utf8");
  assert.match(shell, /\/admin\/catalog\/categories/);
  assert.match(shell, /catalogCategories/);

  const messages = fs.readFileSync(path.join(root, "app/admin/admin-chrome-messages.ts"), "utf8");
  assert.match(messages, /catalogCategories:\s*"دسته‌بندی‌ها"/);
  assert.match(messages, /catalogCategories:\s*"Categories"/);
});

test("drag handle is separate from title/chevron contract in markup", () => {
  const component = fs.readFileSync(
    path.join(root, "design-system/app-category-tree/AppCategoryTree.tsx"),
    "utf8",
  );
  assert.match(component, /category-tree-chevron-/);
  assert.match(component, /category-tree-title-/);
  assert.match(component, /category-tree-drag-handle/);
  assert.match(component, /افزودن زیرمجموعه/);
});

test("mobile layout branch uses data-layout and back control", () => {
  const screen = fs.readFileSync(path.join(root, "app/admin/category-admin-screen.tsx"), "utf8");
  assert.match(screen, /data-layout=\{isNarrow \? "mobile" : "desktop"\}/);
  assert.match(screen, /category-workspace-back/);
  assert.match(screen, /max-width: 1023px/);
});

test("create flow fields stay progressive (no SEO/attrs on create)", () => {
  const screen = fs.readFileSync(path.join(root, "app/admin/category-admin-screen.tsx"), "utf8");
  assert.match(screen, /create-category-name/);
  assert.match(screen, /create-category-slug/);
  assert.equal(screen.includes("seoTitle"), false);
  assert.equal(/CreateCategoryDialog[\s\S]*attribute/i.test(screen), false);
});

test("API mappers tolerate camel and Pascal payloads", () => {
  const node = mapCategoryTreeNode({
    Id: "11111111-1111-1111-1111-111111111111",
    ParentId: null,
    Name: "کتاب",
    Slug: "ketab",
    Status: 0,
    SortOrder: 2,
    IsVisible: true,
    HasChildren: false,
    ProductCount: 3,
  });
  assert.ok(node);
  assert.equal(node!.name, "کتاب");
  assert.equal(node!.status, "Draft");
  assert.equal(node!.productCount, 3);

  const workspace = mapCategoryWorkspace({
    categoryId: "11111111-1111-1111-1111-111111111111",
    parentCategoryId: null,
    status: "Published",
    sortOrder: 1,
    isVisible: true,
    imageMediaAssetId: null,
    iconMediaAssetId: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    translations: [
      {
        categoryId: "11111111-1111-1111-1111-111111111111",
        locale: "fa-IR",
        name: "کتاب",
        slug: "ketab",
        shortDescription: null,
        description: null,
        seoTitle: null,
        seoDescription: null,
        metaKeywords: null,
        updatedAt: "2026-01-01T00:00:00Z",
      },
    ],
  });
  assert.ok(workspace);
  assert.equal(workspace!.status, "Published");
  assert.equal(workspace!.translations.length, 1);
  assert.equal(parseCategoryStatus(2), "Archived");
  assert.equal(slugifyCategoryName("کتابخانه دیجیتال"), "کتابخانه-دیجیتال");
});

test("USER_VISUAL_ACCEPTED remains NO in task contract notes", () => {
  // Guard: screen must not claim visual accept
  const screen = fs.readFileSync(path.join(root, "app/admin/category-admin-screen.tsx"), "utf8");
  assert.equal(screen.includes("USER_VISUAL_ACCEPTED"), false);
});
