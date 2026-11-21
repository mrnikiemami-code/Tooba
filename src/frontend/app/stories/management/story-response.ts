/**
 * Action-specific success binding against the real Story/Media wire.
 * HTTP 2xx alone is never completion.
 */
import type { StoryIntentAction, StoryIntentEnvelope } from "./story-intent";
import { mapAdminStory } from "../story-api";

const GUID_RE =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function unwrapData(payload: unknown): Record<string, unknown> | null {
  if (!payload || typeof payload !== "object") return null;
  const record = payload as Record<string, unknown>;
  if (record.data && typeof record.data === "object" && !Array.isArray(record.data)) {
    return record.data as Record<string, unknown>;
  }
  return record;
}

function readGuid(record: Record<string, unknown>, ...keys: string[]): string | null {
  for (const key of keys) {
    const raw = record[key];
    if (typeof raw === "string" && GUID_RE.test(raw.trim())) return raw.trim();
  }
  return null;
}

export type StorySuccessVerification =
  | { ok: true; data: unknown }
  | { ok: false; reason: string };

/** Verify a parsed 2xx payload is a genuine bound Story/Media completion for this envelope. */
export function verifyStorySuccessPayload(
  envelope: StoryIntentEnvelope,
  payload: unknown,
  mapOk?: (payload: unknown) => unknown,
): StorySuccessVerification {
  if (payload === undefined || payload === null) {
    return { ok: false, reason: "empty_payload" };
  }
  if (typeof payload !== "object") {
    return { ok: false, reason: "non_object_payload" };
  }

  let mapped: unknown = payload;
  if (mapOk) {
    try {
      mapped = mapOk(payload);
    } catch {
      return { ok: false, reason: "mapper_throw" };
    }
    if (mapped === null || mapped === undefined) {
      return { ok: false, reason: "mapper_null" };
    }
  }

  const root = unwrapData(mapped) ?? unwrapData(payload);
  if (!root) return { ok: false, reason: "unwrap_failed" };

  switch (envelope.action) {
    case "create":
      return verifyCreate(envelope, root, mapped);
    case "update":
    case "submit":
    case "approve":
    case "approve_publish":
    case "approve_schedule":
    case "reject":
    case "schedule":
    case "publish_now":
    case "stop":
    case "return_to_draft":
    case "add_item":
    case "update_item":
    case "remove_item":
    case "delete":
    case "reorder_items":
      return verifyStoryTarget(envelope, root, mapped);
    case "replace_media":
      // Stage 1 answers the Media receipt; stage 2 answers the owning Story snapshot.
      return envelope.children[0]?.done
        ? verifyStoryTarget(envelope, root, mapped)
        : verifyUploadOrAdd(envelope, root, mapped);
    case "upload":
    case "upload_add":
      return verifyUploadOrAdd(envelope, root, mapped);
    default:
      return { ok: false, reason: "unknown_action" };
  }
}

function verifyCreate(
  envelope: StoryIntentEnvelope,
  root: Record<string, unknown>,
  mapped: unknown,
): StorySuccessVerification {
  const storyId = readGuid(root, "storyId", "StoryId", "id", "Id");
  if (!storyId) return { ok: false, reason: "create_missing_story_id" };
  if (envelope.role === "seller" && envelope.shopId) {
    const shop = readGuid(root, "sellerPartyId", "SellerPartyId");
    // Some create replies omit shop; if present it must match frozen shop.
    if (shop && shop.toLowerCase() !== envelope.shopId.toLowerCase()) {
      return { ok: false, reason: "create_shop_mismatch" };
    }
  }
  return { ok: true, data: mapped };
}

