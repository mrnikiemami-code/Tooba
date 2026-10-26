/**
 * Focused facts for the primary method list and its DB-backed authoring context.
 *
 * These are the EXACT helpers the list screen calls: the strict decoders of the authorized read payloads
 * and the pure binding/name derivation. No rendering, no network, no fixtures that differ from the wire.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { parseJsonNode, type JsonNode } from "./api/wire-json.ts";
import {
  authoringOwnerKey,
  decodeAuthoringMethodRows,
  decodeAuthoringContext,
  decodeAuthoringOwner,
  duplicateBindingDraftIds,
  methodBinding,
  storedMethodName,
} from "./api/authoring-method-list-api.ts";

const DRAFT = "01a16eeb-70b3-7000-87c2-564c82835496";
const OTHER_DRAFT = "11111111-2222-3333-4444-555555555555";
const FA = "aaaaaaaa-0000-4000-8000-000000000001";
const EN = "bbbbbbbb-0000-4000-8000-000000000002";
const SNAPSHOT = "cccccccc-0000-4000-8000-000000000003";

function nodeOf(value: unknown): JsonNode {
  const node = parseJsonNode(JSON.stringify(value));
  assert.ok(node, "fixture must be valid JSON");
  return node;
}

/* ————————————————— the REAL list wire ————————————————— */

test("list decode keeps the server draft id and reads the stored text of the real default language", () => {
  const rows = decodeAuthoringMethodRows(nodeOf({
    drafts: [
      {
        draftId: DRAFT,
        owner: { ownerKind: "Store", sellerPartyId: null },
        serviceCode: "post",
        optionCode: null,
        revision: 2,
        createdAt: "2026-10-25T00:00:00+00:00",
        updatedAt: "2026-10-25T00:00:00+00:00",
        regionGroups: [{ groupId: SNAPSHOT, order: 0, areas: [] }],
        methodTranslations: [
          { languageId: FA, name: "پست عادی", description: null },
          { languageId: EN, name: "Regular post", description: "x" },
        ],
      },
    ],
  }));

  assert.equal(rows.length, 1);
  const row = rows[0]!;
  assert.equal(row.id, DRAFT, "stable row identity is the server draft id, never a fabricated method code");
  assert.equal(row.draftId, DRAFT);
  assert.equal(row.revision, 2);
  assert.equal(row.areaCount, 1);
  assert.equal(methodBinding(row), "post", "a NULL option stays NULL and is never split onto post:standard");
  assert.equal(storedMethodName(row, FA), "پست عادی");
  assert.equal(storedMethodName(row, EN), "Regular post");
});

test("a missing default-language text is honest 'no name', never another locale's name", () => {
  const rows = decodeAuthoringMethodRows(nodeOf({
    drafts: [{
      draftId: DRAFT,
      serviceCode: "post",
      optionCode: "express",
      revision: 1,
      regionGroups: [],
      methodTranslations: [{ languageId: EN, name: "Express", description: null }],
    }],
  }));
  assert.equal(storedMethodName(rows[0]!, FA), null);
  assert.equal(storedMethodName(rows[0]!, EN), "Express");
  assert.equal(methodBinding(rows[0]!), "post:express");
});

test("the list refuses a rounded 64-bit revision and a duplicated draft row", () => {
  assert.throws(() => decodeAuthoringMethodRows(nodeOf({
    drafts: [{ draftId: DRAFT, serviceCode: "post", optionCode: null, revision: 9007199254740993, regionGroups: [] }],
  })));
  assert.throws(() => decodeAuthoringMethodRows(nodeOf({
    drafts: [
      { draftId: DRAFT, serviceCode: "post", optionCode: null, revision: 1, regionGroups: [] },
      { draftId: DRAFT, serviceCode: "post", optionCode: "express", revision: 1, regionGroups: [] },
    ],
  })));
});

