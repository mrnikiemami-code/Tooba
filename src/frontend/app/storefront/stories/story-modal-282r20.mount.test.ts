/**
 * TB-282-R20 Gate D/E — the served public player is mounted for real here:
 * a photo dwells only after its media painted, a video slide advances on the real
 * `ended` event (never on the photo timer), and a hidden document pauses the dwell.
 * No helper-only string asserts; the real `StoryModal` is mounted.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { ensureDom, flush, mount, q, qq, waitFor } from "../../../features/order-carrier-execution/test-dom-harness.ts";
import { StoryModal } from "./story-modal.tsx";
import type { PublicStoryCard, PublicStoryItem } from "../../stories/story-api.ts";

ensureDom();

function item(over: Partial<PublicStoryItem> & { storyItemId: string }): PublicStoryItem {
  return {
    mediaType: "image",
    mediaUrl: "https://cdn.example.test/slide.jpg",
    caption: over.caption ?? "زیرنویس",
    durationMs: 200,
    ctaType: "none",
    ctaTarget: null,
    ...over,
  };
}

function story(items: PublicStoryItem[], over: Partial<PublicStoryCard> = {}): PublicStoryCard {
  return {
    storyId: "dddddddd-dddd-dddd-dddd-dddddddd20d1",
    title: "استوری آزمون",
    coverMediaUrl: "https://cdn.example.test/cover.jpg",
    isVideo: false,
    displayOrder: 0,
    ctaType: "none",
    ctaTarget: null,
    items,
    ...over,
  };
}

function captionOf(container: HTMLElement): string {
  return q(container, '[data-testid="story-caption"]')?.textContent?.trim() ?? "";
}

test("R20 D15: a photo dwell starts only after the image actually loads", async () => {
  const stories = [
    story([
      item({ storyItemId: "item-1", caption: "اسلاید یک", mediaUrl: "https://cdn.example.test/one.jpg" }),
      item({ storyItemId: "item-2", caption: "اسلاید دو", mediaUrl: "https://cdn.example.test/two.jpg" }),
    ]),
  ];

  const tree = await mount(
    createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories, initialStoryId: null }),
  );
  await flush();

  assert.equal(captionOf(tree.container), "اسلاید یک");

  // Nothing painted yet: a slow or broken load must not burn the slide's time.
  await new Promise((resolve) => setTimeout(resolve, 450));
  assert.equal(captionOf(tree.container), "اسلاید یک", "no dwell may run before the image load event");

  const img = q(tree.container, "img.object-contain");
  assert.ok(img, "the real image element is mounted");
  img.dispatchEvent(new Event("load"));

  await waitFor(() => captionOf(tree.container) === "اسلاید دو", { timeoutMs: 4000 });
  assert.equal(captionOf(tree.container), "اسلاید دو", "a loaded photo dwells and advances on its own duration");

  await tree.unmount();
});

test("R20 D15: a video slide waits for the real ended event, not for the photo duration", async () => {
  const stories = [
    story([
      item({
        storyItemId: "vid-1",
        mediaType: "video",
        mediaUrl: "https://cdn.example.test/clip.mp4",
        caption: "ویدیو",
        durationMs: 60_000,
      }),
      item({ storyItemId: "img-2", caption: "بعد از ویدیو", mediaUrl: "https://cdn.example.test/after.jpg" }),
    ]),
  ];

  const tree = await mount(
    createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories, initialStoryId: null }),
  );
  await flush();

  assert.equal(captionOf(tree.container), "ویدیو");
  const video = q(tree.container, "video");
  assert.ok(video, "a video slide renders the real <video> element");

  await new Promise((resolve) => setTimeout(resolve, 500));
  assert.equal(captionOf(tree.container), "ویدیو", "the photo dwell timer must never advance a video slide");

  video.dispatchEvent(new Event("ended"));
  await waitFor(() => captionOf(tree.container) === "بعد از ویدیو", { timeoutMs: 2000 });
  assert.equal(captionOf(tree.container), "بعد از ویدیو", "the real ended event advances the player");

  await tree.unmount();
});

test("R20 D15: a hidden document pauses the photo dwell and resumes when visible again", async () => {
  const stories = [
    story([
      item({ storyItemId: "h-1", caption: "قبل از پنهان", mediaUrl: "https://cdn.example.test/h1.jpg" }),
      item({ storyItemId: "h-2", caption: "بعد از پنهان", mediaUrl: "https://cdn.example.test/h2.jpg" }),
    ]),
  ];

  const tree = await mount(
    createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories, initialStoryId: null }),
  );
  await flush();

  q(tree.container, "img.object-contain")?.dispatchEvent(new Event("load"));
  await flush();

  const setVisibility = (value: "hidden" | "visible") => {
    Object.defineProperty(document, "visibilityState", { configurable: true, get: () => value });
    document.dispatchEvent(new Event("visibilitychange"));
  };

  setVisibility("hidden");
  await flush();
  await new Promise((resolve) => setTimeout(resolve, 450));
  assert.equal(captionOf(tree.container), "قبل از پنهان", "a hidden document must not consume the slide");

  setVisibility("visible");
  await waitFor(() => captionOf(tree.container) === "بعد از پنهان", { timeoutMs: 4000 });

  await tree.unmount();
});
