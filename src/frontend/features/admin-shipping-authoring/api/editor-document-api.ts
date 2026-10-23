/**
 * Transport boundary for the accepted atomic editor document API.
 *
 * Reads and writes exactly the existing authorized admin routes through the current same-origin
 * `/v1` upstream convention and the shared admin header primitives. The browser never supplies
 * owner/tenant/seller/edition/currency authority, and no second auth client is introduced.
 *
 * Failure outcomes keep the HTTP status and the canonical root/child codes for the later localized
 * presenter. A PUT whose commit cannot be established (network, abort, unreadable success) becomes
 * `outcomeUnknown` and MUST NOT be retried with a new revision by the caller.
 */
import { ADMIN_DEV_ACTOR_HEADER, ADMIN_ACTOR_STORAGE_KEY, DEFAULT_ADMIN_ACTOR_ID, adminRequestHeaders } from "../../../lib/admin/admin-result.ts";
import type { EditorDocument } from "../model/editor-document.ts";
import { decodeEditorDocument } from "./editor-document-decode.ts";
import { parseJsonNode, type JsonNode } from "./wire-json.ts";

/** One canonical property/child validation error group. */
export interface EditorPropertyError {
  readonly property: string;
  readonly codes: readonly string[];
}

/** Typed failure of the editor document transport. */
export type EditorTransportFailure =
  | { readonly kind: "denied"; readonly status: number; readonly errorCode: string | null; readonly title: string | null }
  | { readonly kind: "notFound"; readonly status: 404; readonly errorCode: string | null; readonly title: string | null }
  | {
      readonly kind: "conflict";
      readonly status: 409;
      readonly errorCode: string | null;
      readonly title: string | null;
      readonly errors: readonly EditorPropertyError[];
    }
  | {
      readonly kind: "validation";
      readonly status: number;
      readonly errorCode: string | null;
      readonly title: string | null;
      readonly errors: readonly EditorPropertyError[];
    }
  | { readonly kind: "unavailable"; readonly status: 503; readonly errorCode: string | null; readonly title: string | null }
  | {
      readonly kind: "httpError";
      readonly status: number;
      readonly errorCode: string | null;
      readonly title: string | null;
      readonly errors: readonly EditorPropertyError[];
    }
  | { readonly kind: "untrusted"; readonly status: number; readonly reason: string }
  | { readonly kind: "network"; readonly aborted: boolean }
  | { readonly kind: "outcomeUnknown"; readonly reason: "network" | "aborted" | "unreadable" };

/** Result of one editor document call. */
export type EditorOutcome =
  | { readonly kind: "ok"; readonly document: EditorDocument }
  | { readonly kind: "failure"; readonly failure: EditorTransportFailure };

function actorId(): string {
  if (typeof window === "undefined") return DEFAULT_ADMIN_ACTOR_ID;
  return window.localStorage.getItem(ADMIN_ACTOR_STORAGE_KEY) ?? DEFAULT_ADMIN_ACTOR_ID;
}

/**
 * The ONE set of admin headers every authoring read/write in this feature uses (no second auth client).
 *
 * @param extra Additional request headers (for example a JSON content type).
 */
export function authoringHeaders(extra?: Record<string, string>): Record<string, string> {
  return { ...adminRequestHeaders(), [ADMIN_DEV_ACTOR_HEADER]: actorId(), ...(extra ?? {}) };
}

function headers(extra?: Record<string, string>): Record<string, string> {
  return authoringHeaders(extra);
}

/** Exact authorized route of ONE existing draft's editor document. */
export function editorDocumentPath(draftId: string): string {
  return `/v1/admin/shipping-services/authoring-drafts/${encodeURIComponent(draftId)}/editor-document`;
}

function field(node: ReadonlyMap<string, JsonNode>, key: string): JsonNode | undefined {
  return node.get(key);
}

function problemTitle(root: JsonNode): string | null {
  if (root.kind !== "object") return null;
  const title = field(root.fields, "title");
  return title && title.kind === "string" ? title.value : null;
}

function problemCode(root: JsonNode): string | null {
  if (root.kind !== "object") return null;
  const code = field(root.fields, "errorCode");
  return code && code.kind === "string" ? code.value : null;
}

