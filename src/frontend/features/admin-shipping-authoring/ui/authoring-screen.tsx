"use client";

/**
 * Rendered editor of ONE existing shipping draft: General, the ONE method-level weekly schedule and the
 * rate rows (with their AREA-level delivery range) under each EXISTING region group.
 *
 * Boundary: this component only renders state and dispatches the feature controller. Transport, decoding,
 * ancestry, validation and the ONE atomic PUT stay in `api`/`model`/`session`; group topology/geography
 * editing, whole-method creation, activation and unsupported cutoff/carrier/funding controls are NOT here.
 */
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Dialog } from "../../../design-system";
import { loadAuthoringTypeChoices, type AuthoringTypeChoice } from "../api/authoring-lookup-api.ts";
import type { EditorTransportFailure } from "../api/editor-document-api.ts";
import { authoringMessages, authoringFormat, type AuthoringLocale, type AuthoringMessages } from "../messages/authoring-messages.ts";
import type {
  AuthoringCalculationMode,
  EditorDocument,
  FreightPaymentMode,
  SelectedAreaRead,
} from "../model/editor-document.ts";
import { indexWithTrails } from "../model/editor-destination-ancestry.ts";
import { useStoredTrails } from "./use-stored-trails.ts";
import { buildAuthoringIntent, hasNoIssues, intentHasEdits } from "../model/authoring-intent.ts";
import { resetAuthoringDrafts, seedAuthoringDrafts, type AuthoringDrafts } from "../model/authoring-drafts.ts";
import { composeAncestryResolver, areaIdentity, unresolvedDraftSelections } from "../model/destination-choice.ts";
import type { ResolvedAreaAncestry } from "../model/editor-edits.ts";
import {
  withAddedGroup,
  withGroupAreaAdded,
  withGroupAreaRemoved,
  withGroupTitle,
  withGroupTitleSlots,
  withMovedGroup,
  withRemovedGroup,
  type GroupIssue,
  type GroupsFormState,
} from "../model/editor-groups.ts";
import {
  withCalculationMode,
  withDescription,
  withFreightPaymentMode,
  withName,
  withShippingType,
  type EditorFormState,
} from "../model/editor-form.ts";
import {
  withConfirmedRow,
  withRemovedRow,
  type RateRowDraft,
  type RatesFormState,
} from "../model/editor-rates.ts";
import { areaRangeEdit, createAreaRangeDrafts, inheritedAreaRange, type AreaRangeDrafts } from "../model/editor-area-range.ts";
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
import { AuthoringWeekEditor } from "./authoring-week-editor.tsx";
import { AuthoringGroupsEditor } from "./authoring-groups-editor.tsx";
import { AuthoringLocalIssuesPanel, AuthoringSaveFailurePanel } from "./authoring-problem-view.tsx";
import { AuthoringPublicationPanel } from "./authoring-publication-panel.tsx";
import {
  isRowScoped,
  rateRowKey,
  serverErrorCodes,
  serverFieldIssues,
  serverIssueSelector,
  targetTab,
  type ServerFieldIssue,
} from "./server-field-error.ts";

const TABS = ["general", "regions"] as const;
type ScreenTab = (typeof TABS)[number];

const AREA_KIND_KEYS = {
  Country: "areaCountry",
  AdministrativeArea: "areaAdministrativeArea",
  City: "areaCity",
  ServiceArea: "areaServiceArea",
} as const;

