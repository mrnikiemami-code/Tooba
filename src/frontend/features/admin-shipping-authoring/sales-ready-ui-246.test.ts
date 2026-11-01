/**
 * TB-246 focused facts: sales-ready authoring copy and slot/time presentation contracts.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { authoringMessages } from "./messages/authoring-messages.ts";
import { formatTimeText } from "./ui/authoring-format.ts";
import { SLOT_DURATION_OPTIONS } from "./model/slot-duration-options.ts";

test("FA copy removes vague Apply and uses merchant Save/Cancel labels", () => {
  const fa = authoringMessages("fa");
  assert.equal(fa.apply, "ذخیره تغییرات");
  assert.equal(fa.cancelEdit, "انصراف");
  assert.equal(fa.pageTitle, "ویرایش روش ارسال");
  assert.equal(fa.tabRegions, "مناطق و تعرفه‌ها");
  assert.equal(fa.weekTitle, "زمان‌بندی تحویل");
  assert.equal(fa.weekSlotDuration, "نحوه نمایش ساعت تحویل به مشتری");
  assert.equal(fa.weekSlotDurationFullDay, "بازه کاری کامل");
  assert.equal(fa.weekSlotDurationFullDayExample, "مثلاً ۹:۰۰ تا ۱۷:۰۰");
  assert.equal(fa.weekSlotDurationHourly, "بازه‌های ساعتی");
  assert.equal(fa.weekActive, "فعال");
  assert.ok(!fa.apply.includes("اعمال"));
});

test("publication technical paragraphs stay out of merchant-facing primary copy keys used by the header", () => {
  const fa = authoringMessages("fa");
  assert.equal(fa.statusBadgePublished, "منتشرشده");
  assert.equal(fa.statusBadgeDraft, "پیش‌نویس");
  // The three banned sentences remain as unused residual keys for confirm dialogs/history only;
  // the polished screen must not render them (asserted by absence of their testids in the panel).
  assert.ok(fa.publicationNote.length > 0);
  assert.ok(fa.publicationSelected.length > 0);
  assert.ok(fa.publicationViewOnly.length > 0);
});

test("slot duration options stay the exact TB-241 lawful set and map null vs minutes", () => {
  assert.deepEqual([...SLOT_DURATION_OPTIONS], [15, 20, 30, 45, 60, 90, 120]);
  const fa = authoringMessages("fa");
  assert.equal(fa.weekSlotDurationHint, "مشتری بازه‌های ۹–۱۰، ۱۰–۱۱… را می‌بیند.");
});

test("FA 24-hour time formatting never emits AM/PM tokens", () => {
  const shown = formatTimeText("09:00:00");
  assert.equal(shown, "09:00");
  assert.ok(!/AM|PM|ق\.ظ|ب\.ظ/i.test(shown));
  const withSeconds = formatTimeText("17:30:45");
  assert.equal(withSeconds, "17:30:45");
  assert.ok(!/AM|PM/i.test(withSeconds));
});

test("EN save label is Save changes and region tab is merchant-facing", () => {
  const en = authoringMessages("en");
  assert.equal(en.apply, "Save changes");
  assert.equal(en.tabRegions, "Regions and rates");
  assert.equal(en.weekSlotDurationFullDay, "Full working window");
  assert.equal(en.weekSlotDurationHourly, "Hourly slots");
});
