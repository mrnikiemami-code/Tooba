import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  buildEditorDocumentBody,
  decodeEditorDocument,
  editorDocumentPath,
  loadEditorDocument,
  parseJsonNode,
  saveEditorDocument,
} from "../index.ts";
import type { AreaAncestryResolver, EditorEdits } from "../index.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const GROUP = "22222222-2222-4222-8222-222222222222";
const ROW = "33333333-3333-4333-8333-333333333333";
const LANGUAGE_FA = "44444444-4444-4444-8444-444444444444";
const LANGUAGE_EN = "55555555-5555-4555-8555-555555555555";
const LANGUAGE_AR = "66666666-6666-4666-8666-666666666666";
const LANGUAGE_STORED_INACTIVE = "77777777-7777-4777-8777-777777777777";
const TYPE = "88888888-8888-4888-8888-888888888888";
const COUNTRY = "99999999-9999-4999-8999-999999999999";
const CITY = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const ADMIN_AREA = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

/**
 * Emits the exact number tokens the server writes for `decimal` values (`@scale@` sentinels keep the
 * trailing zeros that `JSON.stringify` would drop). Everything else stays plain fixture JSON.
 */
function rawNumbers(text: string): string {
  return text.replace(/"@(-?[0-9]+\.[0-9]+)@"/g, "$1");
}

/** Real composite wire shape: camelCase, string enums, nulls omitted (`WhenWritingNull`). */
const WIRE = rawNumbers(JSON.stringify({
  draftId: DRAFT,
  owner: { kind: "Marketplace" },
  serviceCode: "tooba.post",
  revision: 7,
  createdAt: "2026-10-23T00:15:00.1234567+03:30",
  updatedAt: "2026-10-23T01:45:10.0000000+03:30",
  type: {
    draftId: DRAFT,
    revision: 7,
    shippingTypeId: TYPE,
    resolution: "Active",
    shippingType: {
      shippingTypeId: TYPE,
      code: "express",
      sortOrder: 2,
      revision: 4,
      translations: [
        { languageId: LANGUAGE_FA, name: "پیشتاز", description: "ارسال سریع" },
        { languageId: LANGUAGE_STORED_INACTIVE, name: "Stored inactive text" },
      ],
    },
  },
  settings: { freightPaymentMode: "CollectOnDelivery", calculationMode: "Manual" },
  presentation: {
    methodTranslations: [
      { languageId: LANGUAGE_FA, name: "پست پیشتاز" },
      { languageId: LANGUAGE_STORED_INACTIVE, name: "inactive stored name" },
    ],
    groupTitles: [
      { languageId: LANGUAGE_FA, title: "  تهران و حومه  " },
      { languageId: LANGUAGE_STORED_INACTIVE, title: "kept inactive title" },
    ],
    contentLanguages: [
      {
        languageId: LANGUAGE_EN,
        code: "en",
        culture: "en-US",
        urlPrefix: "en",
        direction: "ltr",
        displayName: "English",
        nativeName: "English",
        isDefault: true,
        sortOrder: 1,
      },
      {
        languageId: LANGUAGE_FA,
        code: "fa",
        culture: "fa-IR",
        urlPrefix: "",
        direction: "rtl",
        displayName: "فارسی",
        nativeName: "فارسی",
        isDefault: false,
        sortOrder: 2,
      },
      {
        languageId: LANGUAGE_AR,
        code: "ar",
        culture: "ar-SA",
        urlPrefix: "ar",
        direction: "rtl",
        displayName: "العربية",
        nativeName: "العربية",
        isDefault: false,
        sortOrder: 3,
      },
    ],
    defaultContentLanguageId: LANGUAGE_EN,
    hasDefaultLanguageName: true,
  },
  weeklySchedule: {
    timeZoneId: "Asia/Tehran",
    days: [1, 2, 3, 4, 5, 6, 7].map((weekday) => ({
      weekday,
      isWorking: weekday <= 5,
      startTime: "09:30:00",
      endTime: "18:00:00.5000000",
    })),
  },
  regionGroups: [
    {
      groupId: GROUP,
      order: 0,
      currencyCode: "IRR",
      areas: [
        { kind: "City", id: CITY },
        { kind: "Country", id: COUNTRY },
      ],
      rows: [
        {
          rateRowId: ROW,
          minWeightGrams: 0,
          maxWeightGrams: 5000,
          minMerchandiseAmount: "@0.50@",
          freightAmount: "@1850000.00@",
          packagingAmount: "@0.00@",
          minDeliveryDays: 1,
          maxDeliveryDays: 3,
        },
      ],
      titles: [{ languageId: LANGUAGE_FA, title: "تهران" }],
    },
  ],
}));

