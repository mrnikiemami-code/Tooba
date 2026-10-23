import assert from "node:assert/strict";
import test from "node:test";
import {
  buildEditorEdits,
  createEditorForm,
  withCalculationMode,
  withDescription,
  withFreightPaymentMode,
  withName,
  withShippingType,
} from "./model/editor-form.ts";
import { buildEditorDocumentBody } from "./api/editor-document-request.ts";
import { decodeEditorDocument } from "./api/editor-document-decode.ts";
import { parseJsonNode } from "./api/wire-json.ts";
import { draftsMatchingBinding, resolveDraftEntry } from "./api/authoring-lookup-api.ts";
import { indexDestinationFacts, unresolvedSelections } from "./model/editor-destination-ancestry.ts";
import { createEditorController } from "./session/editor-controller.ts";
import type { EditorTransport } from "./session/editor-controller.ts";
import type { EditorOutcome, EditorTransportFailure } from "./api/editor-document-api.ts";
import type { EditorDocument } from "./model/editor-document.ts";

const DRAFT = "11111111-1111-4111-8111-111111111111";
const GROUP_1 = "22222222-2222-4222-8222-222222222221";
const GROUP_2 = "22222222-2222-4222-8222-222222222222";
const ROW_1 = "33333333-3333-4333-8333-333333333331";
const FA = "44444444-4444-4444-8444-444444444441";
const EN = "55555555-5555-4555-8555-555555555551";
const INACTIVE = "66666666-6666-4666-8666-666666666661";
const TYPE_1 = "88888888-8888-4888-8888-888888888881";
const COUNTRY = "99999999-9999-4999-8999-999999999991";
const CITY = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";
const SERVICE_AREA = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";

function rawNumbers(text: string): string {
  return text.replace(/"@(-?[0-9]+\.[0-9]+)@"/g, "$1");
}

/** One real document wire: a city WITHOUT administrative parent and a service area under it. */
function wire(overrides: { readonly destinationFacts?: unknown | "omit"; readonly revision?: number } = {}): string {
  const revision = overrides.revision ?? 4;
  const facts =
    overrides.destinationFacts === "omit"
      ? {}
      : {
          destinationFacts:
            overrides.destinationFacts ??
            [
              {
                kind: "City",
                id: CITY,
                resolution: "Resolved",
                area: { kind: "City", countryId: COUNTRY, administrativeAreaId: null, cityId: CITY, serviceAreaId: null },
                label: "تهران",
              },
              {
                kind: "ServiceArea",
                id: SERVICE_AREA,
                resolution: "Resolved",
                area: {
                  kind: "ServiceArea",
                  countryId: COUNTRY,
                  administrativeAreaId: null,
                  cityId: CITY,
                  serviceAreaId: SERVICE_AREA,
                },
                label: "ناحیهٔ مرکزی",
              },
            ],
        };
  return rawNumbers(
    JSON.stringify({
      draftId: DRAFT,
      owner: { kind: "Store" },
      serviceCode: "tooba.post",
      optionCode: null,
      revision,
      createdAt: "2026-10-23T00:15:00+03:30",
      updatedAt: "2026-10-23T01:45:10+03:30",
      type: { draftId: DRAFT, revision, resolution: "Unconfigured" },
      presentation: {
        methodTranslations: [
          { languageId: FA, name: "پست", description: "سرویس پستی" },
          { languageId: INACTIVE, name: "inactive stored name" },
        ],
        groupTitles: [{ languageId: FA, title: "تهران" }],
        contentLanguages: [
          {
            languageId: FA,
            code: "fa",
            culture: "fa-IR",
            urlPrefix: "",
            direction: "rtl",
            displayName: "فارسی",
            nativeName: "فارسی",
            isDefault: true,
            sortOrder: 1,
          },
          {
            languageId: EN,
            code: "en",
            culture: "en-US",
            urlPrefix: "en",
            direction: "ltr",
            displayName: "English",
            nativeName: "English",
            isDefault: false,
            sortOrder: 2,
          },
        ],
        defaultContentLanguageId: FA,
        hasDefaultLanguageName: true,
      },
      weeklySchedule: {
        timeZoneId: "Asia/Tehran",
        days: [1, 2, 3, 4, 5, 6, 7].map((weekday) => ({
          weekday,
          isWorking: weekday <= 5,
          startTime: "09:30:00",
          endTime: "18:00:00",
        })),
      },
      regionGroups: [
        {
          groupId: GROUP_1,
          order: 0,
          areas: [{ kind: "City", id: CITY }],
          rows: [
            {
              rateRowId: ROW_1,
              minWeightGrams: 0,
              maxWeightGrams: 5000,
              freightAmount: "@1850000.00@",
              packagingAmount: "@0.00@",
              minDeliveryDays: 1,
              maxDeliveryDays: 3,
            },
          ],
          titles: [{ languageId: FA, title: "تهران" }],
        },
        {
          groupId: GROUP_2,
          order: 1,
          currencyCode: "IRR",
          areas: [{ kind: "ServiceArea", id: SERVICE_AREA }],
          rows: [],
          titles: [],
        },
      ],
      ...facts,
    }),
  );
}

