/**
 * Minimal jsdom + react-dom mount harness for node:test / tsx.
 * Import install-jsdom-globals BEFORE this module (or rely on the side-effect import below).
 */
import "./install-jsdom-globals.ts";
import React, {
  act,
  createElement,
  type ReactElement,
  type ReactNode,
} from "react";
import { createRoot, type Root } from "react-dom/client";

export type MountedTree = {
  container: HTMLElement;
  root: Root;
  unmount: () => Promise<void>;
  rerender: (node: ReactNode) => Promise<void>;
};

let mountSeq = 0;

export function ensureDom(): void {
  // Globals installed by install-jsdom-globals; keep React global for classic JSX fallbacks.
  const g = globalThis as typeof globalThis & Record<string, unknown>;
  if (!g.React) {
    try {
      Object.defineProperty(g, "React", { value: React, configurable: true, writable: true });
    } catch {
      g.React = React;
    }
  }
  (g as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true;
}

export async function mount(node: ReactNode): Promise<MountedTree> {
  ensureDom();
  mountSeq += 1;
  const container = document.createElement("div");
  container.id = `tb249r6-root-${mountSeq}`;
  document.body.appendChild(container);
  const root = createRoot(container);
  await act(async () => {
    root.render(node as ReactElement);
  });
  return {
    container,
    root,
    async unmount() {
      await act(async () => {
        root.unmount();
      });
      container.remove();
    },
    async rerender(next: ReactNode) {
      await act(async () => {
        root.render(next as ReactElement);
      });
    },
  };
}

export async function flush(): Promise<void> {
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
}

export async function waitFor(
  predicate: () => boolean,
  options: { timeoutMs?: number; intervalMs?: number } = {},
): Promise<void> {
  const timeoutMs = options.timeoutMs ?? 2000;
  const intervalMs = options.intervalMs ?? 10;
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    if (predicate()) return;
    await act(async () => {
      await new Promise((r) => setTimeout(r, intervalMs));
    });
  }
  throw new Error(`waitFor timed out after ${timeoutMs}ms`);
}

export function q(container: ParentNode, selector: string): HTMLElement {
  const el = container.querySelector(selector);
  if (!el) throw new Error(`missing ${selector}`);
  return el as HTMLElement;
}

export function qq(container: ParentNode, selector: string): HTMLElement[] {
  return Array.from(container.querySelectorAll(selector)) as HTMLElement[];
}

export async function click(el: Element): Promise<void> {
  await act(async () => {
    (el as HTMLElement).dispatchEvent(
      new MouseEvent("click", { bubbles: true, cancelable: true, view: window as unknown as Window }),
    );
  });
  await flush();
}

/** Fire multiple clicks inside one act turn (no await between) to exercise inFlight locks. */
export async function clickBurst(el: Element, times: number): Promise<void> {
  await act(async () => {
    for (let i = 0; i < times; i += 1) {
      (el as HTMLElement).dispatchEvent(
        new MouseEvent("click", { bubbles: true, cancelable: true, view: window as unknown as Window }),
      );
    }
  });
  await flush();
}

export async function setInputValue(el: Element, value: string): Promise<void> {
  const input = el as HTMLInputElement;
  await act(async () => {
    input.focus();
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value")?.set;
    if (!setter) throw new Error("HTMLInputElement value setter missing");
    setter.call(input, value);
    input.dispatchEvent(
      new InputEvent("input", { bubbles: true, cancelable: true, data: value, inputType: "insertText" }),
    );
    input.dispatchEvent(new Event("change", { bubbles: true }));
  });
  await flush();
}

export function el(type: string, props?: Record<string, unknown> | null, ...children: ReactNode[]) {
  return createElement(type, props ?? null, ...children);
}
