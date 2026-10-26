// TB-225-R2 mounted gate — task-owned headless Chrome (fresh profile, dedicated port 9333).
// Only the target CREATED here is used; keyboard input is real CDP input; no user tab, no Electron.
import fs from "node:fs";
import path from "node:path";

const CDP = "http://127.0.0.1:9333";
const URL_TARGET =
  "http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=edit";
const OUT = "D:/Users/User/source/repos/SarvNewVer/docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2";

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

const version = JSON.parse(await (await fetch(`${CDP}/json/version`)).text());
const ws = new WebSocket(version.webSocketDebuggerUrl);
await new Promise((res) => ws.addEventListener("open", res, { once: true }));
let id = 0;
const pending = new Map();
ws.addEventListener("message", (event) => {
  const message = JSON.parse(event.data);
  if (message.id && pending.has(message.id)) {
    pending.get(message.id)(message.result ?? { error: message.error });
    pending.delete(message.id);
  }
});
const send = (method, params = {}, sessionId) =>
  new Promise((resolve) => {
    const n = ++id;
    pending.set(n, resolve);
    ws.send(JSON.stringify({ id: n, method, params, ...(sessionId ? { sessionId } : {}) }));
  });

const targetId = (await send("Target.createTarget", { url: "about:blank" })).targetId;
const sid = (await send("Target.attachToTarget", { targetId, flatten: true })).sessionId;
await send("Page.enable", {}, sid);
await send("Runtime.enable", {}, sid);
await send("Page.navigate", { url: URL_TARGET }, sid);

const evaluate = async (expression) =>
  (await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true }, sid)).result?.value;

const receipts = [];
const record = (name, value) => {
  receipts.push({ name, ...value });
  console.log(name, JSON.stringify(value));
};

const boxOf = (testId) =>
  evaluate(`(() => {
    const el = document.querySelector('[data-testid="${testId}"]');
    if (!el) return null;
    el.scrollIntoView({ block: "center" });
    const r = el.getBoundingClientRect();
    return { x: Math.round(r.x + r.width / 2), y: Math.round(r.y + r.height / 2), w: Math.round(r.width), h: Math.round(r.height) };
  })()`);

const realClick = async (testId) => {
  const box = await boxOf(testId);
  if (box === null || box.w === 0) return { testId, clicked: false, box };
  await sleep(250);
  const settled = await boxOf(testId);
  for (const type of ["mousePressed", "mouseReleased"]) {
    await send("Input.dispatchMouseEvent", { type, x: settled.x, y: settled.y, button: "left", clickCount: 1 }, sid);
  }
  await sleep(400);
  return { testId, clicked: true, box: settled };
};

const keyChar = async (character) => {
  await send("Input.dispatchKeyEvent", { type: "keyDown", text: character, unmodifiedText: character, key: character }, sid);
  await send("Input.dispatchKeyEvent", { type: "keyUp", key: character }, sid);
  await sleep(70);
};

const pressKey = async (key, code, codePoint) => {
  await send("Input.dispatchKeyEvent", { type: "rawKeyDown", key, code, windowsVirtualKeyCode: codePoint, nativeVirtualKeyCode: codePoint }, sid);
  await send("Input.dispatchKeyEvent", { type: "keyUp", key, code, windowsVirtualKeyCode: codePoint }, sid);
  await sleep(120);
};

const readFreight = () =>
  evaluate(`(() => {
    const f = document.querySelector('[data-testid="shipping-authoring-rate-freight"]');
    if (!f) return null;
    return { value: f.value, invalid: f.getAttribute("aria-invalid"), caret: f.selectionStart, focused: document.activeElement === f };
  })()`);

const selectAllFreight = () =>
  evaluate(`(() => {
    const f = document.querySelector('[data-testid="shipping-authoring-rate-freight"]');
    f.focus();
    f.setSelectionRange(0, f.value.length);
    return document.activeElement === f;
  })()`);

for (let i = 0; i < 60; i += 1) {
  await sleep(400);
  if (await evaluate(`!!document.querySelector('[data-testid="shipping-authoring-tab-regions"]')`)) break;
}

record("target", { targetId, browser: version.Browser, profile: "task-owned fresh profile", port: 9333, url: URL_TARGET });
record("tab", await realClick("shipping-authoring-tab-regions"));
record("baseline", await evaluate(`(() => ({
  row: document.querySelector('[data-testid="shipping-authoring-rate-row"]')?.innerText.replace(/\\s+/g, " ").trim().slice(0, 140) ?? null,
  applyDisabled: !!document.querySelector('[data-testid="shipping-authoring-apply"]')?.disabled,
}))()`));

