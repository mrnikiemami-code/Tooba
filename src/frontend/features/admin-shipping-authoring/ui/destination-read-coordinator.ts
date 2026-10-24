/**
 * One cohesive read coordinator for the cascading destination picker.
 *
 * Why it exists: the picker opens up to four sibling Geography branches. A single shared epoch makes
 * every sibling invalidate the others (a correct area response is thrown away the moment the city
 * read starts) and lets a city read consume stale/empty parent evidence. Here each stage owns its
 * own request identity AND its own branch key, so:
 *
 * - a sibling completion can never cancel or invalidate another sibling of the SAME context;
 * - a context change (language/country/area/city) immediately invalidates only the dependent stages,
 *   clears their rows and aborts their in-flight request, so stale rows are never selectable;
 * - a country-wide city read is only trusted AFTER the area set of that exact country/language was
 *   verified; a successful EMPTY area set is a known-empty set (fail closed), never "no constraint";
 * - a failed prerequisite leaves cities untrusted/unavailable instead of silently reading them;
 * - disposal/unmount drops every late completion.
 *
 * The coordinator is framework-free and takes the strict adapter as a parameter, so tests can drive
 * REAL deferred reads (out-of-order completions) without a React harness.
 */
import type {
  GeographyOption,
  GeographyQuery,
  GeographyReadFailure,
  GeographyReadOutcome,
} from "../api/geography-lookup-api.ts";

/** The branch context the operator currently has open (derived from the staged choice). */
export interface DestinationReadContext {
  readonly enabled: boolean;
  readonly languageId: string;
  readonly countryId: string | null;
  readonly administrativeAreaId: string | null;
  readonly cityId: string | null;
}

/** The four sibling lists of the picker. */
export type DestinationListKey = "countries" | "areas" | "cities" | "serviceAreas";

export interface DestinationReadState {
  readonly lists: Readonly<Record<DestinationListKey, readonly GeographyOption[]>>;
  readonly loading: Readonly<Record<DestinationListKey, boolean>>;
  readonly failures: Readonly<Partial<Record<DestinationListKey, GeographyReadFailure>>>;
}

/** The strict adapter contract (the real one is `loadGeographyOptions`). */
export type GeographyReader = (query: GeographyQuery, signal: AbortSignal) => Promise<GeographyReadOutcome>;

export interface DestinationReadCoordinator {
  update(context: DestinationReadContext): void;
  getState(): DestinationReadState;
  dispose(): void;
}

const LIST_KEYS: readonly DestinationListKey[] = ["countries", "areas", "cities", "serviceAreas"];
const EMPTY_ROW: readonly GeographyOption[] = [];

/** An inert state with no loaded rows (also the picker's initial render state). */
export function emptyDestinationReadState(): DestinationReadState {
  return {
    lists: { countries: EMPTY_ROW, areas: EMPTY_ROW, cities: EMPTY_ROW, serviceAreas: EMPTY_ROW },
    loading: { countries: false, areas: false, cities: false, serviceAreas: false },
    failures: {},
  };
}

interface Stage {
  /** The branch key this stage currently belongs to; `null` means "not opened". */
  key: string | null;
  /** Request identity of this stage only. */
  token: number;
  controller: AbortController | null;
}

const IDLE_CONTEXT: DestinationReadContext = {
  enabled: false,
  languageId: "",
  countryId: null,
  administrativeAreaId: null,
  cityId: null,
};

function normalize(value: string | null): string | null {
  if (value === null) return null;
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed.toLowerCase();
}

/**
 * Creates the coordinator.
 *
 * @param read The strict Geography reader (one branch per call).
 * @param notify Receives an immutable snapshot after every observable change.
 */
