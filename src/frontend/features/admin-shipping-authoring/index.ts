/**
 * Public boundary of the shipping authoring feature — the ONLY module the app imports.
 *
 * `authoring-core.ts` holds the non-rendered surface (typed model, authorized transport, strict decode,
 * ancestry index, local session, controller, messages) so unit tests can load it without a JSX
 * transform. The rendered capability of this slice is exported from here for routes/pages:
 * - `ShippingAuthoringScreen`: the composed draft editor;
 * - `ShippingAuthoringDraftEntry`: the read-only legacy-method entry component.
 */
export * from "./authoring-core.ts";

export { ShippingAuthoringScreen } from "./ui/authoring-screen.tsx";
export { ShippingAuthoringDraftEntry } from "./ui/authoring-draft-entry.tsx";

/* ————————————————— primary method list + publication action (TB-221) ————————————————— */

export type {
  AuthoringContextLanguage,
  AuthoringContextRead,
  AuthoringListOutcome,
  AuthoringMethodRow,
  AuthoringMethodTranslationRow,
} from "./api/authoring-method-list-api.ts";
export {
  decodeAuthoringContext,
  decodeAuthoringMethodRows,
  duplicateBindingDraftIds,
  loadAuthoringContext,
  loadAuthoringMethodRows,
  methodBinding,
  storedMethodName,
} from "./api/authoring-method-list-api.ts";

export type { AuthoringPublicationState, AuthoringPublicationView, PublicationOutcome } from "./api/authoring-publication-api.ts";
export {
  decodeAuthoringPublication,
  loadAuthoringPublication,
  publicationPath,
  publicationWithdrawPath,
  publishAuthoringDraft,
  publishRequestBody,
  withdrawAuthoringPublication,
  withdrawRequestBody,
} from "./api/authoring-publication-api.ts";

export type {
  PublicationPanelState,
  PublicationPhase,
  PublicationReadState,
  PublicationWrite,
} from "./model/authoring-publication.ts";
export {
  beginRead,
  beginWrite,
  canPublish,
  canWithdraw,
  initialPublicationState,
  publicationBlockedReason,
  publicationPhase,
  publicationStateMessage,
  resolveRead,
  resolveWrite,
} from "./model/authoring-publication.ts";

export { ShippingAuthoringMethodListScreen } from "./ui/authoring-method-list-screen.tsx";
export { AuthoringPublicationPanel } from "./ui/authoring-publication-panel.tsx";
