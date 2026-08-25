import http from "node:http";
import { spawn } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { setTimeout as delay } from "node:timers/promises";

const outDir = path.resolve("docs/evidence/TB-P05-T014");
const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const port = 9241;
const host = "http://127.0.0.1:5088";
const web = "http://127.0.0.1:3000";
const seedActor = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const emptyActor = "cccccccc-cccc-4ccc-8ccc-000000000014";
const defaultAddressId = "aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1";
let offerId = null;

fs.mkdirSync(outDir, { recursive: true });

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let data = "";
      response.on("data", (chunk) => (data += chunk));
      response.on("end", () => {
        try {
          resolve(JSON.parse(data));
        } catch (error) {
          reject(error);
        }
      });
    }).on("error", reject);
  });
}

async function hostFetch(pathname, { method = "GET", actor = seedActor, body, guestSecret } = {}) {
  const headers = { Accept: "application/json" };
  if (actor) headers["X-Tooba-Dev-Actor-User-Id"] = actor;
  if (guestSecret) headers["X-Tooba-Guest-Secret"] = guestSecret;
  if (body) headers["Content-Type"] = "application/json";
  const response = await fetch(`${host}${pathname}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json, text };
}

async function withSession(viewport, work) {
  const userData = fs.mkdtempSync(path.join(process.env.TEMP || ".", "tooba-t014-chrome-"));
  const chromeProcess = spawn(
    chrome,
    [
      `--remote-debugging-port=${port}`,
      `--user-data-dir=${userData}`,
      "--headless=new",
      "--disable-gpu",
      "--hide-scrollbars",
      `--window-size=${viewport.width},${viewport.height}`,
      "about:blank",
    ],
    { stdio: "ignore" },
  );

  let webSocketUrl = null;
  for (let attempt = 0; attempt < 40; attempt += 1) {
    try {
      const pages = await getJson(`http://127.0.0.1:${port}/json/list`);
      const page = pages.find((item) => item.type === "page") || pages[0];
      if (page?.webSocketDebuggerUrl) {
        webSocketUrl = page.webSocketDebuggerUrl;
        break;
      }
    } catch {
      // Chrome هنوز آماده نیست.
    }
    await delay(250);
  }
  if (!webSocketUrl) {
    chromeProcess.kill();
    throw new Error("Chrome CDP not ready");
  }

  const socket = new WebSocket(webSocketUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve);
    socket.addEventListener("error", reject);
  });
  let nextId = 1;
  const pending = new Map();
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const handlers = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) handlers.reject(new Error(JSON.stringify(message.error)));
      else handlers.resolve(message.result);
    }
  });
  const send = (method, params = {}) =>
    new Promise((resolve, reject) => {
      const id = nextId++;
      pending.set(id, { resolve, reject });
      socket.send(JSON.stringify({ id, method, params }));
    });

  try {
    await send("Page.enable");
    await send("Runtime.enable");
    await send("Emulation.setDeviceMetricsOverride", {
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: 1,
      mobile: viewport.mobile === true,
    });
    await work({ send });
  } finally {
    socket.close();
    chromeProcess.kill();
    try {
      fs.rmSync(userData, { recursive: true, force: true });
    } catch {
      // پاک‌سازی best-effort پروفایل موقت.
    }
  }
}

async function navigate(send, url, readyText) {
  await send("Page.navigate", { url });
  for (let attempt = 0; attempt < 60; attempt += 1) {
    await delay(200);
    const result = await send("Runtime.evaluate", {
      expression: `({ ready: document.body && document.body.innerText.includes(${JSON.stringify(readyText)}), overflow: document.documentElement.scrollWidth > innerWidth + 2, text: (document.body?.innerText || '').slice(0, 240) })`,
      returnByValue: true,
    });
    if (result.result.value?.ready) {
      if (result.result.value.overflow) throw new Error(`horizontal overflow at ${url}`);
      return;
    }
  }
  const last = await send("Runtime.evaluate", {
    expression: `(document.body?.innerText || '').slice(0, 500)`,
    returnByValue: true,
  });
  throw new Error(`page did not become ready: ${url} :: ${last.result.value}`);
}

