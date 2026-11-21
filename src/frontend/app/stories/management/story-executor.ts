/**
 * Story-owned durable executor — dispatches frozen envelope method/path/body BYTES + frozen scope.
 * No arbitrary closure rebuilding body from live form state.
 */
import {
  advanceStoryUploadJournal,
  clearStoryIntentIfCurrent,
  digestFile,
  storeStoryIntent,
  validateStoryEnvelope,
  type StoryIntentEnvelope,
} from "./story-intent";
import { bindStoryEnvelopeRoute, isAuthoritativeStoryRefusal, verifyStorySuccessPayload } from "./story-response";
import { ADMIN_DEV_ACTOR_HEADER } from "../../admin/admin-api";
import { DEV_ACTOR_HEADER, SELLER_PARTY_HEADER } from "../../vendor-panel/seller-api";

export type StoryExecutorGate = {
  inFlightRef: { current: boolean };
  epochRef: { current: number };
  onIntentChanged?: () => void;
};

export type StoryExecutorScope = {
  role: "admin" | "seller";
  shopId: string | null;
  actorUserId: string;
};

export type StoryExecutorResult<T = unknown> =
  | { state: "ok"; data: T; envelope: StoryIntentEnvelope; status: number; retainIntent?: boolean }
  | { state: "blocked"; reason: "in_flight" | "storage" | "scope" | "stale_epoch" | "envelope" | "auth" }
  | { state: "failed"; message: string; envelope: StoryIntentEnvelope; status: number; code?: string };

function buildHeaders(envelope: StoryIntentEnvelope, jsonBody: boolean): HeadersInit {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (jsonBody) headers["Content-Type"] = "application/json";
  if (envelope.role === "seller") {
    if (envelope.shopId) headers[SELLER_PARTY_HEADER] = envelope.shopId;
    headers[DEV_ACTOR_HEADER] = envelope.actorUserId;
  } else {
    headers[ADMIN_DEV_ACTOR_HEADER] = envelope.actorUserId;
  }
  return headers;
}

function parseMessage(payload: unknown, fallback: string): string {
  if (!payload || typeof payload !== "object") return fallback;
  const record = payload as Record<string, unknown>;
  const errors = record.errors;
  if (Array.isArray(errors) && errors[0] && typeof errors[0] === "object") {
    const first = errors[0] as Record<string, unknown>;
    if (typeof first.message === "string" && first.message.trim()) return first.message;
    if (typeof first.code === "string" && first.code.trim()) return first.code;
  }
  if (typeof record.message === "string" && record.message.trim()) return record.message;
  return fallback;
}

function parseCode(payload: unknown): string | undefined {
  if (!payload || typeof payload !== "object") return undefined;
  const record = payload as Record<string, unknown>;
  if (typeof record.errorCode === "string" && record.errorCode.trim()) return record.errorCode.trim();
  const errors = record.errors;
  if (Array.isArray(errors) && errors[0] && typeof errors[0] === "object") {
    const first = errors[0] as Record<string, unknown>;
    if (typeof first.code === "string") return first.code;
  }
  return undefined;
}

/** Terminal HTTP refusal: the owning Story module evaluated the frozen envelope and refused it. */
function isTerminalRefusal(result: Extract<StoryExecutorResult, { state: "failed" }>): boolean {
  if (result.code === "story.transport.rejected" || result.code === "story.response.unverified") return false;
  const status = result.status;
  if (status < 400 || status >= 500) return false;
  // Request timeout / too-early / throttling are retryable-uncertain, not a verdict on the intent.
  if (status === 408 || status === 425 || status === 429) return false;
  // Only the Story module's own `story.*` namespace is authoritative; proxy/foreign codes stay uncertain.
  return Boolean(result.code && /^story\.[a-z][a-zA-Z0-9]*(?:\.[a-zA-Z0-9]+)*$/.test(result.code));
}

