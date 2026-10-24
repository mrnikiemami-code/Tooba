import assert from "node:assert/strict";
import test from "node:test";
import type {
  GeographyOption,
  GeographyQuery,
  GeographyReadOutcome,
} from "./api/geography-lookup-api.ts";
import { decodeGeographyOptions } from "./api/geography-lookup-api.ts";
import {
  choiceAncestry,
  choiceChainActive,
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseLevel,
  chooseServiceArea,
  clearAdministrativeArea,
  clearCity,
  clearServiceArea,
  emptyChoice,
  optionDisplayName,
} from "./model/destination-choice.ts";
import {
  createDestinationReadCoordinator,
  type DestinationReadContext,
  type DestinationReadState,
  type GeographyReader,
} from "./ui/destination-read-coordinator.ts";

const FA = "44444444-4444-4444-8444-444444444441";
const COUNTRY_A = "dddddddd-dddd-4ddd-8ddd-ddddddddddd1";
const COUNTRY_B = "dddddddd-dddd-4ddd-8ddd-ddddddddddd2";
const AREA_A1 = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1";
const AREA_A2 = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee2";
const AREA_B1 = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee9";
const CITY_C1 = "cccccccc-cccc-4ccc-8ccc-ccccccccccc1";
const CITY_C2 = "cccccccc-cccc-4ccc-8ccc-ccccccccccc2";
const SERVICE_S1 = "ffffffff-ffff-4fff-8fff-fffffffffff1";

