import assert from "node:assert/strict";
import test from "node:test";
import {
  createEditorSessionState,
  decodeEditorDocument,
  parseJsonNode,
  reduceEditorSession,
  selectApplyRequest,
} from "./authoring-core.ts";
import type { AreaAncestryResolver, EditorDocument, EditorEdits, EditorSessionState } from "./authoring-core.ts";

const DRAFT_A = "11111111-1111-4111-8111-111111111111";
const DRAFT_B = "99999999-9999-4999-8999-999999999999";
const GROUP_1 = "22222222-2222-4222-8222-222222222222";
const GROUP_2 = "22222222-2222-4222-8222-222222222223";
const ROW_1 = "33333333-3333-4333-8333-333333333333";
const ROW_2 = "33333333-3333-4333-8333-333333333334";
const FA = "44444444-4444-4444-8444-444444444444";
const EN = "55555555-5555-4555-8555-555555555555";
const INACTIVE = "66666666-6666-4666-8666-666666666666";
const TYPE = "88888888-8888-4888-8888-888888888888";
const COUNTRY = "99999999-9999-4999-8999-999999999991";
const CITY = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const ADMIN_AREA = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const ZERO = "00000000-0000-0000-0000-000000000000";

function rawNumbers(text: string): string {
  return text.replace(/"@(-?[0-9]+\.[0-9]+)@"/g, "$1");
}

function wire(draftId = DRAFT_A, revision = 4, secondArea: "City" | "Country" = "City"): string {
  return rawNumbers(
    JSON.stringify({
      draftId,
      owner: { ownerKind: "Store" },
      serviceCode: "tooba.post",
      revision,
      createdAt: "2026-10-23T00:15:00+03:30",
      updatedAt: "2026-10-23T01:45:10+03:30",
      type: { draftId, revision, resolution: "Unconfigured" },
      presentation: {
        methodTranslations: [
          { languageId: FA, name: "پست" },
          { languageId: INACTIVE, name: "inactive stored name" },
        ],
        groupTitles: [{ languageId: INACTIVE, title: "inactive stored title" }],
        contentLanguages: [
          { languageId: FA, code: "fa", culture: "fa-IR", urlPrefix: "", direction: "rtl", displayName: "فارسی", nativeName: "فارسی", isDefault: true, sortOrder: 1 },
          { languageId: EN, code: "en", culture: "en-US", urlPrefix: "en", direction: "ltr", displayName: "English", nativeName: "English", isDefault: false, sortOrder: 2 },
        ],
        defaultContentLanguageId: FA,
        hasDefaultLanguageName: true,
      },
      weeklySchedule: {
        timeZoneId: "Asia/Tehran",
        days: [1, 2, 3, 4, 5, 6, 7].map((weekday) => ({
          weekday,
          isWorking: weekday <= 5,
          startTime: "09:30:00",
          endTime: "18:00:00",
        })),
      },
      regionGroups: [
        {
          groupId: GROUP_1,
          order: 0,
          areas: [{ kind: "Country", id: COUNTRY }],
          rows: [
            {
              rateRowId: ROW_1,
              minWeightGrams: 0,
              maxWeightGrams: 5000,
              minMerchandiseAmount: "@0.50@",
              freightAmount: "@1850000.00@",
              packagingAmount: "@0.00@",
              minDeliveryDays: 1,
              maxDeliveryDays: 3,
            },
          ],
          titles: [{ languageId: FA, title: "تهران" }],
        },
        {
          groupId: GROUP_2,
          order: 1,
          areas: [{ kind: secondArea, id: secondArea === "City" ? CITY : COUNTRY }],
          rows: [
            {
              rateRowId: ROW_2,
              minItemCount: 1,
              freightAmount: "@12.50@",
              packagingAmount: "@0.00@",
              minDeliveryDays: 2,
              maxDeliveryDays: 5,
            },
          ],
          titles: [],
        },
      ],
    }),
  );
}

function parse(text: string) {
  const node = parseJsonNode(text);
  assert.ok(node, "fixture must parse");
  return node;
}

