const FE = "http://127.0.0.1:3000";

async function timed(label, url, init) {
  const started = Date.now();
  const res = await fetch(url, init);
  const body = await res.text();
  const ms = Date.now() - started;
  const title = (body.match(/<title[^>]*>([^<]*)<\/title>/i) || [])[1] || null;
  const robots = (body.match(/name="robots"[^>]*content="([^"]+)"/i) || [])[1] || null;
  const ogTitle = (body.match(/property="og:title"[^>]*content="([^"]+)"/i) || [])[1] || null;
  const canonical = (body.match(/rel="canonical"[^>]*href="([^"]+)"/i) || body.match(/href="([^"]+)"[^>]*rel="canonical"/i) || [])[1] || null;
  const hreflang = (body.match(/hreflang=/g) || []).length;
  const jsonLd = body.includes("application/ld+json");
  const landing = body.includes("data-testid=\"landing-route\"") || body.includes("data-testid=\"storefront-landing-page\"");
  console.log(JSON.stringify({ label, status: res.status, ms, len: body.length, title, robots, ogTitle, canonical, hreflang, jsonLd, landing }));
  return { ms, body, status: res.status };
}

async function main() {
  await timed("A-cold-landing", `${FE}/landing/landing-demo`);
  await timed("B-warm1", `${FE}/landing/landing-demo`);
  await timed("B-warm2", `${FE}/landing/landing-demo`);
  await timed("B-warm3", `${FE}/landing/landing-demo`);
  await timed("C-home", `${FE}/`);
  await timed("D-landing-en", `${FE}/landing/landing-demo`, { headers: { "x-tooba-locale": "en" } });
  const rv = await fetch(`${FE}/api/storefront/revalidate`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ storeScope: "default", locale: "fa", slug: "landing-demo", homeSelection: true }),
  });
  console.log(JSON.stringify({ label: "F-revalidate", status: rv.status, body: await rv.text() }));
  await timed("F-after-revalidate", `${FE}/landing/landing-demo`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
