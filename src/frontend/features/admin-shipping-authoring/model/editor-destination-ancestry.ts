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
  /**
   * Displayable path segments of a selected key, root first.
   *
   * A segment is a real localized name; when an ancestor's name is not part of the read snapshot the
   * list simply starts at the deepest proven name — no GUID, code or guessed ancestor is ever shown.
   */
  readonly segmentsOf: (area: SelectedAreaRead) => readonly string[];
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
  const labelOf = (area: SelectedAreaRead): string | null => {
    const label = factOf(area)?.label?.trim() ?? "";
    return label.length > 0 ? label : null;
  };
  return {
    available: document.destinationFacts !== null,
    factOf,
    labelOf,
    segmentsOf: (area) => {
      const label = labelOf(area);
      return label === null ? [] : [label];
    },
    resolver: { resolve: (area) => {
      const fact = factOf(area);
      return fact === null || fact.resolution !== "Resolved" ? null : toAncestry(fact);
    } },
  };
}

/**
 * Composes the LABELS of this session's verified picks into the document's fact index.
 *
 * A destination the operator has just added has no server fact yet (the server can only prove what is
 * already stored), so without this composition the chip of a freshly picked destination would show
 * "name unavailable" even though its real localized label is known from the row the operator chose.
 * Labels stay display-only: identity and ancestry are unchanged, and a stored fact always wins.
 *
 * @param base The document's own validated fact index.
 * @param pickedLabels Localized labels of picks, keyed by `kind:id`.
 */
export function withPickedLabels(
  base: DestinationFactIndex,
  pickedLabels: ReadonlyMap<string, string>,
): DestinationFactIndex {
  if (pickedLabels.size === 0) return base;
  return {
    ...base,
    labelOf: (area) => {
      const stored = base.labelOf(area);
      if (stored !== null) return stored;
      const picked = pickedLabels.get(keyOf(area))?.trim() ?? "";
      return picked.length > 0 ? picked : null;
    },
  };
}

/**
 * Composes the PATH of this session's verified picks into the document's fact index.
 *
 * Only the terminal of a pick has a server fact, so the ancestors the operator actually walked are known
 * for this session only. A stored selection keeps working from its own fact (and, when the snapshot does
 * not carry ancestor names, honestly starts at the deepest proven name) — nothing is ever invented.
 *
 * @param base The document's own (or already label-composed) fact index.
 * @param trails Displayable segments per `kind:id`, root first.
 */
export function withPickedTrails(
  base: DestinationFactIndex,
  trails: ReadonlyMap<string, readonly string[]>,
): DestinationFactIndex {
  if (trails.size === 0) return base;
  return {
    ...base,
    segmentsOf: (area) => {
      const trail = trails.get(keyOf(area));
      if (trail !== undefined && trail.length > 0) return trail;
      return base.segmentsOf(area);
    },
  };
}

/**
 * The ONE composed index of a document plus every display path known for it.
 *
 * Precedence: the document's own proven fact, then this session's pick, then the stored breadcrumb — a
 * fresh pick is never overridden by an older read of the same key.
 *
 * @param document Validated editor document.
 * @param pickedLabels Localized labels of this session's picks.
 * @param storedTrails Breadcrumb of already stored selections (labels only).
 * @param pickedTrails Breadcrumb of this session's picks.
 */
export function indexWithTrails(
  document: EditorDocument,
  pickedLabels: ReadonlyMap<string, string>,
  storedTrails: ReadonlyMap<string, readonly string[]>,
  pickedTrails: ReadonlyMap<string, readonly string[]>,
): DestinationFactIndex {
  const trails = new Map<string, readonly string[]>([...storedTrails, ...pickedTrails]);
  return withPickedTrails(withPickedLabels(indexDestinationFacts(document), pickedLabels), trails);
}

/** Every stored selection that the snapshot did not prove, in document order (never filtered away). */export function unresolvedSelections(
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
