/**
 * TB-246-R3 focused facts: hierarchical destination exclusivity before Save.
 */
import assert from "node:assert/strict";
import test from "node:test";
import { readFile } from "node:fs/promises";
import type { SelectedAreaRead } from "./model/editor-document.ts";
import type { AreaAncestryResolver, ResolvedAreaAncestry } from "./model/editor-edits.ts";
import {
  classifyDestinationOverlap,
  findAllDestinationOverlaps,
  findDestinationOverlap,
  isStrictGeographicAncestor,
} from "./model/destination-overlap.ts";
import { authoringMessages } from "./messages/authoring-messages.ts";

const COUNTRY = "e3451206-e503-51a8-91ad-afa9ab70e330";
const PROV_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1";
const PROV_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2";
const CITY_A1 = "cccccccc-cccc-cccc-cccc-ccccccccccc1";
const CITY_A2 = "dddddddd-dddd-dddd-dddd-ddddddddddd2";
const SA_A1 = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1";

function area(kind: SelectedAreaRead["kind"], id: string): SelectedAreaRead {
  return { kind, id };
}

function ancestry(
  kind: ResolvedAreaAncestry["kind"],
  parts: {
    countryId?: string;
    administrativeAreaId?: string | null;
    cityId?: string | null;
    serviceAreaId?: string | null;
  },
): ResolvedAreaAncestry {
  return {
    kind,
    countryId: parts.countryId ?? COUNTRY,
    administrativeAreaId: parts.administrativeAreaId,
    cityId: parts.cityId,
    serviceAreaId: parts.serviceAreaId,
  };
}

function resolverOf(map: Record<string, ResolvedAreaAncestry>): AreaAncestryResolver {
  return {
    resolve: (selected) => map[`${selected.kind}:${selected.id.toLowerCase()}`] ?? null,
  };
}

const countryAnc = ancestry("Country", {});
const provA = ancestry("AdministrativeArea", { administrativeAreaId: PROV_A });
const provB = ancestry("AdministrativeArea", { administrativeAreaId: PROV_B });
const cityA1 = ancestry("City", { administrativeAreaId: PROV_A, cityId: CITY_A1 });
const cityA2 = ancestry("City", { administrativeAreaId: PROV_A, cityId: CITY_A2 });
const saA1 = ancestry("ServiceArea", {
  administrativeAreaId: PROV_A,
  cityId: CITY_A1,
  serviceAreaId: SA_A1,
});

const catalog: Record<string, ResolvedAreaAncestry> = {
  [`Country:${COUNTRY}`]: countryAnc,
  [`AdministrativeArea:${PROV_A}`]: provA,
  [`AdministrativeArea:${PROV_B}`]: provB,
  [`City:${CITY_A1}`]: cityA1,
  [`City:${CITY_A2}`]: cityA2,
  [`ServiceArea:${SA_A1}`]: saA1,
};

test("exact duplicate across two groups is blocked", () => {
  const resolve = resolverOf(catalog);
  const conflict = findDestinationOverlap(
    area("Country", COUNTRY),
    countryAnc,
    [{ groupKey: "g1", groupIndex: 0, area: area("Country", COUNTRY) }],
    resolve,
  );
  assert.equal(conflict?.kind, "exact");
  assert.equal(conflict?.existingGroupKey, "g1");
});

test("country + child province is blocked in both insertion orders", () => {
  assert.equal(isStrictGeographicAncestor(countryAnc, provA), true);
  assert.equal(
    classifyDestinationOverlap(area("AdministrativeArea", PROV_A), provA, area("Country", COUNTRY), countryAnc),
    "proposed_descendant",
  );
  assert.equal(
    classifyDestinationOverlap(area("Country", COUNTRY), countryAnc, area("AdministrativeArea", PROV_A), provA),
    "proposed_ancestor",
  );
});

test("country + descendant city/service area is blocked", () => {
  assert.equal(classifyDestinationOverlap(area("City", CITY_A1), cityA1, area("Country", COUNTRY), countryAnc), "proposed_descendant");
  assert.equal(
    classifyDestinationOverlap(area("ServiceArea", SA_A1), saA1, area("Country", COUNTRY), countryAnc),
    "proposed_descendant",
  );
});

test("province + child city/service area is blocked", () => {
  assert.equal(
    classifyDestinationOverlap(area("City", CITY_A1), cityA1, area("AdministrativeArea", PROV_A), provA),
    "proposed_descendant",
  );
  assert.equal(
    classifyDestinationOverlap(area("ServiceArea", SA_A1), saA1, area("AdministrativeArea", PROV_A), provA),
    "proposed_descendant",
  );
});