function option(
  kind: GeographyOption["kind"],
  id: string,
  parentId: string | null,
  isActive = true,
  label = "label",
  code = "code",
): GeographyOption {
  return { kind, id, code, label, parentId, isActive };
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

/** REAL deferred reader: every read stays pending until the test resolves it (no timers, no React). */
function setup(): {
  ctl: ReturnType<typeof createDestinationReadCoordinator>;
  calls: DeferredCall[];
  latest: () => DestinationReadState;
  ofKind(kind: GeographyQuery["kind"]): DeferredCall[];
  lastOfKind(kind: GeographyQuery["kind"]): DeferredCall;
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
  const ofKind = (kind: GeographyQuery["kind"]) => calls.filter((call) => call.query.kind === kind);
  return {
    ctl,
    calls,
    ofKind,
    lastOfKind(kind) {
      const matched = ofKind(kind);
      assert.ok(matched.length > 0, `expected a ${kind} read`);
      return matched[matched.length - 1];
    },
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

/** Flushes the pending microtask chain of a resolved promise. */
const flush = () => new Promise<void>((resolve) => setImmediate(resolve));

test("204-R1: a sibling country read survives the areas/cities reads starting", async () => {
  const { ctl, calls, latest, lastOfKind } = setup();
  ctl.update(context());
  assert.equal(calls.length, 1, "only the country branch is opened");
  assert.equal(calls[0].query.kind, "Country");

  ctl.update(context({ countryId: COUNTRY_A }));
  assert.equal(lastOfKind("AdministrativeArea").query.countryId, COUNTRY_A, "areas start as a sibling");
  assert.equal(lastOfKind("Country").signal.aborted, false, "the country sibling was NOT aborted");

  lastOfKind("AdministrativeArea").resolve(ok([option("AdministrativeArea", AREA_A1, COUNTRY_A)]));
  lastOfKind("Country").resolve(ok([option("Country", COUNTRY_A, null)]));
  await flush();

  assert.deepEqual(
    latest().lists.countries.map((row) => row.id),
    [COUNTRY_A],
    "the country sibling was still applied (the old single-epoch code discarded it)",
  );
  assert.deepEqual(latest().lists.areas.map((row) => row.id), [AREA_A1]);
});

test("204-R1: country-wide cities wait for the verified area set of the same branch", async () => {
  const { ctl, ofKind, latest } = setup();
  ctl.update(context({ countryId: COUNTRY_A }));
  assert.equal(ofKind("City").length, 0, "no city read before the area prerequisite");
  assert.equal(latest().loading.cities, true, "cities are pending on the prerequisite");

  ofKind("AdministrativeArea")[0].resolve(
    ok([option("AdministrativeArea", AREA_A1, COUNTRY_A), option("AdministrativeArea", AREA_A2, COUNTRY_A)]),
  );
  await flush();

  const city = ofKind("City");
  assert.equal(city.length, 1, "the city read starts only after the areas succeeded");
  assert.equal(city[0].query.administrativeAreaId ?? null, null, "country-wide, not the highlighted area");
  assert.deepEqual(
    [...(city[0].query.knownAdministrativeAreaIds ?? new Set<string>())].sort(),
    [AREA_A1, AREA_A2],
    "the verified area set is the parent evidence",
  );
});

test("204-R1: an EMPTY area set is a known-empty prerequisite (fail closed), not no-constraint", async () => {
  const { ctl, ofKind } = setup();
  ctl.update(context({ countryId: COUNTRY_A }));
  ofKind("AdministrativeArea")[0].resolve(ok([]));
  await flush();

  const city = ofKind("City")[0].query;
  assert.ok(city.knownAdministrativeAreaIds, "an empty set is still passed, never undefined");
  assert.equal(city.knownAdministrativeAreaIds?.size, 0);

  // The real strict decoder rejects a city whose parent was never verified in that empty set.
  const decoded = decodeGeographyOptions(city, [
    { id: CITY_C1, code: "c", label: "city", parentId: AREA_A1, isActive: true },
  ]);
  assert.equal(decoded.state, "failure");
  if (decoded.state === "failure") assert.equal(decoded.failure.kind, "untrusted");
});

test("204-R1: a failed area prerequisite leaves cities untrusted and unread", async () => {
  const { ctl, ofKind, latest } = setup();
  ctl.update(context({ countryId: COUNTRY_A }));
  ofKind("AdministrativeArea")[0].resolve(failed("unavailable"));
  await flush();

  assert.equal(ofKind("City").length, 0, "no country-wide city read after a failed prerequisite");
  assert.deepEqual(latest().lists.cities, []);
  assert.equal(latest().loading.cities, false);
  assert.deepEqual(latest().failures.cities, { kind: "unavailable" });
});

test("204-R1: an out-of-order response of a previous country cannot mutate the current lists", async () => {
  const { ctl, latest, ofKind } = setup();
  ctl.update(context({ countryId: COUNTRY_A }));
  ctl.update(context({ countryId: COUNTRY_B }));
  const areas = ofKind("AdministrativeArea");
  assert.equal(areas.length, 2, "the second country opens its own area read");
  assert.equal(areas[0].signal.aborted, true, "the obsolete dependent request is aborted");
  assert.deepEqual(latest().lists.areas, [], "old rows are cleared while awaiting the new parent");

  areas[0].resolve(ok([option("AdministrativeArea", AREA_A1, COUNTRY_A)]));
  await flush();
  assert.deepEqual(latest().lists.areas, [], "the late country-A response was dropped");

  areas[1].resolve(ok([option("AdministrativeArea", AREA_B1, COUNTRY_B)]));
  await flush();
  assert.deepEqual(latest().lists.areas.map((row) => row.id), [AREA_B1]);
});

test("204-R1: clearing the area clears dependents at once, allows country-wide cities, and drops a late filtered response", async () => {
  const { ctl, latest, ofKind } = setup();
  ctl.update(context({ countryId: COUNTRY_A }));
  ofKind("AdministrativeArea")[0].resolve(ok([option("AdministrativeArea", AREA_A1, COUNTRY_A)]));
  await flush();

  ctl.update(context({ countryId: COUNTRY_A, administrativeAreaId: AREA_A1 }));
  const filtered = ofKind("City")[1];
  assert.equal(filtered.query.administrativeAreaId, AREA_A1);
  assert.equal(filtered.query.knownAdministrativeAreaIds, undefined, "a filtered read has an exact filter");
  assert.deepEqual(latest().lists.areas.map((row) => row.id), [AREA_A1], "the area rows stay while filtering");

  ctl.update(context({ countryId: COUNTRY_A }));
  assert.equal(filtered.signal.aborted, true);
  assert.deepEqual(latest().lists.cities, [], "clearing the area clears the city rows immediately");
  assert.deepEqual(
    latest().lists.areas.map((row) => row.id),
    [AREA_A1],
    "the verified area rows survive (only dependents were cleared)",
  );
  const countryWide = ofKind("City")[2];
  assert.equal(countryWide.query.administrativeAreaId ?? null, null);

  filtered.resolve(ok([option("City", CITY_C1, AREA_A1)]));
  await flush();
  assert.deepEqual(latest().lists.cities, [], "the late response of the cleared branch is ignored");
});

test("204-R1: clearing the city drops its service-area rows and any late completion", async () => {
  const { ctl, latest, ofKind } = setup();
  ctl.update(context({ countryId: COUNTRY_A, cityId: CITY_C1 }));
  const service = ofKind("ServiceArea")[0];
  assert.equal(service.query.cityId, CITY_C1);

  ctl.update(context({ countryId: COUNTRY_A }));
  assert.equal(service.signal.aborted, true);
  assert.deepEqual(latest().lists.serviceAreas, []);

  service.resolve(ok([option("ServiceArea", SERVICE_S1, CITY_C1)]));
  await flush();
  assert.deepEqual(latest().lists.serviceAreas, []);
});

test("204-R1: dispose aborts in-flight reads and drops late completions", async () => {
  const { ctl, latest, calls } = setup();
  ctl.update(context());
  const country = calls[0];
  ctl.dispose();
  assert.equal(country.signal.aborted, true);
  country.resolve(ok([option("Country", COUNTRY_A, null)]));
  await flush();
  assert.deepEqual(latest().lists.countries, []);
});

test("204-R1: terminal ancestry comes from that terminal's own verified path", () => {
  const country = option("Country", COUNTRY_A, null);
  const areaA = option("AdministrativeArea", AREA_A1, COUNTRY_A);
  const areaA2 = option("AdministrativeArea", AREA_A2, COUNTRY_A);
  const nullAdminCity = option("City", CITY_C1, null);
  const filteredCity = option("City", CITY_C2, AREA_A1);
  const service = option("ServiceArea", SERVICE_S1, CITY_C1);

  // AdministrativeArea terminal must serialize its OWN row, even with a deeper city staged.
  const withCity = chooseLevel(
    chooseCity(chooseAdministrativeArea(chooseCountry(emptyChoice(), country), areaA), filteredCity),
    "City",
  );
  const asArea = chooseLevel(withCity, "AdministrativeArea");
  assert.deepEqual(choiceAncestry(asArea), {
    kind: "AdministrativeArea",
    countryId: COUNTRY_A,
    administrativeAreaId: AREA_A1,
    cityId: null,
    serviceAreaId: null,
  });

  // A city is an incoherent chain when its verified parent contradicts the staged area.
  const mismatched = { ...withCity, administrativeArea: areaA2 };
  assert.equal(choiceChainActive(mismatched), false);
  // A null-admin city under an explicit area filter is impossible for that branch.
  assert.equal(choiceChainActive({ ...withCity, city: nullAdminCity, administrativeArea: areaA }), false);
  assert.equal(choiceChainActive(withCity), true);

  assert.deepEqual(choiceAncestry(withCity), {
    kind: "City",
    countryId: COUNTRY_A,
    administrativeAreaId: AREA_A1,
    cityId: CITY_C2,
    serviceAreaId: null,
  });

  // A lawful active null-admin city + its service area.
  const withService = chooseLevel(
    chooseServiceArea(chooseCity(chooseCountry(emptyChoice(), country), nullAdminCity), service),
    "ServiceArea",
  );
  assert.equal(choiceChainActive(withService), true);
  assert.deepEqual(choiceAncestry(withService), {
    kind: "ServiceArea",
    countryId: COUNTRY_A,
    administrativeAreaId: null,
    cityId: CITY_C1,
    serviceAreaId: SERVICE_S1,
  });
  assert.equal(choiceAncestry(withService)?.cityId, CITY_C1, "the serialized terminal matches the tuple");
});

test("204-R1: inactive countries/areas/cities/service areas and wrong service parents are rejected", () => {
  const inactiveCountry = option("Country", COUNTRY_A, null, false);
  assert.equal(choiceChainActive(chooseCountry(emptyChoice(), inactiveCountry)), false);

  const country = option("Country", COUNTRY_A, null);
  const inactiveArea = option("AdministrativeArea", AREA_A1, COUNTRY_A, false);
  assert.equal(choiceChainActive(chooseAdministrativeArea(chooseCountry(emptyChoice(), country), inactiveArea)), false);

  const area = option("AdministrativeArea", AREA_A1, COUNTRY_A);
  const inactiveCity = option("City", CITY_C1, AREA_A1, false);
  assert.equal(
    choiceChainActive(chooseCity(chooseAdministrativeArea(chooseCountry(emptyChoice(), country), area), inactiveCity)),
    false,
  );

  const city = option("City", CITY_C1, null);
  const inactiveService = option("ServiceArea", SERVICE_S1, CITY_C1, false);
  assert.equal(
    choiceChainActive(chooseServiceArea(chooseCity(chooseCountry(emptyChoice(), country), city), inactiveService)),
    false,
  );

  const foreignService = option("ServiceArea", SERVICE_S1, CITY_C2);
  assert.equal(
    choiceChainActive(chooseServiceArea(chooseCity(chooseCountry(emptyChoice(), country), city), foreignService)),
    false,
  );

  // An area whose parent is not the staged country is incoherent (forged programmatic option).
  const foreignArea = option("AdministrativeArea", AREA_A1, COUNTRY_B);
  assert.equal(choiceChainActive(chooseAdministrativeArea(chooseCountry(emptyChoice(), country), foreignArea)), false);
});

test("204-R1: clear helpers cascade truthfully", () => {
  const country = option("Country", COUNTRY_A, null);
  const area = option("AdministrativeArea", AREA_A1, COUNTRY_A);
  const city = option("City", CITY_C1, AREA_A1);
  const service = option("ServiceArea", SERVICE_S1, CITY_C1);
  const staged = chooseServiceArea(
    chooseCity(chooseAdministrativeArea(chooseCountry(emptyChoice(), country), area), city),
    service,
  );

  assert.deepEqual(clearServiceArea(staged), { ...staged, serviceArea: null });
  assert.deepEqual(clearCity(staged), { ...staged, city: null, serviceArea: null });
  const withoutArea = clearAdministrativeArea(staged);
  assert.equal(withoutArea.country?.id, COUNTRY_A);
  assert.equal(withoutArea.administrativeArea, null);
  assert.equal(withoutArea.city, null);
  assert.equal(withoutArea.serviceArea, null);
});

test("204-R1: a blank label renders the localized unavailable name, never the code or id", () => {
  const blank = option("City", CITY_C1, null, true, "   ", "RAW-CODE");
  assert.equal(optionDisplayName(blank, "نام در دسترس نیست"), "نام در دسترس نیست");
  assert.notEqual(optionDisplayName(blank, "نام در دسترس نیست"), blank.code);
  assert.notEqual(optionDisplayName(blank, "نام در دسترس نیست"), blank.id);
  assert.equal(optionDisplayName(option("City", CITY_C1, null, true, " تهران "), "x"), "تهران");
});