async function screenshot(send, name) {
  const result = await send("Page.captureScreenshot", { format: "png", fromSurface: true });
  const file = path.join(outDir, name);
  fs.writeFileSync(file, Buffer.from(result.data, "base64"));
  console.log("wrote", file, fs.statSync(file).size);
}

async function setActor(send, actorId) {
  await send("Runtime.evaluate", {
    expression: `localStorage.setItem('tooba.customerActorUserId', ${JSON.stringify(actorId)}); true`,
    returnByValue: true,
  });
}

async function resolveOfferId() {
  for (let page = 1; page <= 12; page += 1) {
    const listing = await hostFetch(`/v1/storefront/products?page=${page}&pageSize=50`, { actor: null });
    const products = listing.json?.products || [];
    const hit = products.find((item) => item.slug === "workspace-live-shirt" && (item.availableUnits ?? 0) > 0)
    || products.find((item) => item.slug === "workspace-live-shirt");
    if (hit?.primaryOfferId) {
      offerId = hit.primaryOfferId;
      console.log("using taxed offerId", offerId, hit.title, "units", hit.availableUnits);
      return;
    }
    if (products.length === 0) break;
  }
  throw new Error("workspace-live-shirt offer not found");
}

async function seedCartInBrowser(send) {
  if (!offerId) throw new Error("offerId unresolved");
  const expression = `(() => {
    const offerId = ${JSON.stringify(offerId)};
    return fetch('/v1/storefront/cart', {
      method: 'POST',
      headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
      body: JSON.stringify({})
    }).then(async (createResponse) => {
      const created = await createResponse.json();
      const cartId = created.cartId || created.CartId;
      const guestSecret = created.guestSecret || created.GuestSecret;
      const version = created.version ?? created.Version ?? 0;
      if (!cartId || !guestSecret) throw new Error('cart create failed');
      sessionStorage.setItem('tooba.storefront.cartId', cartId);
      sessionStorage.setItem('tooba.storefront.guestSecret', guestSecret);
      return fetch('/v1/storefront/cart/' + cartId + '/lines?expectedVersion=' + version, {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
          'X-Tooba-Guest-Secret': guestSecret
        },
        body: JSON.stringify({ offerId, quantity: 1 })
      }).then(async (lineResponse) => {
        if (!lineResponse.ok) {
          const body = await lineResponse.text();
          throw new Error('add line failed ' + lineResponse.status + ' ' + body);
        }
        return { cartId, guestSecret };
      });
    });
  })()`;
  const result = await send("Runtime.evaluate", {
    expression,
    awaitPromise: true,
    returnByValue: true,
  });
  if (result.exceptionDetails) {
    throw new Error(JSON.stringify(result.exceptionDetails));
  }
  return result.result.value;
}

async function clickText(send, text) {
  const result = await send("Runtime.evaluate", {
    expression: `(() => {
      const needle = ${JSON.stringify(text)};
      const nodes = [...document.querySelectorAll('button, a, [role="button"]')];
      const match = nodes.find((node) => (node.innerText || node.textContent || '').includes(needle));
      if (!match) return false;
      match.click();
      return true;
    })()`,
    returnByValue: true,
  });
  if (!result.result.value) throw new Error(`click target missing: ${text}`);
}

await resolveOfferId();

