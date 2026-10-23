/**
 * Local GENERAL-form intent of ONE existing draft.
 *
 * The form never becomes a second source of truth: it starts from the confirmed server baseline and
 * produces ONLY the parts the operator intentionally changed. Untouched settings/type are omitted
 * (preserve), a stored Inactive/Missing type is shown but never auto-cleared, and an unconfigured
 * setting stays a real blank state instead of a guessed default.
 */
import type {
  AuthoringCalculationMode,
  EditorDocument,
  EditorMethodTranslation,
  FreightPaymentMode,
} from "./editor-document.ts";
import type { EditorEdits, MethodSettingsEdit, MethodTranslationEdit } from "./editor-edits.ts";

/** Explicit type intent: untouched, explicit clear, or a real catalog id. */
export type TypeSelection =
  | { readonly kind: "preserve" }
  | { readonly kind: "clear" }
  | { readonly kind: "select"; readonly shippingTypeId: string };

/** Local form values for the general tab. */
export interface EditorFormState {
  readonly names: Readonly<Record<string, string>>;
  readonly descriptions: Readonly<Record<string, string | null>>;
  readonly freightPaymentMode: FreightPaymentMode | null;
  readonly calculationMode: AuthoringCalculationMode | null;
  readonly typeSelection: TypeSelection;
}

/** Local reason an intent cannot be serialized safely (localized by the screen). */
export type EditorFormIssue =
  | { readonly code: "nameRequired"; readonly languageId: string }
  | { readonly code: "settingsIncomplete" };

function languageKey(languageId: string): string {
  return languageId.toLowerCase();
}

/** The stored raw translation of one language, exactly as read (never trimmed or re-created). */
export function storedTranslation(
  document: EditorDocument,
  languageId: string,
): EditorMethodTranslation | undefined {
  const key = languageKey(languageId);
  return document.presentation.methodTranslations.find((entry) => languageKey(entry.languageId) === key);
}

/** Creates the initial form of one loaded document. */
export function createEditorForm(document: EditorDocument): EditorFormState {
  const names: Record<string, string> = {};
  const descriptions: Record<string, string | null> = {};
  for (const language of document.presentation.contentLanguages) {
    const stored = storedTranslation(document, language.languageId);
    names[languageKey(language.languageId)] = stored?.name ?? "";
    descriptions[languageKey(language.languageId)] = stored?.description ?? null;
  }
  return {
    names,
    descriptions,
    freightPaymentMode: document.settings?.freightPaymentMode ?? null,
    calculationMode: document.settings?.calculationMode ?? null,
    typeSelection: { kind: "preserve" },
  };
}

/** Name text currently shown for one language (stored baseline or typed). */
export function nameOf(form: EditorFormState, languageId: string): string {
  return form.names[languageKey(languageId)] ?? "";
}

/** Description text currently shown for one language. */
export function descriptionOf(form: EditorFormState, languageId: string): string {
  const value = form.descriptions[languageKey(languageId)];
  return value ?? "";
}

/** Purely replaces the name of ONE language; every other language keeps its own text. */
export function withName(form: EditorFormState, languageId: string, value: string): EditorFormState {
  return { ...form, names: { ...form.names, [languageKey(languageId)]: value } };
}

/** Purely replaces the description of ONE language. */
export function withDescription(form: EditorFormState, languageId: string, value: string): EditorFormState {
  return { ...form, descriptions: { ...form.descriptions, [languageKey(languageId)]: value } };
}

/** Grey/blank until the operator chooses; no implicit Prepaid/Manual. */
export function withFreightPaymentMode(
  form: EditorFormState,
  mode: FreightPaymentMode,
): EditorFormState {
  return { ...form, freightPaymentMode: mode };
}

/** Independent of the payment timing. */
export function withCalculationMode(
  form: EditorFormState,
  mode: AuthoringCalculationMode,
): EditorFormState {
  return { ...form, calculationMode: mode };
}

/** Selecting a real id emits `select`; choosing the empty option is an EXPLICIT clear. */
export function withShippingType(form: EditorFormState, shippingTypeId: string | null): EditorFormState {
  return {
    ...form,
    typeSelection:
      shippingTypeId === null ? { kind: "clear" } : { kind: "select", shippingTypeId },
  };
}

/** Select value shown by the control: stored/selected id, `""` for explicit clear. */
export function selectedShippingTypeValue(form: EditorFormState, document: EditorDocument): string {
  if (form.typeSelection.kind === "select") return form.typeSelection.shippingTypeId;
  if (form.typeSelection.kind === "clear") return "";
  return document.type.shippingTypeId ?? "";
}

function sameDescription(left: string | null, right: string | null): boolean {
  const l = left === null ? "" : left;
  const r = right === null ? "" : right;
  return l === r;
}

/**
 * Builds the intentional write intent of the current form against the confirmed baseline.
 *
 * @param document Confirmed baseline.
 * @param form Local form values.
 * @returns Only changed parts plus any local issues that must block Apply.
 */
export function buildEditorEdits(
  document: EditorDocument,
  form: EditorFormState,
): { readonly edits: EditorEdits; readonly issues: readonly EditorFormIssue[] } {
  const issues: EditorFormIssue[] = [];
  const translations: MethodTranslationEdit[] = [];
  for (const language of document.presentation.contentLanguages) {
    const key = languageKey(language.languageId);
    const stored = storedTranslation(document, language.languageId);
    const name = form.names[key] ?? stored?.name ?? "";
    const descriptionValue = form.descriptions[key];
    const rawDescription = descriptionValue === undefined ? (stored?.description ?? null) : descriptionValue;
    // An emptied description stays an EXPLICIT null, never a fabricated empty patch.
    const description = rawDescription !== null && rawDescription.trim().length === 0 ? null : rawDescription;
    const changed =
      stored === undefined
        ? name.trim().length > 0
        : stored.name !== name || !sameDescription(stored.description, description);
    if (!changed) continue;
    if (name.trim().length === 0) {
      issues.push({ code: "nameRequired", languageId: language.languageId });
      continue;
    }
    translations.push({ languageId: language.languageId, name, description });
  }

  let methodSettings: MethodSettingsEdit | undefined;
  if (form.freightPaymentMode !== null || form.calculationMode !== null) {
    if (form.freightPaymentMode === null || form.calculationMode === null) {
      issues.push({ code: "settingsIncomplete" });
    } else {
      methodSettings = {
        freightPaymentMode: form.freightPaymentMode,
        calculationMode: form.calculationMode,
      };
    }
  }

  const edits: {
    methodTranslations?: MethodTranslationEdit[];
    methodSettings?: MethodSettingsEdit;
    typeAssignment?: { shippingTypeId: string | null };
  } = {};
  if (translations.length > 0) edits.methodTranslations = translations;
  if (methodSettings) edits.methodSettings = methodSettings;
  if (form.typeSelection.kind === "select") {
    edits.typeAssignment = { shippingTypeId: form.typeSelection.shippingTypeId };
  } else if (form.typeSelection.kind === "clear") {
    edits.typeAssignment = { shippingTypeId: null };
  }
  return { edits, issues };
}

/** True when the form carries an intentional change that Apply would send. */
export function formHasEdits(edits: EditorEdits): boolean {
  return (
    edits.methodTranslations !== undefined ||
    edits.methodSettings !== undefined ||
    edits.typeAssignment !== undefined
  );
}