function parsed() {
  const node = parseJsonNode(WIRE);
  assert.ok(node, "fixture must parse");
  return node;
}

/** Confirmed baseline decoded from the same real wire fixture. */
function baselineDocument() {
  const decoded = decodeEditorDocument(parsed(), DRAFT);
  if (!decoded.ok) throw new Error('decode_failed');
  return decoded.value;
}

function resolver(): AreaAncestryResolver {
  return {
    resolve: (area) =>
      area.kind === "Country"
        ? { kind: "Country", countryId: COUNTRY }
        : { kind: "City", countryId: COUNTRY, administrativeAreaId: ADMIN_AREA, cityId: CITY },
  };
}

function edits(): EditorEdits {
  return {
    regionGroups: [
      {
        groupKey: "g-local-1",
        groupId: GROUP,
        areas: [
          { kind: "Country", id: COUNTRY },
          { kind: "City", id: CITY },
        ],
        rows: [
          {
            rowKey: "r-local-1",
            rateRowId: ROW,
            minWeightGrams: 0,
            maxWeightGrams: 5000,
            minItemCount: null,
            maxItemCount: null,
            minMerchandiseAmount: "0.50",
            maxMerchandiseAmount: null,
            freightAmount: "1850000.00",
            packagingAmount: "0",
            minDeliveryDays: 1,
            maxDeliveryDays: 3,
          },
        ],
        titles: [{ languageId: LANGUAGE_FA, title: "تهران" }],
      },
    ],
    methodTranslations: [{ languageId: LANGUAGE_FA, name: "پست پیشتاز", description: null }],
    typeAssignment: { shippingTypeId: null },
  };
}

test("decodes the real composite wire without inventing or normalising anything", () => {
  const decoded = decodeEditorDocument(parsed(), DRAFT);
  assert.equal(decoded.ok, true);
  if (!decoded.ok) return;
  const doc = decoded.value;
  assert.equal(doc.revision, 7);
  assert.equal(doc.updatedAt, "2026-10-23T01:45:10.0000000+03:30");
  assert.equal(doc.settings?.freightPaymentMode, "CollectOnDelivery");
  assert.equal(doc.optionCode, null);
  assert.deepEqual(doc.owner, { kind: "Marketplace", sellerPartyId: null });
  assert.equal(doc.type.resolution, "Active");
  assert.equal(doc.type.shippingType?.code, "express");
  // Three active languages with a NON-first default and mixed direction, order preserved.
  assert.deepEqual(
    doc.presentation.contentLanguages.map((language) => [language.languageId, language.direction, language.isDefault]),
    [
      [LANGUAGE_EN, "ltr", true],
      [LANGUAGE_FA, "rtl", false],
      [LANGUAGE_AR, "rtl", false],
    ],
  );
  assert.equal(doc.presentation.defaultContentLanguageId, LANGUAGE_EN);
  assert.ok(doc.presentation.hasDefaultLanguageName);
  // Raw text is not trimmed and inactive stored translations are retained.
  assert.equal(doc.presentation.groupTitles[0]?.title, "  تهران و حومه  ");
  assert.ok(doc.presentation.methodTranslations.some((item) => item.languageId === LANGUAGE_STORED_INACTIVE));
  assert.ok(doc.type.shippingType?.translations.some((item) => item.languageId === LANGUAGE_STORED_INACTIVE));
  // Decimal scale and time text survive.
  const row = doc.regionGroups[0]?.rows[0];
  assert.equal(row?.minMerchandiseAmount, "0.50");
  assert.equal(row?.freightAmount, "1850000.00");
  assert.equal(row?.maxWeightGrams, 5000);
  assert.equal(row?.minItemCount, null);
  assert.equal(doc.weeklySchedule?.days[0]?.endTime, "18:00:00.5000000");
  assert.equal(doc.weeklySchedule?.days.length, 7);
});

test("untrusted payloads fail closed instead of yielding a partial document", () => {
  const broken = [
    WIRE.replace('"kind":"City"', '"kind":"Neighbourhood"'),
    WIRE.replace('"rateRowId":"' + ROW + '",', ""),
    WIRE.replace('"regionGroups":', '"regionGroupsUnused":'),
    WIRE.replace('"defaultContentLanguageId":"' + LANGUAGE_EN + '"', '"defaultContentLanguageId":"' + LANGUAGE_STORED_INACTIVE + '"'),
    WIRE.replace('"freightAmount":1850000.00', '"freightAmount":1850000.00, "freightAmount":1'),
    WIRE.replace('"kind":"Marketplace"', '"kind":"Tenant"'),
  ];
  for (const text of broken) {
    const node = parseJsonNode(text);
    // Duplicate keys are rejected by the wire reader itself; anything else must fail decoding.
    const rejected = node === null || !decodeEditorDocument(node, DRAFT).ok;
    assert.equal(rejected, true, `must reject: ${text.slice(0, 60)}`);
  }
  // Another draft's payload is never applied silently.
  assert.equal(decodeEditorDocument(parsed(), "00000000-0000-4000-8000-000000000000").ok, false);
});

