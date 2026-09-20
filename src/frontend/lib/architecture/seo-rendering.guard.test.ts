import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const feRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

function read(rel: string): string {
  return fs.readFileSync(path.join(feRoot, rel), "utf8");
}

test("FE-SEO-001 characterization: home route remains async server entry with generateMetadata", () => {
  const src = read("app/page.tsx");
  assert.ok(!/^["']use client["']/m.test(src.split("\n").slice(0, 5).join("\n")));
  assert.ok(src.includes("generateMetadata"));
  assert.ok(src.includes("async function") || src.includes("export default async"));
  assert.ok(src.includes("buildStorePageMetadata") || src.includes("canonicalForLocale"));
});

test("FE-SEO-001 characterization: PDP and listing routes are not client-only entrypoints", () => {
  for (const rel of ["app/products/[slug]/page.tsx", "app/products/page.tsx", "app/category/[slug]/page.tsx"]) {
    const src = read(rel);
    const head = src.split("\n").slice(0, 8).join("\n");
    assert.ok(!/^["']use client["']/m.test(head), `${rel} must not be a client page entry`);
    assert.ok(src.includes("Metadata") || src.includes("metadata") || src.includes("generateMetadata"), rel);
  }
});

test("FE-SEO-001 characterization: blog article page keeps server metadata capability", () => {
  const src = read("app/blogs/[slug]/page.tsx");
  const head = src.split("\n").slice(0, 8).join("\n");
  assert.ok(!/^["']use client["']/m.test(head));
  assert.ok(src.includes("generateMetadata") || src.includes("metadata"));
});
