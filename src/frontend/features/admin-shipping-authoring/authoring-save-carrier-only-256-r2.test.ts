/**
 * TB-256-R2 — the ONE editor-document save gate must accept a CARRIER-CALENDAR-ONLY edit.
 *
 * Reproduced user defect (canonical FE3000 → Host5088, task-owned draft): the operator edited only
 * the «پذیرش حامل» tab, Save was enabled, and pressing it sent ZERO PUT — no error, no notice, and a
 * reload showed the old values. Root cause: the session's serializable-patch predicate omitted
 * `carrierSchedule`, so `selectApplyRequest` reported `no-changes` while the screen's raw local-dirty
 * signal still enabled the button.
 *
 * These facts pin the seam, not a synthetic model: a carrier-only patch must be READY, must be the
 * exact body with `carrierSchedule`, and must NOT echo the untouched customer week.
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  createEditorSessionState,
  decodeEditorDocument,
  parseJsonNode,
  reduceEditorSession,
  selectApplyRequest,
} from "./authoring-core.ts";
import type { AreaAncestryResolver, EditorDocument, EditorSessionState } from "./authoring-core.ts";
import type { CarrierScheduleEdit } from "./model/editor-edits.ts";

const DRAFT = "01a1beed-d7df-7000-bfc8-0185ea61afe3";
const LANGUAGE_FA = "44444444-4444-4444-8444-444444444444";
const LANGUAGE_EN = "55555555-5555-4555-8555-555555555555";

const resolver: AreaAncestryResolver = { resolve: () => null };

function wire(revision: number, withCarrier: boolean): string {
  const days = [1, 2, 3, 4, 5, 6, 7].map((weekday) =>
    weekday <= 5
      ? { weekday, isWorking: true, startTime: "09:00:00", endTime: "17:00:00" }
      : { weekday, isWorking: false },
  );
  return JSON.stringify({
    draftId: DRAFT,
    owner: { ownerKind: "Store" },
    serviceCode: "tipax",
    optionCode: "standard",
    revision,
    createdAt: "2026-11-09T06:10:20+00:00",
    updatedAt: "2026-11-09T06:11:06+00:00",
    type: { draftId: DRAFT, revision, resolution: "Unconfigured" },
    presentation: {
      methodTranslations: [],
      groupTitles: [],
      contentLanguages: [
        { languageId: LANGUAGE_FA, code: "fa-IR", culture: "fa-IR", urlPrefix: "fa", direction: "rtl", displayName: "فارسی", nativeName: "فارسی", isDefault: true, sortOrder: 0 },
        { languageId: LANGUAGE_EN, code: "en-US", culture: "en-US", urlPrefix: "en", direction: "ltr", displayName: "English", nativeName: "English", isDefault: false, sortOrder: 1 },
      ],
      defaultContentLanguageId: LANGUAGE_FA,
      hasDefaultLanguageName: false,
    },
    weeklySchedule: { timeZoneId: "Asia/Tehran", days, slotDurationMinutes: 60 },
    ...(withCarrier
      ? { carrierSchedule: { timeZoneId: "Asia/Tehran", days } }
      : {}),
    regionGroups: [],
    destinationFacts: [],
  });
}

function document(revision: number, withCarrier = true): EditorDocument {
  const node = parseJsonNode(wire(revision, withCarrier));
  assert.ok(node);
  const decoded = decodeEditorDocument(node, DRAFT);
  if (!decoded.ok) throw new Error(decoded.reason);
  return decoded.value;
}

function loaded(withCarrier = true, revision = 3): EditorSessionState {
  const started = reduceEditorSession(createEditorSessionState(DRAFT), { type: "loadStarted", draftId: DRAFT });
  return reduceEditorSession(started, {
    type: "loadSucceeded",
    draftId: DRAFT,
    epoch: started.epoch,
    document: document(revision, withCarrier),
  });
}

/** Exactly what the carrier tab's single dispatch produces: the weekdays are untouched. */
function carrierOnlyEdit(): CarrierScheduleEdit {
  return {
    timeZoneId: "Asia/Tehran",
    days: [
      { weekday: 1, isWorking: true, startTime: "09:00:00", endTime: "17:00:00" },
      { weekday: 2, isWorking: true, startTime: "09:00:00", endTime: "17:00:00" },
      { weekday: 3, isWorking: true, startTime: "10:00:00", endTime: "14:00:00" },
      { weekday: 4, isWorking: true, startTime: "09:00:00", endTime: "17:00:00" },
      { weekday: 5, isWorking: true, startTime: "09:00:00", endTime: "17:00:00" },
      { weekday: 6, isWorking: false, startTime: null, endTime: null },
      { weekday: 7, isWorking: false, startTime: null, endTime: null },
    ],
  };
}

