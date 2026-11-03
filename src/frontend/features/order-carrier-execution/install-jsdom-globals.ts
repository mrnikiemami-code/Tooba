/**
 * Side-effect: install jsdom globals BEFORE react-dom is imported.
 * React caches InputEvent support at module load; without this it falls into IE attachEvent polyfill.
 */
import Module from "node:module";
import { JSDOM } from "jsdom";

// Allow CJS require of .css (AppDataGrid) under mount tests without parsing stylesheets.
const cjs = Module as unknown as { _extensions?: Record<string, (m: NodeModule, filename: string) => void> };
if (cjs._extensions && typeof cjs._extensions[".css"] !== "function") {
  cjs._extensions[".css"] = (module) => {
    (module as { exports: unknown }).exports = {};
  };
}

const g = globalThis as typeof globalThis & Record<string, unknown>;

const already = (g as { __tb249r6DomInstalled?: boolean }).__tb249r6DomInstalled;
if (!already) {
  const dom = new JSDOM("<!doctype html><html><body></body></html>", {
    url: "http://127.0.0.1:3000/",
    pretendToBeVisual: true,
  });
  const { window } = dom;
  const define = (key: string, value: unknown) => {
    try {
      Object.defineProperty(g, key, { value, configurable: true, writable: true });
    } catch {
      try {
        g[key] = value;
      } catch {
        /* non-configurable getters */
      }
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
  // jsdom lacks <dialog> showModal/close — polyfill for Dialog mount tests.
  const dialogProto = (window as unknown as { HTMLDialogElement?: { prototype: HTMLDialogElement } })
    .HTMLDialogElement?.prototype;
  if (dialogProto) {
    if (typeof dialogProto.showModal !== "function") {
      dialogProto.showModal = function showModal(this: HTMLDialogElement) {
        this.setAttribute("open", "");
      };
    }
    if (typeof dialogProto.close !== "function") {
      dialogProto.close = function close(this: HTMLDialogElement) {
        this.removeAttribute("open");
      };
    }
  } else {
    class HTMLDialogElementPolyfill extends window.HTMLElement {
      showModal() {
        this.setAttribute("open", "");
      }
      close() {
        this.removeAttribute("open");
      }
    }
    define("HTMLDialogElement", HTMLDialogElementPolyfill);
    try {
      window.customElements.define("dialog", HTMLDialogElementPolyfill as unknown as CustomElementConstructor);
    } catch {
      /* already defined */
    }
  }
  define("Node", window.Node);
  define("MutationObserver", window.MutationObserver);
  define("Event", window.Event);
  define("MouseEvent", window.MouseEvent);
  define("InputEvent", window.InputEvent);
  define("KeyboardEvent", window.KeyboardEvent);
  define("getComputedStyle", window.getComputedStyle.bind(window));
  define("requestAnimationFrame", (cb: FrameRequestCallback) => window.setTimeout(() => cb(Date.now()), 0));
  define("cancelAnimationFrame", (id: number) => window.clearTimeout(id));
  define("IS_REACT_ACT_ENVIRONMENT", true);
  define("__tb249r6Dom", dom);
  define("__tb249r6DomInstalled", true);
  Object.defineProperty(window, "IS_REACT_ACT_ENVIRONMENT", { value: true, configurable: true });

  // Soft IE shims in case react-dom still takes the attachEvent branch.
  const proto = window.HTMLElement.prototype as HTMLElement & {
    attachEvent?: (name: string, fn: EventListener) => void;
    detachEvent?: (name: string, fn: EventListener) => void;
  };
  if (typeof proto.attachEvent !== "function") {
    proto.attachEvent = function attachEvent(name: string, fn: EventListener) {
      this.addEventListener(name.replace(/^on/i, "").toLowerCase(), fn);
    };
  }
  if (typeof proto.detachEvent !== "function") {
    proto.detachEvent = function detachEvent(name: string, fn: EventListener) {
      this.removeEventListener(name.replace(/^on/i, "").toLowerCase(), fn);
    };
  }
} else {
  const win = g.window as Window | undefined;
  if (win?.sessionStorage && g.sessionStorage !== win.sessionStorage) {
    try {
      Object.defineProperty(g, "sessionStorage", {
        value: win.sessionStorage,
        configurable: true,
        writable: true,
      });
    } catch {
      g.sessionStorage = win.sessionStorage;
    }
  }
}

export {};
