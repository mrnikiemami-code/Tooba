import assert from "node:assert/strict";
import test from "node:test";
import { createCsrfToken, validateCsrf } from "./csrf.ts";

test("csrf validation accepts matching header and cookie", () => {
  const token = createCsrfToken();
  const request = new Request("http://localhost/api/auth/login", {
    method: "POST",
    headers: { "X-Tooba-Csrf": token },
  });
  assert.equal(validateCsrf(request, token), true);
});

test("csrf validation rejects missing header", () => {
  const request = new Request("http://localhost/api/auth/login", { method: "POST" });
  assert.equal(validateCsrf(request, createCsrfToken()), false);
});