export function createDestinationReadCoordinator(
  read: GeographyReader,
  notify: (state: DestinationReadState) => void,
): DestinationReadCoordinator {
  const stages: Record<DestinationListKey, Stage> = {
    countries: { key: null, token: 0, controller: null },
    areas: { key: null, token: 0, controller: null },
    cities: { key: null, token: 0, controller: null },
    serviceAreas: { key: null, token: 0, controller: null },
  };
  /** `${languageId}|${countryId}` -> verified area id set of that exact branch (empty set = known empty). */
  const verifiedAreas = new Map<string, ReadonlySet<string>>();

  let context: DestinationReadContext = IDLE_CONTEXT;
  let disposed = false;
  let state: DestinationReadState = emptyDestinationReadState();

  function emit(): void {
    if (!disposed) notify(state);
  }

  function setList(name: DestinationListKey, rows: readonly GeographyOption[]): void {
    state = { ...state, lists: { ...state.lists, [name]: rows } };
  }

  function setLoading(name: DestinationListKey, value: boolean): void {
    state = { ...state, loading: { ...state.loading, [name]: value } };
  }

  function setFailure(name: DestinationListKey, failure: GeographyReadFailure | null): void {
    const failures = { ...state.failures };
    if (failure === null) delete failures[name];
    else failures[name] = failure;
    state = { ...state, failures };
  }

  function abortStage(name: DestinationListKey): void {
    stages[name].controller?.abort();
    stages[name].controller = null;
  }

  /** Drops the stage: aborts, invalidates its identity and clears its rows (kept truthful). */
  function resetStage(name: DestinationListKey): void {
    abortStage(name);
    stages[name].key = null;
    stages[name].token += 1;
    setList(name, EMPTY_ROW);
    setLoading(name, false);
    setFailure(name, null);
  }

  function holdStage(name: DestinationListKey, key: string, loading: boolean): void {
    abortStage(name);
    stages[name].key = key;
    stages[name].token += 1;
    setList(name, EMPTY_ROW);
    setFailure(name, null);
    setLoading(name, loading);
  }

  function beginStage(name: DestinationListKey, key: string): { token: number; signal: AbortSignal } {
    abortStage(name);
    const controller = new AbortController();
    stages[name].key = key;
    stages[name].token += 1;
    stages[name].controller = controller;
    setList(name, EMPTY_ROW);
    setLoading(name, true);
    setFailure(name, null);
    return { token: stages[name].token, signal: controller.signal };
  }

  function languageKey(): string {
    return context.languageId.trim().toLowerCase();
  }

  function countriesKey(): string | null {
    return languageKey().length === 0 ? null : languageKey();
  }

  function areasKey(): string | null {
    const countryId = normalize(context.countryId);
    if (countryId === null) return null;
    return `${languageKey()}|${countryId}`;
  }

  function citiesKey(): string | null {
    const countryId = normalize(context.countryId);
    if (countryId === null) return null;
    return `${languageKey()}|${countryId}|${normalize(context.administrativeAreaId) ?? ""}`;
  }

  function serviceAreasKey(): string | null {
    const cityId = normalize(context.cityId);
    if (cityId === null) return null;
    return `${languageKey()}|${cityId}`;
  }

  function failStage(name: DestinationListKey, key: string, failure: GeographyReadFailure): void {
    abortStage(name);
    stages[name].key = key;
    stages[name].token += 1;
    setList(name, EMPTY_ROW);
    setLoading(name, false);
    setFailure(name, failure);
  }

  function apply(name: DestinationListKey, token: number, key: string, outcome: GeographyReadOutcome): void {
    if (disposed) return;
    if (stages[name].token !== token || stages[name].key !== key) return;
    stages[name].controller = null;
    setLoading(name, false);
    if (outcome.state === "ok") {
      setList(name, outcome.options);
      setFailure(name, null);
    } else {
      setList(name, EMPTY_ROW);
      setFailure(name, outcome.failure);
    }

    if (name === "areas") {
      if (outcome.state === "ok") {
        // A successful read — including a legitimately EMPTY catalog — is the verified area set.
        verifiedAreas.set(key, new Set(outcome.options.map((option) => option.id.toLowerCase())));
        if (citiesKey() === `${key}|`) {
          // Country-wide cities waited for exactly this prerequisite.
          const inFlight = stages.cities.controller !== null && state.loading.cities;
          if (!inFlight) startCities();
        }
      } else if (citiesKey() !== null && citiesKey() === `${key}|`) {
        // Prerequisite failure: cities stay untrusted; no country-wide read is attempted.
        failStage("cities", citiesKey() as string, outcome.failure);
      }
    }
    emit();
  }

  function startCountries(): void {
    const key = countriesKey();
    if (key === null) {
      resetStage("countries");
      return;
    }
    const gate = beginStage("countries", key);
    void read({ kind: "Country", languageId: context.languageId }, gate.signal)
      .then((outcome) => apply("countries", gate.token, key, outcome))
      .catch(() => apply("countries", gate.token, key, { state: "failure", failure: { kind: "network" } }));
  }

  function startAreas(): void {
    const key = areasKey();
    if (key === null || normalize(context.countryId) === null) {
      resetStage("areas");
      return;
    }
    const gate = beginStage("areas", key);
    void read(
      { kind: "AdministrativeArea", languageId: context.languageId, countryId: context.countryId ?? undefined },
      gate.signal,
    )
      .then((outcome) => apply("areas", gate.token, key, outcome))
      .catch(() => apply("areas", gate.token, key, { state: "failure", failure: { kind: "network" } }));
  }

  function startCities(): void {
    const key = citiesKey();
    if (key === null || normalize(context.countryId) === null) {
      resetStage("cities");
      return;
    }
    if (normalize(context.administrativeAreaId) === null) {
      const prerequisite = verifiedAreas.get(`${languageKey()}|${normalize(context.countryId)}`);
      if (prerequisite === undefined) {
        holdStage("cities", key, true);
        return;
      }
      const gate = beginStage("cities", key);
      void read(
        {
          kind: "City",
          languageId: context.languageId,
          countryId: context.countryId ?? undefined,
          administrativeAreaId: null,
          knownAdministrativeAreaIds: prerequisite,
        },
        gate.signal,
      )
        .then((outcome) => apply("cities", gate.token, key, outcome))
        .catch(() => apply("cities", gate.token, key, { state: "failure", failure: { kind: "network" } }));
      return;
    }
    const gate = beginStage("cities", key);
    void read(
      {
        kind: "City",
        languageId: context.languageId,
        countryId: context.countryId ?? undefined,
        administrativeAreaId: context.administrativeAreaId,
      },
      gate.signal,
    )
      .then((outcome) => apply("cities", gate.token, key, outcome))
      .catch(() => apply("cities", gate.token, key, { state: "failure", failure: { kind: "network" } }));
  }

  function startServiceAreas(): void {
    const key = serviceAreasKey();
    if (key === null || normalize(context.cityId) === null) {
      resetStage("serviceAreas");
      return;
    }
    const gate = beginStage("serviceAreas", key);
    void read(
      { kind: "ServiceArea", languageId: context.languageId, cityId: context.cityId ?? undefined },
      gate.signal,
    )
      .then((outcome) => apply("serviceAreas", gate.token, key, outcome))
      .catch(() => apply("serviceAreas", gate.token, key, { state: "failure", failure: { kind: "network" } }));
  }

  function update(next: DestinationReadContext): void {
    if (disposed) return;

    // Disabled (save/reconcile/view): stop in-flight work, keep the rows, force a re-read on resume.
    if (!next.enabled) {
      for (const name of LIST_KEYS) {
        abortStage(name);
        stages[name].key = null;
        stages[name].token += 1;
        setLoading(name, false);
      }
      context = { ...next };
      emit();
      return;
    }

    context = { ...next };

    // Countries are independent of country/area/city: a sibling read never invalidates them.
    if (stages.countries.key !== countriesKey()) {
      if (countriesKey() === null) resetStage("countries");
      else startCountries();
    }

    if (stages.areas.key !== areasKey()) {
      if (areasKey() === null) resetStage("areas");
      else startAreas();
    }

    if (stages.cities.key !== citiesKey()) {
      if (citiesKey() === null) resetStage("cities");
      else startCities();
    }

    if (stages.serviceAreas.key !== serviceAreasKey()) {
      if (serviceAreasKey() === null) resetStage("serviceAreas");
      else startServiceAreas();
    }

    emit();
  }

  return {
    update,
    getState: () => state,
    dispose() {
      if (disposed) return;
      disposed = true;
      for (const name of LIST_KEYS) abortStage(name);
    },
  };
}
