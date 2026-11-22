/**
 * کلاینت عمومی/ادمین Story — فقط دادهٔ زندهٔ Host.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import type { GridServerQuery } from "../../design-system/data-grid/types.ts";
import { postAdminGridQuery, type AdminGridQueryResult } from "../../design-system/app-data-grid/admin-grid-query-client.ts";
import { toHostGridQuery } from "../../design-system/app-data-grid/grid-query-mapper.ts";
import {
  DEV_ACTOR_HEADER,
  readActorUserId,
  readSellerPartyId,
  SELLER_PARTY_HEADER,
  SELLER_PARTY_STORAGE_KEY,
} from "../vendor-panel/seller-api.ts";

/**
 * Reuse the frozen occurrence for the same reviewed act.
 * Never mint a new key here — callers use newStoryOccurrenceKey() only for a deliberately NEW act.
 */
export function storyOccurrenceKey(existing?: string | null): string {
  const trimmed = existing?.trim();
  if (!trimmed) {
    throw new Error("story.occurrence.required");
  }
  return trimmed;
}

export function newStoryOccurrenceKey(): string {
  return crypto.randomUUID();
}

export interface PublicStoryItem {
  storyItemId: string;
  mediaType: string;
  mediaUrl: string | null;
  caption: string | null;
  durationMs: number | null;
  ctaType: string;
  ctaTarget: string | null;
  /** Catalog-resolved public path for product CTA (never a bare Guid). */
  ctaHref?: string | null;
}

export interface PublicStoryCard {
  storyId: string;
  title: string;
  coverMediaUrl: string | null;
  isVideo: boolean;
  displayOrder: number;
  ctaType: string;
  ctaTarget: string | null;
  items: PublicStoryItem[];
  sellerPartyId?: string | null;
  shopDisplayName?: string | null;
  shopPublicId?: string | null;
  ctaHref?: string | null;
  /** Publication window end from the owning Story; null when the window has no end. */
  endAt?: string | null;
  /** Explicit cover kind from the owning Media asset; null when the owner did not answer. */
  coverIsVideo?: boolean | null;
  /** Store-preview fill only — never persisted / never published. */
  previewFake?: boolean;
  /** In-memory PreviewFake source marker — never persisted / never shown as jargon in UI. */
  previewSource?: "PreviewFake";
}

