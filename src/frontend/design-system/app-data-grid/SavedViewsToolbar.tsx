"use client";

import { useEffect, useRef, useState } from "react";
import { cn } from "../cn";
import { Button, Checkbox, Input } from "../primitives/core";
import type { SavedGridView } from "../data-grid/types";

type SavedViewsToolbarProps = {
  locale: "fa" | "en";
  messages: {
    savedViews: string;
    saveView: string;
    deleteView: string;
    renameView: string;
    restoreDefault: string;
    defaultViewName: string;
    apply: string;
    cancel: string;
    setDefault: string;
    updateView: string;
    systemDefault: string;
  };
  savedViews: SavedGridView[];
  activeViewId: string | null;
  defaultViewId: string | null;
  onApply: (view: SavedGridView) => void;
  onCreate: (name: string, setAsDefault: boolean) => void;
  onUpdate: (viewId: string) => void;
  onRename: (viewId: string, name: string) => void;
  onDelete: (viewId: string) => void;
  onSetDefault: (viewId: string) => void;
  onRestoreSystemDefault: () => void;
};

/** نوار نمای ذخیره‌شده — pill/menu مطابق mockup تأییدشده. */
export function SavedViewsToolbar({
  locale,
  messages,
  savedViews,
  activeViewId,
  defaultViewId,
  onApply,
  onCreate,
  onUpdate,
  onRename,
  onDelete,
  onSetDefault,
  onRestoreSystemDefault,
}: SavedViewsToolbarProps) {
  const [saveOpen, setSaveOpen] = useState(false);
  const [saveName, setSaveName] = useState("");
  const [saveAsDefault, setSaveAsDefault] = useState(false);
  const [menuViewId, setMenuViewId] = useState<string | null>(null);
  const [renamingId, setRenamingId] = useState<string | null>(null);
  const [renameValue, setRenameValue] = useState("");
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!menuViewId) return;
    const onDoc = (event: MouseEvent) => {
      if (!menuRef.current?.contains(event.target as Node)) setMenuViewId(null);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [menuViewId]);

  function submitCreate() {
    const trimmed = saveName.trim() || messages.defaultViewName;
    onCreate(trimmed, saveAsDefault);
    setSaveName("");
    setSaveAsDefault(false);
    setSaveOpen(false);
  }

  return (
    <div className="flex flex-wrap items-center gap-2" data-testid="app-grid-saved-views">
      <span className="text-xs font-medium text-muted">{messages.savedViews}</span>
      <button
        type="button"
        className={cn(
          "inline-flex min-h-9 items-center rounded-full px-3 text-sm font-medium transition-colors",
          activeViewId === null
            ? "bg-primary text-primary-foreground shadow-sm"
            : "border border-border bg-surface hover:bg-secondary",
        )}
        onClick={() => onRestoreSystemDefault()}
        data-testid="app-grid-restore-default"
      >
        {messages.systemDefault}
      </button>
      {savedViews.map((view) => {
        const active = activeViewId === view.id;
        const isDefault = defaultViewId === view.id;
        return (
          <div key={view.id} className="relative inline-flex items-center">
            {renamingId === view.id ? (
              <div className="inline-flex items-center gap-1">
                <Input
                  aria-label={messages.renameView}
                  value={renameValue}
                  onChange={(event) => setRenameValue(event.target.value)}
                  className="min-w-[8rem] rounded-full px-3 py-1 text-sm"
                />
                <Button type="button" tone="primary" className="min-h-9 rounded-full px-3 text-sm" onClick={() => onRename(view.id, renameValue)}>
                  {messages.apply}
                </Button>
                <button type="button" className="inline-flex size-8 items-center justify-center rounded-full text-muted hover:bg-secondary" onClick={() => setRenamingId(null)}>
                  ×
                </button>
              </div>
            ) : (
              <>
                <button
                  type="button"
                  className={cn(
                    "inline-flex min-h-9 items-center gap-1 rounded-full px-3 text-sm font-medium transition-colors",
                    active ? "bg-primary text-primary-foreground shadow-sm" : "border border-border bg-surface hover:bg-secondary",
                  )}
                  aria-pressed={active}
                  onClick={() => onApply(view)}
                >
                  {view.name || messages.defaultViewName}
                  {isDefault ? <span className="text-[10px] opacity-80">★</span> : null}
                </button>
                <button
                  type="button"
                  className="inline-flex size-8 items-center justify-center rounded-full text-muted hover:bg-secondary"
                  aria-label={`${messages.savedViews}: ${view.name}`}
                  onClick={() => setMenuViewId(menuViewId === view.id ? null : view.id)}
                >
                  ⋮
                </button>
                {menuViewId === view.id ? (
                  <div
                    ref={menuRef}
                    className="absolute start-0 top-full z-[var(--z-overlay)] mt-1 min-w-[11rem] rounded-ds border border-border bg-surface-elevated p-1 shadow-ds"
                  >
                    <button
                      type="button"
                      className="block w-full rounded-ds px-3 py-2 text-start text-sm hover:bg-secondary"
                      onClick={() => {
                        setRenamingId(view.id);
                        setRenameValue(view.name);
                        setMenuViewId(null);
                      }}
                    >
                      {messages.renameView}
                    </button>
                    <button
                      type="button"
                      className="block w-full rounded-ds px-3 py-2 text-start text-sm hover:bg-secondary"
                      onClick={() => {
                        onUpdate(view.id);
                        setMenuViewId(null);
                      }}
                    >
                      {messages.updateView}
                    </button>
                    <button
                      type="button"
                      className="block w-full rounded-ds px-3 py-2 text-start text-sm hover:bg-secondary"
                      onClick={() => {
                        onSetDefault(view.id);
                        setMenuViewId(null);
                      }}
                    >
                      {messages.setDefault}
                    </button>
                    <button
                      type="button"
                      className="block w-full rounded-ds px-3 py-2 text-start text-sm text-danger hover:bg-secondary"
                      onClick={() => {
                        onDelete(view.id);
                        setMenuViewId(null);
                      }}
                    >
                      {messages.deleteView}
                    </button>
                  </div>
                ) : null}
              </>
            )}
          </div>
        );
      })}
      <div className="relative">
        <button
          type="button"
          className="inline-flex min-h-9 items-center gap-1 rounded-full border border-border bg-surface px-3 text-sm font-medium hover:bg-secondary"
          onClick={() => setSaveOpen((open) => !open)}
          data-testid="app-grid-save-view"
        >
          <span aria-hidden>🔖</span>
          {messages.saveView}
        </button>
        {saveOpen ? (
          <div className="absolute start-0 top-full z-[var(--z-overlay)] mt-1 w-[min(16rem,90vw)] rounded-ds border border-border bg-surface-elevated p-3 shadow-ds">
            <label className="grid gap-1 text-sm">
              {locale === "fa" ? "نام نما" : "View name"}
              <Input value={saveName} onChange={(event) => setSaveName(event.target.value)} placeholder={messages.defaultViewName} />
            </label>
            <Checkbox
              className="mt-2"
              label={messages.setDefault}
              checked={saveAsDefault}
              onChange={(event) => setSaveAsDefault(event.target.checked)}
            />
            <div className="mt-3 flex flex-wrap gap-2">
              <Button type="button" tone="primary" className="min-h-9 text-sm" onClick={submitCreate}>
                {messages.saveView}
              </Button>
              <Button type="button" tone="secondary" className="min-h-9 text-sm" onClick={() => setSaveOpen(false)}>
                {messages.cancel}
              </Button>
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}
