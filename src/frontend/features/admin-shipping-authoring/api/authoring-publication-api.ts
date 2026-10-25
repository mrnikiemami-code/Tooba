/**
 * Transport + strict decode of the publication action of ONE existing draft.
 *
 * Three real routes, all under the same authorized authoring-drafts family and through the SAME
 * `authoringHeaders` transport as the editor:
 * - `GET  …/{draftId}/publication`                 — the stored pointer state;
 * - `POST …/{draftId}/publication`                 — capture the saved revision and select it;
 * - `POST …/{draftId}/publication/withdraw`        — clear the selection.
 *
 * The wire carries only the two version tokens; owner/tenant/seller/edition/currency are server-derived
 * and never part of a body. Every token is an exact integer: a rounded 64-bit value is refused instead of
 * being sent as a different revision. A write whose outcome cannot be established becomes
 * `outcomeUnknown` and MUST be resolved by an explicit status read, never by a retry.
 */
import { authoringHeaders, editorTransportFailureFor, type EditorTransportFailure } from "./editor-document-api.ts";
import { isIntegerText, parseJsonNode, RawNumber, writeJsonText, type JsonNode } from "./wire-json.ts";

/** Publication state of one binding, in the exact shape the server reports it. */
export type AuthoringPublicationState = "absent" | "cleared" | "selected";

/** One decoded publication view of ONE draft binding. */
export interface AuthoringPublicationView {
  readonly draftId: string;
  readonly currentDraftRevision: number;
  readonly serviceCode: string;
  readonly optionCode: string | null;
  readonly state: AuthoringPublicationState;
  readonly selectionVersion: number;
  readonly selectedSnapshotId: string | null;
  readonly selectedDraftId: string | null;
  readonly selectedSourceRevision: number | null;
  readonly selectedFormatVersion: number | null;
  readonly selectedMatchesCurrentDraftRevision: boolean;
}

/** Result of one publication call. */
export type PublicationOutcome =
  | { readonly kind: "ok"; readonly view: AuthoringPublicationView }
  | { readonly kind: "failure"; readonly failure: EditorTransportFailure };

const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const ZERO_GUID = "00000000-0000-0000-0000-000000000000";

class PublicationDecodeFailure extends Error {}

/** Exact authorized route of the publication action of one draft. */
export function publicationPath(draftId: string): string {
  return `/v1/admin/shipping-services/authoring-drafts/${encodeURIComponent(draftId)}/publication`;
}

/** Exact authorized route of the withdrawal action of one draft. */
export function publicationWithdrawPath(draftId: string): string {
  return `${publicationPath(draftId)}/withdraw`;
}

function object(node: JsonNode | undefined): ReadonlyMap<string, JsonNode> {
  if (!node || node.kind !== "object") throw new PublicationDecodeFailure("object");
  return node.fields;
}

function text(node: JsonNode | undefined): string {
  if (!node || node.kind !== "string") throw new PublicationDecodeFailure("string");
  return node.value;
}

function required(node: ReadonlyMap<string, JsonNode>, key: string): JsonNode {
  const value = node.get(key);
  if (value === undefined) throw new PublicationDecodeFailure(`missing:${key}`);
  return value;
}

/** Nullable guid; `Host` may OMIT a null field, so an absent key and an explicit null are both absent. */
function optionalGuid(node: JsonNode | undefined): string | null {
  if (node === undefined || node.kind === "null") return null;
  const value = text(node);
  if (!GUID.test(value) || value.toLowerCase() === ZERO_GUID) throw new PublicationDecodeFailure("guid");
  return value;
}

/** Non-negative integer token: exact text, safe range, never a rounded 64-bit value. */
function token(node: JsonNode | undefined, key: string): number {
  if (!node || node.kind !== "number" || !isIntegerText(node.raw)) {
    throw new PublicationDecodeFailure(`token:${key}`);
  }
  const value = Number(node.raw);
  if (!Number.isSafeInteger(value) || String(value) !== node.raw) {
    throw new PublicationDecodeFailure(`token:${key}`);
  }
  if (value < 0) throw new PublicationDecodeFailure(`negative:${key}`);
  return value;
}

function optionalToken(node: JsonNode | undefined): number | null {
  if (node === undefined || node.kind === "null") return null;
  return token(node, "optional");
}

/**
 * Decodes and validates ONE publication view against the draft the caller asked about.
 *
 * The state/provenance pairs are enforced instead of tolerated: `absent` is the ONLY state that may
 * report version 0 and it carries no provenance, `cleared` keeps a real positive version and no
 * provenance, and `selected` carries a complete capture provenance. A response that contradicts that
 * contract is `untrusted`, never repaired into a fabricated token or snapshot.
 *
 * @param node Parsed response root.
 * @param expectedDraftId Draft id from the route.
 * @returns The validated view.
 */
