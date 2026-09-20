import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("architecture guard files exist and package.json wires test:architecture", () => {
  const pkg = JSON.parse(fs.readFileSync(path.join(feRoot, "package.json"), "utf8")) as {
    scripts: Record<string, string>;
  };
  assert.ok(pkg.scripts["test:architecture"], "missing test:architecture script");
  const archDir = path.join(feRoot, "lib/architecture");
  const expected = [
    "frontend-source-size.guard.test.ts",
    "frontend-import-boundary.guard.test.ts",
    "frontend-test-discovery.guard.test.ts",
    "seo-rendering.guard.test.ts",
  ];
  for (const name of expected) {
    assert.ok(fs.existsSync(path.join(archDir, name)), `missing ${name}`);
    assert.ok(
      pkg.scripts["test:architecture"].includes(name) ||
        pkg.scripts["test:architecture"].includes("lib/architecture"),
      `test:architecture should reference architecture guards (${name})`,
    );
  }
});

test("package.json test scripts still invoke critical storefront suite", () => {
  const pkg = JSON.parse(fs.readFileSync(path.join(feRoot, "package.json"), "utf8")) as {
    scripts: Record<string, string>;
  };
  assert.ok(pkg.scripts["test:critical-storefront"]);
  assert.ok(pkg.scripts.test.includes("test:critical-storefront"));
});
