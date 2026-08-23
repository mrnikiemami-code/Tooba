import assert from "node:assert/strict";
import { test } from "node:test";
import {
  clearSectionDirty,
  deserializeMasterDetailReturn,
  deserializeWorkspaceNavigation,
  hasUnsavedChanges,
  markSectionDirty,
  nextCommandState,
  resolveWorkspaceAction,
  serializeMasterDetailReturn,
  serializeWorkspaceNavigation,
  shouldBlockNavigation,
} from "./state.ts";

test("dirty section tracking", () => {
  const dirty = markSectionDirty(new Set(), "core");
  assert.equal(hasUnsavedChanges(dirty), true);
  assert.equal(hasUnsavedChanges(clearSectionDirty(dirty, "core")), false);
});

test("unsaved changes block other sections", () => {
  const dirty = markSectionDirty(new Set(), "core");
  assert.equal(shouldBlockNavigation(dirty, "core", "media"), true);
  assert.equal(shouldBlockNavigation(dirty, "core", "core"), false);
});

test("permission action resolution", () => {
  assert.equal(resolveWorkspaceAction(false, true, true, "view"), "hidden");
  assert.equal(resolveWorkspaceAction(true, false, false, "edit"), "denied");
  assert.equal(resolveWorkspaceAction(true, true, true, "execute"), "allowed");
});

test("conflict and command transitions", () => {
  assert.equal(nextCommandState("idle", "submit"), "submitting");
  assert.equal(nextCommandState("submitting", "conflict"), "conflicted");
  assert.equal(nextCommandState("conflicted", "reset"), "idle");
  assert.equal(nextCommandState("idle", "fail"), "idle");
});

test("section navigation serialization", () => {
  const raw = serializeWorkspaceNavigation("pricing");
  assert.equal(deserializeWorkspaceNavigation(raw).sectionId, "pricing");
});

test("master-detail return state", () => {
  const raw = serializeMasterDetailReturn({ listQuery: "q=ops", selectedId: "row-1" });
  const parsed = deserializeMasterDetailReturn(raw);
  assert.equal(parsed.listQuery, "q=ops");
  assert.equal(parsed.selectedId, "row-1");
});
