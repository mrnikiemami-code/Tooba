/**
 * TB-282-R19 — owning route binding for admin stop / seller media, state-effect success
 * verification, honest "approved but not published" display, and item-level public playback.
 * These mount the real workspace and the real public modal; no helper-only string asserts.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { ensureDom, flush, mount, q, qq, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryManagementScreen } from "./StoryManagementScreen.tsx";
import { ADMIN_STORY_CAPABILITIES } from "./story-capabilities.ts";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";
import { bindStoryEnvelopeRoute, verifyStorySuccessPayload } from "./story-response.ts";
import { faStoryCopy } from "./story-management-copy.ts";
import type { StoryIntentEnvelope } from "./story-intent.ts";
import type { AdminStorySnapshot, PublicStoryCard } from "../story-api.ts";
import { StoryModal } from "../../storefront/stories/story-modal.tsx";

ensureDom();

const ID_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa19a1";
const SHOP = "22222222-2222-2222-2222-222222221919";
const ACTOR = "actor-admin-r19";

function envelope(over: Partial<StoryIntentEnvelope> & { action: StoryIntentEnvelope["action"]; path: string }): StoryIntentEnvelope {
  return {
    version: 2,
    occurrenceKey: "occ-r19",
    method: "POST",
    routeTargetId: ID_A,
    expectedVersion: "4",
    bodyText: JSON.stringify({ expectedVersion: "4", occurrenceKey: "occ-r19" }),
    uploadMeta: null,
    addBodyText: null,
    uploadedAssetId: null,
    children: [],
    role: "admin",
    shopId: null,
    actorUserId: ACTOR,
    recordedAt: new Date().toISOString(),
    ...over,
  };
}

test("R19 A: admin stop binds the owning /disable route, seller keeps /stop", () => {
  const admin = envelope({ action: "stop", path: `/v1/admin/stories/${ID_A}/disable` });
  assert.equal(bindStoryEnvelopeRoute(admin).ok, true);

  const seller = envelope({ action: "stop", role: "seller", path: `/v1/seller/stories/${ID_A}/stop` });
  assert.equal(bindStoryEnvelopeRoute(seller).ok, true);

  // Cross-role paths stay refused: an admin stop is never sent to the seller endpoint.
  const crossed = envelope({ action: "stop", role: "admin", path: `/v1/seller/stories/${ID_A}/stop` });
  assert.equal(bindStoryEnvelopeRoute(crossed).ok, false);
});

test("R19 A: seller media upload binds the real /v1/seller/media endpoint", () => {
  const upload = envelope({
    action: "upload",
    role: "seller",
    path: "/v1/seller/media",
    bodyText: null,
    uploadMeta: { fileName: "a.jpg", fileType: "image/jpeg", fileSize: 10, digestHex: "pending" },
  });
  assert.equal(bindStoryEnvelopeRoute(upload).ok, true);

  const wrong = envelope({
    action: "upload",
    role: "seller",
    path: "/v1/seller/media/upload-story",
    bodyText: null,
    uploadMeta: { fileName: "a.jpg", fileType: "image/jpeg", fileSize: 10, digestHex: "pending" },
  });
  assert.equal(bindStoryEnvelopeRoute(wrong).ok, false);
});

test("R19 A: same-id unchanged Draft is not approve completion; a real Approved snapshot is", () => {
  const approve = envelope({ action: "approve", path: `/v1/admin/stories/${ID_A}/approve` });

  const unchanged = verifyStorySuccessPayload(approve, {
    data: { storyId: ID_A, status: "Draft", reviewStatus: "None", versionToken: "5" },
  });
  assert.equal(unchanged.ok, false);

  const approved = verifyStorySuccessPayload(approve, {
    data: { storyId: ID_A, status: "Draft", reviewStatus: "Approved", versionToken: "6" },
  });
  assert.equal(approved.ok, true);

  // A partial-but-truthful reply with no state fields keeps the existing identity binding.
  const partial = verifyStorySuccessPayload(approve, { data: { storyId: ID_A } });
  assert.equal(partial.ok, true);

  const stop = envelope({ action: "stop", path: `/v1/admin/stories/${ID_A}/disable` });
  assert.equal(verifyStorySuccessPayload(stop, { data: { storyId: ID_A, status: "Active" } }).ok, false);
  assert.equal(verifyStorySuccessPayload(stop, { data: { storyId: ID_A, status: "Disabled" } }).ok, true);
});

function story(over: Partial<AdminStorySnapshot> & { storyId: string; title: string }): AdminStorySnapshot {
  const now = new Date().toISOString();
  return {
    id: over.storyId,
    tenantId: "store-alpha",
    origin: "Seller",
    reviewStatus: "Approved",
    sellerPartyId: SHOP,
    status: "Draft",
    versionToken: 6,
    locale: "fa-IR",
    market: null,
    coverMediaAssetId: null,
    coverMediaUrl: null,
    displayOrder: 0,
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
    actionCodes: ["update", "add_item", "delete", "return_to_draft"],
    effectivelyPublic: false,
    contentReady: true,
    rejectionReason: null,
    submittedAt: now,
    reviewedAt: now,
    submittedByActorUserId: null,
    ...over,
  };
}

function installFetch(rows: AdminStorySnapshot[]) {
  const original = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    if (method === "POST" && url.includes("/stories/query")) {
      return new Response(JSON.stringify({ items: rows, page: 1, pageSize: 50, totalCount: rows.length }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    if (url.includes("locale-options") || url.includes("locales")) {
      return new Response(JSON.stringify([{ code: "fa-IR", name: "فارسی", isDefault: true }]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      });
    }
    return new Response("{}", { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
  return { restore() { globalThis.fetch = original; } };
}

test("R19 A: approved-but-unpublished row never claims the storefront", async () => {
  window.sessionStorage.clear();
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, ACTOR);
  const row = story({ storyId: ID_A, title: "تأییدشده بدون انتشار" });
  const spy = installFetch([row]);

  const tree = await mount(createElement(StoryManagementScreen, { capabilities: ADMIN_STORY_CAPABILITIES }));
  await flush();
  await waitFor(() => qq(tree.container, ".ag-row").length >= 1, { timeoutMs: 5000 });

  const text = tree.container.textContent ?? "";
  assert.match(text, new RegExp(faStoryCopy.displayApprovedUnpublished));
  assert.doesNotMatch(text, new RegExp(faStoryCopy.displayOnStorefront));

  spy.restore();
  await tree.unmount();
});

test("R19 D: a photo cover of a story that also has video dwells as an image and shows its caption", async () => {
  const stories: PublicStoryCard[] = [
    {
      storyId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb19b1",
      title: "استوری فروشگاه",
      coverMediaUrl: "https://cdn.example.test/cover.jpg",
      isVideo: true,
      displayOrder: 0,
      ctaType: "none",
      ctaTarget: null,
      items: [
        {
          storyItemId: "cccccccc-cccc-cccc-cccc-cccccccc19c1",
          // Legacy payloads may omit mediaType: the JPEG URL itself must win over story.isVideo.
          mediaType: "",
          mediaUrl: "https://cdn.example.test/cover.jpg",
          caption: "متن واقعی اسلاید",
          durationMs: 5000,
          ctaType: "none",
          ctaTarget: null,
        },
      ],
    },
  ];

  const tree = await mount(
    createElement(StoryModal, {
      isOpen: true,
      onClose: () => undefined,
      stories,
      initialStoryId: null,
    }),
  );
  await flush();

  assert.equal(qq(tree.container, "video").length, 0, "a JPEG cover must not render as a video slide");
  assert.equal(qq(tree.container, "img.object-contain").length, 1, "the image slide renders its real media");
  assert.match(q(tree.container, '[data-testid="story-caption"]')?.textContent ?? "", /متن واقعی اسلاید/);
  assert.ok(q(tree.container, '[role="dialog"][aria-modal="true"]'), "the modal exposes dialog semantics");

  await tree.unmount();
});
