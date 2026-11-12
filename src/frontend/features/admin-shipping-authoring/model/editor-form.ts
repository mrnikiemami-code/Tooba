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
  PreparationProcessingMode,
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
  /**
   * TB-264: physical preparation time in minutes (`null` = not configured). It is edited and written as
   * an explicit PAIR with {@link handoffMinutes}; a one-sided value is a local issue, never a zero.
   */
  readonly preparationMinutes: number | null;
  /** TB-264: transfer/handoff allowance to the carrier in minutes (`null` = not configured). */
  readonly handoffMinutes: number | null;
  /**
   * TB-264-R1: the explicit MEANING of the pair — `null` until the operator chooses. It is never
   * defaulted to 24/7, and a declared pair without a chosen mode is a local issue.
   */
  readonly preparationMode: PreparationProcessingMode | null;
  readonly typeSelection: TypeSelection;
}

/** Local reason an intent cannot be serialized safely (localized by the screen). */
export type EditorFormIssue =
  | { readonly code: "nameRequired"; readonly languageId: string }
  | { readonly code: "settingsIncomplete" }
  | { readonly code: "timingIncomplete" }
  | { readonly code: "preparationModeRequired" }
  | { readonly code: "preparationModeWithoutTiming" };

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
    preparationMinutes: document.settings?.preparationMinutes ?? null,
    handoffMinutes: document.settings?.handoffMinutes ?? null,
    // `Unspecified` is the server's «not chosen yet»; the form shows it as an explicit blank.
    preparationMode: notChosen(document.settings?.preparationMode),
    typeSelection: { kind: "preserve" },
  };
}

