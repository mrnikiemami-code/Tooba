import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { storefrontAccountLabel } from "./storefront-identity-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("logged-in with profile name uses name", () => {
  assert.equal(storefrontAccountLabel({ displayName: "علی رضایی", mobile: "09111111111" }, "حساب کاربری"), "علی رضایی");
});

test("logged-in without name uses mobile", () => {
  assert.equal(storefrontAccountLabel({ displayName: "", firstName: "", lastName: "", mobile: "09111111111" }, "حساب کاربری"), "09111111111");
});

test("generic account label is last fallback only", () => {
  assert.equal(storefrontAccountLabel({ displayName: "  ", mobile: "" }, "حساب کاربری"), "حساب کاربری");
});

test("first and last compose when displayName is absent", () => {
  assert.equal(storefrontAccountLabel({ firstName: "محمد", lastName: "امامی", mobile: "0912" }, "حساب کاربری"), "محمد امامی");
});

test("account label never uses shipping recipient fields", () => {
  const identity = fs.readFileSync(path.join(root, "app/storefront/storefront-identity-api.ts"), "utf8");
  const menu = fs.readFileSync(path.join(root, "app/storefront/storefront-account-menu.tsx"), "utf8");
  assert.doesNotMatch(identity, /recipientName|RecipientName/);
  assert.doesNotMatch(menu, /recipientName|RecipientName/);
  assert.match(menu, /header-account-label/);
  assert.match(menu, /loadStorefrontSession/);
});
