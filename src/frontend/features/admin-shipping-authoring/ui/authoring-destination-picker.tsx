"use client";

/**
 * One cascading, on-demand destination picker inside one group.
 *
 * It reads ONLY the branch the operator opens (countries → areas of one country → cities of one
 * country[/area] → service areas of one city) through the feature-owned strict adapter, keeps the
 * actually returned rows as the only parent evidence, and hands the verified terminal + its typed
 * ancestry to the caller on a deliberate Add. Disabled during save/reconcile/view.
 *
 * TB-224 rules:
 * - ONLY the controls the chosen terminal level needs are rendered (country / country+province /
 *   country+city / country+city+service area), so the picker is not a wall of four disabled selects;
 * - staging a country, area or city is NOT an addition: choosing an already stored country therefore never
 *   shows a duplicate error, and the operator can keep browsing deeper levels of that same branch;
 * - the duplicate/overlap feedback belongs to ONE submitted Add attempt and is cleared as soon as the path
 *   or the terminal level changes — the stored chips are never removed by it.
 *
 * Request orchestration lives in ONE coordinator (`./destination-read-coordinator.ts`): sibling
 * branches own independent request identities, a country-wide city read waits for the verified area
 * set of that same country/language, and any parent/language change clears the dependent rows at once
 * so a late response of a previous parent can never win.
 */
import { useEffect, useMemo, useRef, useState } from "react";
import { loadGeographyOptions, type GeographyOption, type GeographyReadFailure } from "../api/geography-lookup-api.ts";
import type { AuthoringAreaKind, SelectedAreaRead } from "../model/editor-document.ts";
import type { ResolvedAreaAncestry } from "../model/editor-edits.ts";
import {
  AREA_LEVELS,
  choiceAncestry,
  choiceChainActive,
  choiceTerminal,
  choiceTerminalLabel,
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseLevel,
  chooseServiceArea,
  clearAdministrativeArea,
  clearCity,
  clearServiceArea,
  destinationAddEligibility,
  destinationAddAvailable,
  emptyChoice,
  isDuplicateSelection,
  optionDisplayName,
  type DestinationChoice,
} from "../model/destination-choice.ts";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";
import {
  createDestinationReadCoordinator,
  emptyDestinationReadState,
  type DestinationReadCoordinator,
  type DestinationReadContext,
  type DestinationReadState,
} from "./destination-read-coordinator.ts";

function failureText(failure: GeographyReadFailure, messages: AuthoringMessages): string {
  switch (failure.kind) {
    case "denied":
      return messages.destinationDenied;
    case "untrusted":
      return messages.destinationUntrusted;
    case "unavailable":
      return messages.destinationUnavailable;
    case "network":
      return messages.destinationNetwork;
  }
}

function levelLabel(level: AuthoringAreaKind, messages: AuthoringMessages): string {
  switch (level) {
    case "Country":
      return messages.areaCountry;
    case "AdministrativeArea":
      return messages.areaAdministrativeArea;
    case "City":
      return messages.areaCity;
    case "ServiceArea":
      return messages.areaServiceArea;
  }
}

function OptionSelect({
  label,
  value,
  options,
  disabled,
  unavailableName,
  testId,
  onChange,
}: {
  label: string;
  value: string;
  options: readonly GeographyOption[];
  disabled: boolean;
  unavailableName: string;
  testId: string;
  onChange: (option: GeographyOption | null) => void;
}) {
  return (
    <label className="grid gap-0.5">
      <span className="text-xs font-bold text-gray-600">{label}</span>
      <select
        className="min-h-8 w-full rounded-lg border border-border bg-surface px-2 text-xs text-gray-900"
        value={value}
        disabled={disabled}
        data-testid={testId}
        onChange={(event) => onChange(options.find((option) => option.id === event.target.value) ?? null)}
      >
        <option value="">—</option>
        {options.map((option) => (
          <option key={option.id} value={option.id} disabled={!option.isActive}>
            {optionDisplayName(option, unavailableName)}
          </option>
        ))}
      </select>
    </label>
  );
}

