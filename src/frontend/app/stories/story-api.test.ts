import assert from "node:assert/strict";
import test from "node:test";
import { mapAdminStory, mapPublicStory } from "./story-api.ts";

test("mapPublicStory maps PascalCase storefront payload", () => {
  const story = mapPublicStory({
    StoryId: "11111111-1111-4111-8111-111111111111",
    Title: "موبایل",
    CoverMediaUrl: "/images/stories/1.jpg",
    IsVideo: false,
    DisplayOrder: 2,
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
});
