# TB-TMAR-USERPREFERENCE-AMC-001-W4-R1 — Validation

## Focused build

- `Tooba.UserPreference.Endpoints`

## Focused tests

- `UserPreferenceModuleAmcW3CqrsGuardTests`
- `UserPreferenceModuleAmcW4CertGuardTests`
- `HostPreferencesAmcGuardTests`
- `SettingsFoundationTests.Preference_and_operator_contracts_are_own_only` (contracts smoke)

## Search proof

Under `src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints`:

- `catch (PlatformHttpException` → ZERO
- `FromPlatformException(` → ZERO
- `catch (SemanticException` → ZERO
- foreign App/Infra/Domain project refs → ZERO
- `Order.Contracts` remains Endpoints Customer actor seam only

## Untouched

- schema/migrations
- Host production
- frontend
- Domain / Application / Infrastructure / Contracts
