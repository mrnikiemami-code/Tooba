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
