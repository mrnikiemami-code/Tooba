"use client";

/**
 * PRIMARY admin list of shipping methods.
 *
 * The rows are the owner's EXISTING authoring drafts — the real authority for configured methods — so a
 * draft whose binding is `post` with NO option appears as itself and is never split or fuzzy-matched onto
 * a legacy `post:standard` row. View/Edit navigate straight to that draft's editor (`?mode=view|edit`);
 * there is no intermediate legacy dialog and the stable row identity is the server draft id.
 *
 * The previous catalog/settings screen stays reachable through the explicit secondary link only: a failed
 * read of this list is reported as itself and NEVER falls back to the old screen.
 */
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Eye, Pencil } from "lucide-react";
import {
  AppDataGrid,
  ErrorState,
  createClientGridQueryAdapter,
  useLegacyAdminGridDirectProps,
} from "../../../design-system";
import type { GridColumnDef, GridServerQuery, SavedViewStore } from "../../../design-system/data-grid";
import { AppGridRowActionsCell, type AppGridRowAction } from "../../../design-system/app-data-grid/app-grid-row-actions";
import {
  authoringOwnerKey,
  duplicateBindingDraftIds,
  loadAuthoringContext,
  loadAuthoringMethodRows,
  loadAuthoringOwner,
  methodBinding,
  storedMethodName,
  type AuthoringContextRead,
  type AuthoringMethodRow,
  type AuthoringOwnerRead,
} from "../api/authoring-method-list-api.ts";
import { authoringFormat, authoringMessages, type AuthoringLocale } from "../messages/authoring-messages.ts";

const GRID_ID = "admin-shipping-services-authoring";

/** One rendered row: the stored draft plus the display facts derived from the authoritative context. */
interface MethodListRow extends AuthoringMethodRow {
  readonly displayName: string | null;
  readonly duplicateBinding: boolean;
}

type ListState = "loading" | "ok" | "denied" | "error" | "untrusted";

