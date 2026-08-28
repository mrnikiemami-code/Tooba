import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const dir = dirname(fileURLToPath(import.meta.url));

test("app column header uses shared lucide filter icon", () => {
  const header = readFileSync(join(dir, "app-column-header.tsx"), "utf8");
  const icon = readFileSync(join(dir, "column-filter-icon.tsx"), "utf8");
  assert.match(header, /ColumnFilterIcon/);
  assert.match(icon, /from "lucide-react"/);
  assert.match(icon, /Filter/);
  assert.doesNotMatch(header, /⚲/);
});

test("toolbar advanced filter has no detached magnifier glyph", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  const advancedButton = source.match(/data-testid="app-grid-advanced-filters"[\s\S]{0,260}/);
  assert.ok(advancedButton);
  assert.doesNotMatch(advancedButton![0], /⚲/);
  assert.match(advancedButton![0], /<Filter/);
});

test("active filter trigger exposes data-filter-active state", () => {
  const header = readFileSync(join(dir, "app-column-header.tsx"), "utf8");
  assert.match(header, /data-filter-active=\{isActive \? "true" : "false"\}/);
});

test("number header filter panel supports Enter commit without live typing requests", () => {
  const panel = readFileSync(join(dir, "number-header-filter-panel.tsx"), "utf8");
  assert.match(panel, /event\.key === "Enter"/);
  assert.match(panel, /onApply/);
  assert.match(panel, /"between"/);
  assert.match(panel, /"blank"/);
});
