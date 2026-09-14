import assert from "node:assert/strict";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));

function walk(start: string): string[] {
  const out: string[] = [];
  for (const name of readdirSync(start)) {
    const full = join(start, name);
    if (statSync(full).isDirectory()) {
      out.push(...walk(full));
      continue;
    }
    if (/\.(tsx|ts|css)$/.test(name) && !name.includes(".test.")) {
      out.push(full);
    }
  }
  return out;
}

const sources = [
  ...walk(dir),
  join(dir, "../payment/storefront-payment-handoff.tsx"),
  join(dir, "../payment/result/storefront-payment-result.tsx"),
  join(dir, "../payment/sandbox/storefront-payment-sandbox.tsx"),
  join(dir, "../order/confirmation/storefront-order-confirmation.tsx"),
].map((file) => ({ file, text: readFileSync(file, "utf8") }));

test("migrated storefront brand roles no longer hard-code Tooba/Shopeiva brand hex", () => {
  for (const { file, text } of sources) {
    assert.doesNotMatch(text, /#2563EB|#1[Dd]4[Ee][Dd]8|#E53935|#e53935|#d32f2f|#c62828|#b71c1c/, file);
  }
});

test("Home PDP Shipping Cart use semantic primary tokens", () => {
  const home = readFileSync(join(dir, "storefront-home.tsx"), "utf8");
  const pdp = readFileSync(join(dir, "storefront-pdp.tsx"), "utf8");
  const shipping = readFileSync(join(dir, "storefront-shipping.tsx"), "utf8");
  const cart = readFileSync(join(dir, "storefront-cart.tsx"), "utf8");
  for (const source of [home, pdp, shipping, cart]) {
    assert.match(source, /bg-primary|text-primary|from-primary/);
    assert.doesNotMatch(source, /#2563EB|#E53935/);
  }
  assert.match(shipping, /text-red-600|text-danger|bg-red-50/);
});

test("pages do not define a local palette registry or appearance fetch", () => {
  for (const { file, text } of sources) {
    if (file.endsWith("storefront-appearance-api.ts")) continue;
    assert.doesNotMatch(text, /STOREFRONT_PALETTES\s*=/, file);
    assert.doesNotMatch(text, /\/v1\/storefront\/appearance/, file);
    assert.doesNotMatch(text, /setInterval\([^\)]*appearance/, file);
  }
});