export function ShippingAuthoringMethodListScreen({
  locale,
  savedViewStore,
}: {
  locale: AuthoringLocale;
  /** App-owned saved-view adapter (the feature never imports the app layer). */
  savedViewStore?: SavedViewStore;
}) {
  const router = useRouter();
  const messages = authoringMessages(locale);
  const direction = locale === "fa" ? "rtl" : "ltr";
  const [state, setState] = useState<ListState>("loading");
  const [rows, setRows] = useState<readonly AuthoringMethodRow[]>([]);
  const [context, setContext] = useState<AuthoringContextRead | null>(null);
  const [languageUnavailable, setLanguageUnavailable] = useState(false);
  /** The server-authorized owner of this scope; `null` until it is read (never a local guess). */
  const [owner, setOwner] = useState<AuthoringOwnerRead | null>(null);
  const [ownerUnavailable, setOwnerUnavailable] = useState(false);
  /**
   * Scope generation: every read belongs to one owner scope. A response from a superseded scope is
   * dropped (never merged), and rows are cleared while the new scope loads, so an old owner's methods
   * can never appear under the new owner.
   */
  const generation = useRef(0);

  const load = useCallback(async () => {
    const mine = ++generation.current;
    setState("loading");
    setRows([]);
    setContext(null);
    const [drafts, languages, ownerRead] = await Promise.all([
      loadAuthoringMethodRows(),
      loadAuthoringContext(),
      loadAuthoringOwner(),
    ]);
    if (generation.current !== mine) return; // a superseded scope never wins
    if (ownerRead.state === "ok") {
      setOwner(ownerRead.value);
      setOwnerUnavailable(false);
    } else {
      setOwner(null);
      setOwnerUnavailable(true);
    }
    if (drafts.state !== "ok") {
      const kind = drafts.failure.kind;
      setState(kind === "denied" ? "denied" : kind === "untrusted" ? "untrusted" : "error");
      return;
    }
    setRows(drafts.value);
    // A context that cannot be read is NOT replaced by a guessed language: the name is withheld instead.
    if (languages.state === "ok") {
      setContext(languages.value);
      setLanguageUnavailable(false);
    } else {
      setContext(null);
      setLanguageUnavailable(true);
    }
    setState("ok");
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const duplicates = useMemo(() => duplicateBindingDraftIds(rows), [rows]);
  const viewRows = useMemo<readonly MethodListRow[]>(
    () => rows.map((row) => ({
      ...row,
      displayName: context === null ? null : storedMethodName(row, context.defaultContentLanguageId),
      duplicateBinding: duplicates.has(row.draftId),
    })),
    [context, duplicates, rows],
  );

  const openDraft = useCallback(
    (row: MethodListRow, mode: "view" | "edit") => {
      router.push(`/admin/shipping-services/authoring/${row.draftId}?mode=${mode}`);
    },
    [router],
  );

  const rowActions = useMemo<AppGridRowAction<MethodListRow>[]>(
    () => [
      {
        id: "view",
        label: messages.listViewAction,
        icon: Eye,
        testId: (row) => `shipping-authoring-open-view-${row.draftId}`,
        onClick: (row) => openDraft(row, "view"),
      },
      {
        id: "edit",
        label: messages.listEditAction,
        icon: Pencil,
        testId: (row) => `shipping-authoring-open-edit-${row.draftId}`,
        onClick: (row) => openDraft(row, "edit"),
      },
    ],
    [messages.listEditAction, messages.listViewAction, openDraft],
  );

  const columns = useMemo<GridColumnDef<MethodListRow>[]>(
    () => [
      {
        id: "name",
        header: messages.listColumnName,
        accessor: (row) => (row.displayName !== null ? row.displayName : ""),
        cell: (row) => (
          <span className="flex flex-col">
            <span className="font-bold text-gray-900">
              {row.displayName !== null ? row.displayName : messages.listNoName}
            </span>
            {row.displayName !== null ? null : (
              <span className="mt-0.5 text-[10px] font-semibold text-amber-700" data-testid={`shipping-authoring-no-name-${row.draftId}`}>
                {messages.listNoNameNote}
              </span>
            )}
            {row.duplicateBinding ? (
              <span className="mt-0.5 text-[10px] font-semibold text-red-700" data-testid={`shipping-authoring-duplicate-${row.draftId}`}>
                {messages.listDuplicateBinding}
              </span>
            ) : null}
          </span>
        ),
        width: 280,
        minWidth: 190,
        flex: 1.5,
      },
      {
        id: "binding",
        header: messages.listColumnBinding,
        accessor: (row) => methodBinding(row),
        cell: (row) => (
          <span dir="ltr" className="font-mono text-xs text-gray-800" data-testid={`shipping-authoring-binding-${row.draftId}`}>
            {methodBinding(row)}
          </span>
        ),
        width: 190,
        minWidth: 130,
      },
      {
        id: "revision",
        header: messages.listColumnRevision,
        accessor: (row) => row.revision,
        cell: (row) => (
          <span className="text-xs font-semibold text-gray-700">
            {authoringFormat(messages.listRevisionValue, { revision: row.revision })}
          </span>
        ),
        width: 130,
        minWidth: 110,
      },
      {
        id: "areas",
        header: messages.listColumnAreas,
        accessor: (row) => row.areaCount,
        cell: (row) => (
          <span className="text-xs font-semibold text-gray-700">
            {authoringFormat(messages.listAreaCount, { count: row.areaCount })}
          </span>
        ),
        width: 120,
        minWidth: 100,
      },
      {
        id: "actions",
        header: messages.listColumnActions,
        accessor: () => "",
        exportable: false,
        sortable: false,
        width: 150,
        minWidth: 130,
        maxWidth: 200,
        cell: (row) => <AppGridRowActionsCell row={row} actions={rowActions} compact />,
      },
    ],
    [messages, rowActions],
  );

  const queryAdapter = useCallback(
    async (query: GridServerQuery) => createClientGridQueryAdapter(viewRows, columns)(query),
    [columns, viewRows],
  );
  /**
   * Grid identity is SCOPE-keyed: changing the authorized owner (when the server ever offers more than
   * one) starts a separate grid/saved-view/export namespace instead of inheriting the previous scope's.
   */
  const scopeKey = owner === null ? "unresolved" : authoringOwnerKey(owner);
  const gridProps = useLegacyAdminGridDirectProps({
    gridId: `${GRID_ID}:${scopeKey}`,
    columns,
    queryAdapter,
    savedViewStore,
    locale,
    direction,
  });

  if (state === "denied") {
    return <ErrorState title={messages.listTitle} detail={messages.listDenied} />;
  }
  if (state === "untrusted") {
    return <ErrorState title={messages.listTitle} detail={messages.listUntrusted} onRetry={() => void load()} retryLabel={messages.listRetry} />;
  }
  if (state === "error") {
    return <ErrorState title={messages.listTitle} detail={messages.listError} onRetry={() => void load()} retryLabel={messages.listRetry} />;
  }

  return (
    <main className="space-y-4" dir={direction} lang={locale} data-testid="admin-shipping-authoring-list-screen">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <h1 className="text-xl font-black text-gray-900">{messages.listTitle}</h1>
          <p className="mt-1 text-sm text-gray-500">{messages.listIntro}</p>
        </div>
        <div className="flex flex-wrap items-end gap-3">
          {/* The owner is a REAL server fact, not a local choice: the options come from the authorized
              owner read, so a SingleStore installation honestly shows its one lawful owner only. */}
          <label className="text-xs">
            <span className="block font-bold text-gray-900">{messages.listOwnerLabel}</span>
            <select
              className="mt-1 min-h-9 rounded-lg border border-border bg-surface px-2 text-xs disabled:opacity-70"
              value={scopeKey}
              disabled={owner === null}
              data-testid="admin-shipping-authoring-owner"
              onChange={() => {
                /* One lawful owner: the server exposes no alternate authorized scope to switch to. */
              }}
            >
              {owner === null ? <option value="unresolved">—</option> : null}
              {owner !== null ? (
                <option value={scopeKey}>
                  {owner.ownerKind === "Seller"
                    ? authoringFormat(messages.listOwnerSellerIdentity, { id: owner.sellerPartyId ?? "" })
                    : owner.ownerKind === "Marketplace"
                      ? messages.listOwnerMarketplace
                      : messages.listOwnerStore}
                </option>
              ) : null}
            </select>
          </label>
          <a
            href="?legacy=1"
            className="inline-flex min-h-9 items-center rounded-xl border border-border px-3 text-xs font-bold text-gray-700"
            data-testid="admin-shipping-authoring-legacy-link"
          >
            {messages.listLegacyLink}
          </a>
        </div>
      </div>

      {owner !== null ? (
        <p className="text-[11px] text-gray-500" data-testid="admin-shipping-authoring-owner-note">
          {messages.listOwnerSingleNote}
        </p>
      ) : null}
      {ownerUnavailable ? (
        <p role="alert" className="rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-900" data-testid="admin-shipping-authoring-owner-unavailable">
          {messages.listOwnerUnavailable}
        </p>
      ) : null}

      {languageUnavailable ? (
        <p role="alert" className="rounded-xl border border-amber-200 bg-amber-50 px-3 py-2 text-xs font-semibold text-amber-900" data-testid="admin-shipping-authoring-language-unavailable">
          {messages.listLanguageUnavailable}
        </p>
      ) : null}

      {state === "loading" ? (
        <p className="text-sm text-gray-500">{messages.listLoading}</p>
      ) : viewRows.length === 0 ? (
        <p className="rounded-2xl border border-border bg-surface-elevated px-4 py-6 text-sm text-gray-600" data-testid="admin-shipping-authoring-list-empty">
          {messages.listEmpty}
        </p>
      ) : (
        <section className="overflow-hidden rounded-2xl border border-border bg-surface-elevated shadow-sm">
          <div className="overflow-x-auto p-2 md:p-4">
            <AppDataGrid<MethodListRow> {...gridProps} />
          </div>
        </section>
      )}
    </main>
  );
}
