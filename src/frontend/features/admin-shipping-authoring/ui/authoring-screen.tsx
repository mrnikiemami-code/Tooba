"use client";

/**
 * Rendered editor of ONE existing shipping draft: General (names/type/two settings), the ONE
 * method-level weekly schedule and the rate rows under each EXISTING region group.
 *
 * Boundary: this component only renders state and dispatches the feature controller. Transport,
 * decoding, ancestry, validation and the ONE atomic PUT stay in `api`/`model`/`session`; group
 * topology/geography editing, whole-method creation, activation and unsupported cutoff/carrier/
 * funding controls are NOT here.
 */
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Dialog } from "../../../design-system";
import { loadAuthoringTypeChoices, type AuthoringTypeChoice } from "../api/authoring-lookup-api.ts";
import type { EditorTransportFailure, EditorPropertyError } from "../api/editor-document-api.ts";
import { authoringMessages, authoringFormat, type AuthoringLocale, type AuthoringMessages } from "../messages/authoring-messages.ts";
import type { AuthoringCalculationMode, EditorDocument, FreightPaymentMode } from "../model/editor-document.ts";
import { indexDestinationFacts, unresolvedSelections } from "../model/editor-destination-ancestry.ts";
import { buildAuthoringIntent, hasNoIssues, intentHasEdits } from "../model/authoring-intent.ts";
import { resetAuthoringDrafts, seedAuthoringDrafts } from "../model/authoring-drafts.ts";
import {
  withCalculationMode,
  withDescription,
  withFreightPaymentMode,
  withName,
  withShippingType,
  type EditorFormState,
} from "../model/editor-form.ts";
import {
  withAddedRow,
  withRemovedRow,
  withRowField,
  type RateField,
  type RatesFormState,
} from "../model/editor-rates.ts";
import {
  configureWeek,
  resetWeek,
  withDayTime,
  withDayWorking,
  withPendingTimeZone,
  withTimeZone,
  type WeekFormState,
} from "../model/editor-week.ts";
import { hasUnsavedWork, type EditorSaveStatus } from "../session/editor-session.ts";
import { planAuthoringExit, type AuthoringExitDestination } from "./authoring-exit.ts";
import { useEditorController } from "./use-editor-controller.ts";
import { AuthoringGeneralTab } from "./authoring-general-tab.tsx";
import { AuthoringWeekEditor, weekIssueMessage } from "./authoring-week-editor.tsx";
import { rowIssueMessage } from "./authoring-rates-editor.tsx";
import { AuthoringRegionsPanel } from "./authoring-regions-panel.tsx";

const TABS = ["general", "regions"] as const;
type ScreenTab = (typeof TABS)[number];

const AREA_KIND_KEYS = {
  Country: "areaCountry",
  AdministrativeArea: "areaAdministrativeArea",
  City: "areaCity",
  ServiceArea: "areaServiceArea",
} as const;

/** Field labels for server validation properties; raw codes are never shown as prose. */
function fieldLabel(property: string, messages: AuthoringMessages): string {
  if (/name/i.test(property)) return messages.fieldNameLabel;
  if (/description/i.test(property)) return messages.fieldDescriptionLabel;
  if (/settings|freight|calculation/i.test(property)) return messages.fieldSettingsLabel;
  if (/type/i.test(property)) return messages.fieldTypeLabel;
  return messages.fieldNameLabel;
}

function failureMessages(failure: EditorTransportFailure, messages: AuthoringMessages): readonly string[] {
  switch (failure.kind) {
    case "denied":
      return [messages.saveDenied];
    case "conflict":
      return [messages.saveConflict];
    case "validation":
      return [messages.saveValidation];
    case "unavailable":
      return [messages.saveUnavailable];
    case "outcomeUnknown":
      return [messages.saveUnknown];
    case "untrusted":
      return [messages.loadUntrusted];
    case "notFound":
      return [messages.loadNotFound];
    case "network":
      return [messages.saveUnavailable];
    default:
      return [messages.saveError];
  }
}

