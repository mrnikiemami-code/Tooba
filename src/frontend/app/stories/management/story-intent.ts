/**
 * Durable Story mutation intent — freeze key/body/target before send; exact retry.
 * v1 envelope in sessionStorage with verified read-back.
 */

export type StoryIntentAction =
  | "create"
  | "add_item"
  | "submit"
  | "approve_publish"
  | "approve_schedule"
  | "reject"
  | "schedule"
  | "stop"
  | "upload";

export type StoryIntentEnvelope = {
  version: 1;
  action: StoryIntentAction;
  occurrenceKey: string;
  routeTargetId: string | null;
  expectedVersion: number | null;
  body: unknown;
  role: "admin" | "seller";
  shopId: string | null;
  recordedAt: string;
};

const STORAGE_PREFIX = "tooba.story.intent.v1:";

function storageKey(role: string, shopId: string | null, action: string, target: string | null): string {
  return `${STORAGE_PREFIX}${role}:${shopId ?? "platform"}:${action}:${target ?? "new"}`;
}

export function newStoryOccurrenceKey(): string {
  return crypto.randomUUID();
}

export function storeStoryIntent(envelope: StoryIntentEnvelope): boolean {
  if (typeof sessionStorage === "undefined") return false;
  const key = storageKey(envelope.role, envelope.shopId, envelope.action, envelope.routeTargetId);
  const raw = JSON.stringify(envelope);
  try {
    sessionStorage.setItem(key, raw);
    const read = sessionStorage.getItem(key);
    return read === raw;
  } catch {
    return false;
  }
}

export function readStoryIntent(
  role: "admin" | "seller",
  shopId: string | null,
  action: StoryIntentAction,
  target: string | null,
): StoryIntentEnvelope | null {
  if (typeof sessionStorage === "undefined") return null;
  try {
    const raw = sessionStorage.getItem(storageKey(role, shopId, action, target));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as StoryIntentEnvelope;
    if (parsed?.version !== 1 || !parsed.occurrenceKey) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function clearStoryIntentIfCurrent(envelope: StoryIntentEnvelope): void {
  if (typeof sessionStorage === "undefined") return;
  const key = storageKey(envelope.role, envelope.shopId, envelope.action, envelope.routeTargetId);
  try {
    const raw = sessionStorage.getItem(key);
    if (raw && raw === JSON.stringify(envelope)) sessionStorage.removeItem(key);
  } catch {
    /* ignore */
  }
}
