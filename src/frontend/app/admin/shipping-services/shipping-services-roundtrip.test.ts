/**
 * Integration roundtrip برای `shipping-services-api.ts` روی یک Fulfillment API شبیه‌سازی‌شدهٔ درون‌حافظه‌ای.
 *
 * هدف: اثبات اینکه مسیرهای create / load-edit / save-update / option-edit / deactivate واقعاً
 * همان endpointها را با همان method و همان shape بدنهٔ DTO صدا می‌زنند — بدون هیچ نوشتنی روی
 * دیتابیس اشتراکی Development (چون تغییر کاتالوگ Fulfillment، گزینه‌های واقعی ارسال در checkout را
 * عوض می‌کند).
 *
 * fixture دقیقاً همان قرارداد wire بک‌اند را پیاده می‌کند:
 * `ShippingServiceEndpoints` + `ShippingServiceDirectory` (کد lowercase، ترجمه‌ها روی languageId،
 * هویت پایدار `shippingServiceOptionId`).
 */
import assert from "node:assert/strict";
import test from "node:test";
import {
  deactivateAdminShippingService,
  loadAdminShippingService,
  loadAdminShippingServices,
  writeAdminShippingService,
  type ShippingServiceWriteRequest,
} from "../shipping-services-api.ts";
import { buildShippingServiceWriteRequest } from "./shipping-services-form-model.ts";

const FA = "11111111-1111-4111-8111-111111111111";
const EN = "22222222-2222-4222-8222-222222222222";
const POST_ID = "aaaaaaaa-1111-4111-8111-aaaaaaaaaaaa";
const EXPRESS_ID = "bbbbbbbb-2222-4222-8222-bbbbbbbbbbbb";

type Recorded = {
  method: string;
  url: string;
  body: ShippingServiceWriteRequest | null;
  contentType: string | null;
};

type DetailDto = {
  shippingServiceId: string;
  code: string;
  providerKind: string;
  iconKey: string;
  colorKey: string;
  isActive: boolean;
  sortOrder: number;
  translations: { languageId: string; name: string; description: string | null }[];
  options: {
    shippingServiceOptionId: string;
    code: string;
    isActive: boolean;
    sortOrder: number;
    translations: { languageId: string; name: string }[];
  }[];
};

