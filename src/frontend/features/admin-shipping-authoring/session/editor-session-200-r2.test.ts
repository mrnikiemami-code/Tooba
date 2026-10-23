import assert from "node:assert/strict";
import test from "node:test";
import {
  createEditorSessionState,
  decodeEditorDocument,
  hasUnsavedWork,
  parseJsonNode,
  reduceEditorSession,
  selectApplyRequest,
} from "../authoring-core.ts";
import type { AreaAncestryResolver, EditorDocument, EditorSessionState } from "../authoring-core.ts";

const DRAFT_A = "11111111-1111-4111-8111-111111111111";
const GROUP = "22222222-2222-4222-8222-222222222222";
const ROW = "33333333-3333-4333-8333-333333333333";
const LANGUAGE_FA = "44444444-4444-4444-8444-444444444444";
const LANGUAGE_EN = "55555555-5555-4555-8555-555555555555";
const COUNTRY = "99999999-9999-4999-8999-999999999999";

function rawNumbers(text: string): string {
  return text.replace(/"@(-?[0-9]+\.[0-9]+)@"/g, "$1");
}

function wire(revision: number): string {
  return rawNumbers(
    JSON.stringify({
      draftId: DRAFT_A,
      owner: { kind: "Store" },
      serviceCode: "tooba.post",
      revision,
      createdAt: "2026-10-23T00:15:00+03:30",
      updatedAt: "2026-10-23T00:15:00+03:30",
      type: { draftId: DRAFT_A, revision, resolution: "Unconfigured" },
      presentation: {
        methodTranslations: [{ languageId: LANGUAGE_FA, name: "پست" }],
        groupTitles: [],
        contentLanguages: [
          { languageId: LANGUAGE_FA, code: "fa", culture: "fa-IR", urlPrefix: "", direction: "rtl", displayName: "فارسی", nativeName: "فارسی", isDefault: true, sortOrder: 1 },
        ],
        defaultContentLanguageId: LANGUAGE_FA,
        hasDefaultLanguageName: true,
      },
      regionGroups: [
        {
          groupId: GROUP,
          order: 0,
          areas: [{ kind: "Country", id: COUNTRY }],
          rows: [
            {
              rateRowId: ROW,
              freightAmount: "@10.00@",
              packagingAmount: "@0.00@",
              minDeliveryDays: 1,
              maxDeliveryDays: 2,
            },
          ],
          titles: [],
        },
      ],
    }),
  );
}

function document(revision: number): EditorDocument {
  const node = parseJsonNode(wire(revision));
  assert.ok(node);
  const decoded = decodeEditorDocument(node, DRAFT_A);
  if (!decoded.ok) throw new Error("decode_failed");
  return decoded.value;
}

const resolver: AreaAncestryResolver = {
  resolve: (area) => (area.kind === "Country" ? { kind: "Country", countryId: COUNTRY } : null),
};

function loaded(revision = 1): EditorSessionState {
  const started = reduceEditorSession(createEditorSessionState(DRAFT_A), { type: "loadStarted", draftId: DRAFT_A });
  return reduceEditorSession(started, {
    type: "loadSucceeded",
    draftId: DRAFT_A,
    epoch: started.epoch,
    document: document(revision),
  });
}

function dirty(): EditorSessionState {
  return reduceEditorSession(loaded(), {
    type: "edited",
    draftId: DRAFT_A,
    edits: { methodTranslations: [{ languageId: LANGUAGE_EN, name: "Post", description: null }] },
  });
}

test("R2-01 same-draft loadStarted while dirty keeps the SAME state, epoch, baseline and edits", () => {
  const state = dirty();
  assert.equal(state.unsaved, true);
  const reload = reduceEditorSession(state, { type: "loadStarted", draftId: DRAFT_A });
  assert.equal(reload, state, "a refresh of the same draft must not silently restart the session");
  assert.equal(reload.epoch, state.epoch);
  assert.equal(reload.load, "loaded");
  assert.deepEqual(reload.edits, state.edits);
  assert.equal(reload.baseline, state.baseline);

  // An unsolicited success for the unchanged epoch cannot clear the operator's work either.
  const unsolicited = reduceEditorSession(state, {
    type: "loadSucceeded",
    draftId: DRAFT_A,
    epoch: state.epoch,
    document: document(9),
  });
  assert.equal(unsolicited.edits.methodTranslations?.length, 1);
  assert.equal(unsolicited.baseline, state.baseline);
});

