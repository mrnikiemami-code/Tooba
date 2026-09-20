import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const repoRoot = path.resolve(feRoot, "../..");
const baselinePath = path.join(repoRoot, "docs/evidence/TB-TMAR-FE-F1/frontend-flat-folder-baseline.json");

const ROUTE_NAMES = new Set([
  "page.tsx", "page.ts", "layout.tsx", "layout.ts", "loading.tsx", "loading.ts",
  "error.tsx", "error.ts", "not-found.tsx", "not-found.ts", "template.tsx", "template.ts",
  "default.tsx", "default.ts", "route.ts", "route.js",
]);

type Baseline = {
  flatBusinessFeatureFiles: string[];
  approvedRouteConventionFiles: string[];
  approvedCompatibilityShims: string[];
  adminApiExportNames: string[];
  adminApiDumpingGround: string;
};

function listFlatAdminFiles(): string[] {
  const dir = path.join(feRoot, "app/admin");
  return fs
    .readdirSync(dir)
    .filter((n) => {
      const full = path.join(dir, n);
      return fs.statSync(full).isFile() && /\.(ts|tsx|js|jsx|css)$/.test(n);
    })
    .map((n) => "src/frontend/app/admin/" + n)
    .sort();
}

function collectAdminApiExports(text: string): string[] {
  const exportNames: string[] = [];
  const re =
    /^export (?:async )?function ([A-Za-z0-9_]+)|^export (?:const|type|interface|class) ([A-Za-z0-9_]+)|^export \{([^}]+)\}/gm;
  let m: RegExpExecArray | null;
  while ((m = re.exec(text))) {
    if (m[1]) exportNames.push(m[1]);
    else if (m[2]) exportNames.push(m[2]);
    else if (m[3]) {
      for (const part of m[3].split(",")) {
        const name = part.trim().replace(/^type\s+/, "").split(/\s+as\s+/).pop()!.trim();
        if (name) exportNames.push(name);
      }
    }
  }
  return [...new Set(exportNames)].sort();
}

test("FE-FOLDER-001: no new business-feature files directly under app/admin", () => {
  const baseline = JSON.parse(fs.readFileSync(baselinePath, "utf8")) as Baseline;
  const allowed = new Set([
    ...baseline.flatBusinessFeatureFiles,
    ...baseline.approvedRouteConventionFiles,
    ...baseline.approvedCompatibilityShims,
  ]);
  const actual = listFlatAdminFiles();
  const novel = actual.filter((p) => !allowed.has(p) && !ROUTE_NAMES.has(path.basename(p)));
  const novelRoutes = actual.filter((p) => ROUTE_NAMES.has(path.basename(p)) && !allowed.has(p));
  // New route convention files under flat admin root are rare; allow only if baselined.
  assert.equal(novel.length, 0, `New flat admin business files:\n${novel.join("\n")}`);
  assert.equal(novelRoutes.length, 0, `Unexpected new route files at admin root:\n${novelRoutes.join("\n")}`);
});

test("FE-FOLDER-002: no new capability exports on admin-api dumping ground", () => {
  const baseline = JSON.parse(fs.readFileSync(baselinePath, "utf8")) as Baseline;
  const text = fs.readFileSync(path.join(repoRoot, baseline.adminApiDumpingGround), "utf8");
  const actual = collectAdminApiExports(text);
  const allowed = new Set(baseline.adminApiExportNames);
  const novel = actual.filter((n) => !allowed.has(n));
  assert.equal(novel.length, 0, `New admin-api exports:\n${novel.join("\n")}`);
});