/** Fulfillment catalog شبیه‌سازی‌شده: فقط حافظه، هیچ I/O. */
function createFixtureApi() {
  const services = new Map<string, DetailDto>();
  services.set(POST_ID, {
    shippingServiceId: POST_ID,
    code: "post",
    providerKind: "post",
    iconKey: "post",
    colorKey: "blue",
    isActive: true,
    sortOrder: 10,
    translations: [
      { languageId: FA, name: "پست", description: null },
      { languageId: EN, name: "Post", description: null },
    ],
    options: [
      {
        shippingServiceOptionId: EXPRESS_ID,
        code: "express",
        isActive: true,
        sortOrder: 10,
        translations: [
          { languageId: FA, name: "پیشتاز" },
          { languageId: EN, name: "Express" },
        ],
      },
    ],
  });

  const log: Recorded[] = [];
  let failNext: "write" | "load" | "deactivate" | null = null;
  let createCounter = 0;

  function json(payload: unknown, status = 200): Response {
    return new Response(JSON.stringify(payload), {
      status,
      headers: { "Content-Type": "application/json" },
    });
  }

  function toListItem(detail: DetailDto) {
    const activeOptionCount = detail.options.filter((o) => o.isActive).length;
    return {
      shippingServiceId: detail.shippingServiceId,
      code: detail.code,
      providerKind: detail.providerKind,
      iconKey: detail.iconKey,
      colorKey: detail.colorKey,
      name: detail.translations.find((t) => t.languageId === FA)?.name ?? detail.code,
      isActive: detail.isActive,
      sortOrder: detail.sortOrder,
      optionCount: detail.options.length,
      activeOptionCount,
    };
  }

  async function handle(url: string, method: string, init?: RequestInit): Promise<Response> {
    const body = init?.body ? (JSON.parse(String(init.body)) as ShippingServiceWriteRequest) : null;
    log.push({
      method,
      url,
      body,
      contentType: new Headers(init?.headers).get("Content-Type"),
    });

    const path = url.split("?")[0];
    const deactivateMatch = /\/shipping-services\/([^/]+)\/deactivate$/.exec(path);
    const detailMatch = /\/shipping-services\/([^/]+)$/.exec(path);
    const fail = failNext;
    failNext = null;

    if (path.endsWith("/shipping-services") || path.endsWith("/shipping-services/")) {
      if (fail === "write") return json({ errorCode: "shipping_service.code_duplicate" }, 409);
      if (method === "GET") return json([...services.values()].map(toListItem));
      if (method === "POST" && body) {
        const code = body.code.trim().toLowerCase();
        if ([...services.values()].some((s) => s.code === code)) {
          return json({ errorCode: "shipping_service.code_duplicate" }, 409);
        }
        createCounter += 1;
        const id = `cccccccc-3333-4333-8333-ccccccccccc${createCounter}`;
        const created: DetailDto = {
          shippingServiceId: id,
          code,
          providerKind: body.providerKind,
          iconKey: body.iconKey,
          colorKey: body.colorKey,
          isActive: body.isActive,
          sortOrder: body.sortOrder,
          translations: body.translations.map((t) => ({
            languageId: t.languageId,
            name: t.name,
            description: t.description ?? null,
          })),
          options: body.options.map((o, index) => ({
            shippingServiceOptionId: o.shippingServiceOptionId ?? `dddddddd-4444-4444-8444-ddddddddddd${index}`,
            code: o.code,
            isActive: o.isActive,
            sortOrder: o.sortOrder,
            translations: o.translations.map((t) => ({ languageId: t.languageId, name: t.name })),
          })),
        };
        services.set(id, created);
        return json(created, 201);
      }
      return json({ errorCode: "shipping_service.write_failed" }, 405);
    }

    if (deactivateMatch) {
      const service = services.get(deactivateMatch[1]!);
      if (fail === "deactivate") return json({ errorCode: "host-unreachable" }, 503);
      if (!service) return json({ errorCode: "shipping_service.not_found" }, 404);
      service.isActive = false;
      return json({ ok: true });
    }

    if (detailMatch) {
      const service = services.get(detailMatch[1]!);
      if (method === "GET") {
        if (fail === "load") return json({ errorCode: "shipping_service.load_failed" }, 500);
        if (!service) return json({ errorCode: "shipping_service.not_found" }, 404);
        return json(service);
      }
      if (method === "PUT" && body) {
        if (fail === "write") return json({ errorCode: "shipping_service.code_duplicate" }, 409);
        if (!service) return json({ errorCode: "shipping_service.not_found" }, 404);
        service.code = body.code.trim().toLowerCase();
        service.providerKind = body.providerKind;
        service.iconKey = body.iconKey;
        service.colorKey = body.colorKey;
        service.isActive = body.isActive;
        service.sortOrder = body.sortOrder;
        service.translations = body.translations.map((t) => ({
          languageId: t.languageId,
          name: t.name,
          description: t.description ?? null,
        }));
        // قرارداد بک‌اند: ردیف‌های نوع سرویس بازنویسی می‌شوند اما هویت پاس‌داده‌شده حفظ می‌شود.
        service.options = body.options.map((o, index) => ({
          shippingServiceOptionId: o.shippingServiceOptionId ?? `eeeeeeee-5555-4555-8555-eeeeeeeeeee${index}`,
          code: o.code,
          isActive: o.isActive,
          sortOrder: o.sortOrder,
          translations: o.translations.map((t) => ({ languageId: t.languageId, name: t.name })),
        }));
        return json(service);
      }
    }

    return json({ errorCode: "unexpected" }, 404);
  }

  return {
    log,
    services,
    install() {
      const original = globalThis.fetch;
      globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
        return handle(url, (init?.method ?? "GET").toUpperCase(), init);
      }) as typeof fetch;
      return () => {
        globalThis.fetch = original;
      };
    },
    failNextRequest(kind: "write" | "load" | "deactivate") {
      failNext = kind;
    },
    writes() {
      return log.filter((row) => row.method === "POST" || row.method === "PUT");
    },
  };
}

