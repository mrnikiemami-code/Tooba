import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import test from "node:test";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const EXCL = new Set(["node_modules", ".next", "dist", "build", "coverage", ".git", "tmp", ".tmp"]);

function walk(dir: string, acc: string[] = []): string[] {
  for (const name of fs.readdirSync(dir)) {
    if (EXCL.has(name)) continue;
    const full = path.join(dir, name);
    const st = fs.statSync(full);
    if (st.isDirectory()) walk(full, acc);
    else acc.push(full);
  }
  return acc;
}

function isTestFile(abs: string): boolean {
  return /\.(guard\.)?test\.(ts|tsx|js|jsx)$/.test(path.basename(abs));
}

test("architecture guard files exist and package.json wires test:architecture", () => {
  const pkg = JSON.parse(fs.readFileSync(path.join(feRoot, "package.json"), "utf8")) as {
    scripts: Record<string, string>;
  };
  assert.ok(pkg.scripts["test:architecture"], "missing test:architecture script");
  assert.ok(pkg.scripts["test:discovered"] || pkg.scripts.test?.includes("run-discovered-tests"));
  const archDir = path.join(feRoot, "lib/architecture");
  const expected = [
    "frontend-source-size.guard.test.ts",
    "frontend-import-boundary.guard.test.ts",
    "frontend-flat-folder.guard.test.ts",
    "frontend-feature-boundary.guard.test.ts",
    "frontend-test-discovery.guard.test.ts",
    "seo-rendering.guard.test.ts",
  ];
  for (const name of expected) {
    assert.ok(fs.existsSync(path.join(archDir, name)), `missing ${name}`);
    assert.ok(
      pkg.scripts["test:architecture"].includes(name) ||
        pkg.scripts["test:architecture"].includes("lib/architecture"),
      `test:architecture should reference ${name}`,
    );
  }
});

test("canonical discovery lists every on-disk frontend test file", () => {
  const onDisk = walk(feRoot)
    .filter(isTestFile)
    .map((f) => path.relative(feRoot, f).replace(/\\/g, "/"))
    .sort();
  const listed = spawnSync(process.execPath, ["scripts/run-discovered-tests.mjs", "--list"], {
    cwd: feRoot,
    encoding: "utf8",
  });
  assert.equal(listed.status, 0, listed.stderr || listed.stdout);
  const payload = JSON.parse(listed.stdout) as { count: number; files: string[] };
  assert.equal(payload.count, onDisk.length);
  assert.deepEqual(payload.files, onDisk);
  assert.ok(payload.count > 100, `expected broad discovery, got ${payload.count}`);
});

test("package.json test scripts still invoke critical storefront suite", () => {
  const pkg = JSON.parse(fs.readFileSync(path.join(feRoot, "package.json"), "utf8")) as {
    scripts: Record<string, string>;
  };
  assert.ok(pkg.scripts["test:critical-storefront"]);
});
