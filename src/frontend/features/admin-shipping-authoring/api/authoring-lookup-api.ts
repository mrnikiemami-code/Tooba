/**
 * Authorized lookups needed by the general editor slice, through the SAME feature transport.
 *
 * Two reads only:
 * 1. the owner's existing authoring drafts, so a legacy flat method can be matched to the draft that
 *    already owns its exact server-resolved `(serviceCode, optionCode)` binding — never a text split,
 *    never an automatic create;
 * 2. the real active shipping-type catalog (`authoring-types`) used by the type selector.
 *
 * Both reads are strictly decoded: a malformed payload is `untrusted`, a real 0/1/many match is the
 * caller's decision, and no value is invented from a partial body.
 */
import type { EditorTypeTranslation } from "../model/editor-document.ts";
import {
  authoringHeaders,
  editorTransportFailureFor,
  parseResponseRoot,
  type EditorTransportFailure,
} from "./editor-document-api.ts";
import { isIntegerText, type JsonNode } from "./wire-json.ts";

/** One existing draft's binding identity, as served by the authorized list route. */
export interface AuthoringDraftBindingRow {
  readonly draftId: string;
  readonly serviceCode: string;
  readonly optionCode: string | null;
}

/** One real active shipping type of `authoring-types`, with its raw stored translations. */
export interface AuthoringTypeChoice {
  readonly shippingTypeId: string;
  readonly code: string;
  readonly sortOrder: number;
  readonly revision: number;
  readonly translations: readonly EditorTypeTranslation[];
}

/** Outcome of one authorized read. */
export type LookupOutcome<T> =
  | { readonly state: "ok"; readonly value: T }
  | { readonly state: "failure"; readonly failure: EditorTransportFailure };

/** Exact authorized list route of the owner's drafts. */
export const AUTHORING_DRAFTS_PATH = "/v1/admin/shipping-services/authoring-drafts";
/** Exact authorized catalog route of the active shipping types. */
export const AUTHORING_TYPES_PATH = "/v1/admin/shipping-services/authoring-types";

const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const ZERO_GUID = "00000000-0000-0000-0000-000000000000";

class LookupDecodeFailure extends Error {}

function object(node: JsonNode | undefined): ReadonlyMap<string, JsonNode> {
  if (!node || node.kind !== "object") throw new LookupDecodeFailure("object");
  return node.fields;
}

function array(node: JsonNode | undefined): readonly JsonNode[] {
  if (!node || node.kind !== "array") throw new LookupDecodeFailure("array");
  return node.items;
}

function text(node: JsonNode | undefined): string {
  if (!node || node.kind !== "string") throw new LookupDecodeFailure("string");
  return node.value;
}

function guid(node: JsonNode | undefined): string {
  const value = text(node);
  if (!GUID.test(value) || value.toLowerCase() === ZERO_GUID) throw new LookupDecodeFailure("guid");
  return value;
}

function required(node: ReadonlyMap<string, JsonNode>, key: string): JsonNode {
  const value = node.get(key);
  if (value === undefined) throw new LookupDecodeFailure(`missing:${key}`);
  return value;
}

function optionalText(node: JsonNode | undefined): string | null {
  if (node === undefined || node.kind === "null") return null;
  return text(node);
}

/** Strict decode of the authorized draft list into binding rows only. */
export function decodeAuthoringDraftRows(node: JsonNode): readonly AuthoringDraftBindingRow[] {
  const root = object(node);
  return array(required(root, "drafts")).map((entry) => {
    const item = object(entry);
    return {
      draftId: guid(required(item, "draftId")),
      serviceCode: text(required(item, "serviceCode")),
      optionCode: optionalText(item.get("optionCode")),
    };
  });
}

