/**
 * Display PATH of ALREADY STORED destination selections.
 *
 * The editor document proves each stored selection's identity and ancestry ids, but it carries only the
 * terminal's localized label — so after a reload a chip would lose its breadcrumb even though the real
 * chain is known. This module reads the ANCESTOR labels from the EXISTING Geography admin routes, in
 * bounded SET-based reads (one per distinct parent, never one per chip).
 *
 * Nothing is invented: the terminal name always comes from the stored fact, and when a required ancestor
 * label was not proven no path is published at all — the chip then keeps its honest leaf-only name.
 * Labels are display-only; identity/ancestry still come from the stored fact and are never pricing truth.
 */
import type { GeographyQuery, GeographyReadOutcome } from "../api/geography-lookup-api.ts";
import type { AuthoringAreaKind, EditorDestinationFact } from "./editor-document.ts";

/** One stored selection whose ancestor names are worth resolving. */
export interface StoredTrailChain {
  /** Stable `kind:id` identity of the selection (same key the fact index uses). */
  readonly key: string;
  readonly kind: AuthoringAreaKind;
  /** Proven terminal name of this exact selection. */
  readonly label: string | null;
  readonly countryId: string | null;
  readonly administrativeAreaId: string | null;
  readonly cityId: string | null;
  readonly serviceAreaId: string | null;
}

/** One strict Geography branch read (injected so the orchestration stays testable). */
export type TrailBranchReader = (query: GeographyQuery) => Promise<GeographyReadOutcome>;

function factKey(fact: EditorDestinationFact): string {
  return `${fact.kind}:${fact.id.toLowerCase()}`;
}

/**
 * Chains of the document's RESOLVED stored selections.
 *
 * Unresolved/foreign selections are skipped: their ancestry was never proven, so no path may be drawn.
 *
 * @param facts Validated destination facts of the current read snapshot.
 */
export function storedTrailChains(facts: readonly EditorDestinationFact[]): readonly StoredTrailChain[] {
  const chains: StoredTrailChain[] = [];
  for (const fact of facts) {
    const area = fact.area;
    if (fact.resolution !== "Resolved" || area === null) continue;
    chains.push({
      key: factKey(fact),
      kind: fact.kind,
      label: fact.label,
      countryId: area.countryId,
      administrativeAreaId: area.administrativeAreaId,
      cityId: area.cityId,
      serviceAreaId: area.serviceAreaId,
    });
  }
  return chains;
}

function lower(value: string | null): string | null {
  return value === null ? null : value.toLowerCase();
}

/** Reads ONE branch and merges its verified labels into the map (a failed read merges nothing). */
async function readInto(
  read: TrailBranchReader,
  query: GeographyQuery,
  target: Map<string, string>,
  signal?: AbortSignal,
): Promise<void> {
  if (signal?.aborted) return;
  const outcome = await read(query);
  if (outcome.state !== "ok") return;
  for (const option of outcome.options) {
    const label = option.label.trim();
    if (label.length === 0) continue;
    const key = option.id.toLowerCase();
    if (!target.has(key)) target.set(key, label);
  }
}

/** Levels below the terminal that a path must prove before it may be shown. */
function needsAdministrativeArea(kind: AuthoringAreaKind): boolean {
  return kind === "City" || kind === "ServiceArea";
}

/**
 * Resolves the display path of every stored chain, root first.
 *
 * @param chains Resolved stored selections.
 * @param languageId Language the labels must be read in (the document's default content language).
 * @param read Strict branch reader (the existing Geography adapter).
 * @param signal Aborts the whole resolution when the document is replaced or the screen unmounts.
 */
export async function readStoredTrailSegments(
  chains: readonly StoredTrailChain[],
  languageId: string,
  read: TrailBranchReader,
  signal?: AbortSignal,
): Promise<ReadonlyMap<string, readonly string[]>> {
  const trails = new Map<string, readonly string[]>();
  if (chains.length === 0 || languageId.trim().length === 0) return trails;

  const countries = new Map<string, string>();
  const administrativeAreas = new Map<string, string>();
  const cities = new Map<string, string>();
  const countryIds = new Set<string>();
  const adminByCountry = new Map<string, Set<string>>();
  const cityByAdmin = new Map<string, Set<string>>();
  for (const chain of chains) {
    const country = lower(chain.countryId);
    const admin = lower(chain.administrativeAreaId);
    const city = lower(chain.cityId);
    if (country !== null) countryIds.add(country);
    if (country !== null && admin !== null && needsAdministrativeArea(chain.kind)) {
      const set = adminByCountry.get(country) ?? new Set<string>();
      set.add(admin);
      adminByCountry.set(country, set);
    }
    if (admin !== null && city !== null && chain.kind === "ServiceArea") {
      const set = cityByAdmin.get(admin) ?? new Set<string>();
      set.add(city);
      cityByAdmin.set(admin, set);
    }
  }

  if (countryIds.size > 0) {
    await readInto(read, { kind: "Country", languageId }, countries, signal);
  }
  for (const [countryId, admins] of adminByCountry) {
    if (signal?.aborted) return trails;
    const proven = new Map<string, string>();
    await readInto(read, { kind: "AdministrativeArea", languageId, countryId }, proven, signal);
    for (const adminId of admins) {
      const label = proven.get(adminId);
      if (label !== undefined) administrativeAreas.set(adminId, label);
    }
  }
  // Cities are read for EXACTLY the stored administrative areas: no eager country-wide city scan.
  for (const [adminId, cityIds] of cityByAdmin) {
    for (const countryId of countryIds) {
      if (signal?.aborted) return trails;
      const proven = new Map<string, string>();
      await readInto(read, { kind: "City", languageId, countryId, administrativeAreaId: adminId }, proven, signal);
      for (const cityId of cityIds) {
        const label = proven.get(cityId);
        if (label !== undefined) cities.set(cityId, label);
      }
    }
  }

  for (const chain of chains) {
    const terminal = chain.label?.trim() ?? "";
    if (terminal.length === 0) continue;
    const country = lower(chain.countryId);
    const admin = lower(chain.administrativeAreaId);
    const city = lower(chain.cityId);
    const countryLabel = country === null ? undefined : countries.get(country);
    const adminLabel = admin === null ? undefined : administrativeAreas.get(admin);
    const cityLabel = city === null ? undefined : cities.get(city);
    // Every level this chain actually carries must be proven, otherwise the path would silently skip one.
    if (country !== null && countryLabel === undefined) continue;
    if (admin !== null && needsAdministrativeArea(chain.kind) && adminLabel === undefined) continue;
    if (chain.kind === "ServiceArea" && city !== null && cityLabel === undefined) continue;
    const segments: string[] = [];
    // A country selection's terminal IS the country: the proven country label is the whole path, so the
    // leaf must not repeat the root ("ایران › ایران" was a real display wart).
    if (chain.kind === "Country") {
      segments.push(terminal);
      trails.set(chain.key, segments);
      continue;
    }
    if (countryLabel !== undefined) segments.push(countryLabel);
    // A country-wide city keeps its own two-segment path instead of a fabricated province.
    if (adminLabel !== undefined && needsAdministrativeArea(chain.kind)) segments.push(adminLabel);
    if (cityLabel !== undefined && chain.kind === "ServiceArea") segments.push(cityLabel);
    segments.push(terminal);
    trails.set(chain.key, segments);
  }
  return trails;
}
