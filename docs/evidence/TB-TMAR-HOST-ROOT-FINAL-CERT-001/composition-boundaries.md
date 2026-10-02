# Composition boundaries — TB-TMAR-HOST-ROOT-FINAL-CERT-001

## Program business authority

ZERO: no domain decisions, no ISender.Send, no module business lambdas, no repository/DbContext/SQL/transactions in Program.

## Module endpoint ownership

PRESERVED: business HTTP via module Map* composition methods only. Direct Program routes = platform diagnostics only (gated).

## CQRS / MediatR

Canonical `AddToobaCqrsFoundation` with assembly-type registration markers only. No second pipeline, no direct handler invocation.

## Foreign references (Program + csproj)

| State | Value |
| --- | --- |
| Foreign-Application-Composition-State | COMPOSITION_ONLY |
| Foreign-Infrastructure-Composition-State | COMPOSITION_ONLY |
| Foreign-Domain-Dependency-State | ZERO (no Domain project references) |
| Foreign-DbContext-State | ZERO |
| Foreign-Business-Invocation-State | ZERO |

## Persistence authority in Program

ZERO (no SaveChanges / BeginTransaction / DbSet / ExecuteSql / NpgsqlConnection for business).

## Labels

`PROGRAM_BUSINESS_AUTHORITY_ZERO`  
`MODULE_ENDPOINT_OWNERSHIP_PRESERVED`
