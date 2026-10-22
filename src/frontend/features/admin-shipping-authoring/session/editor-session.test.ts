import assert from "node:assert/strict";
import test from "node:test";
import {
  createEditorSessionState,
  decodeEditorDocument,
  parseJsonNode,
  reduceEditorSession,
  selectApplyRequest,
} from "../index.ts";
import type { AreaAncestryResolver, EditorDocument, EditorEdits, EditorSessionState } from "../index.ts";

const DRAFT_A = "11111111-1111-4111-8111-111111111111";
const DRAFT_B = "99999999-9999-4999-8999-999999999999";
const GROUP = "22222222-2222-4222-8222-222222222222";
const ROW = "33333333-3333-4333-8333-333333333333";
const LANGUAGE_FA = "44444444-4444-4444-8444-444444444444";
const LANGUAGE_EN = "55555555-5555-4555-8555-555555555555";
const LANGUAGE_AR = "66666666-6666-4666-8666-666666666666";
const COUNTRY = "99999999-9999-4999-8999-999999999999";
const CITY = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

/** Emits the exact number tokens the server writes for `decimal` values. */
function rawNumbers(text: string): string {
  return text.replace(/"@(-?[0-9]+\.[0-9]+)@"/g, "$1");
}

function wire(draftId: string, revision: number, groupId: string, rowId: string): string {
  return rawNumbers(JSON.stringify({
    draftId,
    owner: { kind: "Store" },
    serviceCode: "tooba.post",
    revision,
    createdAt: "2026-10-23T00:15:00+03:30",
    updatedAt: "2026-10-23T00:15:00+03:30",
    type: { draftId, revision, resolution: "Unconfigured" },
    presentation: {
      methodTranslations: [{ languageId: LANGUAGE_FA, name: "پست" }],
      groupTitles: [{ languageId: LANGUAGE_AR, title: "عربي" }],
      contentLanguages: [
        { languageId: LANGUAGE_FA, code: "fa", culture: "fa-IR", urlPrefix: "", direction: "rtl", displayName: "فارسی", nativeName: "فارسی", isDefault: true, sortOrder: 1 },
        { languageId: LANGUAGE_EN, code: "en", culture: "en-US", urlPrefix: "en", direction: "ltr", displayName: "English", nativeName: "English", isDefault: false, sortOrder: 2 },
      ],
      defaultContentLanguageId: LANGUAGE_FA,
      hasDefaultLanguageName: true,
    },
    regionGroups: [
      {
        groupId,
        order: 0,
        areas: [{ kind: "Country", id: COUNTRY }],
        rows: [
          {
            rateRowId: rowId,
            freightAmount: "@10.00@",
            packagingAmount: "@0.00@",
            minDeliveryDays: 1,
            maxDeliveryDays: 2,
          },
        ],
        titles: [],
      },
    ],
  }));
}

function document(draftId: string, revision: number, groupId = GROUP, rowId = ROW): EditorDocument {
  const node = parseJsonNode(wire(draftId, revision, groupId, rowId));
  assert.ok(node);
  const decoded = decodeEditorDocument(node, draftId);
  if (!decoded.ok) throw new Error(decoded.reason);
  return decoded.value;
}

const resolver: AreaAncestryResolver = {
  resolve: (area) => (area.kind === "Country" ? { kind: "Country", countryId: COUNTRY } : null),
};

function loaded(draftId = DRAFT_A, revision = 1): EditorSessionState {
  const started = reduceEditorSession(createEditorSessionState(draftId), { type: "loadStarted", draftId });
  return reduceEditorSession(started, {
    type: "loadSucceeded",
    draftId,
    generation: started.generation,
    document: document(draftId, revision),
  });
}

