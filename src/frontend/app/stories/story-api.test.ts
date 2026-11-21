import assert from "node:assert/strict";
import test from "node:test";
import { fetchPublicStories, mapAdminStory, mapPublicStory } from "./story-api.ts";

test("mapPublicStory maps PascalCase storefront payload", () => {
  const story = mapPublicStory({
    StoryId: "11111111-1111-4111-8111-111111111111",
    Title: "موبایل",
    CoverMediaUrl: "/images/stories/1.jpg",
    IsVideo: false,
    DisplayOrder: 2,
    ShopDisplayName: "فروشگاه آلفا",
    ShopPublicId: "shop-alpha-public",
    Items: [
      {
        StoryItemId: "22222222-2222-4222-8222-222222222222",
        MediaType: "image",
        MediaUrl: "/images/stories/1.jpg",
        Caption: null,
        DurationMs: 5000,
        CtaType: "internal",
        CtaTarget: "/products",
      },
    ],
  });
  assert.ok(story);
  assert.equal(story?.storyId, "11111111-1111-4111-8111-111111111111");
  assert.equal(story?.title, "موبایل");
  assert.equal(story?.displayOrder, 2);
  assert.equal(story?.items.length, 1);
  assert.equal(story?.items[0]?.ctaTarget, "/products");
  assert.equal(story?.items[0]?.durationMs, 5000);
  assert.equal(story?.shopDisplayName, "فروشگاه آلفا");
  assert.equal(story?.shopPublicId, "shop-alpha-public");
});

test("mapPublicStory keeps missing shop public id honest", () => {
  const story = mapPublicStory({
    storyId: "33333333-3333-4333-8333-333333333333",
    title: "ویترین",
    coverMediaUrl: null,
    isVideo: false,
    displayOrder: 0,
    shopDisplayName: "ویترین اصلی",
    shopPublicId: null,
    items: [
      {
        storyItemId: "44444444-4444-4444-8444-444444444444",
        mediaType: "image",
        mediaUrl: "/x.jpg",
        caption: null,
        durationMs: null,
        ctaType: "none",
        ctaTarget: null,
      },
    ],
  });
  assert.equal(story?.shopDisplayName, "ویترین اصلی");
  assert.equal(story?.shopPublicId, null);
});

test("mapAdminStory normalizes numeric status and items", () => {
  const story = mapAdminStory({
    storyId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    tenantId: "a0000000-0001-4000-8000-000000000001",
    locale: "fa",
    market: null,
    title: "بازی",
    coverMediaAssetId: null,
    coverMediaUrl: "/images/stories/video/1.mp4",
    displayOrder: 1,
    startAt: null,
    endAt: null,
    status: 2,
    ctaType: "category",
    ctaTarget: "/offers",
    versionToken: 1,
    createdAt: "2026-08-27T00:00:00Z",
    updatedAt: "2026-08-27T00:00:00Z",
    items: [
      {
        storyItemId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        displayOrder: 0,
        mediaType: "video",
        mediaAssetId: null,
        mediaUrl: "/images/stories/video/1.mp4",
        caption: null,
        durationMs: 8000,
        ctaType: "category",
        ctaTarget: "/offers",
        createdAt: "2026-08-27T00:00:00Z",
        updatedAt: "2026-08-27T00:00:00Z",
      },
    ],
  });
  assert.equal(story?.status, "Active");
  assert.equal(story?.id, story?.storyId);
  assert.equal(story?.items[0]?.mediaType, "video");
  assert.equal(story?.origin, "Admin");
  assert.equal(story?.reviewStatus, "None");
  assert.equal(story?.sellerPartyId, null);
  assert.equal(story?.rejectionReason, null);
});

test("mapAdminStory maps origin/review/seller ownership fields (camel + Pascal)", () => {
  const camel = mapAdminStory({
    storyId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    tenantId: "a0000000-0001-4000-8000-000000000001",
    origin: 1,
    reviewStatus: 3,
    sellerPartyId: "01a030d1-40cb-7000-8abe-6d31739956c5",
    rejectionReason: "محتوای نامناسب",
    submittedAt: "2026-08-27T10:00:00Z",
    reviewedAt: "2026-08-27T11:00:00Z",
    submittedByActorUserId: "actor-submit",
    reviewedByActorUserId: "actor-review",
    locale: "fa",
    market: null,
    title: "فروشنده",
    coverMediaAssetId: null,
    coverMediaUrl: null,
    displayOrder: 0,
    startAt: null,
    endAt: null,
    status: "Draft",
    ctaType: "none",
    ctaTarget: null,
    versionToken: 2,
    createdAt: "2026-08-27T00:00:00Z",
    updatedAt: "2026-08-27T00:00:00Z",
    items: [],
  });
  assert.equal(camel?.origin, "Seller");
  assert.equal(camel?.reviewStatus, "Rejected");
  assert.equal(camel?.sellerPartyId, "01a030d1-40cb-7000-8abe-6d31739956c5");
  assert.equal(camel?.rejectionReason, "محتوای نامناسب");
  assert.equal(camel?.submittedAt, "2026-08-27T10:00:00Z");
  assert.equal(camel?.reviewedAt, "2026-08-27T11:00:00Z");
  assert.equal(camel?.submittedByActorUserId, "actor-submit");
  assert.equal(camel?.reviewedByActorUserId, "actor-review");

  const pascal = mapAdminStory({
    StoryId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
    TenantId: "a0000000-0001-4000-8000-000000000001",
    Origin: "Seller",
    ReviewStatus: "Submitted",
    SellerPartyId: "01a030d1-40cb-7000-8abe-6d31739956c5",
    RejectionReason: null,
    SubmittedAt: "2026-08-27T12:00:00Z",
    ReviewedAt: null,
    SubmittedByActorUserId: "actor-2",
    ReviewedByActorUserId: null,
    Locale: "en",
    Market: null,
    Title: "Pending",
    CoverMediaAssetId: null,
    CoverMediaUrl: null,
    DisplayOrder: 3,
    StartAt: null,
    EndAt: null,
    Status: 0,
    CtaType: "none",
    CtaTarget: null,
    VersionToken: 1,
    CreatedAt: "2026-08-27T00:00:00Z",
    UpdatedAt: "2026-08-27T00:00:00Z",
    Items: [],
  });
  assert.equal(pascal?.origin, "Seller");
  assert.equal(pascal?.reviewStatus, "Submitted");
  assert.equal(pascal?.status, "Draft");
  assert.equal(pascal?.submittedByActorUserId, "actor-2");
  assert.equal(pascal?.reviewedByActorUserId, null);
});

test("fetchPublicStories separates a service failure from an empty catalogue", async () => {
  const original = globalThis.fetch;
  globalThis.fetch = (async () => new Response("{}", { status: 503 })) as typeof fetch;
  try {
    const result = await fetchPublicStories("fa");
    assert.equal(result.state, "error");
  } finally {
    globalThis.fetch = original;
  }
});