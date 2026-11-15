/**
 * TB-270-R3 — finds the EXACT physical parcel a create/recover post-dispatch operation produced.
 * TB-270-R4 Repair C — exact operation identity only; a single remaining candidate is never proof.
 *
 * The review must hand off to the planning modal for the parcel that really exists, never for a guess:
 *  - a resend is identified by its exact source parcel AND the exact non-empty occurrence key the owning
 *    command recorded on the parcel (`shippingDispatchKey`);
 *  - a replacement is identified by the damage decision recorded on the REQUESTED source parcel, that decision's
 *    reviewed occurrence key, and the Created child that very decision links.
 * An ambiguous, missing or mismatched identity returns `null`, so the operator stays on the refreshed parcel cards
 * instead of being sent to a modal for a parcel that was not proven to be this operation's own.
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

function idOf(shipment: UnknownRecord): string | null {
  return text(shipment, "shipmentId", "ShipmentId");
}

/** Every parcel of the snapshot, matching the requested exact identity. */
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

function isCreated(shipment: UnknownRecord): boolean {
  return statusOf(shipment) === "created";
}

function damagedResolutionIdOf(shipment: UnknownRecord): string | null {
  return text(shipment, "damagedResolutionId", "DamagedResolutionId");
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
  // TB-270-R4: BOTH identities are required. Without the reviewed occurrence key there is no exact operation
  // identity at all, and "the only Created child of this source" is a guess — never a handoff target.
  if (!source || !key) return null;

  const shipments = shipmentsOf(snapshot);

  if (probe.kind === "resend") {
    const exact = shipments.filter((shipment) => {
      if (!isCreated(shipment)) return false;
      if (text(shipment, "resendOfShipmentId", "ResendOfShipmentId") !== source) return false;
      // A damaged-goods replacement also carries the source id; only a pure resend is a resend.
      if (damagedResolutionIdOf(shipment)) return false;
      return text(shipment, "shippingDispatchKey", "ShippingDispatchKey") === key;
    });
    return exact.length === 1 ? idOf(exact[0]) : null;
  }

  // The decision must live on the REQUESTED source parcel: a decision of any other returned parcel — even one that
  // happens to carry the same key — is a different operation and must never be substituted.
  const sourceParcel = shipments.find((shipment) => idOf(shipment) === source);
  if (!sourceParcel) return null;

  const matching = decisionsOf(sourceParcel).filter((decision) => {
    if ((text(decision, "outcome", "Outcome") ?? "").toLowerCase() !== "replacement") return false;
    const status = (text(decision, "status", "Status") ?? "").toLowerCase();
    if (status !== "reserved" && status !== "fulfilled") return false;
    if ((text(decision, "occurrenceKey", "OccurrenceKey") ?? "") !== key) return false;
    return Boolean(text(decision, "replacementShipmentId", "ReplacementShipmentId"));
  });
  if (matching.length === 0) return null;

  const replacementIds = new Set(
    matching.map((decision) => text(decision, "replacementShipmentId", "ReplacementShipmentId")!),
  );
  // Two decisions answering for two different parcels under one reviewed key is ambiguous: open nothing.
  if (replacementIds.size !== 1) return null;

  const replacementId = [...replacementIds][0];
  const decisionIds = new Set(
    matching.map((decision) => text(decision, "damagedGoodsResolutionId", "DamagedGoodsResolutionId")),
  );

  const child = shipments.find((shipment) => {
    if (idOf(shipment) !== replacementId) return false;
    if (!isCreated(shipment)) return false;
    if (!damagedResolutionIdOf(shipment)) return false;
    // The child must be the parcel THIS decision funds, not merely a parcel with a similar id.
    const childDecisionId = damagedResolutionIdOf(shipment);
    if (!decisionIds.has(childDecisionId)) return false;
    // When the parcel also records its source, it must be the requested one.
    const childSource = text(shipment, "resendOfShipmentId", "ResendOfShipmentId");
    if (childSource && childSource !== source) return false;
    return true;
  });

  return child ? replacementId : null;
}
