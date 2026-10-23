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
 * Canonical ancestry of the staged terminal, taken from the rows the operator really selected.
 *
 * @param choice Staged choice.
 * @returns The typed tuple, or `null` when the staged chain is incomplete/incoherent.
 */
export function choiceAncestry(choice: DestinationChoice): ResolvedAreaAncestry | null {
  const terminal = choiceTerminal(choice);
  if (terminal === null || choice.country === null) return null;
  const countryId = choice.country.id;
  const administrativeAreaId = choice.city ? choice.city.parentId : choice.administrativeArea?.id ?? null;
  const cityId = choice.city?.id ?? null;
  const serviceAreaId = choice.serviceArea?.id ?? null;
  switch (terminal.kind) {
    case "Country":
      return { kind: "Country", countryId, administrativeAreaId: null, cityId: null, serviceAreaId: null };
    case "AdministrativeArea":
      return administrativeAreaId === null
        ? null
        : { kind: "AdministrativeArea", countryId, administrativeAreaId, cityId: null, serviceAreaId: null };
    case "City":
      return { kind: "City", countryId, administrativeAreaId, cityId, serviceAreaId: null };
    case "ServiceArea":
      return cityId === null || serviceAreaId === null
        ? null
        : { kind: "ServiceArea", countryId, administrativeAreaId, cityId, serviceAreaId };
  }
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
