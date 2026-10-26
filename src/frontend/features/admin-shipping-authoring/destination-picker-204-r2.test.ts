import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import type {
  GeographyOption,
  GeographyQuery,
  GeographyReadOutcome,
} from "./api/geography-lookup-api.ts";
import { decodeGeographyOptions } from "./api/geography-lookup-api.ts";
import {
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseLevel,
  chooseServiceArea,
  destinationAddEligibility,
  emptyChoice,
  type DestinationChoice,
  type DestinationRowEvidence,
} from "./model/destination-choice.ts";
import {
  createDestinationReadCoordinator,
  type DestinationReadContext,
  type DestinationReadState,
  type GeographyReader,
} from "./ui/destination-read-coordinator.ts";

const FA = "44444444-4444-4444-8444-444444444441";
const EN = "55555555-5555-4555-8555-555555555551";
const CW = "dddddddd-dddd-4ddd-8ddd-ddddddddddd1"; // country A
const CX = "dddddddd-dddd-4ddd-8ddd-ddddddddddd2"; // country B
const ADMIN_ON = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1";
const ADMIN_OFF = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee2";
const ADMIN_UNKNOWN = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee3";
const CITY_NULL = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";
const CITY_ON = "cccccccc-cccc-4ccc-8ccc-ccccccccccc2";
const CITY_OFF_PARENT = "cccccccc-cccc-4ccc-8ccc-ccccccccccc3";
const CITY_MISSING_PARENT = "cccccccc-cccc-4ccc-8ccc-ccccccccccc4";
const OTHER_CITY = "cccccccc-cccc-4ccc-8ccc-ccccccccccc5";
const SERVICE = "ffffffff-ffff-4fff-8fff-fffffffffff1";

function row(
  kind: GeographyOption["kind"],
  id: string,
  parentId: string | null,
  isActive = true,
): GeographyOption {
  return { kind, id, code: "c", label: "l", parentId, isActive };
}

const ok = (options: readonly GeographyOption[]): GeographyReadOutcome => ({ state: "ok", options });
const failed = (kind: "denied" | "untrusted" | "unavailable" | "network"): GeographyReadOutcome => ({
  state: "failure",
  failure: { kind },
});

interface DeferredCall {
  readonly query: GeographyQuery;
  readonly signal: AbortSignal;
  resolve(outcome: GeographyReadOutcome): void;
}

function setup(): {
  ctl: ReturnType<typeof createDestinationReadCoordinator>;
  ofKind(kind: GeographyQuery["kind"]): DeferredCall[];
  latest: () => DestinationReadState;
} {
  const calls: DeferredCall[] = [];
  const read: GeographyReader = (query, signal) =>
    new Promise<GeographyReadOutcome>((resolve) => {
      calls.push({ query, signal, resolve });
    });
  let latest: DestinationReadState | undefined;
  const ctl = createDestinationReadCoordinator(read, (state) => {
    latest = state;
  });
  return {
    ctl,
    ofKind: (kind) => calls.filter((call) => call.query.kind === kind),
    latest: () => {
      assert.ok(latest, "expected a state notification");
      return latest;
    },
  };
}

function context(overrides: Partial<DestinationReadContext> = {}): DestinationReadContext {
  return {
    enabled: true,
    languageId: FA,
    countryId: null,
    administrativeAreaId: null,
    cityId: null,
    ...overrides,
  };
}

const flush = () => new Promise<void>((resolve) => setImmediate(resolve));

test("204-R2 A: A -> B -> A waits for the FRESH areas read instead of cached evidence", async () => {
  const { ctl, ofKind, latest } = setup();
  ctl.update(context({ countryId: CW }));
  ofKind("AdministrativeArea")[0].resolve(ok([row("AdministrativeArea", ADMIN_ON, CW)]));
  await flush();
  assert.equal(ofKind("City").length, 1, "the first A branch discharged its own prerequisite");

  ctl.update(context({ countryId: CX }));
  ctl.update(context({ countryId: CW }));

  assert.equal(ofKind("AdministrativeArea").length, 3, "re-entry starts a fresh A areas read");
  assert.equal(ofKind("City").length, 1, "the cached A set must NOT discharge the fresh prerequisite");
  assert.equal(latest().loading.cities, true, "cities stay pending on the fresh prerequisite");
  assert.equal(latest().settled.areas, false, "the previous generation is no longer settled");
  assert.deepEqual(latest().lists.cities, []);

  ofKind("AdministrativeArea")[2].resolve(ok([row("AdministrativeArea", ADMIN_ON, CW)]));
  await flush();
  assert.equal(ofKind("City").length, 2, "cities start only after the fresh A areas succeed");
});