/**
 * Dispatch the frozen envelope. For upload_add after upload success, uses addBodyText on add path child.
 * Callers that still need FormData for unresolved upload pass uploadFile; never rebuild JSON from form.
 */
export async function runDurableStoryMutation<T = unknown>(args: {
  gate: StoryExecutorGate;
  envelope: StoryIntentEnvelope;
  scope: StoryExecutorScope;
  /** Required only when the unresolved upload step still needs original bytes. */
  uploadFile?: File | null;
  mapOk?: (payload: unknown) => T;
}): Promise<StoryExecutorResult<T>> {
  if (args.gate.inFlightRef.current) return { state: "blocked", reason: "in_flight" };

  const scopeCheck = validateStoryEnvelope(args.envelope, args.scope);
  if (!scopeCheck.ok) return { state: "blocked", reason: scopeCheck.reason === "route_binding" ? "envelope" : "scope" };
  const routeCheck = bindStoryEnvelopeRoute(args.envelope);
  if (!routeCheck.ok) return { state: "blocked", reason: "envelope" };

  if (!storeStoryIntent(args.envelope)) return { state: "blocked", reason: "storage" };
  args.gate.onIntentChanged?.();

  args.gate.inFlightRef.current = true;
  const started = args.gate.epochRef.current;
  try {
    const result = await dispatchFrozenEnvelope(args.envelope, args.uploadFile ?? null, args.mapOk);
    if (started !== args.gate.epochRef.current) return { state: "blocked", reason: "stale_epoch" };
    if (result.state === "ok") {
      if (!result.retainIntent) {
        clearStoryIntentIfCurrent(result.envelope);
        args.gate.onIntentChanged?.();
      }
      return result;
    }
    // A definite refusal keeps nothing to continue: replaying the exact bytes would be refused again,
    // and the leftover envelope made the next attempt look like a bogus "storage/unknown" outcome.
    if (result.state === "failed" && isTerminalRefusal(result)) {
      clearStoryIntentIfCurrent(result.envelope);
      args.gate.onIntentChanged?.();
    }
    return result;
  } finally {
    args.gate.inFlightRef.current = false;
  }
}

function buildAddFromJournal(
  journal: StoryIntentEnvelope,
  uploaded: { mediaAssetId: string; contentType: string; displayUrl: string },
  buildAddBody?: (uploaded: { mediaAssetId: string; contentType: string; displayUrl: string }) => {
    path: string;
    body: Record<string, unknown>;
  },
): { path: string; body: Record<string, unknown> } | null {
  if (buildAddBody) return buildAddBody(uploaded);
  if (!journal.bodyText || !journal.routeTargetId || !journal.children[1]) return null;
  try {
    const planned = JSON.parse(journal.bodyText) as Record<string, unknown>;
    const mediaType = uploaded.contentType.startsWith("video/") ? "video" : "image";
    const imageDurationMs =
      mediaType === "image"
        ? Math.max(1, Number(planned.durationSec ?? planned.durationMs ?? 5) || 5) *
          (planned.durationMs ? 1 : 1000)
        : null;
    const body = {
      mediaType,
      mediaAssetId: uploaded.mediaAssetId,
      mediaUrl: uploaded.displayUrl,
      caption: typeof planned.caption === "string" ? planned.caption : null,
      durationMs: mediaType === "image" ? (planned.durationMs ?? imageDurationMs) : null,
      ctaType: planned.ctaType ?? "none",
      ctaTarget: planned.ctaTarget ?? null,
      expectedVersion: planned.expectedVersion ?? journal.expectedVersion,
      occurrenceKey: journal.children[1].occurrenceKey,
    };
    const path =
      journal.role === "seller"
        ? `/v1/seller/stories/${journal.routeTargetId}/items`
        : `/v1/admin/stories/${journal.routeTargetId}/items`;
    return { path, body };
  } catch {
    return null;
  }
}