test("the authoring context must resolve its own default language", () => {
  const context = decodeAuthoringContext(nodeOf({
    edition: "SingleStore",
    defaultCurrency: "IRR",
    contentLanguages: [
      { languageId: FA, code: "fa-IR", culture: "fa-IR", urlPrefix: "", direction: "rtl", displayName: "فارسی", nativeName: "فارسی", isDefault: true, sortOrder: 0 },
      { languageId: EN, code: "en-US", culture: "en-US", urlPrefix: "en", direction: "ltr", displayName: "English", nativeName: "English", isDefault: false, sortOrder: 1 },
    ],
    defaultContentLanguageId: FA,
  }));
  assert.equal(context.defaultContentLanguageId, FA);
  assert.equal(context.contentLanguages.length, 2);

  assert.throws(() => decodeAuthoringContext(nodeOf({
    contentLanguages: [{ languageId: EN, code: "en", displayName: "English", nativeName: "English", isDefault: true }],
    defaultContentLanguageId: FA,
  })));
});

test("two rows with the same exact binding are flagged, other bindings are not", () => {
  const rows = decodeAuthoringMethodRows(nodeOf({
    drafts: [
      { draftId: DRAFT, serviceCode: "post", optionCode: null, revision: 1, regionGroups: [] },
      { draftId: OTHER_DRAFT, serviceCode: "post", optionCode: null, revision: 3, regionGroups: [] },
    ],
  }));
  const duplicates = duplicateBindingDraftIds(rows);
  assert.equal(duplicates.size, 2);
  assert.ok(duplicates.has(DRAFT) && duplicates.has(OTHER_DRAFT));
});

/* ————————————————— the authorized owner + separate surfaces (TB-223) ————————————————— */

test("223-01 the authorized owner is decoded as exactly one lawful scope", () => {
  assert.deepEqual(decodeAuthoringOwner(nodeOf({ ownerKind: "Store", sellerPartyId: null })), {
    ownerKind: "Store",
    sellerPartyId: null,
  });
  assert.deepEqual(decodeAuthoringOwner(nodeOf({ ownerKind: "Marketplace" })), {
    ownerKind: "Marketplace",
    sellerPartyId: null,
  });
  const seller = decodeAuthoringOwner(nodeOf({ ownerKind: "Seller", sellerPartyId: SNAPSHOT }));
  assert.equal(seller.ownerKind, "Seller");
  assert.equal(seller.sellerPartyId, SNAPSHOT);
});

test("223-02 a contradictory or unknown owner is untrusted, never repaired into a scope", () => {
  // A store/marketplace carrying a seller id, a seller without one, and an unknown kind are all refused.
  assert.throws(() => decodeAuthoringOwner(nodeOf({ ownerKind: "Store", sellerPartyId: SNAPSHOT })));
  assert.throws(() => decodeAuthoringOwner(nodeOf({ ownerKind: "Marketplace", sellerPartyId: SNAPSHOT })));
  assert.throws(() => decodeAuthoringOwner(nodeOf({ ownerKind: "Seller", sellerPartyId: null })));
  assert.throws(() => decodeAuthoringOwner(nodeOf({ ownerKind: "Seller", sellerPartyId: "00000000-0000-0000-0000-000000000000" })));
  assert.throws(() => decodeAuthoringOwner(nodeOf({ ownerKind: "Reseller", sellerPartyId: null })));
});

test("223-03 each lawful owner has its own grid namespace, so views never cross surfaces", () => {
  assert.equal(authoringOwnerKey({ ownerKind: "Store", sellerPartyId: null }), "store");
  assert.equal(authoringOwnerKey({ ownerKind: "Marketplace", sellerPartyId: null }), "marketplace");
  assert.equal(authoringOwnerKey({ ownerKind: "Seller", sellerPartyId: SNAPSHOT.toUpperCase() }), `seller:${SNAPSHOT}`);
  assert.notEqual(
    authoringOwnerKey({ ownerKind: "Seller", sellerPartyId: SNAPSHOT }),
    authoringOwnerKey({ ownerKind: "Seller", sellerPartyId: OTHER_DRAFT }),
  );
});
