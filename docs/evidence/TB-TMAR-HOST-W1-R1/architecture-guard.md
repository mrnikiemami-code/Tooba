# TB-TMAR-HOST-W1-R1 Architecture Guard

Guard added:
`TmarFoundationTests.Module_business_MediatR_handlers_do_not_live_in_Infrastructure`

Scans `src/backend/Modules/**/*.Infrastructure/**/*.cs` for `IRequestHandler<`.
Fails if any module business MediatR handler lives in Infrastructure.

After repair: zero offenders.
Handlers reside in `Tooba.Catalog.Application/StoreLandingPageWriteHandlers.cs`.
