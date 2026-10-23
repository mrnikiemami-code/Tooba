/**
 * Synchronous destination-fact index for ONE editor document.
 *
 * The only source is the validated `destinationFacts` of the SAME read snapshot. There is no
 * all-world Geography scan, no guessed parent, no global cache and no browser-supplied authority: a
 * selection that the server did not prove stays unresolved and blocks Apply with a localized reason.
 */
import type {
  EditorDestinationFact,
  EditorDocument,
  SelectedAreaRead,
  UnresolvedAreaSelection,
} from "./editor-document.ts";
import type { AreaAncestryResolver, ResolvedAreaAncestry } from "./editor-edits.ts";

/** Read-only view over the facts of one document. */
export interface DestinationFactIndex {
  /** False when the server omitted the hydration field entirely (explicitly unavailable). */
  readonly available: boolean;
  /** The one fact of a selected key, or `null` when the snapshot did not prove it. */
  readonly factOf: (area: SelectedAreaRead) => EditorDestinationFact | null;
  /** Real localized label of the selected key, or `null` (the UI never shows a raw GUID as a name). */
  readonly labelOf: (area: SelectedAreaRead) => string | null;
  /** Authoritative ancestry resolver consumed by the request builder. */
  readonly resolver: AreaAncestryResolver;
}

function keyOf(area: SelectedAreaRead): string {
  return `${area.kind}:${area.id.toLowerCase()}`;
}

function toAncestry(fact: EditorDestinationFact): ResolvedAreaAncestry | null {
  if (!fact.area) return null;
  return {
    kind: fact.area.kind,
    countryId: fact.area.countryId,
    administrativeAreaId: fact.area.administrativeAreaId,
    cityId: fact.area.cityId,
    serviceAreaId: fact.area.serviceAreaId,
  };
}

/**
 * Builds the fact index of one document.
 *
 * @param document The validated editor document (facts already proven against its own selections).
 */
export function indexDestinationFacts(document: EditorDocument): DestinationFactIndex {
  const facts = new Map<string, EditorDestinationFact>();
  for (const fact of document.destinationFacts ?? []) facts.set(keyOf(fact), fact);
  const factOf = (area: SelectedAreaRead): EditorDestinationFact | null => facts.get(keyOf(area)) ?? null;
  return {
    available: document.destinationFacts !== null,
    factOf,
    labelOf: (area) => {
      const label = factOf(area)?.label?.trim() ?? "";
      return label.length > 0 ? label : null;
    },
    resolver: { resolve: (area) => {
      const fact = factOf(area);
      return fact === null || fact.resolution !== "Resolved" ? null : toAncestry(fact);
    } },
  };
}

/** Every stored selection that the snapshot did not prove, in document order (never filtered away). */
export function unresolvedSelections(
  document: EditorDocument,
  index: DestinationFactIndex,
): readonly UnresolvedAreaSelection[] {
  const unresolved: UnresolvedAreaSelection[] = [];
  for (const group of document.regionGroups) {
    for (const area of group.areas) {
      if (index.resolver.resolve(area) === null) unresolved.push({ groupId: group.groupId, area });
    }
  }
  return unresolved;
}

/**
 * True when the persisted geography cannot be re-sent truthfully.
 *
 * @param document Editor document.
 * @param index Its fact index.
 */
export function destinationFactsBlockApply(document: EditorDocument, index: DestinationFactIndex): boolean {
  return !index.available || unresolvedSelections(document, index).length > 0;
}
