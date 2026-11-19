/**
 * Durable Story mutation intent — freeze exact wire before send; refuse overwrite of pending.
 * v2 envelope in sessionStorage with verified read-back and actor binding.
 */

export type StoryIntentAction =
  | "create"
  | "update"
  | "add_item"
  | "remove_item"
  | "reorder_items"
  | "submit"
  | "approve_publish"
  | "approve_schedule"
  | "reject"
  | "schedule"
  | "stop"
  | "return_to_draft"
  | "upload"
  | "upload_add";

export type StoryIntentEnvelope = {
  version: 2;
  action: StoryIntentAction;
  occurrenceKey: string;
  method: string;
  path: string;
  routeTargetId: string | null;
  expectedVersion: number | null;
  /** Exact JSON body string or FormData fingerprint metadata (not binary). */
  bodyText: string | null;
  uploadMeta: {
    fileName: string;
    fileType: string;
    fileSize: number;
    digestHex: string;
  } | null;
  children: Array<{ step: string; occurrenceKey: string; done: boolean }>;
  role: "admin" | "seller";
  shopId: string | null;
  actorUserId: string;
  recordedAt: string;
};

export type StoryIntentRead =
  | { state: "ok"; envelope: StoryIntentEnvelope }
  | { state: "missing" }
  | { state: "unreadable"; detail: string };

const STORAGE_PREFIX = "tooba.story.intent.v2:";

function storageKey(role: string, shopId: string | null, action: string, target: string | null): string {
  return `${STORAGE_PREFIX}${role}:${shopId ?? "platform"}:${action}:${target ?? "new"}`;
}

export function newStoryOccurrenceKey(): string {
  return crypto.randomUUID();
}

export async function digestFile(file: File): Promise<string> {
  const buf = await file.arrayBuffer();
  const hash = await crypto.subtle.digest("SHA-256", buf);
  return [...new Uint8Array(hash)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

export function storeStoryIntent(envelope: StoryIntentEnvelope): boolean {
  if (typeof sessionStorage === "undefined") return false;
  if (!envelope.actorUserId?.trim()) return false;
  const key = storageKey(envelope.role, envelope.shopId, envelope.action, envelope.routeTargetId);
  try {
    const existing = sessionStorage.getItem(key);
    if (existing) {
      try {
        const parsed = JSON.parse(existing) as StoryIntentEnvelope;
        if (parsed?.version === 2 && parsed.occurrenceKey && parsed.occurrenceKey !== envelope.occurrenceKey) {
          return false;
        }
      } catch {
        return false;
      }
    }
    const raw = JSON.stringify(envelope);
    sessionStorage.setItem(key, raw);
    return sessionStorage.getItem(key) === raw;
  } catch {
    return false;
  }
}

export function readStoryIntent(
  role: "admin" | "seller",
  shopId: string | null,
  action: StoryIntentAction,
  target: string | null,
): StoryIntentRead {
  if (typeof sessionStorage === "undefined") return { state: "unreadable", detail: "storage.unavailable" };
  try {
    const raw = sessionStorage.getItem(storageKey(role, shopId, action, target));
    if (!raw) return { state: "missing" };
    const parsed = JSON.parse(raw) as StoryIntentEnvelope;
    if (parsed?.version !== 2 || !parsed.occurrenceKey || !parsed.actorUserId) {
      return { state: "unreadable", detail: "storage.malformed" };
    }
    return { state: "ok", envelope: parsed };
  } catch (err) {
    return { state: "unreadable", detail: err instanceof Error ? err.message : "storage.throw" };
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

export function listPendingStoryIntents(role: "admin" | "seller", shopId: string | null): StoryIntentEnvelope[] {
  if (typeof sessionStorage === "undefined") return [];
  const prefix = `${STORAGE_PREFIX}${role}:${shopId ?? "platform"}:`;
  const rows: StoryIntentEnvelope[] = [];
  try {
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i);
      if (!key?.startsWith(prefix)) continue;
      const raw = sessionStorage.getItem(key);
      if (!raw) continue;
      try {
        const parsed = JSON.parse(raw) as StoryIntentEnvelope;
        if (parsed?.version === 2 && parsed.occurrenceKey) rows.push(parsed);
      } catch {
        /* skip malformed */
      }
    }
  } catch {
    return [];
  }
  return rows;
}