/** Server `Unspecified`/absent is a real blank state, never a chosen mode. */
function notChosen(mode: PreparationProcessingMode | undefined): PreparationProcessingMode | null {
  return mode === undefined || mode === "Unspecified" ? null : mode;
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

/**
 * TB-264: physical preparation time in minutes. `null` clears the pair member; the pair is validated on
 * Apply, so the operator can type one side first without a half-written policy being sent.
 */
export function withPreparationMinutes(form: EditorFormState, minutes: number | null): EditorFormState {
  return { ...form, preparationMinutes: minutes };
}

/** TB-264: transfer/handoff allowance in minutes (the other half of the same explicit pair). */
export function withHandoffMinutes(form: EditorFormState, minutes: number | null): EditorFormState {
  return { ...form, handoffMinutes: minutes };
}

/** TB-264-R1: the explicit meaning of the preparation minutes; `null` returns to «not chosen». */
export function withPreparationMode(
  form: EditorFormState,
  mode: PreparationProcessingMode | null,
): EditorFormState {
  return { ...form, preparationMode: mode };
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

/** The stored raw description of one language (null when there is none). */
function storedDescription(document: EditorDocument, languageId: string): string | null {
  return storedTranslation(document, languageId)?.description ?? null;
}

/** The raw description currently held by the form, falling back to the stored one. */
function formDescription(form: EditorFormState, languageId: string, document: EditorDocument): string | null {
  const value = form.descriptions[languageKey(languageId)];
  return value === undefined ? storedDescription(document, languageId) : value;
}

/** An emptied description is the same EXPLICIT clear whether it is `""` or blank whitespace. */
function normalizeDescription(value: string | null): string | null {
  return value !== null && value.trim().length === 0 ? null : value;
}

/** True when BOTH settings equal the confirmed baseline (no wrapper must be emitted). */
function settingsEqualBaseline(document: EditorDocument, form: EditorFormState): boolean {
  return (
    form.freightPaymentMode === (document.settings?.freightPaymentMode ?? null) &&
    form.calculationMode === (document.settings?.calculationMode ?? null) &&
    form.preparationMinutes === (document.settings?.preparationMinutes ?? null) &&
    form.handoffMinutes === (document.settings?.handoffMinutes ?? null) &&
    form.preparationMode === notChosen(document.settings?.preparationMode)
  );
}

/**
 * Builds the intentional write intent of the current form against the confirmed baseline.
 *
 * Only real differences from the confirmed baseline are serialized: an untouched configured
 * setting (or a type id equal to the stored one) never becomes a write, and reverting every value
 * back to the baseline yields `{}`. Local problems (blank name, one-sided settings) are reported as
 * issues and are never turned into a partial patch that silently drops them.
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
    const rawDescription = formDescription(form, language.languageId, document);
    // An emptied description stays an EXPLICIT null, never a fabricated empty patch.
    const description = normalizeDescription(rawDescription);
    // Any raw material difference counts — including a blank name and a description typed for a
    // language that has no stored translation yet (that is a real intent with a local name issue).
    const nameDiffers = stored === undefined ? name.length > 0 : stored.name !== name;
    const descriptionDiffers = !sameDescription(storedDescription(document, language.languageId), description);
    if (!nameDiffers && !descriptionDiffers) continue;
    if (name.trim().length === 0) {
      issues.push({ code: "nameRequired", languageId: language.languageId });
      continue;
    }
    translations.push({ languageId: language.languageId, name, description });
  }

  let methodSettings: MethodSettingsEdit | undefined;
  if (!settingsEqualBaseline(document, form)) {
    if (form.freightPaymentMode === null || form.calculationMode === null) {
      issues.push({ code: "settingsIncomplete" });
    } else if ((form.preparationMinutes === null) !== (form.handoffMinutes === null)) {
      // TB-264: the physical timing policy is an explicit pair — both values or neither.
      issues.push({ code: "timingIncomplete" });
    } else if (form.preparationMinutes !== null && form.preparationMode === null) {
      // TB-264-R1: a declared pair has NO implicit meaning; the operator must choose the processing
      // basis instead of inheriting a silent 24/7 default.
      issues.push({ code: "preparationModeRequired" });
    } else if (form.preparationMinutes === null && form.preparationMode !== null) {
      // A processing mode without a physical policy would be stored as a meaning-less setting.
      issues.push({ code: "preparationModeWithoutTiming" });
    } else {
      // Once a settings write exists it carries BOTH axes, so the untouched axis is preserved.
      // The timing pair is written verbatim, including an explicit `null` pair that clears the policy.
      methodSettings = {
        freightPaymentMode: form.freightPaymentMode,
        calculationMode: form.calculationMode,
        preparationMinutes: form.preparationMinutes,
        handoffMinutes: form.handoffMinutes,
        preparationMode: form.preparationMode,
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
  // Preserved accepted-202 semantics: an explicit select/clear is a real operator intent. Only an
  // untouched (`preserve`) selection is omitted; a select equal to the stored id is not coerced.
  if (form.typeSelection.kind === "select") {
    edits.typeAssignment = { shippingTypeId: form.typeSelection.shippingTypeId };
  } else if (form.typeSelection.kind === "clear") {
    edits.typeAssignment = { shippingTypeId: null };
  }
  return { edits, issues };
}

/**
 * True when the operator changed ANY raw form value against the confirmed baseline.
 *
 * This is deliberately WIDER than {@link buildEditorEdits}: invalid work (a blanked name, an
 * unfinished settings pair, a description typed before its name) is real local work. It must stay
 * dirty, keep the discard warning and block Apply instead of looking clean just because it cannot
 * be serialized yet.
 */
export function formHasLocalChanges(document: EditorDocument, form: EditorFormState): boolean {
  for (const language of document.presentation.contentLanguages) {
    const key = languageKey(language.languageId);
    const stored = storedTranslation(document, language.languageId);
    const name = form.names[key] ?? stored?.name ?? "";
    if (stored === undefined ? name.length > 0 : stored.name !== name) return true;
    if (
      normalizeDescription(formDescription(form, language.languageId, document)) !==
      normalizeDescription(storedDescription(document, language.languageId))
    ) {
      return true;
    }
  }
  if (!settingsEqualBaseline(document, form)) return true;
  if (form.typeSelection.kind === "select") return form.typeSelection.shippingTypeId !== document.type.shippingTypeId;
  if (form.typeSelection.kind === "clear") return document.type.shippingTypeId !== null;
  return false;
}

/** True when the form carries an intentional change that Apply would send. */
export function formHasEdits(edits: EditorEdits): boolean {
  return (
    edits.methodTranslations !== undefined ||
    edits.methodSettings !== undefined ||
    edits.typeAssignment !== undefined
  );
}