/**
 * Staged media replacement: same upload→continuation journal as `upload_add`, but the continuation
 * is the owning `update_item` PUT for the SAME slide. The old media stays authoritative until both
 * the upload and the update complete; a lost upload resumes that exact file, a lost update retries
 * ONLY the frozen update bytes.
 */
function buildReplaceFromJournal(
  journal: StoryIntentEnvelope,
  uploaded: { mediaAssetId: string; contentType: string; displayUrl: string },
): { path: string; body: Record<string, unknown> } | null {
  if (!journal.bodyText || !journal.routeTargetId || !journal.targetItemId || !journal.children[1]) return null;
  try {
    const planned = JSON.parse(journal.bodyText) as Record<string, unknown>;
    const mediaType = uploaded.contentType.startsWith("video/") ? "video" : "image";
    const rawDuration = Number(planned.durationSec ?? planned.durationMs ?? 5);
    const durationSec = Number.isFinite(rawDuration) ? Math.max(1, Math.round(rawDuration)) : 5;
    const body = {
      mediaType,
      mediaAssetId: uploaded.mediaAssetId,
      mediaUrl: uploaded.displayUrl,
      caption: typeof planned.caption === "string" ? planned.caption : null,
      durationMs: mediaType === "image" ? durationSec * 1000 : null,
      ctaType: planned.ctaType ?? "none",
      ctaTarget: planned.ctaTarget ?? null,
      expectedVersion: planned.expectedVersion ?? journal.expectedVersion,
      occurrenceKey: journal.children[1].occurrenceKey,
    };
    const root = journal.role === "seller" ? "/v1/seller/stories" : "/v1/admin/stories";
    return { path: `${root}/${journal.routeTargetId}/items/${journal.targetItemId}`, body };
  } catch {
    return null;
  }
}

