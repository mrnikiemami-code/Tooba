# TB-TMAR-OFFER-REFERENCE-VS-STRUCTURE-R1

## Offer disk (unchanged)
src/backend/Modules/Offer/{Application,Contracts,Domain,Endpoints,Infrastructure,Tests}

## Before
Logical folder: /Modules/ (flat)
Offer projects under /Modules/: Domain, Application, Infrastructure (+ local uncommitted Contracts/Endpoints/Tests from prior visibility fix)

## After
Logical folder: /Modules/Offer/
All 6 Offer projects nested under /Modules/Offer/ only
Disk paths unchanged
Other modules unchanged

## Validation
dotnet sln Tooba.slnx list → 6 Offer entries, 0 duplicates
dotnet build Tooba.slnx --no-restore → succeeded, 0 errors
