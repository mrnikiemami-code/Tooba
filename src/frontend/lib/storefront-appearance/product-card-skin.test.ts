import assert from "node:assert/strict";
import test from "node:test";
import {
  CLASSIC_PRODUCT_CARD_CHROME,
  DEFAULT_PRODUCT_CARD_SKIN,
  isKnownProductCardSkin,
  listProductCardSkins,
  resolveProductCardSkin,
  resolveProductCardSkinChrome,
} from "./product-card-skin.ts";

test("four curated skins resolve with classic default", () => {
  assert.equal(DEFAULT_PRODUCT_CARD_SKIN, "classic");
  assert.deepEqual(listProductCardSkins().map((item) => item.key), ["classic", "clean", "elevated", "glass"]);
  assert.equal(resolveProductCardSkin(null), "classic");
  assert.equal(resolveProductCardSkin("legacy"), "classic");
  assert.equal(resolveProductCardSkin("Clean"), "clean");
  assert.equal(isKnownProductCardSkin("glass"), true);
  assert.equal(isKnownProductCardSkin("custom-html"), false);
  for (const skin of listProductCardSkins()) {
    assert.ok(skin.nameFa.length > 0);
    assert.ok(skin.descriptionFa.length > 0);
    assert.doesNotMatch(skin.nameFa, /classic|clean|elevated|glass/i);
    assert.doesNotMatch(skin.descriptionFa, /classic|clean|elevated|glass/i);
  }
});

test("classic chrome stays the accepted default classes", () => {
  assert.equal(
    CLASSIC_PRODUCT_CARD_CHROME.article,
    "group relative flex flex-col rounded-2xl overflow-hidden bg-surface border border-border hover:shadow-xl hover:shadow-black/40 hover:-translate-y-1 transition-all duration-300",
  );
  assert.equal(CLASSIC_PRODUCT_CARD_CHROME.media, "relative aspect-[4/5] bg-background overflow-hidden");
  assert.equal(
    CLASSIC_PRODUCT_CARD_CHROME.action,
    "w-7 h-7 flex items-center justify-center rounded-full bg-surface-elevated/90 backdrop-blur-sm shadow-md hover:scale-110 transition-transform",
  );
  assert.equal(resolveProductCardSkinChrome("classic"), CLASSIC_PRODUCT_CARD_CHROME);
  for (const skin of listProductCardSkins()) {
    assert.match(skin.chrome.article, /hover:-translate-y-1/);
    assert.match(skin.chrome.media, /aspect-\[4\/5\]/);
  }
});
