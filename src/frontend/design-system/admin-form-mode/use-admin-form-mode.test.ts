import assert from "node:assert/strict";
import test from "node:test";
import {
  cancelAdminEditMode,
  completeAdminSave,
  createAdminFormModeState,
  enterAdminEditMode,
  markAdminFormDirty,
  reduceAdminFormMode,
} from "./use-admin-form-mode.ts";

test("default mode is view", () => {
  const s = createAdminFormModeState(true, true);
  assert.equal(s.mode, "view");
  assert.equal(s.canEdit, true);
  assert.equal(s.isDirty, false);
});

test("view-only cannot enter edit", () => {
  const s = createAdminFormModeState(true, false);
  assert.equal(enterAdminEditMode(s).mode, "view");
});

test("editor can enter edit, cancel, and save", () => {
  let s = createAdminFormModeState(true, true);
  s = enterAdminEditMode(s);
  assert.equal(s.mode, "edit");
  s = markAdminFormDirty(s);
  assert.equal(s.isDirty, true);
  s = cancelAdminEditMode(s);
  assert.equal(s.mode, "view");
  assert.equal(s.isDirty, false);

  s = enterAdminEditMode(s);
  s = markAdminFormDirty(s);
  s = completeAdminSave(s);
  assert.equal(s.mode, "view");
  assert.equal(s.isDirty, false);
});

test("reducer capabilities reset edit when canEdit revoked", () => {
  let s = createAdminFormModeState(true, true);
  s = reduceAdminFormMode(s, { type: "edit" });
  s = reduceAdminFormMode(s, { type: "capabilities", canView: true, canEdit: false });
  assert.equal(s.mode, "view");
  assert.equal(s.canEdit, false);
});
