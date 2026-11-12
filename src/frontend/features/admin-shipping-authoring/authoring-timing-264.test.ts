import assert from "node:assert/strict";
import test from "node:test";
import {
  buildEditorEdits,
  createEditorForm,
  formHasLocalChanges,
  withHandoffMinutes,
  withPreparationMinutes,
  withPreparationMode,
} from "./authoring-core.ts";
import type { EditorDocument } from "./authoring-core.ts";

/**
 * TB-264/TB-264-R1: the physical preparation/transfer timing pair lives in the EXISTING authoring form and
 * patch, and its MEANING (working week vs explicit 24/7) is a first-class, operator-chosen field.
 *
 * A focused stub document is deliberate here: the assertions are about the timing pair's effect on the
 * form/edit contract, not about decoding (which the adapter tests already cover field-by-field).
 */
const DRAFT = "11111111-1111-4111-8111-111111111111";
const LANGUAGE_FA = "44444444-4444-4444-8444-444444444444";

function document(settings: EditorDocument["settings"]): EditorDocument {
  return {
    draftId: DRAFT,
    owner: { kind: "Marketplace", sellerPartyId: null },
    serviceCode: "tooba.post",
    optionCode: null,
    revision: 7,
    createdAt: "2026-11-11T10:00:00.0000000+03:30",
    updatedAt: "2026-11-11T10:00:00.0000000+03:30",
    type: {
      draftId: DRAFT,
      revision: 7,
      shippingTypeId: null,
      resolution: "Unconfigured",
      shippingType: null,
    },
    settings,
    presentation: {
      methodTranslations: [{ languageId: LANGUAGE_FA, name: "پست پیشتاز", description: null }],
      groupTitles: [],
      contentLanguages: [
        {
          languageId: LANGUAGE_FA,
          code: "fa",
          culture: "fa-IR",
          urlPrefix: "",
          direction: "rtl",
          displayName: "فارسی",
          nativeName: "فارسی",
          isDefault: true,
          sortOrder: 1,
        },
      ],
      defaultContentLanguageId: LANGUAGE_FA,
      hasDefaultLanguageName: true,
    },
    weeklySchedule: null,
    carrierSchedule: null,
    rates: [],
    groups: [],
    destinations: [],
  } as unknown as EditorDocument;
}

test("TB-264: an untouched timing pair produces no settings write", () => {
  const stored = document({
    freightPaymentMode: "Prepaid",
    calculationMode: "Manual",
    preparationMinutes: 120,
    handoffMinutes: 45,
    preparationMode: "WorkingWeek",
  });
  const form = createEditorForm(stored);

  assert.equal(form.preparationMinutes, 120);
  assert.equal(form.handoffMinutes, 45);
  assert.equal(form.preparationMode, "WorkingWeek");
  assert.equal(formHasLocalChanges(stored, form), false);

  const { edits, issues } = buildEditorEdits(stored, form);
  assert.equal(issues.length, 0);
  assert.equal(edits.methodSettings, undefined);
});

test("TB-264: editing only preparation still writes the complete pair and is a real change", () => {
  const stored = document({
    freightPaymentMode: "Prepaid",
    calculationMode: "Manual",
    preparationMinutes: 120,
    handoffMinutes: 45,
    preparationMode: "WorkingWeek",
  });
  const base = createEditorForm(stored);
  const form = withPreparationMinutes(base, 150);

  assert.equal(formHasLocalChanges(stored, form), true);

  const { edits, issues } = buildEditorEdits(stored, form);
  assert.equal(issues.length, 0);
  assert.deepEqual(edits.methodSettings, {
    freightPaymentMode: "Prepaid",
    calculationMode: "Manual",
    preparationMinutes: 150,
    handoffMinutes: 45,
    preparationMode: "WorkingWeek",
  });
});

test("TB-264: a one-sided pair is a local issue and never a partial write", () => {
  // Legacy method: no timing policy at all.
  const stored = document({ freightPaymentMode: "Prepaid", calculationMode: "Manual" });
  const form = withPreparationMinutes(createEditorForm(stored), 90);

  const { edits, issues } = buildEditorEdits(stored, form);
  assert.deepEqual(issues.map((issue) => issue.code), ["timingIncomplete"]);
  assert.equal(edits.methodSettings, undefined);

  // Filling the other half clears the issue and produces the complete pair.
  const complete = withPreparationMode(withHandoffMinutes(form, 30), "Continuous");
  const ok = buildEditorEdits(stored, complete);
  assert.equal(ok.issues.length, 0);
  assert.deepEqual(ok.edits.methodSettings, {
    freightPaymentMode: "Prepaid",
    calculationMode: "Manual",
    preparationMinutes: 90,
    handoffMinutes: 30,
    preparationMode: "Continuous",
  });
});

test("TB-264-R1: a declared pair without a chosen processing meaning is refused locally", () => {
  // A stored pair whose meaning was never chosen reads back as an explicit blank, not as 24/7.
  const stored = document({
    freightPaymentMode: "Prepaid",
    calculationMode: "Manual",
    preparationMinutes: 120,
    handoffMinutes: 45,
    preparationMode: "Unspecified",
  });
  const form = withPreparationMinutes(createEditorForm(stored), 130);

  const { edits, issues } = buildEditorEdits(stored, form);
  assert.deepEqual(issues.map((issue) => issue.code), ["preparationModeRequired"]);
  assert.equal(edits.methodSettings, undefined);

  // Choosing the meaning is itself a real intent and unlocks the complete write.
  const chosen = withPreparationMode(form, "WorkingWeek");
  const ok = buildEditorEdits(stored, chosen);
  assert.equal(ok.issues.length, 0);
  assert.deepEqual(ok.edits.methodSettings, {
    freightPaymentMode: "Prepaid",
    calculationMode: "Manual",
    preparationMinutes: 130,
    handoffMinutes: 45,
    preparationMode: "WorkingWeek",
  });
});

test("TB-264-R1: a processing meaning without any timing pair is refused, never stored alone", () => {
  const stored = document({ freightPaymentMode: "Prepaid", calculationMode: "Manual" });
  const form = withPreparationMode(createEditorForm(stored), "Continuous");

  assert.equal(formHasLocalChanges(stored, form), true);
  const { edits, issues } = buildEditorEdits(stored, form);
  assert.deepEqual(issues.map((issue) => issue.code), ["preparationModeWithoutTiming"]);
  assert.equal(edits.methodSettings, undefined);
});

test("TB-264: clearing both values removes the policy explicitly instead of leaving a stale one", () => {
  const stored = document({
    freightPaymentMode: "Prepaid",
    calculationMode: "Manual",
    preparationMinutes: 120,
    handoffMinutes: 45,
    preparationMode: "WorkingWeek",
  });
  const cleared = withPreparationMode(
    withHandoffMinutes(withPreparationMinutes(createEditorForm(stored), null), null),
    null,
  );

  const { edits, issues } = buildEditorEdits(stored, cleared);
  assert.equal(issues.length, 0);
  assert.deepEqual(edits.methodSettings, {
    freightPaymentMode: "Prepaid",
    calculationMode: "Manual",
    preparationMinutes: null,
    handoffMinutes: null,
    preparationMode: null,
  });
});