test("the single PUT carries the exact revision, decimal text and explicit clear", async () => {
  const calls: { url: string; init: RequestInit }[] = [];
  const original = globalThis.fetch;
  globalThis.fetch = (async (url: string, init: RequestInit) => {
    calls.push({ url: String(url), init });
    return new Response(WIRE, { status: 200, headers: { "Content-Type": "application/json" } });
  }) as typeof fetch;
  try {
    const built = buildEditorDocumentBody(baselineDocument(), edits(), resolver());
    assert.equal(built.status, "ready");
    if (built.status !== "ready") return;
    assert.equal(
      built.text,
      '{"expectedRevision":7,"regionGroups":[{"groupId":"' +
        GROUP +
        '","areas":[{"kind":"Country","countryId":"' +
        COUNTRY +
        '","administrativeAreaId":null,"cityId":null,"serviceAreaId":null},{"kind":"City","countryId":"' +
        COUNTRY +
        '","administrativeAreaId":"' +
        ADMIN_AREA +
        '","cityId":"' +
        CITY +
        '","serviceAreaId":null}],"rows":[{"rateRowId":"' +
        ROW +
        '","minWeightGrams":0,"maxWeightGrams":5000,"minItemCount":null,"maxItemCount":null,"minMerchandiseAmount":0.50,"maxMerchandiseAmount":null,"freightAmount":1850000.00,"packagingAmount":0,"minDeliveryDays":1,"maxDeliveryDays":3}],"titles":[{"languageId":"' +
        LANGUAGE_FA +
        '","title":"تهران"}]}],"methodTranslations":[{"languageId":"' +
        LANGUAGE_FA +
        '","name":"پست پیشتاز","description":null}],"typeAssignment":{"shippingTypeId":null}}',
    );
    // No owner/tenant/seller/currency/edition authority and no client-local keys in the body.
    assert.doesNotMatch(built.text, /owner|tenant|seller|currency|edition|groupKey|rowKey/i);

    const outcome = await saveEditorDocument(DRAFT, built.text);
    assert.equal(outcome.kind, "ok");
    assert.equal(calls.length, 1, "exactly one atomic PUT");
    assert.equal(calls[0]?.init.method, "PUT");
    assert.equal(calls[0]?.url, `/v1/admin/shipping-services/authoring-drafts/${DRAFT}/editor-document`);
    assert.equal(calls[0]?.url, editorDocumentPath(DRAFT));
    const headers = calls[0]?.init.headers as Record<string, string>;
    assert.equal(headers["Content-Type"], "application/json");
    assert.ok("Accept-Language" in headers);
    assert.ok(!Object.keys(headers).some((name) => /tenant|seller|owner|currency|edition/i.test(name)));
  } finally {
    globalThis.fetch = original;
  }
});

test("absent optional sections stay PRESERVE and [] stays no text change", () => {
  const built = buildEditorDocumentBody(
    { ...baselineDocument(), revision: 3 },
    { regionGroups: [], methodTranslations: [] },
    resolver(),
  );
  assert.equal(built.status, "ready");
  if (built.status !== "ready") return;
  assert.equal(built.text, '{"expectedRevision":3,"regionGroups":[],"methodTranslations":[]}');
});

test("a read selection without authoritative ancestry blocks request construction", () => {
  const built = buildEditorDocumentBody(baselineDocument(), edits(), { resolve: () => null });
  assert.equal(built.status, "needsResolution");
  if (built.status !== "needsResolution") return;
  assert.equal(built.pending.length, 2);
  assert.deepEqual(built.pending[0]?.area, { kind: "Country", id: COUNTRY });
  assert.equal(built.pending[0]?.groupId, GROUP);
});

