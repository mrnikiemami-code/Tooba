/**
 * Durable Story mutation intent — freeze exact wire before send; refuse overwrite of pending.
 * v2 envelope in sessionStorage with actor+role+shop namespace, verified read-back.
 */
export type StoryIntentAction =
  | "create"
  | "update"
  | "add_item"
  | "update_item"
  | "remove_item"
  | "delete"
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

export type StoryUploadMeta = {
  fileName: string;
  fileType: string;
  fileSize: number;
  digestHex: string;
};

export type StoryIntentChild = {
  step: string;
  occurrenceKey: string;
  done: boolean;
};

export type StoryIntentEnvelope = {
  version: 2;
  action: StoryIntentAction;
  occurrenceKey: string;
  method: string;
  path: string;
  routeTargetId: string | null;
  expectedVersion: number | null;
  /** Exact JSON body string (complete DTO). Null only while upload bytes are still unresolved. */
  bodyText: string | null;
  uploadMeta: StoryUploadMeta | null;
  /** After upload success: exact next add-item body frozen before send. */
  addBodyText: string | null;
  uploadedAssetId: string | null;
  children: StoryIntentChild[];
  role: "admin" | "seller";
  shopId: string | null;
  actorUserId: string;
  recordedAt: string;
};

export type StoryIntentRead =
  | { state: "ok"; envelope: StoryIntentEnvelope }
  | { state: "missing" }
  | { state: "unreadable"; detail: string }
  | { state: "legacy_blocked"; detail: string };

const STORAGE_PREFIX = "tooba.story.intent.v2:";
const LEGACY_PREFIXES = ["tooba.story.intent.v1:", "tooba.story.intent:"] as const;
const KNOWN_ACTIONS = new Set<StoryIntentAction>([
  "create",
  "update",
  "add_item",
  "update_item",
  "remove_item",
  "delete",
  "reorder_items",
  "submit",
  "approve_publish",
  "approve_schedule",
  "reject",
  "schedule",
  "stop",
  "return_to_draft",
  "upload",
  "upload_add",
]);

function storageKey(
  role: string,
  shopId: string | null,
  actorUserId: string,
  action: string,
  target: string | null,
): string {
  return `${STORAGE_PREFIX}${role}:${shopId ?? "platform"}:${actorUserId}:${action}:${target ?? "new"}`;
}

export function newStoryOccurrenceKey(): string {
  return crypto.randomUUID();
}

