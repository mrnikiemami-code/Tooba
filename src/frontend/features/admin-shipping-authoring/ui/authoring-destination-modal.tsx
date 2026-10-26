"use client";

/**
 * ONE modal for adding a delivery destination, with FOUR fixed cascading fields.
 *
 * The operator's requested flow is a single sequential path: country → province → city → area. The four
 * fields are always visible and labelled; each one stays disabled until the level above it is chosen, the
 * choices come ONLY from the real Geography reads of the opened branch (never a hardcoded list), and every
 * parent change clears its dependents so a stale/out-of-order response can never win.
 *
 * Only the DEEPEST chosen stage becomes a selection. Ancestors describe the path (the breadcrumb shown on
 * the chip) and are never added as extra coverage. The duplicate/overlap refusal happens on the explicit
 * Add attempt — staging a parent is never an error — and it is reported through the existing dialog, with
 * the staged path left intact so the operator can pick a lawful terminal.
 */
import { useEffect, useMemo, useRef, useState } from "react";
import { Dialog } from "../../../design-system";
import { loadGeographyOptions, type GeographyOption, type GeographyReadFailure } from "../api/geography-lookup-api.ts";
import type { EditorDocument, SelectedAreaRead } from "../model/editor-document.ts";
import type { ResolvedAreaAncestry } from "../model/editor-edits.ts";
import {
  choiceAncestry,
  choiceChainActive,
  choiceTerminal,
  choiceTerminalLabel,
  chooseAdministrativeArea,
  chooseCity,
  chooseCountry,
  chooseServiceArea,
  clearAdministrativeArea,
  clearCity,
  clearServiceArea,
  destinationAddAvailable,
  emptyStagedPath,
  isDuplicateSelection,
  optionDisplayName,
  pathChoice,
  type StagedPath,
} from "../model/destination-choice.ts";
import type { AuthoringMessages } from "../messages/authoring-messages.ts";
import {
  createDestinationReadCoordinator,
  emptyDestinationReadState,
  type DestinationReadCoordinator,
  type DestinationReadContext,
  type DestinationReadState,
} from "./destination-read-coordinator.ts";
import { AuthoringMessageBox } from "./authoring-message-box.tsx";

/** One display segment of a verified path (identity + real localized label, never a guessed name). */
export interface DestinationPathSegment {
  readonly label: string;
}

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

function PathSelect({
  label,
  value,
  options,
  disabled,
  placeholder,
  unavailableName,
  testId,
  onChange,
}: {
  label: string;
  value: string;
  options: readonly GeographyOption[];
  disabled: boolean;
  placeholder: string;
  unavailableName: string;
  testId: string;
  onChange: (option: GeographyOption | null) => void;
}) {
  return (
    <label className="grid gap-0.5">
      <span className="text-xs font-bold text-gray-700">{label}</span>
      <select
        className="min-h-10 w-full rounded-lg border border-border bg-surface px-2 text-sm text-gray-900 disabled:cursor-not-allowed disabled:opacity-60"
        value={value}
        disabled={disabled}
        data-testid={testId}
        onChange={(event) => onChange(options.find((option) => option.id === event.target.value) ?? null)}
      >
        <option value="">{placeholder}</option>
        {options.map((option) => (
          <option key={option.id} value={option.id} disabled={!option.isActive}>
            {optionDisplayName(option, unavailableName)}
          </option>
        ))}
      </select>
    </label>
  );
}

