import assert from "node:assert/strict";
import test from "node:test";
import {
  buildEditorEdits,
  createEditorForm,
  formHasLocalChanges,
  withCalculationMode,
  withDescription,
  withFreightPaymentMode,
  withName,
  withShippingType,
  type EditorFormState,
} from "./model/editor-form.ts";
import type { EditorDocument, EditorMethodSettings } from "./model/editor-document.ts";
import { createEditorController, type EditorTransport } from "./session/editor-controller.ts";
import type { EditorOutcome } from "./api/editor-document-api.ts";
import { planAuthoringExit } from "./ui/authoring-exit.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const FA = "44444444-4444-4444-8444-444444444441";
const EN = "55555555-5555-4555-8555-555555555551";

function documentWith(settings: EditorMethodSettings | null, revision = 4): EditorDocument {
  const language = (languageId: string, code: string, index: number) => ({
    languageId,
    code,
    culture: code === "fa" ? "fa-IR" : "en-US",
    urlPrefix: code === "fa" ? "" : code,
    direction: code === "fa" ? ("rtl" as const) : ("ltr" as const),
    displayName: code,
    nativeName: code,
    isDefault: index === 0,
    sortOrder: index + 1,
  });
  return {
    draftId: DRAFT,
    owner: { kind: "Store", sellerPartyId: null },
    serviceCode: "tooba.post",
    optionCode: null,
    revision,
    createdAt: "2026-10-23T00:15:00+03:30",
    updatedAt: "2026-10-23T01:45:10+03:30",
    type: { draftId: DRAFT, revision, shippingTypeId: null, resolution: "Unconfigured", shippingType: null },
    settings,
    presentation: {
      methodTranslations: [{ languageId: FA, name: "پست", description: "سرویس پستی" }],
      groupTitles: [],
      contentLanguages: [language(FA, "fa", 0), language(EN, "en", 1)],
      defaultContentLanguageId: FA,
      hasDefaultLanguageName: true,
    },
    weeklySchedule: null,
    regionGroups: [],
    destinationFacts: null,
  };
}

function deferred<T>(): { readonly promise: Promise<T>; readonly resolve: (value: T) => void } {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}

test("R202-01 configured but untouched settings emit no wrapper; changing one axis keeps the other", () => {
  const document = documentWith({ freightPaymentMode: "Prepaid", calculationMode: "Manual" });
  const base = createEditorForm(document);
  assert.deepEqual(buildEditorEdits(document, base).edits, {});
  assert.deepEqual(buildEditorEdits(document, base).issues, []);
  assert.equal(formHasLocalChanges(document, base), false);

  const changed = withCalculationMode(base, "Free");
  const built = buildEditorEdits(document, changed);
  assert.deepEqual(built.edits.methodSettings, { freightPaymentMode: "Prepaid", calculationMode: "Free" });
  assert.equal(formHasLocalChanges(document, changed), true);

  // Reverting every value to the confirmed baseline is clean again — no phantom dirty state.
  const reverted = withCalculationMode(changed, "Manual");
  assert.deepEqual(buildEditorEdits(document, reverted).edits, {});
  assert.equal(formHasLocalChanges(document, reverted), false);
});

test("R202-02 a blanked stored name is dirty and invalid, never a silently clean edit", () => {
  const document = documentWith(null);
  const blank = withName(createEditorForm(document), FA, "");
  const built = buildEditorEdits(document, blank);
  assert.deepEqual(built.issues, [{ code: "nameRequired", languageId: FA }]);
  assert.deepEqual(built.edits, {});
  assert.equal(formHasLocalChanges(document, blank), true);
});

test("R202-03 a description typed for an untranslated language reports nameRequired and stays dirty", () => {
  const document = documentWith(null);
  const typed = withDescription(createEditorForm(document), EN, "English description");
  const built = buildEditorEdits(document, typed);
  assert.deepEqual(built.issues, [{ code: "nameRequired", languageId: EN }]);
  assert.deepEqual(built.edits, {});
  assert.equal(formHasLocalChanges(document, typed), true);
});

test("R202-04 one-sided settings on an unconfigured baseline are dirty AND invalid", () => {
  const document = documentWith(null);
  const half = withFreightPaymentMode(createEditorForm(document), "Prepaid");
  const built = buildEditorEdits(document, half);
  assert.deepEqual(built.issues, [{ code: "settingsIncomplete" }]);
  assert.deepEqual(built.edits, {});
  assert.equal(formHasLocalChanges(document, half), true);
});

test("R202-05 untouched type is omitted; an explicit clear stays an explicit operator intent", () => {
  const document = documentWith(null);
  const base = createEditorForm(document);
  assert.deepEqual(buildEditorEdits(document, base).edits, {});
  // Preserved accepted-202 semantics: choosing the empty option is an explicit clear, not a no-op.
  assert.deepEqual(buildEditorEdits(document, withShippingType(base, null)).edits, {
    typeAssignment: { shippingTypeId: null },
  });
  assert.equal(formHasLocalChanges(document, withShippingType(base, null)), false);

  const typed: EditorDocument = {
    ...document,
    type: {
      draftId: DRAFT,
      revision: 4,
      shippingTypeId: "88888888-8888-4888-8888-888888888881",
      resolution: "Active",
      shippingType: null,
    },
  };
  const typedBase = createEditorForm(typed);
  assert.deepEqual(buildEditorEdits(typed, typedBase).edits, {});
  assert.deepEqual(buildEditorEdits(typed, withShippingType(typedBase, null)).edits, {
    typeAssignment: { shippingTypeId: null },
  });
});