function edit(): EditorEdits {
  return {
    regionGroups: [
      {
        groupKey: "local-1",
        groupId: GROUP,
        areas: [{ kind: "Country", id: COUNTRY }],
        rows: [
          {
            rowKey: "local-row-1",
            rateRowId: ROW,
            minWeightGrams: null,
            maxWeightGrams: null,
            minItemCount: null,
            maxItemCount: null,
            minMerchandiseAmount: null,
            maxMerchandiseAmount: null,
            freightAmount: "12.50",
            packagingAmount: "0.00",
            minDeliveryDays: 1,
            maxDeliveryDays: 2,
          },
        ],
        titles: [],
      },
    ],
  };
}

test("load states stay honest: empty values load, denied/notfound/error stay distinct", () => {
  const emptyWire = JSON.stringify({
    draftId: DRAFT_A,
    owner: { kind: "Store" },
    serviceCode: "tooba.post",
    revision: 1,
    createdAt: "2026-10-23T00:15:00+03:30",
    updatedAt: "2026-10-23T00:15:00+03:30",
    type: { draftId: DRAFT_A, revision: 1, resolution: "Unconfigured" },
    presentation: { methodTranslations: [], groupTitles: [], contentLanguages: [], defaultContentLanguageId: "00000000-0000-4000-8000-000000000000", hasDefaultLanguageName: false },
    regionGroups: [],
  });
  const node = parseJsonNode(emptyWire);
  assert.ok(node);
  // An empty language registry cannot resolve the default language, so the whole payload is untrusted.
  assert.equal(decodeEditorDocument(node, DRAFT_A).ok, false);

  for (const [failure, expected] of [
    [{ kind: "denied", status: 403, errorCode: null, title: null } as const, "denied"],
    [{ kind: "notFound", status: 404, errorCode: null, title: null } as const, "notfound"],
    [{ kind: "unavailable", status: 503, errorCode: null, title: null } as const, "error"],
    [{ kind: "network", aborted: false } as const, "error"],
  ] as const) {
    const started = reduceEditorSession(createEditorSessionState(DRAFT_A), { type: "loadStarted", draftId: DRAFT_A });
    const failed = reduceEditorSession(started, {
      type: "loadFailed",
      draftId: DRAFT_A,
      generation: started.generation,
      failure,
    });
    assert.equal(failed.load, expected);
    assert.equal(failed.baseline, null);
  }
});

test("conflict keeps local edits and a successful save adopts real server ids and revision", () => {
  const state = reduceEditorSession(loaded(), { type: "edited", draftId: DRAFT_A, edits: edit() });
  assert.equal(state.unsaved, true);
  const started = reduceEditorSession(state, { type: "saveStarted", draftId: DRAFT_A, requestId: 1 });
  assert.equal(started.save, "saving");
  const conflicted = reduceEditorSession(started, {
    type: "saveFailed",
    draftId: DRAFT_A,
    requestId: 1,
    failure: { kind: "conflict", status: 409, errorCode: "shipping.authoring.revision_conflict", title: null, errors: [] },
  });
  assert.equal(conflicted.save, "conflict");
  assert.equal(conflicted.unsaved, true, "conflict must not drop the operator's edits");
  assert.deepEqual(conflicted.edits, edit());

  const retried = reduceEditorSession(conflicted, { type: "saveStarted", draftId: DRAFT_A, requestId: 2 });
  const savedWire = document(DRAFT_A, 2, "cccccccc-cccc-4ccc-8ccc-cccccccccccc", "dddddddd-dddd-4ddd-8ddd-dddddddddddd");
  const saved = reduceEditorSession(retried, { type: "saveSucceeded", draftId: DRAFT_A, requestId: 2, document: savedWire });
  assert.equal(saved.unsaved, false);
  assert.equal(saved.save, "clean");
  assert.equal(saved.baseline?.revision, 2);
  assert.equal(saved.baseline?.regionGroups[0]?.groupId, "cccccccc-cccc-4ccc-8ccc-cccccccccccc");
  assert.equal(saved.baseline?.regionGroups[0]?.rows[0]?.rateRowId, "dddddddd-dddd-4ddd-8ddd-dddddddddddd");
  // A no-op success may keep the same revision and is still accepted.
  const noop = reduceEditorSession(loaded(DRAFT_A, 5), { type: "edited", draftId: DRAFT_A, edits: edit() });
  const noopStarted = reduceEditorSession(noop, { type: "saveStarted", draftId: DRAFT_A, requestId: 9 });
  const noopSaved = reduceEditorSession(noopStarted, {
    type: "saveSucceeded",
    draftId: DRAFT_A,
    requestId: 9,
    document: document(DRAFT_A, 5),
  });
  assert.equal(noopSaved.baseline?.revision, 5);
});

