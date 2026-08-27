# 12 — Backend tests proof (TB-P06-T017)

## Suite

`src/backend/Host/Tooba.Host.Tests/StoryFoundationTests.cs`

| Test | Coverage |
|---|---|
| `Story_module_boundary_static_checks` | Schema `story`, `IStoryDirectory` methods, Active enum, admin auth guard present in endpoints |
| `Public_visibility_status_cta_reorder_locale_and_admin_auth_behave` | Seed ≥2 public fa; locale excludes English rail; video flag; Draft/Scheduled/Expired/Disabled hidden; unsafe CTA throws; reorder; admin 401/403 |

## Notes

- Postgres Testcontainers (`[Collection("PostgresSerial")]`); Skippable when Docker unavailable.
- Unsafe CTA assertion: create with `ctaType=external`, `ctaTarget=javascript:alert(1)` → `InvalidOperationException`.

## Command (Worker fills exact counts if needed)

```text
dotnet test src/backend/Host/Tooba.Host.Tests --filter StoryFoundation
```

See final runtime / CI commands in `17-final-validation.md` if numbers not yet pasted.
