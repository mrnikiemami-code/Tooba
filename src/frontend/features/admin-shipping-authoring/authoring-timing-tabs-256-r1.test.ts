import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  isTimingTabKey,
  nextTimingTabIndex,
  TIMING_TAB_IDS,
} from "./ui/timing-tab-navigation.ts";

const here = path.dirname(fileURLToPath(import.meta.url));
const screenSource = fs.readFileSync(path.join(here, "ui/authoring-screen.tsx"), "utf8");

test("TB-256-R1: the timing calendars are the same underlined tabs as General/Regions", () => {
  // The tablist owns the two independent calendars…
  assert.match(screenSource, /role="tablist"[\s\S]{0,400}TIMING_TABS\.map/);
  assert.match(screenSource, /role="tab"/);
  assert.match(screenSource, /role="tabpanel"/);
  assert.match(screenSource, /aria-selected=\{timingTab === id\}/);
  assert.match(screenSource, /aria-controls=\{`shipping-authoring-timing-panel-\$\{id\}`\}/);
  assert.match(screenSource, /aria-labelledby="shipping-authoring-timing-tab-/);
  // Roving tabindex: exactly one tab is reachable by Tab, the rest by arrow keys.
  assert.match(screenSource, /tabIndex=\{timingTab === id \? 0 : -1\}/);

  // …and it is styled as a bottom-bordered tab, NOT as a filled pill button.
  assert.match(screenSource, /-mb-px min-h-11 border-b-2 border-primary/);
  assert.match(screenSource, /-mb-px min-h-11 border-b-2 border-transparent/);
  const timingBlock = screenSource.slice(
    screenSource.indexOf("TIMING_TABS.map"),
    screenSource.indexOf("shipping-authoring-timing-panel-customer"),
  );
  assert.doesNotMatch(timingBlock, /rounded-full/);
  assert.doesNotMatch(timingBlock, /bg-primary/);
});

test("TB-256-R1: switching timing tabs is presentation only — both editors stay mounted", () => {
  assert.match(screenSource, /hidden=\{timingTab !== "customer"\}/);
  assert.match(screenSource, /hidden=\{timingTab !== "carrier"\}/);
  // No reset/disable is wired to the tab switch: the setter is the only local change.
  assert.match(screenSource, /onClick=\{\(\) => setTimingTab\(id\)\}/);
  assert.doesNotMatch(screenSource, /setTimingTab\([^)]*\)\s*\n\s*(?:changeWeek|changeCarrier|resetWeek|resetCarrierWeek)\(/);
});

test("TB-256-R1: keyboard navigation is direction-aware and wraps", () => {
  // TB-264-R1 added the independent processing calendar as a THIRD tab in the same tablist.
  assert.deepEqual([...TIMING_TAB_IDS], ["customer", "carrier", "processing"]);

  // LTR: ArrowRight moves forward, ArrowLeft backward.
  assert.equal(nextTimingTabIndex(0, "ArrowRight", false), 1);
  assert.equal(nextTimingTabIndex(1, "ArrowRight", false), 2);
  assert.equal(nextTimingTabIndex(2, "ArrowLeft", false), 1);

  // RTL: the SAME physical keys move the other way in reading order.
  assert.equal(nextTimingTabIndex(0, "ArrowLeft", true), 1);
  assert.equal(nextTimingTabIndex(2, "ArrowRight", true), 1);

  // Both directions wrap instead of stopping at the edge.
  assert.equal(nextTimingTabIndex(2, "ArrowRight", false), 0);
  assert.equal(nextTimingTabIndex(0, "ArrowLeft", false), 2);

  // Home/End mean first/last in ANY direction.
  assert.equal(nextTimingTabIndex(2, "Home", true), 0);
  assert.equal(nextTimingTabIndex(0, "End", false), 2);

  // Keys the tablist does not own keep their default behaviour (no focus move).
  assert.equal(isTimingTabKey("Enter"), false);
  assert.equal(isTimingTabKey("Tab"), false);
  assert.equal(nextTimingTabIndex(1, "Enter", false), 1);
  assert.equal(nextTimingTabIndex(0, "Tab", true), 0);

  // Degenerate input never throws and never invents a tab.
  assert.equal(nextTimingTabIndex(0, "ArrowRight", false, 0), 0);
  // An out-of-range current index restarts from the first tab instead of failing.
  assert.equal(nextTimingTabIndex(7, "ArrowRight", false), 1);
});
