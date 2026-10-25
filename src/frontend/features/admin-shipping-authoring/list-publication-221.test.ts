/**
 * TB-221 focused facts for the primary method list and the publication action.
 *
 * These are the EXACT helpers the components call: the strict decoders of the authorized read payloads,
 * the pure publication state model (in-flight gate, stale/unknown discipline, phase) and the request body
 * builders. No rendering, no network, no fixtures that differ from the real wire shape.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { parseJsonNode, type JsonNode } from "./api/wire-json.ts";
import {
  decodeAuthoringMethodRows,
  decodeAuthoringContext,
  duplicateBindingDraftIds,
  methodBinding,
  storedMethodName,
} from "./api/authoring-method-list-api.ts";
import {
  decodeAuthoringPublication,
  publishRequestBody,
  withdrawRequestBody,
  type AuthoringPublicationView,
} from "./api/authoring-publication-api.ts";
import {
  beginRead,
  beginWrite,
  canPublish,
  canWithdraw,
  initialPublicationState,
  publicationBlockedReason,
  publicationPhase,
  publicationStateMessage,
  resolveRead,
  resolveWrite,
} from "./model/authoring-publication.ts";

const DRAFT = "01a16eeb-70b3-7000-87c2-564c82835496";
const OTHER_DRAFT = "11111111-2222-3333-4444-555555555555";
const FA = "aaaaaaaa-0000-4000-8000-000000000001";
const EN = "bbbbbbbb-0000-4000-8000-000000000002";
const SNAPSHOT = "cccccccc-0000-4000-8000-000000000003";

function nodeOf(value: unknown): JsonNode {
  const node = parseJsonNode(JSON.stringify(value));
  assert.ok(node, "fixture must be valid JSON");
  return node;
}

/* ————————————————— the REAL list wire ————————————————— */

test("list decode keeps the server draft id and reads the stored text of the real default language", () => {
  const rows = decodeAuthoringMethodRows(nodeOf({
    drafts: [
      {
        draftId: DRAFT,
        owner: { ownerKind: "Store", sellerPartyId: null },
        serviceCode: "post",
        optionCode: null,
        revision: 2,
        createdAt: "2026-10-25T00:00:00+00:00",
        updatedAt: "2026-10-25T00:00:00+00:00",
        regionGroups: [{ groupId: SNAPSHOT, order: 0, areas: [] }],
        methodTranslations: [
          { languageId: FA, name: "پست عادی", description: null },
          { languageId: EN, name: "Regular post", description: "x" },
        ],
      },
    ],
  }));

  assert.equal(rows.length, 1);
  const row = rows[0]!;
  assert.equal(row.id, DRAFT, "stable row identity is the server draft id, never a fabricated method code");
  assert.equal(row.draftId, DRAFT);
  assert.equal(row.revision, 2);
  assert.equal(row.areaCount, 1);
  assert.equal(methodBinding(row), "post", "a NULL option stays NULL and is never split onto post:standard");
  assert.equal(storedMethodName(row, FA), "پست عادی");
  assert.equal(storedMethodName(row, EN), "Regular post");
});

test("a missing default-language text is honest 'no name', never another locale's name", () => {
  const rows = decodeAuthoringMethodRows(nodeOf({
    drafts: [{
      draftId: DRAFT,
      serviceCode: "post",
      optionCode: "express",
      revision: 1,
      regionGroups: [],
      methodTranslations: [{ languageId: EN, name: "Express", description: null }],
    }],
  }));
  assert.equal(storedMethodName(rows[0]!, FA), null);
  assert.equal(storedMethodName(rows[0]!, EN), "Express");
  assert.equal(methodBinding(rows[0]!), "post:express");
});

test("the list refuses a rounded 64-bit revision and a duplicated draft row", () => {
  assert.throws(() => decodeAuthoringMethodRows(nodeOf({
    drafts: [{ draftId: DRAFT, serviceCode: "post", optionCode: null, revision: 9007199254740993, regionGroups: [] }],
  })));
  assert.throws(() => decodeAuthoringMethodRows(nodeOf({
    drafts: [
      { draftId: DRAFT, serviceCode: "post", optionCode: null, revision: 1, regionGroups: [] },
      { draftId: DRAFT, serviceCode: "post", optionCode: "express", revision: 1, regionGroups: [] },
    ],
  })));
});