function decode(text = wire()): EditorDocument {
  const node = parseJsonNode(text);
  assert.ok(node);
  const decoded = decodeEditorDocument(node, DRAFT);
  assert.ok(decoded.ok, decoded.ok ? undefined : decoded.reason);
  return decoded.value;
}

function bodyOf(document: EditorDocument, form: ReturnType<typeof createEditorForm>): Record<string, unknown> {
  const built = buildEditorEdits(document, form);
  assert.deepEqual(built.issues, []);
  const index = indexDestinationFacts(document);
  const body = buildEditorDocumentBody(document, built.edits, index.resolver);
  assert.equal(body.status, "ready", body.status === "ready" ? undefined : JSON.stringify(body));
  if (body.status !== "ready") throw new Error("unreachable");
  const parsed = JSON.parse(body.text) as Record<string, unknown>;
  return parsed;
}

function deferred<T>(): { readonly promise: Promise<T>; readonly resolve: (value: T) => void } {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}

test("201 facts decode into canonical ancestry even when a city/service area has NO admin parent", () => {
  const document = decode();
  const index = indexDestinationFacts(document);
  assert.equal(index.available, true);
  assert.deepEqual(unresolvedSelections(document, index), []);
  const city = index.resolver.resolve({ kind: "City", id: CITY });
  assert.deepEqual(city, {
    kind: "City",
    countryId: COUNTRY,
    administrativeAreaId: null,
    cityId: CITY,
    serviceAreaId: null,
  });
  const area = index.resolver.resolve({ kind: "ServiceArea", id: SERVICE_AREA });
  assert.deepEqual(area, {
    kind: "ServiceArea",
    countryId: COUNTRY,
    administrativeAreaId: null,
    cityId: CITY,
    serviceAreaId: SERVICE_AREA,
  });
  assert.equal(index.labelOf({ kind: "City", id: CITY }), "تهران");
});

test("null-admin city AND null-admin service area survive the atomic PUT roundtrip", () => {
  const document = decode();
  const form = withCalculationMode(withFreightPaymentMode(createEditorForm(document), "Prepaid"), "Manual");
  const body = bodyOf(document, form);
  const groups = body.regionGroups as { areas: { kind: string; administrativeAreaId: unknown }[] }[];
  assert.deepEqual(groups[0]!.areas, [
    { kind: "City", countryId: COUNTRY, administrativeAreaId: null, cityId: CITY, serviceAreaId: null },
  ]);
  assert.deepEqual(groups[1]!.areas, [
    {
      kind: "ServiceArea",
      countryId: COUNTRY,
      administrativeAreaId: null,
      cityId: CITY,
      serviceAreaId: SERVICE_AREA,
    },
  ]);
  // Retained rows/currency/titles are preserved exactly; a general edit never rewrites geography.
  assert.equal((groups[0] as unknown as { rows: { rateRowId: string }[] }).rows[0]!.rateRowId, ROW_1);
  assert.equal(body.expectedRevision, 4);
  assert.equal("methodSettings" in body, true);
});