/** Field labels for server validation properties; raw codes are never shown as prose. */
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
  const [groups, setGroups] = useState<GroupsFormState>([]);
  const [rates, setRates] = useState<RatesFormState>({});
  /** The operator's OWN typed shared delivery range per area: the ONE form state of that control. */
  const [areaRanges, setAreaRanges] = useState<AreaRangeDrafts>({});
  const [week, setWeek] = useState<WeekFormState | null>(null);
  /** Verified ancestry of terminals the operator picked in THIS session (reset with the drafts). */
  const [pickedFacts, setPickedFacts] = useState<ReadonlyMap<string, ResolvedAreaAncestry>>(new Map());
  /** Localized labels of those same picks, so a fresh chip never shows "name unavailable". */
  const [pickedLabels, setPickedLabels] = useState<ReadonlyMap<string, string>>(new Map());
  /** Verified walking path per freshly picked area (display breadcrumb of that session's pick). */
  const [pickedTrails, setPickedTrails] = useState<ReadonlyMap<string, readonly string[]>>(new Map());
  /** Breadcrumb of ALREADY STORED selections, from the existing Geography read (labels only). */
  const storedTrails = useStoredTrails(document);
  const [contentLanguageId, setContentLanguageId] = useState<string>("");
  const [types, setTypes] = useState<readonly AuthoringTypeChoice[]>([]);
  const [typesState, setTypesState] = useState<"loading" | "ok" | "error">("loading");
  const [notice, setNotice] = useState<string | null>(null);
  const [confirmDiscardVisible, setConfirmDiscardVisible] = useState(false);
  const applyRef = useRef(false);
  const exitDestinationRef = useRef<AuthoringExitDestination>("back");

  const formSeed = state.formSeed;
  const groupCounter = useRef(0);
  /**
   * Handler-boundary guard: a view-mode (or saving/reconciling) tree must not change local rows even
   * if a control were activated programmatically — presentation alone is not the protection.
   */
  const mutationBlocked = useRef(true);
  mutationBlocked.current = mode !== "edit" || session.save === "saving" || session.reconcile === "pending";
  const blockedNow = useCallback(() => mutationBlocked.current, []);
  const activeLanguageIds = useMemo(
    () => (document ? document.presentation.contentLanguages.map((language) => language.languageId) : []),
    [document],
  );

  /**
   * ONE cohesive reseed of EVERY local draft (General, groups, rows, AREA RANGES, week) from a confirmed
   * baseline; staged picker facts are cleared with it. A render/invalid edit/reconcile never calls it.
   */
  const applyLocalDrafts = useCallback((drafts: AuthoringDrafts, source: EditorDocument) => {
    setForm(drafts.form);
    setGroups(drafts.groups);
    setRates(drafts.rates);
    setAreaRanges(createAreaRangeDrafts(source));
    setWeek(drafts.week);
    setPickedFacts(new Map());
    setPickedLabels(new Map());
  }, []);

  useEffect(() => {
    if (!document) return;
    const languageIds = document.presentation.contentLanguages.map((language) => language.languageId);
    applyLocalDrafts(seedAuthoringDrafts(document, languageIds), document);
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
  const index = useMemo(
    () => (document ? indexWithTrails(document, pickedLabels, storedTrails, pickedTrails) : null),
    [document, pickedLabels, storedTrails, pickedTrails],
  );
  /** Retained server-proven facts + this session's verified picks, as ONE resolver. */
  const resolver = useMemo(
    () => (index ? composeAncestryResolver(index.resolver, pickedFacts) : null),
    [index, pickedFacts],
  );
  const intent = useMemo(
    () => (document && form && week ? buildAuthoringIntent(document, form, rates, week, groups, areaRanges) : null),
    [areaRanges, document, form, rates, week, groups],
  );

  /** ONE dispatch: sets the local drafts and hands the controller a SINGLE composed patch. */
  const commit = useCallback(
    (
      nextForm: EditorFormState,
      nextGroups: GroupsFormState,
      nextRates: RatesFormState,
      nextWeek: WeekFormState,
      nextAreaRanges: AreaRangeDrafts = areaRanges,
    ) => {
      if (blockedNow()) return;
      setForm(nextForm);
      setGroups(nextGroups);
      setRates(nextRates);
      setAreaRanges(nextAreaRanges);
      setWeek(nextWeek);
      if (!document) return;
      const composed = buildAuthoringIntent(document, nextForm, nextRates, nextWeek, nextGroups, nextAreaRanges);
      controller.edit(composed.edits, composed.localDirty);
    },
    [areaRanges, blockedNow, controller, document],
  );

  const pushEdits = useCallback(
    (nextForm: EditorFormState) => {
      if (blockedNow() || !week) return;
      commit(nextForm, groups, rates, week);
    },
    [commit, groups, rates, week],
  );

  /** ONE confirmed row from its modal: a NEW row inherits this area's own range; an edit replaces its stable key. */
  const commitRow = useCallback(
    (groupKey: string, row: RateRowDraft) => {
      if (blockedNow() || !form || !week) return;
      const inherited =
        row.rateRowId === null && row.minDeliveryDays === "" && row.maxDeliveryDays === ""
          ? inheritedAreaRange(rates[groupKey] ?? [], areaRanges[groupKey] ?? null)
          : null;
      const confirmed: RateRowDraft =
        inherited === null ? row : { ...row, minDeliveryDays: inherited.min, maxDeliveryDays: inherited.max };
      commit(form, groups, withConfirmedRow(rates, groupKey, confirmed), week);
    },
    [areaRanges, blockedNow, commit, form, groups, rates, week],
  );
  /** ONE entry of one area's shared range: the typed text IS form state; a lawful pair is written to its rows. */
  const changeAreaRange = useCallback(
    (groupKey: string, min: string, max: string) => {
      if (blockedNow() || !form || !week) return;
      const next = areaRangeEdit(rates, areaRanges, groupKey, min, max);
      commit(form, groups, next.rates, week, next.drafts);
    },
    [areaRanges, blockedNow, commit, form, groups, rates, week],
  );

  const removeRow = useCallback(
    (groupKey: string, rowKey: string) => {
      if (blockedNow() || !form || !week) return;
      commit(form, groups, withRemovedRow(rates, groupKey, rowKey), week);
    },
    [commit, form, groups, rates, week],
  );

  const addGroup = useCallback(() => {
    if (blockedNow() || !form || !week) return;
    groupCounter.current += 1;
    const key = `group-new-${groupCounter.current}`;
    const nextGroups = withGroupTitleSlots(withAddedGroup(groups, key), key, activeLanguageIds);
    commit(form, nextGroups, { ...rates, [key]: [] }, week);
  }, [activeLanguageIds, commit, form, groups, rates, week]);

  const removeGroup = useCallback(
    (groupKey: string) => {
      if (blockedNow() || !form || !week) return;
      const nextRates: Record<string, readonly RateRowDraft[]> = { ...rates };
      delete nextRates[groupKey];
      commit(form, withRemovedGroup(groups, groupKey), nextRates, week);
    },
    [commit, form, groups, rates, week],
  );

  const moveGroup = useCallback(
    (groupKey: string, delta: -1 | 1) => {
      if (blockedNow() || !form || !week) return;
      commit(form, withMovedGroup(groups, groupKey, delta), rates, week);
    },
    [commit, form, groups, rates, week],
  );

  const changeGroupTitle = useCallback(
    (groupKey: string, languageId: string, value: string) => {
      if (blockedNow() || !form || !week) return;
      commit(form, withGroupTitle(groups, groupKey, languageId, value), rates, week);
    },
    [commit, form, groups, rates, week],
  );

  const removeGroupArea = useCallback(
    (groupKey: string, area: SelectedAreaRead) => {
      if (blockedNow() || !form || !week) return;
      commit(form, withGroupAreaRemoved(groups, groupKey, area), rates, week);
    },
    [commit, form, groups, rates, week],
  );

  const addGroupArea = useCallback(
    (
      groupKey: string,
      area: SelectedAreaRead,
      ancestry: ResolvedAreaAncestry,
      label: string,
      trail: readonly string[],
    ) => {
      if (blockedNow() || !form || !week) return;
      const nextGroups = withGroupAreaAdded(groups, groupKey, area);
      if (nextGroups === groups) return; // an exact duplicate is rejected locally, never double-sent
      setPickedFacts((previous) => new Map(previous).set(areaIdentity(area), ancestry));
      if (label.trim().length > 0) {
        setPickedLabels((previous) => new Map(previous).set(areaIdentity(area), label.trim()));
      }
      // The REAL walked path of this pick, so the chip shows the verified breadcrumb (display only).
      if (trail.length > 0) {
        setPickedTrails((previous) => new Map(previous).set(areaIdentity(area), [...trail]));
      }
      commit(form, nextGroups, rates, week);
    },
    [commit, form, groups, rates, week],
  );

  const changeWeek = useCallback(
    (nextWeek: WeekFormState) => {
      if (!form) return;
      commit(form, groups, rates, nextWeek);
    },
    [commit, form, groups, rates],
  );

  const busy = session.save === "saving" || session.reconcile === "pending";
  const readOnly = mode === "view" || busy;
  const blockedByFacts = document && index ? !index.available : false;
  /** Only the PROPOSED tree blocks: a removed unresolved destination/group no longer does. */
  const unresolved = resolver ? unresolvedDraftSelections(groups, resolver) : [];
  const formIssues = intent?.issues.general ?? [];
  const groupProblems = intent?.issues.groups ?? [];
  const rateRowIssues = intent?.issues.rates ?? [];
  const weekProblemIssues = intent?.issues.week ?? [];  const nameIssueLanguageIds = formIssues.flatMap((issue) =>
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
    if (applyRef.current || !resolver) return;
    applyRef.current = true;
    setNotice(null);
    try {
      const outcome = await controller.apply(resolver);
      if (outcome.status !== "sent") return;
      setNotice(authoringFormat(messages.saved, { revision: controller.getState().savedRevision ?? "" }));
      setMode("view");
    } finally {
      applyRef.current = false;
    }
  }, [controller, messages.saved, resolver]);

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
        setGroups([]);
        setRates({});
        setAreaRanges({});
        setWeek(null);
        setPickedFacts(new Map());
        setPickedLabels(new Map());
      } else {
        controller.cancelDiscard();
        // ONE cohesive reseed: General AND group collection AND rate rows AND week come from the SAME
        // confirmed baseline, so discarded groups/rows/week can never stay or be resurrected.
        if (document) {
          applyLocalDrafts(resetAuthoringDrafts(document, activeLanguageIds), document);
        }
        controller.edit({});
      }
      setMode("view");
      setNotice(null);
    },
    [activeLanguageIds, controller, document, router, session.commitUncertain],
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
  /** Localized, actionable placement of the server's rejection on real fields of this screen. */
  const serverIssues = useMemo(
    () => (saveFailure ? serverFieldIssues(saveFailure, messages) : []),
    [saveFailure, messages],
  );
  const serverCodes = useMemo(() => (saveFailure ? serverErrorCodes(saveFailure) : []), [saveFailure]);
  /** Reasons that belong next to their own field (not in the summary prose). */
  const fieldServerErrors = useMemo(() => {
    const map: Partial<Record<"name" | "description" | "settings" | "type" | "weekTimeZone" | "weekDays", string>> = {};
    for (const issue of serverIssues) {
      if (issue.target === "unknown" || issue.target === "regions" || issue.target === "rates") continue;
      if (map[issue.target] === undefined) map[issue.target] = issue.reason;
    }
    return map;
  }, [serverIssues]);
  /** Rate-row reasons keyed by composite group+row ownership of the SUBMITTED document. */
  const rateServerErrors = useMemo(() => {
    const map = new Map<string, string>();
    for (const issue of serverIssues) {
      if (!isRowScoped(issue)) continue;
      const key = rateRowKey(issue.groupIndex!, issue.rowIndex!);
      if (!map.has(key)) map.set(key, issue.reason);
    }
    return map;
  }, [serverIssues]);

  /**
   * Reveals the owning section and moves focus to the exact GROUP -> ROW -> input of the rejection.
   * Opening a closed optional-conditions disclosure is presentation only: it never edits a value,
   * never marks the form dirty and never touches the revision.
   */
  const revealServerIssue = useCallback((issue: ServerFieldIssue) => {
    setTab(targetTab(issue.target));
    const selector = serverIssueSelector(issue);
    if (selector === null) return;
    window.requestAnimationFrame(() => {
      const element = globalThis.document.querySelector<HTMLElement>(selector);
      if (!element) return;
      // The target may be inside a collapsed disclosure (optional conditions): open ONLY the
      // disclosures that contain it, so the input is focusable and still inside its own group.
      for (let parent = element.parentElement; parent !== null; parent = parent.parentElement) {
        if (parent instanceof HTMLDetailsElement && !parent.open) parent.open = true;
      }
      element.scrollIntoView({ block: "center" });
      element.focus({ preventScroll: true });
    });
  }, []);

  // A new rejection immediately shows the failing section and focuses the first rejected field.
  const revealedFailure = useRef<EditorTransportFailure | null>(null);
  useEffect(() => {
    if (!saveFailure || serverIssues.length === 0) return;
    if (revealedFailure.current === saveFailure) return;
    revealedFailure.current = saveFailure;
    revealServerIssue(serverIssues[0]);
  }, [saveFailure, serverIssues, revealServerIssue]);

  return (
    <div dir={direction} lang={locale} className="mx-auto w-full max-w-6xl space-y-3" data-testid="shipping-authoring-screen">
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

      {/* Mode notice: the view-mode instruction, or the unapplied-work warning alone. Edit mode has no
          standing prose of its own — the Apply button state and this warning carry the truth. */}
      {mode === "view" || session.unsaved ? (
        <p
          role="note"
          className="rounded-xl border border-border bg-surface px-3 py-2 text-xs font-semibold text-gray-600"
          data-testid="shipping-authoring-mode-banner"
        >
          {mode === "view" ? messages.viewBanner : messages.unsavedNote}
          {mode === "view" && session.unsaved ? ` · ${messages.unsavedNote}` : ""}
        </p>
      ) : null}

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

      <AuthoringSaveFailurePanel
        failure={saveFailure}
        status={saveStatus}
        serverIssues={serverIssues}
        serverCodes={serverCodes}
        messages={messages}
        commitUncertain={session.commitUncertain}
        onReveal={revealServerIssue}
        onReconcile={() => void controller.reconcile()}
      />
      {session.reconcileFailure ? (
        <p role="alert" className="text-xs font-semibold text-danger">{messages.reconcileFailed}</p>
      ) : null}

      {/* Publication appears only for a LOADED draft: its own status read is the authority, and it never
          reads/writes the editor document. */}
      {session.load === "loaded" ? (
        <AuthoringPublicationPanel
          draftId={draftId}
          currentRevision={document?.revision ?? null}
          mode={mode}
          busy={busy}
          messages={messages}
          onPublished={setNotice}
        />
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
                      <li key={`${item.groupKey}:${item.area.kind}:${item.area.id}`}>
                        {messages[AREA_KIND_KEYS[item.area.kind]]} —{" "}
                        {index.labelOf(item.area) ?? messages.unavailableName}
                      </li>
                    ))}
                  </ul>
                </>
              ) : null}
            </div>
          ) : null}

          {intent ? <AuthoringLocalIssuesPanel issues={intent.issues} groupProblems={groupProblems} messages={messages} /> : null}

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
                serverErrors={fieldServerErrors}
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
                      editable={!readOnly && !busy}
                      locale={locale}
                      messages={messages}
                      serverErrors={{ timeZone: fieldServerErrors.weekTimeZone, days: fieldServerErrors.weekDays }}
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
            <AuthoringGroupsEditor
              document={document}
              index={index}
              locale={locale}
              messages={messages}
              contentLanguageId={contentLanguageId}
              activeLanguageIds={activeLanguageIds}
              groups={groups}
              rates={rates}
              rateIssues={rateRowIssues}
              serverRateErrors={rateServerErrors}
              areaRanges={areaRanges}
              areaRangeIssues={intent?.issues.areaRanges}
              unresolved={unresolved}
              editable={!readOnly && !busy}
              onAddGroup={addGroup}
              onRemoveGroup={removeGroup}
              onMoveGroup={moveGroup}
              onChangeTitle={changeGroupTitle}
              onRemoveArea={removeGroupArea}
              onAddArea={addGroupArea}
              onCommitRow={commitRow}
              onRemoveRow={removeRow}
              onChangeAreaRange={changeAreaRange}
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
