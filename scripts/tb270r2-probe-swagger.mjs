const base = "http://127.0.0.1:17321";
for (const u of ["/swagger/v1/swagger.json", "/swagger/swagger.json", "/swagger/v1/openapi.json"]) {
  try {
    const r = await fetch(base + u);
    const t = await r.text();
    console.log(u, r.status, t.length);
    if (r.ok) {
      const doc = JSON.parse(t);
      for (const [path, ops] of Object.entries(doc.paths ?? {})) {
        console.log(path, Object.keys(ops).join(","));
      }
    }
  } catch (e) { console.log(u, "ERR", e.message); }
}
