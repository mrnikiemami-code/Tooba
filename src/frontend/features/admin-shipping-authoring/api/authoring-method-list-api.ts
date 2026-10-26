/**
 * Authorized reads behind the PRIMARY shipping-method list.
 *
 * Exactly two reads, both through the SAME feature transport as the editor:
 * 1. the owner's existing authoring drafts — the real authority for configured methods (a draft's own
 *    `(serviceCode, optionCode)` binding, its saved revision, its area count and its stored texts);
 * 2. the existing authoring context — the authoritative default content language, so the display name
 *    is the text stored for THAT language and never a `fa`/`en` guess or a locale mapping.
 *
 * No per-row editor/presentation/publication read: everything shown per row arrives in the one list
 * payload. A malformed payload is `untrusted` (never a partial or invented row).
 */
import {
  authoringHeaders,
  editorTransportFailureFor,
  parseResponseRoot,
  type EditorTransportFailure,
} from "./editor-document-api.ts";
import { isIntegerText, type JsonNode } from "./wire-json.ts";

/** One stored name/description text of a draft for one content language. */
export interface AuthoringMethodTranslationRow {
  readonly languageId: string;
  readonly name: string;
  readonly description: string | null;
}

/** One existing draft as a method-row of the primary list. Stable identity is the SERVER draft id. */
export interface AuthoringMethodRow {
  readonly id: string;
  readonly draftId: string;
  readonly serviceCode: string;
  readonly optionCode: string | null;
  readonly revision: number;
  readonly areaCount: number;
  readonly translations: readonly AuthoringMethodTranslationRow[];
}

/** One active content language of the authoring context. */
export interface AuthoringContextLanguage {
  readonly languageId: string;
  readonly code: string;
  readonly displayName: string;
  readonly nativeName: string;
  readonly isDefault: boolean;
}

/** The authoritative authoring context of this owner. */
export interface AuthoringContextRead {
  readonly defaultContentLanguageId: string;
  readonly contentLanguages: readonly AuthoringContextLanguage[];
}

/** Outcome of one authorized read. */
export type AuthoringListOutcome<T> =
  | { readonly state: "ok"; readonly value: T }
  | { readonly state: "failure"; readonly failure: EditorTransportFailure };

/** Exact authorized list route of the owner's drafts (same route the editor entry already uses). */
const AUTHORING_DRAFTS_PATH = "/v1/admin/shipping-services/authoring-drafts";
/** Exact authorized authoring-context route (default content language registry). */
const AUTHORING_CONTEXT_PATH = "/v1/admin/shipping-services/authoring-context";

const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const ZERO_GUID = "00000000-0000-0000-0000-000000000000";

class MethodListDecodeFailure extends Error {}

function object(node: JsonNode | undefined): ReadonlyMap<string, JsonNode> {
  if (!node || node.kind !== "object") throw new MethodListDecodeFailure("object");
  return node.fields;
}

function array(node: JsonNode | undefined): readonly JsonNode[] {
  if (!node || node.kind !== "array") throw new MethodListDecodeFailure("array");
  return node.items;
}

function text(node: JsonNode | undefined): string {
  if (!node || node.kind !== "string") throw new MethodListDecodeFailure("string");
  return node.value;
}

function guid(node: JsonNode | undefined): string {
  const value = text(node);
  if (!GUID.test(value) || value.toLowerCase() === ZERO_GUID) throw new MethodListDecodeFailure("guid");
  return value;
}

function optionalText(node: JsonNode | undefined): string | null {
  if (node === undefined || node.kind === "null") return null;
  return text(node);
}

function required(node: ReadonlyMap<string, JsonNode>, key: string): JsonNode {
  const value = node.get(key);
  if (value === undefined) throw new MethodListDecodeFailure(`missing:${key}`);
  return value;
}

/** Exact integer token text; a value above the safe-integer range is refused, never rounded. */
function token(node: JsonNode | undefined): number {
  if (!node || node.kind !== "number" || !isIntegerText(node.raw)) throw new MethodListDecodeFailure("token");
  const value = Number(node.raw);
  if (!Number.isSafeInteger(value) || String(value) !== node.raw) throw new MethodListDecodeFailure("token");
  return value;
}

/**
 * Strict decode of the authorized draft list into list rows.
 *
 * @param node Parsed response root.
 * @returns Rows: one per stored draft, in server order.
 */
