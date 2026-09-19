const HOST = "http://127.0.0.1:5088";
const dev = await (await fetch(`${HOST}/v1/admin/dev-context`)).json();
const actor = dev.actorUserId;
async function j(path, opts = {}) {
  const r = await fetch(`${HOST}${path}`, {
    ...opts,
    headers: {
      "X-Tooba-Dev-Actor-User-Id": actor,
      "Content-Type": "application/json",
      ...(opts.headers || {}),
    },
  });
  const text = await r.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  return { status: r.status, data };
}
const stamp = Date.now();
const page = await j("/v1/admin/pages", {
  method: "POST",
  body: JSON.stringify({ title: "dbg", slug: `dbg-${stamp}`, locale: "fa", pageType: "Landing" }),
});
console.log("page", page.status, page.data?.pageId || page.data);
const id = page.data?.pageId;
const sec = await j(`/v1/admin/pages/${id}/sections`, {
  method: "POST",
  body: JSON.stringify({
    sectionType: "ProductCollection",
    config: JSON.stringify({
      title: "x",
      source: "PromotionCampaign",
      promotionTypeCode: "AMAZING",
      take: 8,
    }),
    isEnabled: true,
  }),
});
console.log("section", sec.status, sec.data?.config?.slice?.(0, 120) || sec.data);
const prev = await j(`/v1/admin/pages/${id}/preview`);
console.log("preview status", prev.status);
const section = (prev.data?.sections || []).find((s) => s.sectionType === "ProductCollection");
console.log(
  JSON.stringify(
    {
      items: section?.items?.length,
      products: prev.data?.products?.length,
      first: section?.items?.[0],
      commerce: await (await fetch(`${HOST}/__platform-commerce`, { headers: { "X-Tooba-Dev-Actor-User-Id": actor } })).json().catch((e) => String(e)),
    },
    null,
    2,
  ),
);
