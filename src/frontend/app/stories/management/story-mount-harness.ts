/**
 * Neutral Story mount harness — jsdom + CSS stub for AppDataGrid.
 * Story-owned; do not import other product feature test helpers.
 */
import Module from "node:module";
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-expect-error jsdom ships without types in this package set
import { JSDOM } from "jsdom";
import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import type { ReactElement } from "react";

const cjs = Module as unknown as { _extensions?: Record<string, (m: NodeModule, filename: string) => void> };
if (cjs._extensions && typeof cjs._extensions[".css"] !== "function") {
  cjs._extensions[".css"] = (module) => {
    (module as { exports: unknown }).exports = {};
  };
}

const g = globalThis as typeof globalThis & Record<string, unknown>;
if (!(g as { __storyMountDom?: boolean }).__storyMountDom) {
  const dom = new JSDOM("<!doctype html><html><body></body></html>", {
    url: "http://127.0.0.1:3000/",
    pretendToBeVisual: true,
  });
  const { window } = dom;
  const define = (key: string, value: unknown) => {
    try {
      Object.defineProperty(g, key, { value, configurable: true, writable: true });
    } catch {
      g[key] = value;
    }
  };
  define("window", window);
  define("self", window);
  define("document", window.document);
  define("navigator", window.navigator);
  define("sessionStorage", window.sessionStorage);
  define("localStorage", window.localStorage);
  define("HTMLElement", window.HTMLElement);
  define("HTMLInputElement", window.HTMLInputElement);
  define("HTMLButtonElement", window.HTMLButtonElement);
  define("Node", window.Node);
  define("MutationObserver", window.MutationObserver);
  define("Event", window.Event);
  define("MouseEvent", window.MouseEvent);
  define("InputEvent", window.InputEvent);
  (g as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;
  (g as { __storyMountDom?: boolean }).__storyMountDom = true;
}

export function flush(ms = 0): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function mount(node: ReactElement): Promise<{
  host: HTMLElement;
  root: Root;
  unmount: () => Promise<void>;
}> {
  const host = document.createElement("div");
  document.body.appendChild(host);
  const root = createRoot(host);
  await act(async () => {
    root.render(node);
    await flush(20);
  });
  return {
    host,
    root,
    unmount: async () => {
      await act(async () => {
        root.unmount();
        await flush(0);
      });
      host.remove();
    },
  };
}

export function q(host: ParentNode, selector: string): Element | null {
  return host.querySelector(selector);
}