test("the authoring context must resolve its own default language", () => {
  const context = decodeAuthoringContext(nodeOf({
    edition: "SingleStore",
    defaultCurrency: "IRR",
    contentLanguages: [
      { languageId: FA, code: "fa-IR", culture: "fa-IR", urlPrefix: "", direction: "rtl", displayName: "فارسی", nativeName: "فارسی", isDefault: true, sortOrder: 0 },
      { languageId: EN, code: "en-US", culture: "en-US", urlPrefix: "en", direction: "ltr", displayName: "English", nativeName: "English", isDefault: false, sortOrder: 1 },
    ],
    defaultContentLanguageId: FA,
  }));
  assert.equal(context.defaultContentLanguageId, FA);
  assert.equal(context.contentLanguages.length, 2);

  assert.throws(() => decodeAuthoringContext(nodeOf({
    contentLanguages: [{ languageId: EN, code: "en", displayName: "English", nativeName: "English", isDefault: true }],
    defaultContentLanguageId: FA,
  })));
});

test("two rows with the same exact binding are flagged, other bindings are not", () => {
  const rows = decodeAuthoringMethodRows(nodeOf({
    drafts: [
      { draftId: DRAFT, serviceCode: "post", optionCode: null, revision: 1, regionGroups: [] },
      { draftId: OTHER_DRAFT, serviceCode: "post", optionCode: null, revision: 3, regionGroups: [] },
    ],
  }));
  const duplicates = duplicateBindingDraftIds(rows);
  assert.equal(duplicates.size, 2);
  assert.ok(duplicates.has(DRAFT) && duplicates.has(OTHER_DRAFT));
});

/* ————————————————— the REAL publication wire ————————————————— */

const view = (overrides: Record<string, unknown> = {}): JsonNode => nodeOf({
  draftId: DRAFT,
  currentDraftRevision: 2,
  serviceCode: "post",
  optionCode: null,
  state: "Absent",
  selectionVersion: 0,
  selectedSnapshotId: null,
  selectedDraftId: null,
  selectedSourceRevision: null,
  selectedFormatVersion: null,
  selectedMatchesCurrentDraftRevision: false,
  ...overrides,
});

test("publication decode accepts the Host STRING enum names and omitted null fields", () => {
  const absent = decodeAuthoringPublication(view({ selectedSnapshotId: undefined }), DRAFT);
  assert.equal(absent.state, "absent");
  assert.equal(absent.selectionVersion, 0);
  assert.equal(absent.selectedSnapshotId, null);

  const selected = decodeAuthoringPublication(view({
    state: "Selected",
    selectionVersion: 4,
    selectedSnapshotId: SNAPSHOT,
    selectedDraftId: DRAFT,
    selectedSourceRevision: 2,
    selectedFormatVersion: 1,
    selectedMatchesCurrentDraftRevision: true,
  }), DRAFT);
  assert.equal(selected.state, "selected");
  assert.equal(selected.selectionVersion, 4);
  assert.equal(selected.selectedSnapshotId, SNAPSHOT);
  assert.equal(selected.selectedFormatVersion, 1);

  const cleared = decodeAuthoringPublication(view({ state: "Cleared", selectionVersion: 5 }), DRAFT);
  assert.equal(cleared.state, "cleared");
  assert.equal(cleared.selectionVersion, 5);
});

test("publication decode refuses numeric enums, contradictory state and any other draft's body", () => {
  assert.throws(() => decodeAuthoringPublication(view({ state: 2, selectionVersion: 4 }), DRAFT), "integer enums are not the wire");
  assert.throws(() => decodeAuthoringPublication(view({ selectionVersion: 4 }), DRAFT), "absent must report version 0");
  assert.throws(() => decodeAuthoringPublication(view({ state: "Cleared", selectionVersion: 0 }), DRAFT), "cleared must keep a real version");
  assert.throws(() => decodeAuthoringPublication(view({ state: "Selected", selectionVersion: 4 }), DRAFT), "selected needs provenance");
  assert.throws(() => decodeAuthoringPublication(view(), OTHER_DRAFT), "a stale/foreign draft response is never adopted");
});

test("publish/withdraw bodies carry ONLY the exact tokens", () => {
  assert.equal(publishRequestBody(12, 3), '{"expectedRevision":12,"expectedSelectionVersion":3}');
  assert.equal(withdrawRequestBody(3), '{"expectedSelectionVersion":3}');
  assert.throws(() => publishRequestBody(9007199254740993, 3));
});

/* ————————————————— the pure state model ————————————————— */

function selectedView(overrides: Partial<AuthoringPublicationView> = {}): AuthoringPublicationView {
  return {
    draftId: DRAFT,
    currentDraftRevision: 2,
    serviceCode: "post",
    optionCode: null,
    state: "selected",
    selectionVersion: 4,
    selectedSnapshotId: SNAPSHOT,
    selectedDraftId: DRAFT,
    selectedSourceRevision: 2,
    selectedFormatVersion: 1,
    selectedMatchesCurrentDraftRevision: true,
    ...overrides,
  };
}