test("outcomeUnknown and validation failures preserve work and never fake a baseline", () => {
  const state = reduceEditorSession(loaded(), { type: "edited", draftId: DRAFT_A, edits: edit() });
  const started = reduceEditorSession(state, { type: "saveStarted", draftId: DRAFT_A, requestId: 3 });
  const unknown = reduceEditorSession(started, {
    type: "saveFailed",
    draftId: DRAFT_A,
    requestId: 3,
    failure: { kind: "outcomeUnknown", reason: "network" },
  });
  assert.equal(unknown.save, "outcomeUnknown");
  assert.equal(unknown.unsaved, true);
  assert.equal(unknown.baseline?.revision, 1);

  const blocked = selectApplyRequest(unknown, 4, resolver);
  assert.equal(blocked.status, "ready", "unknown outcome is retried only by an explicit decision, not automatically");

  const invalid = reduceEditorSession(
    reduceEditorSession(loaded(), { type: "edited", draftId: DRAFT_A, edits: edit() }),
    { type: "saveStarted", draftId: DRAFT_A, requestId: 5 },
  );
  const failed = reduceEditorSession(invalid, {
    type: "saveFailed",
    draftId: DRAFT_A,
    requestId: 5,
    failure: {
      kind: "validation",
      status: 422,
      errorCode: "shipping.authoring.region_payload_invalid",
      title: null,
      errors: [{ property: "regionGroups[0].rows[0].freightAmount", codes: ["invalid"] }],
    },
  });
  assert.equal(failed.save, "validationFailed");
  assert.equal(failed.unsaved, true);
});

test("duplicate Apply is gated and late results from another generation never overwrite", () => {
  const dirty = reduceEditorSession(loaded(), { type: "edited", draftId: DRAFT_A, edits: edit() });
  const first = reduceEditorSession(dirty, { type: "saveStarted", draftId: DRAFT_A, requestId: 1 });
  const second = reduceEditorSession(first, { type: "saveStarted", draftId: DRAFT_A, requestId: 2 });
  assert.equal(second, first, "second Apply in flight is ignored, not queued");
  assert.equal(selectApplyRequest(first, 2, resolver).status, "blocked");

  const lateLoadA = { type: "loadSucceeded", draftId: DRAFT_A, generation: 1, document: document(DRAFT_A, 9) } as const;
  const afterLate = reduceEditorSession(first, lateLoadA);
  assert.equal(afterLate, first, "a stale load result is discarded");

  // Session B cannot be overwritten by A's late load/save.
  const switchAway = reduceEditorSession(first, { type: "switchRequested", draftId: DRAFT_A, nextDraftId: DRAFT_B });
  assert.equal(switchAway.decision, "discardUnsaved");
  assert.equal(switchAway.draftId, DRAFT_A, "switching must wait for the explicit decision");
  const confirmed = reduceEditorSession(switchAway, { type: "discardConfirmed", draftId: DRAFT_A });
  assert.equal(confirmed.draftId, DRAFT_B);
  assert.equal(confirmed.unsaved, false);
  assert.equal(confirmed.baseline, null);
  const startedB = reduceEditorSession(confirmed, { type: "loadStarted", draftId: DRAFT_B });
  const loadedB = reduceEditorSession(startedB, {
    type: "loadSucceeded",
    draftId: DRAFT_B,
    generation: startedB.generation,
    document: document(DRAFT_B, 4),
  });
  assert.equal(loadedB.baseline?.draftId, DRAFT_B);
  const lateA = reduceEditorSession(loadedB, {
    type: "saveSucceeded",
    draftId: DRAFT_A,
    requestId: 1,
    document: document(DRAFT_A, 8),
  });
  assert.equal(lateA, loadedB);
  const lateBGen = reduceEditorSession(loadedB, {
    type: "loadSucceeded",
    draftId: DRAFT_B,
    generation: 0,
    document: document(DRAFT_B, 77),
  });
  assert.equal(lateBGen, loadedB);
});