// API probes for markdown evidence
const seedList = await hostFetch("/v1/customer/addresses");
const emptyList = await hostFetch("/v1/customer/addresses", { actor: emptyActor });
const foreignGet = await hostFetch(`/v1/customer/addresses/${defaultAddressId}`, {
  actor: emptyActor,
});
const createProbe = await hostFetch("/v1/customer/addresses", {
  method: "POST",
  actor: emptyActor,
  body: {
    recipientName: "گیرندهٔ ایزوله",
    contactMobile: "+989120000099",
    country: "IR",
    provinceName: "شیراز",
    cityName: "شیراز",
    postalCode: "71345",
    postalAddress: "خیابان آزمون ایزوله، پلاک ۹۹",
    buildingUnit: "۱",
    label: "آزمون",
    isDefault: true,
  },
});
const createdId = createProbe.json?.addressId || createProbe.json?.AddressId;
const afterCreate = await hostFetch("/v1/customer/addresses", { actor: emptyActor });
if (createdId) {
  await hostFetch(`/v1/customer/addresses/${createdId}`, { method: "DELETE", actor: emptyActor });
}

async function createCartWithLine() {
  const cartCreate = await hostFetch("/v1/storefront/cart", { method: "POST", actor: null, body: {} });
  const cartId = cartCreate.json?.cartId || cartCreate.json?.CartId;
  const guestSecret = cartCreate.json?.guestSecret || cartCreate.json?.GuestSecret;
  const cartVersion = cartCreate.json?.version ?? cartCreate.json?.Version ?? 0;
  const addLine = await hostFetch(`/v1/storefront/cart/${cartId}/lines?expectedVersion=${cartVersion}`, {
    method: "POST",
    actor: null,
    guestSecret,
    body: { offerId, quantity: 1 },
  });
  return {
    cartId,
    guestSecret,
    expectedVersion: addLine.json?.version ?? addLine.json?.Version,
    addStatus: addLine.status,
    addBody: addLine.text?.slice(0, 300),
  };
}

await withSession({ width: 1440, height: 900, mobile: false }, async ({ send }) => {
  await send("Page.navigate", { url: web });
  await delay(500);
  await setActor(send, seedActor);

  await navigate(send, `${web}/customer-panel/addresses`, "آدرس‌های من");
  await delay(400);
  await screenshot(send, "03-customer-address-list-desktop.png");

  await clickText(send, "آدرس جدید");
  await delay(300);
  await navigate(send, `${web}/customer-panel/addresses`, "آدرس‌های من");
  await clickText(send, "آدرس جدید");
  await delay(400);
  // Fill via React-controlled inputs by placeholder/label Persian labels
  await send("Runtime.evaluate", {
    expression: `(() => {
      const map = [
        ['عنوان آدرس', 'شواهد'],
        ['آدرس کامل', 'خیابان شواهد، پلاک ۱۱۴'],
        ['کشور', 'IR'],
        ['استان', 'تهران'],
        ['شهر', 'تهران'],
        ['کد پستی', '19199'],
        ['واحد / پلاک', '۲'],
        ['نام گیرنده', 'گیرندهٔ شواهد'],
        ['شماره تماس', '09120000114'],
      ];
      for (const [labelText, value] of map) {
        const label = [...document.querySelectorAll('label')].find((el) => (el.childNodes[0]?.textContent || '').trim() === labelText || (el.innerText || '').trim().startsWith(labelText));
        if (!label) continue;
        const field = label.querySelector('input, textarea');
        if (!field) continue;
        const proto = field.tagName === 'TEXTAREA' ? window.HTMLTextAreaElement.prototype : window.HTMLInputElement.prototype;
        const setter = Object.getOwnPropertyDescriptor(proto, 'value')?.set;
        setter?.call(field, value);
        field.dispatchEvent(new Event('input', { bubbles: true }));
        field.dispatchEvent(new Event('change', { bubbles: true }));
      }
      return true;
    })()`,
    returnByValue: true,
  });
  await delay(200);
  await screenshot(send, "04-customer-address-create.png");
  // Close create form without saving to keep seed deterministic
  await send("Runtime.evaluate", {
    expression: `(() => {
      const cancel = [...document.querySelectorAll('button')].find((b) => /انصراف|بستن|لغو/.test(b.innerText || ''));
      if (cancel) cancel.click();
      else {
        const x = [...document.querySelectorAll('button')].find((b) => b.querySelector('svg') && /X|بستن/.test(b.getAttribute('aria-label') || ''));
        x?.click();
      }
      return true;
    })()`,
    returnByValue: true,
  });
  await delay(300);

  // Open edit on default address and show default badge
  await send("Runtime.evaluate", {
    expression: `(() => {
      const edit = document.querySelector('button[aria-label="ویرایش آدرس"]');
      edit?.click();
      return !!edit;
    })()`,
    returnByValue: true,
  });
  await delay(400);
  await screenshot(send, "05-customer-address-edit-default.png");
  await send("Runtime.evaluate", {
    expression: `(() => {
      const cancel = [...document.querySelectorAll('button')].find((b) => /انصراف|بستن|لغو/.test(b.innerText || ''));
      cancel?.click();
      return true;
    })()`,
    returnByValue: true,
  });

  await setActor(send, emptyActor);
  await navigate(send, `${web}/customer-panel/addresses`, "هیچ آدرسی یافت نشد");
  await delay(300);
  await screenshot(send, "06-customer-address-empty-state.png");

  await setActor(send, seedActor);
  await send("Page.navigate", { url: web });
  await delay(400);
  await setActor(send, seedActor);
  await seedCartInBrowser(send);
  await navigate(send, `${web}/checkout`, "انتخاب آدرس");
  await delay(600);
  await clickText(send, "آدرس‌های من");
  await delay(500);
  await navigateReady(send, "گیرندهٔ نمایشی توبا");
  await screenshot(send, "07-checkout-saved-address-selection.png");
});

