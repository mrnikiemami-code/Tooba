"use client";

/**
 * One cascading, on-demand destination picker inside one group.
 *
 * It reads ONLY the branch the operator opens (countries → areas of one country → cities of one
 * country[/area] → service areas of one city) through the feature-owned strict adapter, keeps the
 * actually returned rows as the only parent evidence, and hands the verified terminal + its typed
 * ancestry to the caller on a deliberate Add. Disabled during save/reconcile/view.
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
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseLevel,
  chooseServiceArea,
  clearAdministrativeArea,
  clearCity,
  clearServiceArea,
  destinationAddEligibility,
  emptyChoice,
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
      <span className="text-[10px] font-bold text-gray-600">{label}</span>
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
  onAdd: (area: SelectedAreaRead, ancestry: ResolvedAreaAncestry) => void;
}) {
  const [choice, setChoice] = useState<DestinationChoice>(() => emptyChoice());
  const [readState, setReadState] = useState<DestinationReadState>(() => emptyDestinationReadState());
  const [rejected, setRejected] = useState(false);
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
  const duplicate = terminal !== null && isSelected(terminal);
  // Add is gated by the CURRENT verified rows (not by staged flags alone).
  const eligible = destinationAddEligibility(choice, readState);
  const canAdd = !disabled && eligible && ancestry !== null && !duplicate;

  return (
    <div className="grid gap-2 rounded-lg border border-dashed border-border p-2" data-testid="shipping-authoring-destination-picker">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-[11px] font-bold text-gray-800">{messages.destinationTitle}</p>
        <div className="flex gap-1">
          {AREA_LEVELS.map((level) => (
            <button
              key={level}
              type="button"
              disabled={disabled}
              className={`inline-flex min-h-7 items-center rounded-lg border px-2 text-[11px] font-bold ${
                choice.level === level ? "border-primary bg-primary/10 text-primary" : "border-border text-gray-700"
              }`}
              data-testid={`shipping-authoring-destination-level-${level}`}
              onClick={() => setChoice((previous) => chooseLevel(previous, level))}
            >
              {levelLabel(level, messages)}
            </button>
          ))}
        </div>
      </div>

      <p className="text-[10px] text-gray-500">{messages.destinationHint}</p>

      <div className="grid gap-2 md:grid-cols-4">
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
        <OptionSelect
          label={messages.areaCity}
          value={cityId}
          options={lists.cities}
          disabled={disabled || countryId === ""}
          unavailableName={messages.destinationNameUnavailable}
          testId="shipping-authoring-destination-city"
          onChange={(option) => setChoice((previous) => (option === null ? clearCity(previous) : chooseCity(previous, option)))}
        />
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
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
          disabled={!canAdd}
          data-testid="shipping-authoring-destination-add"
          onClick={() => {
            // Re-checked synchronously against the coordinator's CURRENT evidence, not a stale closure.
            const evidence = coordinator.current?.getState() ?? readState;
            if (terminal === null || ancestry === null) return;
            if (!choiceChainActive(choice) || !destinationAddEligibility(choice, evidence)) return;
            if (isSelected(terminal)) {
              setRejected(true);
              return;
            }
            setRejected(false);
            onAdd(terminal, ancestry);
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
        {loading ? <span className="text-[10px] text-gray-500">{messages.destinationLoading}</span> : null}
        {failure ? (
          <span role="alert" className="text-[10px] font-semibold text-danger">
            {failureText(failure, messages)}
          </span>
        ) : null}
        {duplicate || rejected ? (
          <span role="alert" className="text-[10px] font-semibold text-danger" data-testid="shipping-authoring-destination-duplicate">
            {messages.destinationDuplicate}
          </span>
        ) : null}
      </div>

      {countryId !== "" && !loading && lists.cities.length === 0 ? (
        <p className="text-[10px] text-gray-500">{messages.destinationEmpty}</p>
      ) : null}
    </div>
  );
}