test("TB-256-R2: a carrier-only edit is a real unsaved patch and yields ONE ready Apply request", () => {
  const state = reduceEditorSession(loaded(), {
    type: "edited",
    draftId: DRAFT,
    edits: { carrierSchedule: carrierOnlyEdit() },
  });
  assert.equal(state.unsaved, true, "editing only the carrier calendar must count as unsaved work");

  const request = selectApplyRequest(state, 1, resolver);
  assert.equal(request.status, "ready", "the saved carrier calendar must be submittable, not refused as no-changes");
  if (request.status !== "ready") return;
  assert.equal(request.expectedRevision, 3);

  const body = JSON.parse(request.body.text) as Record<string, unknown>;
  assert.equal(body.expectedRevision, 3);
  // The carrier calendar is the patch…
  const carrier = body.carrierSchedule as { timeZoneId: string; days: { weekday: number; endTime: string | null }[] };
  assert.equal(carrier.timeZoneId, "Asia/Tehran");
  assert.equal(carrier.days.find((day) => day.weekday === 3)?.endTime, "14:00:00");
  assert.equal(carrier.days.length, 7);
  // …and the independent customer week is PRESERVED (never echoed, never replaced).
  assert.equal(Object.prototype.hasOwnProperty.call(body, "weeklySchedule"), false);
  assert.equal(Object.prototype.hasOwnProperty.call(body, "methodSettings"), false);

  const started = reduceEditorSession(state, { type: "saveStarted", draftId: DRAFT, epoch: state.epoch, requestId: 1 });
  assert.equal(started.save, "saving", "the in-flight save must be accepted, so exactly one PUT can be sent");
});

test("TB-256-R2: the confirmed save clears the carrier patch and adopts the new revision", () => {
  const edited = reduceEditorSession(loaded(), {
    type: "edited",
    draftId: DRAFT,
    edits: { carrierSchedule: carrierOnlyEdit() },
  });
  const started = reduceEditorSession(edited, { type: "saveStarted", draftId: DRAFT, epoch: edited.epoch, requestId: 1 });
  const saved = reduceEditorSession(started, {
    type: "saveSucceeded",
    draftId: DRAFT,
    epoch: started.epoch,
    requestId: 1,
    document: document(4),
  });
  assert.equal(saved.save, "clean");
  assert.equal(saved.unsaved, false);
  assert.equal(saved.baseline?.revision, 4);
  assert.equal(saved.baseline?.weeklySchedule?.days[0]?.endTime, "17:00:00", "the customer week is untouched");
  // No residual intent: a second Apply cannot resend the same carrier patch.
  assert.equal(selectApplyRequest(saved, 2, resolver).status, "blocked");
});

test("TB-256-R2: an unconfigured-to-configured carrier calendar (copy) is also submittable", () => {
  const state = reduceEditorSession(loaded(false), {
    type: "edited",
    draftId: DRAFT,
    edits: { carrierSchedule: carrierOnlyEdit() },
  });
  assert.equal(state.unsaved, true);
  const request = selectApplyRequest(state, 1, resolver);
  assert.equal(request.status, "ready");
  if (request.status !== "ready") return;
  const body = JSON.parse(request.body.text) as Record<string, unknown>;
  assert.ok(body.carrierSchedule, "a first-time configured carrier calendar must reach the server");
});

test("TB-256-R2: a rejected carrier-only save keeps the operator's edits for correction", () => {
  const edited = reduceEditorSession(loaded(), {
    type: "edited",
    draftId: DRAFT,
    edits: { carrierSchedule: carrierOnlyEdit() },
  });
  const started = reduceEditorSession(edited, { type: "saveStarted", draftId: DRAFT, epoch: edited.epoch, requestId: 1 });
  const failed = reduceEditorSession(started, {
    type: "saveFailed",
    draftId: DRAFT,
    epoch: started.epoch,
    requestId: 1,
    failure: { kind: "validation", status: 400, errorCode: "shipping.authoring.schedule_day_invalid", title: null, errors: [] },
  });
  assert.equal(failed.save, "validationFailed");
  assert.equal(failed.unsaved, true);
  assert.deepEqual(failed.edits.carrierSchedule, carrierOnlyEdit());
});
