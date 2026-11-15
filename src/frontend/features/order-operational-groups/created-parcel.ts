/**
 * TB-270-R3 — finds the EXACT physical parcel a create/recover post-dispatch operation produced.
 *
 * The review must hand off to the planning modal for the parcel that really exists, never for a guess:
 *  - a resend is identified by its source parcel and its occurrence key (`shippingDispatchKey`);
 *  - a replacement is identified by the damaged-goods decision that funds it.
 * An ambiguous or absent match returns `null`, so the operator stays on the refreshed parcel cards instead of
 * being sent to a modal for a parcel that was not proven to be theirs.
 *
 * Pure: transport-free and tolerant of camelCase/PascalCase JSON only.
 */

type UnknownRecord = Record<string, unknown>;

function text(record: UnknownRecord, ...names: string[]): string | null {
  for (const name of names) {
    const value = record[name];
    if (typeof value === "string" && value.trim().length > 0) return value.trim();
  }
  return null;
}

function list(record: UnknownRecord, ...names: string[]): UnknownRecord[] {
  for (const name of names) {
    const value = record[name];
    if (Array.isArray(value)) {
      return value.filter((row): row is UnknownRecord => Boolean(row) && typeof row === "object");
    }
  }
  return [];
}

function shipmentsOf(snapshot: unknown): UnknownRecord[] {
  if (!snapshot || typeof snapshot !== "object") return [];
  return list(snapshot as UnknownRecord, "shipments", "Shipments");
}

function decisionsOf(shipment: UnknownRecord): UnknownRecord[] {
  return list(shipment, "damagedResolutions", "DamagedResolutions");
}

/** Every parcel of the snapshot, newest first, matching the requested identity. */
export type CreatedParcelProbe = {
  /** `resend` or `replacement` — the two linked attempts a plan is confirmed for. */
  kind: "resend" | "replacement";
  /** The parent parcel the operation was run on. */
  sourceShipmentId: string;
  /** The reviewed occurrence key sent to the server (exact identity of this attempt). */
  idempotencyKey: string;
};

function statusOf(shipment: UnknownRecord): string {
  return (text(shipment, "status", "Status") ?? "").toLowerCase();
}

/**
 * The single physical parcel this operation created or recovered, or `null` when it cannot be proven.
 */
export function resolveCreatedParcelId(
  snapshot: unknown,
  probe: CreatedParcelProbe,
): string | null {
  const source = probe.sourceShipmentId.trim();
  const key = probe.idempotencyKey.trim();
  if (!source) return null;

  const shipments = shipmentsOf(snapshot);
  const candidates: UnknownRecord[] = [];

  if (probe.kind === "resend") {
    for (const shipment of shipments) {
      const resendOf = text(shipment, "resendOfShipmentId", "ResendOfShipmentId");
      if (resendOf !== source) continue;
      // A damaged-goods replacement also carries the source id; only a pure resend is a resend.
      if (text(shipment, "damagedResolutionId", "DamagedResolutionId")) continue;
      // TB-270-R3: a delivered/dispatched/cancelled child is COMPLETED HISTORY. Planning it would offer a
      // calendar for a parcel that already left (or can never leave), so it is never a handoff target.
      if (statusOf(shipment) !== "created") continue;
      candidates.push(shipment);
    }
  } else {
    for (const shipment of shipments) {
      if (!text(shipment, "damagedResolutionId", "DamagedResolutionId")) continue;
      if (statusOf(shipment) !== "created") continue;
      candidates.push(shipment);
    }
  }

  if (candidates.length === 0) return null;

  // 1) The exact occurrence key the server recorded on the parcel is the strongest identity.
  if (key) {
    const exact = candidates.filter(
      (shipment) => text(shipment, "shippingDispatchKey", "ShippingDispatchKey") === key,
    );
    if (exact.length === 1) return text(exact[0], "shipmentId", "ShipmentId");
    if (exact.length > 1) return null;
  }

  // 2) For a replacement the decision's own link is exact (replacementShipmentId on the source parcel's decision).
  if (probe.kind === "replacement") {
    const linked = new Set<string>();
    for (const shipment of shipments) {
      for (const decision of decisionsOf(shipment)) {
        const replacementId = text(decision, "replacementShipmentId", "ReplacementShipmentId");
        const decisionKey = text(decision, "occurrenceKey", "OccurrenceKey");
        const outcome = (text(decision, "outcome", "Outcome") ?? "").toLowerCase();
        const status = (text(decision, "status", "Status") ?? "").toLowerCase();
        if (!replacementId) continue;
        if (outcome !== "replacement") continue;
        if (status !== "reserved" && status !== "fulfilled") continue;
        if (key && decisionKey && decisionKey !== key) continue;
        if (key && !decisionKey) continue;
        linked.add(replacementId);
      }
    }
    if (linked.size === 1) {
      const only = [...linked][0];
      return candidates.some((shipment) => text(shipment, "shipmentId", "ShipmentId") === only)
        ? only
        : null;
    }
    return null;
  }

  // 3) A resend must be the ONLY still-created candidate of that source; two candidates are ambiguous.
  const created = candidates.filter((shipment) => statusOf(shipment) === "created");
  return created.length === 1 ? text(created[0], "shipmentId", "ShipmentId") : null;
}
