import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { ensureDom, flush, mount, q, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryPreviewPlayer } from "./story-preview-player.tsx";
import { faStoryCopy } from "./story-management-copy.ts";
import type { AdminStoryItem } from "../story-api.ts";

ensureDom();

const item = (over: Partial<AdminStoryItem> & { storyItemId: string }): AdminStoryItem => ({
  storyItemId: over.storyItemId,
  displayOrder: 0,
  mediaType: "image",
  mediaAssetId: null,
  mediaUrl: "https://cdn.example.test/slide.jpg",
  caption: over.caption ?? "زیرنویس",
  durationMs: 200,
  ctaType: "none",
  ctaTarget: null,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...over,
});

test("admin preview dwells a photo only after it actually painted", async () => {
  const tree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [
        item({ storyItemId: "p1", caption: "عکس یک", mediaUrl: "https://cdn.example.test/one.jpg" }),
        item({ storyItemId: "p2", caption: "عکس دو", mediaUrl: "https://cdn.example.test/two.jpg" }),
      ],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  assert.match(q(tree.container, '[data-testid="story-preview-caption"]')?.textContent ?? "", /عکس یک/);
  await new Promise((resolve) => setTimeout(resolve, 450));
  assert.match(q(tree.container, '[data-testid="story-preview-caption"]')?.textContent ?? "", /عکس یک/);
  q(tree.container, '[data-testid="story-preview-image"]')?.dispatchEvent(new Event("load"));
  await waitFor(
    () => /عکس دو/.test(q(tree.container, '[data-testid="story-preview-caption"]')?.textContent ?? ""),
    { timeoutMs: 3000 },
  );
  await tree.unmount();
});

test("admin preview waits for the real video ended event, not the photo timer", async () => {
  const tree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [
        item({
          storyItemId: "v1",
          mediaType: "video",
          mediaUrl: "https://cdn.example.test/clip.mp4",
          caption: "ویدیو",
          durationMs: 60_000,
        }),
        item({ storyItemId: "p2", caption: "پس از ویدیو", mediaUrl: "https://cdn.example.test/after.jpg" }),
      ],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  assert.match(q(tree.container, '[data-testid="story-preview-caption"]')?.textContent ?? "", /ویدیو/);
  await new Promise((resolve) => setTimeout(resolve, 450));
  assert.match(q(tree.container, '[data-testid="story-preview-caption"]')?.textContent ?? "", /ویدیو/);
  q(tree.container, '[data-testid="story-preview-video"]')?.dispatchEvent(new Event("ended"));
  await waitFor(
    () => /پس از ویدیو/.test(q(tree.container, '[data-testid="story-preview-caption"]')?.textContent ?? ""),
    { timeoutMs: 2000 },
  );
  await tree.unmount();
});