test("204-R2 A: a failed fresh A areas read produces no city read at all", async () => {
  const { ctl, ofKind, latest } = setup();
  ctl.update(context({ countryId: CW }));
  ofKind("AdministrativeArea")[0].resolve(ok([row("AdministrativeArea", ADMIN_ON, CW)]));
  await flush();
  ctl.update(context({ countryId: CX }));
  ctl.update(context({ countryId: CW }));

  ofKind("AdministrativeArea")[2].resolve(failed("unavailable"));
  await flush();

  assert.equal(ofKind("City").length, 1, "no country-wide city read after the fresh failure");
  assert.deepEqual(latest().lists.cities, []);
  assert.equal(latest().settled.areas, false);
  assert.equal(latest().settled.cities, false);
  assert.deepEqual(latest().failures.cities, { kind: "unavailable" });
});

test("204-R2 A: a late response of the FIRST A generation cannot satisfy the new one", async () => {
  const { ctl, ofKind, latest } = setup();
  ctl.update(context({ countryId: CW }));
  const firstA = ofKind("AdministrativeArea")[0];
  ctl.update(context({ countryId: CX }));
  ctl.update(context({ countryId: CW }));
  assert.equal(firstA.signal.aborted, true, "the obsolete A generation was aborted");

  firstA.resolve(ok([row("AdministrativeArea", ADMIN_ON, CW)]));
  await flush();
  assert.deepEqual(latest().lists.areas, [], "the late first-A response is dropped");
  assert.equal(latest().settled.areas, false);
  assert.equal(ofKind("City").length, 0, "no city read from stale evidence");

  ofKind("AdministrativeArea")[2].resolve(ok([row("AdministrativeArea", ADMIN_ON, CW)]));
  await flush();
  assert.equal(ofKind("City").length, 1);
});

test("204-R2 B: country-wide city needs its actual CURRENT ACTIVE parent row", () => {
  const country = row("Country", CW, null);
  const base = (areas: readonly GeographyOption[], cities: readonly GeographyOption[]) => ({
    lists: { countries: [country], areas, cities, serviceAreas: [] },
    settled: { countries: true, areas: true, cities: true, serviceAreas: false },
  });
  const staged = (cityRow: GeographyOption): DestinationChoice =>
    chooseLevel(chooseCity(chooseCountry(emptyChoice(), country), cityRow), "City");

  assert.equal(
    destinationAddEligibility(staged(row("City", CITY_OFF_PARENT, ADMIN_OFF)), base([row("AdministrativeArea", ADMIN_OFF, CW, false)], [row("City", CITY_OFF_PARENT, ADMIN_OFF)])),
    false,
    "an active city under an INACTIVE returned area is rejected",
  );
  assert.equal(
    destinationAddEligibility(staged(row("City", CITY_ON, ADMIN_ON)), base([row("AdministrativeArea", ADMIN_ON, CW)], [row("City", CITY_ON, ADMIN_ON)])),
    true,
    "an active parent accepts",
  );
  assert.equal(
    destinationAddEligibility(staged(row("City", CITY_MISSING_PARENT, ADMIN_UNKNOWN)), base([row("AdministrativeArea", ADMIN_ON, CW)], [row("City", CITY_MISSING_PARENT, ADMIN_UNKNOWN)])),
    false,
    "a parent absent from the current rows is rejected",
  );
  assert.equal(
    destinationAddEligibility(staged(row("City", CITY_NULL, null)), base([row("AdministrativeArea", ADMIN_ON, CW)], [row("City", CITY_NULL, null)])),
    true,
    "a null-admin city stays lawful on a verified country-wide read",
  );
  assert.equal(
    destinationAddEligibility(staged(row("City", CITY_NULL, null)), {
      lists: { countries: [country], areas: [], cities: [row("City", CITY_NULL, null)], serviceAreas: [] },
      settled: { countries: true, areas: false, cities: true, serviceAreas: false },
    }),
    false,
    "a null-admin city is NOT lawful without a settled country-wide areas read",
  );
  assert.equal(
    destinationAddEligibility(staged(row("City", CITY_ON, ADMIN_ON)), {
      lists: { countries: [country], areas: [row("AdministrativeArea", ADMIN_ON, CW)], cities: [row("City", CITY_ON, ADMIN_ON)], serviceAreas: [] },
      settled: { countries: true, areas: true, cities: false, serviceAreas: false },
    }),
    false,
    "pending cities stage cannot be added",
  );
});

