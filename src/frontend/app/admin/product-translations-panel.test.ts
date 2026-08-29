import assert from "node:assert/strict";
import test from "node:test";
import { translationReadiness } from "./product-translations-panel.tsx";

test("translationReadiness distinguishes missing partial complete", () => {
  assert.equal(
    translationReadiness({ name: "", shortDescription: "", description: "", seoTitle: "", seoDescription: "" }),
    "missing",
  );
  assert.equal(
    translationReadiness({ name: "Hat", shortDescription: "", description: "", seoTitle: "", seoDescription: "" }),
    "partial",
  );
  assert.equal(
    translationReadiness({
      name: "Hat",
      shortDescription: "Warm",
      description: "Full text",
      seoTitle: "SEO",
      seoDescription: "Meta",
    }),
    "complete",
  );
});
