"use client";

/**
 * One cascading, on-demand destination picker inside one group.
 *
 * It reads ONLY the branch the operator opens (countries → areas of one country → cities of one
 * country[/area] → service areas of one city) through the feature-owned strict adapter, keeps the
 * actually returned rows as the only parent evidence, and hands the verified terminal + its typed
 * ancestry to the caller on a deliberate Add. Disabled during save/reconcile/view; a late response of
 * a previous parent/language request can never overwrite the current stage.
 */
import { useEffect, useRef, useState } from "react";
import { loadGeographyOptions, type GeographyOption, type GeographyReadFailure } from "../api/geography-lookup-api.ts";
import type { AuthoringAreaKind, SelectedAreaRead } from "../model/editor-document.ts";
import type { ResolvedAreaAncestry } from "../model/editor-edits.ts";
import {
  AREA_LEVELS,
  choiceAncestry,
  choiceTerminal,
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseLevel,
  chooseServiceArea,
  emptyChoice,
  type DestinationChoice,
} from "../model/destination-choice.ts";
import type { AuthoringLocale, AuthoringMessages } from "../messages/authoring-messages.ts";

type Lists = {
  readonly countries: readonly GeographyOption[];
  readonly areas: readonly GeographyOption[];
  readonly cities: readonly GeographyOption[];
  readonly serviceAreas: readonly GeographyOption[];
};

const EMPTY_LISTS: Lists = { countries: [], areas: [], cities: [], serviceAreas: [] };

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
  testId,
  onChange,
}: {
  label: string;
  value: string;
  options: readonly GeographyOption[];
  disabled: boolean;
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
          <option key={option.id} value={option.id}>
            {option.label.trim().length > 0 ? option.label : option.code}
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
  const [lists, setLists] = useState<Lists>(EMPTY_LISTS);
  const [failure, setFailure] = useState<GeographyReadFailure | null>(null);
  const [loading, setLoading] = useState(false);
  const [rejected, setRejected] = useState(false);
  const epoch = useRef(0);

  const countryId = choice.country?.id ?? "";
  const areaId = choice.administrativeArea?.id ?? "";
  const cityId = choice.city?.id ?? "";

  useEffect(() => {
    if (disabled) return;
    const current = ++epoch.current;
    const controller = new AbortController();
    setLoading(true);
    void loadGeographyOptions({ kind: "Country", languageId }, controller.signal).then((outcome) => {
      if (current !== epoch.current || controller.signal.aborted) return;
      setLoading(false);
      if (outcome.state === "ok") {
        setLists((previous) => ({ ...previous, countries: outcome.options }));
        setFailure(null);
      } else {
        setLists((previous) => ({ ...previous, countries: [] }));
        setFailure(outcome.failure);
      }
    });
    return () => controller.abort();
  }, [disabled, languageId]);

  useEffect(() => {
    if (disabled || countryId === "") return;
    const current = ++epoch.current;
    const controller = new AbortController();
    setLoading(true);
    void loadGeographyOptions({ kind: "AdministrativeArea", languageId, countryId }, controller.signal).then((outcome) => {
      if (current !== epoch.current || controller.signal.aborted) return;
      setLoading(false);
      if (outcome.state === "ok") {
        setLists((previous) => ({ ...previous, areas: outcome.options }));
        setFailure(null);
      } else {
        setLists((previous) => ({ ...previous, areas: [] }));
        setFailure(outcome.failure);
      }
    });
    return () => controller.abort();
  }, [disabled, countryId, languageId]);

  useEffect(() => {
    if (disabled || countryId === "") return;
    const current = ++epoch.current;
    const controller = new AbortController();
    const knownAdministrativeAreaIds = new Set(lists.areas.map((option) => option.id.toLowerCase()));
    setLoading(true);
    void loadGeographyOptions(
      {
        kind: "City",
        languageId,
        countryId,
        administrativeAreaId: areaId || null,
        knownAdministrativeAreaIds: knownAdministrativeAreaIds.size > 0 ? knownAdministrativeAreaIds : undefined,
      },
      controller.signal,
    ).then((outcome) => {
      if (current !== epoch.current || controller.signal.aborted) return;
      setLoading(false);
      if (outcome.state === "ok") {
        setLists((previous) => ({ ...previous, cities: outcome.options }));
        setFailure(null);
      } else {
        setLists((previous) => ({ ...previous, cities: [] }));
        setFailure(outcome.failure);
      }
    });
    return () => controller.abort();
    // `lists.areas` is deliberately NOT a dependency: only the opened branch may trigger a read.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [disabled, countryId, areaId, languageId]);

  useEffect(() => {
    if (disabled || cityId === "") return;
    const current = ++epoch.current;
    const controller = new AbortController();
    setLoading(true);
    void loadGeographyOptions({ kind: "ServiceArea", languageId, cityId }, controller.signal).then((outcome) => {
      if (current !== epoch.current || controller.signal.aborted) return;
      setLoading(false);
      if (outcome.state === "ok") {
        setLists((previous) => ({ ...previous, serviceAreas: outcome.options }));
        setFailure(null);
      } else {
        setLists((previous) => ({ ...previous, serviceAreas: [] }));
        setFailure(outcome.failure);
      }
    });
    return () => controller.abort();
  }, [disabled, cityId, languageId]);

  const terminal = choiceTerminal(choice);
  const ancestry = choiceAncestry(choice);
  const duplicate = terminal !== null && isSelected(terminal);

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
          testId="shipping-authoring-destination-area"
          onChange={(option) =>
            setChoice((previous) => (option === null ? previous : chooseAdministrativeArea(previous, option)))
          }
        />
        <OptionSelect
          label={messages.areaCity}
          value={cityId}
          options={lists.cities}
          disabled={disabled || countryId === ""}
          testId="shipping-authoring-destination-city"
          onChange={(option) => setChoice((previous) => (option === null ? previous : chooseCity(previous, option)))}
        />
        <OptionSelect
          label={messages.areaServiceArea}
          value={choice.serviceArea?.id ?? ""}
          options={lists.serviceAreas}
          disabled={disabled || cityId === ""}
          testId="shipping-authoring-destination-service-area"
          onChange={(option) =>
            setChoice((previous) => (option === null ? previous : chooseServiceArea(previous, option)))
          }
        />
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          className="inline-flex min-h-8 items-center rounded-lg border border-border bg-surface px-2 text-[11px] font-bold text-gray-800"
          disabled={disabled || terminal === null || ancestry === null || duplicate}
          data-testid="shipping-authoring-destination-add"
          onClick={() => {
            if (terminal === null || ancestry === null) return;
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