/** Upload then add — advances journal with asset id + frozen add body; after unknown add retries ONLY add. */
export async function runDurableUploadAdd(args: {
  gate: StoryExecutorGate;
  envelope: StoryIntentEnvelope;
  scope: StoryExecutorScope;
  uploadFile?: File | null;
  buildAddBody?: (uploaded: { mediaAssetId: string; contentType: string; displayUrl: string }) => {
    path: string;
    body: Record<string, unknown>;
  };
}): Promise<StoryExecutorResult> {
  if (args.gate.inFlightRef.current) return { state: "blocked", reason: "in_flight" };
  if (args.envelope.action !== "upload_add" && args.envelope.action !== "replace_media") {
    return { state: "blocked", reason: "envelope" };
  }

  // Mutex BEFORE async digest.
  args.gate.inFlightRef.current = true;
  const started = args.gate.epochRef.current;
  try {
    let journal = args.envelope;
    const scopeCheck = validateStoryEnvelope(journal, args.scope);
    if (!scopeCheck.ok) return { state: "blocked", reason: "scope" };
    const uploadRoute = bindStoryEnvelopeRoute(journal);
    if (!uploadRoute.ok) return { state: "blocked", reason: "envelope" };

    if (!journal.children[0]?.done || !journal.addBodyText || !journal.uploadedAssetId) {
      if (!args.uploadFile) {
        return {
          state: "failed",
          message: "همان فایل قبلی را دوباره انتخاب کنید تا ادامه داده شود.",
          envelope: journal,
          status: 0,
          code: "story.upload.file_required",
        };
      }
      const digest = await digestFile(args.uploadFile);
      if (started !== args.gate.epochRef.current) return { state: "blocked", reason: "stale_epoch" };
      journal = {
        ...journal,
        uploadMeta: {
          fileName: args.uploadFile.name,
          fileType: args.uploadFile.type,
          fileSize: args.uploadFile.size,
          digestHex: digest,
        },
      };
      if (!storeStoryIntent(journal)) return { state: "blocked", reason: "storage" };
      args.gate.onIntentChanged?.();
      const uploadBind = bindStoryEnvelopeRoute(journal);
      if (!uploadBind.ok) return { state: "blocked", reason: "envelope" };
      const uploaded = await dispatchFrozenEnvelope(journal, args.uploadFile);
      if (started !== args.gate.epochRef.current) return { state: "blocked", reason: "stale_epoch" };
      if (uploaded.state !== "ok") return uploaded;
      const payload = uploaded.data as {
        mediaAssetId?: string;
        MediaAssetId?: string;
        contentType?: string;
        ContentType?: string;
        displayUrl?: string;
        DisplayUrl?: string;
        data?: Record<string, unknown>;
      };
      const root = payload?.data && typeof payload.data === "object" ? payload.data : payload;
      const mediaAssetId = String(root.mediaAssetId ?? root.MediaAssetId ?? "").trim();
      const contentType = String(root.contentType ?? root.ContentType ?? "").trim();
      const displayUrl = String(root.displayUrl ?? root.DisplayUrl ?? "").trim();
      if (!mediaAssetId || !contentType) {
        return {
          state: "failed",
          message: "پاسخ آپلود ناقص بود؛ اقدام ناتمام ماند.",
          envelope: journal,
          status: uploaded.status,
        };
      }
      const built =
        journal.action === "replace_media"
          ? buildReplaceFromJournal(journal, { mediaAssetId, contentType, displayUrl })
          : buildAddFromJournal(journal, { mediaAssetId, contentType, displayUrl }, args.buildAddBody);
      if (!built) {
        return {
          state: "failed",
          message: "اطلاعات اسلاید برای ادامه کافی نیست.",
          envelope: journal,
          status: 0,
        };
      }
      const advanced: StoryIntentEnvelope = {
        ...journal,
        children: [
          { ...journal.children[0]!, done: true },
          journal.children[1]!,
        ],
        uploadedAssetId: mediaAssetId,
        addBodyText: JSON.stringify(built.body),
        method: journal.action === "replace_media" ? "PUT" : "POST",
        path: built.path,
        bodyText: JSON.stringify(built.body),
      };
      if (!advanceStoryUploadJournal(journal, advanced)) {
        // If storage already matches advanced (replay), continue; else fail closed retaining evidence.
        const same =
          journal.children[0]?.done &&
          journal.uploadedAssetId === mediaAssetId &&
          journal.addBodyText === JSON.stringify(built.body);
        if (!same) {
          return {
            state: "failed",
            message: "ثبت مرحلهٔ بعد از آپلود ممکن نشد؛ فایل دوباره ارسال نشد.",
            envelope: journal,
            status: 0,
          };
        }
      }
      journal = advanced;
      args.gate.onIntentChanged?.();
    } else if (!storeStoryIntent(journal)) {
      return { state: "blocked", reason: "storage" };
    } else {
      args.gate.onIntentChanged?.();
    }

    const addBind = bindStoryEnvelopeRoute(journal);
    if (!addBind.ok) return { state: "blocked", reason: "envelope" };
    const addResult = await dispatchFrozenEnvelope(journal, null);
    if (started !== args.gate.epochRef.current) return { state: "blocked", reason: "stale_epoch" };
    if (addResult.state === "ok") {
      clearStoryIntentIfCurrent(journal);
      args.gate.onIntentChanged?.();
      return addResult;
    }
    if (addResult.state === "failed") {
      if (isTerminalRefusal(addResult)) {
        clearStoryIntentIfCurrent(journal);
        args.gate.onIntentChanged?.();
      }
      return { state: "failed", message: addResult.message, envelope: journal, status: addResult.status, code: addResult.code };
    }
    return addResult;
  } finally {
    args.gate.inFlightRef.current = false;
  }
}