await withSession({ width: 390, height: 844, mobile: true }, async ({ send }) => {
  await send("Page.navigate", { url: web });
  await delay(400);
  await setActor(send, seedActor);
  await navigate(send, `${web}/customer-panel/addresses`, "آدرس‌های من");
  await delay(400);
  await screenshot(send, "10-customer-address-mobile-390x844.png");

  await seedCartInBrowser(send);
  await navigate(send, `${web}/checkout`, "انتخاب آدرس");
  await delay(600);
  await clickText(send, "آدرس‌های من");
  await delay(500);
  await navigateReady(send, "گیرندهٔ نمایشی توبا");
  await screenshot(send, "11-checkout-mobile-address-390x844.png");
});

const snapshotCart = await createCartWithLine();
const snapshotCheckout = await hostFetch("/v1/storefront/checkout", {
  method: "POST",
  actor: seedActor,
  guestSecret: snapshotCart.guestSecret,
  body: {
    cartId: snapshotCart.cartId,
    expectedCartVersion: snapshotCart.expectedVersion,
    idempotencyKey: `t014-snapshot-${Date.now()}`,
    shipping: {
      recipientName: "ignored",
      contactMobile: "ignored",
      provinceName: "ignored",
      cityName: "ignored",
      postalAddress: "ignored",
      postalCode: "ignored",
      savedAddressId: defaultAddressId,
    },
  },
});

const foreignCart = await createCartWithLine();
const foreignCheckout = await hostFetch("/v1/storefront/checkout", {
  method: "POST",
  actor: emptyActor,
  guestSecret: foreignCart.guestSecret,
  body: {
    cartId: foreignCart.cartId,
    expectedCartVersion: foreignCart.expectedVersion,
    idempotencyKey: `t014-foreign-${Date.now()}`,
    shipping: {
      recipientName: "x",
      contactMobile: "09120000000",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "x",
      postalCode: "19199",
      savedAddressId: defaultAddressId,
    },
  },
});

const guestCart = await createCartWithLine();
const guestCheckout = await hostFetch("/v1/storefront/checkout", {
  method: "POST",
  actor: null,
  guestSecret: guestCart.guestSecret,
  body: {
    cartId: guestCart.cartId,
    expectedCartVersion: guestCart.expectedVersion,
    idempotencyKey: `t014-guest-${Date.now()}`,
    shipping: {
      recipientName: "مهمان خطی",
      contactMobile: "09123334455",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "خیابان مهمان، پلاک ۱",
      postalCode: "19199",
    },
  },
});