test("create roundtrip: POST path, method, DTO shape and list refresh", async () => {
  const api = createFixtureApi();
  const restore = api.install();
  try {
    const created = await writeAdminShippingService(
      null,
      buildShippingServiceWriteRequest({
        code: "  NewService ",
        providerKind: "courier",
        iconKey: "courier",
        colorKey: "emerald",
        isActive: true,
        sortOrder: "70",
        translations: [
          { languageId: FA, name: " سرویس تازه " },
          { languageId: EN, name: "" },
        ],
        options: [
          {
            code: " Fast ",
            isActive: true,
            sortOrder: 10,
            translations: [{ languageId: FA, name: " سریع " }],
          },
        ],
      }),
    );

    assert.equal(created.state, "ok");
    const write = api.writes()[0]!;
    assert.equal(write.method, "POST");
    assert.equal(write.url, "/v1/admin/shipping-services/");
    assert.match(write.contentType ?? "", /application\/json/);
    assert.deepEqual(write.body, {
      code: "newservice",
      providerKind: "courier",
      iconKey: "courier",
      colorKey: "emerald",
      isActive: true,
      sortOrder: 70,
      translations: [{ languageId: FA, name: "سرویس تازه", description: null }],
      options: [
        {
          shippingServiceOptionId: null,
          code: "fast",
          isActive: true,
          sortOrder: 10,
          translations: [{ languageId: FA, name: "سریع" }],
        },
      ],
    });

    // detail پاسخ باید هویت واقعی نوع سرویس را برگرداند، نه null.
    assert.equal(created.data?.options[0]?.shippingServiceOptionId.length, 36);

    const list = await loadAdminShippingServices("fa");
    assert.equal(list.state, "ok");
    assert.equal(list.data?.length, 2);
    assert.deepEqual(
      list.data?.map((row) => row.code),
      ["post", "newservice"],
    );
    assert.equal(list.data?.[1]?.optionCount, 1);
    assert.equal(list.data?.[1]?.activeOptionCount, 1);
  } finally {
    restore();
  }
});

test("load-edit then save-update roundtrip: GET detail, PUT with preserved option identity", async () => {
  const api = createFixtureApi();
  const restore = api.install();
  try {
    const detail = await loadAdminShippingService(POST_ID);
    assert.equal(detail.state, "ok");
    assert.equal(api.log[0]?.method, "GET");
    assert.equal(api.log[0]?.url, `/v1/admin/shipping-services/${POST_ID}`);
    assert.equal(detail.data?.options[0]?.shippingServiceOptionId, EXPRESS_ID);
    assert.equal(detail.data?.options[0]?.translations[0]?.name, "پیشتاز");

    // ویرایش نام سرویس و نام نوع سرویس روی همان هویت بارگذاری‌شده.
    const updated = await writeAdminShippingService(
      POST_ID,
      buildShippingServiceWriteRequest({
        code: detail.data!.code,
        providerKind: detail.data!.providerKind,
        iconKey: detail.data!.iconKey,
        colorKey: detail.data!.colorKey,
        isActive: detail.data!.isActive,
        sortOrder: String(detail.data!.sortOrder),
        translations: [
          { languageId: FA, name: "پست ویژه" },
          { languageId: EN, name: "Post Special" },
        ],
        options: [
          {
            shippingServiceOptionId: EXPRESS_ID,
            code: "express",
            isActive: false,
            sortOrder: 10,
            translations: [{ languageId: FA, name: "پیشتاز ویژه" }],
          },
        ],
      }),
    );

    assert.equal(updated.state, "ok");
    const write = api.writes()[0]!;
    assert.equal(write.method, "PUT");
    assert.equal(write.url, `/v1/admin/shipping-services/${POST_ID}`);
    assert.equal(write.body?.options[0]?.shippingServiceOptionId, EXPRESS_ID);
    assert.equal(write.body?.code, "post");

    assert.equal(updated.data?.translations[0]?.name, "پست ویژه");
    assert.equal(updated.data?.options[0]?.shippingServiceOptionId, EXPRESS_ID);
    assert.equal(updated.data?.options[0]?.translations[0]?.name, "پیشتاز ویژه");
    assert.equal(updated.data?.options[0]?.isActive, false);

    const list = await loadAdminShippingServices("fa");
    assert.equal(list.data?.[0]?.name, "پست ویژه");
    assert.equal(list.data?.[0]?.optionCount, 1);
    assert.equal(list.data?.[0]?.activeOptionCount, 0);
  } finally {
    restore();
  }
});

