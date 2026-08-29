import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const workspacePath = path.join(root, "app/admin/product-workspace-screen.tsx");
const listPath = path.join(root, "app/admin/product-list.tsx");
const shellPath = path.join(root, "design-system/workspace/WorkspaceShell.tsx");
const hostPath = path.join(root, "app/admin/host-client.ts");

test("enter edit does not force general section", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  const enterEdit = screen.match(/function handleEnterEdit\(\) \{[\s\S]*?\n  \}/)?.[0] ?? "";
  assert.match(enterEdit, /formMode\.onEdit\(\)/);
  assert.equal(enterEdit.includes('setSectionId("general")'), false);
});

test("onSectionChange guards dirty without cancelling edit mode", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  const requestSection = screen.match(/function requestSectionChange\(next: string\) \{[\s\S]*?\n  \}/)?.[0] ?? "";
  assert.match(requestSection, /isAnyDirty/);
  assert.match(requestSection, /setPendingNav/);
  assert.equal(requestSection.includes("formMode.onCancel()"), false);
  assert.equal(screen.includes("confirmDiscardIfDirty()"), false);
  assert.match(screen, /product-workspace-unsaved-dialog/);
});

test("readOnly passes viewScope only", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(screen, /readOnly=\{viewScope\}/);
  assert.equal(screen.includes('readOnly={formMode.mode === "view" || viewScope}'), false);
});

test("translations tab has no deferred next-task copy", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.equal(screen.includes("تسک بعدی"), false);
  assert.match(screen, /عمومی و SEO/);
});

test("shell edit exit and mode badge", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(screen, /پایان ویرایش/);
  assert.match(screen, /label: formMode\.mode === "edit" \? "ویرایش" : "مشاهده"/);
  assert.equal(screen.includes('data-testid="product-edit-save"'), false);
  assert.equal(screen.includes('actor: "ops"'), false);
  assert.match(screen, /item\.actor\?\.trim\(\) \|\| "سیستم"/);
});

test("AppDataGrid preserved; no AgGridReact in product list", () => {
  const list = fs.readFileSync(listPath, "utf8");
  assert.match(list, /AppDataGrid/);
  assert.equal(list.includes("AgGridReact"), false);
  assert.match(list, /admin-create-product-cancel/);
  assert.match(list, /\bButton\b/);
});

test("no گونه in product workspace labels or host variant docs", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  const host = fs.readFileSync(hostPath, "utf8");
  assert.equal(screen.includes("گونه"), false);
  assert.equal(host.includes("گونه"), false);
  assert.match(screen, /تنوع‌ها/);
});

test("WorkspaceShell desktop tabs scroll horizontally", () => {
  const shell = fs.readFileSync(shellPath, "utf8");
  assert.match(shell, /flex flex-nowrap gap-2 overflow-x-auto/);
  assert.match(shell, /shrink-0/);
});

test("summary strip sits above tabs; product summary cards expanded", () => {
  const shell = fs.readFileSync(shellPath, "utf8");
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(shell, /workspace-summary-strip/);
  assert.match(screen, /product-summary-cards/);
  assert.match(screen, /product-general-media-preview/);
  assert.equal(screen.includes('headerName: "قیمت (ریال)"'), false);
});

test("product list hides Product-looking price/stock columns by default", () => {
  const list = fs.readFileSync(listPath, "utf8");
  const shell = fs.readFileSync(path.join(root, "app/admin/admin-shell.tsx"), "utf8");
  assert.match(list, /AppDataGrid/);
  assert.equal(list.includes('headerName: "قیمت (ریال)"'), false);
  assert.equal(list.includes('headerName: "موجودی"'), false);
  assert.match(list, /headerName: "تنوع"/);
  assert.match(list, /searchParams\.get\("create"\)/);
  assert.match(shell, /products\?create=1/);
});