/** Which path stages a terminal level needs (the administrative filter stays OPTIONAL for city/service). */
function needsAdministrativeArea(level: AuthoringAreaKind): boolean {
  return level === "AdministrativeArea" || level === "City" || level === "ServiceArea";
}

function needsCity(level: AuthoringAreaKind): boolean {
  return level === "City" || level === "ServiceArea";
}

function needsServiceArea(level: AuthoringAreaKind): boolean {
  return level === "ServiceArea";
}

export function AuthoringDestinationPicker({
  languageId,
  locale,
  messages,
  isSelected,
  disabled,
  onAdd,
}: {
  languageId: string;
  locale: AuthoringLocale;
  messages: AuthoringMessages;
  isSelected: (area: SelectedAreaRead) => boolean;
  disabled: boolean;
  onAdd: (area: SelectedAreaRead, ancestry: ResolvedAreaAncestry, label: string) => void;
}) {
  const [choice, setChoice] = useState<DestinationChoice>(() => emptyChoice());
  const [readState, setReadState] = useState<DestinationReadState>(() => emptyDestinationReadState());
  /** Set ONLY by a submitted Add attempt that the authoritative rule needs to explain; cleared on path change. */
  const [rejectedAttempt, setRejectedAttempt] = useState<string | null>(null);
  const coordinator = useRef<DestinationReadCoordinator | null>(null);

  const countryId = choice.country?.id ?? "";
  const areaId = choice.administrativeArea?.id ?? "";
  const cityId = choice.city?.id ?? "";
  const serviceAreaId = choice.serviceArea?.id ?? "";

  // One owner of the picker's read lifecycle (React StrictMode safe: created/disposed per mount).
  useEffect(() => {
    const created = createDestinationReadCoordinator(loadGeographyOptions, setReadState);
    coordinator.current = created;
    setReadState(created.getState());
    return () => {
      created.dispose();
      if (coordinator.current === created) coordinator.current = null;
    };
  }, []);

  const context = useMemo<DestinationReadContext>(
    () => ({
      enabled: !disabled,
      languageId,
      countryId: countryId === "" ? null : countryId,
      administrativeAreaId: areaId === "" ? null : areaId,
      cityId: cityId === "" ? null : cityId,
    }),
    [disabled, languageId, countryId, areaId, cityId],
  );

  useEffect(() => {
    coordinator.current?.update(context);
  }, [context]);

  // A changed path or terminal level invalidates the previous attempt's explanation, nothing else.
  useEffect(() => {
    setRejectedAttempt(null);
  }, [choice.level, countryId, areaId, cityId, serviceAreaId]);

  const lists = readState.lists;
  const loading = Object.values(readState.loading).some(Boolean);
  const failure =
    readState.failures.countries ??
    readState.failures.areas ??
    readState.failures.cities ??
    readState.failures.serviceAreas ??
    null;

  const terminal = choiceTerminal(choice);
  const ancestry = choiceAncestry(choice);
  // Add is gated by the CURRENT verified rows (not by staged flags alone) — and NOT by "already stored",
  // because staging an existing country must stay usable to reach a province/city beneath it.
  const canAdd = destinationAddAvailable(choice, readState, disabled);

  return (
    <div className="grid gap-2 rounded-lg border border-dashed border-border p-2" data-testid="shipping-authoring-destination-picker">
      <p className="text-[11px] font-bold text-gray-800">{messages.destinationTitle}</p>

      <div className="grid gap-2 sm:grid-cols-2">
        <label className="grid gap-0.5">
          <span className="text-xs font-bold text-gray-600">{messages.destinationLevel}</span>
          <select
            className="min-h-8 w-full rounded-lg border border-border bg-surface px-2 text-xs text-gray-900"
            value={choice.level}
            disabled={disabled}
            data-testid="shipping-authoring-destination-level"
            onChange={(event) => {
              const level = AREA_LEVELS.find((candidate) => candidate === event.target.value);
              if (level === undefined) return;
              setChoice((previous) => chooseLevel(previous, level));
            }}
          >
            {AREA_LEVELS.map((level) => (
              <option key={level} value={level}>
                {levelLabel(level, messages)}
              </option>
            ))}
          </select>
        </label>

        <OptionSelect
          label={messages.areaCountry}
          value={countryId}
          options={lists.countries}
          disabled={disabled}
          unavailableName={messages.destinationNameUnavailable}
          testId="shipping-authoring-destination-country"
          onChange={(option) =>
            setChoice((previous) =>
              option === null ? emptyChoice(previous.level) : chooseCountry(previous, option),
            )
          }
        />

        {/* Only the levels this terminal actually needs are rendered. */}
        {needsAdministrativeArea(choice.level) ? (
          <OptionSelect
            label={messages.areaAdministrativeArea}
            value={areaId}
            options={lists.areas}
            disabled={disabled || countryId === ""}
            unavailableName={messages.destinationNameUnavailable}
            testId="shipping-authoring-destination-area"
            onChange={(option) =>
              setChoice((previous) =>
                option === null ? clearAdministrativeArea(previous) : chooseAdministrativeArea(previous, option),
              )
            }
          />
        ) : null}
        {needsCity(choice.level) ? (
          <OptionSelect
            label={messages.areaCity}
            value={cityId}
            options={lists.cities}
            disabled={disabled || countryId === ""}
            unavailableName={messages.destinationNameUnavailable}
            testId="shipping-authoring-destination-city"
            onChange={(option) => setChoice((previous) => (option === null ? clearCity(previous) : chooseCity(previous, option)))}
          />
        ) : null}
        {needsServiceArea(choice.level) ? (
          <OptionSelect
            label={messages.areaServiceArea}
            value={serviceAreaId}
            options={lists.serviceAreas}
            disabled={disabled || cityId === ""}
            unavailableName={messages.destinationNameUnavailable}
            testId="shipping-authoring-destination-service-area"
            onChange={(option) =>
              setChoice((previous) => (option === null ? clearServiceArea(previous) : chooseServiceArea(previous, option)))
            }
          />
        ) : null}
      </div>

      <p className="text-xs text-gray-500">{messages.destinationHint}</p>

      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
          disabled={!canAdd}
          data-testid="shipping-authoring-destination-add"
          onClick={() => {
            // Re-checked synchronously against the coordinator's CURRENT evidence, not a stale closure.
            const evidence = coordinator.current?.getState() ?? readState;
            if (!choiceChainActive(choice)) return;
            if (!destinationAddEligibility(choice, evidence)) return;
            const settled = choiceTerminal(choice);
            const settledAncestry = choiceAncestry(choice);
            if (settled === null || settledAncestry === null) return;
            if (isDuplicateSelection(settled, isSelected)) {
              // The attempt is refused, but the staged path stays so a deeper level is still reachable.
              setRejectedAttempt(`${settled.kind}:${settled.id.toLowerCase()}`);
              return;
            }            setRejectedAttempt(null);
            onAdd(settled, settledAncestry, choiceTerminalLabel(choice));
          }}
        >
          {messages.destinationAddAction}
        </button>
        <button
          type="button"
          className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-700"
          data-testid="shipping-authoring-destination-clear"
          disabled={disabled}
          onClick={() => setChoice(emptyChoice(choice.level))}
        >
          {messages.groupRemoveCancelAction}
        </button>
        {loading ? <span className="text-xs text-gray-500">{messages.destinationLoading}</span> : null}
        {failure ? (
          <span role="alert" className="text-xs font-semibold text-danger">
            {failureText(failure, messages)}
          </span>
        ) : null}
        {rejectedAttempt !== null ? (
          <span role="alert" className="text-xs font-semibold text-danger" data-testid="shipping-authoring-destination-duplicate">
            {messages.destinationDuplicate}{" "}
            <span className="font-normal text-gray-600">{messages.destinationOverlapHint}</span>
          </span>
        ) : null}
      </div>

      {countryId !== "" && !loading && lists.cities.length === 0 && needsCity(choice.level) ? (
        <p className="text-xs text-gray-500">{messages.destinationEmpty}</p>
      ) : null}
    </div>
  );
}
