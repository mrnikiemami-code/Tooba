import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

const SCAN_DIRS = [
  "app/storefront",
  "app/login",
  "app/cart",
  "app/shipping",
  "app/checkout",
  "app/payment",
  "app/order",
  "app/blogs",
  "app/customer-panel",
];

const SCAN_FILES = [
  "app/support/support-ui.tsx",
  "app/wallet/wallet-ui.tsx",
];

const STATUS_OR_DECORATIVE = /bg-(?:red|emerald|amber|green|blue|rose|orange)-50\b|bg-primary(?:\/| )|bg-black\/|bg-surface\/|blur-|rounded-full/;

function walk(dir: string, acc: string[] = []): string[] {
  if (!fs.existsSync(path.join(root, dir))) return acc;
  const abs = path.join(root, dir);
  for (const name of fs.readdirSync(abs)) {
    const rel = path.join(dir, name).replaceAll("\\", "/");
    const st = fs.statSync(path.join(root, rel));
    if (st.isDirectory()) walk(rel, acc);
    else if (rel.endsWith(".tsx")) acc.push(rel);
  }
  return acc;
}

function isExceptionLine(line: string): boolean {
  if (STATUS_OR_DECORATIVE.test(line)) return true;
  if (/enam|namad-01|data-storefront-fixed-surface/.test(line)) return true;
  if (/focus:ring-/.test(line) && !/\bbg-\[#/.test(line)) return true;
  return false;
}

test("customer-facing components do not introduce structural raw colors", () => {
  const files = [...SCAN_DIRS.flatMap((dir) => walk(dir)), ...SCAN_FILES];
  assert.ok(files.length > 20);
  const violations: string[] = [];
  for (const rel of files) {
    if (rel.endsWith(".test.ts") || rel.endsWith(".guard.test.ts")) continue;
    const source = fs.readFileSync(path.join(root, rel), "utf8");
    const lines = source.split(/\r?\n/);
    lines.forEach((line, index) => {
      if (isExceptionLine(line)) return;
      if (/\bbg-white\b/.test(line)) violations.push(`${rel}:${index + 1} bg-white`);
      if (/\bbg-\[#/.test(line)) violations.push(`${rel}:${index + 1} bg-[#`);
      if (/backgroundColor:\s*['"`]#/.test(line) || /background:\s*['"`]#/.test(line)) {
        violations.push(`${rel}:${index + 1} inline hex background`);
      }
    });
  }
  assert.deepEqual(violations, [], violations.join("\n"));
});

test("component surfaces are centrally derived not Store settings", () => {
  const derived = fs.readFileSync(path.join(root, "lib/storefront-appearance/derived-surface.ts"), "utf8");
  const globals = fs.readFileSync(path.join(root, "app/globals.css"), "utf8");
  const helpers = fs.readFileSync(path.join(root, "app/admin/admin-appearance-settings.helpers.ts"), "utf8");
  const api = fs.readFileSync(path.join(root, "app/admin/appearance-settings-api.ts"), "utf8");
  assert.match(derived, /deriveComponentSurfaces/);
  assert.match(derived, /card:/);
  assert.match(derived, /elevated:/);
  assert.match(derived, /input:/);
  assert.match(globals, /--color-card-derived/);
  assert.match(globals, /data-storefront-canvas/);
  assert.match(globals, /data-customer-panel-canvas/);
  assert.doesNotMatch(api, /cardSurface|elevatedSurface|inputSurface/);
  assert.match(helpers, /کارت و فرم را سیستم/);
  assert.doesNotMatch(helpers, /type=\"color\"/);
});
