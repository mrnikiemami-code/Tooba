# TB-TMAR-STORY-AMC-001-W5 — Exhaustive validator coverage

Mode: MIGRATE
Slice: VALIDATOR_COVERAGE
Parent: TB-TMAR-STORY-AMC-001-W4

## Inventory

| Classification | Count |
| --- | ---: |
| Endpoint-reachable MediatR requests | 25 |
| VALIDATOR_REQUIRED | 15 |
| NO_VALIDATOR_REQUIRED | 10 |

NO_VALIDATOR reasons: route/actor ids only (Get/Enable/Disable/Approve/Remove/Submit/ListSeller/GetPublic) — no transport body fields requiring FluentValidation.

## Durable guard

`StoryModuleAmcW5ValidatorGuardTests` enforces exact endpoint `new …Command/Query` inventory, DI discovery of the 15 validators, and absence of validators on the 10 NO_VALIDATOR requests.

## Microservice

Validators are Story.Application-local; no foreign module types.
