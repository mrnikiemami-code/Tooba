/**
 * Cascading destination CHOICE of one operator session, and the ONE ancestry resolver used at Apply.
 *
 * Nothing here derives a parent from an id, a label, a code or "the currently highlighted area": each
 * level carries the row the operator actually opened, whose `parentId` was already verified by the
 * strict decoder. A newly chosen terminal therefore contributes real operation-local facts, while
 * retained selections keep their server-proven `destinationFacts` — both feed one resolver.
 */
import type { AuthoringAreaKind, SelectedAreaRead } from "./editor-document.ts";
import type { AreaAncestryResolver, ResolvedAreaAncestry } from "./editor-edits.ts";
import type { GroupDraft } from "./editor-groups.ts";
import type { GeographyOption } from "../api/geography-lookup-api.ts";

/** One staged (not yet added) choice. Changing a parent clears every dependent staged level. */
export interface DestinationChoice {
  readonly level: AuthoringAreaKind;
  readonly country: GeographyOption | null;
  readonly administrativeArea: GeographyOption | null;
  readonly city: GeographyOption | null;
  readonly serviceArea: GeographyOption | null;
}

/** An untouched choice stage. */
export function emptyChoice(level: AuthoringAreaKind = "Country"): DestinationChoice {
  return { level, country: null, administrativeArea: null, city: null, serviceArea: null };
}

/** Terminal order used to reason about the cascade (never a coverage/pricing decision). */
export const AREA_LEVELS: readonly AuthoringAreaKind[] = ["Country", "AdministrativeArea", "City", "ServiceArea"];

/** Selects the country and clears every dependent staged level. */
export function chooseCountry(choice: DestinationChoice, option: GeographyOption): DestinationChoice {
  return { ...emptyChoice(choice.level), country: option };
}

/** Selects an administrative area of the staged country and clears city/service area. */
export function chooseAdministrativeArea(choice: DestinationChoice, option: GeographyOption): DestinationChoice {
  return { ...choice, administrativeArea: option, city: null, serviceArea: null };
}

/** Selects a city; its ACTUAL nullable parent is kept on the row, never replaced by the highlighted area. */
export function chooseCity(choice: DestinationChoice, option: GeographyOption): DestinationChoice {
  return { ...choice, city: option, serviceArea: null };
}

/** Selects a service area of the staged city. */
export function chooseServiceArea(choice: DestinationChoice, option: GeographyOption): DestinationChoice {
  return { ...choice, serviceArea: option };
}

/** Clears the staged area (keeps the country, so country-wide cities become reachable again). */
export function clearAdministrativeArea(choice: DestinationChoice): DestinationChoice {
  return { ...choice, administrativeArea: null, city: null, serviceArea: null };
}

/** Clears the staged city and its dependent service area. */
export function clearCity(choice: DestinationChoice): DestinationChoice {
  return { ...choice, city: null, serviceArea: null };
}

/** Clears only the staged service area (the city stays selected). */
export function clearServiceArea(choice: DestinationChoice): DestinationChoice {
  return { ...choice, serviceArea: null };
}

/** Changes the terminal level without discarding already chosen parents. */
export function chooseLevel(choice: DestinationChoice, level: AuthoringAreaKind): DestinationChoice {
  return { ...choice, level };
}

/** Stable map key of one selection identity. */
export function areaIdentity(area: SelectedAreaRead): string {
  return `${area.kind}:${area.id.toLowerCase()}`;
}

/** The chosen terminal of the staged level, or `null` when a required parent is still missing. */
export function choiceTerminal(choice: DestinationChoice): SelectedAreaRead | null {
  switch (choice.level) {
    case "Country":
      return choice.country ? { kind: "Country", id: choice.country.id } : null;
    case "AdministrativeArea":
      return choice.country && choice.administrativeArea
        ? { kind: "AdministrativeArea", id: choice.administrativeArea.id }
        : null;
    case "City":
      return choice.country && choice.city ? { kind: "City", id: choice.city.id } : null;
    case "ServiceArea":
      return choice.country && choice.city && choice.serviceArea
        ? { kind: "ServiceArea", id: choice.serviceArea.id }
        : null;
  }
}

/**
 * Localized label of the staged terminal, exactly as the chosen row carries it.
 *
 * Display-only: it never proves ancestry and is never sent to the server. An empty/blank label stays
 * empty so the caller can show the honest "name unavailable" state instead of a code.
 *
 * @param choice Staged choice.
 */