test("dirty close asks for a decision and cancel keeps the work", () => {
  const dirty = reduceEditorSession(loaded(), { type: "edited", draftId: DRAFT_A, edits: edit() });
  const asked = reduceEditorSession(dirty, { type: "closeRequested", draftId: DRAFT_A });
  assert.equal(asked.decision, "discardUnsaved");
  assert.equal(asked.unsaved, true);
  const cancelled = reduceEditorSession(asked, { type: "discardCancelled", draftId: DRAFT_A });
  assert.equal(cancelled.decision, "none");
  assert.equal(cancelled.unsaved, true);
  const discarded = reduceEditorSession(cancelled, { type: "discardConfirmed", draftId: DRAFT_A });
  assert.equal(discarded.unsaved, false);
  assert.equal(discarded.baseline?.revision, 1, "discard returns to the confirmed server baseline");
  const cleanClose = reduceEditorSession(discarded, { type: "closeRequested", draftId: DRAFT_A });
  assert.equal(cleanClose.decision, "none");
});

test("only intentionally touched active languages are patched and the interface locale is not authority", () => {
  const state = reduceEditorSession(loaded(), {
    type: "edited",
    draftId: DRAFT_A,
    edits: {
      methodTranslations: [{ languageId: LANGUAGE_EN, name: "Post", description: null }],
      regionGroups: [
        {
          groupKey: "local-1",
          groupId: GROUP,
          areas: [{ kind: "Country", id: COUNTRY }],
          rows: [],
          titles: [{ languageId: LANGUAGE_EN, title: "Tehran" }],
        },
      ],
    },
  });
  const request = selectApplyRequest(state, 7, resolver);
  assert.equal(request.status, "ready");
  if (request.status !== "ready" || request.body.status !== "ready") return;
  const body = JSON.parse(request.body.text) as Record<string, unknown>;
  // Untouched Persian baseline text is never echoed as a writable patch.
  assert.deepEqual(body.methodTranslations, [{ languageId: LANGUAGE_EN, name: "Post", description: null }]);
  const groups = body.regionGroups as { titles: unknown[] }[];
  assert.deepEqual(groups[0]?.titles, [{ languageId: LANGUAGE_EN, title: "Tehran" }]);
  assert.equal(body.expectedRevision, 1);
  assert.doesNotMatch(request.body.text, /locale|currency|IRR|fa-IR/i);
  // A second identical build is byte-identical: no locale/culture input reaches the body.
  const again = selectApplyRequest(state, 8, resolver);
  assert.equal(again.status, "ready");
  if (again.status === "ready" && again.body.status === "ready") {
    assert.equal(again.body.text, request.body.text);
  }
  // Unresolved geography blocks the request with an explicit reason instead of dropping the area.
  const unresolved = reduceEditorSession(loaded(), { type: "edited", draftId: DRAFT_A, edits: edit() });
  const cityState = reduceEditorSession(unresolved, {
    type: "edited",
    draftId: DRAFT_A,
    edits: {
      regionGroups: [
        {
          groupKey: "local-1",
          groupId: GROUP,
          areas: [{ kind: "City", id: CITY }],
          rows: [],
          titles: [],
        },
      ],
    },
  });
  const blocked = selectApplyRequest(cityState, 9, resolver);
  assert.equal(blocked.status, "blocked");
  if (blocked.status === "blocked") assert.equal(blocked.reason, "needs-resolution");
});
