/**
 * TB-261 — ONE transient notification for a completed authoring action.
 *
 * The user rejected the permanent «تغییرات ذخیره شد» / «منتشر شد» strip: it stayed on screen after the
 * action, it was replayed by every re-render/reload, and it duplicated nothing useful. The latest explicit
 * user direction is the EXISTING common Toast mechanism (`react-toastify`, configured once in
 * `app/providers.tsx` with `autoClose: 3000`), fired ONCE per completed action and never derived from state.
 *
 * Proven here on the shipped panel: one confirmed publish = exactly one `toast.success`, no success strip in
 * the rendered tree, and a fresh mount (the reload path) replays nothing.
 *
 * Run with:
 *   npx tsx --tsconfig tsconfig.mount-tests.json --test features/admin-shipping-authoring/tb261-notification.mount.test.ts
 */
import "../order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import { createElement } from "react";
import test from "node:test";
import { toast } from "react-toastify";
import { AuthoringPublicationPanel } from "./ui/authoring-publication-panel.tsx";
import { authoringMessages } from "./messages/authoring-messages.ts";
import { click, mount, q, qq, flush, waitFor } from "../order-carrier-execution/test-dom-harness.ts";

const DRAFT = "01a1beed-d7df-7000-bfc8-0185ea61afe3";
const SNAPSHOT = "01a1c000-0000-7000-0000-000000000001";

const MESSAGES = authoringMessages("fa");

function view(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    draftId: DRAFT,
    currentDraftRevision: 3,
    serviceCode: "post",
    optionCode: null,
    state: "Absent",
    selectionVersion: 0,
    selectedSnapshotId: null,
    selectedDraftId: null,
    selectedSourceRevision: null,
    selectedFormatVersion: null,
    selectedMatchesCurrentDraftRevision: false,
    ...overrides,
  };
}

type Call = { method: string; url: string };

/** One real POST per confirmed publish; the read path stays a plain GET so a reload is the loading path. */
function installFetch(): { calls: Call[]; restore: () => void } {
  const original = globalThis.fetch;
  const calls: Call[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
    calls.push({ method, url });
    if (method === "GET") {
      return new Response(JSON.stringify(view()), { status: 200, headers: { "Content-Type": "application/json" } });
    }
    return new Response(
      JSON.stringify(
        view({
          state: "Selected",
          selectionVersion: 1,
          selectedSnapshotId: SNAPSHOT,
          selectedDraftId: DRAFT,
          selectedSourceRevision: 3,
          selectedFormatVersion: 1,
          selectedMatchesCurrentDraftRevision: true,
        }),
      ),
      { status: 200, headers: { "Content-Type": "application/json" } },
    );
  }) as typeof fetch;
  return {
    calls,
    restore: () => {
      globalThis.fetch = original;
    },
  };
}

/** The shared toast object, observed instead of a second notification system being installed. */
function spyOnSuccess(): { messages: string[]; restore: () => void } {
  const target = toast as unknown as { success: (message: unknown, options?: unknown) => unknown };
  const original = target.success;
  const messages: string[] = [];
  target.success = (message: unknown) => {
    messages.push(String(message));
    return 1;
  };
  return {
    messages,
    restore: () => {
      target.success = original;
    },
  };
}

function panel() {
  return createElement(AuthoringPublicationPanel, {
    draftId: DRAFT,
    currentRevision: 3,
    mode: "view",
    busy: false,
    messages: MESSAGES,
  });
}

test("TB-261 mounted: ONE confirmed publish shows ONE transient toast and no persistent success strip", async () => {
  const fetchState = installFetch();
  const spy = spyOnSuccess();
  try {
    const mounted = await mount(panel());
    await waitFor(() => qq(mounted.container, "[data-testid='shipping-authoring-publication-publish']").length === 1);
    await flush();

    const publish = q(mounted.container, "[data-testid='shipping-authoring-publication-publish']");
    await click(publish);
    await waitFor(
      () => qq(mounted.container, "[data-testid='shipping-authoring-publication-confirm-accept']").length === 1,
    );
    await click(q(mounted.container, "[data-testid='shipping-authoring-publication-confirm-accept']"));
    await waitFor(() => fetchState.calls.filter((call) => call.method === "POST").length === 1);
    await flush();

    assert.equal(spy.messages.length, 1, "a completed publish reports exactly once");
    assert.match(spy.messages[0]!, /منتشر/);

    // The outcome is transient: nothing of it is rendered as a permanent strip/role=status in the panel.
    assert.ok(!mounted.container.textContent?.includes(spy.messages[0]!));
    assert.equal(qq(mounted.container, "[role='status']").length, 0);
    assert.equal(qq(mounted.container, "[data-testid='shipping-authoring-publication-success']").length, 0);

    // A re-render of the SAME mounted tree never replays the notification…
    await mounted.rerender(panel());
    await flush();
    assert.equal(spy.messages.length, 1, "a re-render must not replay a completed action");

    // …and the reload path (a fresh mount, GET only) replays nothing either.
    await mounted.unmount();
    const reloaded = await mount(panel());
    await waitFor(() => qq(reloaded.container, "[data-testid='shipping-authoring-publication-publish']").length === 1);
    await flush();
    assert.equal(spy.messages.length, 1, "a GET/reload must never replay the toast");
    assert.ok(!reloaded.container.textContent?.includes(spy.messages[0]!));
    await reloaded.unmount();
  } finally {
    spy.restore();
    fetchState.restore();
  }
});
