# TB-P08-T012 — Runtime smoke

## Status

**BLOCKER — live smoke not completed against T012 bits.**

Observations this session:

- Host process is running (`Tooba.Host` PID locked `bin/Debug` DLL) and answers HTTP 200 on health.
- That process still holds the **pre-T012** binary; Host rebuild could not copy `Tooba.Host.dll` (file lock).
- FE probe returned **308** (redirect); no confirmed Next.js `:3000` edit session against new Content editor.

Therefore fa/en Draft edit / DAM / Featured / SEO / Category / Author persistence was **not** claimed as PASS.

## Compile note

`dotnet build` produced **no C# errors**; failure was MSB3027 copy-to-bin while Host was running. Restart Host after push to load T012 endpoints, then re-smoke.

## Planned checks after Host+FE restart

1. Edit body + Save (fa + en Draft)
2. Insert DAM image
3. Set/remove Featured
4. SEO use-featured vs explicit + effective preview
5. Valid Category + Author change
6. Reload persistence
7. No raw Bad Request / `content.update.rejected` / JSON in UI