function doc(draftId = DRAFT_A, revision = 4, secondArea: "City" | "Country" = "City"): EditorDocument {
  const decoded = decodeEditorDocument(parse(wire(draftId, revision, secondArea)), draftId);
  if (!decoded.ok) throw new Error('decode_failed');
  return decoded.value;
}

const fullResolver: AreaAncestryResolver = {
  resolve: (area) =>
    area.kind === "Country"
      ? { kind: "Country", countryId: COUNTRY }
      : { kind: "City", countryId: COUNTRY, administrativeAreaId: ADMIN_AREA, cityId: CITY },
};

/** Resolver that cannot supply city ancestry (used to prove retained selections block a write). */
const noCityResolver: AreaAncestryResolver = {
  resolve: (area) => (area.kind === "Country" ? { kind: "Country", countryId: COUNTRY } : null),
};

/** Resolver that answers with the same kind but a DIFFERENT terminal id. */
const hostileResolver: AreaAncestryResolver = {
  resolve: (area) => ({ kind: area.kind, countryId: CITY }),
};

function loaded(draftId = DRAFT_A, revision = 4, secondArea: "City" | "Country" = "City"): EditorSessionState {
  const started = reduceEditorSession(createEditorSessionState(draftId), { type: "loadStarted", draftId });
  return reduceEditorSession(started, {
    type: "loadSucceeded",
    draftId,
    epoch: started.epoch,
    document: doc(draftId, revision, secondArea),
  });
}

function bodyOf(state: EditorSessionState, resolver = fullResolver): Record<string, unknown> {
  return JSON.parse(textOf(state, resolver)) as Record<string, unknown>;
}

function textOf(state: EditorSessionState, resolver = fullResolver): string {
  const request = selectApplyRequest(state, 1, resolver);
  assert.equal(request.status, "ready", `expected ready, got ${JSON.stringify(request)}`);
  if (request.status !== "ready" || request.body.status !== "ready") throw new Error("body not ready");
  return request.body.text;
}

function methodOnly(state: EditorSessionState): EditorSessionState {
  return reduceEditorSession(state, {
    type: "edited",
    draftId: state.draftId,
    edits: { methodTranslations: [{ languageId: EN, name: "Post", description: null }] },
  });
}

test("R200-01 omitted region groups keep EVERY confirmed baseline group, row, id and amount", () => {
  const body = bodyOf(methodOnly(loaded()));
  const groups = body.regionGroups as {
    groupId: string;
    areas: { kind: string; id: string }[];
    rows: { rateRowId: string; freightAmount: number; minMerchandiseAmount?: number }[];
    titles: unknown[];
  }[];
  assert.equal(groups.length, 2, "method-only edit must not delete persisted groups");
  assert.deepEqual(groups.map((group) => group.groupId), [GROUP_1, GROUP_2]);
  assert.deepEqual(groups.map((group) => group.rows.map((row) => row.rateRowId)), [[ROW_1], [ROW_2]]);
  assert.deepEqual(
    groups[0]?.areas,
    [{ kind: "Country", countryId: COUNTRY, administrativeAreaId: null, cityId: null, serviceAreaId: null }],
  );
  assert.deepEqual(
    groups[1]?.areas,
    [{ kind: "City", countryId: COUNTRY, administrativeAreaId: ADMIN_AREA, cityId: CITY, serviceAreaId: null }],
  );
  // Untouched titles are NOT echoed as writes; untouched inactive text is never re-sent.
  assert.deepEqual(groups.map((group) => group.titles), [[], []]);
  assert.deepEqual(body.methodTranslations, [{ languageId: EN, name: "Post", description: null }]);
  // Exact decimal text preserved through retention.
  const text = textOf(methodOnly(loaded()));
  assert.match(text, /"freightAmount":1850000\.00/);
  assert.match(text, /"minMerchandiseAmount":0\.50/);
  assert.match(text, /"freightAmount":12\.50/);
});