function errorGroups(failure: EditorTransportFailure): readonly EditorPropertyError[] {
  return failure.kind === "conflict" || failure.kind === "validation" || failure.kind === "httpError"
    ? failure.errors
    : [];
}

export function ShippingAuthoringScreen({
  draftId,
  locale,
  initialMode,
}: {
  draftId: string;
  locale: AuthoringLocale;
  initialMode: "view" | "edit";
}) {
  const router = useRouter();
  const messages = useMemo(() => authoringMessages(locale), [locale]);
  const direction = locale === "fa" ? "rtl" : "ltr";
  const { controller, state } = useEditorController(draftId);
  const session = state.session;
  const document = session.baseline;

  const [mode, setMode] = useState<"view" | "edit">(initialMode);
  const [tab, setTab] = useState<ScreenTab>("general");
  const [form, setForm] = useState<EditorFormState | null>(null);
  const [rates, setRates] = useState<RatesFormState>({});
  const [week, setWeek] = useState<WeekFormState | null>(null);
  const [contentLanguageId, setContentLanguageId] = useState<string>("");
  const [types, setTypes] = useState<readonly AuthoringTypeChoice[]>([]);
  const [typesState, setTypesState] = useState<"loading" | "ok" | "error">("loading");
  const [notice, setNotice] = useState<string | null>(null);
  const [confirmDiscardVisible, setConfirmDiscardVisible] = useState(false);
  const applyRef = useRef(false);
  const exitDestinationRef = useRef<AuthoringExitDestination>("back");

  const formSeed = state.formSeed;
  const rowCounter = useRef(0);

  /**
   * All local drafts (General, rate rows, weekly schedule) are seeded from the CONFIRMED baseline
   * only on an accepted transition (fresh load, accepted save, explicit discard + fresh read). A
   * render, an invalid `edited({})` or a reconcile never re-seeds them, so typed work survives.
   */
  useEffect(() => {
    if (!document) return;
    const drafts = seedAuthoringDrafts(document);
    setForm(drafts.form);
    setRates(drafts.rates);
    setWeek(drafts.week);
    const defaultLanguage = document.presentation.contentLanguages.find(
      (language) => language.languageId.toLowerCase() === document.presentation.defaultContentLanguageId.toLowerCase(),
    );
    const fallback = (defaultLanguage ?? document.presentation.contentLanguages[0])?.languageId ?? "";
    // Keep the operator's chosen language when the new confirmed document still offers it.
    setContentLanguageId((current) =>
      document.presentation.contentLanguages.some((language) => language.languageId === current) ? current : fallback,
    );
    // `document` is intentionally excluded: reconcile updates the baseline but must preserve the
    // operator's pending local edits for review against the new confirmed baseline.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formSeed]);

  // A stale success notice never survives a fresh read.
  useEffect(() => {
    if (session.load === "loading") setNotice(null);
  }, [session.load]);

  useEffect(() => {
    let current = true;
    void (async () => {
      const outcome = await loadAuthoringTypeChoices();
      if (!current) return;
      if (outcome.state === "ok") {
        setTypes(outcome.value);
        setTypesState("ok");
        return;
      }
      setTypes([]);
      setTypesState("error");
    })();
    return () => {
      current = false;
    };
  }, []);

  // Dirty/unknown/in-flight work must survive a browser-level navigation or reload.
  useEffect(() => {
    const handler = (event: BeforeUnloadEvent): void => {
      if (!hasUnsavedWork(session)) return;
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", handler);
    return () => window.removeEventListener("beforeunload", handler);
  }, [session]);

  useEffect(() => {
    if (session.decision === "discardUnsaved") setConfirmDiscardVisible(true);
  }, [session.decision]);

  const index = useMemo(() => (document ? indexDestinationFacts(document) : null), [document]);
  const intent = useMemo(
    () => (document && form && week ? buildAuthoringIntent(document, form, rates, week) : null),
    [document, form, rates, week],
  );

  /**
   * ONE dispatch for every capability: sets the three local drafts and hands the controller a SINGLE
   * composed patch, so a General edit never drops a pending rate/week change and vice versa.
   */
  const commit = useCallback(
    (nextForm: EditorFormState, nextRates: RatesFormState, nextWeek: WeekFormState) => {
      setForm(nextForm);
      setRates(nextRates);
      setWeek(nextWeek);
      if (!document) return;
      const composed = buildAuthoringIntent(document, nextForm, nextRates, nextWeek);
      controller.edit(composed.edits, composed.localDirty);
    },
    [controller, document],
  );

  const pushEdits = useCallback(
    (nextForm: EditorFormState) => {
      if (!week) return;
      commit(nextForm, rates, week);
    },
    [commit, rates, week],
  );

  const changeRow = useCallback(
    (groupId: string, rowKey: string, field: RateField, value: string) => {
      if (!form || !week) return;
      commit(form, withRowField(rates, groupId, rowKey, field, value), week);
    },
    [commit, form, rates, week],
  );

  const addRow = useCallback(
    (groupId: string) => {
      if (!form || !week) return;
      rowCounter.current += 1;
      commit(form, withAddedRow(rates, groupId, `new-${rowCounter.current}`), week);
    },
    [commit, form, rates, week],
  );

  const removeRow = useCallback(
    (groupId: string, rowKey: string) => {
      if (!form || !week) return;
      commit(form, withRemovedRow(rates, groupId, rowKey), week);
    },
    [commit, form, rates, week],
  );

  const changeWeek = useCallback(
    (nextWeek: WeekFormState) => {
      if (!form) return;
      commit(form, rates, nextWeek);
    },
    [commit, form, rates],
  );

  const busy = session.save === "saving" || session.reconcile === "pending";
  const readOnly = mode === "view" || busy;
  const blockedByFacts = document && index ? !index.available : false;
  const unresolved = document && index ? unresolvedSelections(document, index) : [];
  const formIssues = intent?.issues.general ?? [];
  const rateRowIssues = intent?.issues.rates ?? [];
  const weekProblemIssues = intent?.issues.week ?? [];
  const nameIssueLanguageIds = formIssues.flatMap((issue) =>
    issue.code === "nameRequired" ? [issue.languageId] : [],
  );
  const settingsIncomplete = formIssues.some((issue) => issue.code === "settingsIncomplete");
  const hasIntent = intent !== null && intentHasEdits(intent.edits);
  const canApply =
    !readOnly &&
    session.unsaved &&
    document !== null &&
    hasIntent &&
    intent !== null &&
    hasNoIssues(intent.issues) &&
    !blockedByFacts &&
    unresolved.length === 0 &&
    !session.commitUncertain &&
    session.load === "loaded";

  const apply = useCallback(async () => {
    if (applyRef.current || !index) return;
    applyRef.current = true;
    setNotice(null);
    try {
      const outcome = await controller.apply(index.resolver);
      if (outcome.status !== "sent") return;
      const revision = controller.getState().savedRevision;
      setNotice(authoringFormat(messages.saved, { revision: revision ?? "" }));
      setMode("view");
    } finally {
      applyRef.current = false;
    }
  }, [controller, index, messages.saved]);

  /** Enters the requested destination once the session confirms nothing is at risk. */
  const enterDestination = useCallback(
    (destination: AuthoringExitDestination) => {
      exitDestinationRef.current = "back";
      setConfirmDiscardVisible(false);
      if (destination === "back") {
        router.push("/admin/shipping-services");
        return;
      }
      // "Cancel Edit": stay on the screen, show the confirmed document again, keep the session.
      if (session.commitUncertain) {
        // The latched unknown commit is never cleared here: only the explicit fresh read recovers.
        controller.confirmDiscard();
        setForm(null);
        setRates({});
        setWeek(null);
      } else {
        controller.cancelDiscard();
        // ONE cohesive reseed: General AND rate rows AND week come from the SAME confirmed baseline,
        // so a discarded row/week can never stay on screen or be resurrected by the next edit.
        if (document) {
          const drafts = resetAuthoringDrafts(document);
          setForm(drafts.form);
          setRates(drafts.rates);
          setWeek(drafts.week);
        }
        controller.edit({});
      }
      setMode("view");
      setNotice(null);
    },
    [controller, document, router, session.commitUncertain],
  );

  const requestExit = useCallback(
    (destination: AuthoringExitDestination) => {
      const plan = planAuthoringExit({
        save: session.save,
        reconciling: session.reconcile === "pending",
        hasLocalWork: hasUnsavedWork(session),
      });
      if (plan === "blocked") return;
      if (plan === "direct") {
        enterDestination(destination);
        return;
      }
      exitDestinationRef.current = destination;
      // The existing discard decision (session) owns the confirmation dialog.
      controller.requestClose();
    },
    [controller, enterDestination, session],
  );

  const goBack = useCallback(() => requestExit("back"), [requestExit]);

  const cancelEdit = useCallback(() => requestExit("view"), [requestExit]);

  const confirmDiscard = useCallback(() => {
    const destination = exitDestinationRef.current;
    if (destination === "back") {
      setConfirmDiscardVisible(false);
      controller.confirmDiscard();
      exitDestinationRef.current = "back";
      router.push("/admin/shipping-services");
      return;
    }
    enterDestination("view");
  }, [controller, enterDestination, router]);

  const cancelDiscard = useCallback(() => {
    setConfirmDiscardVisible(false);
    exitDestinationRef.current = "back";
    controller.cancelDiscard();
  }, [controller]);

  const saveStatus: EditorSaveStatus = session.save;
  const saveFailure = session.saveFailure;

  return (
    <div dir={direction} lang={locale} className="space-y-3" data-testid="shipping-authoring-screen">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex min-w-0 items-center gap-2">
          <button
            type="button"
            className="inline-flex min-h-9 items-center gap-1 rounded-lg border border-border px-2.5 text-xs font-bold text-gray-700 disabled:opacity-50"
            data-testid="shipping-authoring-back"
            disabled={busy}
            onClick={goBack}
          >
            {messages.back}
          </button>
          <h1 className="truncate text-base font-black text-gray-900">{messages.pageTitle}</h1>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          {mode === "view" ? (
            <button
              type="button"
              className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
              data-testid="shipping-authoring-edit"
              onClick={() => setMode("edit")}
            >
              {messages.editAction}
            </button>
          ) : (
            <button
              type="button"
              className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700 disabled:opacity-50"
              data-testid="shipping-authoring-cancel-edit"
              disabled={busy}
              onClick={cancelEdit}
            >
              {messages.cancelEdit}
            </button>
          )}
          <button
            type="button"
            className="inline-flex min-h-9 items-center rounded-lg bg-primary px-3 text-xs font-bold text-primary-foreground disabled:opacity-50"
            data-testid="shipping-authoring-apply"
            disabled={!canApply}
            onClick={() => void apply()}
          >
            {session.save === "saving" ? messages.applying : messages.apply}
          </button>
        </div>
      </header>

      <p
        role="note"
        className="rounded-xl border border-border bg-surface px-3 py-2 text-xs font-semibold text-gray-600"
        data-testid="shipping-authoring-mode-banner"
      >
        {mode === "view" ? messages.viewBanner : messages.editBanner}
        {session.unsaved ? ` · ${messages.unsavedNote}` : ""}
      </p>

      {notice ? (
        <p role="status" className="rounded-xl border border-emerald-200 bg-emerald-50 px-3 py-2 text-xs font-semibold text-emerald-800">
          {notice}
        </p>
      ) : null}

      {session.load === "loading" ? <p className="text-xs text-gray-500">{messages.entryLoading}</p> : null}
      {session.load === "denied" ? (
        <p role="alert" className="text-xs font-semibold text-danger">{messages.loadDenied}</p>
      ) : null}
      {session.load === "notfound" ? (
        <p role="alert" className="text-xs font-semibold text-danger">{messages.loadNotFound}</p>
      ) : null}
      {session.load === "error" ? (
        <div role="alert" className="flex items-center gap-2 text-xs font-semibold text-danger">
          <span>{messages.loadError}</span>
          <button
            type="button"
            className="inline-flex min-h-8 items-center rounded-lg border border-border px-2 text-gray-700"
            data-testid="shipping-authoring-retry"
            onClick={() => controller.retryLoad()}
          >
            {messages.retry}
          </button>
        </div>
      ) : null}

      {saveFailure && saveStatus !== "clean" ? (
        <div role="alert" className="rounded-xl border border-danger/40 bg-red-50 px-3 py-2 text-xs font-semibold text-danger">
          {failureMessages(saveFailure, messages).map((text) => (
            <p key={text} data-testid={`shipping-authoring-save-${saveFailure.kind}`}>
              {text}
            </p>
          ))}
          {errorGroups(saveFailure).length > 0 ? (
            <p className="mt-1 font-normal text-gray-700">
              {authoringFormat(messages.validationSummary, { count: errorGroups(saveFailure).length })}{" "}
              {errorGroups(saveFailure)
                .map((group) => fieldLabel(group.property, messages))
                .join(" · ")}
            </p>
          ) : null}
          {saveStatus === "outcomeUnknown" || session.commitUncertain ? (
            <button
              type="button"
              className="mt-2 inline-flex min-h-8 items-center rounded-lg border border-border px-2 text-gray-800"
              data-testid="shipping-authoring-reconcile"
              onClick={() => void controller.reconcile()}
            >
              {messages.reconcileAction}
            </button>
          ) : null}
        </div>
      ) : null}
      {session.reconcileFailure ? (
        <p role="alert" className="text-xs font-semibold text-danger">{messages.reconcileFailed}</p>
      ) : null}

      {document && index ? (
        <>
          <div role="tablist" aria-label={messages.pageTitle} className="flex flex-wrap gap-1 border-b border-border">
            {TABS.map((id) => (
              <button
                key={id}
                type="button"
                role="tab"
                aria-selected={tab === id}
                data-testid={`shipping-authoring-tab-${id}`}
                className={
                  tab === id
                    ? "-mb-px min-h-10 border-b-2 border-primary px-3 text-sm font-black text-gray-900"
                    : "-mb-px min-h-10 border-b-2 border-transparent px-3 text-sm font-bold text-gray-500"
                }
                onClick={() => setTab(id)}
              >
                {id === "general" ? messages.tabGeneral : messages.tabRegions}
              </button>
            ))}
          </div>

          {blockedByFacts || unresolved.length > 0 ? (
            <div
              role="alert"
              className="rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-900"
              data-testid="shipping-authoring-blocked"
            >
              <p className="font-black">{messages.blockedTitle}</p>
              {blockedByFacts ? <p className="mt-1 font-normal">{messages.blockedUnavailable}</p> : null}
              {unresolved.length > 0 ? (
                <>
                  <p className="mt-1 font-normal">{messages.blockedUnresolved}</p>
                  <ul className="mt-1 list-disc ps-4 font-normal">
                    {unresolved.map((item) => (
                      <li key={`${item.groupId ?? "root"}:${item.area.kind}:${item.area.id}`}>
                        {messages[AREA_KIND_KEYS[item.area.kind]]} —{" "}
                        {index.labelOf(item.area) ?? messages.unavailableName}
                      </li>
                    ))}
                  </ul>
                </>
              ) : null}
            </div>
          ) : null}

          {intent && !hasNoIssues(intent.issues) ? (
            <div
              role="alert"
              className="rounded-xl border border-danger/40 bg-red-50 px-3 py-2 text-xs font-semibold text-danger"
              data-testid="shipping-authoring-local-issues"
            >
              <p className="font-black">{messages.applyBlockedLocal}</p>
              <ul className="mt-1 list-disc space-y-0.5 ps-4 font-normal">
                {Array.from(
                  new Set([
                    ...intent.issues.general.map((issue) =>
                      issue.code === "nameRequired" ? messages.nameRequired : messages.settingsIncomplete,
                    ),
                    ...intent.issues.rates.map((issue) => rowIssueMessage(issue.code, messages)),
                    ...intent.issues.week.map((issue) => weekIssueMessage(issue.code, messages)),
                  ]),
                ).map((text) => (
                  <li key={text}>{text}</li>
                ))}
              </ul>
            </div>
          ) : null}

          <div role="tabpanel" hidden={tab !== "general"} data-testid="shipping-authoring-panel-general">
            {form ? (
              <AuthoringGeneralTab
                document={document}
                form={form}
                mode={readOnly ? "view" : "edit"}
                locale={locale}
                messages={messages}
                contentLanguageId={contentLanguageId}
                onContentLanguageChange={setContentLanguageId}
                typeChoices={types}
                typeChoicesState={typesState}
                nameIssueLanguageIds={nameIssueLanguageIds}
                settingsIncomplete={settingsIncomplete}
                onName={(languageId, value) => pushEdits(withName(form, languageId, value))}
                onDescription={(languageId, value) => pushEdits(withDescription(form, languageId, value))}
                onFreightPaymentMode={(mode2: FreightPaymentMode) => pushEdits(withFreightPaymentMode(form, mode2))}
                onCalculationMode={(mode2: AuthoringCalculationMode) => pushEdits(withCalculationMode(form, mode2))}
                onShippingType={(shippingTypeId) => pushEdits(withShippingType(form, shippingTypeId))}
                scheduleSlot={
                  week ? (
                    <AuthoringWeekEditor
                      schedule={document.weeklySchedule}
                      week={week}
                      issues={weekProblemIssues}
                      editable={!readOnly}
                      locale={locale}
                      messages={messages}
                      onPendingTimeZone={(value) => changeWeek(withPendingTimeZone(week, value))}
                      onConfigure={() => changeWeek(configureWeek(week))}
                      onTimeZone={(value) => changeWeek(withTimeZone(week, value))}
                      onDayWorking={(weekday, isWorking) => changeWeek(withDayWorking(week, weekday, isWorking))}
                      onDayTime={(weekday, bound, value) => changeWeek(withDayTime(week, weekday, bound, value))}
                      onReset={() => changeWeek(resetWeek(document.weeklySchedule))}
                    />
                  ) : null
                }
              />
            ) : null}
          </div>
          <div role="tabpanel" hidden={tab !== "regions"} data-testid="shipping-authoring-panel-regions">
            <AuthoringRegionsPanel
              document={document}
              index={index}
              locale={locale}
              messages={messages}
              contentLanguageId={contentLanguageId}
              rates={rates}
              issues={rateRowIssues}
              editable={!readOnly}
              onAddRow={addRow}
              onRemoveRow={removeRow}
              onChangeField={changeRow}
            />
          </div>
        </>
      ) : null}

      <Dialog open={confirmDiscardVisible} onClose={cancelDiscard} title={messages.discardTitle} showCloseButton>
        <div className="space-y-3 text-sm text-gray-700">
          <p>{messages.discardText}</p>
          <div className="flex flex-wrap justify-end gap-2">
            <button
              type="button"
              className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
              data-testid="shipping-authoring-discard-cancel"
              onClick={cancelDiscard}
            >
              {messages.discardCancel}
            </button>
            <button
              type="button"
              className="inline-flex min-h-9 items-center rounded-lg bg-danger px-3 text-xs font-bold text-white"
              data-testid="shipping-authoring-discard-confirm"
              onClick={confirmDiscard}
            >
              {messages.discardConfirm}
            </button>
          </div>
        </div>
      </Dialog>
    </div>
  );
}
