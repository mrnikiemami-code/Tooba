const base = "http://127.0.0.1:17321";
for (const u of ["/", "/api", "/swagger", "/swagger/index.html", "/index.html", "/api/state", "/api/status", "/api/health"]) {
  try {
    const r = await fetch(base + u);
    const t = await r.text();
    console.log(u, r.status, t.length, JSON.stringify(t.slice(0, 300)));
  } catch (e) { console.log(u, "ERR", e.message); }
}