test("R200-01 settings/type-only edits also retain the whole baseline, and explicit [] is the only removal", () => {
  const settingsBody = bodyOf(
    reduceEditorSession(loaded(), {
      type: "edited",
      draftId: DRAFT_A,
      edits: { methodSettings: { freightPaymentMode: "Prepaid", calculationMode: "Manual" } },
    }),
  );
  assert.equal((settingsBody.regionGroups as unknown[]).length, 2);
  assert.deepEqual(settingsBody.methodTranslations, []);

  const typedBody = bodyOf(
    reduceEditorSession(loaded(), {
      type: "edited",
      draftId: DRAFT_A,
      edits: { typeAssignment: { shippingTypeId: null } },
    }),
  );
  assert.equal((typedBody.regionGroups as unknown[]).length, 2);
  assert.deepEqual(typedBody.typeAssignment, { shippingTypeId: null });

  const cleared = bodyOf(
    reduceEditorSession(loaded(), { type: "edited", draftId: DRAFT_A, edits: { regionGroups: [] } }),
  );
  assert.deepEqual(cleared.regionGroups, [], "explicit [] remains an intentional whole-collection removal");
});

test("R200-01 retained selections still need authoritative ancestry and never silently vanish", () => {
  const request = selectApplyRequest(methodOnly(loaded()), 1, noCityResolver);
  assert.equal(request.status, "blocked");
  if (request.status !== "blocked") return;
  assert.equal(request.reason, "needs-resolution");
});

test("R200-01 inactive/unknown language intent is rejected, not filtered into success", () => {
  const inactiveIntent = reduceEditorSession(loaded(), {
    type: "edited",
    draftId: DRAFT_A,
    edits: { methodTranslations: [{ languageId: INACTIVE, name: "rewrite", description: null }] },
  });
  const request = selectApplyRequest(inactiveIntent, 1, fullResolver);
  assert.equal(request.status, "blocked");
  if (request.status === "blocked") assert.equal(request.reason, "invalid-intent");

  const titleIntent = reduceEditorSession(loaded(), {
    type: "edited",
    draftId: DRAFT_A,
    edits: {
      regionGroups: [
        { groupKey: "g", groupId: GROUP_1, areas: [{ kind: "Country", id: COUNTRY }], rows: [], titles: [{ languageId: INACTIVE, title: "x" }] },
      ],
    },
  });
  const titleRequest = selectApplyRequest(titleIntent, 1, fullResolver);
  assert.equal(titleRequest.status, "blocked");
  if (titleRequest.status === "blocked") assert.equal(titleRequest.reason, "invalid-intent");
  // The confirmed baseline is untouched by a rejected intent.
  assert.equal(titleIntent.baseline?.presentation.methodTranslations.length, 2);
});

test("R200-04 a resolver cannot substitute another terminal id or an incoherent ancestor shape", () => {
  const state = loaded();
  const edit: EditorEdits = {
    methodSettings: { freightPaymentMode: "Prepaid", calculationMode: "Manual" },
  };
  const hostile = selectApplyRequest(reduceEditorSession(state, { type: "edited", draftId: DRAFT_A, edits: edit }), 1, hostileResolver);
  assert.equal(hostile.status, "blocked");
  if (hostile.status === "blocked") assert.equal(hostile.reason, "invalid-intent");

  const extraAncestor: AreaAncestryResolver = {
    resolve: (area) =>
      area.kind === "Country"
        ? { kind: "Country", countryId: COUNTRY, cityId: CITY }
        : { kind: "City", countryId: COUNTRY, administrativeAreaId: ADMIN_AREA, cityId: CITY },
  };
  const shape = selectApplyRequest(
    reduceEditorSession(state, { type: "edited", draftId: DRAFT_A, edits: edit }),
    1,
    extraAncestor,
  );
  assert.equal(shape.status, "blocked");
  if (shape.status === "blocked") assert.equal(shape.reason, "invalid-intent");

  const wrongKind: AreaAncestryResolver = {
    resolve: () => ({ kind: "ServiceArea", countryId: COUNTRY, administrativeAreaId: ADMIN_AREA, cityId: CITY, serviceAreaId: CITY }),
  };
  const mismatched = selectApplyRequest(
    reduceEditorSession(state, { type: "edited", draftId: DRAFT_A, edits: edit }),
    1,
    wrongKind,
  );
  // retained destinations resolve to ServiceArea: rejected, never rewritten.
  assert.equal(mismatched.status, "blocked");
  if (mismatched.status === "blocked") assert.equal(mismatched.reason, "invalid-intent");
});

