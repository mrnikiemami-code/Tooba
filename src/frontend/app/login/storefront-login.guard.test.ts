import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));
const page = fs.readFileSync(path.join(root, "page.tsx"), "utf8");
const ui = fs.readFileSync(path.join(root, "storefront-login.tsx"), "utf8");

test("canonical login page is locale-routed mobile+OTP without password UI", () => {
  assert.match(page, /StorefrontCustomerLogin/);
  assert.match(ui, /data-testid="storefront-login-page"/);
  assert.match(ui, /login-mobile-input/);
  assert.match(ui, /login-otp-input/);
  assert.match(ui, /\/api\/auth\/otp-request/);
  assert.match(ui, /\/api\/auth\/otp-complete/);
  assert.doesNotMatch(ui, /type="password"/);
  assert.doesNotMatch(ui, /identifierKind/);
});
