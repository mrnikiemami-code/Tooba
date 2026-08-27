/**
 * کلاینت عمومی/ادمین Story — فقط دادهٔ زندهٔ Host.
 */

import { ADMIN_DEV_ACTOR_HEADER, type AdminResult } from "../admin/admin-api.ts";
import {
  DEV_ACTOR_HEADER,
  readActorUserId,
  readSellerPartyId,
  SELLER_PARTY_HEADER,
  SELLER_PARTY_STORAGE_KEY,
} from "../vendor-panel/seller-api.ts";

export interface PublicStoryItem {
  storyItemId: string;
  mediaType: string;
  mediaUrl: string | null;
  caption: string | null;
  durationMs: number | null;
  ctaType: string;
  ctaTarget: string | null;
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
  id: string;
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
  return {
    storyId,
    title: text(prop(item, "title", "Title")),
    coverMediaUrl: nullableText(prop(item, "coverMediaUrl", "CoverMediaUrl")),
    isVideo: Boolean(prop(item, "isVideo", "IsVideo")),
    displayOrder: Number(prop(item, "displayOrder", "DisplayOrder") ?? 0) || 0,
    ctaType: text(prop(item, "ctaType", "CtaType"), "none"),
    ctaTarget: nullableText(prop(item, "ctaTarget", "CtaTarget")),
    items,
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
    id: storyId,
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

export async function fetchPublicStories(locale?: string): Promise<PublicStoryCard[]> {
  const params = new URLSearchParams();
  if (locale) params.set("locale", locale);
  const query = params.toString();
  try {
    const response = await fetch(`${storyBase()}/v1/storefront/stories${query ? `?${query}` : ""}`, {
      headers: { Accept: "application/json" },
      cache: "no-store",
    });
    if (!response.ok) return [];
    const payload = await response.json();
    if (!Array.isArray(payload)) return [];
    return payload
      .map(mapPublicStory)
      .filter((row): row is PublicStoryCard => row !== null)
      .sort((a, b) => a.displayOrder - b.displayOrder);
  } catch {
    return [];
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

export async function createAdminStory(input: {
  title: string;
  locale?: string | null;
  market?: string | null;
  coverMediaUrl?: string | null;
  displayOrder?: number | null;
  ctaType?: string | null;
  ctaTarget?: string | null;
}): Promise<{ ok: boolean; story?: AdminStorySnapshot; message?: string }> {
  try {
    const response = await fetch("/v1/admin/stories", {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        title: input.title,
        locale: input.locale ?? null,
        market: input.market ?? null,
        coverMediaAssetId: null,
        coverMediaUrl: input.coverMediaUrl ?? null,
        displayOrder: input.displayOrder ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
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
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function enableAdminStory(storyId: string): Promise<boolean> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/enable`, {
      method: "POST",
      headers: adminHeaders(true),
    });
    return response.ok;
  } catch {
    return false;
  }
}

export async function disableAdminStory(storyId: string): Promise<boolean> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/disable`, {
      method: "POST",
      headers: adminHeaders(true),
    });
    return response.ok;
  } catch {
    return false;
  }
}

export async function scheduleAdminStory(
  storyId: string,
  input: { startAt?: string | null; endAt?: string | null },
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/schedule`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        startAt: input.startAt ?? null,
        endAt: input.endAt ?? null,
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function addAdminStoryItem(
  storyId: string,
  input: {
    mediaType: string;
    mediaUrl?: string | null;
    caption?: string | null;
    durationMs?: number | null;
    ctaType?: string | null;
    ctaTarget?: string | null;
  },
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/items`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({
        mediaType: input.mediaType,
        mediaAssetId: null,
        mediaUrl: input.mediaUrl ?? null,
        caption: input.caption ?? null,
        durationMs: input.durationMs ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
        displayOrder: null,
      }),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function removeAdminStoryItem(storyId: string, itemId: string): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(
      `/v1/admin/stories/${encodeURIComponent(storyId)}/items/${encodeURIComponent(itemId)}`,
      { method: "DELETE", headers: adminHeaders() },
    );
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function reorderAdminStories(storyIds: string[]): Promise<AdminResult<AdminStorySnapshot[]>> {
  try {
    const response = await fetch("/v1/admin/stories/reorder", {
      method: "PUT",
      headers: adminHeaders(true),
      body: JSON.stringify({ storyIds }),
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

export async function approveAdminStory(storyId: string): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/approve`, {
      method: "POST",
      headers: adminHeaders(true),
    });
    return adminJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}

export async function rejectAdminStory(
  storyId: string,
  reason: string,
): Promise<AdminResult<AdminStorySnapshot>> {
  try {
    const response = await fetch(`/v1/admin/stories/${encodeURIComponent(storyId)}/reject`, {
      method: "POST",
      headers: adminHeaders(true),
      body: JSON.stringify({ reason }),
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
  displayOrder?: number | null;
  ctaType?: string | null;
  ctaTarget?: string | null;
};

type StoryItemMutationInput = {
  mediaType: string;
  mediaUrl?: string | null;
  caption?: string | null;
  durationMs?: number | null;
  ctaType?: string | null;
  ctaTarget?: string | null;
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
        coverMediaAssetId: null,
        coverMediaUrl: input.coverMediaUrl ?? null,
        displayOrder: input.displayOrder ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
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
        coverMediaAssetId: null,
        coverMediaUrl: input.coverMediaUrl ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
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
): Promise<AdminResult<AdminStorySnapshot>> {
  const partyId = resolveSellerPartyId(sellerPartyId);
  if (!partyId) {
    return { state: "error", data: null, status: 0, message: "seller.identity.missing" };
  }
  try {
    const response = await fetch(`/v1/seller/stories/${encodeURIComponent(storyId)}/submit`, {
      method: "POST",
      headers: sellerStoryHeaders(partyId, true),
    });
    return sellerJsonResult(response, mapAdminStory);
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
        mediaAssetId: null,
        mediaUrl: input.mediaUrl ?? null,
        caption: input.caption ?? null,
        durationMs: input.durationMs ?? null,
        ctaType: input.ctaType ?? "none",
        ctaTarget: input.ctaTarget ?? null,
        displayOrder: null,
      }),
    });
    return sellerJsonResult(response, mapAdminStory);
  } catch {
    return { state: "error", data: null, status: 0, message: "host-unreachable" };
  }
}
