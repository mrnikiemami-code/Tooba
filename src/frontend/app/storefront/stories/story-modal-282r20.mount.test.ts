/**
 * TB-282-R20 Gate D/E — the served public player is mounted for real here:
 * a photo dwells only after its media painted, a video slide advances on the real
 * `ended` event (never on the photo timer), and a hidden document pauses the dwell.
 * No helper-only string asserts; the real `StoryModal` is mounted.
 */
import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement, useEffect, useState } from "react";
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

test("R22 F05: a data refresh keeps the current story and slide instead of resetting to the first", async () => {
  const storyA = story([item({ storyItemId: "a-1", caption: "A یک", mediaUrl: "https://cdn.example.test/a1.jpg" })], { storyId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa22a1", title: "A" });
  const storyB = story([
    item({ storyItemId: "b-1", caption: "B یک", mediaUrl: "https://cdn.example.test/b1.jpg" }),
    item({ storyItemId: "b-2", caption: "B دو", mediaUrl: "https://cdn.example.test/b2.jpg" }),
  ], { storyId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb22b1", title: "B" });
  const tree = await mount(createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories: [storyA, storyB], initialStoryId: storyA.storyId }));
  await flush();
  window.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowRight" }));
  await flush();
  window.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowRight" }));
  await flush();
  assert.equal(captionOf(tree.container), "B دو");

  const refreshedB = story(storyB.items, { storyId: storyB.storyId, title: "B" });
  await tree.rerender(createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories: [storyA, refreshedB], initialStoryId: storyA.storyId }));
  await flush();
  assert.equal(captionOf(tree.container), "B دو", "refresh must preserve the current story/item");
  await tree.unmount();
});

test("R22 F05: removing the current story closes the player instead of switching silently", async () => {
  const storyA = story([item({ storyItemId: "ra-1", caption: "A", mediaUrl: "https://cdn.example.test/ra.jpg" })], { storyId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa22a2", title: "A" });
  const storyB = story([item({ storyItemId: "rb-1", caption: "B", mediaUrl: "https://cdn.example.test/rb.jpg" })], { storyId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb22b2", title: "B" });
  let closed = false;
  const tree = await mount(createElement(StoryModal, { isOpen: true, onClose: () => { closed = true; }, stories: [storyA, storyB], initialStoryId: storyB.storyId }));
  await flush();
  await tree.rerender(createElement(StoryModal, { isOpen: true, onClose: () => { closed = true; }, stories: [storyA], initialStoryId: storyB.storyId }));
  await flush();
  assert.equal(closed, true);
  assert.equal(qq(tree.container, '[data-testid="story-modal"]').length, 0);
  await tree.unmount();
});

test("R22 F05: reordering the story array does not change the playing story", async () => {
  const storyA = story([item({ storyItemId: "oa-1", caption: "A", mediaUrl: "https://cdn.example.test/oa.jpg" })], { storyId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa22a3", title: "A" });
  const storyB = story([item({ storyItemId: "ob-1", caption: "B", mediaUrl: "https://cdn.example.test/ob.jpg" })], { storyId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb22b3", title: "B" });
  const storyX = story([item({ storyItemId: "ox-1", caption: "X", mediaUrl: "https://cdn.example.test/ox.jpg" })], { storyId: "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxx22x1", title: "X" });
  const tree = await mount(createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories: [storyA, storyB], initialStoryId: storyA.storyId }));
  await flush();
  window.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowRight" }));
  await flush();
  assert.equal(captionOf(tree.container), "B");
  await tree.rerender(createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories: [storyX, storyA, storyB], initialStoryId: storyA.storyId }));
  await flush();
  assert.equal(captionOf(tree.container), "B", "the selected story id, not the old array index, must survive reorder");
  await tree.unmount();
});

test("R22 F05: reordering slides inside the playing story keeps the current slide", async () => {
  const body = story([
    item({ storyItemId: "s-1", caption: "Slide one", mediaUrl: "https://cdn.example.test/s1.jpg" }),
    item({ storyItemId: "s-2", caption: "Slide two", mediaUrl: "https://cdn.example.test/s2.jpg" }),
  ], { storyId: "cccccccc-cccc-cccc-cccc-cccccccc22c1", title: "C" });
  const tree = await mount(createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories: [body], initialStoryId: body.storyId }));
  await flush();
  window.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowRight" }));
  await flush();
  assert.equal(captionOf(tree.container), "Slide two");
  const reordered = story([body.items[1]!, body.items[0]!], { storyId: body.storyId, title: body.title });
  await tree.rerender(createElement(StoryModal, { isOpen: true, onClose: () => undefined, stories: [reordered], initialStoryId: body.storyId }));
  await flush();
  assert.equal(captionOf(tree.container), "Slide two", "the selected slide id must survive slide reorder");
  await tree.unmount();
});

test("R22 F05: navigating inside the player reports the real current story to its host", async () => {
  const storyA = story([item({ storyItemId: "ca-1", caption: "A", mediaUrl: "https://cdn.example.test/ca.jpg" })], { storyId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa22a4", title: "A" });
  const storyB = story([item({ storyItemId: "cb-1", caption: "B", mediaUrl: "https://cdn.example.test/cb.jpg" })], { storyId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb22b4", title: "B" });
  let current = storyA.storyId;
  const tree = await mount(createElement(StoryModal, {
    isOpen: true,
    onClose: () => undefined,
    stories: [storyA, storyB],
    initialStoryId: storyA.storyId,
    onCurrentStoryChange: (id: string) => { current = id; },
  }));
  await flush();
  window.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowRight" }));
  await flush();
  assert.equal(current, storyB.storyId);
  await tree.unmount();
});

function RemovalHost({ first, second }: { first: string; second: string }) {
  const [stories, setStories] = useState<PublicStoryCard[]>(() => [
    story([item({ storyItemId: "host-a", caption: "A", mediaUrl: "https://cdn.example.test/host-a.jpg" })], { storyId: first, title: "A" }),
    story([item({ storyItemId: "host-b", caption: "B", mediaUrl: "https://cdn.example.test/host-b.jpg" })], { storyId: second, title: "B" }),
  ]);
  const [selected, setSelected] = useState<string | null>(first);
  const [open, setOpen] = useState(true);
  useEffect(() => {
    if (!open || !selected) return;
    if (!stories.some((row) => row.storyId === selected)) setOpen(false);
  }, [open, selected, stories]);
  return createElement("div", null,
    createElement(StoryModal, {
      isOpen: open,
      onClose: () => setOpen(false),
      stories,
      initialStoryId: first,
      onCurrentStoryChange: setSelected,
    }),
    createElement("button", {
      "data-testid": "remove-a",
      onClick: () => setStories((rows) => rows.filter((row) => row.storyId !== first)),
    }, "remove A"),
    createElement("span", { "data-testid": "open-state" }, open ? "open" : "closed"),
  );
}

test("R22 F05: open A, go to B, then remove A keeps the healthy B player open", async () => {
  const tree = await mount(createElement(RemovalHost, {
    first: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa22a5",
    second: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb22b5",
  }));
  await flush();
  window.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowRight" }));
  await flush();
  (q(tree.container, '[data-testid="remove-a"]') as HTMLButtonElement).click();
  await flush();
  assert.equal(q(tree.container, '[data-testid="open-state"]').textContent, "open");
  await tree.unmount();
});
