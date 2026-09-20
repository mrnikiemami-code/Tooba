import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const repoRoot = path.resolve(feRoot, "../..");
const baselinePath = path.join(
  repoRoot,
  "docs/evidence/TB-TMAR-FE-BASELINE/frontend-source-size-baseline.json",
);

const EXT = new Set([".ts", ".tsx", ".js", ".jsx", ".mjs", ".cjs"]);
const EXCL = new Set(["node_modules", ".next", "dist", "build", "coverage", ".git", "tmp", ".tmp"]);

type BaselineFile = { path: string; loc: number; classification: string };
type Baseline = { thresholdNewFileLoc: number; files: BaselineFile[] };

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

function physicalLoc(text: string): number {
  return text.split(/\r\n|\n/).length;
}

function repoRel(abs: string): string {
  return path.relative(repoRoot, abs).replace(/\\/g, "/");
}

test("FE-SIZE: oversized frontend files are shrink-only against baseline", () => {
  const baseline = JSON.parse(fs.readFileSync(baselinePath, "utf8")) as Baseline;
  const threshold = baseline.thresholdNewFileLoc ?? 800;
  const baselineMap = new Map(
    baseline.files
      .filter((f) => f.classification === "OVERSIZED_LEGACY" || f.classification === "CRITICAL_GOD_FILE")
      .map((f) => [f.path, f.loc]),
  );

  const sources = walk(feRoot).filter((f) => EXT.has(path.extname(f).toLowerCase()));
  const violations: string[] = [];

  for (const abs of sources) {
    const rel = repoRel(abs);
    if (path.basename(rel).startsWith(".tmp-")) continue;
    const loc = physicalLoc(fs.readFileSync(abs, "utf8"));
    const baseLoc = baselineMap.get(rel);
    if (baseLoc !== undefined) {
      if (loc > baseLoc) {
        violations.push(`OVERSIZED_GROWTH ${rel}: ${loc} > baseline ${baseLoc}`);
      }
      continue;
    }
    if (loc > threshold) {
      violations.push(`NEW_OVERSIZED_FILE ${rel}: ${loc} > ${threshold}`);
    }
  }

  for (const [rel] of baselineMap) {
    const abs = path.join(repoRoot, rel);
    if (!fs.existsSync(abs)) {
      violations.push(`BASELINE_ENTRY_MISSING_FILE ${rel}`);
    }
  }

  assert.equal(violations.length, 0, violations.join("\n"));
});
