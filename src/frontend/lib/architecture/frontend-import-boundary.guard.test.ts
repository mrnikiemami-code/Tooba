import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const repoRoot = path.resolve(feRoot, "../..");
const baselinePath = path.join(
  repoRoot,
  "docs/evidence/TB-TMAR-FE-BASELINE/frontend-import-boundary-baseline.json",
);

const EXT = new Set([".ts", ".tsx", ".js", ".jsx"]);
const EXCL = new Set(["node_modules", ".next", "dist", "build", "coverage", ".git"]);

type Edge = { from: string; to: string };

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

function repoRel(abs: string): string {
  return path.relative(repoRoot, abs).replace(/\\/g, "/");
}

function isShared(rel: string): boolean {
  return rel.startsWith("src/frontend/design-system/") || rel.startsWith("src/frontend/lib/");
}

function looksFeatureTarget(spec: string): boolean {
  return (
    spec.includes("/app/admin/") ||
    spec.includes("/app/storefront/") ||
    /(^|\/)(\.\.\/)+app\/(admin|storefront)\//.test(spec) ||
    spec.includes("app/admin/") ||
    spec.includes("app/storefront/")
  );
}

function collectSuspicious(): Edge[] {
  const edges: Edge[] = [];
  const importRe =
    /(?:import\s+(?:type\s+)?(?:[\s\S]*?)\s+from\s+|require\s*\(\s*)['"]([^'"]+)['"]/g;
  for (const abs of walk(feRoot).filter((f) => EXT.has(path.extname(f).toLowerCase()))) {
    const from = repoRel(abs);
    if (!isShared(from)) continue;
    const text = fs.readFileSync(abs, "utf8");
    let m: RegExpExecArray | null;
    importRe.lastIndex = 0;
    while ((m = importRe.exec(text))) {
      const spec = m[1];
      if (!(spec.startsWith(".") || spec.startsWith("@/") || spec.startsWith("~/"))) continue;
      if (looksFeatureTarget(spec)) {
        edges.push({ from, to: spec });
      }
    }
  }
  return edges;
}

function key(e: Edge): string {
  return `${e.from} -> ${e.to}`;
}

test("FE-BOUNDARY-001: no new shared→feature reverse imports beyond baseline", () => {
  const baseline = JSON.parse(fs.readFileSync(baselinePath, "utf8")) as {
    suspiciousSharedToFeature: Edge[];
  };
  const allowed = new Set(baseline.suspiciousSharedToFeature.map(key));
  const actual = collectSuspicious();
  const novel = actual.filter((e) => !allowed.has(key(e)));
  assert.equal(
    novel.length,
    0,
    `New shared→feature imports:\n${novel.map(key).join("\n")}`,
  );
});