async function dispatchFrozenEnvelope<T>(
  envelope: StoryIntentEnvelope,
  uploadFile: File | null,
  mapOk?: (payload: unknown) => T,
): Promise<StoryExecutorResult<T>> {
  // Final send-boundary bind for every stage (upload, add, update-item, recovery).
  const route = bindStoryEnvelopeRoute(envelope);
  if (!route.ok) {
    return { state: "blocked", reason: "envelope" };
  }

  // Staged continuation: upload child done and the next body frozen → send ONLY that continuation
  // (add-item POST or replace-media PUT); never re-upload the bytes.
  const staged = envelope.action === "upload_add" || envelope.action === "replace_media";
  if (staged && envelope.children[0]?.done && envelope.addBodyText && envelope.uploadedAssetId) {
    if (envelope.bodyText == null) return { state: "blocked", reason: "envelope" };
    const continuation: StoryIntentEnvelope = {
      ...envelope,
      method: envelope.method,
      path: envelope.path,
      bodyText: envelope.addBodyText,
    };
    const continuationRoute = bindStoryEnvelopeRoute(continuation);
    if (!continuationRoute.ok) return { state: "blocked", reason: "envelope" };
    return sendJsonRequest(continuation, envelope.method, envelope.path, envelope.addBodyText, mapOk);
  }

  if (envelope.action === "upload" || staged) {
    if (!uploadFile) {
      return {
        state: "failed",
        message: "همان فایل قبلی را دوباره انتخاب کنید تا ادامه داده شود.",
        envelope,
        status: 0,
        code: "story.upload.file_required",
      };
    }
    if (
      uploadFile.name !== envelope.uploadMeta?.fileName ||
      uploadFile.size !== envelope.uploadMeta.fileSize ||
      uploadFile.type !== envelope.uploadMeta.fileType
    ) {
      return {
        state: "failed",
        message: "فایل انتخاب‌شده با اقدام ناتمام یکی نیست.",
        envelope,
        status: 0,
        code: "story.upload.file_mismatch",
      };
    }
    const form = new FormData();
    form.append("file", uploadFile);
    form.append("occurrenceKey", envelope.occurrenceKey);
    if (envelope.role === "admin" && envelope.shopId && envelope.path.includes("upload-for-shop")) {
      form.append("sellerPartyId", envelope.shopId);
    }
    try {
      const response = await fetch(envelope.path, {
        method: envelope.method || "POST",
        headers: buildHeaders(envelope, false),
        body: form,
      });
      return finalizeResponse(envelope, response, mapOk);
    } catch (err) {
      return transportRejectionResult(envelope, err);
    }
  }

  if (envelope.bodyText == null) {
    return { state: "blocked", reason: "envelope" };
  }
  return sendJsonRequest(envelope, envelope.method, envelope.path, envelope.bodyText, mapOk);
}

/** Connection loss / rejected fetch — uncertain, keep exact persisted envelope. */
function transportRejectionResult<T>(
  envelope: StoryIntentEnvelope,
  err: unknown,
): StoryExecutorResult<T> {
  const isNetwork =
    err instanceof TypeError ||
    (err instanceof Error && /failed to fetch|network|load failed|econn|abort/i.test(err.message));
  if (!isNetwork && !(err instanceof Error)) {
    // Non-Error throw: still uncertain at the wire boundary (do not invent success).
  }
  return {
    state: "failed",
    message: "ارتباط قطع شد؛ نتیجه نامشخص است و اقدام قابل ادامه است.",
    envelope,
    status: 0,
    code: "story.transport.rejected",
  };
}

async function sendJsonRequest<T>(
  envelope: StoryIntentEnvelope,
  method: string,
  path: string,
  bodyText: string,
  mapOk?: (payload: unknown) => T,
): Promise<StoryExecutorResult<T>> {
  const wired: StoryIntentEnvelope = { ...envelope, method, path, bodyText };
  const route = bindStoryEnvelopeRoute(wired);
  if (!route.ok) return { state: "blocked", reason: "envelope" };
  try {
    const response = await fetch(path, {
      method,
      headers: buildHeaders(envelope, true),
      body: bodyText,
    });
    return finalizeResponse(envelope, response, mapOk);
  } catch (err) {
    return transportRejectionResult(envelope, err);
  }
}