export function decodeAuthoringPublication(node: JsonNode, expectedDraftId: string): AuthoringPublicationView {
  const root = object(node);
  const draftId = text(required(root, "draftId"));
  if (!GUID.test(draftId) || draftId.toLowerCase() !== expectedDraftId.toLowerCase()) {
    throw new PublicationDecodeFailure("draftId:mismatch");
  }

  const stateText = text(required(root, "state"));
  const state: AuthoringPublicationState =
    stateText === "Absent" ? "absent" : stateText === "Cleared" ? "cleared" : stateText === "Selected" ? "selected" : (() => {
      throw new PublicationDecodeFailure(`state:${stateText}`);
    })();

  const matches = required(root, "selectedMatchesCurrentDraftRevision");
  if (matches.kind !== "boolean") throw new PublicationDecodeFailure("matches");

  const view: AuthoringPublicationView = {
    draftId,
    currentDraftRevision: token(required(root, "currentDraftRevision"), "currentDraftRevision"),
    serviceCode: text(required(root, "serviceCode")),
    optionCode: (() => {
      const value = root.get("optionCode");
      if (value === undefined || value.kind === "null") return null;
      return text(value);
    })(),
    state,
    selectionVersion: token(required(root, "selectionVersion"), "selectionVersion"),
    selectedSnapshotId: optionalGuid(root.get("selectedSnapshotId")),
    selectedDraftId: optionalGuid(root.get("selectedDraftId")),
    selectedSourceRevision: optionalToken(root.get("selectedSourceRevision")),
    selectedFormatVersion: optionalToken(root.get("selectedFormatVersion")),
    selectedMatchesCurrentDraftRevision: matches.value,
  };

  const provenance = view.selectedSnapshotId !== null || view.selectedDraftId !== null
    || view.selectedSourceRevision !== null;
  if (state === "absent") {
    if (view.selectionVersion !== 0) throw new PublicationDecodeFailure("absent:version");
    if (provenance) throw new PublicationDecodeFailure("absent:provenance");
    return view;
  }
  if (view.selectionVersion === 0) throw new PublicationDecodeFailure(`${state}:version`);
  if (state === "cleared") {
    if (provenance) throw new PublicationDecodeFailure("cleared:provenance");
    return view;
  }
  if (view.selectedSnapshotId === null || view.selectedDraftId === null || view.selectedSourceRevision === null) {
    throw new PublicationDecodeFailure("selected:provenance");
  }
  return view;
}

/**
 * Exact publish body: the confirmed editor baseline revision and the pointer version the operator saw.
 *
 * @param expectedRevision Saved revision the operator confirmed.
 * @param expectedSelectionVersion Pointer version read immediately before the action.
 * @returns JSON text with both tokens as exact integers.
 */
export function publishRequestBody(expectedRevision: number, expectedSelectionVersion: number): string {
  return writeJsonText({
    expectedRevision: new RawNumber(tokenText(expectedRevision, "expectedRevision")),
    expectedSelectionVersion: new RawNumber(tokenText(expectedSelectionVersion, "expectedSelectionVersion")),
  });
}

/**
 * Exact withdraw body: only the pointer version the operator saw.
 *
 * @param expectedSelectionVersion Pointer version read immediately before the action.
 * @returns JSON text with the one token as an exact integer.
 */
export function withdrawRequestBody(expectedSelectionVersion: number): string {
  return writeJsonText({
    expectedSelectionVersion: new RawNumber(tokenText(expectedSelectionVersion, "expectedSelectionVersion")),
  });
}

function tokenText(value: number, key: string): string {
  if (!Number.isSafeInteger(value) || value < 0) {
    throw new Error(`publication_token_invalid:${key}`);
  }
  return String(value);
}

async function send(
  path: string,
  init: RequestInit,
  draftId: string,
  signal: AbortSignal | undefined,
  onUnknown: boolean,
): Promise<PublicationOutcome> {
  let response: Response;
  try {
    response = await fetch(path, { ...init, signal });
  } catch {
    const aborted = signal?.aborted === true;
    return {
      kind: "failure",
      failure: onUnknown
        ? { kind: "outcomeUnknown", reason: aborted ? "aborted" : "network" }
        : { kind: "network", aborted },
    };
  }

  const body = await response.text().then(
    (value) => value,
    () => null,
  );

  if (!response.ok) {
    const root = body === null ? null : parseJsonNode(body);
    return { kind: "failure", failure: editorTransportFailureFor(response.status, root) };
  }
  const root = body === null ? null : parseJsonNode(body);
  if (root === null) {
    return {
      kind: "failure",
      failure: onUnknown
        ? { kind: "outcomeUnknown", reason: "unreadable" }
        : { kind: "untrusted", status: response.status, reason: "body:unreadable" },
    };
  }
  try {
    return { kind: "ok", view: decodeAuthoringPublication(root, draftId) };
  } catch (error) {
    if (error instanceof PublicationDecodeFailure) {
      // A success status with an unusable body after a WRITE is an unknown outcome, not a success.
      return {
        kind: "failure",
        failure: onUnknown
          ? { kind: "outcomeUnknown", reason: "unreadable" }
          : { kind: "untrusted", status: response.status, reason: error.message },
      };
    }
    throw error;
  }
}

/** Reads the stored publication state of one owned draft. */
export function loadAuthoringPublication(draftId: string, signal?: AbortSignal): Promise<PublicationOutcome> {
  return send(publicationPath(draftId), { method: "GET", headers: authoringHeaders(), cache: "no-store" }, draftId, signal, false);
}

/** Captures the current saved revision and selects it (ONE deliberate publication). */
export function publishAuthoringDraft(
  draftId: string,
  expectedRevision: number,
  expectedSelectionVersion: number,
  signal?: AbortSignal,
): Promise<PublicationOutcome> {
  return send(
    publicationPath(draftId),
    {
      method: "POST",
      headers: authoringHeaders({ "Content-Type": "application/json" }),
      body: publishRequestBody(expectedRevision, expectedSelectionVersion),
      cache: "no-store",
    },
    draftId,
    signal,
    true,
  );
}

/** Withdraws the current selection of one owned draft. */
export function withdrawAuthoringPublication(
  draftId: string,
  expectedSelectionVersion: number,
  signal?: AbortSignal,
): Promise<PublicationOutcome> {
  return send(
    publicationWithdrawPath(draftId),
    {
      method: "POST",
      headers: authoringHeaders({ "Content-Type": "application/json" }),
      body: withdrawRequestBody(expectedSelectionVersion),
      cache: "no-store",
    },
    draftId,
    signal,
    true,
  );
}