test("statuses map to explicit failures and a PUT is never auto-retried as unknown", async () => {
  const original = globalThis.fetch;
  const cases: { status: number; body: string; kind: string }[] = [
    { status: 401, body: "{}", kind: "denied" },
    { status: 403, body: '{"title":"forbidden"}', kind: "denied" },
    { status: 404, body: "{}", kind: "notFound" },
    { status: 409, body: '{"errorCode":"shipping.authoring.revision_conflict","errors":{"expectedRevision":["stale"]}}', kind: "conflict" },
    { status: 422, body: '{"errorCode":"shipping.authoring.region_payload_invalid","errors":{"regionGroups[0].rows[0].freightAmount":["invalid"]}}', kind: "validation" },
    { status: 400, body: "{}", kind: "validation" },
    { status: 503, body: "{}", kind: "unavailable" },
    { status: 500, body: "{}", kind: "httpError" },
  ];
  try {
    for (const item of cases) {
      globalThis.fetch = (async () => new Response(item.body, { status: item.status })) as typeof fetch;
      const put = await saveEditorDocument(DRAFT, "{}");
      assert.equal(put.kind, "failure");
      if (put.kind !== "failure") continue;
      assert.equal(put.failure.kind, item.kind, `status ${item.status}`);
      const get = await loadEditorDocument(DRAFT);
      assert.equal(get.kind, "failure");
      if (get.kind === "failure") assert.equal(get.failure.kind, item.kind);
    }

    const conflict = await (async () => {
      globalThis.fetch = (async () =>
        new Response(
          '{"errorCode":"shipping.authoring.revision_conflict","errors":{"expectedRevision":["stale"],"regionGroups[0].rows[1].freightAmount":["invalid"]}}',
          { status: 409 },
        )) as typeof fetch;
      return saveEditorDocument(DRAFT, "{}");
    })();
    assert.equal(conflict.kind, "failure");
    if (conflict.kind === "failure" && conflict.failure.kind === "conflict") {
      assert.equal(conflict.failure.errorCode, "shipping.authoring.revision_conflict");
      assert.deepEqual(
        conflict.failure.errors.map((entry) => entry.property),
        ["expectedRevision", "regionGroups[0].rows[1].freightAmount"],
      );
    }

    // Network failure on a PUT cannot establish no-commit: outcomeUnknown, no second attempt.
    let attempts = 0;
    globalThis.fetch = (async () => {
      attempts += 1;
      throw new TypeError("network down");
    }) as typeof fetch;
    const putUnknown = await saveEditorDocument(DRAFT, "{}");
    assert.equal(putUnknown.kind, "failure");
    if (putUnknown.kind === "failure") assert.equal(putUnknown.failure.kind, "outcomeUnknown");
    assert.equal(attempts, 1, "no automatic replay with a new revision");

    // A cancelled GET is not a fabricated not-found.
    const controller = new AbortController();
    controller.abort();
    globalThis.fetch = (async (_url: string, init: RequestInit) => {
      if (init.signal?.aborted) throw new DOMException("aborted", "AbortError");
      return new Response("{}", { status: 200 });
    }) as typeof fetch;
    const getAbort = await loadEditorDocument(DRAFT, controller.signal);
    assert.equal(getAbort.kind, "failure");
    if (getAbort.kind === "failure") {
      assert.equal(getAbort.failure.kind, "network");
      if (getAbort.failure.kind === "network") assert.equal(getAbort.failure.aborted, true);
    }

    // Malformed 2xx is untrusted for a GET and unknowable for a PUT.
    globalThis.fetch = (async () => new Response("<html>", { status: 200 })) as typeof fetch;
    const getBad = await loadEditorDocument(DRAFT);
    assert.equal(getBad.kind, "failure");
    if (getBad.kind === "failure") assert.equal(getBad.failure.kind, "untrusted");
    const putBad = await saveEditorDocument(DRAFT, "{}");
    assert.equal(putBad.kind, "failure");
    if (putBad.kind === "failure") assert.equal(putBad.failure.kind, "outcomeUnknown");
  } finally {
    globalThis.fetch = original;
  }
});

test("the feature boundary stays public-only and never imports a page-owned client", () => {
  const root = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
  assert.ok(fs.existsSync(path.join(root, "index.ts")));
  const files = ["api/editor-document-api.ts", "api/editor-document-request.ts", "api/editor-document-decode.ts", "api/wire-json.ts", "session/editor-session.ts", "model/editor-document.ts", "model/editor-edits.ts"];
  for (const rel of files) {
    const text = fs.readFileSync(path.join(root, rel), "utf8");
    assert.doesNotMatch(text, /app\/admin\//, `${rel} imports page-owned admin code`);
    assert.doesNotMatch(text, /localStorage\.setItem|window\.confirm|react|next\//, `${rel} must stay renderer/store free`);
  }
  const api = fs.readFileSync(path.join(root, "api/editor-document-api.ts"), "utf8");
  assert.match(api, /lib\/admin\/admin-result/);
  assert.match(api, /\/v1\/admin\/shipping-services\/authoring-drafts/);
  assert.doesNotMatch(api, /method:\s*"PATCH"/);
  assert.doesNotMatch(api, /Promise\.all/);
});