function propertyErrors(root: JsonNode): readonly EditorPropertyError[] {
  if (root.kind !== "object") return [];
  const errors = field(root.fields, "errors");
  if (!errors || errors.kind !== "object") return [];
  const groups: EditorPropertyError[] = [];
  for (const [property, codes] of errors.fields) {
    if (codes.kind !== "array") return [];
    const values: string[] = [];
    for (const code of codes.items) {
      if (code.kind !== "string") return [];
      values.push(code.value);
    }
    groups.push({ property, codes: values });
  }
  return groups;
}

function failureFor(status: number, root: JsonNode | null): EditorTransportFailure {
  const code = root ? problemCode(root) : null;
  const title = root ? problemTitle(root) : null;
  const errors = root ? propertyErrors(root) : [];
  if (status === 401 || status === 403) return { kind: "denied", status, errorCode: code, title };
  if (status === 404) return { kind: "notFound", status: 404, errorCode: code, title };
  if (status === 409) return { kind: "conflict", status: 409, errorCode: code, title, errors };
  if (status === 400 || status === 422) return { kind: "validation", status, errorCode: code, title, errors };
  if (status === 503) return { kind: "unavailable", status: 503, errorCode: code, title };
  return { kind: "httpError", status, errorCode: code, title, errors };
}

function readBody(response: Response): Promise<string | null> {
  return response.text().then(
    (text) => text,
    () => null,
  );
}

function documentFrom(text: string, status: number, draftId: string): EditorOutcome {
  const root = parseJsonNode(text);
  if (!root || root.kind !== "object") return { kind: "failure", failure: { kind: "untrusted", status, reason: "root:object" } };
  const decoded = decodeEditorDocument(root, draftId);
  return decoded.ok
    ? { kind: "ok", document: decoded.value }
    : { kind: "failure", failure: { kind: "untrusted", status, reason: decoded.reason } };
}

async function send(
  draftId: string,
  init: RequestInit,
  signal: AbortSignal | undefined,
  onUnknown: boolean,
): Promise<EditorOutcome> {
  let response: Response;
  try {
    response = await fetch(editorDocumentPath(draftId), { ...init, signal });
  } catch {
    const aborted = signal?.aborted === true;
    return {
      kind: "failure",
      failure: onUnknown ? { kind: "outcomeUnknown", reason: aborted ? "aborted" : "network" } : { kind: "network", aborted },
    };
  }

  const text = await readBody(response);
  if (!response.ok) {
    const root = text === null ? null : parseJsonNode(text);
    return { kind: "failure", failure: failureFor(response.status, root) };
  }
  if (text === null) {
    return {
      kind: "failure",
      failure: onUnknown
        ? { kind: "outcomeUnknown", reason: "unreadable" }
        : { kind: "untrusted", status: response.status, reason: "body:unreadable" },
    };
  }
  const decoded = documentFrom(text, response.status, draftId);
  if (decoded.kind === "failure" && onUnknown) {
    return { kind: "failure", failure: { kind: "outcomeUnknown", reason: "unreadable" } };
  }
  return decoded;
}

/**
 * Reads the composite editor document of one existing admin draft.
 * @param draftId Draft id from the route.
 * @param signal Optional caller cancellation.
 * @returns Typed document or typed failure (a cancelled/failed GET is never reported as not-found).
 */
export function loadEditorDocument(draftId: string, signal?: AbortSignal): Promise<EditorOutcome> {
  return send(draftId, { method: "GET", headers: headers() }, signal, false);
}

/**
 * Sends the ONE atomic editor-document replace for this draft.
 *
 * @param draftId Draft id from the route.
 * @param bodyText Exact request body produced by the builder.
 * @param signal Optional caller cancellation.
 * @returns The confirmed server document or a typed failure; never a synthetic success.
 */
export function saveEditorDocument(draftId: string, bodyText: string, signal?: AbortSignal): Promise<EditorOutcome> {
  return send(
    draftId,
    { method: "PUT", headers: headers({ "Content-Type": "application/json" }), body: bodyText },
    signal,
    true,
  );
}

/** Re-exported canonical failure mapping for the feature's other authorized reads (no second client). */
export { failureFor as editorTransportFailureFor };

/**
 * Parses one response body into a JSON node, keeping `null` for an unreadable/empty body.
 *
 * @param text Exact response text.
 */
export function parseResponseRoot(text: string | null): JsonNode | null {
  return text === null ? null : parseJsonNode(text);
}