export interface AdminStoryItem {
  storyItemId: string;
  displayOrder: number;
  mediaType: string;
  mediaAssetId: string | null;
  mediaUrl: string | null;
  caption: string | null;
  durationMs: number | null;
  ctaType: string;
  ctaTarget: string | null;
  /** Owning product title for product CTA when Catalog resolved it; null = unavailable. */
  ctaProductTitle?: string | null;
  ctaHref?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface AdminStorySnapshot {
  storyId: string;
  tenantId: string;
  origin: string;
  reviewStatus: string;
  sellerPartyId: string | null;
  rejectionReason: string | null;
  submittedAt: string | null;
  reviewedAt: string | null;
  submittedByActorUserId: string | null;
  reviewedByActorUserId: string | null;
  locale: string | null;
  market: string | null;
  title: string;
  coverMediaAssetId: string | null;
  coverMediaUrl: string | null;
  displayOrder: number;
  startAt: string | null;
  endAt: string | null;
  status: string;
  ctaType: string;
  ctaTarget: string | null;
  versionToken: number;
  createdAt: string;
  updatedAt: string;
  items: AdminStoryItem[];
  /** False when the server response omitted `items`; an empty array alone does not prove deletion. */
  itemsPresent?: boolean;
  id: string;
  createdByActorUserId: string | null;
  shopDisplayName: string | null;
  contentReady: boolean;
  effectivelyPublic: boolean;
  actionCodes: string[];
}

const STORY_STATUS_NAMES: Record<number, string> = {
  0: "Draft",
  1: "Scheduled",
  2: "Active",
  3: "Expired",
  4: "Disabled",
};

const STORY_ORIGIN_NAMES: Record<number, string> = {
  0: "Admin",
  1: "Seller",
};

const STORY_REVIEW_STATUS_NAMES: Record<number, string> = {
  0: "None",
  1: "Submitted",
  2: "Approved",
  3: "Rejected",
};

function recordOf(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function prop(item: Record<string, unknown>, camel: string, pascal: string): unknown {
  return item[camel] ?? item[pascal];
}

function text(value: unknown, fallback = ""): string {
  return value == null ? fallback : String(value);
}

function nullableText(value: unknown): string | null {
  if (value == null || value === "") return null;
  return String(value);
}

function numberOrNull(value: unknown): number | null {
  if (value == null || value === "") return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

function enumNameOf(value: unknown, names: Record<number, string>, fallback: string): string {
  if (typeof value === "number" && names[value]) return names[value]!;
  if (typeof value === "string" && value.trim()) {
    const asNum = Number(value);
    if (Number.isInteger(asNum) && names[asNum]) return names[asNum]!;
    return value;
  }
  return fallback;
}

function statusOf(value: unknown): string {
  return enumNameOf(value, STORY_STATUS_NAMES, "Draft");
}

function originOf(value: unknown): string {
  return enumNameOf(value, STORY_ORIGIN_NAMES, "Admin");
}

function reviewStatusOf(value: unknown): string {
  return enumNameOf(value, STORY_REVIEW_STATUS_NAMES, "None");
}

export function mapPublicStoryItem(value: unknown): PublicStoryItem | null {
  const item = recordOf(value);
  if (!item) return null;
  const storyItemId = text(prop(item, "storyItemId", "StoryItemId"));
  if (!storyItemId) return null;
  return {
    storyItemId,
    mediaType: text(prop(item, "mediaType", "MediaType"), "image"),
    mediaUrl: nullableText(prop(item, "mediaUrl", "MediaUrl")),
    caption: nullableText(prop(item, "caption", "Caption")),
    durationMs: numberOrNull(prop(item, "durationMs", "DurationMs")),
    ctaType: text(prop(item, "ctaType", "CtaType"), "none"),
    ctaTarget: nullableText(prop(item, "ctaTarget", "CtaTarget")),
    ctaHref: nullableText(prop(item, "ctaHref", "CtaHref")),
  };
}

export function mapPublicStory(value: unknown): PublicStoryCard | null {
  const item = recordOf(value);
  if (!item) return null;
  const storyId = text(prop(item, "storyId", "StoryId"));
  if (!storyId) return null;
  const itemsRaw = prop(item, "items", "Items");
  const items = Array.isArray(itemsRaw)
    ? itemsRaw.map(mapPublicStoryItem).filter((row): row is PublicStoryItem => row !== null)
    : [];
  const coverIsVideo = prop(item, "coverIsVideo", "CoverIsVideo");
  return {
    storyId,
    title: text(prop(item, "title", "Title")),
    coverMediaUrl: nullableText(prop(item, "coverMediaUrl", "CoverMediaUrl")),
    isVideo: Boolean(prop(item, "isVideo", "IsVideo")),
    displayOrder: Number(prop(item, "displayOrder", "DisplayOrder") ?? 0) || 0,
    ctaType: text(prop(item, "ctaType", "CtaType"), "none"),
    ctaTarget: nullableText(prop(item, "ctaTarget", "CtaTarget")),
    items,
    sellerPartyId: nullableText(prop(item, "sellerPartyId", "SellerPartyId")),
    shopDisplayName: nullableText(prop(item, "shopDisplayName", "ShopDisplayName")),
    shopPublicId: nullableText(prop(item, "shopPublicId", "ShopPublicId")),
    ctaHref: nullableText(prop(item, "ctaHref", "CtaHref")),
    endAt: nullableText(prop(item, "endAt", "EndAt")),
    coverIsVideo: typeof coverIsVideo === "boolean" ? coverIsVideo : null,
  };
}

export function mapAdminStoryItem(value: unknown): AdminStoryItem | null {
  const item = recordOf(value);
  if (!item) return null;
  const storyItemId = text(prop(item, "storyItemId", "StoryItemId"));
  if (!storyItemId) return null;
  return {
    storyItemId,
    displayOrder: Number(prop(item, "displayOrder", "DisplayOrder") ?? 0) || 0,
    mediaType: text(prop(item, "mediaType", "MediaType"), "image"),
    mediaAssetId: nullableText(prop(item, "mediaAssetId", "MediaAssetId")),
    mediaUrl: nullableText(prop(item, "mediaUrl", "MediaUrl")),
    caption: nullableText(prop(item, "caption", "Caption")),
    durationMs: numberOrNull(prop(item, "durationMs", "DurationMs")),
    ctaType: text(prop(item, "ctaType", "CtaType"), "none"),
    ctaTarget: nullableText(prop(item, "ctaTarget", "CtaTarget")),
    ctaProductTitle: nullableText(prop(item, "ctaProductTitle", "CtaProductTitle")),
    ctaHref: nullableText(prop(item, "ctaHref", "CtaHref")),
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
  };
}

export function mapAdminStory(value: unknown): AdminStorySnapshot | null {
  const item = recordOf(value);
  if (!item) return null;
  const storyId = text(prop(item, "storyId", "StoryId"));
  if (!storyId) return null;
  const itemsRaw = prop(item, "items", "Items");
  const items = Array.isArray(itemsRaw)
    ? itemsRaw.map(mapAdminStoryItem).filter((row): row is AdminStoryItem => row !== null)
    : [];
  return {
    storyId,
    tenantId: text(prop(item, "tenantId", "TenantId")),
    origin: originOf(prop(item, "origin", "Origin")),
    reviewStatus: reviewStatusOf(prop(item, "reviewStatus", "ReviewStatus")),
    sellerPartyId: nullableText(prop(item, "sellerPartyId", "SellerPartyId")),
    rejectionReason: nullableText(prop(item, "rejectionReason", "RejectionReason")),
    submittedAt: nullableText(prop(item, "submittedAt", "SubmittedAt")),
    reviewedAt: nullableText(prop(item, "reviewedAt", "ReviewedAt")),
    submittedByActorUserId: nullableText(prop(item, "submittedByActorUserId", "SubmittedByActorUserId")),
    reviewedByActorUserId: nullableText(prop(item, "reviewedByActorUserId", "ReviewedByActorUserId")),
    locale: nullableText(prop(item, "locale", "Locale")),
    market: nullableText(prop(item, "market", "Market")),
    title: text(prop(item, "title", "Title")),
    coverMediaAssetId: nullableText(prop(item, "coverMediaAssetId", "CoverMediaAssetId")),
    coverMediaUrl: nullableText(prop(item, "coverMediaUrl", "CoverMediaUrl")),
    displayOrder: Number(prop(item, "displayOrder", "DisplayOrder") ?? 0) || 0,
    startAt: nullableText(prop(item, "startAt", "StartAt")),
    endAt: nullableText(prop(item, "endAt", "EndAt")),
    status: statusOf(prop(item, "status", "Status")),
    ctaType: text(prop(item, "ctaType", "CtaType"), "none"),
    ctaTarget: nullableText(prop(item, "ctaTarget", "CtaTarget")),
    versionToken: Number(prop(item, "versionToken", "VersionToken") ?? 0) || 0,
    createdAt: text(prop(item, "createdAt", "CreatedAt")),
    updatedAt: text(prop(item, "updatedAt", "UpdatedAt")),
    items,
    itemsPresent: Array.isArray(itemsRaw),
    id: storyId,
    createdByActorUserId: nullableText(prop(item, "createdByActorUserId", "CreatedByActorUserId")),
    shopDisplayName: nullableText(prop(item, "shopDisplayName", "ShopDisplayName")),
    contentReady: Boolean(prop(item, "contentReady", "ContentReady")),
    effectivelyPublic: Boolean(prop(item, "effectivelyPublic", "EffectivelyPublic")),
    actionCodes: (() => {
      const raw = prop(item, "actionCodes", "ActionCodes");
      return Array.isArray(raw) ? raw.map((code) => String(code)) : [];
    })(),
  };
}

function storyBase(): string {
  if (typeof window === "undefined") {
    return process.env.TOOBA_HOST_ORIGIN ?? "http://127.0.0.1:5088";
  }
  return "";
}

function adminHeaders(json = false): Record<string, string> {
  const headers: Record<string, string> = { Accept: "application/json" };
  if (json) headers["Content-Type"] = "application/json";
  const actor = typeof window !== "undefined" ? localStorage.getItem("tooba.adminActorUserId") : null;
  if (actor) headers[ADMIN_DEV_ACTOR_HEADER] = actor;
  return headers;
}

function resolveSellerPartyId(explicit?: string | null): string | null {
  if (explicit) return explicit;
  if (typeof window === "undefined") return null;
  return readSellerPartyId(window.location.search) ?? localStorage.getItem(SELLER_PARTY_STORAGE_KEY);
}

function resolveSellerActorId(): string | null {
  return readActorUserId();
}

function sellerStoryHeaders(sellerPartyId: string, json = false): Record<string, string> {
  const headers: Record<string, string> = {
    Accept: "application/json",
    [SELLER_PARTY_HEADER]: sellerPartyId,
  };
  if (json) headers["Content-Type"] = "application/json";
  const actor = resolveSellerActorId();
  if (actor) headers[DEV_ACTOR_HEADER] = actor;
  return headers;
}

async function adminJsonResult<T>(
  response: Response,
  map: (value: unknown) => T | null,
): Promise<AdminResult<T>> {
  if (response.status === 401 || response.status === 403) {
    return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
  }
  if (!response.ok) {
    return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
  }
  const payload = await response.json();
  const mapped = map(payload);
  if (!mapped) return { state: "error", data: null, status: response.status, message: "invalid-response" };
  return { state: "ok", data: mapped, status: response.status };
}

export type PublicStoriesResult =
  | { state: "ok"; stories: PublicStoryCard[] }
  | { state: "error"; message: string };

export async function fetchPublicStories(locale?: string): Promise<PublicStoriesResult> {
  const params = new URLSearchParams();
  if (locale) params.set("locale", locale);
  const query = params.toString();
  try {
    const response = await fetch(`${storyBase()}/v1/storefront/stories${query ? `?${query}` : ""}`, {
      headers: { Accept: "application/json" },
      cache: "no-store",
    });
    if (!response.ok) return { state: "error", message: `story.http.${response.status}` };
    const payload = await response.json();
    if (!Array.isArray(payload)) return { state: "error", message: "story.invalid_response" };
    const stories = payload
      .map(mapPublicStory)
      .filter((row): row is PublicStoryCard => row !== null)
      .sort((a, b) => a.displayOrder - b.displayOrder);
    return { state: "ok", stories };
  } catch {
    return { state: "error", message: "story.unreachable" };
  }
}

export async function listAdminStories(options?: {
  reviewStatus?: string | null;
}): Promise<AdminResult<AdminStorySnapshot[]>> {
  try {
    const params = new URLSearchParams();
    if (options?.reviewStatus) params.set("reviewStatus", options.reviewStatus);
    const query = params.toString();
    const response = await fetch(`/v1/admin/stories${query ? `?${query}` : ""}`, { headers: adminHeaders() });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    const payload = await response.json();
    const rows = Array.isArray(payload)
      ? payload.map(mapAdminStory).filter((row): row is AdminStorySnapshot => row !== null)
      : [];
    return { state: "ok", data: rows, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Server GridQuery — استوری Admin. */
export function queryAdminStoriesGrid(
  query: GridServerQuery,
  options?: { reviewStatus?: string | null },
): Promise<AdminGridQueryResult<AdminStorySnapshot>> {
  const params = new URLSearchParams();
  if (options?.reviewStatus) params.set("reviewStatus", options.reviewStatus);
  const suffix = params.toString();
  const path = `/v1/admin/stories/query${suffix ? `?${suffix}` : ""}`;
  return postAdminGridQuery(path, query, adminHeaders(), (item) => mapAdminStory(item));
}

export async function createAdminStory(input: {
  title: string;
  locale?: string | null;
  market?: string | null;
  coverMediaUrl?: string | null;
  coverMediaAssetId?: string | null;
  displayOrder?: number | null;
  ctaType?: string | null;
  ctaTarget?: string | null;
  sellerPartyId?: string | null;
  occurrenceKey?: string | null;
  startAt?: string | null;
  endAt?: string | null;
}): Promise<{ ok: boolean; story?: AdminStorySnapshot; message?: string }> {
  try {
    const response = await fetch("/v1/admin/stories", {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        title: input.title,
        locale: input.locale ?? null,
        market: input.market ?? null,
        coverMediaAssetId: input.coverMediaAssetId ?? null,
        coverMediaUrl: input.coverMediaUrl ?? null,
        displayOrder: input.displayOrder ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
        sellerPartyId: input.sellerPartyId ?? null,
        startAt: input.startAt ?? null,
        endAt: input.endAt ?? null,
        occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return { ok: false, message: (recordOf(payload)?.title as string) ?? `create-http-${response.status}` };
    }
    const story = mapAdminStory(payload);
    return story ? { ok: true, story } : { ok: false, message: "invalid-response" };
  } catch {
    return { ok: false, message: "host-unreachable" };
  }
}

export async function updateAdminStory(
  storyId: string,
  input: {
    title: string;
    locale?: string | null;
    market?: string | null;
    coverMediaUrl?: string | null;
    ctaType?: string | null;
    ctaTarget?: string | null;
    expectedVersion: number;
    occurrenceKey?: string | null;
    startAt?: string | null;
    endAt?: string | null;
  },
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}`, {
      method: "PUT",
      headers: adminHeaders(true),
      body: JSON.stringify({
        title: input.title,
        locale: input.locale ?? null,
        market: input.market ?? null,
        coverMediaAssetId: null,
        coverMediaUrl: input.coverMediaUrl ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
        startAt: input.startAt ?? null,
        endAt: input.endAt ?? null,
        expectedVersion: input.expectedVersion,
        occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function enableAdminStory(
  storyId: string,
  expectedVersion: number,
  occurrenceKey?: string | null,
): Promise<boolean> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/enable`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({ expectedVersion, occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()) }),
    });
    return response.ok;
  } catch {
    return false;
  }
}

export async function disableAdminStory(
  storyId: string,
  expectedVersion: number,
  occurrenceKey?: string | null,
): Promise<boolean> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/disable`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({ expectedVersion, occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()) }),
    });
    return response.ok;
  } catch {
    return false;
  }
}

export async function scheduleAdminStory(
  storyId: string,
  input: {
    startAt?: string | null;
    endAt?: string | null;
    expectedVersion: number;
    occurrenceKey?: string | null;
  },
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/schedule`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        startAt: input.startAt ?? null,
        endAt: input.endAt ?? null,
        expectedVersion: input.expectedVersion,
        occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function getAdminStory(storyId: string): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}`, {
      headers: adminHeaders(),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Platform Story media upload with occurrence-keyed replay (Media-owned path). */
export async function uploadAdminStoryMedia(
  file: File,
  occurrenceKey: string,
): Promise<AdminResult<{ mediaAssetId: string; contentType: string; displayUrl: string | null }>> {
  try {
    const body = new FormData();
    body.append("file", file);
    body.append("occurrenceKey", storyOccurrenceKey(occurrenceKey));
    const headers = adminHeaders(false);
    delete headers["Content-Type"];
    const response = await fetch("/v1/admin/media/upload-story-platform", {
      method: "POST",
      headers,
      body,
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    const payload = await response.json().catch(() => null);
    const row = recordOf(payload);
    const mediaAssetId = text(prop(row ?? {}, "mediaAssetId", "MediaAssetId"));
    if (!mediaAssetId) {
      return { state: "error", data: null, status: response.status, message: "invalid-response" };
    }
    return {
      state: "ok",
      data: {
        mediaAssetId,
        contentType: text(prop(row ?? {}, "contentType", "ContentType")) || file.type,
        displayUrl: nullableText(prop(row ?? {}, "displayUrl", "DisplayUrl")),
      },
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Admin upload owning a shop's Story media. */
export async function uploadAdminStoryMediaForShop(
  file: File,
  sellerPartyId: string,
  occurrenceKey: string,
): Promise<AdminResult<{ mediaAssetId: string; contentType: string; displayUrl: string | null }>> {
  try {
    const body = new FormData();
    body.append("file", file);
    body.append("sellerPartyId", sellerPartyId);
    body.append("occurrenceKey", storyOccurrenceKey(occurrenceKey));
    const headers = adminHeaders(false);
    delete headers["Content-Type"];
    const response = await fetch("/v1/admin/media/upload-for-shop", {
      method: "POST",
      headers,
      body,
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    }
    const payload = await response.json().catch(() => null);
    const row = recordOf(payload);
    const mediaAssetId = text(prop(row ?? {}, "mediaAssetId", "MediaAssetId"));
    if (!mediaAssetId) {
      return { state: "error", data: null, status: response.status, message: "invalid-response" };
    }
    return {
      state: "ok",
      data: {
        mediaAssetId,
        contentType: text(prop(row ?? {}, "contentType", "ContentType")) || file.type,
        displayUrl: nullableText(prop(row ?? {}, "displayUrl", "DisplayUrl")),
      },
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function addAdminStoryItem(
  storyId: string,
  input: {
    mediaType: string;
    mediaAssetId?: string | null;
    mediaUrl?: string | null;
    caption?: string | null;
    durationMs?: number | null;
    ctaType?: string | null;
    ctaTarget?: string | null;
    expectedVersion: number;
    occurrenceKey?: string | null;
  },
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/items`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        mediaType: input.mediaType,
        mediaAssetId: input.mediaAssetId ?? null,
        mediaUrl: input.mediaUrl ?? null,
        caption: input.caption ?? null,
        durationMs: input.durationMs ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
        displayOrder: null,
        expectedVersion: input.expectedVersion,
        occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function removeAdminStoryItem(
  storyId: string,
  itemId: string,
  expectedVersion: number,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(
      `/v1/admin/stories/${encodeURIComponent(storyId)}/items/${encodeURIComponent(itemId)}`,
      {
        method: "DELETE",
        headers: adminHeaders(true),
        body: JSON.stringify({
          expectedVersion,
          occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()),
        }),
      },
    );
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function reorderAdminStories(
  storyIds: string[],
  expectedVersions: number[],
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot[]>> {
  try {
    const response = await fetch("/v1/admin/stories/reorder", {
      method: "PUT",
      headers: adminHeaders(true),
      body: JSON.stringify({
        storyIds,
        expectedVersions,
        occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "admin.authorization.denied" };
    }
    if (!response.ok) return { state: "error", data: null, status: response.status, message: `admin.http.${response.status}` };
    const payload = await response.json();
    const rows = Array.isArray(payload)
      ? payload.map(mapAdminStory).filter((row): row is AdminStorySnapshot => row !== null)
      : [];
    return { state: "ok", data: rows, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function approveAdminStory(
  storyId: string,
  expectedVersion: number,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/approve`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({ expectedVersion, occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()) }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function approveAndPublishAdminStory(
  storyId: string,
  expectedVersion: number,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/approve-and-publish`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({ expectedVersion, occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()) }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function approveAndScheduleAdminStory(
  storyId: string,
  input: {
    expectedVersion: number;
    startAt?: string | null;
    endAt?: string | null;
    occurrenceKey?: string | null;
  },
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/approve-and-schedule`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        expectedVersion: input.expectedVersion,
        startAt: input.startAt ?? null,
        endAt: input.endAt ?? null,
        occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function rejectAdminStory(
  storyId: string,
  reason: string,
  expectedVersion: number,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/reject`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        reason,
        expectedVersion,
        occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

type StoryMutationInput = {
  title: string;
  locale?: string | null;
  market?: string | null;
  coverMediaUrl?: string | null;
  coverMediaAssetId?: string | null;
  displayOrder?: number | null;
  ctaType?: string | null;
  ctaTarget?: string | null;
  expectedVersion?: number | null;
  occurrenceKey?: string | null;
  sellerPartyId?: string | null;
  startAt?: string | null;
  endAt?: string | null;
};

type StoryItemMutationInput = {
  mediaType: string;
  mediaAssetId?: string | null;
  mediaUrl?: string | null;
  caption?: string | null;
  durationMs?: number | null;
  ctaType?: string | null;
  ctaTarget?: string | null;
  expectedVersion?: number | null;
  occurrenceKey?: string | null;
};

async function sellerJsonResult(
  response: Response,
  map: (value: unknown) => AdminStorySnapshot | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  if (response.status === 401 || response.status === 403) {
    return { state: "denied", data: null, status: response.status, message: "seller.authorization.denied" };
  }
  if (!response.ok) {
    return { state: "error", data: null, status: response.status, message: `seller.http.${response.status}` };
  }
  const payload = await response.json();
  const mapped = map(payload);
  if (!mapped) return { state: "error", data: null, status: response.status, message: "invalid-response" };
  return { state: "ok", data: mapped, status: response.status };
}

export async function listSellerStories(
  sellerPartyId?: string | null,
): Promise<AdminResult<AdminStorySnapshot[]>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch("/v1/seller/stories", { headers: sellerStoryHeaders(partyId) });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "seller.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `seller.http.${response.status}` };
    }
    const payload = await response.json();
    const rows = Array.isArray(payload)
      ? payload.map(mapAdminStory).filter((row): row is AdminStorySnapshot => row !== null)
      : [];
    return { state: "ok", data: rows, status: response.status };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function getSellerStory(
  storyId: string,
  sellerPartyId?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(`/v1/seller/stories/${encodeURIComponent(storyId)}`, {
      headers: sellerStoryHeaders(partyId),
    });
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

/** Upload a seller Story media file; returns Ready asset id. */
export async function uploadSellerStoryMedia(
  file: File,
  occurrenceKey: string,
  sellerPartyId?: string | null,
): Promise<AdminResult<{ mediaAssetId: string; contentType: string; displayUrl: string | null }>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const body = new FormData();
    body.append("file", file);
    body.append("occurrenceKey", storyOccurrenceKey(occurrenceKey));
    const headers = sellerStoryHeaders(partyId, false);
    delete headers["Content-Type"];
    const response = await fetch("/v1/seller/media", {
      method: "POST",
      headers,
      body,
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "seller.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `seller.http.${response.status}` };
    }
    const payload = await response.json().catch(() => null);
    const row = recordOf(payload);
    const mediaAssetId = text(prop(row ?? {}, "mediaAssetId", "MediaAssetId"));
    if (!mediaAssetId) {
      return { state: "error", data: null, status: response.status, message: "invalid-response" };
    }
    return {
      state: "ok",
      data: {
        mediaAssetId,
        contentType: text(prop(row ?? {}, "contentType", "ContentType")),
        displayUrl: nullableText(prop(row ?? {}, "displayUrl", "DisplayUrl")),
      },
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function createSellerStory(
  input: StoryMutationInput,
  sellerPartyId?: string | null,
): Promise<{ ok: boolean; story?: AdminStorySnapshot; message?: string }> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) return { ok: false, message: "seller.identity.missing" };
  try {
    const response = await fetch("/v1/seller/stories", {
      method: "POST",
      headers: sellerStoryHeaders(partyId, true),
      body: JSON.stringify({
        title: input.title,
        locale: input.locale ?? null,
        market: input.market ?? null,
        coverMediaAssetId: input.coverMediaAssetId ?? null,
        coverMediaUrl: input.coverMediaUrl ?? null,
        displayOrder: input.displayOrder ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
        startAt: input.startAt ?? null,
        endAt: input.endAt ?? null,
        occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    const payload = await response.json().catch(() => null);
    if (!response.ok) {
      return { ok: false, message: (recordOf(payload)?.title as string) ?? `create-http-${response.status}` };
    }
    const story = mapAdminStory(payload);
    return story ? { ok: true, story } : { ok: false, message: "invalid-response" };
  } catch {
    return { ok: false, message: "host-unreachable" };
  }
}

export async function updateSellerStory(
  storyId: string,
  input: StoryMutationInput,
  sellerPartyId?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(`/v1/seller/stories/${encodeURIComponent(storyId)}`, {
      method: "PUT",
      headers: sellerStoryHeaders(partyId, true),
      body: JSON.stringify({
        title: input.title,
        locale: input.locale ?? null,
        market: input.market ?? null,
        coverMediaAssetId: input.coverMediaAssetId ?? null,
        coverMediaUrl: input.coverMediaUrl ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
        startAt: input.startAt ?? null,
        endAt: input.endAt ?? null,
        expectedVersion: input.expectedVersion ?? null,
        occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function submitSellerStory(
  storyId: string,
  sellerPartyId?: string | null,
  expectedVersion?: number | null,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(`/v1/seller/stories/${encodeURIComponent(storyId)}/submit`, {
      method: "POST",
      headers: sellerStoryHeaders(partyId, true),
      body: JSON.stringify({
        expectedVersion: expectedVersion ?? null,
        occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function returnSellerStoryToDraft(
  storyId: string,
  expectedVersion: number,
  sellerPartyId?: string | null,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(`/v1/seller/stories/${encodeURIComponent(storyId)}/return-to-draft`, {
      method: "POST",
      headers: sellerStoryHeaders(partyId, true),
      body: JSON.stringify({ expectedVersion, occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()) }),
    });
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function stopSellerStory(
  storyId: string,
  expectedVersion: number,
  sellerPartyId?: string | null,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(`/v1/seller/stories/${encodeURIComponent(storyId)}/stop`, {
      method: "POST",
      headers: sellerStoryHeaders(partyId, true),
      body: JSON.stringify({ expectedVersion, occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()) }),
    });
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function querySellerStories(
  query: GridServerQuery,
  sellerPartyId?: string | null,
): Promise<AdminResult<{ items: AdminStorySnapshot[]; page: number; pageSize: number; totalCount: number }>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch("/v1/seller/stories/query", {
      method: "POST",
      headers: sellerStoryHeaders(partyId, true),
      body: JSON.stringify(toHostGridQuery(query)),
    });
    if (response.status === 401 || response.status === 403) {
      return { state: "denied", data: null, status: response.status, message: "seller.authorization.denied" };
    }
    if (!response.ok) {
      return { state: "error", data: null, status: response.status, message: `seller.http.${response.status}` };
    }
    const payload = await response.json().catch(() => null);
    const page = payload as {
      items?: unknown[];
      page?: number;
      pageSize?: number;
      totalCount?: number;
    } | null;
    return {
      state: "ok",
      data: {
        items: Array.isArray(page?.items)
          ? page!.items!.map((row) => mapAdminStory(row)).filter((row): row is AdminStorySnapshot => row !== null)
          : [],
        page: typeof page?.page === "number" ? page.page : 1,
        pageSize: typeof page?.pageSize === "number" ? page.pageSize : 50,
        totalCount: typeof page?.totalCount === "number" ? page.totalCount : 0,
      },
      status: response.status,
    };
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function addSellerStoryItem(
  storyId: string,
  input: StoryItemMutationInput,
  sellerPartyId?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(`/v1/seller/stories/${encodeURIComponent(storyId)}/items`, {
      method: "POST",
      headers: sellerStoryHeaders(partyId, true),
      body: JSON.stringify({
        mediaType: input.mediaType,
        mediaAssetId: input.mediaAssetId ?? null,
        mediaUrl: input.mediaUrl ?? null,
        caption: input.caption ?? null,
        durationMs: input.durationMs ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
        displayOrder: null,
        expectedVersion: input.expectedVersion ?? null,
        occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function updateAdminStoryItem(
  storyId: string,
  itemId: string,
  input: {
    caption?: string | null;
    durationMs?: number | null;
    ctaType?: string | null;
    ctaTarget?: string | null;
    mediaType?: string | null;
    mediaAssetId?: string | null;
    mediaUrl?: string | null;
    expectedVersion: number;
    occurrenceKey?: string | null;
  },
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(
      `/v1/admin/stories/${encodeURIComponent(storyId)}/items/${encodeURIComponent(itemId)}`,
      {
        method: "PUT",
        headers: adminHeaders(true),
        body: JSON.stringify({
          mediaType: input.mediaType ?? null,
          mediaAssetId: input.mediaAssetId ?? null,
          mediaUrl: input.mediaUrl ?? null,
          caption: input.caption ?? null,
          durationMs: input.durationMs ?? null,
          ctaType: input.ctaType ?? "none",
          ctaTarget: input.ctaTarget ?? null,
          expectedVersion: input.expectedVersion,
          occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
        }),
      },
    );
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function reorderAdminStoryItems(
  storyId: string,
  itemIdsInOrder: string[],
  expectedVersion: number,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/items/reorder`, {
      method: "PUT",
      headers: adminHeaders(true),
      body: JSON.stringify({
        itemIds: itemIdsInOrder,
        expectedVersion,
        occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function updateSellerStoryItem(
  storyId: string,
  itemId: string,
  input: {
    caption?: string | null;
    durationMs?: number | null;
    ctaType?: string | null;
    ctaTarget?: string | null;
    mediaType?: string | null;
    mediaAssetId?: string | null;
    mediaUrl?: string | null;
    expectedVersion: number;
    occurrenceKey?: string | null;
  },
  sellerPartyId?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(
      `/v1/seller/stories/${encodeURIComponent(storyId)}/items/${encodeURIComponent(itemId)}`,
      {
        method: "PUT",
        headers: sellerStoryHeaders(partyId, true),
        body: JSON.stringify({
          mediaType: input.mediaType ?? null,
          mediaAssetId: input.mediaAssetId ?? null,
          mediaUrl: input.mediaUrl ?? null,
          caption: input.caption ?? null,
          durationMs: input.durationMs ?? null,
          ctaType: input.ctaType ?? "none",
          ctaTarget: input.ctaTarget ?? null,
          expectedVersion: input.expectedVersion,
          occurrenceKey: storyOccurrenceKey(input.occurrenceKey ?? newStoryOccurrenceKey()),
        }),
      },
    );
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function removeSellerStoryItem(
  storyId: string,
  itemId: string,
  expectedVersion: number,
  sellerPartyId?: string | null,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(
      `/v1/seller/stories/${encodeURIComponent(storyId)}/items/${encodeURIComponent(itemId)}`,
      {
        method: "DELETE",
        headers: sellerStoryHeaders(partyId, true),
        body: JSON.stringify({
          expectedVersion,
          occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()),
        }),
      },
    );
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function reorderSellerStoryItems(
  storyId: string,
  itemIdsInOrder: string[],
  expectedVersion: number,
  sellerPartyId?: string | null,
  occurrenceKey?: string | null,
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(`/v1/seller/stories/${encodeURIComponent(storyId)}/items/reorder`, {
      method: "PUT",
      headers: sellerStoryHeaders(partyId, true),
      body: JSON.stringify({
        itemIds: itemIdsInOrder,
        expectedVersion,
        occurrenceKey: storyOccurrenceKey(occurrenceKey ?? newStoryOccurrenceKey()),
      }),
    });
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
