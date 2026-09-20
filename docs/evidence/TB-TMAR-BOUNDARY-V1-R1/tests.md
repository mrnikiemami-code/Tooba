# Tests — TB-TMAR-BOUNDARY-V1-R1

Command:

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter "FullyQualifiedName~TmarSourceSizeAndInfraAppTests|FullyQualifiedName~TmarFoundationTests|FullyQualifiedName~ArchitectureBoundaryTests"
```

Result: Passed! Failed: 0, Passed: 24, Total: 24

Covered:

- source-size inventory generation proof (`Source_size_inventory_evidence_exists_and_matches_scan_count`)
- source-size architecture guard (`Hand_written_source_size_does_not_expand_beyond_baseline`)
- synthetic NEW >800 LOC rejection
- synthetic oversized growth rejection
- synthetic shrink allowed
- Infrastructure→foreign Application baseline guard
- ArchitectureBoundaryTests (existing suite)
- TmarFoundationTests (existing Domain/Infra Domain + Host write + App→App + …)