test("204-R2 B: a service area inherits only the currently verified city", () => {
  const country = row("Country", CW, null);
  const city = row("City", CITY_NULL, null);
  const evidenceFor = (services: readonly GeographyOption[]): DestinationRowEvidence => ({
    lists: {
      countries: [country],
      areas: [row("AdministrativeArea", ADMIN_ON, CW)],
      cities: [city],
      serviceAreas: services,
    },
    settled: { countries: true, areas: true, cities: true, serviceAreas: true },
  });
  const staged = chooseLevel(
    chooseServiceArea(chooseCity(chooseCountry(emptyChoice(), country), city), row("ServiceArea", SERVICE, CITY_NULL)),
    "ServiceArea",
  );

  assert.equal(destinationAddEligibility(staged, evidenceFor([row("ServiceArea", SERVICE, CITY_NULL)])), true);
  assert.equal(
    destinationAddEligibility(staged, evidenceFor([row("ServiceArea", SERVICE, OTHER_CITY)])),
    false,
    "a service area of another city is rejected",
  );
  assert.equal(destinationAddEligibility(staged, evidenceFor([])), false, "an absent service row is rejected");
  assert.equal(
    destinationAddEligibility(staged, {
      ...evidenceFor([row("ServiceArea", SERVICE, CITY_NULL, false)]),
    }),
    false,
    "an inactive service row is rejected",
  );
});

test("204-R2 B/C: context refresh, unrelated failures and terminal scoping", () => {
  const country = row("Country", CW, null);
  const area = row("AdministrativeArea", ADMIN_ON, CW);
  const city = row("City", CITY_ON, ADMIN_ON);
  const countryTerminal = chooseLevel(chooseCountry(emptyChoice(), country), "Country");
  const areaTerminal = chooseLevel(
    chooseAdministrativeArea(chooseCountry(emptyChoice(), country), area),
    "AdministrativeArea",
  );
  const cityTerminal = chooseLevel(chooseCity(chooseAdministrativeArea(chooseCountry(emptyChoice(), country), area), city), "City");

  // After a language/context refresh the rows are empty and nothing old may be added.
  const refreshed: DestinationRowEvidence = {
    lists: { countries: [], areas: [], cities: [], serviceAreas: [] },
    settled: { countries: false, areas: false, cities: false, serviceAreas: false },
  };
  assert.equal(destinationAddEligibility(countryTerminal, refreshed), false);
  assert.equal(destinationAddEligibility(areaTerminal, refreshed), false);
  assert.equal(destinationAddEligibility(cityTerminal, refreshed), false);

  // A country can be added while unrelated child branches failed.
  const countryOnly: DestinationRowEvidence = {
    lists: { countries: [country], areas: [], cities: [], serviceAreas: [] },
    settled: { countries: true, areas: false, cities: false, serviceAreas: false },
  };
  assert.equal(destinationAddEligibility(countryTerminal, countryOnly), true);
  assert.equal(destinationAddEligibility(areaTerminal, countryOnly), false, "area terminal needs its own row");
  assert.equal(destinationAddEligibility(cityTerminal, countryOnly), false);

  // The terminal path only: an AdministrativeArea terminal ignores a deeper staged city.
  const areaWithDeeperCity: DestinationRowEvidence = {
    lists: { countries: [country], areas: [area], cities: [], serviceAreas: [] },
    settled: { countries: true, areas: true, cities: false, serviceAreas: false },
  };
  assert.equal(destinationAddEligibility(areaTerminal, areaWithDeeperCity), true);
  assert.equal(destinationAddEligibility(cityTerminal, areaWithDeeperCity), false);

  const ready: DestinationRowEvidence = {
    lists: { countries: [country], areas: [area], cities: [city], serviceAreas: [] },
    settled: { countries: true, areas: true, cities: true, serviceAreas: false },
  };
  assert.equal(destinationAddEligibility(cityTerminal, ready), true);
  assert.equal(
    destinationAddEligibility(cityTerminal, { ...ready, lists: { ...ready.lists, areas: [row("AdministrativeArea", ADMIN_ON, CW, false)] } }),
    false,
    "a filtered city whose stage area became inactive is rejected",
  );
  assert.equal(
    destinationAddEligibility(cityTerminal, { ...ready, lists: { ...ready.lists, countries: [row("Country", CW, null, false)] } }),
    false,
    "an inactive country is rejected",
  );
  assert.equal(
    destinationAddEligibility(cityTerminal, { ...ready, lists: { ...ready.lists, cities: [row("City", CITY_ON, null)] } }),
    false,
    "a null-parent city contradicts an explicit area filter",
  );
});