test("R200-02 a never-reused epoch rejects old A results after A->B->A", () => {
  const openedA = reduceEditorSession(createEditorSessionState(DRAFT_A), { type: "loadStarted", draftId: DRAFT_A });
  assert.ok(openedA.epoch > 0);
  const dirtyA = reduceEditorSession(openedA, { type: "edited", draftId: DRAFT_A, edits: {} });
  const switchAsked = reduceEditorSession(dirtyA, { type: "switchRequested", draftId: DRAFT_A, nextDraftId: DRAFT_B });
  const toB = reduceEditorSession(switchAsked, { type: "discardConfirmed", draftId: DRAFT_A });
  assert.equal(toB.draftId, DRAFT_B);
  assert.ok(toB.epoch > openedA.epoch, "epoch must advance across switch");
  const openB = reduceEditorSession(toB, { type: "loadStarted", draftId: DRAFT_B });
  const loadedB = reduceEditorSession(openB, {
    type: "loadSucceeded",
    draftId: DRAFT_B,
    epoch: openB.epoch,
    document: doc(DRAFT_B, 2),
  });
  const backToA = reduceEditorSession(loadedB, { type: "switchRequested", draftId: DRAFT_B, nextDraftId: DRAFT_A });
  assert.equal(backToA.decision, "none", "a clean session switches without a discard prompt");
  const openA2 = reduceEditorSession(
    reduceEditorSession(backToA, { type: "discardConfirmed", draftId: DRAFT_B }),
    { type: "loadStarted", draftId: DRAFT_A },
  );
  assert.ok(openA2.epoch > openedA.epoch, "reopening A must not reuse the old epoch");

  const staleSuccess = reduceEditorSession(openA2, {
    type: "loadSucceeded",
    draftId: DRAFT_A,
    epoch: openedA.epoch,
    document: doc(DRAFT_A, 9),
  });
  assert.equal(staleSuccess, openA2, "old A load success must be ignored");
  const staleFailure = reduceEditorSession(openA2, {
    type: "loadFailed",
    draftId: DRAFT_A,
    epoch: openedA.epoch,
    failure: { kind: "denied", status: 403, errorCode: null, title: null },
  });
  assert.equal(staleFailure, openA2);

  const currentA = reduceEditorSession(openA2, {
    type: "loadSucceeded",
    draftId: DRAFT_A,
    epoch: openA2.epoch,
    document: doc(DRAFT_A, 5),
  });
  assert.equal(currentA.baseline?.revision, 5);
});

test("R200-02 late old save cannot clear new edits even with a reused numeric request id", () => {
  const state = loaded();
  const dirty = reduceEditorSession(state, { type: "edited", draftId: DRAFT_A, edits: { regionGroups: [] } });
  const inFlight = reduceEditorSession(dirty, { type: "saveStarted", draftId: DRAFT_A, epoch: dirty.epoch, requestId: 1 });
  assert.equal(inFlight.save, "saving");
  const discardAsked = reduceEditorSession(inFlight, { type: "closeRequested", draftId: DRAFT_A });
  const discarded = reduceEditorSession(discardAsked, { type: "discardConfirmed", draftId: DRAFT_A });
  assert.ok(discarded.epoch > inFlight.epoch);
  const reloaded = reduceEditorSession(
    reduceEditorSession(discarded, { type: "loadStarted", draftId: DRAFT_A }),
    { type: "loadSucceeded", draftId: DRAFT_A, epoch: discarded.epoch + 1, document: doc(DRAFT_A, 6) },
  );
  const newDirty = reduceEditorSession(reloaded, {
    type: "edited",
    draftId: DRAFT_A,
    edits: { methodTranslations: [{ languageId: EN, name: "New", description: null }] },
  });
  const oldSave = reduceEditorSession(newDirty, {
    type: "saveSucceeded",
    draftId: DRAFT_A,
    epoch: inFlight.epoch,
    requestId: 1,
    document: doc(DRAFT_A, 7),
  });
  assert.equal(oldSave, newDirty, "old save completion must not touch the new session");
  const sameEpochStaleSave = reduceEditorSession(newDirty, {
    type: "saveSucceeded",
    draftId: DRAFT_A,
    epoch: newDirty.epoch,
    requestId: 99,
    document: doc(DRAFT_A, 7),
  });
  assert.equal(sameEpochStaleSave, newDirty, "request id still gates within an epoch");
});