test("R202-06 invalid raw work stays unsaved and can never become an empty PUT", async () => {
  const document = documentWith(null);
  let saves = 0;
  const transport: EditorTransport = {
    load: async (): Promise<EditorOutcome> => ({ kind: "ok", document }),
    save: async (): Promise<EditorOutcome> => {
      saves += 1;
      return { kind: "ok", document: documentWith(null, 5) };
    },
  };
  const controller = createEditorController(DRAFT, transport);
  controller.begin();
  await new Promise((resolve) => setImmediate(resolve));

  const blank = withName(createEditorForm(document), FA, "");
  const built = buildEditorEdits(document, blank);
  controller.edit(built.edits, formHasLocalChanges(document, blank));
  assert.equal(controller.getState().session.unsaved, true, "invalid raw work remains dirty");
  assert.equal(controller.getState().session.save, "clean");

  const blocked = await controller.apply({ resolve: () => null });
  assert.equal(blocked.status, "blocked");
  assert.equal(saves, 0, "no empty/no-change PUT was sent");

  // A later render or an invalid edited({}) must not erase the raw work.
  controller.edit({}, true);
  assert.equal(controller.getState().session.unsaved, true);
  controller.dispose();
});

test("R202-07 explicit cancel/back plan is blocked while saving and confirms real work", () => {
  assert.equal(planAuthoringExit({ save: "clean", reconciling: false, hasLocalWork: false }), "direct");
  assert.equal(planAuthoringExit({ save: "clean", reconciling: false, hasLocalWork: true }), "confirm");
  assert.equal(planAuthoringExit({ save: "outcomeUnknown", reconciling: false, hasLocalWork: true }), "confirm");
  assert.equal(planAuthoringExit({ save: "saving", reconciling: false, hasLocalWork: true }), "blocked");
  assert.equal(planAuthoringExit({ save: "clean", reconciling: true, hasLocalWork: true }), "blocked");
});

test("R202-08 disposed controller ignores a late save and never fabricates success metadata", async () => {
  const document = documentWith(null);
  const gate = deferred<EditorOutcome>();
  const transport: EditorTransport = {
    load: async () => ({ kind: "ok", document }),
    save: async () => gate.promise,
  };
  const controller = createEditorController(DRAFT, transport);
  controller.begin();
  await new Promise((resolve) => setImmediate(resolve));
  assert.equal(controller.getState().formSeed, 1, "an accepted load seeds the form once");

  const form = withName(createEditorForm(document), FA, "پست ویژه");
  controller.edit(buildEditorEdits(document, form).edits);
  const pending = controller.apply({ resolve: () => null });
  controller.dispose();
  gate.resolve({ kind: "ok", document: documentWith(null, 9) });

  assert.equal((await pending).status, "ignored");
  assert.equal(controller.getState().savedRevision, null);
  assert.equal(controller.getState().formSeed, 1);
  assert.equal(controller.getState().session.baseline!.revision, 4);
  assert.equal(controller.getState().session.unsaved, true);
});

test("R202-09 an accepted save advances the form seed; reconcile keeps pending work and seed", async () => {
  const document = documentWith(null);
  let loads = 0;
  let saves = 0;
  const transport: EditorTransport = {
    load: async () => {
      loads += 1;
      return { kind: "ok", document: documentWith(null, loads === 1 ? 4 : 7) };
    },
    save: async (): Promise<EditorOutcome> => {
      saves += 1;
      return saves === 1
        ? { kind: "failure", failure: { kind: "outcomeUnknown", reason: "network" } }
        : { kind: "ok", document: documentWith(null, 5) };
    },
  };
  const controller = createEditorController(DRAFT, transport);
  controller.begin();
  await new Promise((resolve) => setImmediate(resolve));
  assert.equal(controller.getState().formSeed, 1);

  const form = withName(createEditorForm(document), FA, "پست ۲");
  controller.edit(buildEditorEdits(document, form).edits, true);
  await controller.apply({ resolve: () => null });
  assert.equal(controller.getState().session.commitUncertain, true);
  assert.equal(controller.getState().formSeed, 1, "a save attempt never re-seeds the form");

  await controller.reconcile();
  const after = controller.getState();
  assert.equal(after.session.commitUncertain, false);
  assert.equal(after.session.baseline!.revision, 7);
  assert.equal(after.session.unsaved, true, "pending local edits survive reconciliation for review");
  assert.equal(after.formSeed, 1, "reconcile must not erase local work by re-seeding");

  // A definitive accepted save does re-seed from the new confirmed baseline.
  const second = withName(createEditorForm(after.session.baseline!), FA, "پست ۳");
  controller.edit(buildEditorEdits(after.session.baseline!, second).edits);
  assert.equal((await controller.apply({ resolve: () => null })).status, "sent");
  assert.equal(controller.getState().formSeed, 2);
  assert.equal(controller.getState().savedRevision, 5);
  controller.dispose();
});

test("R202-10 a fresh form is not considered dirty by typing then clearing a description", () => {
  const document = documentWith(null);
  const cleared = withDescription(withDescription(createEditorForm(document), EN, "x"), EN, "");
  assert.equal(formHasLocalChanges(document, cleared), false);
  const form: EditorFormState = cleared;
  assert.deepEqual(buildEditorEdits(document, form).edits, {});
});