const mutateAfter = await hostFetch(`/v1/customer/addresses/${defaultAddressId}`, {
  method: "PUT",
  actor: seedActor,
  body: {
    recipientName: "ویرایش‌شده بعد از سفارش",
    contactMobile: "+989120000014",
    country: "IR",
    provinceName: "تهران",
    cityName: "تهران",
    postalCode: "19199",
    postalAddress: "آدرس تغییر کرده بعد از سفارش",
    buildingUnit: "واحد ۱",
    label: "خانه",
    isDefault: true,
  },
});

const getAfterMutate = await hostFetch(
  `/v1/storefront/checkout/${snapshotCheckout.json?.checkoutId || snapshotCheckout.json?.CheckoutId}?cartId=${snapshotCart.cartId}`,
  {
    actor: seedActor,
    guestSecret: snapshotCart.guestSecret,
  },
);

await hostFetch(`/v1/customer/addresses/${defaultAddressId}`, {
  method: "PUT",
  actor: seedActor,
  body: {
    recipientName: "گیرندهٔ نمایشی توبا",
    contactMobile: "+989120000014",
    country: "IR",
    provinceName: "تهران",
    cityName: "تهران",
    postalCode: "19199",
    postalAddress: "خیابان نمونه، پلاک ۱۴، دفتر نمایشی فروشگاه",
    buildingUnit: "واحد ۱",
    label: "خانه",
    isDefault: true,
  },
});
const probePath = path.join(outDir, "_api-probe.json");
fs.writeFileSync(
  probePath,
  JSON.stringify(
    {
      offerId,
      seedListStatus: seedList.status,
      seedCount: Array.isArray(seedList.json) ? seedList.json.length : seedList.json?.items?.length ?? seedList.json,
      emptyBefore: emptyList.status,
      emptyCount: Array.isArray(emptyList.json) ? emptyList.json.length : emptyList.json,
      foreignGetStatus: foreignGet.status,
      createProbeStatus: createProbe.status,
      afterCreateCount: Array.isArray(afterCreate.json) ? afterCreate.json.length : afterCreate.json,
      snapshotCartAddStatus: snapshotCart.addStatus,
      snapshotCheckoutStatus: snapshotCheckout.status,
      snapshotRecipient:
        snapshotCheckout.json?.recipientName ||
        snapshotCheckout.json?.RecipientName ||
        snapshotCheckout.json,
      snapshotPostalAddress:
        snapshotCheckout.json?.postalAddress ||
        snapshotCheckout.json?.PostalAddress ||
        null,
      afterMutateRecipient:
        getAfterMutate.json?.recipientName ||
        getAfterMutate.json?.RecipientName ||
        getAfterMutate.status,
      foreignCheckoutStatus: foreignCheckout.status,
      foreignCheckoutBody: foreignCheckout.text?.slice(0, 400),
      guestCheckoutStatus: guestCheckout.status,
      guestRecipient:
        guestCheckout.json?.recipientName ||
        guestCheckout.json?.RecipientName ||
        guestCheckout.json,
      mutateDefaultStatus: mutateAfter.status,
    },
    null,
    2,
  ),
);
console.log("wrote", probePath);
console.log("capture complete");

async function navigateReady(send, readyText) {
  for (let attempt = 0; attempt < 40; attempt += 1) {
    await delay(150);
    const result = await send("Runtime.evaluate", {
      expression: `({ ready: document.body && document.body.innerText.includes(${JSON.stringify(readyText)}), overflow: document.documentElement.scrollWidth > innerWidth + 2 })`,
      returnByValue: true,
    });
    if (result.result.value?.ready) {
      if (result.result.value.overflow) throw new Error(`horizontal overflow waiting for ${readyText}`);
      return;
    }
  }
  throw new Error(`ready text missing: ${readyText}`);
}