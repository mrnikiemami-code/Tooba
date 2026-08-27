import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const shellSource = fs.readFileSync(path.join(root, "app/admin/admin-shell.tsx"), "utf8");

test("admin shell marks settings nav live and clears deferred list", () => {
  const settingsIdx = shellSource.indexOf('href: "/admin/settings"');
  assert.ok(settingsIdx >= 0, "settings href missing");
  assert.ok(shellSource.slice(settingsIdx, settingsIdx + 120).includes("live: true"), "settings must be live: true");

  const deferredStart = shellSource.indexOf("export const ADMIN_DEFERRED_NAV_HREFS");
  assert.ok(deferredStart >= 0, "ADMIN_DEFERRED_NAV_HREFS missing");
  const deferredBlock = shellSource.slice(
    deferredStart,
    shellSource.indexOf("] as const;", deferredStart) + 11,
  );
  assert.equal(deferredBlock.includes("/admin/settings"), false, "settings must not remain deferred");
});