function verifyStoryTarget(
  envelope: StoryIntentEnvelope,
  root: Record<string, unknown>,
  mapped: unknown,
): StorySuccessVerification {
  // Boolean true replies are not bound evidence for Story mutations.
  if (typeof mapped === "boolean") return { ok: false, reason: "boolean_without_story" };
  const snap = mapAdminStory(root) ?? mapAdminStory(mapped);
  const storyId = readGuid(root, "storyId", "StoryId") ?? snap?.storyId ?? null;
  if (!storyId) return { ok: false, reason: "missing_story_id" };
  if (envelope.routeTargetId && storyId.toLowerCase() !== envelope.routeTargetId.toLowerCase()) {
    return { ok: false, reason: "story_target_mismatch" };
  }

  // A version the reply carries must not regress behind the version the frozen command targeted.
  // A reply that carries no version field must not be read as version 0 from the mapper default.
  const version = readInt(root, "versionToken", "VersionToken")
    ?? (carries(root, "versionToken", "VersionToken") ? snap?.versionToken ?? null : null);
  if (envelope.expectedVersion != null && version != null && version < envelope.expectedVersion) {
    return { ok: false, reason: "story_version_regressed" };
  }

  // Deletes are tombstones/replays: identity (and version) is the whole contract the wire owns.
  if (envelope.action === "delete") return { ok: true, data: mapped };

  const effect = requiredEffect(envelope.action);
  if (!effect) return { ok: true, data: mapped };

  // Content edits own no assertable state beyond the bound snapshot identity/version above.
  const effective = effectSatisfied(effect, root, snap);
  if (effective.ok) return { ok: true, data: mapped };
  return { ok: false, reason: effective.reason };
}

function readInt(record: Record<string, unknown>, ...keys: string[]): number | null {
  for (const key of keys) {
    const raw = record[key];
    if (typeof raw === "number" && Number.isFinite(raw)) return raw;
    if (typeof raw === "string" && /^\d+$/.test(raw.trim())) return Number(raw.trim());
  }
  return null;
}

function carries(record: Record<string, unknown>, ...keys: string[]): boolean {
  return keys.some((key) => Object.prototype.hasOwnProperty.call(record, key));
}

type StoryEffect = {
  /** Review state the owning command must leave behind, when it owns one. */
  review?: "Approved" | "Rejected" | "Submitted";
  /** Publication state the owning command must leave behind. */
  status?: "Active" | "Scheduled" | "Disabled" | "Draft";
  /** publish_now may also satisfy the effect through effective public eligibility. */
  acceptPublic?: boolean;
};

/**
 * The state each reviewed/publication command actually owns in the real endpoint contract
 * (`AdminStorySnapshot` returned by every Story mutation). Content-only edits own no state.
 */
function requiredEffect(action: StoryIntentEnvelope["action"]): StoryEffect | null {
  switch (action) {
    case "approve":
      return { review: "Approved" };
    case "reject":
      return { review: "Rejected" };
    case "submit":
      return { review: "Submitted" };
    case "approve_publish":
      return { review: "Approved", status: "Active", acceptPublic: true };
    case "approve_schedule":
      return { review: "Approved", status: "Scheduled" };
    case "publish_now":
      return { status: "Active", acceptPublic: true };
    case "schedule":
      return { status: "Scheduled" };
    case "stop":
      return { status: "Disabled" };
    case "return_to_draft":
      return { status: "Draft" };
    default:
      return null;
  }
}

/**
 * A 2xx reply must show the effect the frozen action owns. A same-id unchanged Draft
 * (e.g. approve returning the untouched row) or a reply that carries no state at all is NOT
 * completion — the owning Story mutations always return `AdminStorySnapshot`, so missing state
 * means the response shape cannot prove the effect and the command stays uncertain/replayable.
 */
function effectSatisfied(
  effect: StoryEffect,
  root: Record<string, unknown>,
  snap: NonNullable<ReturnType<typeof mapAdminStory>> | null,
): { ok: true } | { ok: false; reason: string } {
  const carriesReview = carries(root, "reviewStatus", "ReviewStatus");
  const carriesStatus = carries(root, "status", "Status");
  const carriesPublic = carries(root, "effectivelyPublic", "EffectivelyPublic");

  if (!snap) return { ok: false, reason: "unverified_shape" };

  if (effect.review) {
    if (!carriesReview) return { ok: false, reason: "unverified_shape" };
    if (snap.reviewStatus !== effect.review) return { ok: false, reason: "story_state_unchanged" };
  }

  if (effect.status) {
    const publicOk = Boolean(effect.acceptPublic && carriesPublic && snap.effectivelyPublic);
    if (!carriesStatus) {
      if (publicOk) return { ok: true };
      return { ok: false, reason: "unverified_shape" };
    }
    if (snap.status !== effect.status && !publicOk) {
      return { ok: false, reason: "story_state_unchanged" };
    }
  }

  return { ok: true };
}

