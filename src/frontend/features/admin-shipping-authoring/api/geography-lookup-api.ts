/**
 * Feature-owned STRICT read adapter for the EXISTING Geography admin routes.
 *
 * The legacy app picker is not reused: its decoder filters rows, falls back to `code` as a name and
 * does not verify parents, so a malformed/foreign-parent response would silently become a smaller
 * "successful" list. Here a non-array payload, a missing/blank/non-GUID identity, a duplicate
 * identity, a wrong parent for the requested branch or a bad `isActive` flag is a TYPED failure.
 * A legitimately empty catalog stays `ok` with no options.
 *
 * Identity is always `(kind, id)`; labels are display-only and may legitimately be empty/missing (the
 * UI then shows an unavailable state rather than inventing a name from the code).
 */
import { authoringHeaders } from "./editor-document-api.ts";
import type { AuthoringAreaKind } from "../model/editor-document.ts";

/** One verified option row of the real `GeographyPlaceOption` wire shape. */
export interface GeographyOption {
  readonly kind: AuthoringAreaKind;
  readonly id: string;
  readonly code: string;
  readonly label: string;
  readonly parentId: string | null;
  readonly isActive: boolean;
}

/** Stable typed failure of one Geography read (never an empty success). */
export type GeographyReadFailure =
  | { readonly kind: "denied" }
  | { readonly kind: "untrusted" }
  | { readonly kind: "unavailable" }
  | { readonly kind: "network" };

export type GeographyReadOutcome =
  | { readonly state: "ok"; readonly options: readonly GeographyOption[] }
  | { readonly state: "failure"; readonly failure: GeographyReadFailure };

/** What the operator is currently opening (one branch only, never a world scan). */
export interface GeographyQuery {
  readonly kind: AuthoringAreaKind;
  readonly languageId: string;
  readonly countryId?: string;
  readonly administrativeAreaId?: string | null;
  readonly cityId?: string;
  /** Country-wide city reads: the returned non-null parent must be one of these verified areas. */
  readonly knownAdministrativeAreaIds?: ReadonlySet<string>;
}

const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
const ZERO = "00000000-0000-0000-0000-000000000000";

function isId(value: unknown): value is string {
  return typeof value === "string" && GUID.test(value) && value.toLowerCase() !== ZERO;
}

/** Parent rule of each level against the requested branch (wrong parent is malformed, not filtered). */
function parentAccepted(kind: AuthoringAreaKind, parentId: string | null, query: GeographyQuery): boolean {
  switch (kind) {
    case "Country":
      return parentId === null;
    case "AdministrativeArea":
      return query.countryId !== undefined && parentId !== null && parentId.toLowerCase() === query.countryId.toLowerCase();
    case "City": {
      if (query.countryId === undefined) return false;
      if (query.administrativeAreaId) {
        // Explicit area filter: exactly that parent, and never a null-parent contradiction.
        return parentId !== null && parentId.toLowerCase() === query.administrativeAreaId.toLowerCase();
      }
      // Country-wide: absent parent evidence is NOT evidence (a verified empty set is).
      if (!query.knownAdministrativeAreaIds) return false;
      if (parentId === null) return true;
      return query.knownAdministrativeAreaIds.has(parentId.toLowerCase());
    }
    case "ServiceArea":
      return query.cityId !== undefined && parentId !== null && parentId.toLowerCase() === query.cityId.toLowerCase();
  }
}

/**
 * Strict decode of one Geography read.
 *
 * @param query The branch that was actually requested.
 * @param payload The parsed JSON body of the response.
 */
export function decodeGeographyOptions(query: GeographyQuery, payload: unknown): GeographyReadOutcome {
  if (!Array.isArray(payload)) return { state: "failure", failure: { kind: "untrusted" } };
  // Parent evidence is compared against normalized ids regardless of the caller's casing.
  const evidence = query.knownAdministrativeAreaIds
    ? { ...query, knownAdministrativeAreaIds: new Set([...query.knownAdministrativeAreaIds].map((id) => id.toLowerCase())) }
    : query;
  const options: GeographyOption[] = [];
  const seen = new Set<string>();
  for (const raw of payload) {
    if (!raw || typeof raw !== "object") return { state: "failure", failure: { kind: "untrusted" } };
    const row = raw as Record<string, unknown>;
    const { id, parentId, code } = row;
    if (!isId(id)) return { state: "failure", failure: { kind: "untrusted" } };
    if (typeof code !== "string" || code.trim().length === 0) return { state: "failure", failure: { kind: "untrusted" } };
    if (parentId !== null && parentId !== undefined && !isId(parentId)) {
      return { state: "failure", failure: { kind: "untrusted" } };
    }
    const parent = parentId === null || parentId === undefined ? null : (parentId as string);
    if (!parentAccepted(query.kind, parent, evidence)) return { state: "failure", failure: { kind: "untrusted" } };
    if (typeof row.isActive !== "boolean") return { state: "failure", failure: { kind: "untrusted" } };
    const key = id.toLowerCase();
    if (seen.has(key)) return { state: "failure", failure: { kind: "untrusted" } };
    seen.add(key);
    options.push({
      kind: query.kind,
      id,
      code,
      label: typeof row.label === "string" ? row.label : "",
      parentId: parent,
      isActive: row.isActive,
    });
  }
  return { state: "ok", options };
}

/** Exact existing route of the requested branch (no new endpoint, no query invention). */
export function geographyQueryPath(query: GeographyQuery): string | null {
  const language = `languageId=${encodeURIComponent(query.languageId)}`;
  switch (query.kind) {
    case "Country":
      return `/v1/admin/geography/countries?${language}`;
    case "AdministrativeArea":
      return query.countryId
        ? `/v1/admin/geography/countries/${encodeURIComponent(query.countryId)}/administrative-areas?${language}`
        : null;
    case "City": {
      if (!query.countryId) return null;
      const params = new URLSearchParams({ languageId: query.languageId });
      if (query.administrativeAreaId) params.set("administrativeAreaId", query.administrativeAreaId);
      return `/v1/admin/geography/countries/${encodeURIComponent(query.countryId)}/cities?${params.toString()}`;
    }
    case "ServiceArea":
      return query.cityId
        ? `/v1/admin/geography/cities/${encodeURIComponent(query.cityId)}/service-areas?${language}`
        : null;
  }
}

/** Reads ONE branch with the current admin headers and strict decoding. */
export async function loadGeographyOptions(
  query: GeographyQuery,
  signal?: AbortSignal,
): Promise<GeographyReadOutcome> {
  const path = geographyQueryPath(query);
  if (path === null) return { state: "failure", failure: { kind: "untrusted" } };
  let response: Response;
  try {
    response = await fetch(path, { headers: authoringHeaders(), signal });
  } catch {
    return { state: "failure", failure: { kind: "network" } };
  }
  if (response.status === 401 || response.status === 403) return { state: "failure", failure: { kind: "denied" } };
  if (!response.ok) return { state: "failure", failure: { kind: "unavailable" } };
  const payload: unknown = await response.json().catch(() => null);
  return decodeGeographyOptions(query, payload);
}
