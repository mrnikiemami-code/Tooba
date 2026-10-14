/**
 * In-page fetch/confirm interception used for the TB-SHIPPING-MVP-ADMIN-ROUNDTRIP-051 browser run.
 *
 * Injected into the already-running dev page via CDP Runtime.evaluate. It intercepts ONLY
 * `/v1/admin/shipping-services*` so the real admin screen (React) drives a purely in-memory
 * Fulfillment catalog: the shared Development database is never written and real checkout
 * shipping options are untouched. Every other request (/v1/admin/languages, assets, HMR) is
 * passed through to the real fetch.
 *
 * Probe surface: window.__ship = { log, catalog, fail, confirms, confirmAnswer, langIds, activeLangId }
 */
(() => {
  if (window.__ship && window.__ship.installed) return "already-installed";

  const FA_NAMES = { post: "پست", in_person: "تحویل حضوری" };

  const state = (window.__ship = {
    installed: false,
    log: [],
    catalog: new Map(),
    fail: null,
    confirms: [],
    confirmAnswer: true,
    langIds: [],
    activeLangId: null,
    seeded: false,
  });

  const uuid = (prefix, n) => `${prefix}-1111-4111-8111-${String(n).padStart(12, "0")}`;
  const json = (payload, status = 200) =>
    new Response(JSON.stringify(payload), {
      status,
      headers: { "Content-Type": "application/json" },
    });

  function detailTranslations(name) {
    return state.langIds.map((languageId) => ({
      languageId,
      name: languageId === state.activeLangId ? name : `${name}-alt`,
      description: null,
    }));
  }

  function seed() {
    if (state.seeded || !state.langIds.length) return;
    state.seeded = true;
    state.catalog.set("aaaaaaaa-0000-4000-8000-000000000001", {
      shippingServiceId: "aaaaaaaa-0000-4000-8000-000000000001",
      code: "post",
      providerKind: "post",
      iconKey: "post",
      colorKey: "blue",
      isActive: true,
      sortOrder: 10,
      translations: detailTranslations(FA_NAMES.post),
      options: [
        {
          shippingServiceOptionId: "bbbbbbbb-0000-4000-8000-000000000001",
          code: "express",
          isActive: true,
          sortOrder: 10,
          translations: state.langIds.map((languageId) => ({
            languageId,
            name: languageId === state.activeLangId ? "پیشتاز" : "Express",
          })),
        },
      ],
    });
    state.catalog.set("aaaaaaaa-0000-4000-8000-000000000002", {
      shippingServiceId: "aaaaaaaa-0000-4000-8000-000000000002",
      code: "in_person",
      providerKind: "in_person",
      iconKey: "store",
      colorKey: "rose",
      isActive: true,
      sortOrder: 20,
      translations: detailTranslations(FA_NAMES.in_person),
      options: [],
    });
  }

  const listItem = (detail) => ({
    shippingServiceId: detail.shippingServiceId,
    code: detail.code,
    providerKind: detail.providerKind,
    iconKey: detail.iconKey,
    colorKey: detail.colorKey,
    name: detail.translations.find((t) => t.languageId === state.activeLangId)?.name ?? detail.code,
    isActive: detail.isActive,
    sortOrder: detail.sortOrder,
    optionCount: detail.options.length,
    activeOptionCount: detail.options.filter((o) => o.isActive).length,
  });

  let created = 0;

  async function handleShipping(url, method, init) {
    const body = init && init.body ? JSON.parse(String(init.body)) : null;
    state.log.push({ method, url, body });
    const fail = state.fail;
    state.fail = null;
    const path = url.split("?")[0];
    const deactivateMatch = /\/shipping-services\/([^/]+)\/deactivate$/.exec(path);
    const detailMatch = /\/shipping-services\/([^/]+)$/.exec(path);

    if (/\/shipping-services\/?$/.test(path)) {
      if (fail === "list") return json({ errorCode: "shipping_service.load_failed" }, 500);
      if (method === "GET") return json([...state.catalog.values()].map(listItem));
      if (method === "POST" && body) {
        if (fail === "write") return json({ errorCode: "shipping_service.code_duplicate" }, 409);
        const code = body.code.trim().toLowerCase();
        if ([...state.catalog.values()].some((s) => s.code === code)) {
          return json({ errorCode: "shipping_service.code_duplicate" }, 409);
        }
        created += 1;
        const id = uuid("cccccccc", created);
        const created0 = {
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
            shippingServiceOptionId: o.shippingServiceOptionId || uuid("dddddddd", index + created),
            code: o.code,
            isActive: o.isActive,
            sortOrder: o.sortOrder,
            translations: o.translations.map((t) => ({ languageId: t.languageId, name: t.name })),
          })),
        };
        state.catalog.set(id, created0);
        return json(created0, 201);
      }
      return json({ errorCode: "shipping_service.write_failed" }, 405);
    }

    if (deactivateMatch) {
      if (fail === "deactivate") return json({ errorCode: "shipping_service.deactivate_failed" }, 503);
      const service = state.catalog.get(deactivateMatch[1]);
      if (!service) return json({ errorCode: "shipping_service.not_found" }, 404);
      service.isActive = false;
      return json({ ok: true });
    }

    if (detailMatch) {
      const service = state.catalog.get(detailMatch[1]);
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
        service.options = body.options.map((o, index) => ({
          shippingServiceOptionId: o.shippingServiceOptionId || uuid("eeeeeeee", index),
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

  const originalFetch = window.fetch.bind(window);
  window.fetch = async (input, init) => {
    const url = typeof input === "string" ? input : input && input.url ? input.url : String(input);
    const method = String((init && init.method) || (input && input.method) || "GET").toUpperCase();

    if (url.includes("/v1/admin/languages") && !/\/languages\//.test(url)) {
      const response = await originalFetch(input, init);
      try {
        const rows = await response.clone().json();
        const active = (Array.isArray(rows) ? rows : []).filter((r) => r.active ?? r.Active);
        state.langIds = active.map((r) => String(r.languageId ?? r.LanguageId));
        state.activeLangId = state.langIds[0] ?? null;
        seed();
      } catch {
        /* languages probe is best-effort */
      }
      return response;
    }

    if (!url.includes("/v1/admin/shipping-services")) return originalFetch(input, init);
    return handleShipping(url, method, init);
  };

  const originalConfirm = window.confirm.bind(window);
  window.confirm = (message) => {
    state.confirms.push(String(message ?? ""));
    return state.confirmAnswer;
  };

  state.originalFetch = originalFetch;
  state.originalConfirm = originalConfirm;
  state.installed = true;
  return "installed";
})();