function verifyUploadOrAdd(
  envelope: StoryIntentEnvelope,
  root: Record<string, unknown>,
  mapped: unknown,
): StorySuccessVerification {
  // After add child: Story snapshot or item identity.
  if (envelope.action === "upload_add" && envelope.children[0]?.done && envelope.addBodyText) {
    const storyId = readGuid(root, "storyId", "StoryId");
    const itemId = readGuid(root, "storyItemId", "StoryItemId");
    if (storyId) {
      if (envelope.routeTargetId && storyId !== envelope.routeTargetId) {
        return { ok: false, reason: "add_story_mismatch" };
      }
      return { ok: true, data: mapped };
    }
    if (itemId) return { ok: true, data: mapped };
    return { ok: false, reason: "add_missing_identity" };
  }

  const mediaAssetId = readGuid(root, "mediaAssetId", "MediaAssetId");
  const contentType = String(root.contentType ?? root.ContentType ?? "").trim();
  const status = String(root.status ?? root.Status ?? root.state ?? root.State ?? "Ready").trim();
  if (!mediaAssetId) return { ok: false, reason: "upload_missing_asset" };
  if (!contentType) return { ok: false, reason: "upload_missing_type" };
  if (status && !/ready|uploaded|available/i.test(status) && status !== "1") {
    // Allow missing/Ready; refuse explicit failure states.
    if (/fail|reject|error|pending/i.test(status)) {
      return { ok: false, reason: "upload_not_ready" };
    }
  }
  return { ok: true, data: mapped };
}

/**
 * The Story module owns the `story.*` error namespace (`StoryErrorCodes` + `StoryValidators`).
 * Only a parsed 4xx ProblemDetails carrying one of those codes is an authoritative no-effect
 * refusal. HTML error pages, proxy/gateway bodies, foreign-module codes, 408/425/429 and any
 * ambiguous response stay uncertain: they can be produced after the command already ran.
 */
const STORY_OWNING_CODE = /^story\.[a-z][a-zA-Z0-9]*(?:\.[a-zA-Z0-9]+)*$/;

export function isAuthoritativeStoryRefusal(status: number, payload: unknown): boolean {
  if (!Number.isFinite(status) || status < 400 || status >= 500) return false;
  // Retryable/timeout classes never prove the server evaluated the intent.
  if (status === 408 || status === 425 || status === 429) return false;
  if (!payload || typeof payload !== "object") return false;
  const record = payload as Record<string, unknown>;
  const rootCode = typeof record.errorCode === "string" ? record.errorCode.trim() : "";
  if (rootCode && STORY_OWNING_CODE.test(rootCode)) return true;
  const errors = record.errors;
  if (Array.isArray(errors)) {
    for (const entry of errors) {
      if (!entry || typeof entry !== "object") continue;
      const code = (entry as Record<string, unknown>).code;
      if (typeof code === "string" && STORY_OWNING_CODE.test(code.trim())) return true;
    }
  }
  return false;
}