export function decodeAuthoringMethodRows(node: JsonNode): readonly AuthoringMethodRow[] {
  const root = object(node);
  const seen = new Set<string>();
  return array(required(root, "drafts")).map((entry) => {
    const item = object(entry);
    const draftId = guid(required(item, "draftId"));
    if (seen.has(draftId.toLowerCase())) throw new MethodListDecodeFailure("duplicate:draftId");
    seen.add(draftId.toLowerCase());

    const revision = token(required(item, "revision"));
    if (revision < 0) throw new MethodListDecodeFailure("revision");
    const groups = array(required(item, "regionGroups"));
    // `methodTranslations` is an additive field: an absent array is an honest "no stored text in this
    // payload" for an older server, never an invented translation.
    const translations = item.get("methodTranslations");

    return {
      id: draftId,
      draftId,
      serviceCode: text(required(item, "serviceCode")),
      optionCode: optionalText(item.get("optionCode")),
      revision,
      areaCount: groups.length,
      translations:
        translations === undefined || translations.kind === "null"
          ? []
          : array(translations).map((translation) => {
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

/**
 * Strict decode of the authoring context (default content language + its active languages).
 *
 * @param node Parsed response root.
 * @returns The context: the default language always belongs to the active languages it lists.
 */
export function decodeAuthoringContext(node: JsonNode): AuthoringContextRead {
  const root = object(node);
  const defaultContentLanguageId = guid(required(root, "defaultContentLanguageId"));
  const contentLanguages = array(required(root, "contentLanguages")).map((entry) => {
    const fields = object(entry);
    const isDefault = required(fields, "isDefault");
    if (isDefault.kind !== "boolean") throw new MethodListDecodeFailure("isDefault");
    return {
      languageId: guid(required(fields, "languageId")),
      code: text(required(fields, "code")),
      displayName: text(required(fields, "displayName")),
      nativeName: text(required(fields, "nativeName")),
      isDefault: isDefault.value,
    };
  });
  const lower = defaultContentLanguageId.toLowerCase();
  if (!contentLanguages.some((language) => language.languageId.toLowerCase() === lower)) {
    throw new MethodListDecodeFailure("default-unresolved");
  }
  return { defaultContentLanguageId, contentLanguages };
}

/**
 * Display name of one row for the authoritative default content language.
 *
 * @param row List row with its stored texts.
 * @param defaultContentLanguageId Default content language of the authoring context.
 * @returns The stored name, or `null` when that language has no stored name (honest "no name").
 */
export function storedMethodName(
  row: AuthoringMethodRow,
  defaultContentLanguageId: string,
): string | null {
  const lower = defaultContentLanguageId.toLowerCase();
  const match = row.translations.find((translation) => translation.languageId.toLowerCase() === lower);
  return match ? match.name : null;
}

/**
 * Canonical binding text of a row: `serviceCode` or `serviceCode:optionCode` exactly as stored.
 *
 * @param row List row.
 */
export function methodBinding(row: AuthoringMethodRow): string {
  return row.optionCode === null ? row.serviceCode : `${row.serviceCode}:${row.optionCode}`;
}

/**
 * Duplicate-binding detection across the owner's rows.
 *
 * @param rows List rows.
 * @returns Draft ids whose exact binding appears more than once (ambiguous for a customer-facing method).
 */
export function duplicateBindingDraftIds(rows: readonly AuthoringMethodRow[]): ReadonlySet<string> {
  const counts = new Map<string, number>();
  for (const row of rows) {
    const key = methodBinding(row).toLowerCase();
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  const duplicates = new Set<string>();
  for (const row of rows) {
    if ((counts.get(methodBinding(row).toLowerCase()) ?? 0) > 1) duplicates.add(row.draftId);
  }
  return duplicates;
}

/** The single server-authorized authoring owner of THIS request (never a client-chosen owner). */
export interface AuthoringOwnerRead {
  readonly ownerKind: "Store" | "Marketplace" | "Seller";
  /** Business seller identity of the authorized scope; only for `Seller`, never a user account. */
  readonly sellerPartyId: string | null;
}

/** Exact authorized authoring-owner route (server resolves the scope after authorization). */
const AUTHORING_OWNER_PATH = "/v1/admin/shipping-services/authoring-owner";

/**
 * Strict decode of the authorized owner. The route accepts no owner/seller parameter, so this is the
 * only owner the caller may see; a seller id is required for `Seller` and forbidden otherwise.
 *
 * @param node Parsed response root.
 */
export function decodeAuthoringOwner(node: JsonNode): AuthoringOwnerRead {
  const root = object(node);
  const ownerKind = text(required(root, "ownerKind"));
  if (ownerKind !== "Store" && ownerKind !== "Marketplace" && ownerKind !== "Seller") {
    throw new MethodListDecodeFailure("ownerKind");
  }
  const sellerPartyId = optionalText(root.get("sellerPartyId"));
  if (ownerKind === "Seller") {
    if (sellerPartyId === null) throw new MethodListDecodeFailure("sellerPartyId:missing");
    if (!GUID.test(sellerPartyId) || sellerPartyId.toLowerCase() === ZERO_GUID) {
      throw new MethodListDecodeFailure("sellerPartyId:invalid");
    }
  } else if (sellerPartyId !== null) {
    throw new MethodListDecodeFailure("sellerPartyId:unexpected");
  }
  return { ownerKind, sellerPartyId };
}

/** Stable key of an authorized owner, used to isolate grid state/saved views per scope. */
export function authoringOwnerKey(owner: AuthoringOwnerRead): string {
  return owner.ownerKind === "Seller"
    ? `seller:${(owner.sellerPartyId ?? "").toLowerCase()}`
    : owner.ownerKind.toLowerCase();
}

/**
 * Reads the server-authorized authoring owner of this request.
 *
 * @param signal Abort signal of the caller (a superseded read is aborted, never merged).
 */
export function loadAuthoringOwner(signal?: AbortSignal): Promise<AuthoringListOutcome<AuthoringOwnerRead>> {
  return read(AUTHORING_OWNER_PATH, decodeAuthoringOwner, signal);
}

async function readJson(
  path: string,
  signal: AbortSignal | undefined,
): Promise<{ readonly response: Response; readonly root: JsonNode | null }> {
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
): Promise<AuthoringListOutcome<T>> {
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
    if (error instanceof MethodListDecodeFailure) {
      return {
        state: "failure",
        failure: { kind: "untrusted", status: read1.response.status, reason: error.message },
      };
    }
    throw error;
  }
}

/** Reads the owner's existing authoring drafts for the primary list. */
export function loadAuthoringMethodRows(
  signal?: AbortSignal,
): Promise<AuthoringListOutcome<readonly AuthoringMethodRow[]>> {
  return read(AUTHORING_DRAFTS_PATH, decodeAuthoringMethodRows, signal);
}

/** Reads the authoritative authoring context (default content language). */
export function loadAuthoringContext(signal?: AbortSignal): Promise<AuthoringListOutcome<AuthoringContextRead>> {
  return read(AUTHORING_CONTEXT_PATH, decodeAuthoringContext, signal);
}
