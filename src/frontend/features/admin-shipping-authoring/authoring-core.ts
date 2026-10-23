/**
 * Public boundary of the shipping authoring editor feature.
 *
 * Transport (`api`), typed read model (`model`) and local session state (`session`) stay separate so
 * the presentation slice can be added later without moving ownership. No rendered page is exported
 * yet: this slice is the typed adapter + session foundation only.
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

