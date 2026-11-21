import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { click, ensureDom, flush, mount, q, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
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

test("admin preview exposes the slide CTA destination and a safe href", async () => {
  const tree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [item({
        storyItemId: "cta-1",
        caption: "CTA",
        ctaType: "external",
        ctaTarget: "https://example.test/offer",
        ctaHref: "https://example.test/offer",
      })],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  const cta = q(tree.container, '[data-testid="story-preview-cta"]') as HTMLAnchorElement | null;
  assert.ok(cta);
  assert.equal(cta!.getAttribute("href"), "https://example.test/offer");
  assert.equal(cta!.getAttribute("target"), null);
  assert.match(q(tree.container, '[data-testid="story-preview-cta-destination"]')?.textContent ?? "", /پیوند بیرونی/);
  await tree.unmount();
});

test("admin preview video starts muted but can be unmuted", async () => {
  const tree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [item({
        storyItemId: "video-sound",
        mediaType: "video",
        mediaUrl: "https://cdn.example.test/sound.mp4",
        caption: "ویدیو با صدا",
      })],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  const video = q(tree.container, '[data-testid="story-preview-video"]') as HTMLVideoElement;
  assert.equal(video.muted, true);
  await click(q(tree.container, '[data-testid="story-preview-mute"]'));
  await flush();
  assert.equal(video.muted, false);
  await tree.unmount();
});

test("admin preview never double-prefixes an already localized CTA", async () => {
  const tree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [item({
        storyItemId: "cta-localized",
        ctaType: "internal",
        ctaTarget: "/en/products/one",
        ctaHref: "/en/products/one",
      })],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  const cta = q(tree.container, '[data-testid="story-preview-cta"]') as HTMLAnchorElement | null;
  assert.equal(cta?.getAttribute("href"), "/en/products/one");
  await tree.unmount();
});

test("admin preview localizes an internal CTA through the shared locale authority", async () => {
  const tree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [item({
        storyItemId: "cta-internal",
        ctaType: "internal",
        ctaTarget: "/products/two",
      })],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  const cta = q(tree.container, '[data-testid="story-preview-cta"]') as HTMLAnchorElement | null;
  assert.equal(cta?.getAttribute("href"), "/fa/products/two");
  await tree.unmount();
});

test("R22 F12: a single video stops at ended and a single image stops at the end", async () => {
  const videoTree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [item({ storyItemId: "only-video", mediaType: "video", mediaUrl: "https://cdn.example.test/only.mp4", caption: "تنها ویدیو" })],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  q(videoTree.container, '[data-testid="story-preview-video"]')?.dispatchEvent(new Event("ended"));
  await flush();
  const videoPlayer = q(videoTree.container, '[data-testid="story-preview-player"]');
  assert.equal(videoPlayer?.getAttribute("data-preview-paused"), "true");
  assert.equal(videoPlayer?.getAttribute("data-preview-index"), "1");
  await videoTree.unmount();

  const imageTree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [item({ storyItemId: "only-image", mediaUrl: "https://cdn.example.test/only.jpg", caption: "تنها عکس" })],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  await click(q(imageTree.container, '[aria-label="اسلاید بعدی"]'));
  await flush();
  const imagePlayer = q(imageTree.container, '[data-testid="story-preview-player"]');
  assert.equal(imagePlayer?.getAttribute("data-preview-paused"), "true");
  assert.equal(imagePlayer?.getAttribute("data-preview-index"), "1");
  await imageTree.unmount();
});

test("R22 F12: a failed image is a visible media error, not a completed dwell", async () => {
  const tree = await mount(
    createElement(StoryPreviewPlayer, {
      items: [item({ storyItemId: "broken", mediaUrl: "https://cdn.example.test/broken.jpg", caption: "خراب" })],
      copy: faStoryCopy,
      numberLocale: "fa-IR",
      testId: "story-edit-preview",
    }),
  );
  await flush();
  q(tree.container, '[data-testid="story-preview-image"]')?.dispatchEvent(new Event("error"));
  await flush();
  assert.ok(q(tree.container, '[data-testid="story-preview-media-error"]'));
  await tree.unmount();
});
