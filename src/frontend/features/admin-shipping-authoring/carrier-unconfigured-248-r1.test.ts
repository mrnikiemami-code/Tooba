import test from "node:test";
import assert from "node:assert/strict";
import { authoringMessages } from "./messages/authoring-messages.ts";

test("missing carrier credentials keep the required unconfigured copy in fa and en", () => {
  assert.equal(authoringMessages("fa").carrierUnconfigured, "اتصال شرکت حمل تنظیم نشده است");
  assert.equal(authoringMessages("en").carrierUnconfigured, "Carrier connection is not configured.");
  assert.ok(authoringMessages("fa").carrierLabelUnsupported.includes("برچسب"));
  assert.ok(authoringMessages("en").carrierLabelUnsupported.toLowerCase().includes("label"));
});
