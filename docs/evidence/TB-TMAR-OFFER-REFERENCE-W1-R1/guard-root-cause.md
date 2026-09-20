# guard-root-cause

## Why previous PASS was possible despite physical mismatch

Root cause: **guard checked project/csproj boundaries and Host route absence, plus source-size, but did not assert on-disk folder placement or namespace↔folder alignment.**

Previous `OfferArchitectureGuardTests` verified:

- Domain/Application/Endpoints project references
- no production file >800 LOC
- Host SellerPanelEndpoints lacked `/offers` maps

It did **not** verify:

- files live under Aggregates/Ports/Adapters/etc.
- namespaces match physical folders
- empty ceremonial folders absent
- no flat root dumping-ground

## Repair

Added durable lock **ARCH-MODULE-PHYSICAL-001** via `OfferPhysicalStructureGuardTests`:

- projects exist on disk
- no flat root dumping-ground
- production files under approved folders
- namespaces align with folders
- no empty ceremonial folders
- Offer routes absent from Host + present in module Endpoints

Namespace/docs alone are insufficient for COMPLETE_REFERENCE_PATTERN.
