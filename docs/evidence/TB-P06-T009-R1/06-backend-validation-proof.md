# 06 — Backend validation proof (TB-P06-T009-R1)

```text
dotnet restore src/backend/Tooba.slnx  -> OK
dotnet build src/backend/Tooba.slnx    -> 0 Warning(s) 0 Error(s)
dotnet test src/backend/Tooba.slnx     -> Passed: 237 total (233 Host + 4 MigrationRunner), Failed: 0, Skipped: 0
dotnet test --filter FulfillmentFoundationTests -> Passed: 3, Failed: 0
```

Build rerun after stopping running Host to avoid DLL file-lock errors.