export async function digestFile(file: File): Promise<string> {
  const buf = await file.arrayBuffer();
  const hash = await crypto.subtle.digest("SHA-256", buf);
  return [...new Uint8Array(hash)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

export function validateStoryEnvelope(
  envelope: StoryIntentEnvelope,
  current?: { role: "admin" | "seller"; shopId: string | null; actorUserId: string },
): { ok: true } | { ok: false; reason: string } {
  if (envelope.version !== 2) return { ok: false, reason: "version" };
  if (!KNOWN_ACTIONS.has(envelope.action)) return { ok: false, reason: "action" };
  if (!envelope.occurrenceKey?.trim()) return { ok: false, reason: "occurrence" };
  if (!envelope.method?.trim() || !envelope.path?.trim()) return { ok: false, reason: "route" };
  if (!envelope.actorUserId?.trim()) return { ok: false, reason: "actor" };
  if (envelope.action !== "upload" && envelope.action !== "upload_add" && envelope.bodyText == null) {
    return { ok: false, reason: "body" };
  }
  if (
    (envelope.action === "upload" || envelope.action === "upload_add") &&
    (!envelope.uploadMeta?.digestHex || !envelope.uploadMeta.fileName) &&
    !(envelope.children[0]?.done && envelope.addBodyText && envelope.uploadedAssetId)
  ) {
    return { ok: false, reason: "upload_meta" };
  }
  if (current) {
    if (envelope.role !== current.role) return { ok: false, reason: "role_scope" };
    if (envelope.actorUserId !== current.actorUserId) return { ok: false, reason: "actor_scope" };
    // Seller session shop must match frozen shop; admin shopId is story target (may be platform null).
    if (envelope.role === "seller" && (envelope.shopId ?? null) !== (current.shopId ?? null)) {
      return { ok: false, reason: "shop_scope" };
    }
  }
  return { ok: true };
}

function samePayload(a: StoryIntentEnvelope, b: StoryIntentEnvelope): boolean {
  return (
    a.method === b.method &&
    a.path === b.path &&
    a.bodyText === b.bodyText &&
    a.addBodyText === b.addBodyText &&
    a.uploadedAssetId === b.uploadedAssetId &&
    JSON.stringify(a.uploadMeta) === JSON.stringify(b.uploadMeta) &&
    JSON.stringify(a.children) === JSON.stringify(b.children) &&
    a.role === b.role &&
    (a.shopId ?? null) === (b.shopId ?? null) &&
    a.actorUserId === b.actorUserId &&
    a.routeTargetId === b.routeTargetId &&
    a.expectedVersion === b.expectedVersion
  );
}

function storage(): Storage | null {
  try {
    const s = globalThis.sessionStorage;
    if (!s) return null;
    return s;
  } catch {
    return null;
  }
}

export function storeStoryIntent(envelope: StoryIntentEnvelope): boolean {
  const store = storage();
  if (!store) return false;
  const valid = validateStoryEnvelope(envelope);
  if (!valid.ok) return false;
  const key = storageKey(envelope.role, envelope.shopId, envelope.actorUserId, envelope.action, envelope.routeTargetId);
  try {
    const existing = store.getItem(key);
    if (existing) {
      try {
        const parsed = JSON.parse(existing) as StoryIntentEnvelope;
        if (parsed?.version === 2 && parsed.occurrenceKey) {
          if (parsed.occurrenceKey !== envelope.occurrenceKey) return false;
          if (!samePayload(parsed, envelope)) return false;
        } else {
          return false;
        }
      } catch {
        return false;
      }
    }
    const raw = JSON.stringify(envelope);
    store.setItem(key, raw);
    return store.getItem(key) === raw;
  } catch {
    return false;
  }
}

/**
 * Explicit child advancement (upload → add) — only from exact previous journal to next frozen state.
 * Does not allow arbitrary rewrite under the same key.
 */
export function advanceStoryUploadJournal(
  previous: StoryIntentEnvelope,
  next: StoryIntentEnvelope,
): boolean {
  if (!storage()) return false;
  if (previous.occurrenceKey !== next.occurrenceKey) return false;
  if (previous.action !== "upload_add" || next.action !== "upload_add") return false;
  if (previous.actorUserId !== next.actorUserId) return false;
  if ((previous.shopId ?? null) !== (next.shopId ?? null)) return false;
  if (previous.role !== next.role) return false;
  if (JSON.stringify(previous.uploadMeta) !== JSON.stringify(next.uploadMeta)) return false;
  if (!previous.children[0] || !next.children[0] || !next.children[1]) return false;
  if (previous.children[0].occurrenceKey !== next.children[0].occurrenceKey) return false;
  if (!next.children[0].done || !next.uploadedAssetId || !next.addBodyText) return false;
  const valid = validateStoryEnvelope(next);
  if (!valid.ok) return false;
  const key = storageKey(next.role, next.shopId, next.actorUserId, next.action, next.routeTargetId);
  const store = storage();
  if (!store) return false;
  try {
    const rawExisting = store.getItem(key);
    if (!rawExisting || rawExisting !== JSON.stringify(previous)) return false;
    const raw = JSON.stringify(next);
    store.setItem(key, raw);
    return store.getItem(key) === raw;
  } catch {
    return false;
  }
}

export function readStoryIntent(
  role: "admin" | "seller",
  shopId: string | null,
  actorUserId: string,
  action: StoryIntentAction,
  target: string | null,
): StoryIntentRead {
  const store = storage();
  if (!store) return { state: "unreadable", detail: "storage.unavailable" };
  try {
    const raw = store.getItem(storageKey(role, shopId, actorUserId, action, target));
    if (!raw) return { state: "missing" };
    const parsed = JSON.parse(raw) as StoryIntentEnvelope;
    if ((parsed as { version?: number })?.version === 1) return { state: "legacy_blocked", detail: "v1" };
    const valid = validateStoryEnvelope(parsed);
    if (!valid.ok) return { state: "unreadable", detail: valid.reason };
    return { state: "ok", envelope: parsed };
  } catch (err) {
    return { state: "unreadable", detail: err instanceof Error ? err.message : "storage.throw" };
  }
}

export function clearStoryIntentIfCurrent(envelope: StoryIntentEnvelope): void {
  const store = storage();
  if (!store) return;
  const key = storageKey(envelope.role, envelope.shopId, envelope.actorUserId, envelope.action, envelope.routeTargetId);
  try {
    const raw = store.getItem(key);
    if (raw && raw === JSON.stringify(envelope)) store.removeItem(key);
  } catch {
    /* ignore */
  }
}

export type PendingStoryIntent =
  | { state: "ok"; envelope: StoryIntentEnvelope }
  | { state: "blocked"; detail: string; rawKey: string };

export function listPendingStoryIntents(
  role: "admin" | "seller",
  shopId: string | null,
  actorUserId: string,
): PendingStoryIntent[] {
  try {
    const store = storage();
    if (!store) {
      return [{ state: "blocked", detail: "storage.unavailable", rawKey: "*" }];
    }
    if (!actorUserId?.trim()) {
      return [{ state: "blocked", detail: "actor.missing", rawKey: "*" }];
    }
    // Admin intents freeze story.sellerPartyId as shopId; session shop is platform null.
    // Enumerate every admin:{shop}:{actor}:… key for this actor so shop-target deletes recover.
    const prefix =
      role === "admin"
        ? `${STORAGE_PREFIX}${role}:`
        : `${STORAGE_PREFIX}${role}:${shopId ?? "platform"}:${actorUserId}:`;
    const adminActorSegment = `:${actorUserId}:`;
    const rows: PendingStoryIntent[] = [];
    const length = store.length;
    for (let i = 0; i < length; i++) {
      const key = store.key(i);
      if (!key) continue;
      if (LEGACY_PREFIXES.some((p) => key.startsWith(p))) {
        rows.push({ state: "blocked", detail: "legacy_namespace", rawKey: key });
        continue;
      }
      if (!key.startsWith(prefix)) {
        // Another actor/role/shop under the same v2 family — never expose contents.
        if (key.startsWith(STORAGE_PREFIX)) continue;
        continue;
      }
      if (role === "admin" && !key.includes(adminActorSegment)) continue;
      let raw: string | null;
      try {
        raw = store.getItem(key);
      } catch (err) {
        rows.push({
          state: "blocked",
          detail: err instanceof Error ? err.message : "storage.get.throw",
          rawKey: key,
        });
        continue;
      }
      if (!raw) continue;
      try {
        const parsed = JSON.parse(raw) as StoryIntentEnvelope;
        if ((parsed as { version?: number })?.version === 1) {
          rows.push({ state: "blocked", detail: "legacy_v1", rawKey: key });
          continue;
        }
        const valid = validateStoryEnvelope(
          parsed,
          role === "admin"
            ? { role, shopId: parsed.shopId ?? null, actorUserId }
            : { role, shopId, actorUserId },
        );
        if (!valid.ok) {
          rows.push({ state: "blocked", detail: valid.reason, rawKey: key });
          continue;
        }
        rows.push({ state: "ok", envelope: parsed });
      } catch {
        rows.push({ state: "blocked", detail: "malformed", rawKey: key });
      }
    }
    return rows;
  } catch (err) {
    return [
      {
        state: "blocked",
        detail: err instanceof Error ? err.message : "storage.enumerate.throw",
        rawKey: "*",
      },
    ];
  }
}

/** Merchant-facing label — never action enum / key fragments. */
export function pendingStoryLabel(envelope: StoryIntentEnvelope): string {
  switch (envelope.action) {
    case "create":
      return "ساخت پیش‌نویس هنوز تمام نشده";
    case "update":
      return "ذخیرهٔ عنوان هنوز تمام نشده";
    case "upload_add":
    case "upload":
    case "add_item":
      return "افزودن اسلاید هنوز تمام نشده";
    case "submit":
      return "ارسال برای تأیید هنوز تمام نشده";
    case "approve_publish":
      return "تأیید و انتشار هنوز تمام نشده";
    case "approve_schedule":
    case "schedule":
      return "زمان‌بندی هنوز تمام نشده";
    case "reject":
      return "رد استوری هنوز تمام نشده";
    case "stop":
      return "توقف نمایش هنوز تمام نشده";
    case "return_to_draft":
      return "بازگشت به پیش‌نویس هنوز تمام نشده";
    case "remove_item":
      return "حذف اسلاید هنوز تمام نشده";
    case "delete":
      return "حذف استوری هنوز تمام نشده";
    case "reorder_items":
      return "تغییر ترتیب هنوز تمام نشده";
    default:
      return "اقدام ناتمام";
  }
}