test("R200-03 unknown commit stays latched until explicit reconciliation or discard+fresh read", () => {
  const dirty = methodOnly(loaded());
  const saving = reduceEditorSession(dirty, { type: "saveStarted", draftId: DRAFT_A, epoch: dirty.epoch, requestId: 1 });
  const unknown = reduceEditorSession(saving, {
    type: "saveFailed",
    draftId: DRAFT_A,
    epoch: saving.epoch,
    requestId: 1,
    failure: { kind: "outcomeUnknown", reason: "network" },
  });
  assert.equal(unknown.save, "outcomeUnknown");
  assert.equal(unknown.unsaved, true);

  const editedAfter = reduceEditorSession(unknown, {
    type: "edited",
    draftId: DRAFT_A,
    edits: { methodTranslations: [{ languageId: EN, name: "Again", description: null }] },
  });
  assert.equal(editedAfter.save, "outcomeUnknown", "a local edit must not clear uncertainty");
  const blocked = selectApplyRequest(editedAfter, 2, fullResolver);
  assert.equal(blocked.status, "blocked");
  if (blocked.status === "blocked") assert.equal(blocked.reason, "uncertain");

  const replay = reduceEditorSession(editedAfter, { type: "saveStarted", draftId: DRAFT_A, epoch: editedAfter.epoch, requestId: 2 });
  assert.equal(replay.save, "outcomeUnknown", "no second PUT while the previous commit is unknown");
  assert.equal(replay.saveRequestId, 0);

  // Stale reconciliation result is ignored.
  const reconciling = reduceEditorSession(replay, { type: "reconcileStarted", draftId: DRAFT_A, epoch: replay.epoch + 1 });
  assert.equal(reconciling.save, "outcomeUnknown");
  const staleReconcile = reduceEditorSession(reconciling, {
    type: "reconcileSucceeded",
    draftId: DRAFT_A,
    epoch: replay.epoch,
    document: doc(DRAFT_A, 10),
  });
  assert.equal(staleReconcile, reconciling);

  const reconciled = reduceEditorSession(reconciling, {
    type: "reconcileSucceeded",
    draftId: DRAFT_A,
    epoch: reconciling.epoch,
    document: doc(DRAFT_A, 11),
  });
  assert.equal(reconciled.save, "clean");
  assert.equal(reconciled.baseline?.revision, 11);
  assert.equal(reconciled.unsaved, true, "edits are retained, not silently dropped");
  const allowed = selectApplyRequest(reconciled, 3, fullResolver);
  assert.equal(allowed.status, "ready");
  if (allowed.status === "ready") assert.equal(allowed.expectedRevision, 11);
});