test("destination facts fail closed when absent, duplicated, contradictory, unselected or mis-terminated", () => {
  const missingFaсt = JSON.parse(wire()) as Record<string, unknown>;
  missingFaсt.destinationFacts = [
    {
      kind: "City",
      id: CITY,
      resolution: "Resolved",
      area: { kind: "City", countryId: COUNTRY, administrativeAreaId: null, cityId: CITY, serviceAreaId: null },
      label: null,
    },
  ];
  const cases: readonly [string, unknown][] = [
    ["missing-selected", missingFaсt.destinationFacts],
    [
      "duplicate",
      [
        ...(JSON.parse(wire()).destinationFacts as unknown[]),
        {
          kind: "City",
          id: CITY,
          resolution: "Unresolved",
          area: null,
          label: null,
        },
      ],
    ],
    [
      "contradictory",
      [
        {
          kind: "City",
          id: CITY,
          resolution: "Resolved",
          area: null,
          label: null,
        },
      ],
    ],
    [
      "wrong-terminal",
      [
        {
          kind: "City",
          id: CITY,
          resolution: "Resolved",
          area: {
            kind: "City",
            countryId: COUNTRY,
            administrativeAreaId: null,
            cityId: SERVICE_AREA,
            serviceAreaId: null,
          },
          label: null,
        },
      ],
    ],
  ];
  for (const [reason, facts] of cases) {
    const payload = JSON.parse(wire()) as Record<string, unknown>;
    payload.destinationFacts = facts;
    const node = parseJsonNode(JSON.stringify(payload));
    assert.ok(node);
    const decoded = decodeEditorDocument(node, DRAFT);
    assert.equal(decoded.ok, false, reason);
  }

  const absent = decode(wire({ destinationFacts: "omit" }));
  assert.equal(absent.destinationFacts, null);
  const index = indexDestinationFacts(absent);
  assert.equal(index.available, false);
  assert.equal(index.resolver.resolve({ kind: "City", id: CITY }), null);
});

test("general form emits only intentionally edited active languages and requires both settings", () => {
  const document = decode();
  const base = createEditorForm(document);
  // Nothing touched: no intent at all.
  assert.deepEqual(buildEditorEdits(document, base).edits, {});
  // One English name typed: only that active language, never the inactive stored raw text.
  const named = withName(base, EN, "Post");
  const built = buildEditorEdits(document, named);
  assert.deepEqual(built.edits.methodTranslations, [{ languageId: EN, name: "Post", description: null }]);
  // Explicit description clear stays an explicit null (a real stored description is required first).
  const described = withDescription(base, FA, "توضیح موقت");
  assert.deepEqual(buildEditorEdits(document, described).edits.methodTranslations, [
    { languageId: FA, name: "پست", description: "توضیح موقت" },
  ]);
  const cleared = withDescription(described, FA, "");
  assert.deepEqual(buildEditorEdits(document, cleared).edits.methodTranslations, [
    { languageId: FA, name: "پست", description: null },
  ]);
  // A single independent setting cannot be written; both must be explicit.
  const half = withFreightPaymentMode(base, "Prepaid");
  assert.deepEqual(buildEditorEdits(document, half).issues, [{ code: "settingsIncomplete" }]);
  // Explicit type clear is a real intent; untouched wrapper is omitted.
  const clearedType = withShippingType(base, null);
  assert.deepEqual(buildEditorEdits(document, clearedType).edits.typeAssignment, { shippingTypeId: null });
});

test("controller: load → edit → ONE atomic PUT → adopt server response without a post-save GET", async () => {
  const sent: string[] = [];
  const loads: string[] = [];
  const server = JSON.parse(wire({ revision: 5 })) as Record<string, unknown>;
  const transport: EditorTransport = {
    load: async (): Promise<EditorOutcome> => {
      loads.push("GET");
      return { kind: "ok", document: decode() };
    },
    save: async (_draftId, bodyText): Promise<EditorOutcome> => {
      sent.push(bodyText);
      return { kind: "ok", document: decode(JSON.stringify(server)) };
    },
  };
  const controller = createEditorController(DRAFT, transport);
  controller.begin();
  await new Promise((resolve) => setImmediate(resolve));
  const document = controller.getState().session.baseline!;
  const form = withName(withCalculationMode(withFreightPaymentMode(createEditorForm(document), "CollectOnDelivery"), "Free"), FA, "پست ویژه");
  controller.edit(buildEditorEdits(document, form).edits);
  const outcome = await controller.apply(indexDestinationFacts(document).resolver);
  assert.equal(outcome.status, "sent");
  assert.equal(sent.length, 1);
  assert.equal(loads.length, 1, "no unnecessary post-save GET");
  const state = controller.getState().session;
  assert.equal(state.baseline!.revision, 5);
  assert.equal(state.unsaved, false);
  assert.equal(state.save, "clean");
  const body = JSON.parse(sent[0]!) as Record<string, unknown>;
  assert.equal((body.methodTranslations as unknown[]).length, 1);
  assert.equal(body.expectedRevision, 4);
  controller.dispose();
});

