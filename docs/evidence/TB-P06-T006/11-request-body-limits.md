# 11 — Request body limits (TB-P06-T006)

## Configuration

| Key | Default | Production |
|---|---|---|
| `Tooba:AuthSecurity:MaxRequestBodyBytes` | `10485760` (10 MiB) | `10485760` |

Defined in `AuthSecurityHostOptions.MaxRequestBodyBytes`.

## Kestrel binding

`Program.cs`:

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = authSecurityOptions.MaxRequestBodyBytes;
});
```

Applies globally to all Host endpoints, including auth and storefront APIs.

## Validation

`AuthSecurityOptionsValidator` rejects `MaxRequestBodyBytes <= 0` at startup.

## Behavior

Requests exceeding the limit are rejected at the server layer (413 Payload Too Large from Kestrel) before application handlers execute.

No per-route override added in this task.