export function AuthoringDestinationModal({
  open,
  languageId,
  messages,
  disabled,
  isSelected,
  onAdd,
  onClose,
}: {
  open: boolean;
  languageId: string;
  messages: AuthoringMessages;
  /** Save/reconcile/view: the modal shows the path but cannot add. */
  disabled: boolean;
  isSelected: (area: SelectedAreaRead) => boolean;
  onAdd: (
    area: SelectedAreaRead,
    ancestry: ResolvedAreaAncestry,
    label: string,
    segments: readonly DestinationPathSegment[],
  ) => void;
  onClose: () => void;
}) {
  const [path, setPath] = useState<StagedPath>(() => emptyStagedPath());
  const [readState, setReadState] = useState<DestinationReadState>(() => emptyDestinationReadState());
  /** `null` = no message; otherwise the exact rejection the explicit Add attempt produced. */
  const [refusal, setRefusal] = useState<string | null>(null);
  const [added, setAdded] = useState(false);
  const coordinator = useRef<DestinationReadCoordinator | null>(null);

  const countryId = path.country?.id ?? "";
  const areaId = path.administrativeArea?.id ?? "";
  const cityId = path.city?.id ?? "";
  const serviceAreaId = path.serviceArea?.id ?? "";

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
      enabled: open && !disabled,
      languageId,
      countryId: countryId === "" ? null : countryId,
      administrativeAreaId: areaId === "" ? null : areaId,
      cityId: cityId === "" ? null : cityId,
      // The requested sequential branch: a city list is never read before an area is chosen.
      citiesRequireAdministrativeArea: true,
    }),
    [open, disabled, languageId, countryId, areaId, cityId],
  );

  useEffect(() => {
    coordinator.current?.update(context);
  }, [context]);

  // A changed path invalidates the previous attempt's explanation; nothing else.
  useEffect(() => {
    setRefusal(null);
    setAdded(false);
  }, [countryId, areaId, cityId, serviceAreaId]);

  const choice = pathChoice(path);
  const lists = readState.lists;
  const loading = Object.values(readState.loading).some(Boolean);
  const failure =
    readState.failures.countries ??
    readState.failures.areas ??
    readState.failures.cities ??
    readState.failures.serviceAreas ??
    null;
  const canAdd = destinationAddAvailable(choice, readState, disabled);

  /** Display labels of the chosen ancestors, in path order (display only, never sent to the server). */
  const segments = useMemo<DestinationPathSegment[]>(() => {
    const ordered = [path.country, path.administrativeArea, path.city, path.serviceArea];
    return ordered
      .filter((option): option is GeographyOption => option !== null)
      .map((option) => ({ label: optionDisplayName(option, messages.destinationNameUnavailable) }));
  }, [path, messages.destinationNameUnavailable]);

  /** Honest "representation gap" of the requested sequential branch (never a silent fallback). */
  const branchNote = useMemo<string | null>(() => {
    if (countryId === "" || loading) return null;
    if (readState.settled.areas && lists.areas.length === 0) return messages.destinationNoAdministrativeArea;
    if (areaId !== "" && readState.settled.cities && lists.cities.length === 0) return messages.destinationNoCity;
    if (cityId !== "" && readState.settled.serviceAreas && lists.serviceAreas.length === 0) {
      return messages.destinationNoServiceArea;
    }
    return null;
  }, [areaId, cityId, countryId, lists.areas.length, lists.cities.length, lists.serviceAreas.length, loading, messages, readState.settled]);

  const submit = () => {
    // Re-checked synchronously against the coordinator's CURRENT evidence, not a stale render closure.
    const evidence = coordinator.current?.getState() ?? readState;
    const current = pathChoice(path);
    if (!choiceChainActive(current)) return;
    if (!destinationAddAvailable(current, evidence, disabled)) return;
    const terminal = choiceTerminal(current);
    const ancestry = choiceAncestry(current);
    if (terminal === null || ancestry === null) return;
    if (isDuplicateSelection(terminal, isSelected)) {
      setRefusal(messages.destinationDuplicate);
      return;
    }
    setRefusal(null);
    onAdd(terminal, ancestry, choiceTerminalLabel(current), segments);
    setAdded(true);
    // Keep the chosen parents for rapid subsequent picks; only the deepest leaf is cleared again.
    if (path.serviceArea !== null) setPath((previous) => ({ ...previous, serviceArea: null }));
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} title={messages.destinationModalTitle} showCloseButton={false} size="lg">
        <div className="flex max-h-[70vh] flex-col gap-3 overflow-y-auto pe-1 text-sm text-gray-700">
          <p className="text-xs text-gray-600">{messages.destinationModalIntro}</p>

          <div className="grid gap-2 sm:grid-cols-2">
            <PathSelect
              label={messages.destinationCountryField}
              value={countryId}
              options={lists.countries}
              disabled={disabled}
              placeholder={messages.destinationSelectPlaceholder}
              unavailableName={messages.destinationNameUnavailable}
              testId="shipping-authoring-destination-country"
              onChange={(option) =>
                setPath((previous) => {
                  const base = pathChoice(previous);
                  return option === null
                    ? emptyStagedPath()
                    : pathOf(chooseCountry(base, option));
                })
              }
            />
            <PathSelect
              label={messages.destinationProvinceField}
              value={areaId}
              options={lists.areas}
              disabled={disabled || countryId === ""}
              placeholder={messages.destinationSelectPlaceholder}
              unavailableName={messages.destinationNameUnavailable}
              testId="shipping-authoring-destination-area"
              onChange={(option) =>
                setPath((previous) => {
                  const base = pathChoice(previous);
                  return pathOf(option === null ? clearAdministrativeArea(base) : chooseAdministrativeArea(base, option));
                })
              }
            />
            <PathSelect
              label={messages.destinationCityField}
              value={cityId}
              options={lists.cities}
              disabled={disabled || areaId === ""}
              placeholder={messages.destinationSelectPlaceholder}
              unavailableName={messages.destinationNameUnavailable}
              testId="shipping-authoring-destination-city"
              onChange={(option) =>
                setPath((previous) => {
                  const base = pathChoice(previous);
                  return pathOf(option === null ? clearCity(base) : chooseCity(base, option));
                })
              }
            />
            <PathSelect
              label={messages.destinationServiceAreaField}
              value={serviceAreaId}
              options={lists.serviceAreas}
              disabled={disabled || cityId === ""}
              placeholder={messages.destinationSelectPlaceholder}
              unavailableName={messages.destinationNameUnavailable}
              testId="shipping-authoring-destination-service-area"
              onChange={(option) =>
                setPath((previous) => {
                  const base = pathChoice(previous);
                  return pathOf(option === null ? clearServiceArea(base) : chooseServiceArea(base, option));
                })
              }
            />
          </div>

          {segments.length > 0 ? (
            <p className="text-xs text-gray-600" data-testid="shipping-authoring-destination-path">
              <span className="font-bold">{messages.destinationPathLabel}: </span>
              <span dir="auto">{segments.map((segment) => segment.label).join(" › ")}</span>
            </p>
          ) : null}

          {branchNote ? (
            <p className="text-xs text-amber-800" data-testid="shipping-authoring-destination-branch-note">
              {branchNote}
            </p>
          ) : null}
          {!branchNote && canAdd ? (
            <p className="text-xs text-gray-500" data-testid="shipping-authoring-destination-deepest">
              {messages.destinationSelectDeepestAvailable}
            </p>
          ) : null}
          {added ? (
            <p role="status" className="text-xs font-semibold text-emerald-800" data-testid="shipping-authoring-destination-added">
              {messages.destinationAddedKept}
            </p>
          ) : null}
          {loading ? <span className="text-xs text-gray-500">{messages.destinationLoading}</span> : null}
          {failure ? (
            <span role="alert" className="text-xs font-semibold text-danger">
              {failureText(failure, messages)}
            </span>
          ) : null}

          <div className="sticky bottom-0 flex flex-wrap justify-end gap-2 border-t border-border bg-surface pt-2">
            <button
              type="button"
              className="inline-flex min-h-10 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
              data-testid="shipping-authoring-destination-close"
              onClick={onClose}
            >
              {messages.destinationCloseAction}
            </button>
            <button
              type="button"
              className="inline-flex min-h-10 items-center rounded-lg bg-primary px-3 text-xs font-bold text-primary-foreground disabled:opacity-50"
              disabled={!canAdd}
              data-testid="shipping-authoring-destination-add"
              onClick={submit}
            >
              {messages.destinationAddConfirm}
            </button>
          </div>
        </div>
      </Dialog>

      <AuthoringMessageBox
        open={refusal !== null}
        title={messages.destinationOverlapRefusedTitle}
        text={refusal ?? ""}
        closeLabel={messages.destinationCloseAction}
        testId="shipping-authoring-destination-refusal"
        onClose={() => setRefusal(null)}
      />
    </>
  );
}

/** The four staged fields of one choice (the level is derived, so it is not stored twice). */
function pathOf(choice: {
  readonly country: GeographyOption | null;
  readonly administrativeArea: GeographyOption | null;
  readonly city: GeographyOption | null;
  readonly serviceArea: GeographyOption | null;
}): StagedPath {
  return {
    country: choice.country,
    administrativeArea: choice.administrativeArea,
    city: choice.city,
    serviceArea: choice.serviceArea,
  };
}