record("open-modal", await realClick("shipping-authoring-rate-row-edit"));
record("modal", { freight: (await readFreight())?.value ?? null });

const focused = await evaluate(`(() => {
  const f = document.querySelector('[data-testid="shipping-authoring-rate-freight"]');
  if (!f) return null;
  const r = f.getBoundingClientRect();
  f.focus();
  return { testId: document.activeElement?.getAttribute("data-testid") ?? null, w: Math.round(r.width) };
})()`);
record("focus", focused);

if (!focused || focused.testId !== "shipping-authoring-rate-freight") {
  record("typing", { blocked: "focus did not land on the freight field", focused });
} else {
  // 1) A valid grouped value keeps a middle insertion's caret after the typed digit (real keys).
  await selectAllFreight();
  for (const character of "1234") await keyChar(character);
  record("valid-typed", await readFreight());
  await evaluate(`(() => { const f = document.querySelector('[data-testid="shipping-authoring-rate-freight"]'); f.focus(); f.setSelectionRange(3, 3); })()`);
  await keyChar("9");
  record("middle-insert", await readFreight());

  // 2) Valid whole-rial entry: 0 and five million.
  for (const sequence of ["0", "5000000"]) {
    await selectAllFreight();
    for (const character of sequence) await keyChar(character);
    record(`valid-${sequence}`, await readFreight());
  }

  // 3) The operator PASTES a malformed grouping. The paste event is dispatched on the real element (so the
  //    field's own paste path is taken) and the text is inserted by CDP input, never by a value setter.
  await selectAllFreight();
  await evaluate(`(() => {
    const f = document.querySelector('[data-testid="shipping-authoring-rate-freight"]');
    f.dispatchEvent(new ClipboardEvent("paste", { bubbles: true, cancelable: true, clipboardData: new DataTransfer() }));
  })()`);
  await send("Input.insertText", { text: "12,34" }, sid);
  await sleep(350);
  record("pasted-invalid", await readFreight());

  // 4) The R2 defect: appended keys after the operator's own separator must never become accepted money.
  for (const digit of ["5", "6", "7"]) {
    await keyChar(digit);
    record(`appended-${digit}`, await readFreight());
  }

  // 5) Blur/re-render and another field changing never launder it.
  await realClick("shipping-authoring-rate-packaging");
  await sleep(300);
  record("after-other-field", await readFreight());
  const refocus = await evaluate(`(() => {
    const f = document.querySelector('[data-testid="shipping-authoring-rate-freight"]');
    f.focus();
    f.setSelectionRange(f.value.length, f.value.length);
    return document.activeElement === f;
  })()`);
  await keyChar("8");
  record("appended-8", await readFreight());

  // 6) A refusing Confirm keeps the modal open with a NON-EMPTY localized reason.
  record("confirm", await realClick("shipping-authoring-rate-modal-confirm"));
  record("refusal", await evaluate(`(() => {
    const box = document.querySelector('[data-testid="shipping-authoring-rate-refusal"]');
    return {
      open: !!box,
      reason: box?.querySelector("p")?.textContent ?? null,
      modalStillOpen: !!document.querySelector('[data-testid="shipping-authoring-rate-modal-confirm"]')?.getBoundingClientRect().width,
      freight: document.querySelector('[data-testid="shipping-authoring-rate-freight"]')?.value ?? null,
    };
  })()`));

  const shot = await send("Page.captureScreenshot", { format: "png" }, sid);
  if (shot.data) fs.writeFileSync(path.join(OUT, "screens", "225-R2-refusal.png"), Buffer.from(shot.data, "base64"));

  // 7) Cancel: the main area must stay exactly as stored and the form must not be falsely dirty.
  await evaluate(`document.querySelector('[data-testid="shipping-authoring-rate-refusal-ok"]')?.click()`);
  await sleep(300);
  record("cancel", await realClick("shipping-authoring-rate-modal-cancel"));
  await sleep(500);
  record("after-cancel", await evaluate(`(() => ({
    row: document.querySelector('[data-testid="shipping-authoring-rate-row"]')?.innerText.replace(/\\s+/g, " ").trim().slice(0, 140) ?? null,
    applyDisabled: !!document.querySelector('[data-testid="shipping-authoring-apply"]')?.disabled,
  }))()`));
}

fs.mkdirSync(path.join(OUT, "screens"), { recursive: true });
fs.writeFileSync(path.join(OUT, "mount-receipts.json"), JSON.stringify(receipts, null, 2));
ws.close();
