import type { OfferHistoryPage } from "./types.ts";

/** Parse the owner history contract without converting malformed responses to empty success. */
export function parseOfferHistory(payload: unknown, offerId: string): OfferHistoryPage {
  if (!payload || typeof payload !== "object") throw new Error("offer.history.load_failed");
  const raw = payload as Record<string, unknown>;
  const get = (row: Record<string, unknown>, key: string) => row[key] ?? row[key[0]!.toUpperCase() + key.slice(1)];
  const rows = get(raw, "items"), totalCount = get(raw, "totalCount"), page = get(raw, "page"), pageSize = get(raw, "pageSize");
  if (!Array.isArray(rows) || typeof totalCount !== "number" || !Number.isInteger(totalCount) || totalCount < rows.length
      || typeof page !== "number" || !Number.isInteger(page) || page < 1
      || typeof pageSize !== "number" || !Number.isInteger(pageSize) || pageSize < 1 || rows.length > pageSize) throw new Error("offer.history.load_failed");
  const items = rows.map(value => {
    if (!value || typeof value !== "object") throw new Error("offer.history.load_failed");
    const row = value as Record<string, unknown>;
    const string = (key: string) => { const v = get(row, key); return typeof v === "string" ? v : null; };
    const id = string("offerId"), occurredAt = string("occurredAt"), kind = string("kind");
    if (id !== offerId || !occurredAt || !Number.isFinite(Date.parse(occurredAt)) || !kind) throw new Error("offer.history.load_failed");
    return { offerId: id, occurredAt, kind, eventId: string("eventId"), targetId: string("targetId"), locationId: string("locationId"), actorUserId: string("actorUserId"), summary: string("summary") ?? "", oldValue: string("oldValue"), newValue: string("newValue") };
  });
  return { items, totalCount, page, pageSize };
}