test("R2-01 same-draft loadStarted during an in-flight save keeps the saving request alive", () => {
  const state = reduceEditorSession(dirty(), { type: "edited", draftId: DRAFT_A, edits: dirty().edits });
  const saving = reduceEditorSession(state, {
    type: "saveStarted",
    draftId: DRAFT_A,
    epoch: state.epoch,
    requestId: 4,
  });
  assert.equal(saving.save, "saving");
  const reload = reduceEditorSession(saving, { type: "loadStarted", draftId: DRAFT_A });
  assert.equal(reload, saving, "a same-draft reload must not invalidate the in-flight save");
  assert.equal(reload.epoch, saving.epoch);
  assert.equal(reload.saveRequestId, 4);

  const accepted = reduceEditorSession(saving, {
    type: "saveSucceeded",
    draftId: DRAFT_A,
    epoch: saving.epoch,
    requestId: 4,
    document: document(2),
  });
  assert.equal(accepted.save, "clean");
  assert.equal(accepted.baseline?.revision, 2);
});

test("R2-01 explicit discard then fresh load, and unknown reconciliation, still work", () => {
  const asked = reduceEditorSession(dirty(), { type: "closeRequested", draftId: DRAFT_A });
  const discarded = reduceEditorSession(asked, { type: "discardConfirmed", draftId: DRAFT_A });
  assert.equal(discarded.load, "idle");
  assert.equal(discarded.unsaved, false);
  const fresh = reduceEditorSession(discarded, { type: "loadStarted", draftId: DRAFT_A });
  assert.equal(fresh.load, "loading");
  const reloaded = reduceEditorSession(fresh, {
    type: "loadSucceeded",
    draftId: DRAFT_A,
    epoch: fresh.epoch,
    document: document(3),
  });
  assert.equal(reloaded.baseline?.revision, 3);

  const state = dirty();
  const saving = reduceEditorSession(state, { type: "saveStarted", draftId: DRAFT_A, epoch: state.epoch, requestId: 1 });
  const unknown = reduceEditorSession(saving, {
    type: "saveFailed",
    draftId: DRAFT_A,
    epoch: saving.epoch,
    requestId: 1,
    failure: { kind: "outcomeUnknown", reason: "network" },
  });
  const reconciling = reduceEditorSession(unknown, { type: "reconcileStarted", draftId: DRAFT_A, epoch: unknown.epoch + 1 });
  assert.equal(reconciling.reconcile, "pending");
  const reconciled = reduceEditorSession(reconciling, {
    type: "reconcileSucceeded",
    draftId: DRAFT_A,
    epoch: reconciling.epoch,
    document: document(4),
  });
  assert.equal(reconciled.commitUncertain, false);
  assert.equal(reconciled.baseline?.revision, 4);
});

test("R2-02 hasUnsavedWork protects latched uncertainty, in-flight save and ordinary dirt", () => {
  assert.equal(hasUnsavedWork(loaded()), false, "a clean loaded session has nothing at risk");

  const state = dirty();
  const saving = reduceEditorSession(state, { type: "saveStarted", draftId: DRAFT_A, epoch: state.epoch, requestId: 1 });
  assert.equal(hasUnsavedWork(saving), true, "an in-flight save is work at risk");

  const unknown = reduceEditorSession(saving, {
    type: "saveFailed",
    draftId: DRAFT_A,
    epoch: saving.epoch,
    requestId: 1,
    failure: { kind: "outcomeUnknown", reason: "unreadable" },
  });
  assert.equal(hasUnsavedWork(unknown), true);

  // Uncertainty must not disappear merely because the editable values are empty.
  const emptied = reduceEditorSession(unknown, { type: "edited", draftId: DRAFT_A, edits: {} });
  assert.equal(emptied.unsaved, false);
  assert.equal(emptied.commitUncertain, true);
  assert.equal(hasUnsavedWork(emptied), true, "unknown commit is still unresolved work");
  const apply = selectApplyRequest(emptied, 2, resolver);
  assert.equal(apply.status, "blocked");
  if (apply.status === "blocked") assert.equal(apply.reason, "uncertain");
  const asked = reduceEditorSession(emptied, { type: "closeRequested", draftId: DRAFT_A });
  assert.equal(asked.decision, "discardUnsaved");

  const discarded = reduceEditorSession(asked, { type: "discardConfirmed", draftId: DRAFT_A });
  assert.equal(hasUnsavedWork(discarded), true, "an unresolved commit survives discard until a fresh read");
});