export function choiceTerminalLabel(choice: DestinationChoice): string {
  switch (choice.level) {
    case "Country":
      return choice.country?.label.trim() ?? "";
    case "AdministrativeArea":
      return choice.administrativeArea?.label.trim() ?? "";
    case "City":
      return choice.city?.label.trim() ?? "";
    case "ServiceArea":
      return choice.serviceArea?.label.trim() ?? "";
  }
}

/**
 * Canonical ancestry of the staged terminal, taken from the rows the operator really selected.
 *
 * @param choice Staged choice.
 * @returns The typed tuple, or `null` when the staged chain is incomplete/incoherent.
 */
export function choiceAncestry(choice: DestinationChoice): ResolvedAreaAncestry | null {
  const terminal = choiceTerminal(choice);
  if (terminal === null || choice.country === null) return null;
  const countryId = choice.country.id;
  const cityId = choice.city?.id ?? null;
  // The city row's verified parent is the only administrative authority of a city/service-area
  // terminal; an AdministrativeArea terminal takes its OWN staged row, never a deeper city parent.
  const cityParentId = choice.city?.parentId ?? null;
  const serviceAreaId = choice.serviceArea?.id ?? null;
  switch (terminal.kind) {
    case "Country":
      return { kind: "Country", countryId, administrativeAreaId: null, cityId: null, serviceAreaId: null };
    case "AdministrativeArea":
      if (choice.administrativeArea === null) return null;
      return {
        kind: "AdministrativeArea",
        countryId,
        administrativeAreaId: choice.administrativeArea.id,
        cityId: null,
        serviceAreaId: null,
      };
    case "City":
      return { kind: "City", countryId, administrativeAreaId: cityParentId, cityId, serviceAreaId: null };
    case "ServiceArea":
      return cityId === null || serviceAreaId === null
        ? null
        : { kind: "ServiceArea", countryId, administrativeAreaId: cityParentId, cityId, serviceAreaId };
  }
}

/**
 * Active/chain coherence of the staged choice, re-checked at Add (never only at render).
 *
 * A forged/stale programmatic option can reach the state, so every level of the chain must be an
 * ACTIVE row whose verified parent is the level above it; an explicit area filter may not yield a
 * null-admin city, and a service area must belong to the staged city.
 */
export function choiceChainActive(choice: DestinationChoice): boolean {
  const { country, administrativeArea, city, serviceArea } = choice;
  if (country === null || !country.isActive) return false;
  if (administrativeArea !== null) {
    if (!administrativeArea.isActive) return false;
    if (
      administrativeArea.parentId === null ||
      administrativeArea.parentId.toLowerCase() !== country.id.toLowerCase()
    ) {
      return false;
    }
  }
  if (city !== null) {
    if (!city.isActive) return false;
    if (city.parentId !== null && administrativeArea !== null) {
      if (city.parentId.toLowerCase() !== administrativeArea.id.toLowerCase()) return false;
    } else if (city.parentId === null && administrativeArea !== null) {
      // A filtered country/area read can only return cities of that area.
      return false;
    }
  }
  if (serviceArea !== null) {
    if (!serviceArea.isActive) return false;
    if (
      city === null ||
      serviceArea.parentId === null ||
      serviceArea.parentId.toLowerCase() !== city.id.toLowerCase()
    ) {
      return false;
    }
  }
  return true;
}

/** Display name of one option row; a blank label is honestly shown as unavailable, never as its code. */
export function optionDisplayName(option: GeographyOption, unavailableName: string): string {
  const label = option.label.trim();
  return label.length > 0 ? label : unavailableName;
}

const EVIDENCE_LEVELS = ["countries", "areas", "cities", "serviceAreas"] as const;

/** The CURRENT rows the picker actually holds (language/context scoped evidence, never a cache). */
export interface DestinationRowEvidence {
  readonly lists: Readonly<Record<(typeof EVIDENCE_LEVELS)[number], readonly GeographyOption[]>>;
  readonly settled: Readonly<Record<(typeof EVIDENCE_LEVELS)[number], boolean>>;
}

function currentRow(rows: readonly GeographyOption[], id: string): GeographyOption | null {
  const target = id.toLowerCase();
  return rows.find((row) => row.id.toLowerCase() === target) ?? null;
}

function sameId(left: string | null, right: string): boolean {
  return left !== null && left.toLowerCase() === right.toLowerCase();
}

