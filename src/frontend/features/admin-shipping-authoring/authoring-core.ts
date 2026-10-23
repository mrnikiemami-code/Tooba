/**
 * Public boundary of the shipping authoring editor feature.
 *
 * Transport (`api`), typed read model (`model`) and local session state (`session`) stay separate;
 * the rendered capability is exported from `index.ts` and only that boundary may be imported by app
 * pages. No transport/business logic lives in a page.
 */
export type {
  AuthoringAreaKind,
  AuthoringCalculationMode,
  AuthoringOwnerKind,
  AuthoringTypeResolution,
  DecimalText,
  EditorContentLanguage,
  EditorDocument,
  EditorGroupTitle,
  EditorMethodSettings,
  EditorMethodTranslation,
  EditorOwnerView,
  EditorPresentation,
  EditorRateRow,
  EditorRegionGroup,
  EditorScheduleDay,
  EditorShippingType,
  EditorTypeAssignment,
  EditorTypeTranslation,
  EditorWeeklySchedule,
  FreightPaymentMode,
  SelectedAreaRead,
  TimeText,
  UnresolvedAreaSelection,
} from "./model/editor-document.ts";

export type {
  AreaAncestryResolver,
  EditorEdits,
  GroupTitleEdit,
  MethodSettingsEdit,
  MethodTranslationEdit,
  RateRowEdit,
  RegionGroupEdit,
  ResolvedAreaAncestry,
  ScheduleDayEdit,
  TypeAssignmentEdit,
  WeeklyScheduleEdit,
} from "./model/editor-edits.ts";

export type {
  EditorOutcome,
  EditorPropertyError,
  EditorTransportFailure,
} from "./api/editor-document-api.ts";
export { editorDocumentPath, loadEditorDocument, saveEditorDocument } from "./api/editor-document-api.ts";

export type { EditorIntentIssue, EditorRequestBody } from "./api/editor-document-request.ts";
export { buildEditorDocumentBody } from "./api/editor-document-request.ts";

export { decodeEditorDocument, sameId } from "./api/editor-document-decode.ts";
export { isDecimalText, isIntegerText, parseJsonNode, RawNumber, writeJsonText } from "./api/wire-json.ts";

export type {
  EditorApplyRequest,
  EditorDecision,
  EditorLoadStatus,
  EditorReconcileStatus,
  EditorSaveStatus,
  EditorSessionAction,
  EditorSessionState,
} from "./session/editor-session.ts";
export {
  createEditorSessionState,
  hasUnsavedWork,
  reduceEditorSession,
  selectApplyRequest,
} from "./session/editor-session.ts";


/* ————————————————— additive rendered slice (TB-202) ————————————————— */

export type {
  AuthoringDraftBindingRow,
  AuthoringTypeChoice,
  DraftEntryResolution,
  LookupOutcome,
} from "./api/authoring-lookup-api.ts";
export {
  AUTHORING_DRAFTS_PATH,
  AUTHORING_TYPES_PATH,
  decodeAuthoringDraftRows,
  decodeAuthoringTypeChoices,
  draftsMatchingBinding,
  loadAuthoringDraftRows,
  loadAuthoringTypeChoices,
  resolveDraftEntry,
} from "./api/authoring-lookup-api.ts";

export type {
  AuthoringDestinationResolution,
  EditorDestinationFact,
  EditorDestinationWriteShape,
} from "./model/editor-document.ts";
export type { DestinationFactIndex } from "./model/editor-destination-ancestry.ts";
export {
  destinationFactsBlockApply,
  indexDestinationFacts,
  unresolvedSelections,
} from "./model/editor-destination-ancestry.ts";

export type { EditorFormIssue, EditorFormState, TypeSelection } from "./model/editor-form.ts";
export {
  buildEditorEdits,
  createEditorForm,
  descriptionOf,
  formHasEdits,
  formHasLocalChanges,
  nameOf,
  selectedShippingTypeValue,
  storedTranslation,
  withCalculationMode,
  withDescription,
  withFreightPaymentMode,
  withName,
  withShippingType,
} from "./model/editor-form.ts";

export type {
  EditorApplyOutcome,
  EditorController,
  EditorControllerState,
  EditorTransport,
} from "./session/editor-controller.ts";
export { confirmedDocument, createEditorController } from "./session/editor-controller.ts";

export type { AuthoringLocale, AuthoringMessageKey, AuthoringMessages } from "./messages/authoring-messages.ts";
export { authoringFormat, authoringMessages } from "./messages/authoring-messages.ts";

/* ————————————————— additive capabilities (TB-203: rows + week) ————————————————— */

export type {
  RateField,
  RateRowDraft,
  RateRowIssue,
  RateRowIssueCode,
  RatesFormState,
} from "./model/editor-rates.ts";
export {
  RATE_LIMITS,
  buildRegionGroupsEdits,
  createRatesForm,
  draftFromRow,
  newRateRow,
  rateIssues,
  ratesDirty,
  rowIssue,
  withAddedRow,
  withRemovedRow,
  withRowField,
} from "./model/editor-rates.ts";

export type { ScheduleDayDraft, WeekFormState, WeekIssue, WeekIssueCode } from "./model/editor-week.ts";
export {
  TIME_ZONE_MAX_LENGTH,
  WEEKDAYS,
  buildWeeklyScheduleEdit,
  configureWeek,
  createWeekForm,
  resetWeek,
  weekDirty,
  weekIssues,
  withDayTime,
  withDayWorking,
  withPendingTimeZone,
  withTimeZone,
} from "./model/editor-week.ts";

export type { AuthoringIntent, AuthoringIntentIssues } from "./model/authoring-intent.ts";
export { buildAuthoringIntent, hasNoIssues, intentHasEdits } from "./model/authoring-intent.ts";

export type { AuthoringDrafts } from "./model/authoring-drafts.ts";
export { resetAuthoringDrafts, seedAuthoringDrafts } from "./model/authoring-drafts.ts";