/** Known Story/Media route binding — nonempty strings alone are not enough. */
export function bindStoryEnvelopeRoute(
  envelope: StoryIntentEnvelope,
): { ok: true } | { ok: false; reason: string } {
  const method = envelope.method.trim().toUpperCase();
  const path = envelope.path.trim();
  const role = envelope.role;
  const action = envelope.action;

  const expect = (m: string, re: RegExp) => method === m && re.test(path);

  const sellerStory = /^\/v1\/seller\/stories(?:\/|$)/;
  const adminStory = /^\/v1\/admin\/stories(?:\/|$)/;
  // Anchored: reject appended/foreign path suffixes (e.g. …/upload-story/../other).
  const mediaUpload =
    role === "seller"
      ? /^\/v1\/seller\/media\/?$/
      : /^\/v1\/admin\/media\/upload-(?:story-platform|for-shop)\/?$/;

  const storyRoot = role === "seller" ? sellerStory : adminStory;
  if (
    !storyRoot.test(path) &&
    !mediaUpload.test(path) &&
    action !== "upload" &&
    action !== "upload_add" &&
    action !== "replace_media"
  ) {
    return { ok: false, reason: "foreign_module_path" };
  }

  const guid = "[0-9a-fA-F-]{36}";
  const ok = ((): boolean => {
    switch (action as StoryIntentAction) {
      case "create":
        return expect("POST", role === "seller" ? /^\/v1\/seller\/stories\/?$/ : /^\/v1\/admin\/stories\/?$/);
      case "update":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("PUT", new RegExp(`^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "submit":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/submit/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "stop":
        // The owning stop command is role-specific: seller /stop, admin /disable (AdminDisableAsync).
        // Accepting only the seller route made every admin stop a client-side "route_binding" failure.
        return (
          Boolean(envelope.routeTargetId) &&
          expect(
            "POST",
            role === "seller"
              ? new RegExp(`^/v1/seller/stories/${guid}/stop/?$`)
              : new RegExp(`^/v1/admin/stories/${guid}/disable/?$`),
          ) &&
          path.includes(envelope.routeTargetId!)
        );
      case "return_to_draft":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/seller/stories/${guid}/return-to-draft/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "approve":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/admin/stories/${guid}/approve/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "approve_publish":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/admin/stories/${guid}/approve-and-publish/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "approve_schedule":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/admin/stories/${guid}/approve-and-schedule/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "schedule":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/admin/stories/${guid}/schedule/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "publish_now":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/admin/stories/${guid}/enable/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "reject":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/admin/stories/${guid}/reject/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "add_item":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("POST", new RegExp(`^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/items/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "remove_item":
        return (
          Boolean(envelope.routeTargetId) &&
          expect(
            "DELETE",
            new RegExp(`^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/items/${guid}/?$`),
          ) &&
          path.includes(envelope.routeTargetId!)
        );
      case "delete":
        return (
          Boolean(envelope.routeTargetId) &&
          expect("DELETE", new RegExp(`^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/?$`)) &&
          path.includes(envelope.routeTargetId!)
        );
      case "reorder_items":
        return (
          Boolean(envelope.routeTargetId) &&
          expect(
            "PUT",
            new RegExp(`^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/items/reorder/?$`),
          ) &&
          path.includes(envelope.routeTargetId!)
        );
      case "upload":
        return expect("POST", mediaUpload);
      case "upload_add": {
        if (envelope.children[0]?.done && envelope.addBodyText) {
          return (
            Boolean(envelope.routeTargetId) &&
            expect("POST", new RegExp(`^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/items/?$`)) &&
            path.includes(envelope.routeTargetId!)
          );
        }
        return expect("POST", mediaUpload);
      }
      case "replace_media": {
        // Stage 1 is the same platform media upload; stage 2 is the owning update_item PUT for the
        // SAME slide, so the old media stays authoritative until both steps have completed.
        if (envelope.children[0]?.done && envelope.addBodyText) {
          return (
            Boolean(envelope.routeTargetId) &&
            Boolean(envelope.targetItemId) &&
            expect(
              "PUT",
              new RegExp(`^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/items/${guid}/?$`),
            ) &&
            path.includes(envelope.routeTargetId!) &&
            path.includes(envelope.targetItemId!)
          );
        }
        return expect("POST", mediaUpload);
      }
      case "update_item":
        return (
          Boolean(envelope.routeTargetId) &&
          expect(
            "PUT",
            new RegExp(
              `^/v1/${role === "seller" ? "seller" : "admin"}/stories/${guid}/items/${guid}/?$`,
            ),
          ) &&
          path.includes(envelope.routeTargetId!)
        );
      default:
        return false;
    }
  })();

  return ok ? { ok: true } : { ok: false, reason: "route_binding" };
}