/**
 * The ONE Add eligibility of the staged terminal, computed from the CURRENT verified rows.
 *
 * Staged flags alone are not evidence: a row chosen under an old language/context, a row whose parent
 * is absent or inactive in the current generation, or a path whose prerequisite is still pending or
 * failed cannot be added. Country-wide non-null city parents are matched to a current ACTIVE area row
 * even when no administrative filter is selected; a null-admin city stays lawful on a verified
 * country-wide read. Only the required terminal path is inspected, so an unrelated child failure does
 * not block a Country/AdministrativeArea terminal.
 *
 * @param choice The staged choice.
 * @param evidence The picker's current rows plus which stages settled successfully.
 */
export function destinationAddEligibility(
  choice: DestinationChoice,
  evidence: DestinationRowEvidence,
): boolean {
  if (choice.country === null || !evidence.settled.countries) return false;
  const countryRow = currentRow(evidence.lists.countries, choice.country.id);
  if (countryRow === null || !countryRow.isActive || countryRow.parentId !== null) return false;

  const terminal = choiceTerminal(choice);
  if (terminal === null) return false;
  if (terminal.kind === "Country") return true;

  // The required terminal path always contains the administrative area when it is staged.
  let areaRow: GeographyOption | null = null;
  if (choice.administrativeArea !== null) {
    if (!evidence.settled.areas) return false;
    areaRow = currentRow(evidence.lists.areas, choice.administrativeArea.id);
    if (areaRow === null || !areaRow.isActive) return false;
    if (!sameId(areaRow.parentId, countryRow.id)) return false;
  } else if (terminal.kind === "AdministrativeArea") {
    return false;
  }

  if (terminal.kind === "AdministrativeArea") return true;

  if (choice.city === null || !evidence.settled.cities) return false;
  const cityRow = currentRow(evidence.lists.cities, choice.city.id);
  if (cityRow === null || !cityRow.isActive) return false;

  if (areaRow !== null) {
    // Explicit area filter: exactly that parent, never a null-parent contradiction.
    if (!sameId(cityRow.parentId, areaRow.id)) return false;
  } else {
    // Country-wide branch: verified evidence must exist; a non-null parent must be a CURRENT ACTIVE area.
    if (!evidence.settled.areas) return false;
    if (cityRow.parentId !== null) {
      const parentRow = currentRow(evidence.lists.areas, cityRow.parentId);
      if (parentRow === null || !parentRow.isActive) return false;
      if (!sameId(parentRow.parentId, countryRow.id)) return false;
    }
  }

  if (terminal.kind === "City") return true;

  if (choice.serviceArea === null || !evidence.settled.serviceAreas) return false;
  const serviceRow = currentRow(evidence.lists.serviceAreas, choice.serviceArea.id);
  if (serviceRow === null || !serviceRow.isActive) return false;
  return sameId(serviceRow.parentId, cityRow.id);
}

/**
 * Composes the baseline `destinationFacts` resolver with the operation-local verified picks.
 *
 * @param baseline The document's own validated fact resolver.
 * @param added Verified ancestry of newly chosen terminals, keyed by {@link areaIdentity}.
 */
export function composeAncestryResolver(
  baseline: AreaAncestryResolver,
  added: ReadonlyMap<string, ResolvedAreaAncestry>,
): AreaAncestryResolver {
  return {
    resolve: (area) => baseline.resolve(area) ?? added.get(areaIdentity(area)) ?? null,
  };
}

/** One proposed selection the resolver cannot prove truthfully. */
export interface UnresolvedDraftSelection {
  readonly groupKey: string;
  readonly area: SelectedAreaRead;
}

/**
 * Unresolved selections of the PROPOSED submitted tree only.
 *
 * A destination the operator explicitly removed (or a whole removed group) is gone from the drafts,
 * so it can no longer block an otherwise valid Apply; what remains visible and unresolved still does.
 */
export function unresolvedDraftSelections(
  groups: readonly Pick<GroupDraft, "key" | "areas">[],
  resolver: AreaAncestryResolver,
): readonly UnresolvedDraftSelection[] {
  const unresolved: UnresolvedDraftSelection[] = [];
  for (const group of groups) {
    for (const area of group.areas) {
      if (resolver.resolve(area) === null) unresolved.push({ groupKey: group.key, area });
    }
  }
  return unresolved;
}