test("city + child service area is blocked", () => {
  assert.equal(
    classifyDestinationOverlap(area("ServiceArea", SA_A1), saA1, area("City", CITY_A1), cityA1),
    "proposed_descendant",
  );
});

test("sibling provinces and sibling cities are allowed; non-overlapping branches allowed", () => {
  assert.equal(
    classifyDestinationOverlap(area("AdministrativeArea", PROV_B), provB, area("AdministrativeArea", PROV_A), provA),
    null,
  );
  assert.equal(classifyDestinationOverlap(area("City", CITY_A2), cityA2, area("City", CITY_A1), cityA1), null);
  assert.equal(
    classifyDestinationOverlap(area("City", CITY_A2), cityA2, area("AdministrativeArea", PROV_B), provB),
    null,
  );
});

test("conflict result carries candidate/existing identities and group ownership", () => {
  const conflict = findDestinationOverlap(
    area("City", CITY_A1),
    cityA1,
    [{ groupKey: "north", groupIndex: 2, area: area("Country", COUNTRY) }],
    resolverOf(catalog),
  );
  assert.equal(conflict?.candidate.id, CITY_A1);
  assert.equal(conflict?.existing.id, COUNTRY);
  assert.equal(conflict?.existingGroupKey, "north");
  assert.equal(conflict?.existingGroupIndex, 2);
  assert.equal(conflict?.kind, "proposed_descendant");
});

test("missing ancestry fails closed without inventing a relationship", () => {
  assert.equal(
    classifyDestinationOverlap(area("City", CITY_A1), cityA1, area("Country", COUNTRY), null),
    "unproven",
  );
  assert.equal(classifyDestinationOverlap(area("City", CITY_A1), null, area("Country", COUNTRY), countryAnc), "unproven");
});

test("whole-tree validation finds overlaps and clears when either side removed", () => {
  const resolve = resolverOf(catalog);
  const withOverlap = findAllDestinationOverlaps(
    [
      { key: "g0", areas: [area("Country", COUNTRY)] },
      { key: "g1", areas: [area("City", CITY_A1)] },
    ],
    resolve,
  );
  assert.equal(withOverlap.length, 1);
  const clearedLeft = findAllDestinationOverlaps([{ key: "g1", areas: [area("City", CITY_A1)] }], resolve);
  assert.equal(clearedLeft.length, 0);
  const clearedRight = findAllDestinationOverlaps([{ key: "g0", areas: [area("Country", COUNTRY)] }], resolve);
  assert.equal(clearedRight.length, 0);
  const siblings = findAllDestinationOverlaps(
    [
      { key: "g0", areas: [area("AdministrativeArea", PROV_A)] },
      { key: "g1", areas: [area("AdministrativeArea", PROV_B)] },
    ],
    resolve,
  );
  assert.equal(siblings.length, 0);
});

test("modal refuses Add via findOverlap and keeps staged path wiring", async () => {
  const modal = await readFile(new URL("ui/authoring-destination-modal.tsx", import.meta.url), "utf8");
  assert.match(modal, /findOverlap/);
  assert.match(modal, /formatOverlap/);
  assert.match(modal, /setRefusal\(formatOverlap\(conflict\)\)/);
  assert.equal(modal.includes("isDuplicateSelection"), false);
  assert.match(modal, /onAdd\(terminal, ancestry/);
});

test("screen disables Save on overlaps and maps region_selection_overlap without vague fieldRejected", async () => {
  const screen = await readFile(new URL("ui/authoring-screen.tsx", import.meta.url), "utf8");
  const server = await readFile(new URL("ui/server-field-error.ts", import.meta.url), "utf8");
  const fa = authoringMessages("fa");
  assert.match(screen, /destinationOverlaps\.length === 0/);
  assert.match(screen, /shipping-authoring-overlap-summary/);
  assert.match(server, /regionSelectionOverlapServer/);
  assert.match(server, /shipping\.authoring\.region_selection_overlap/);
  assert.equal(fa.regionSelectionOverlapServer.includes("shipping.authoring"), false);
  assert.match(fa.destinationOverlapConflict, /هم‌پوشانی/);
  assert.equal(fa.fieldRejectedServer.includes("هم‌پوشانی"), false);
});
