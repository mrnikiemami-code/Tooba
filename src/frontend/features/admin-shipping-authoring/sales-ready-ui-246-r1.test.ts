/**
 * TB-246-R1 focused facts: hit targets, shared slot authority, schedule/destination/rate contracts.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import { authoringMessages } from "./messages/authoring-messages.ts";
import { SLOT_DURATION_OPTIONS } from "./model/slot-duration-options.ts";
import { formatTimeText } from "./ui/authoring-format.ts";
import { WEEK_DISPLAY_ORDER } from "./model/editor-week.ts";

const fa = authoringMessages("fa");

test("production SLOT_DURATION_OPTIONS is the shared authority (not duplicated in tests)", () => {
  assert.deepEqual([...SLOT_DURATION_OPTIONS], [15, 20, 30, 45, 60, 90, 120]);
});

test("week editor source renders seven day testids and FA 24h time inputs", async () => {
  const week = await readFile(new URL("ui/authoring-week-editor.tsx", import.meta.url), "utf8");
  for (const weekday of WEEK_DISPLAY_ORDER) {
    assert.match(week, new RegExp(`shipping-authoring-week-day-\\$\\{weekday\\}|shipping-authoring-week-day-`));
  }
  assert.match(week, /WEEK_DISPLAY_ORDER\.map/);
  assert.match(week, /data-testid=\{`shipping-authoring-week-day-\$\{weekday\}`\}/);
  assert.match(week, /data-time-format="24h"/);
  assert.match(week, /weekActive/);
  assert.match(week, /weekClosed/);
  assert.match(week, /SLOT_DURATION_OPTIONS/);
  assert.equal(formatTimeText("09:00:00"), "09:00");
  assert.equal(/AM|PM/i.test(formatTimeText("17:30:00")), false);
});

test("merchant copy keeps save label and destination add wording", () => {
  assert.equal(fa.apply, "ذخیره تغییرات");
  assert.equal(fa.destinationOpenAction, "افزودن مقصد");
  assert.equal(fa.cancelEdit, "انصراف");
});

test("interactive class contract forbids min-h-7/8/9 on authoring UI sources", async () => {
  const files = [
    "ui/authoring-groups-editor.tsx",
    "ui/authoring-week-editor.tsx",
    "ui/authoring-general-tab.tsx",
    "ui/authoring-screen.tsx",
    "ui/authoring-rates-editor.tsx",
    "ui/authoring-destination-picker.tsx",
    "ui/authoring-destination-field.tsx",
    "ui/authoring-money-field.tsx",
    "ui/authoring-rate-modal.tsx",
  ];
  for (const file of files) {
    const text = await readFile(new URL(file, import.meta.url), "utf8");
    assert.equal(/min-h-[789]\b/.test(text), false, `${file} still has sub-44px min-h`);
  }
});

test("screen keeps single header ownership and hides edit-mode publication chrome", async () => {
  const screen = await readFile(new URL("ui/authoring-screen.tsx", import.meta.url), "utf8");
  const publication = await readFile(new URL("ui/authoring-publication-panel.tsx", import.meta.url), "utf8");
  assert.match(screen, /data-testid="shipping-authoring-page-header"/);
  assert.match(screen, /data-testid="shipping-authoring-sticky-save"/);
  assert.match(screen, /data-testid="shipping-authoring-apply"/);
  assert.match(publication, /mode === "edit"/);
  assert.equal(publication.includes("shipping-authoring-publication-note"), false);
  assert.equal(publication.includes("shipping-authoring-publication-status"), false);
});

test("destination remove and rate actions retain stable testids and handler wiring", async () => {
  const groups = await readFile(new URL("ui/authoring-groups-editor.tsx", import.meta.url), "utf8");
  assert.match(groups, /data-testid="shipping-authoring-area-remove"/);
  assert.match(groups, /data-testid="shipping-authoring-rate-add"/);
  assert.match(groups, /onRemoveArea\(group\.key, area\)/);
  assert.match(groups, /data-testid="shipping-authoring-group-up"/);
  assert.match(groups, /shipping-authoring-area-breadcrumb/);
  assert.match(groups, /min-h-11/);
});

test("save path still uses existing apply control testid and messages.apply", async () => {
  const screen = await readFile(new URL("ui/authoring-screen.tsx", import.meta.url), "utf8");
  assert.match(screen, /data-testid="shipping-authoring-apply"/);
  assert.match(screen, /messages\.apply/);
  assert.match(screen, /void apply\(\)/);
});
