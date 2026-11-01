/**
 * TB-246-R2 focused facts: one toolbar, no overlay, compact slot/day layout, glyph ≤20px.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import { authoringMessages } from "./messages/authoring-messages.ts";
import { SLOT_DURATION_OPTIONS } from "./model/slot-duration-options.ts";
import { WEEK_DISPLAY_ORDER } from "./model/editor-week.ts";

const fa = authoringMessages("fa");

async function source(rel: string): Promise<string> {
  return readFile(new URL(rel, import.meta.url), "utf8");
}

test("edit mode exposes exactly one Save and one Cancel; no fixed overlay", async () => {
  const screen = await source("ui/authoring-screen.tsx");
  assert.equal((screen.match(/data-testid="shipping-authoring-apply"/g) ?? []).length, 1);
  assert.equal((screen.match(/data-testid="shipping-authoring-cancel-edit"/g) ?? []).length, 1);
  assert.equal(screen.includes("shipping-authoring-sticky-save"), false);
  assert.equal(screen.includes("shipping-authoring-sticky-cancel"), false);
  assert.equal(screen.includes("shipping-authoring-sticky-apply"), false);
  assert.equal(/fixed\s+inset-x-0\s+bottom-0/.test(screen), false);
  assert.match(screen, /data-testid="shipping-authoring-toolbar"/);
  assert.match(screen, /sticky top-0/);
  assert.match(screen, /shipping-authoring-page-header/);
});

test("native radio glyphs stay h-4 w-4; hit targets stay min-h-11 on labels", async () => {
  const general = await source("ui/authoring-general-tab.tsx");
  const week = await source("ui/authoring-week-editor.tsx");
  for (const text of [general, week]) {
    assert.equal(/type="radio"[\s\S]{0,120}min-h-11 min-w-11/.test(text), false);
    assert.match(text, /type="radio"[\s\S]{0,80}className="h-4 w-4/);
    assert.match(text, /min-h-11 cursor-pointer/);
  }
});

test("full-window hides duration select; hourly reveals one SLOT_DURATION_OPTIONS select", async () => {
  const week = await source("ui/authoring-week-editor.tsx");
  assert.match(week, /shipping-authoring-week-slot-full-day/);
  assert.match(week, /shipping-authoring-week-slot-hourly/);
  assert.match(week, /week\.slotDurationMinutes !== null \? \(/);
  assert.match(week, /shipping-authoring-week-slot-duration-select/);
  assert.match(week, /SLOT_DURATION_OPTIONS/);
  // Hourly duration select must not offer a contradictory full-window empty option.
  const selectBlock = week.slice(
    week.indexOf("shipping-authoring-week-slot-duration-select"),
    week.indexOf("shipping-authoring-week-slot-duration-select") + 900,
  );
  assert.equal(selectBlock.includes('option value=""'), false);
  assert.deepEqual([...SLOT_DURATION_OPTIONS], [15, 20, 30, 45, 60, 90, 120]);
});

test("seven weekdays render once as compact rows with bounded HH:mm width", async () => {
  const week = await source("ui/authoring-week-editor.tsx");
  assert.match(week, /WEEK_DISPLAY_ORDER\.map/);
  assert.equal(WEEK_DISPLAY_ORDER.length, 7);
  assert.match(week, /min-h-14/);
  assert.match(week, /w-\[7\.5rem\]/);
  assert.match(week, /divide-y divide-border/);
  // Closed days must not paint a second closed label under the status control.
  assert.equal(/weekClosed[\s\S]{0,80}shipping-authoring-week-bounds/.test(week), false);
  assert.equal(week.includes("shipping-authoring-week-bounds-"), false);
});

test("merchant slot label and short calculation segmented copy", () => {
  assert.equal(fa.weekSlotDuration, "نحوه نمایش ساعت تحویل به مشتری");
  assert.equal(fa.weekSlotDurationFullDay, "بازه کاری کامل");
  assert.equal(fa.weekSlotDurationHourly, "بازه‌های ساعتی");
  assert.equal(fa.calculationManual, "دستی");
  assert.equal(fa.calculationCarrier, "خودکار");
  assert.equal(fa.calculationFree, "رایگان");
  assert.equal(fa.apply, "ذخیره تغییرات");
  assert.equal(fa.cancelEdit, "انصراف");
});

test("save/day/slot callbacks retain original handler wiring", async () => {
  const screen = await source("ui/authoring-screen.tsx");
  const week = await source("ui/authoring-week-editor.tsx");
  assert.match(screen, /void apply\(\)/);
  assert.match(screen, /messages\.apply/);
  assert.match(week, /onSlotDuration\(null\)/);
  assert.match(week, /onSlotDuration\(60\)/);
  assert.match(week, /onDayWorking\(weekday, true\)/);
  assert.match(week, /onDayWorking\(weekday, false\)/);
  assert.match(week, /onDayTime\(weekday, "startTime"/);
  assert.match(week, /shipping-authoring-week-reset/);
});
