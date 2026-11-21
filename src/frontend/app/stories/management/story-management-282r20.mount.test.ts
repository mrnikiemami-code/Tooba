/**
 * TB-282-R20 focused direct facts for the staged media-replacement journal, authoritative
 * refusal classification and response-effect verification. These exercise the REAL executor
 * against a controlled fetch, not a string helper.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { toast } from "react-toastify";
import {
  click,
  ensureDom,
  flush,
  mount,
  q,
  qq,
  setSelectValue,
  waitFor,
} from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { runDurableStoryMutation, runDurableUploadAdd, type StoryExecutorGate } from "./story-executor.ts";
import { digestFile, listPendingStoryIntents, type StoryIntentEnvelope } from "./story-intent.ts";
import {
  bindStoryEnvelopeRoute,
  isAuthoritativeStoryRefusal,
  verifyStorySuccessPayload,
} from "./story-response.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import type { AdminStorySnapshot } from "../story-api.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";

ensureDom();

const STORY = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa20a1";
const ITEM = "dddddddd-dddd-dddd-dddd-dddddddd20d1";
const ACTOR = "actor-r20-seller";
const SHOP = "22222222-2222-2222-2222-222222222220";
const OCC_UPLOAD = "occ-r20-upload";
const OCC_UPDATE = "occ-r20-update";
const ASSET = "eeeeeeee-eeee-eeee-eeee-eeeeeeee20e1";

type Call = { method: string; url: string; body: string | null };

function gate(): StoryExecutorGate {
  return { inFlightRef: { current: false }, epochRef: { current: 1 } };
}

async function replacementEnvelope(): Promise<StoryIntentEnvelope> {
  const file = new File(["replacement-bytes"], "new.jpg", { type: "image/jpeg" });
  const digest = await digestFile(file);
  return {
    version: 2,
    action: "replace_media",
    occurrenceKey: OCC_UPLOAD,
    method: "POST",
    path: "/v1/seller/media",
    routeTargetId: STORY,
    targetItemId: ITEM,
    expectedVersion: 7,
    bodyText: JSON.stringify({
      caption: "زیرنویس تازه",
      durationSec: 6,
      ctaType: "none",
      ctaTarget: null,
      expectedVersion: 7,
      itemOccurrenceKey: OCC_UPDATE,
    }),
    uploadMeta: { fileName: "new.jpg", fileType: "image/jpeg", fileSize: file.size, digestHex: digest },
    addBodyText: null,
    uploadedAssetId: null,
    children: [
      { step: "upload", occurrenceKey: OCC_UPLOAD, done: false },
      { step: "update_item", occurrenceKey: OCC_UPDATE, done: false },
    ],
    role: "seller",
    shopId: SHOP,
    actorUserId: ACTOR,
    recordedAt: new Date().toISOString(),
  };
}

function replacementFile(): File {
  return new File(["replacement-bytes"], "new.jpg", { type: "image/jpeg" });
}

const scope = { role: "seller" as const, shopId: SHOP, actorUserId: ACTOR };

function installFetch(handler: (call: Call) => Response | Promise<Response>) {
  const calls: Call[] = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const call: Call = {
      method: (init?.method ?? "GET").toUpperCase(),
      url: String(input),
      body: typeof init?.body === "string" ? init.body : null,
    };
    calls.push(call);
    return handler(call);
  }) as typeof fetch;
  return {
    calls,
    restore() {
      globalThis.fetch = original;
    },
  };
}

const uploadOk = () =>
  new Response(
    JSON.stringify({ mediaAssetId: ASSET, contentType: "image/jpeg", displayUrl: "https://cdn.example.test/new.jpg" }),
    { status: 200, headers: { "Content-Type": "application/json" } },
  );

const storySnapshot = (over: Record<string, unknown> = {}) =>
  new Response(
    JSON.stringify({
      data: {
        storyId: STORY,
        status: "Draft",
        reviewStatus: "Approved",
        versionToken: 8,
        items: [
          {
            storyItemId: ITEM,
            displayOrder: 0,
            mediaType: "image",
            mediaAssetId: ASSET,
            mediaUrl: "https://cdn.example.test/new.jpg",
            caption: "زیرنویس تازه",
            durationMs: 6000,
            ctaType: "none",
            ctaTarget: null,
          },
        ],
        ...over,
      },
    }),
    { status: 200, headers: { "Content-Type": "application/json" } },
  );

test("R20 C12: lost replace-media UPDATE retries ONLY the frozen update, never a second upload", async () => {
  window.sessionStorage.clear();
  const envelope = await replacementEnvelope();
  let updateAttempts = 0;
  const spy = installFetch((call) => {
    if (call.method === "POST" && call.url === "/v1/seller/media") return uploadOk();
    if (call.method === "PUT" && call.url === `/v1/seller/stories/${STORY}/items/${ITEM}`) {
      updateAttempts += 1;
      // First update reply is an HTML gateway page: uncertain, so the journal must survive intact.
      if (updateAttempts === 1) {
        return new Response("<html><body>404</body></html>", { status: 404, headers: { "Content-Type": "text/html" } });
      }
      return storySnapshot();
    }
    throw new Error(`unexpected ${call.method} ${call.url}`);
  });

  try {
    const first = await runDurableUploadAdd({ gate: gate(), envelope, scope, uploadFile: replacementFile() });
    assert.equal(first.state, "failed");
    assert.equal(first.state === "failed" && first.code, "story.response.unverified");
    assert.equal(spy.calls.length, 2, "upload then exactly one update attempt");
    assert.equal(spy.calls[0]!.method, "POST");
    assert.equal(spy.calls[0]!.url, "/v1/seller/media");
    assert.equal(spy.calls[1]!.method, "PUT");

    // The frozen journal keeps the asset, the exact update bytes and the same slide identity.
    const pending = listPendingStoryIntents("seller", SHOP, ACTOR);
    assert.equal(pending.length, 1);
    const stored = pending[0]!;
    if (stored.state !== "ok") throw new Error("journal missing");
    assert.equal(stored.envelope.action, "replace_media");
    assert.equal(stored.envelope.targetItemId, ITEM);
    assert.equal(stored.envelope.children[0]!.done, true);
    assert.equal(stored.envelope.uploadedAssetId, ASSET);
    assert.equal(stored.envelope.method, "PUT");
    const frozenUpdate = stored.envelope.addBodyText!;
    assert.match(frozenUpdate, new RegExp(ASSET));
    assert.match(frozenUpdate, new RegExp(OCC_UPDATE));
    assert.match(frozenUpdate, /زیرنویس تازه/);
    assert.doesNotMatch(JSON.stringify(stored.envelope.uploadMeta), /pending/);
    assert.equal(stored.envelope.uploadMeta?.digestHex.length, 64);

    // Recovery: same stored envelope, no file needed, no upload, one PUT with identical bytes.
    const replay = await runDurableUploadAdd({ gate: gate(), envelope: stored.envelope, scope, uploadFile: null });
    assert.equal(replay.state, "ok");
    assert.equal(spy.calls.length, 3, "recovery sends only the update");
    assert.equal(spy.calls[2]!.method, "PUT");
    assert.equal(spy.calls[2]!.body, frozenUpdate);
    assert.equal(listPendingStoryIntents("seller", SHOP, ACTOR).length, 0, "resolved journal is cleared");
  } finally {
    spy.restore();
  }
});

test("R20 C12: lost upload keeps the exact journal and resumes that same upload once", async () => {
  window.sessionStorage.clear();
  const envelope = await replacementEnvelope();
  let uploads = 0;
  const spy = installFetch((call) => {
    if (call.method === "POST" && call.url === "/v1/seller/media") {
      uploads += 1;
      if (uploads === 1) throw new TypeError("Failed to fetch");
      return uploadOk();
    }
    if (call.method === "PUT") return storySnapshot();
    throw new Error(`unexpected ${call.method} ${call.url}`);
  });

  try {
    const lost = await runDurableUploadAdd({ gate: gate(), envelope, scope, uploadFile: replacementFile() });
    assert.equal(lost.state, "failed");
    const pending = listPendingStoryIntents("seller", SHOP, ACTOR);
    assert.equal(pending.length, 1);
    if (pending[0]!.state !== "ok") throw new Error("journal missing");
    assert.equal(pending[0]!.envelope.children[0]!.done, false, "no asset is invented from a lost upload");
    assert.equal(pending[0]!.envelope.uploadedAssetId, null);

    // The same frozen intent resumes the upload (no new occurrence key) and then the update.
    const resumed = await runDurableUploadAdd({
      gate: gate(),
      envelope: pending[0]!.envelope,
      scope,
      uploadFile: replacementFile(),
    });
    assert.equal(resumed.state, "ok");
    assert.deepEqual(
      spy.calls.map((c) => `${c.method} ${c.url}`),
      [
        "POST /v1/seller/media",
        "POST /v1/seller/media",
        `PUT /v1/seller/stories/${STORY}/items/${ITEM}`,
      ],
    );
  } finally {
    spy.restore();
  }
});

test("R20 C12: replacement bind refuses a missing slide identity and cross-role routes", async () => {
  const envelope = await replacementEnvelope();
  assert.equal(bindStoryEnvelopeRoute(envelope).ok, true);
  assert.equal(bindStoryEnvelopeRoute({ ...envelope, targetItemId: null }).ok, true, "upload stage only needs media route");

  const advanced: StoryIntentEnvelope = {
    ...envelope,
    children: [{ ...envelope.children[0]!, done: true }, envelope.children[1]!],
    uploadedAssetId: ASSET,
    addBodyText: "{}",
    method: "PUT",
    path: `/v1/seller/stories/${STORY}/items/${ITEM}`,
    bodyText: "{}",
  };
  assert.equal(bindStoryEnvelopeRoute(advanced).ok, true);
  assert.equal(bindStoryEnvelopeRoute({ ...advanced, targetItemId: null }).ok, false);
  assert.equal(
    bindStoryEnvelopeRoute({ ...advanced, path: `/v1/admin/stories/${STORY}/items/${ITEM}` }).ok,
    false,
    "a seller-frozen scope can never be replayed against the admin route",
  );
  assert.equal(
    bindStoryEnvelopeRoute({ ...advanced, path: `/v1/seller/stories/${STORY}/items/${STORY}` }).ok,
    false,
    "the frozen update must address the SAME slide it replaces",
  );
});

test("R20 A4: only an owning story.* 4xx is an authoritative no-effect refusal", () => {
  assert.equal(isAuthoritativeStoryRefusal(404, { errorCode: "story.missing" }), true);
  assert.equal(isAuthoritativeStoryRefusal(409, { errors: [{ code: "story.version.stale" }] }), true);
  assert.equal(isAuthoritativeStoryRefusal(409, { errorCode: "story.content.notReady" }), true);
  assert.equal(isAuthoritativeStoryRefusal(409, { errorCode: "story.correction.version_stale" }), false);
  assert.equal(isAuthoritativeStoryRefusal(404, { errorCode: "catalog.notfound" }), false);
  assert.equal(isAuthoritativeStoryRefusal(404, { errorCode: "not_found" }), false);
  assert.equal(isAuthoritativeStoryRefusal(404, { message: "gone" }), false);
  assert.equal(isAuthoritativeStoryRefusal(429, { errorCode: "story.throttled" }), false);
  assert.equal(isAuthoritativeStoryRefusal(500, { errorCode: "story.failed" }), false);
  assert.equal(isAuthoritativeStoryRefusal(404, "<html>404</html>"), false);
});

test("R20 A3: version regression and state-free approve are refused; real Approved is accepted", () => {
  const approve: StoryIntentEnvelope = {
    version: 2,
    action: "approve",
    occurrenceKey: "occ-r20-approve",
    method: "POST",
    path: `/v1/admin/stories/${STORY}/approve`,
    routeTargetId: STORY,
    expectedVersion: 8,
    bodyText: JSON.stringify({ expectedVersion: 8, occurrenceKey: "occ-r20-approve" }),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "admin",
    shopId: null,
    actorUserId: "actor-r20-admin",
    recordedAt: new Date().toISOString(),
  };

  assert.equal(
    verifyStorySuccessPayload(approve, { data: { storyId: STORY, reviewStatus: "Approved", versionToken: 9 } }).ok,
    true,
  );
  assert.equal(verifyStorySuccessPayload(approve, { data: { storyId: STORY } }).ok, false);
  assert.equal(
    verifyStorySuccessPayload(approve, { data: { storyId: STORY, reviewStatus: "Approved", versionToken: 7 } }).ok,
    false,
    "a version behind the frozen target is not completion",
  );
  assert.equal(
    verifyStorySuccessPayload(approve, {
      data: { storyId: STORY, reviewStatus: "None", status: "Draft", versionToken: 9 },
    }).ok,
    false,
    "same-id unchanged Draft is not approval",
  );
});

test("R20 A3b: content updates prove the requested text/media, not just the story identity", () => {
  const update: StoryIntentEnvelope = {
    version: 2,
    action: "update",
    occurrenceKey: "occ-r20-update",
    method: "PUT",
    path: `/v1/admin/stories/${STORY}`,
    routeTargetId: STORY,
    expectedVersion: 8,
    bodyText: JSON.stringify({ title: "عنوان تازه", locale: "fa", startAt: "2026-11-21T08:00:00Z", endAt: "2026-11-22T08:00:00Z", expectedVersion: 8, occurrenceKey: "occ-r20-update" }),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "admin",
    shopId: null,
    actorUserId: "actor-r20-admin",
    recordedAt: new Date().toISOString(),
  };
  const oldStory = { data: { storyId: STORY, title: "عنوان قبلی", locale: "fa", startAt: "2026-11-20T08:00:00Z", endAt: "2026-11-23T08:00:00Z", versionToken: 9 } };
  const newStory = { data: { storyId: STORY, title: "عنوان تازه", locale: "fa", startAt: "2026-11-21T08:00:00Z", endAt: "2026-11-22T08:00:00Z", versionToken: 9 } };
  assert.equal(verifyStorySuccessPayload(update, oldStory).ok, false);
  assert.equal(verifyStorySuccessPayload(update, newStory).ok, true);
  assert.equal(
    verifyStorySuccessPayload(update, { data: { ...newStory.data, startAt: "2026-11-21T09:00:00Z" } }).ok,
    false,
    "a different start window is not a completed save",
  );
  assert.equal(
    verifyStorySuccessPayload(update, { data: { ...newStory.data, endAt: "2026-11-22T09:00:00Z" } }).ok,
    false,
    "a different end window is not a completed save",
  );

  const updateItem: StoryIntentEnvelope = {
    ...update,
    action: "update_item",
    occurrenceKey: "occ-r20-item",
    path: `/v1/admin/stories/${STORY}/items/${ITEM}`,
    targetItemId: ITEM,
    bodyText: JSON.stringify({
      mediaType: "image",
      mediaAssetId: ASSET,
      mediaUrl: "https://cdn.example.test/new.jpg",
      caption: "زیرنویس تازه",
      durationMs: 6000,
      ctaType: "none",
      ctaTarget: null,
      expectedVersion: 8,
      occurrenceKey: "occ-r20-item",
    }),
  };
  const oldItem = {
    data: {
      storyId: STORY,
      title: "عنوان تازه",
      locale: "fa",
      versionToken: 9,
      items: [{ storyItemId: ITEM, mediaType: "image", mediaAssetId: "old", mediaUrl: "old.jpg", caption: "قدیمی", durationMs: 5000, ctaType: "none", ctaTarget: null }],
    },
  };
  const newItem = {
    data: {
      storyId: STORY,
      title: "عنوان تازه",
      locale: "fa",
      versionToken: 9,
      items: [{ storyItemId: ITEM, mediaType: "image", mediaAssetId: ASSET, mediaUrl: "https://cdn.example.test/new.jpg", caption: "زیرنویس تازه", durationMs: 6000, ctaType: "none", ctaTarget: null }],
    },
  };
  assert.equal(verifyStorySuccessPayload(updateItem, oldItem).ok, false, "an unchanged slide is not a completed edit");
  assert.equal(verifyStorySuccessPayload(updateItem, newItem).ok, true);
});
test("R22 F04: remove, reorder, schedule and upload_add must prove their own effect", () => {
  const other = "cccccccc-cccc-cccc-cccc-cccccccc20c1";
  const story = (overrides: Record<string, unknown>) => ({
    data: {
      storyId: STORY,
      title: "Story",
      locale: "fa",
      status: "Draft",
      reviewStatus: "None",
      versionToken: 9,
      startAt: null,
      endAt: null,
      items: [
        { storyItemId: ITEM, mediaType: "image", mediaAssetId: ASSET, mediaUrl: "https://cdn.example.test/item.jpg", caption: "item", durationMs: 5000, ctaType: "none", ctaTarget: null },
        { storyItemId: other, mediaType: "image", mediaAssetId: null, mediaUrl: "https://cdn.example.test/other.jpg", caption: "other", durationMs: 5000, ctaType: "none", ctaTarget: null },
      ],
      ...overrides,
    },
  });
  const envelope = (action: StoryIntentEnvelope["action"], body: Record<string, unknown>, extra: Partial<StoryIntentEnvelope> = {}): StoryIntentEnvelope => ({
    version: 2,
    action,
    occurrenceKey: `occ-${action}`,
    method: "POST",
    path: `/v1/admin/stories/${STORY}`,
    routeTargetId: STORY,
    expectedVersion: 8,
    bodyText: JSON.stringify(body),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "admin",
    shopId: null,
    actorUserId: "audit",
    recordedAt: new Date().toISOString(),
    ...extra,
  });

  const remove = envelope("remove_item", { expectedVersion: 8 }, { targetItemId: ITEM, path: `/v1/admin/stories/${STORY}/items/${ITEM}` });
  assert.equal(verifyStorySuccessPayload(remove, story({})).ok, false);
  assert.equal(verifyStorySuccessPayload(remove, story({ items: [{ storyItemId: other, mediaType: "image", mediaAssetId: null, mediaUrl: "/o.jpg", caption: "o", durationMs: 5000, ctaType: "none", ctaTarget: null }] })).ok, true);

  const reorder = envelope("reorder_items", { itemIds: [other, ITEM], expectedVersion: 8 });
  assert.equal(verifyStorySuccessPayload(reorder, story({})).ok, false);
  assert.equal(verifyStorySuccessPayload(reorder, story({ items: [
    { storyItemId: other, mediaType: "image", mediaAssetId: null, mediaUrl: "/o.jpg", caption: "o", durationMs: 5000, ctaType: "none", ctaTarget: null },
    { storyItemId: ITEM, mediaType: "image", mediaAssetId: ASSET, mediaUrl: "/i.jpg", caption: "i", durationMs: 5000, ctaType: "none", ctaTarget: null },
  ] })).ok, true);

  const startAt = "2026-12-01T10:00:00Z";
  const endAt = "2026-12-02T10:00:00Z";
  const schedule = envelope("schedule", { startAt, endAt, expectedVersion: 8 });
  assert.equal(verifyStorySuccessPayload(schedule, story({ status: "Scheduled", startAt: "2026-11-01T10:00:00Z", endAt: "2026-11-02T10:00:00Z" })).ok, false);
  assert.equal(verifyStorySuccessPayload(schedule, story({ status: "Scheduled", startAt, endAt })).ok, true);
  assert.equal(verifyStorySuccessPayload(schedule, story({ status: "Draft", startAt, endAt })).ok, false, "correct dates with Draft state are not a scheduled completion");

  const approveSchedule = envelope("approve_schedule", { startAt, endAt, expectedVersion: 8 });
  assert.equal(verifyStorySuccessPayload(approveSchedule, story({ reviewStatus: "None", status: "Scheduled", startAt, endAt })).ok, false);
  assert.equal(verifyStorySuccessPayload(approveSchedule, story({ reviewStatus: "Approved", status: "Scheduled", startAt, endAt })).ok, true);

  const addBody = { mediaType: "image", mediaAssetId: ASSET, mediaUrl: "https://cdn.example.test/item.jpg", caption: "item", durationMs: 5000, ctaType: "none", ctaTarget: null };
  const add = envelope("upload_add", addBody, {
    children: [{ step: "upload", occurrenceKey: "u", done: true }, { step: "add", occurrenceKey: "a", done: false }],
    addBodyText: JSON.stringify(addBody),
    uploadedAssetId: ASSET,
  });
  assert.equal(verifyStorySuccessPayload(add, { data: { storyId: STORY } }).ok, false);
  assert.equal(verifyStorySuccessPayload(add, story({})).ok, true);
});
test("R22 F01: a stale response cannot leave the executor mutex locked", async () => {
  const envelope: StoryIntentEnvelope = {
    version: 2,
    action: "update",
    occurrenceKey: "occ-r22-stale-lock",
    method: "PUT",
    path: `/v1/admin/stories/${STORY}`,
    routeTargetId: STORY,
    expectedVersion: 8,
    bodyText: JSON.stringify({ title: "after", locale: "fa", expectedVersion: 8, occurrenceKey: "occ-r22-stale-lock" }),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "admin",
    shopId: null,
    actorUserId: "actor-r22-admin",
    recordedAt: new Date().toISOString(),
  };
  const response = () => new Response(
    JSON.stringify({ data: { storyId: STORY, title: "after", locale: "fa", versionToken: 9, items: [] } }),
    { status: 200, headers: { "Content-Type": "application/json" } },
  );
  const original = globalThis.fetch;
  let resolveFirst: ((value: Response) => void) | null = null;
  let calls = 0;
  globalThis.fetch = (() => new Promise<Response>((resolve) => {
    calls += 1;
    if (calls === 1) resolveFirst = resolve;
    else resolve(response());
  })) as typeof fetch;

  const currentGate = gate();
  const scope = { role: "admin" as const, shopId: null, actorUserId: "actor-r22-admin" };
  const first = runDurableStoryMutation({ gate: currentGate, envelope, scope });
  assert.equal(currentGate.inFlightRef.current, true);
  currentGate.epochRef.current += 1;
  resolveFirst!(response());
  const stale = await first;
  assert.equal(stale.state, "blocked");
  assert.equal(stale.state === "blocked" && stale.reason, "stale_epoch");
  assert.equal(currentGate.inFlightRef.current, false);

  const second = await runDurableStoryMutation({ gate: currentGate, envelope, scope });
  assert.equal(second.state, "ok");
  assert.equal(currentGate.inFlightRef.current, false);
  globalThis.fetch = original;
});
type BulkRow = AdminStorySnapshot;

function bulkRow(index: number): BulkRow {
  const now = new Date().toISOString();
  const hex = index.toString(16).padStart(12, "0");
  return {
    id: `aaaaaaaa-0000-4000-8000-${hex}`,
    storyId: `aaaaaaaa-0000-4000-8000-${hex}`,
    tenantId: "store-alpha",
    origin: "Seller",
    reviewStatus: "Submitted",
    sellerPartyId: SHOP,
    status: "Draft",
    versionToken: index + 1,
    locale: "fa-IR",
    market: null,
    coverMediaAssetId: null,
    coverMediaUrl: null,
    displayOrder: index,
    ctaType: "none",
    ctaTarget: null,
    startAt: null,
    endAt: null,
    createdAt: now,
    updatedAt: now,
    createdByActorUserId: null,
    reviewedByActorUserId: null,
    items: [],
    shopDisplayName: "فروشگاه",
    actionCodes: ["approve", "delete"],
    effectivelyPublic: false,
    contentReady: true,
    rejectionReason: null,
    submittedAt: now,
    reviewedAt: null,
    submittedByActorUserId: null,
    title: `Row-${index}`,
  } as AdminStorySnapshot;
}

test("R20 E: a 1000-row controlled plan renders compact confirmation and one completion toast with zero extra writes on cancel", async () => {
  window.sessionStorage.clear();
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-admin-r20-bulk");
  const rows = Array.from({ length: 1000 }, (_, index) => bulkRow(index));
  const approvePosts: string[] = [];
  let lastQueryBody = "";
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    const body = String(init?.body ?? "");
    if (method === "POST" && /\/approve\/?$/.test(url) && !url.includes("approve-and")) {
      approvePosts.push(url);
      const id = url.match(/stories\/([0-9a-fA-F-]+)\/approve/)?.[1];
      const row = rows.find((candidate) => candidate.storyId === id) ?? rows[0]!;
      return new Response(JSON.stringify({ ...row, reviewStatus: "Approved", actionCodes: ["delete"] }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("locales") || url.includes("locale-options")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("/stories")) {
      if (method === "POST" || url.includes("/query")) lastQueryBody = body + " " + url;
      const hit = rows.find((candidate) => url.includes(candidate.storyId));
      if (method === "GET" && hit) {
        return new Response(JSON.stringify(hit), {
          status: 200,
          headers: { "Content-Type": "application/json" },
        });
      }
      return new Response(JSON.stringify({ items: rows, page: 1, pageSize: 1000, totalCount: rows.length }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;

  const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }));
  try {
    await flush();
    await waitFor(() => qq(tree.container, ".ag-row").length >= 1, { timeoutMs: 8000 });
    const sizeSelect = qq(tree.container, "select").find((node) =>
      Array.from((node as HTMLSelectElement).options).some((option) => option.value === "1000"),
    );
    assert.ok(sizeSelect, "the real grid offers the 1000 page size");
    await setSelectValue(sizeSelect!, "1000");
    await waitFor(() => lastQueryBody.includes("1000"), { timeoutMs: 8000 });
    await waitFor(
      () => /1000|۱٬۰۰۰|۱,۰۰۰/.test(q(tree.container, "[data-app-grid-pagination]").textContent ?? ""),
      { timeoutMs: 20000 },
    );
    const renderedRows = qq(tree.container, ".ag-row").length;
    assert.ok(renderedRows > 0 && renderedRows < 1000, `virtualized row DOM stays bounded, got ${renderedRows}`);

    const headerCheckbox = qq(
      tree.container,
      '.ag-header-row input[type="checkbox"], .ag-header-cell input[type="checkbox"], .ag-header-select-all input[type="checkbox"]',
    )[0];
    assert.ok(
      headerCheckbox,
      `the real grid header select-all exists (header checkboxes=${qq(tree.container, '.ag-header-row input[type="checkbox"], .ag-header-cell input[type="checkbox"]').length})`,
    );
    await click(headerCheckbox!);
    await waitFor(
      () => /1000|۱٬۰۰۰|۱,۰۰۰/.test(q(tree.container, '[data-testid="app-grid-selected-count"]').textContent ?? ""),
      { timeoutMs: 8000 },
    );

    await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]'));
    await flush();
    await waitFor(() => qq(document.body, '[data-testid="story-admin-op-dialog"]').length === 1);
    const dialogText = q(document.body, '[data-testid="story-admin-op-dialog"]').textContent ?? "";
    assert.match(q(document.body, '[data-testid="story-admin-op-processable-count"]').textContent ?? "", /1000|۱٬۰۰۰|۱,۰۰۰/);
    assert.ok(!dialogText.includes("Row-0"), "confirmation never lists 1000 titles");
    assert.ok(dialogText.length < 2000, `confirmation stays compact, got ${dialogText.length} chars`);

    await click(q(document.body, '[data-testid="story-admin-op-cancel"]'));
    await flush();
    assert.equal(approvePosts.length, 0, "cancel performs zero writes for the 1000-row plan");

    // Completion rendering: the same 1000-row plan through the real confirm path ends in ONE toast,
    // no result dialog and no page-sized message block.
    let completionToasts = 0;
    const toastRecord = toast as unknown as Record<string, (...args: unknown[]) => unknown>;
    const originalSuccess = toastRecord.success!.bind(toast);
    toastRecord.success = (...args: unknown[]) => {
      completionToasts += 1;
      return originalSuccess(...args);
    };
    try {
      await click(q(tree.container, '[data-testid="app-grid-bulk-story-bulk-approve"]'));
      await flush();
      await waitFor(() => qq(document.body, '[data-testid="story-admin-op-dialog"]').length === 1);
      await click(q(document.body, '[data-testid="story-admin-op-confirm"]'));
      await waitFor(() => approvePosts.length === 1000, { timeoutMs: 180000 });
      await flush();
      assert.equal(qq(document.body, '[data-testid="story-admin-op-result"]').length, 0, "no 1000-row result dialog");
      assert.ok(completionToasts === 1, `completion is exactly one toast, got ${completionToasts}`);
    } finally {
      toastRecord.success = originalSuccess;
    }
  } finally {
    globalThis.fetch = original;
    await tree.unmount();
  }
});
