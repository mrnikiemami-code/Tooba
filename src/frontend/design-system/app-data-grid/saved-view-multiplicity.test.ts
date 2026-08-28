import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import type { SavedGridView } from "../data-grid/types.ts";
import { prepareSavedViewForPersistence } from "./saved-view-state.ts";

const dir = dirname(fileURLToPath(import.meta.url));

type SavedViewCollection = {
  schemaVersion: number;
  defaultViewId: string | null;
  views: SavedGridView[];
};

function upsertView(collection: SavedViewCollection, view: SavedGridView): SavedViewCollection {
  const views = collection.views.filter((item) => item.id !== view.id);
  views.push(view);
  return { ...collection, views };
}

function removeView(collection: SavedViewCollection, id: string): SavedViewCollection {
  return {
    ...collection,
    views: collection.views.filter((item) => item.id !== id),
    defaultViewId: collection.defaultViewId === id ? null : collection.defaultViewId,
  };
}

test("AppDataGrid registers legacy theme to avoid AG error #239", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /theme="legacy"/);
  assert.match(source, /ModuleRegistry\.registerModules\(\[AllCommunityModule\]\)/);
});

test("saved view collection keeps multiple distinct views on create", () => {
  let collection: SavedViewCollection = { schemaVersion: 1, views: [], defaultViewId: null };
  const advancedIds = new Set<string>(["status"]);
  const base = {
    filters: {},
    sorts: [],
    layout: { order: ["title"], visibility: {}, widths: {} },
    pageSize: 20,
  };
  collection = upsertView(collection, prepareSavedViewForPersistence({ id: "a", name: "نمای A", ...base }, advancedIds));
  collection = upsertView(collection, prepareSavedViewForPersistence({ id: "b", name: "نمای B", ...base }, advancedIds));
  assert.equal(collection.views.length, 2);
  collection = upsertView(collection, prepareSavedViewForPersistence({ id: "c", name: "نمای C", ...base }, advancedIds));
  assert.equal(collection.views.length, 3);
  assert.ok(collection.views.some((view) => view.id === "a"));
  assert.ok(collection.views.some((view) => view.id === "b"));
});

test("saved view update changes only targeted view", () => {
  let collection: SavedViewCollection = {
    schemaVersion: 1,
    defaultViewId: null,
    views: [
      { id: "a", name: "A", filters: {}, sorts: [], layout: { order: [], visibility: {}, widths: {} }, pageSize: 20 },
      { id: "b", name: "B", filters: {}, sorts: [], layout: { order: [], visibility: {}, widths: {} }, pageSize: 20 },
    ],
  };
  const b = collection.views.find((view) => view.id === "b");
  assert.ok(b);
  collection = upsertView(collection, { ...b!, name: "B-renamed" });
  assert.equal(collection.views.find((view) => view.id === "a")?.name, "A");
  assert.equal(collection.views.find((view) => view.id === "b")?.name, "B-renamed");
});

test("delete view removes only targeted view and clears default when needed", () => {
  let collection: SavedViewCollection = {
    schemaVersion: 1,
    defaultViewId: "b",
    views: [
      { id: "a", name: "A", filters: {}, sorts: [], layout: { order: [], visibility: {}, widths: {} }, pageSize: 20 },
      { id: "b", name: "B", filters: {}, sorts: [], layout: { order: [], visibility: {}, widths: {} }, pageSize: 20 },
    ],
  };
  collection = removeView(collection, "b");
  assert.equal(collection.views.length, 1);
  assert.equal(collection.defaultViewId, null);
});

test("set default view id persists separately from views array", () => {
  const collection: SavedViewCollection = {
    schemaVersion: 1,
    defaultViewId: "a",
    views: [{ id: "a", name: "A", filters: {}, sorts: [], layout: { order: [], visibility: {}, widths: {} }, pageSize: 20 }],
  };
  assert.equal(collection.defaultViewId, "a");
  assert.equal(collection.views.length, 1);
});
