# validation — TB-TMAR-HOST-ERRORS-AMC-001-W1

## Builds

- Tooba.BuildingBlocks: PASS
- Tooba.Host: PASS
- Tooba.Host.Tests: PASS

## Focused tests PASS

- HostErrorsAmcW1GuardTests (all)
- TenantResolutionTests (all)
- TenantResolutionPlatformErrorTests (edition 503 / connection 503)
- ErrorContractTests (all; Testing env + SingleStore fixture)
- PlatformOptionsValidatorTests / HostNormalizerTests (included in focused filter)
- TmarDurableGuardTests Recovery assertions (updated for W1 SoT)

## Notes

- ErrorContractTests switched from Development → Testing to avoid ValidateOnBuild DI gaps unrelated to Errors.
- One Host-boot catalog de-dupe in Order contributor (Catalog-owned reservation.policy descriptors).