test("R200-03 discard while uncertain cannot resubmit the old baseline", () => {
  const dirty = methodOnly(loaded());
  const saving = reduceEditorSession(dirty, { type: "saveStarted", draftId: DRAFT_A, epoch: dirty.epoch, requestId: 1 });
  const unknown = reduceEditorSession(saving, {
    type: "saveFailed",
    draftId: DRAFT_A,
    epoch: saving.epoch,
    requestId: 1,
    failure: { kind: "outcomeUnknown", reason: "unreadable" },
  });
  const discarded = reduceEditorSession(unknown, { type: "discardConfirmed", draftId: DRAFT_A });
  assert.equal(discarded.baseline, null, "old baseline must not be resubmittable");
  assert.equal(discarded.load, "idle");
  const request = selectApplyRequest(discarded, 2, fullResolver);
  assert.equal(request.status, "blocked");
  if (request.status === "blocked") assert.equal(request.reason, "not-loaded");
  const fresh = reduceEditorSession(discarded, { type: "loadStarted", draftId: DRAFT_A });
  assert.equal(fresh.load, "loading");
  const freshLoaded = reduceEditorSession(fresh, {
    type: "loadSucceeded",
    draftId: DRAFT_A,
    epoch: fresh.epoch,
    document: doc(DRAFT_A, 12),
  });
  assert.equal(freshLoaded.baseline?.revision, 12);
  assert.equal(freshLoaded.save, "clean");
});

test("R200-03 conflict/validation and same-revision no-op behaviour are preserved", () => {
  const dirty = methodOnly(loaded());
  const saving = reduceEditorSession(dirty, { type: "saveStarted", draftId: DRAFT_A, epoch: dirty.epoch, requestId: 1 });
  const conflict = reduceEditorSession(saving, {
    type: "saveFailed",
    draftId: DRAFT_A,
    epoch: saving.epoch,
    requestId: 1,
    failure: { kind: "conflict", status: 409, errorCode: "shipping.authoring.revision_conflict", title: null, errors: [] },
  });
  assert.equal(conflict.save, "conflict");
  assert.equal(conflict.unsaved, true);
  const after = selectApplyRequest(
    reduceEditorSession(conflict, { type: "edited", draftId: DRAFT_A, edits: conflict.edits }),
    2,
    fullResolver,
  );
  assert.equal(after.status, "ready");

  const noop = reduceEditorSession(saving, {
    type: "saveSucceeded",
    draftId: DRAFT_A,
    epoch: saving.epoch,
    requestId: 1,
    document: doc(DRAFT_A, 4),
  });
  assert.equal(noop.unsaved, false);
  assert.equal(noop.baseline?.revision, 4);
});

test("R200-04 read-contract coherence negatives and a valid control", () => {
  const validControl = decodeEditorDocument(parse(wire()), DRAFT_A);
  assert.equal(validControl.ok, true);

  const replacements: [string, string][] = [
    [`"groupId":"${GROUP_1}"`, `"groupId":"${ZERO}"`],
    [`"type":{"draftId":"${DRAFT_A}"`, `"type":{"draftId":"${DRAFT_B}"`],
    [`"languageId":"${EN}","code":"en"`, `"languageId":"${FA}","code":"en"`],
    [`"isDefault":false,"sortOrder":2`, `"isDefault":true,"sortOrder":2`],
    [`"endTime":"18:00:00"`, `"endTime":"99:99:99"`],
    [`"weekday":7`, `"weekday":6`],
  ];
  for (const [from, to] of replacements) {
    const text = wire().replace(from, to);
    if (text === wire()) assert.fail(`mutation_failed:${from}`);
    const decoded = decodeEditorDocument(parse(text), DRAFT_A);
    if (decoded.ok) assert.fail(`accepted_mutation:${to}`);
  }

  const sixDays = wire().replace(
    ',{"weekday":7,"isWorking":false,"startTime":"09:30:00","endTime":"18:00:00"}',
    "",
  );
  assert.notEqual(sixDays, wire());
  assert.equal(decodeEditorDocument(parse(sixDays), DRAFT_A).ok, false, "fewer than seven days is incoherent");
});

// Runs the repairs against the unchanged baseline helpers too (guards the exact wire text).
test("R200-01 baseline retention does not mutate the confirmed baseline", () => {
  const state = loaded();
  const before = JSON.stringify(state.baseline);
  bodyOf(methodOnly(state), fullResolver);
  assert.equal(JSON.stringify(state.baseline), before);
});

