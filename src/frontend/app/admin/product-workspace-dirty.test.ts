import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { createProductWorkspaceDirtyRegistry } from "./product-workspace-dirty.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));
const dirtyPath = path.join(root, "product-workspace-dirty.ts");
const dirtyCtxPath = path.join(root, "product-workspace-dirty-context.tsx");
const workspacePath = path.join(root, "product-workspace-screen.tsx");
const attributesPath = path.join(root, "product-attributes-panel.tsx");
const variantsPath = path.join(root, "product-variants-panel.tsx");
const seoPath = path.join(root, "product-seo-panel.tsx");
const mediaPath = path.join(root, "product-media-panel.tsx");

test("dirty registry module exports registration API", () => {
  const src = fs.readFileSync(dirtyPath, "utf8");
  assert.match(src, /createProductWorkspaceDirtyRegistry/);
  assert.match(src, /isAnyDirty/);
  assert.match(src, /discardAll/);
  assert.match(src, /discardSection/);
  assert.match(src, /isSectionDirty/);
  assert.match(src, /ProductWorkspaceDirtyRegistry/);
});

test("dirty context provides provider and registration hook", () => {
  const src = fs.readFileSync(dirtyCtxPath, "utf8");
  assert.match(src, /ProductWorkspaceDirtyProvider/);
  assert.match(src, /useProductWorkspaceDirtyRegistration/);
  assert.match(src, /register/);
  assert.match(src, /unregister/);
});

test("createProductWorkspaceDirtyRegistry tracks dirty and discard", () => {
  const discarded: string[] = [];
  const registry = createProductWorkspaceDirtyRegistry();

  registry.register("attributes", {
    isDirty: true,
    discard: () => discarded.push("attributes"),
  });
  registry.register("seo", {
    isDirty: false,
    discard: () => discarded.push("seo"),
  });

  assert.equal(registry.isAnyDirty(), true);
  assert.equal(registry.isSectionDirty("attributes"), true);
  assert.equal(registry.isSectionDirty("seo"), false);
  assert.deepEqual([...registry.dirtySectionIds()], ["attributes"]);

  registry.discardAll();
  assert.deepEqual(discarded, ["attributes"]);

  registry.register("attributes", {
    isDirty: false,
    discard: () => discarded.push("attributes-again"),
  });
  assert.equal(registry.isAnyDirty(), false);

  registry.register("variants", {
    isDirty: true,
    discard: () => discarded.push("variants"),
  });
  registry.discardSection("variants");
  assert.deepEqual(discarded.at(-1), "variants");

  registry.unregister("variants");
  assert.equal(registry.isSectionDirty("variants"), false);
});

test("workspace screen wires Dialog unsaved guard and pendingNav", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(screen, /ProductWorkspaceDirtyProvider/);
  assert.match(screen, /product-workspace-unsaved-dialog/);
  assert.match(screen, /product-workspace-unsaved-stay/);
  assert.match(screen, /product-workspace-unsaved-discard/);
  assert.match(screen, /pendingNav/);
  assert.match(screen, /requestSectionChange/);
  assert.match(screen, /beforeunload/);
  assert.match(screen, /ادامه و لغو تغییرات/);
  assert.match(screen, /بازگشت/);
  assert.match(screen, /تغییرات ذخیره‌نشده دارید/);
  assert.match(screen, /type: "exit-edit"/);
  assert.equal(screen.includes("confirmDiscardIfDirty()"), false);
});

test("onSectionChange guards dirty before setSectionId", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  const requestSection = screen.match(/function requestSectionChange\(next: string\) \{[\s\S]*?\n  \}/)?.[0] ?? "";
  assert.match(requestSection, /isAnyDirty/);
  assert.match(requestSection, /setPendingNav\(\{ type: "tab"/);
  assert.match(requestSection, /setSectionId\(next\)/);
  assert.equal(requestSection.includes("confirmDiscardIfDirty"), false);
});

test("exit edit is guarded via pendingNav dialog", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  const cancel = screen.match(/function handleCancelEdit\(\) \{[\s\S]*?\n  \}/)?.[0] ?? "";
  assert.match(cancel, /setPendingNav\(\{ type: "exit-edit" \}/);
  assert.equal(cancel.includes("confirmDiscardIfDirty"), false);
});

test("attributes / variants / seo / media register dirty", () => {
  const attributes = fs.readFileSync(attributesPath, "utf8");
  const variants = fs.readFileSync(variantsPath, "utf8");
  const seo = fs.readFileSync(seoPath, "utf8");
  const media = fs.readFileSync(mediaPath, "utf8");

  assert.match(attributes, /useProductWorkspaceDirtyRegistration\("attributes"/);
  assert.match(variants, /useProductWorkspaceDirtyRegistration\("variants"/);
  assert.match(seo, /useProductWorkspaceDirtyRegistration\("seo"/);
  assert.match(media, /useProductWorkspaceDirtyRegistration\("media"/);
});

test("panel save paths clear dirty / reload after save", () => {
  const attributes = fs.readFileSync(attributesPath, "utf8");
  const variants = fs.readFileSync(variantsPath, "utf8");
  const seo = fs.readFileSync(seoPath, "utf8");

  assert.match(attributes, /setDirty\(false\)/);
  assert.match(attributes, /async function onSave\(\) \{[\s\S]*setDirty\(false\)/);
  assert.match(variants, /await reload\(\)/);
  assert.match(seo, /setDraft\(draftFromSeoDetail\(result\.detail\)\)/);
});

test("attributes and variants cancel no longer use window.confirm", () => {
  const attributes = fs.readFileSync(attributesPath, "utf8");
  const variants = fs.readFileSync(variantsPath, "utf8");
  assert.equal(attributes.includes("window.confirm"), false);
  assert.equal(variants.includes("window.confirm"), false);
});

test("seo locale switch may keep local window.confirm; tab switch uses Dialog", () => {
  const seo = fs.readFileSync(seoPath, "utf8");
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(seo, /window\.confirm/);
  assert.match(screen, /product-workspace-unsaved-dialog/);
  assert.match(screen, /onSectionChange=\{requestSectionChange\}/);
});