test("204-R2 B: strict decoder fails closed on missing country-wide evidence", () => {
  const filtered = { kind: "City" as const, languageId: FA, countryId: CW, administrativeAreaId: ADMIN_ON };
  assert.equal(
    decodeGeographyOptions(filtered, [{ id: CITY_ON, code: "c", label: "l", parentId: null, isActive: true }]).state,
    "failure",
    "a filtered city with a null parent is rejected",
  );
  assert.equal(
    decodeGeographyOptions(filtered, [{ id: CITY_ON, code: "c", label: "l", parentId: ADMIN_ON, isActive: true }]).state,
    "ok",
  );

  const countryWide = { kind: "City" as const, languageId: FA, countryId: CW };
  assert.equal(
    decodeGeographyOptions(countryWide, [{ id: CITY_NULL, code: "c", label: "l", parentId: null, isActive: true }]).state,
    "failure",
    "absent parent evidence is not evidence",
  );
  assert.equal(
    decodeGeographyOptions({ ...countryWide, knownAdministrativeAreaIds: new Set<string>() }, [
      { id: CITY_NULL, code: "c", label: "l", parentId: null, isActive: true },
    ]).state,
    "ok",
    "a verified EMPTY set still allows a null-admin city",
  );
  assert.equal(
    decodeGeographyOptions({ ...countryWide, knownAdministrativeAreaIds: new Set<string>() }, [
      { id: CITY_ON, code: "c", label: "l", parentId: ADMIN_ON, isActive: true },
    ]).state,
    "failure",
    "a non-null parent outside the known set is rejected",
  );
  assert.equal(
    decodeGeographyOptions({ ...countryWide, knownAdministrativeAreaIds: new Set([ADMIN_ON]) }, [
      { id: CITY_ON, code: "c", label: "l", parentId: ADMIN_ON, isActive: true },
    ]).state,
    "ok",
  );
});

test("204-R2: the picker actually wires the eligibility helper and current coordinator evidence", () => {
  const picker = readFileSync(new URL("./ui/authoring-destination-picker.tsx", import.meta.url), "utf8");
  assert.match(picker, /destinationAddAvailable\(choice, readState, disabled\)/, "availability uses the current rows and never the duplicate rule");
  assert.match(picker, /coordinator\.current\?\.getState\(\)/, "Add re-reads current coordinator evidence");
  assert.match(picker, /destinationAddEligibility\(choice, evidence\)/, "Add re-checks eligibility synchronously");
  assert.equal(picker.includes("epoch"), false, "no shared epoch remains");
  assert.equal(picker.includes("app/admin/shipping-services"), false, "no feature->app legacy import");
});
