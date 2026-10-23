"use client";

/**
 * Rendered GENERAL editor of ONE existing shipping draft (the first connected visible slice).
 *
 * Boundary: this component only renders state and dispatches the feature controller. Transport,
 * decoding, ancestry and the ONE atomic PUT stay in `api`/`model`/`session`; region/rate/week editing,
 * whole-method creation, activation and unsupported cutoff/carrier/funding controls are NOT here.
 */
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Dialog } from "../../../design-system";
import { loadAuthoringTypeChoices, type AuthoringTypeChoice } from "../api/authoring-lookup-api.ts";
import type { EditorTransportFailure, EditorPropertyError } from "../api/editor-document-api.ts";
import { authoringMessages, authoringFormat, type AuthoringLocale, type AuthoringMessages } from "../messages/authoring-messages.ts";
import type { AuthoringCalculationMode, FreightPaymentMode } from "../model/editor-document.ts";
import { indexDestinationFacts, unresolvedSelections } from "../model/editor-destination-ancestry.ts";
import {
  buildEditorEdits,
  createEditorForm,
  withCalculationMode,
  withDescription,
  withFreightPaymentMode,
  withName,
  withShippingType,
  type EditorFormState,
} from "../model/editor-form.ts";
import { hasUnsavedWork, type EditorSaveStatus } from "../session/editor-session.ts";
import { useEditorController } from "./use-editor-controller.ts";
import { AuthoringGeneralTab } from "./authoring-general-tab.tsx";
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
  const [contentLanguageId, setContentLanguageId] = useState<string>("");
  const [types, setTypes] = useState<readonly AuthoringTypeChoice[]>([]);
  const [typesState, setTypesState] = useState<"loading" | "ok" | "error">("loading");
  const [notice, setNotice] = useState<string | null>(null);
  const [confirmDiscardVisible, setConfirmDiscardVisible] = useState(false);
  const applyRef = useRef(false);

  // The form starts from the CONFIRMED baseline, never from an optimistic guess.
  useEffect(() => {
    if (!document) return;
    setForm((current) => current ?? createEditorForm(document));
    setContentLanguageId((current) => {
      if (current.length > 0) return current;
      const defaultLanguage = document.presentation.contentLanguages.find(
        (language) => language.languageId.toLowerCase() === document.presentation.defaultContentLanguageId.toLowerCase(),
      );
      return (defaultLanguage ?? document.presentation.contentLanguages[0])?.languageId ?? "";
    });
  }, [document]);

  // A confirmed save re-establishes the baseline: the form is rebuilt from the server truth.
  useEffect(() => {
    if (!document || session.save !== "clean" || session.unsaved) return;
    setForm(createEditorForm(document));
  }, [document, session.save, session.unsaved]);

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
  const built = useMemo(
    () => (document && form ? buildEditorEdits(document, form) : null),
    [document, form],
  );

  const pushEdits = useCallback(
    (nextForm: EditorFormState) => {
      setForm(nextForm);
      if (!document) return;
      controller.edit(buildEditorEdits(document, nextForm).edits);
    },
    [controller, document],
  );

  const readOnly = mode === "view" || session.save === "saving" || session.reconcile === "pending";
  const blockedByFacts = document && index ? !index.available : false;
  const unresolved = document && index ? unresolvedSelections(document, index) : [];
  const formIssues = built?.issues ?? [];
  const nameIssueLanguageIds = formIssues.flatMap((issue) =>
    issue.code === "nameRequired" ? [issue.languageId] : [],
  );
  const settingsIncomplete = formIssues.some((issue) => issue.code === "settingsIncomplete");
  const hasIntent = built !== null && Object.keys(built.edits).length > 0;
  const canApply =
    !readOnly &&
    document !== null &&
    hasIntent &&
    formIssues.length === 0 &&
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
      if (outcome.status === "sent") {
        const revision = controller.getState().savedRevision;
        setNotice(authoringFormat(messages.saved, { revision: revision ?? "" }));
        setMode("view");
      }
    } finally {
      applyRef.current = false;
    }
  }, [controller, index, messages.saved]);

  const goBack = useCallback(() => {
    if (controller.requestClose()) router.push("/admin/shipping-services");
  }, [controller, router]);

  const confirmDiscard = useCallback(() => {
    setConfirmDiscardVisible(false);
    controller.confirmDiscard();
    router.push("/admin/shipping-services");
  }, [controller, router]);

  const cancelDiscard = useCallback(() => {
    setConfirmDiscardVisible(false);
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
            className="inline-flex min-h-9 items-center gap-1 rounded-lg border border-border px-2.5 text-xs font-bold text-gray-700"
            data-testid="shipping-authoring-back"
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
              className="inline-flex min-h-9 items-center rounded-lg border border-border px-3 text-xs font-bold text-gray-700"
              data-testid="shipping-authoring-cancel-edit"
              onClick={() => {
                if (document) setForm(createEditorForm(document));
                controller.edit({});
                setMode("view");
              }}
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
