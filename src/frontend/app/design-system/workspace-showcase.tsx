"use client";

import { useMemo, useState } from "react";
import {
  Button,
  Card,
  Cluster,
  DataGrid,
  Stack,
  WorkspaceShell,
  enWorkspaceMessages,
  faWorkspaceMessages,
} from "../../design-system";
import { demoBulkActions, demoEntityLookup, demoOpsColumns, demoQueryAdapter, demoSavedViewStore } from "../../design-system/data-grid/demo-data";
import { clearSectionDirty, markSectionDirty, shouldBlockNavigation } from "../../design-system/workspace";
import { useTheme } from "../../design-system";

const sections = [
  { id: "core", label: "Core" },
  { id: "media", label: "Media" },
  { id: "pricing", label: "Pricing" },
  { id: "related", label: "Related grid" },
];

/**
 * ویترین مصنوعی الگوهای Workspace. صفحهٔ محصول/سفارش واقعی نیست.
 */
export function WorkspaceShowcase() {
  const { theme, setDirection } = useTheme();
  const messages = theme.direction === "rtl" ? faWorkspaceMessages : enWorkspaceMessages;
  const [sectionId, setSectionId] = useState("core");
  const [dirty, setDirty] = useState<Set<string>>(new Set());
  const [pending, setPending] = useState<string | null>(null);
  const [mode, setMode] = useState<"view" | "edit">("view");
  const [readOnly, setReadOnly] = useState(false);
  const [conflict, setConflict] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [narrow, setNarrow] = useState(false);
  const [validation, setValidation] = useState<string | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);

  function requestSection(next: string) {
    if (shouldBlockNavigation(dirty, sectionId, next)) {
      setPending(next);
      return;
    }
    setSectionId(next);
  }

  const actions = useMemo(
    () => [
      { id: "save", label: messages.save, kind: "primary" as const, permission: readOnly ? ("denied" as const) : ("allowed" as const) },
      { id: "edit", label: messages.edit, kind: "secondary" as const, permission: "allowed" as const },
      { id: "archive", label: "Archive", kind: "destructive" as const, permission: "allowed" as const, needsConfirmation: true },
      { id: "more", label: messages.moreActions, kind: "overflow" as const, permission: "allowed" as const },
    ],
    [messages, readOnly],
  );

  return (
    <Card>
      <Stack>
        <h2 className="ds-title">Workspace patterns</h2>
        <p className="ds-caption text-muted">دادهٔ مصنوعی؛ Workspace دامنه پیاده نشده است.</p>
        <Cluster>
          <Button type="button" tone="secondary" onClick={() => setDirection(theme.direction === "rtl" ? "ltr" : "rtl")}>
            {theme.direction}
          </Button>
          <Button type="button" tone="secondary" onClick={() => setNarrow((value) => !value)}>
            {narrow ? "desktop" : "mobile"}
          </Button>
          <Button type="button" tone="secondary" onClick={() => setMode((value) => (value === "view" ? "edit" : "view"))}>
            {mode}
          </Button>
          <Button type="button" tone="secondary" onClick={() => setDirty((current) => markSectionDirty(current, sectionId))}>
            dirty
          </Button>
          <Button type="button" tone="secondary" onClick={() => setValidation("Name is required")}>
            validation
          </Button>
          <Button type="button" tone="secondary" onClick={() => setConflict(messages.conflict)}>
            conflict
          </Button>
          <Button type="button" tone="secondary" onClick={() => setReadOnly((value) => !value)}>
            {readOnly ? "editable" : "read-only"}
          </Button>
          <Button type="button" tone="secondary" onClick={() => setError(error ? null : "Load failed")}>
            error
          </Button>
        </Cluster>
        <WorkspaceShell
          title="Synthetic operations record"
          subtitle="Not a Product workspace"
          breadcrumbs={["Ops", "Synthetic", "WS-1001"]}
          statusItems={[
            { id: "pub", label: "draft", tone: "warning" },
            { id: "stock", label: "available", tone: "success" },
          ]}
          sections={sections}
          activeSectionId={sectionId}
          onSectionChange={requestSection}
          actions={actions}
          onAction={(id) => {
            if (id === "edit") setMode("edit");
            if (id === "save") {
              setDirty((current) => clearSectionDirty(current, sectionId));
              setMode("view");
              setValidation(null);
            }
            if (id === "archive") setConfirmOpen(true);
          }}
          messages={messages}
          readOnly={readOnly}
          error={error}
          onRetry={() => setError(null)}
          conflict={conflict}
          onReloadConflict={() => setConflict(null)}
          dirtySections={dirty}
          pendingSectionId={pending}
          onStay={() => setPending(null)}
          onDiscardNavigation={() => {
            if (pending) {
              setDirty((current) => clearSectionDirty(current, sectionId));
              setSectionId(pending);
              setPending(null);
            }
          }}
          forceNarrow={narrow}
          summary={<p>Summary: demo record · mode {mode}</p>}
          inspector={<p>{messages.details}: related seller north</p>}
          activity={[{ id: "a1", at: "2026-08-24", actor: "ops-user", summary: "Opened synthetic record" }]}
          audit={[{ id: "u1", at: "2026-08-24", actor: "system", event: "status.draft" }]}
        >
          {sectionId !== "related" ? (
            <div>
              <p>
                Section {sectionId} · {mode}
              </p>
              {validation ? <p className="text-danger">{validation}</p> : null}
              {mode === "edit" ? <p>Editable field (synthetic)</p> : <p>Read view (synthetic)</p>}
            </div>
          ) : (
            <DataGrid
              columns={demoOpsColumns}
              queryAdapter={demoQueryAdapter}
              bulkActions={demoBulkActions}
              savedViewStore={demoSavedViewStore}
              entityLookup={demoEntityLookup}
              onServerExport={async () => undefined}
            />
          )}
        </WorkspaceShell>
        {confirmOpen ? (
          <div className="rounded-ds border border-danger/40 p-3">
            <p>{messages.confirmDestructive}</p>
            <Button type="button" tone="secondary" onClick={() => setConfirmOpen(false)}>
              {messages.cancel}
            </Button>
          </div>
        ) : null}
      </Stack>
    </Card>
  );
}
