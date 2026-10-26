/**
 * Public boundary of the shipping authoring feature — the ONLY module the app imports.
 *
 * `authoring-core.ts` holds the non-rendered surface (typed model, authorized transport, strict decode,
 * ancestry index, local session, controller, messages) so unit tests can load it without a JSX
 * transform. The rendered capability of this slice is exported from here for routes/pages:
 * - `ShippingAuthoringScreen`: the composed draft editor;
 * - `ShippingAuthoringDraftEntry`: the read-only legacy-method entry component;
 * - `ShippingAuthoringMethodListScreen`: the owner-authorized list of existing authoring drafts.
 */
export * from "./authoring-core.ts";

export { ShippingAuthoringScreen } from "./ui/authoring-screen.tsx";
export { ShippingAuthoringDraftEntry } from "./ui/authoring-draft-entry.tsx";

/* ————————————————— primary method list (TB-221) ————————————————— */

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

export { ShippingAuthoringMethodListScreen } from "./ui/authoring-method-list-screen.tsx";