/** Strict decode of the authorized active-type catalog. */
export function decodeAuthoringTypeChoices(node: JsonNode): readonly AuthoringTypeChoice[] {
  const root = object(node);
  return array(required(root, "types")).map((entry) => {
    const item = object(entry);
    const sortOrder = required(item, "sortOrder");
    const revision = required(item, "revision");
    if (sortOrder.kind !== "number" || !isIntegerText(sortOrder.raw)) throw new LookupDecodeFailure("sortOrder");
    if (revision.kind !== "number" || !isIntegerText(revision.raw)) throw new LookupDecodeFailure("revision");
    return {
      shippingTypeId: guid(required(item, "shippingTypeId")),
      code: text(required(item, "code")),
      sortOrder: Number(sortOrder.raw),
      revision: Number(revision.raw),
      translations: array(required(item, "translations")).map((translation) => {
        const fields = object(translation);
        return {
          languageId: guid(required(fields, "languageId")),
          name: text(required(fields, "name")),
          description: optionalText(fields.get("description")),
        };
      }),
    };
  });
}

/** Exact binding match: `serviceCode` equal and `optionCode` identity (null only matches null). */
export function draftsMatchingBinding(
  rows: readonly AuthoringDraftBindingRow[],
  serviceCode: string,
  optionCode: string | null,
): readonly AuthoringDraftBindingRow[] {
  return rows.filter(
    (row) =>
      row.serviceCode === serviceCode &&
      (row.optionCode === null ? optionCode === null : optionCode !== null && row.optionCode === optionCode),
  );
}

/** Honest binding-to-draft entry resolution of one legacy flat method. */
export type DraftEntryResolution =
  | { readonly state: "matched"; readonly draftId: string }
  | { readonly state: "none" }
  | { readonly state: "ambiguous"; readonly count: number };

/**
 * Resolves the ONE draft of an exact binding.
 *
 * @param rows Owner drafts read from the authorized list.
 * @param serviceCode Server-resolved binding service code.
 * @param optionCode Server-resolved optional binding option code.
 */
export function resolveDraftEntry(
  rows: readonly AuthoringDraftBindingRow[],
  serviceCode: string,
  optionCode: string | null,
): DraftEntryResolution {
  const matches = draftsMatchingBinding(rows, serviceCode, optionCode);
  if (matches.length === 0) return { state: "none" };
  if (matches.length > 1) return { state: "ambiguous", count: matches.length };
  return { state: "matched", draftId: matches[0]!.draftId };
}

async function readJson(path: string, signal: AbortSignal | undefined): Promise<
  { readonly response: Response; readonly root: JsonNode | null }
> {
  const response = await fetch(path, { method: "GET", headers: authoringHeaders(), signal, cache: "no-store" });
  const text = await response.text().then(
    (value) => value,
    () => null,
  );
  return { response, root: parseResponseRoot(text) };
}

async function read<T>(
  path: string,
  decode: (node: JsonNode) => T,
  signal: AbortSignal | undefined,
): Promise<LookupOutcome<T>> {
  let read1: { readonly response: Response; readonly root: JsonNode | null };
  try {
    read1 = await readJson(path, signal);
  } catch {
    return { state: "failure", failure: { kind: "network", aborted: signal?.aborted === true } };
  }
  if (!read1.response.ok) {
    return { state: "failure", failure: editorTransportFailureFor(read1.response.status, read1.root) };
  }
  if (read1.root === null) {
    return { state: "failure", failure: { kind: "untrusted", status: read1.response.status, reason: "body:unreadable" } };
  }
  try {
    return { state: "ok", value: decode(read1.root) };
  } catch (error) {
    if (error instanceof LookupDecodeFailure) {
      return {
        state: "failure",
        failure: { kind: "untrusted", status: read1.response.status, reason: error.message },
      };
    }
    throw error;
  }
}

/** Reads the owner's existing authoring drafts (authorized admin route). */
export function loadAuthoringDraftRows(signal?: AbortSignal): Promise<LookupOutcome<readonly AuthoringDraftBindingRow[]>> {
  return read(AUTHORING_DRAFTS_PATH, decodeAuthoringDraftRows, signal);
}

/** Reads the real active shipping-type catalog (authorized admin route). */
export function loadAuthoringTypeChoices(signal?: AbortSignal): Promise<LookupOutcome<readonly AuthoringTypeChoice[]>> {
  return read(AUTHORING_TYPES_PATH, decodeAuthoringTypeChoices, signal);
}