test("a duplicate click cannot reach a second POST (synchronous in-flight gate)", () => {
  const ready = resolveRead(initialPublicationState, { kind: "ok", view: selectedView({ state: "selected", selectedSourceRevision: 1 }) });
  const inFlight = beginWrite(ready, "publish", "view", false, 2);
  assert.ok(inFlight);
  assert.equal(inFlight.pending, "publish");
  assert.equal(beginWrite(inFlight, "publish", "view", false, 2), null);
});

test("publish is view-mode only, needs a known revision, and is refused when already current", () => {
  const absent = resolveRead(initialPublicationState, { kind: "ok", view: selectedView({ state: "absent", selectionVersion: 0, selectedSnapshotId: null, selectedDraftId: null, selectedSourceRevision: null, selectedFormatVersion: null }) });
  assert.equal(publicationBlockedReason(absent, "publish", "edit", false, 2), "publicationViewOnly");
  assert.equal(publicationBlockedReason(absent, "publish", "view", true, 2), "publicationBusy");
  assert.equal(publicationBlockedReason(absent, "publish", "view", false, null), "publicationNoRevision");
  assert.equal(canPublish(absent, "view", false, 2), true);

  const current = resolveRead(initialPublicationState, { kind: "ok", view: selectedView() });
  assert.equal(publicationBlockedReason(current, "publish", "view", false, 2), "publicationAlreadyCurrent");
  assert.equal(canWithdraw(current, "view", false, 2), true);

  assert.equal(publicationBlockedReason(absent, "withdraw", "view", false, 2), "publicationNothingToWithdraw");
});

test("a conflict latches stale and no further write is sent until an explicit read succeeds", () => {
  const ready = resolveRead(initialPublicationState, { kind: "ok", view: selectedView({ selectedSourceRevision: 1 }) });
  const inFlight = beginWrite(ready, "publish", "view", false, 2);
  assert.ok(inFlight);
  const conflicted = resolveWrite(inFlight, {
    kind: "failure",
    failure: { kind: "conflict", status: 409, errorCode: "selected_capture.conflict", title: null, errors: [] },
  });
  assert.equal(conflicted.stale, true);
  assert.equal(conflicted.pending, null);
  assert.equal(beginWrite(conflicted, "publish", "view", false, 2), null, "no automatic retry after a rejection");
  assert.equal(publicationStateMessage(conflicted), "publicationStale");

  const reRead = resolveRead(conflicted, { kind: "ok", view: selectedView({ selectionVersion: 5, selectedSourceRevision: 1 }) });
  assert.equal(reRead.stale, false);
  assert.equal(canPublish(reRead, "view", false, 2), true);
});

test("an unknown outcome blocks every write until the operator reads the status", () => {
  const ready = resolveRead(initialPublicationState, { kind: "ok", view: selectedView({ selectedSourceRevision: 1 }) });
  const inFlight = beginWrite(ready, "withdraw", "view", false, 2);
  assert.ok(inFlight);
  const unknown = resolveWrite(inFlight, { kind: "failure", failure: { kind: "outcomeUnknown", reason: "network" } });
  assert.equal(unknown.unknown, true);
  assert.equal(unknown.pending, null);
  assert.equal(unknown.view?.selectionVersion, 4, "the last real token is kept, never reset to 0");
  assert.equal(publicationBlockedReason(unknown, "withdraw", "view", false, 2), "publicationUnknownBlock");
  assert.equal(publicationStateMessage(unknown), "publicationUnknown");

  assert.equal(beginRead(unknown).read, "loading");
  const reRead = resolveRead(unknown, { kind: "ok", view: selectedView() });
  assert.equal(reRead.unknown, false);
});

test("phase compares the confirmed revision with the selected source revision", () => {
  assert.equal(publicationPhase(selectedView(), 2), "current");
  assert.equal(publicationPhase(selectedView(), 3), "older");
  assert.equal(publicationPhase(selectedView({ selectedSourceRevision: 1, selectedMatchesCurrentDraftRevision: false }), 3), "older");
  assert.equal(publicationPhase(selectedView({ state: "absent", selectionVersion: 0 }), 2), "absent");
  assert.equal(publicationPhase(selectedView({ state: "cleared" }), 2), "cleared");
});

test("a failed read never clears the unknown latch and never invents a token", () => {
  const ready = resolveRead(initialPublicationState, { kind: "ok", view: selectedView() });
  const unknown = resolveWrite({ ...ready, pending: "publish" }, { kind: "failure", failure: { kind: "outcomeUnknown", reason: "aborted" } });
  const failedRead = resolveRead(unknown, {
    kind: "failure",
    failure: { kind: "untrusted", status: 200, reason: "body:unreadable" },
  });
  assert.equal(failedRead.unknown, true);
  assert.equal(failedRead.view, null, "an untrusted read exposes no token at all");
  assert.equal(failedRead.read, "untrusted");
  assert.equal(beginWrite(failedRead, "publish", "view", false, 2), null);
});
