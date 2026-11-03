/**
 * Canonical frontend test discovery for node:test.
 * Discovers test and guard.test files under src/frontend excluding vendor/build.
 */
import fs from "node:fs";
import path from "node:path";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const EXCL = new Set(["node_modules", ".next", "dist", "build", "coverage", ".git", "tmp", ".tmp"]);

function walk(dir, acc = []) {
  for (const name of fs.readdirSync(dir)) {
    if (EXCL.has(name)) continue;
    const full = path.join(dir, name);
    const st = fs.statSync(full);
    if (st.isDirectory()) walk(full, acc);
    else acc.push(full);
  }
  return acc;
}

function isTestFile(abs) {
  const base = path.basename(abs);
  return /\.(guard\.)?test\.(ts|tsx|js|jsx)$/.test(base);
}

const files = walk(feRoot)
  .filter(isTestFile)
  .map((f) => path.relative(feRoot, f).replace(/\\/g, "/"))
  .sort();

if (process.argv.includes("--list")) {
  console.log(JSON.stringify({ count: files.length, files }, null, 2));
  process.exit(0);
}

if (files.length === 0) {
  console.error("No test files discovered");
  process.exit(1);
}

// Prefer tsx so mount tests can import .tsx React components (jsdom harness).
// Fall back to strip-types for environments without tsx.
const tsxCli = path.join(feRoot, "node_modules", "tsx", "dist", "cli.mjs");
const mountTsconfig = path.join(feRoot, "tsconfig.mount-tests.json");
const useTsx = fs.existsSync(tsxCli);
const tsxArgs = fs.existsSync(mountTsconfig)
  ? ["--tsconfig", mountTsconfig, "--test", ...files]
  : ["--test", ...files];
const result = useTsx
  ? spawnSync(process.execPath, [tsxCli, ...tsxArgs], {
      cwd: feRoot,
      stdio: "inherit",
      env: process.env,
    })
  : spawnSync(process.execPath, ["--experimental-strip-types", "--test", ...files], {
      cwd: feRoot,
      stdio: "inherit",
      env: process.env,
    });
process.exit(result.status ?? 1);