test("confirmed deactivate roundtrip: POST deactivate and inactive row", async () => {
  const api = createFixtureApi();
  const restore = api.install();
  try {
    const result = await deactivateAdminShippingService(POST_ID);
    assert.equal(result.state, "ok");

    const write = api.writes()[0]!;
    assert.equal(write.method, "POST");
    assert.equal(write.url, `/v1/admin/shipping-services/${POST_ID}/deactivate`);
    assert.equal(write.body, null);
    assert.equal(api.services.get(POST_ID)?.isActive, false);

    const list = await loadAdminShippingServices("fa");
    assert.equal(list.data?.[0]?.isActive, false);
  } finally {
    restore();
  }
});

test("rejected mutations surface a stable code and leave the catalog untouched", async () => {
  const api = createFixtureApi();
  const restore = api.install();
  try {
    api.failNextRequest("write");
    const badWrite = await writeAdminShippingService(
      POST_ID,
      buildShippingServiceWriteRequest({
        code: "post",
        providerKind: "post",
        iconKey: "post",
        colorKey: "blue",
        isActive: false,
        sortOrder: "10",
        translations: [{ languageId: FA, name: "پست" }],
        options: [],
      }),
    );
    assert.equal(badWrite.state, "error");
    assert.equal(badWrite.status, 409);
    assert.equal(badWrite.message, "shipping_service.code_duplicate");
    // هیچ تغییری نباید اعمال شده باشد.
    assert.equal(api.services.get(POST_ID)?.isActive, true);
    assert.equal(api.services.get(POST_ID)?.translations[0]?.name, "پست");
    assert.equal(api.services.get(POST_ID)?.options.length, 1);

    api.failNextRequest("load");
    const badLoad = await loadAdminShippingService(POST_ID);
    assert.equal(badLoad.state, "error");
    assert.equal(badLoad.status, 500);

    api.failNextRequest("deactivate");
    const badDeactivate = await deactivateAdminShippingService(POST_ID);
    assert.equal(badDeactivate.state, "error");
    assert.equal(api.services.get(POST_ID)?.isActive, true);
  } finally {
    restore();
  }
});

test("denied and unreachable responses map to denied/host-unreachable", async () => {
  const api = createFixtureApi();
  const original = api.install();
  try {
    globalThis.fetch = (async () =>
      new Response(null, { status: 403 })) as typeof fetch;
    assert.equal((await loadAdminShippingServices("fa")).state, "denied");
    assert.equal((await writeAdminShippingService(null, buildShippingServiceWriteRequest({
      code: "x",
      providerKind: "post",
      iconKey: "post",
      colorKey: "blue",
      isActive: true,
      sortOrder: "1",
      translations: [{ languageId: FA, name: "x" }],
      options: [],
    }))).state, "denied");
    assert.equal((await deactivateAdminShippingService(POST_ID)).state, "denied");

    globalThis.fetch = (async () => {
      throw new TypeError("fetch failed");
    }) as typeof fetch;
    assert.equal((await loadAdminShippingService(POST_ID)).state, "error");
    assert.equal((await loadAdminShippingService(POST_ID)).message, "host-unreachable");
  } finally {
    original();
  }
});

test("generic endpoint failure keeps a non-empty message for the UI fallback", async () => {
  const api = createFixtureApi();
  const restore = api.install();
  try {
    globalThis.fetch = (async () =>
      new Response(JSON.stringify({ title: "boom" }), { status: 500 })) as typeof fetch;
    const result = await writeAdminShippingService(null, buildShippingServiceWriteRequest({
      code: "x",
      providerKind: "post",
      iconKey: "post",
      colorKey: "blue",
      isActive: true,
      sortOrder: "1",
      translations: [{ languageId: FA, name: "x" }],
      options: [],
    }));
    assert.equal(result.state, "error");
    assert.equal(result.message, "boom");
  } finally {
    restore();
  }
});
