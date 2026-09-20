import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const repoRoot = path.resolve(feRoot, "../..");
const EXT = new Set([".ts", ".tsx", ".js", ".jsx"]);
const EXCL = new Set(["node_modules", ".next", "dist", "build", "coverage", ".git"]);

function walk(dir: string, acc: string[] = []): string[] {
  if (!fs.existsSync(dir)) return acc;
  for (const name of fs.readdirSync(dir)) {
    if (EXCL.has(name)) continue;
    const full = path.join(dir, name);
    const st = fs.statSync(full);
    if (st.isDirectory()) walk(full, acc);
    else acc.push(full);
  }
  return acc;
}

function repoRel(abs: string): string {
  return path.relative(repoRoot, abs).replace(/\\/g, "/");
}

test("FE-BOUNDARY: external code must not deep-import admin-languages internals", () => {
  const deepRe = /features\/admin-languages\/(api|components)\//;
  const publicRe = /features\/admin-languages(?:\/index)?(?:\.ts)?['"]/;
  const violations: string[] = [];
  for (const abs of walk(feRoot).filter((f) => EXT.has(path.extname(f).toLowerCase()))) {
    const rel = repoRel(abs);
    if (rel.startsWith("src/frontend/features/admin-languages/")) continue;
    const text = fs.readFileSync(abs, "utf8");
    const importRe = /(?:from\s+|require\s*\(\s*)['"]([^'"]+)['"]/g;
    let m: RegExpExecArray | null;
    while ((m = importRe.exec(text))) {
      const spec = m[1].replace(/\\/g, "/");
      if (deepRe.test(spec)) {
        violations.push(`${rel} -> ${spec}`);
      }
    }
  }
  assert.equal(violations.length, 0, violations.join("\n"));
  // public boundary itself must exist
  assert.ok(fs.existsSync(path.join(feRoot, "features/admin-languages/index.ts")));
  assert.ok(publicRe);
});
