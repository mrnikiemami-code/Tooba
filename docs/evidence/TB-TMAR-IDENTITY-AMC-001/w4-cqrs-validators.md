# TB-TMAR-IDENTITY-AMC-001 — W4 CQRS + validators

## Scope

- Application Auth Commands/Queries + FluentValidation for endpoint-reachable requests
- Endpoints dispatch via `ISender` + `ApiResponseFactory.From` (register keeps 201 JSON)
- Host registers Identity Application assembly in `AddToobaCqrsFoundation`
- Host auth platform seams retained (throttle/session/middleware)

## Endpoint-reachable request matrix

| Request | Classification | Reason |
| --- | --- | --- |
| RegisterAuthUserCommand | VALIDATOR_REQUIRED | identifier kind/value/password shape |
| CompletePasswordResetCommand | VALIDATOR_REQUIRED | challenge/secret/password shape |
| RequestIdentifierVerificationCommand | VALIDATOR_REQUIRED | kind/identifier shape |
| CompleteIdentifierVerificationCommand | VALIDATOR_REQUIRED | challenge/secret shape |
| RequestOtpLoginCommand | VALIDATOR_REQUIRED | mobile identifier shape |
| ChangePasswordCommand | VALIDATOR_REQUIRED | current/new password shape |
| LoginWithPasswordCommand | NO_VALIDATOR_REQUIRED | enumeration-safe auth collapse to 401 |
| RefreshAuthSessionCommand | NO_VALIDATOR_REQUIRED | enumeration-safe session collapse to 401 |
| CompleteOtpLoginCommand | NO_VALIDATOR_REQUIRED | enumeration-safe auth collapse to 401 |
| RequestPasswordResetCommand | NO_VALIDATOR_REQUIRED | always-accept enumeration-safe |
| LogoutSessionCommand | NO_VALIDATOR_REQUIRED | trusted session context only |
| LogoutAllSessionsCommand | NO_VALIDATOR_REQUIRED | trusted session context only |
| GetAuthMeQuery | NO_VALIDATOR_REQUIRED | trusted session context only |

## Coupling

- Identity.Application → CustomerProfile.Contracts only (Me projection)
- Identity.Endpoints → Application + Contracts + BuildingBlocks (no foreign App/Infra/Domain)
- Zero Identity → foreign Application/Infrastructure/Domain

## Durable guard

`IdentityValidatorCoverageGuardTests` in Host.Tests.
