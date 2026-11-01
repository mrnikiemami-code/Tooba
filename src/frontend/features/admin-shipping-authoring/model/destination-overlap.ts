/**
 * Pure hierarchical destination exclusivity for ONE shipping method (TB-246-R3).
 *
 * Compares a proposed terminal with every already-selected terminal using only server-proven
 * {@link ResolvedAreaAncestry} facts. Labels, display order and string prefixes are never authority.
 * Missing ancestry fails closed (no invented relationship).
 */
import type { AuthoringAreaKind, SelectedAreaRead } from "./editor-document.ts";
import type { AreaAncestryResolver, ResolvedAreaAncestry } from "./editor-edits.ts";
import { areaIdentity } from "./destination-choice.ts";

/** How the candidate relates to one existing selection. */
export type DestinationOverlapKind =
  | "exact"
  | "proposed_ancestor"
  | "proposed_descendant"
  | "unproven";

/** One structured conflict between a candidate and an existing selection. */
export interface DestinationOverlapConflict {
  readonly kind: DestinationOverlapKind;
  readonly candidate: SelectedAreaRead;
  readonly existing: SelectedAreaRead;
  readonly existingGroupKey: string;
  /** Zero-based display index of the conflicting group in the proposed collection, when known. */
  readonly existingGroupIndex: number;
}

export interface ExistingDestinationRef {
  readonly groupKey: string;
  readonly groupIndex: number;
  readonly area: SelectedAreaRead;
}

function idEq(left: string | null | undefined, right: string | null | undefined): boolean {
  if (left == null || right == null) return false;
  const a = left.trim();
  const b = right.trim();
  if (a.length === 0 || b.length === 0) return false;
  return a.toLowerCase() === b.toLowerCase();
}

function sameSelection(left: SelectedAreaRead, right: SelectedAreaRead): boolean {
  return left.kind === right.kind && idEq(left.id, right.id);
}

/**
 * True when `higher` is a STRICT geographic ancestor of `lower` (same branch, not the same terminal).
 * Both ancestries must be complete for their kinds; otherwise the caller must treat the pair as unproven.
 */
export function isStrictGeographicAncestor(
  higher: ResolvedAreaAncestry,
  lower: ResolvedAreaAncestry,
): boolean {
  if (!idEq(higher.countryId, lower.countryId)) return false;
  switch (higher.kind) {
    case "Country":
      return lower.kind !== "Country";
    case "AdministrativeArea": {
      if (!idEq(higher.administrativeAreaId, lower.administrativeAreaId)) return false;
      return lower.kind === "City" || lower.kind === "ServiceArea";
    }
    case "City": {
      if (!idEq(higher.administrativeAreaId, lower.administrativeAreaId)) return false;
      if (!idEq(higher.cityId, lower.cityId)) return false;
      return lower.kind === "ServiceArea";
    }
    case "ServiceArea":
      return false;
    default: {
      const _exhaustive: never = higher.kind;
      void _exhaustive;
      return false;
    }
  }
}

/** Ancestry is usable for hierarchical comparison of its declared kind. */
export function ancestryUsable(ancestry: ResolvedAreaAncestry): boolean {
  if (!ancestry.countryId?.trim()) return false;
  switch (ancestry.kind as AuthoringAreaKind) {
    case "Country":
      return true;
    case "AdministrativeArea":
      return Boolean(ancestry.administrativeAreaId?.trim());
    case "City":
      return Boolean(ancestry.administrativeAreaId?.trim() && ancestry.cityId?.trim());
    case "ServiceArea":
      return Boolean(
        ancestry.administrativeAreaId?.trim() && ancestry.cityId?.trim() && ancestry.serviceAreaId?.trim(),
      );
    default:
      return false;
  }
}

/**
 * Classifies the relationship of one proposed terminal against one existing selection.
 *
 * Exact equality needs only `(kind,id)`. Hierarchical decisions require usable ancestry on both sides;
 * otherwise the result is `unproven` (fail closed).
 */
export function classifyDestinationOverlap(
  candidate: SelectedAreaRead,
  candidateAncestry: ResolvedAreaAncestry | null,
  existing: SelectedAreaRead,
  existingAncestry: ResolvedAreaAncestry | null,
): DestinationOverlapKind | null {
  if (sameSelection(candidate, existing)) return "exact";
  if (candidateAncestry === null || existingAncestry === null) return "unproven";
  if (!ancestryUsable(candidateAncestry) || !ancestryUsable(existingAncestry)) return "unproven";
  if (isStrictGeographicAncestor(candidateAncestry, existingAncestry)) return "proposed_ancestor";
  if (isStrictGeographicAncestor(existingAncestry, candidateAncestry)) return "proposed_descendant";
  return null;
}

/**
 * First hierarchical conflict of a proposed Add against the current proposed tree.
 * Exact duplicates use this same authority (not a second divergent check).
 */
export function findDestinationOverlap(
  candidate: SelectedAreaRead,
  candidateAncestry: ResolvedAreaAncestry,
  existing: readonly ExistingDestinationRef[],
  resolve: AreaAncestryResolver,
): DestinationOverlapConflict | null {
  for (const entry of existing) {
    const kind = classifyDestinationOverlap(
      candidate,
      candidateAncestry,
      entry.area,
      resolve.resolve(entry.area),
    );
    if (kind === null) continue;
    return {
      kind,
      candidate,
      existing: entry.area,
      existingGroupKey: entry.groupKey,
      existingGroupIndex: entry.groupIndex,
    };
  }
  return null;
}

/**
 * Every pairwise overlap in the proposed collection (each unordered pair reported once,
 * with the first group as "existing" and the second as "candidate" for stable messaging).
 */
export function findAllDestinationOverlaps(
  groups: readonly { readonly key: string; readonly areas: readonly SelectedAreaRead[] }[],
  resolve: AreaAncestryResolver,
): readonly DestinationOverlapConflict[] {
  const refs: ExistingDestinationRef[] = [];
  groups.forEach((group, groupIndex) => {
    for (const area of group.areas) {
      refs.push({ groupKey: group.key, groupIndex, area });
    }
  });
  const conflicts: DestinationOverlapConflict[] = [];
  const seen = new Set<string>();
  for (let i = 0; i < refs.length; i += 1) {
    const left = refs[i]!;
    const leftAncestry = resolve.resolve(left.area);
    for (let j = i + 1; j < refs.length; j += 1) {
      const right = refs[j]!;
      const kind = classifyDestinationOverlap(right.area, resolve.resolve(right.area), left.area, leftAncestry);
      if (kind === null) continue;
      const key = [areaIdentity(left.area), areaIdentity(right.area)].sort().join("|");
      if (seen.has(key)) continue;
      seen.add(key);
      conflicts.push({
        kind,
        candidate: right.area,
        existing: left.area,
        existingGroupKey: left.groupKey,
        existingGroupIndex: left.groupIndex,
      });
    }
  }
  return conflicts;
}

/** Flatten groups into ExistingDestinationRef rows for Add checks. */
export function existingDestinationRefs(
  groups: readonly { readonly key: string; readonly areas: readonly SelectedAreaRead[] }[],
): readonly ExistingDestinationRef[] {
  const refs: ExistingDestinationRef[] = [];
  groups.forEach((group, groupIndex) => {
    for (const area of group.areas) {
      refs.push({ groupKey: group.key, groupIndex, area });
    }
  });
  return refs;
}