async function finalizeResponse<T>(
  envelope: StoryIntentEnvelope,
  response: Response,
  mapOk?: (payload: unknown) => T,
): Promise<StoryExecutorResult<T>> {
  try {
    if (response.status === 401 || response.status === 403) {
      return { state: "blocked", reason: "auth" };
    }
    let payload: unknown = undefined;
    let parseFailed = false;
    const text = await response.text();
    if (!text || !text.trim()) {
      if (response.ok) {
        return {
          state: "failed",
          message: "پاسخ سرور قابل تأیید نبود؛ اقدام ناتمام ماند.",
          envelope,
          status: response.status,
          code: "story.response.unverified",
        };
      }
    } else {
      try {
        payload = JSON.parse(text);
      } catch {
        parseFailed = true;
        payload = undefined;
      }
    }
    if (!response.ok) {
      // Only a parsed owning Story refusal (4xx ProblemDetails with a `story.*` errorCode) is an
      // authoritative no-effect verdict. An HTML/proxy/gateway body, a foreign code or an unknown
      // shape must stay uncertain: it may have been produced after the command ran, so erasing the
      // frozen intent would hide that.
      const code = parseCode(payload);
      if (parseFailed || !code || !isAuthoritativeStoryRefusal(response.status, payload)) {
        return {
          state: "failed",
          message: "پاسخ سرور قابل تأیید نبود؛ نتیجه نامشخص است و اقدام قابل ادامه است.",
          envelope,
          status: response.status,
          code: "story.response.unverified",
        };
      }
      return {
        state: "failed",
        message: parseMessage(payload, "اقدام ممکن نشد."),
        envelope,
        status: response.status,
        code,
      };
    }
    if (response.status < 200 || response.status >= 300) {
      return {
        state: "failed",
        message: "پاسخ نامعتبر از سرور.",
        envelope,
        status: response.status,
        code: "story.response.unverified",
      };
    }
    if (parseFailed || payload === undefined) {
      return {
        state: "failed",
        message: "پاسخ سرور قابل تأیید نبود؛ اقدام ناتمام ماند.",
        envelope,
        status: response.status,
        code: "story.response.unverified",
      };
    }
    const verified = verifyStorySuccessPayload(envelope, payload, mapOk as ((p: unknown) => unknown) | undefined);
    if (!verified.ok) {
      return {
        state: "failed",
        message: "پاسخ سرور با اقدام انجام‌شده هم‌خوان نبود؛ اقدام ناتمام ماند.",
        envelope,
        status: response.status,
        code: "story.response.unverified",
      };
    }
    return { state: "ok", data: verified.data as T, envelope, status: response.status };
  } catch {
    return {
      state: "failed",
      message: "خواندن پاسخ ممکن نشد؛ اقدام ناتمام ماند.",
      envelope,
      status: response.status || 0,
      code: "story.response.unverified",
    };
  }
}

/** Recovery: dispatch stored envelope even when detail GET failed. */
export async function replayPendingStoryIntent<T = unknown>(args: {
  gate: StoryExecutorGate;
  envelope: StoryIntentEnvelope;
  scope: StoryExecutorScope;
  uploadFile?: File | null;
  mapOk?: (payload: unknown) => T;
}): Promise<StoryExecutorResult<T>> {
  if (args.envelope.action === "upload_add" || args.envelope.action === "replace_media") {
    return runDurableUploadAdd({
      gate: args.gate,
      envelope: args.envelope,
      scope: args.scope,
      uploadFile: args.uploadFile,
    }) as Promise<StoryExecutorResult<T>>;
  }
  return runDurableStoryMutation(args);
}