test("controller: a double Apply while in flight sends exactly one PUT", async () => {
  const gate = deferred<EditorOutcome>();
  let saves = 0;
  const transport: EditorTransport = {
    load: async () => ({ kind: "ok", document: decode() }),
    save: async () => {
      saves += 1;
      return gate.promise;
    },
  };
  const controller = createEditorController(DRAFT, transport);
  controller.begin();
  await new Promise((resolve) => setImmediate(resolve));
  const document = controller.getState().session.baseline!;
  const form = withShippingType(withCalculationMode(withFreightPaymentMode(createEditorForm(document), "Prepaid"), "Manual"), TYPE_1);
  controller.edit(buildEditorEdits(document, form).edits);
  const resolver = indexDestinationFacts(document).resolver;
  const first = controller.apply(resolver);
  const second = await controller.apply(resolver);
  assert.equal(second.status, "blocked");
  assert.equal(saves, 1);
  gate.resolve({ kind: "ok", document: decode(JSON.stringify(JSON.parse(wire({ revision: 6 })))) });
  assert.equal((await first).status, "sent");
  controller.dispose();
});

test("controller: 403 and 409 keep the edits, never retry automatically", async () => {
  for (const failure of [
    { kind: "denied" as const, status: 403, errorCode: "admin.authorization.denied", title: null },
    { kind: "conflict" as const, status: 409, errorCode: "conflict", title: null, errors: [] },
  ] satisfies readonly EditorTransportFailure[]) {
    let saves = 0;
    const transport: EditorTransport = {
      load: async () => ({ kind: "ok", document: decode() }),
      save: async () => {
        saves += 1;
        return { kind: "failure", failure };
      },
    };
    const controller = createEditorController(DRAFT, transport);
    controller.begin();
    await new Promise((resolve) => setImmediate(resolve));
    const document = controller.getState().session.baseline!;
    const form = withName(createEditorForm(document), FA, "نام تازه");
    controller.edit(buildEditorEdits(document, form).edits);
    const outcome = await controller.apply(indexDestinationFacts(document).resolver);
    assert.equal(outcome.status, "failed");
    const state = controller.getState().session;
    assert.equal(state.unsaved, true, "edits are kept");
    assert.equal(saves, 1, "no automatic retry");
    assert.equal(state.commitUncertain, false);
    controller.dispose();
  }
});

test("controller: an unknown commit stays latched, blocks Apply and needs an explicit reconcile read", async () => {
  let saves = 0;
  let loads = 0;
  const transport: EditorTransport = {
    load: async () => {
      loads += 1;
      return { kind: "ok", document: decode(JSON.stringify(JSON.parse(wire({ revision: 7 })))) };
    },
    save: async () => {
      saves += 1;
      return { kind: "failure", failure: { kind: "outcomeUnknown", reason: "network" } };
    },
  };
  const controller = createEditorController(DRAFT, transport);
  controller.begin();
  await new Promise((resolve) => setImmediate(resolve));
  const document = controller.getState().session.baseline!;
  const resolver = indexDestinationFacts(document).resolver;
  controller.edit(buildEditorEdits(document, withName(createEditorForm(document), FA, "پست تازه")).edits);
  await controller.apply(resolver);
  assert.equal(controller.getState().session.commitUncertain, true);
  // Further edits never clear the latch and Apply is refused without a second PUT.
  controller.edit(buildEditorEdits(document, withName(createEditorForm(document), FA, "پست ۲")).edits);
  const blocked = await controller.apply(resolver);
  assert.equal(blocked.status, "blocked");
  assert.equal(saves, 1);
  await controller.reconcile();
  const after = controller.getState().session;
  assert.equal(after.commitUncertain, false);
  assert.equal(after.baseline!.revision, 7);
  assert.equal(loads, 2, "one explicit reconciliation read");
  controller.dispose();
});

test("entry: exact binding matches exactly one draft, zero and many are honest states", () => {
  const rows = [
    { draftId: DRAFT, serviceCode: "tooba.post", optionCode: null },
    { draftId: "12121212-1212-4212-8212-121212121212", serviceCode: "tooba.post", optionCode: "express" },
  ];
  assert.deepEqual(resolveDraftEntry(rows, "tooba.post", null), { state: "matched", draftId: DRAFT });
  assert.deepEqual(resolveDraftEntry(rows, "tooba.post", "express"), {
    state: "matched",
    draftId: "12121212-1212-4212-8212-121212121212",
  });
  assert.deepEqual(resolveDraftEntry(rows, "tooba.other", null), { state: "none" });
  const ambiguous = [
    ...rows,
    { draftId: "13131313-1313-4313-8313-131313131313", serviceCode: "tooba.post", optionCode: null },
  ];
  assert.deepEqual(resolveDraftEntry(ambiguous, "tooba.post", null), { state: "ambiguous", count: 2 });
  assert.equal(draftsMatchingBinding(rows, "tooba.post", "express").length, 1);
});
